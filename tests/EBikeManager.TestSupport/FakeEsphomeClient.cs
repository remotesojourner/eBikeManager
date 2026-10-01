using System.Net.Sockets;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Utils;

namespace EBikeManager.TestSupport;

internal sealed class FakeEsphomeClient : IAsyncDisposable
{
    private readonly TcpClient _tcp;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FakeEsphomeClient(TcpClient tcp)
    {
        _tcp = tcp;
        Stream = tcp.GetStream();
    }

    public NetworkStream Stream { get; }

    public NoiseCipher? SendCipher { get; set; }

    public NoiseCipher? ReceiveCipher { get; set; }

    public bool Subscribed { get; set; }

    public async Task WriteRawAsync(byte[] bytes, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await Stream.WriteAsync(bytes, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public Task WriteAsync(EsphomeMessageType type, byte[] payload, CancellationToken cancellationToken)
    {
        if (SendCipher is not { } cipher) return WriteRawAsync([0x00, .. Varint((uint)payload.Length), .. Varint((uint)type), .. payload], cancellationToken);

        var encrypted = cipher.Encrypt([(byte)((int)type >> 8), (byte)type, (byte)(payload.Length >> 8), (byte)payload.Length, .. payload]);
        return WriteRawAsync([0x01, (byte)(encrypted.Length >> 8), (byte)encrypted.Length, .. encrypted], cancellationToken);
    }

    public async Task<(EsphomeMessageType Type, byte[] Payload)> ReadAsync(CancellationToken cancellationToken)
    {
        if (ReceiveCipher is { } cipher)
        {
            var message = cipher.Decrypt(await ReadNoiseFrameAsync(cancellationToken));
            return ((EsphomeMessageType)((message[0] << 8) | message[1]), message[4..]);
        }

        if (await ReadByteAsync(cancellationToken) != 0x00) throw new InvalidDataException("Expected a plaintext frame.");
        var length = await ReadVarintAsync(cancellationToken);
        var type = await ReadVarintAsync(cancellationToken);
        var payload = new byte[length];
        await Stream.ReadExactlyAsync(payload, cancellationToken);
        return ((EsphomeMessageType)type, payload);
    }

    public async Task<byte[]> ReadNoiseFrameAsync(CancellationToken cancellationToken)
    {
        var header = new byte[3];
        await Stream.ReadExactlyAsync(header, cancellationToken);
        if (header[0] != 0x01) throw new InvalidDataException("Expected an encrypted frame.");
        var frame = new byte[(header[1] << 8) | header[2]];
        await Stream.ReadExactlyAsync(frame, cancellationToken);
        return frame;
    }

    public async Task<byte> ReadByteAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        await Stream.ReadExactlyAsync(buffer, cancellationToken);
        return buffer[0];
    }

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
        _tcp.Dispose();
        _writeLock.Dispose();
    }

    private async Task<int> ReadVarintAsync(CancellationToken cancellationToken)
    {
        var value = 0;
        for (var shift = 0; ; shift += 7)
        {
            var current = await ReadByteAsync(cancellationToken);
            value |= (current & 0x7F) << shift;
            if ((current & 0x80) == 0) return value;
        }
    }

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
