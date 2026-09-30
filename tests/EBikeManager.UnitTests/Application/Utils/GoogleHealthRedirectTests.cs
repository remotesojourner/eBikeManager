using EBikeManager.Application.Utils;

namespace EBikeManager.UnitTests.Application.Utils;

public class GoogleHealthRedirectTests
{
    [Theory]
    [InlineData("https://ebike.example.com/integrations/google-health/callback", true)]
    [InlineData("https://ebike.home.example.co.uk:8443/integrations/google-health/callback", true)]
    [InlineData("http://localhost:2004/integrations/google-health/callback", true)]
    [InlineData("http://127.0.0.1:2004/integrations/google-health/callback", true)]
    [InlineData("http://ebike.example.com/integrations/google-health/callback", false)]
    [InlineData("https://192.168.1.100:2004/integrations/google-health/callback", false)]
    [InlineData("http://192.168.1.100:2004/integrations/google-health/callback", false)]
    [InlineData("https://homeserver:2004/integrations/google-health/callback", false)]
    [InlineData("https://ebike.local/integrations/google-health/callback", false)]
    public void GoogleOnlyReturnsToHttpsDomainsOrThisComputer(string address, bool allowed)
    {
        Assert.Equal(allowed, GoogleHealthRedirect.IsAllowed(new Uri(address)));
    }
}
