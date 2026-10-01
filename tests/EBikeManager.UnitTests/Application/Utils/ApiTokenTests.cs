using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class ApiTokenTests
{
    [Fact]
    public void TokensAreRandomAndOnlyMatchTheirOwnHash()
    {
        var token = ApiToken.Generate();
        var other = ApiToken.Generate();
        var hash = ApiToken.Hash(token);

        Assert.StartsWith(ApiToken.Prefix, token, StringComparison.Ordinal);
        Assert.NotEqual(token, other);
        Assert.DoesNotContain(token, hash, StringComparison.Ordinal);
        Assert.True(ApiToken.Matches(token, hash));
        Assert.False(ApiToken.Matches(other, hash));
        Assert.False(ApiToken.Matches(null, hash));
        Assert.False(ApiToken.Matches(token, null));
    }
}
