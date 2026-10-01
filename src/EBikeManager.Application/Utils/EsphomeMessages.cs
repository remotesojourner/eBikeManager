using EBikeManager.Application.Models;

namespace EBikeManager.Application.Utils;

internal static class EsphomeMessages
{
    public const uint ApiVersionMajor = 1;
    public const uint ApiVersionMinor = 10;

    public static byte[] HelloRequest(string clientInfo) =>
        new ProtoWriter().String(1, clientInfo).UInt32(2, ApiVersionMajor).UInt32(3, ApiVersionMinor).ToArray();

    public static byte[] GetTimeResponse(DateTimeOffset now) =>
        new ProtoWriter().Fixed32(1, (uint)now.ToUnixTimeSeconds()).ToArray();

    public static EsphomeDeviceInfo DeviceInfo(ReadOnlySpan<byte> payload)
    {
        var reader = new ProtoReader(payload);
        string? name = null, friendlyName = null, mac = null, version = null, project = null;
        while (reader.NextField(out var field))
        {
            if (reader.WireType != ProtoReader.LengthWire)
            {
                reader.Skip();
                continue;
            }

            switch (field)
            {
                case 2: name = reader.String(); break;
                case 3: mac = reader.String(); break;
                case 4: version = reader.String(); break;
                case 8: project = reader.String(); break;
                case 13: friendlyName = reader.String(); break;
                default: reader.Skip(); break;
            }
        }

        return new EsphomeDeviceInfo(name ?? "", Blank(friendlyName), Blank(mac), Blank(version), Blank(project));
    }

    public static EsphomeEntity? Entity(ReadOnlySpan<byte> payload)
    {
        var reader = new ProtoReader(payload);
        uint? key = null;
        string? name = null;
        while (reader.NextField(out var field))
        {
            switch (field)
            {
                case 2 when reader.WireType == ProtoReader.Fixed32Wire: key = reader.Fixed32(); break;
                case 3 when reader.WireType == ProtoReader.LengthWire: name = reader.String(); break;
                default: reader.Skip(); break;
            }
        }

        return key is { } found && name != null ? new EsphomeEntity(found, name) : null;
    }

    public static EsphomeState SensorState(ReadOnlySpan<byte> payload)
    {
        var reader = new ProtoReader(payload);
        uint key = 0;
        var value = 0f;
        var missing = false;
        while (reader.NextField(out var field))
        {
            switch (field)
            {
                case 1 when reader.WireType == ProtoReader.Fixed32Wire: key = reader.Fixed32(); break;
                case 2 when reader.WireType == ProtoReader.Fixed32Wire: value = reader.Float(); break;
                case 3 when reader.WireType == ProtoReader.VarintWire: missing = reader.Bool(); break;
                default: reader.Skip(); break;
            }
        }

        return new EsphomeState(key, missing || float.IsNaN(value) ? null : value, null);
    }

    public static EsphomeState BinarySensorState(ReadOnlySpan<byte> payload)
    {
        var reader = new ProtoReader(payload);
        uint key = 0;
        var value = false;
        var missing = false;
        while (reader.NextField(out var field))
        {
            switch (field)
            {
                case 1 when reader.WireType == ProtoReader.Fixed32Wire: key = reader.Fixed32(); break;
                case 2 when reader.WireType == ProtoReader.VarintWire: value = reader.Bool(); break;
                case 3 when reader.WireType == ProtoReader.VarintWire: missing = reader.Bool(); break;
                default: reader.Skip(); break;
            }
        }

        return new EsphomeState(key, null, missing ? null : value);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
