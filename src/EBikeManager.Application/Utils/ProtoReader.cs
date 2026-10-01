using System.Buffers.Binary;
using System.Text;

namespace EBikeManager.Application.Utils;

internal ref struct ProtoReader
{
    public const int VarintWire = 0;
    public const int Fixed64Wire = 1;
    public const int LengthWire = 2;
    public const int Fixed32Wire = 5;

    private const int MaxVarintBytes = 10;

    private readonly ReadOnlySpan<byte> _data;
    private int _position;

    public ProtoReader(ReadOnlySpan<byte> data)
    {
        _data = data;
    }

    public int WireType { get; private set; }

    public bool NextField(out int field)
    {
        if (_position >= _data.Length)
        {
            field = 0;
            return false;
        }

        var tag = Varint();
        field = (int)(tag >> 3);
        WireType = (int)(tag & 7);
        return true;
    }

    public ulong Varint()
    {
        ulong value = 0;
        for (var shift = 0; shift < MaxVarintBytes * 7; shift += 7)
        {
            var current = Take(1)[0];
            value |= (ulong)(current & 0x7F) << shift;
            if ((current & 0x80) == 0) return value;
        }

        throw new InvalidDataException("A protobuf varint is too long.");
    }

    public uint Fixed32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));

    public float Float() => BitConverter.UInt32BitsToSingle(Fixed32());

    public bool Bool() => Varint() != 0;

    public string String() => Encoding.UTF8.GetString(Take(checked((int)Varint())));

    public void Skip()
    {
        switch (WireType)
        {
            case VarintWire:
                Varint();
                break;
            case Fixed64Wire:
                Take(8);
                break;
            case LengthWire:
                Take(checked((int)Varint()));
                break;
            case Fixed32Wire:
                Take(4);
                break;
            default:
                throw new InvalidDataException($"Unknown protobuf wire type {WireType}.");
        }
    }

    private ReadOnlySpan<byte> Take(int count)
    {
        if (count < 0 || _position + count > _data.Length) throw new InvalidDataException("A protobuf message ended early.");

        var taken = _data.Slice(_position, count);
        _position += count;
        return taken;
    }
}
