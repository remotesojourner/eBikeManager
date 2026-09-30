using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class PasswordHashTests
{
    [Fact]
    public void VerifiesTheOriginalPasswordOnly()
    {
        var hash = PasswordHash.Create("correct horse");

        Assert.True(PasswordHash.Verify("correct horse", hash));
        Assert.False(PasswordHash.Verify("correct horsE", hash));
    }

    [Fact]
    public void SaltsEveryHash()
    {
        Assert.NotEqual(PasswordHash.Create("same password"), PasswordHash.Create("same password"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2-sha256$abc$c2FsdA==$a2V5")]
    [InlineData("pbkdf2-sha256$1000$not base64$a2V5")]
    public void RejectsMalformedHashes(string hash)
    {
        Assert.False(PasswordHash.Verify("anything", hash));
    }
}
