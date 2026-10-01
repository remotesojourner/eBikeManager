using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

internal sealed class EsphomeConnection : IAsyncDisposable
{
    public const byte PlainPreamble = 0x00;
    public const byte NoisePreamble = 0x01;
    public const string WrongKeyReply = "Handshake MAC failure";

    private const int MaxMessageLength = 64 * 1024;

    private static readonly byte[] _prologue = "NoiseAPIInit\0\0"u8.ToArray();

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private NoiseCipher? _send;
    private NoiseCipher? _receive;

    private EsphomeConnection(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }

    public static byte[] Prologue => [.. _prologue];

    public static async Task<EsphomeConnection> OpenAsync(string host, int port, byte[]? encryptionKey, CancellationToken cancellationToken)
    {
        var client = new TcpClient { NoDelay = true };
        try
        {
            await client.ConnectAsync(host, port, cancellationToken);
        }
        catch (SocketException ex)
        {
            client.Dispose();
            throw new BridgeConnectionException(ApplicationStrings.Format(ApplicationStrings.BridgeUnreachable, $"{host}:{port}", ex.Message), ex);
        }

        var connection = new EsphomeConnection(client);
        try
        {
            if (encryptionKey != null) await connection.HandshakeAsync(encryptionKey, cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task SendAsync(EsphomeMessageType type, byte[] payload, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _stream.WriteAsync(_send is { } cipher ? NoiseFrame(cipher, type, payload) : PlainFrame(type, payload), cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<EsphomeFrame> ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            return _receive is { } cipher ? await ReceiveNoiseAsync(cipher, cancellationToken) : await ReceivePlainAsync(cancellationToken);
        }
        catch (EndOfStreamException ex)
        {
            throw new BridgeConnectionException(ApplicationStrings.BridgeClosed, ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _client.Dispose();
        _writeLock.Dispose();
    }

    private async Task HandshakeAsync(byte[] encryptionKey, CancellationToken cancellationToken)
    {
        var handshake = new NoiseHandshake(initiator: true, encryptionKey, _prologue);
        var hello = handshake.WriteMessage([]);
        byte[] opening = [.. NoiseHeader(0), .. NoiseHeader(hello.Length + 1), PlainPreamble, .. hello];
        await _stream.WriteAsync(opening, cancellationToken);

        var serverHello = await ReadHandshakeFrameAsync(cancellationToken);
        if (serverHello is not [NoisePreamble, ..]) throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood);

        var reply = await ReadHandshakeFrameAsync(cancellationToken);
        if (reply.Length == 0) throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood);
        if (reply[0] != PlainPreamble)
        {
            var reason = Encoding.UTF8.GetString(reply.AsSpan(1));
            throw new BridgeConnectionException(reason == WrongKeyReply
                ? ApplicationStrings.BridgeWrongKey
                : ApplicationStrings.Format(ApplicationStrings.BridgeHandshakeFailed, reason));
        }

        try
        {
            handshake.ReadMessage(reply.AsSpan(1));
        }
        catch (CryptographicException ex)
        {
            throw new BridgeConnectionException(ApplicationStrings.BridgeWrongKey, ex);
        }

        (_send, _receive) = handshake.Split();
    }

    private async Task<byte[]> ReadHandshakeFrameAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ReadNoiseFrameAsync(cancellationToken);
        }
        catch (IOException ex)
        {
            throw new BridgeConnectionException(ApplicationStrings.BridgeNotEncrypted, ex);
        }
    }

    private async Task<byte[]> ReadNoiseFrameAsync(CancellationToken cancellationToken)
    {
        var header = new byte[3];
        await _stream.ReadExactlyAsync(header, cancellationToken);
        if (header[0] == PlainPreamble) throw new BridgeConnectionException(ApplicationStrings.BridgeNotEncrypted);
        if (header[0] != NoisePreamble) throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood);

        var frame = new byte[(header[1] << 8) | header[2]];
        await _stream.ReadExactlyAsync(frame, cancellationToken);
        return frame;
    }

    private async Task<EsphomeFrame> ReceiveNoiseAsync(NoiseCipher cipher, CancellationToken cancellationToken)
    {
        var frame = await ReadNoiseFrameAsync(cancellationToken);
        byte[] message;
        try
        {
            message = cipher.Decrypt(frame);
        }
        catch (CryptographicException ex)
        {
            throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood, ex);
        }

        if (message.Length < 4) throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood);
        return new EsphomeFrame((EsphomeMessageType)((message[0] << 8) | message[1]), message[4..]);
    }

    private async Task<EsphomeFrame> ReceivePlainAsync(CancellationToken cancellationToken)
    {
        var preamble = await ReadByteAsync(cancellationToken);
        if (preamble == NoisePreamble) throw new BridgeConnectionException(ApplicationStrings.BridgeNeedsKey);
        if (preamble != PlainPreamble) throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood);

        var length = await ReadVarintAsync(cancellationToken);
        var type = await ReadVarintAsync(cancellationToken);
        if (length > MaxMessageLength) throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood);

        var payload = new byte[length];
        await _stream.ReadExactlyAsync(payload, cancellationToken);
        return new EsphomeFrame((EsphomeMessageType)type, payload);
    }

    private async Task<int> ReadVarintAsync(CancellationToken cancellationToken)
    {
        var value = 0;
        for (var shift = 0; shift < 32; shift += 7)
        {
            var current = await ReadByteAsync(cancellationToken);
            value |= (current & 0x7F) << shift;
            if ((current & 0x80) == 0) return value;
        }

        throw new BridgeConnectionException(ApplicationStrings.BridgeNotUnderstood);
    }

    private async Task<byte> ReadByteAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        await _stream.ReadExactlyAsync(buffer, cancellationToken);
        return buffer[0];
    }

    private static byte[] PlainFrame(EsphomeMessageType type, byte[] payload) =>
        [PlainPreamble, .. Varint((uint)payload.Length), .. Varint((uint)type), .. payload];

    private static byte[] NoiseFrame(NoiseCipher cipher, EsphomeMessageType type, byte[] payload)
    {
        var encrypted = cipher.Encrypt([(byte)((int)type >> 8), (byte)type, (byte)(payload.Length >> 8), (byte)payload.Length, .. payload]);
        return [.. NoiseHeader(encrypted.Length), .. encrypted];
    }

    private static byte[] NoiseHeader(int length) => [NoisePreamble, (byte)(length >> 8), (byte)length];

    private static byte[] Varint(uint value)
    {
        var bytes = new List<byte>();
        while (value >= 0x80)
        {
            bytes.Add((byte)(value | 0x80));
            value >>= 7;
        }

        bytes.Add((byte)value);
        return [.. bytes];
    }
}
