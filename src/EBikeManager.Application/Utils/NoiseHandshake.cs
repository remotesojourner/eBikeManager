using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Math.EC.Rfc7748;

namespace EBikeManager.Application.Utils;

internal sealed class NoiseHandshake
{
    public const string ProtocolName = "Noise_NNpsk0_25519_ChaChaPoly_SHA256";
    public const int KeyLength = 32;

    private readonly bool _initiator;
    private readonly byte[] _psk;
    private readonly byte[] _ephemeralPrivate;
    private readonly byte[] _ephemeralPublic = new byte[KeyLength];
    private byte[] _chainingKey;
    private byte[] _hash;
    private NoiseCipher? _cipher;
    private byte[]? _remoteEphemeral;

    public NoiseHandshake(bool initiator, ReadOnlySpan<byte> psk, ReadOnlySpan<byte> prologue, byte[]? ephemeralPrivate = null)
    {
        if (psk.Length != KeyLength) throw new ArgumentException("The pre-shared key must be 32 bytes.", nameof(psk));

        _initiator = initiator;
        _psk = psk.ToArray();
        _ephemeralPrivate = ephemeralPrivate ?? RandomNumberGenerator.GetBytes(KeyLength);
        X25519.GeneratePublicKey(_ephemeralPrivate, 0, _ephemeralPublic, 0);
        _hash = SHA256.HashData(Encoding.ASCII.GetBytes(ProtocolName));
        _chainingKey = [.. _hash];
        MixHash(prologue);
    }

    public byte[] HandshakeHash => [.. _hash];

    public byte[] WriteMessage(ReadOnlySpan<byte> payload)
    {
        if (_initiator) MixKeyAndHash(_psk);
        MixHash(_ephemeralPublic);
        MixKey(_ephemeralPublic);
        if (!_initiator) MixKey(Agree(_remoteEphemeral ?? throw new InvalidOperationException("The initiator's message hasn't been read.")));
        return [.. _ephemeralPublic, .. EncryptAndHash(payload)];
    }

    public byte[] ReadMessage(ReadOnlySpan<byte> message)
    {
        if (message.Length < KeyLength) throw new CryptographicException("The handshake message is too short.");

        if (!_initiator) MixKeyAndHash(_psk);
        _remoteEphemeral = message[..KeyLength].ToArray();
        MixHash(_remoteEphemeral);
        MixKey(_remoteEphemeral);
        if (_initiator) MixKey(Agree(_remoteEphemeral));
        return DecryptAndHash(message[KeyLength..]);
    }

    public (NoiseCipher Send, NoiseCipher Receive) Split()
    {
        var keys = Hkdf(_chainingKey, [], 2);
        var first = new NoiseCipher(keys[0]);
        var second = new NoiseCipher(keys[1]);
        return _initiator ? (first, second) : (second, first);
    }

    private byte[] EncryptAndHash(ReadOnlySpan<byte> plaintext)
    {
        var ciphertext = _cipher?.Encrypt(plaintext, _hash) ?? plaintext.ToArray();
        MixHash(ciphertext);
        return ciphertext;
    }

    private byte[] DecryptAndHash(ReadOnlySpan<byte> ciphertext)
    {
        var plaintext = _cipher?.Decrypt(ciphertext, _hash) ?? ciphertext.ToArray();
        MixHash(ciphertext);
        return plaintext;
    }

    private byte[] Agree(byte[] remotePublic)
    {
        var shared = new byte[KeyLength];
        if (!X25519.CalculateAgreement(_ephemeralPrivate, 0, remotePublic, 0, shared, 0)) throw new CryptographicException("The handshake key agreement failed.");
        return shared;
    }

    private void MixHash(ReadOnlySpan<byte> data) => _hash = SHA256.HashData([.. _hash, .. data]);

    private void MixKey(ReadOnlySpan<byte> inputKeyMaterial)
    {
        var keys = Hkdf(_chainingKey, inputKeyMaterial, 2);
        _chainingKey = keys[0];
        _cipher = new NoiseCipher(keys[1]);
    }

    private void MixKeyAndHash(ReadOnlySpan<byte> inputKeyMaterial)
    {
        var keys = Hkdf(_chainingKey, inputKeyMaterial, 3);
        _chainingKey = keys[0];
        MixHash(keys[1]);
        _cipher = new NoiseCipher(keys[2]);
    }

    private static byte[][] Hkdf(byte[] chainingKey, ReadOnlySpan<byte> inputKeyMaterial, int outputs)
    {
        var tempKey = HMACSHA256.HashData(chainingKey, inputKeyMaterial);
        var result = new byte[outputs][];
        byte[] previous = [];
        for (var index = 0; index < outputs; index++)
        {
            byte[] block = [.. previous, (byte)(index + 1)];
            previous = HMACSHA256.HashData(tempKey, block);
            result[index] = previous;
        }

        return result;
    }
}
