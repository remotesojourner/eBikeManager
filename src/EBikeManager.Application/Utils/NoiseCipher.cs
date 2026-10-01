using System.Buffers.Binary;
using System.Security.Cryptography;

namespace EBikeManager.Application.Utils;

internal sealed class NoiseCipher
{
    public const int TagLength = 16;

    private const int NonceLength = 12;

    private readonly byte[] _key;
    private ulong _nonce;

    public NoiseCipher(byte[] key)
    {
        _key = key;
    }

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData = default)
    {
        var output = new byte[plaintext.Length + TagLength];
        using var aead = new ChaCha20Poly1305(_key);
        aead.Encrypt(NextNonce(), plaintext, output.AsSpan(0, plaintext.Length), output.AsSpan(plaintext.Length), associatedData);
        return output;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> associatedData = default)
    {
        if (ciphertext.Length < TagLength) throw new CryptographicException("The encrypted message is too short.");

        var output = new byte[ciphertext.Length - TagLength];
        using var aead = new ChaCha20Poly1305(_key);
        aead.Decrypt(NextNonce(), ciphertext[..output.Length], ciphertext[output.Length..], output, associatedData);
        return output;
    }

    private byte[] NextNonce()
    {
        var nonce = new byte[NonceLength];
        BinaryPrimitives.WriteUInt64LittleEndian(nonce.AsSpan(4), _nonce++);
        return nonce;
    }
}
