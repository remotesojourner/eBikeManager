using EBikeManager.Web.Configuration;
using Microsoft.Extensions.Configuration;

namespace EBikeManager.UnitTests.Web.Configuration;

public class EBikeManagerOptionsSetupTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData(" 1 ", true)]
    [InlineData("yes", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void DisableAuthIsOnlySwitchedOnByTrueOneOrYes(string? value, bool expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [EBikeManagerOptionsSetup.DisableAuthVariable] = value })
            .Build();

        Assert.Equal(expected, EBikeManagerOptionsSetup.Read(configuration).DisableAuth);
    }
}
