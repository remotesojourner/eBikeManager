using System.Buffers.Binary;
using System.Text;

namespace EBikeManager.Application.Utils;

internal sealed class ProtoWriter
{
    private const int VarintWire = 0;
    private const int LengthWire = 2;
    private const int Fixed32Wire = 5;

    private readonly List<byte> _bytes = [];

    public ProtoWriter String(int field, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Tag(field, LengthWire);
        Varint((ulong)bytes.Length);
        _bytes.AddRange(bytes);
        return this;
    }

    public ProtoWriter UInt32(int field, uint value)
    {
        Tag(field, VarintWire);
        Varint(value);
        return this;
    }

    public ProtoWriter Bool(int field, bool value) => UInt32(field, value ? 1u : 0u);

    public ProtoWriter Fixed32(int field, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        Tag(field, Fixed32Wire);
        _bytes.AddRange(bytes);
        return this;
    }

    public ProtoWriter Float(int field, float value) => Fixed32(field, BitConverter.SingleToUInt32Bits(value));

    public byte[] ToArray() => [.. _bytes];

    private void Tag(int field, int wireType) => Varint((ulong)((field << 3) | wireType));

    private void Varint(ulong value)
    {
        while (value >= 0x80)
        {
            _bytes.Add((byte)(value | 0x80));
            value >>= 7;
        }

        _bytes.Add((byte)value);
    }
}
