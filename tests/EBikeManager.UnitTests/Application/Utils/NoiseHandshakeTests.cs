using System.Security.Cryptography;
using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class NoiseHandshakeTests
{
    private static readonly byte[] _prologue = Convert.FromHexString("4a6f686e2047616c74");
    private static readonly byte[] _psk = Convert.FromHexString("54686973206973206d7920417573747269616e20706572737065637469766521");
    private static readonly byte[] _initiatorEphemeral = Convert.FromHexString("893e28b9dc6ca8d611ab664754b8ceb7bac5117349a4439a6b0569da977c464a");
    private static readonly byte[] _responderEphemeral = Convert.FromHexString("bbdb4cdbd309f1a1f2e1456967fe288cadd6f712d65dc7b7793d5e63da6b375b");

    [Fact]
    public void TheHandshakeAndMessagesMatchThePublishedNoiseTestVectors()
    {
        var initiator = new NoiseHandshake(initiator: true, _psk, _prologue, _initiatorEphemeral);
        var responder = new NoiseHandshake(initiator: false, _psk, _prologue, _responderEphemeral);

        var first = initiator.WriteMessage(Hex("4c756477696720766f6e204d69736573"));
        Assert.Equal(Hex("ca35def5ae56cec33dc2036731ab14896bc4c75dbb07a61f879f8e3afa4c794479b962b8aff8485742ac32f905ba45369e2465fb59e138a93d67a0d1266b6a54"), first);
        Assert.Equal(Hex("4c756477696720766f6e204d69736573"), responder.ReadMessage(first));

        var second = responder.WriteMessage(Hex("4d757272617920526f746862617264"));
        Assert.Equal(Hex("95ebc60d2b1fa672c1f46a8aa265ef51bfe38e7ccb39ec5be34069f144808843d6062704d5a9c422a8e834423f8c1feada7e8d0d910a1a2cd030fb584221e3"), second);
        Assert.Equal(Hex("4d757272617920526f746862617264"), initiator.ReadMessage(second));

        var handshakeHash = Hex("f4d03dc34495c95729ea6de9e1b59004b59733102488b3e24bc441e0be208eaf");
        Assert.Equal(handshakeHash, initiator.HandshakeHash);
        Assert.Equal(handshakeHash, responder.HandshakeHash);

        var (initiatorSend, initiatorReceive) = initiator.Split();
        var (responderSend, responderReceive) = responder.Split();
        var third = initiatorSend.Encrypt(Hex("462e20412e20486179656b"));
        Assert.Equal(Hex("e632c3763d7669067383433197a3baddf146e9e70ad4b4e9e59e0f"), third);
        Assert.Equal(Hex("462e20412e20486179656b"), responderReceive.Decrypt(third));
        var fourth = responderSend.Encrypt(Hex("4361726c204d656e676572"));
        Assert.Equal(Hex("64c6bee32ea91c8474bb4c21d7a700109ad45af77b29764ba5eb1e"), fourth);
        Assert.Equal(Hex("4361726c204d656e676572"), initiatorReceive.Decrypt(fourth));
    }

    [Fact]
    public void ADifferentKeyFailsTheHandshake()
    {
        var initiator = new NoiseHandshake(initiator: true, _psk, _prologue);
        var responder = new NoiseHandshake(initiator: false, RandomNumberGenerator.GetBytes(NoiseHandshake.KeyLength), _prologue);

        var first = initiator.WriteMessage([]);

        Assert.ThrowsAny<CryptographicException>(() => responder.ReadMessage(first));
    }

    private static byte[] Hex(string value) => Convert.FromHexString(value);
}
