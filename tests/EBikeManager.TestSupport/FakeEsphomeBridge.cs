using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Utils;

namespace EBikeManager.TestSupport;

internal sealed class FakeEsphomeBridge : IAsyncDisposable
{
    public const string DeviceName = "ebike-bridge-1a2b3c";
    public const string FriendlyName = "eBike Bridge";
    public const string EsphomeVersion = "2025.11.2";
    public const string Battery = "Battery SoC (Live)";
    public const string Odometer = "Odometer (Live)";
    public const string Speed = "Speed";
    public const string Connected = "Connected";
    public const string SystemLocked = "System Locked";
    public const string ChargerConnected = "Charger Connected";

    private static readonly string[] _sensors = ["Speed", "Cadence", "Rider Power", "Ambient Brightness", Battery, Odometer, "Charge Time to 80% (Estimate)", "Charge Time to 100% (Estimate)"];
    private static readonly string[] _binarySensors = [Connected, "Light", SystemLocked, ChargerConnected, "Light Reserve", "Diagnosis Active", "In Motion"];
    private static readonly byte[] _prologue = "NoiseAPIInit\0\0"u8.ToArray();

    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly byte[]? _key;
    private readonly bool _dual;
    private readonly List<(uint Key, string Name, bool Binary)> _entities = [];
    private readonly Dictionary<uint, object> _states = [];
    private readonly List<FakeEsphomeClient> _clients = [];
    private readonly Lock _lock = new();
    private readonly Task _accepting;

    public FakeEsphomeBridge(byte[]? encryptionKey = null, bool dual = false)
    {
        _key = encryptionKey;
        _dual = dual;
        string[] prefixes = dual ? ["eBike 1 ", "eBike 2 "] : ["eBike "];
        uint key = 100;
        foreach (var prefix in prefixes)
        {
            foreach (var name in _sensors) _entities.Add((key++, prefix + name, false));
            foreach (var name in _binarySensors) _entities.Add((key++, prefix + name, true));
        }

        _entities.Add((key, "WiFi Signal", false));
        _listener.Start();
        _accepting = AcceptAsync(_stop.Token);
    }

    public static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public string Address => $"127.0.0.1:{Port}";

    public int Connections { get; private set; }

    public Task SetAsync(string sensor, float value, int slot = 1) => PublishAsync(sensor, slot, value);

    public Task SetAsync(string sensor, bool value, int slot = 1) => PublishAsync(sensor, slot, value);

    public async Task DisconnectEveryoneAsync()
    {
        List<FakeEsphomeClient> clients;
        lock (_lock)
        {
            clients = [.. _clients];
            _clients.Clear();
        }

        foreach (var client in clients) await client.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        await DisconnectEveryoneAsync();
        try
        {
            await _accepting;
        }
        catch (Exception ex) when (ex is OperationCanceledException or SocketException or ObjectDisposedException)
        {
        }

        _stop.Dispose();
    }

    private async Task PublishAsync(string sensor, int slot, object value)
    {
        var name = (_dual ? $"eBike {slot} " : "eBike ") + sensor;
        var entity = _entities.Single(entity => entity.Name == name);
        List<FakeEsphomeClient> subscribed;
        lock (_lock)
        {
            _states[entity.Key] = value;
            subscribed = [.. _clients.Where(client => client.Subscribed)];
        }

        foreach (var client in subscribed) await SendStateAsync(client, entity.Key, value, _stop.Token);
    }

    private async Task AcceptAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = new FakeEsphomeClient(await _listener.AcceptTcpClientAsync(cancellationToken));
            lock (_lock)
            {
                _clients.Add(client);
                Connections++;
            }

            _ = ServeAsync(client, cancellationToken);
        }
    }

    private async Task ServeAsync(FakeEsphomeClient client, CancellationToken cancellationToken)
    {
        try
        {
            if (_key != null && !await HandshakeAsync(client, _key, cancellationToken)) return;
            while (!cancellationToken.IsCancellationRequested)
            {
                var (type, _) = await client.ReadAsync(cancellationToken);
                await AnswerAsync(client, type, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is IOException or EndOfStreamException or ObjectDisposedException or OperationCanceledException or InvalidDataException or SocketException or CryptographicException)
        {
        }
        finally
        {
            lock (_lock) _clients.Remove(client);
            await client.DisposeAsync();
        }
    }

    private static async Task<bool> HandshakeAsync(FakeEsphomeClient client, byte[] key, CancellationToken cancellationToken)
    {
        if (await client.ReadByteAsync(cancellationToken) != 0x01)
        {
            await client.WriteRawAsync(NoiseFrame([0x01, .. "Bad indicator byte"u8]), cancellationToken);
            return false;
        }

        var rest = new byte[2];
        await client.Stream.ReadExactlyAsync(rest, cancellationToken);
        var hello = await client.ReadNoiseFrameAsync(cancellationToken);
        var handshake = new NoiseHandshake(initiator: false, key, _prologue);
        try
        {
            handshake.ReadMessage(hello.AsSpan(1));
        }
        catch (CryptographicException)
        {
            await client.WriteRawAsync(NoiseFrame([0x01, .. Encoding.UTF8.GetBytes(DeviceName), 0x00]), cancellationToken);
            await client.WriteRawAsync(NoiseFrame([0x01, .. "Handshake MAC failure"u8]), cancellationToken);
            return false;
        }

        await client.WriteRawAsync(NoiseFrame([0x01, .. Encoding.UTF8.GetBytes(DeviceName), 0x00, .. "AA:BB:CC:1A:2B:3C"u8, 0x00]), cancellationToken);
        await client.WriteRawAsync(NoiseFrame([0x00, .. handshake.WriteMessage([])]), cancellationToken);
        (client.SendCipher, client.ReceiveCipher) = handshake.Split();
        return true;
    }

    private async Task AnswerAsync(FakeEsphomeClient client, EsphomeMessageType type, CancellationToken cancellationToken)
    {
        switch (type)
        {
            case EsphomeMessageType.HelloRequest:
                await client.WriteAsync(EsphomeMessageType.HelloResponse, new ProtoWriter().UInt32(1, 1).UInt32(2, 10).String(3, EsphomeVersion).String(4, DeviceName).ToArray(), cancellationToken);
                break;
            case EsphomeMessageType.DeviceInfoRequest:
                await client.WriteAsync(EsphomeMessageType.DeviceInfoResponse, new ProtoWriter()
                    .String(2, DeviceName).String(3, "AA:BB:CC:1A:2B:3C").String(4, EsphomeVersion)
                    .String(8, _dual ? "xunil99.bosch_ebike_ldi_dual" : "xunil99.bosch_ebike_ldi").String(13, FriendlyName).ToArray(), cancellationToken);
                break;
            case EsphomeMessageType.ListEntitiesRequest:
                foreach (var entity in _entities)
                {
                    await client.WriteAsync(
                        entity.Binary ? EsphomeMessageType.ListEntitiesBinarySensorResponse : EsphomeMessageType.ListEntitiesSensorResponse,
                        new ProtoWriter().String(1, entity.Name.ToLowerInvariant().Replace(' ', '_')).Fixed32(2, entity.Key).String(3, entity.Name).ToArray(),
                        cancellationToken);
                }

                await client.WriteAsync(EsphomeMessageType.ListEntitiesDoneResponse, [], cancellationToken);
                break;
            case EsphomeMessageType.SubscribeStatesRequest:
                List<KeyValuePair<uint, object>> states;
                lock (_lock)
                {
                    client.Subscribed = true;
                    states = [.. _states];
                }

                foreach (var (key, value) in states) await SendStateAsync(client, key, value, cancellationToken);
                break;
            case EsphomeMessageType.PingRequest:
                await client.WriteAsync(EsphomeMessageType.PingResponse, [], cancellationToken);
                break;
            case EsphomeMessageType.DisconnectRequest:
                await client.WriteAsync(EsphomeMessageType.DisconnectResponse, [], cancellationToken);
                break;
        }
    }

    private static Task SendStateAsync(FakeEsphomeClient client, uint key, object value, CancellationToken cancellationToken) => value switch
    {
        bool flag => client.WriteAsync(EsphomeMessageType.BinarySensorStateResponse, new ProtoWriter().Fixed32(1, key).Bool(2, flag).ToArray(), cancellationToken),
        float number => client.WriteAsync(EsphomeMessageType.SensorStateResponse, new ProtoWriter().Fixed32(1, key).Float(2, number).ToArray(), cancellationToken),
        _ => Task.CompletedTask
    };

    private static byte[] NoiseFrame(byte[] content) => [0x01, (byte)(content.Length >> 8), (byte)content.Length, .. content];
}
