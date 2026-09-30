using System.Net;
using EBikeManager.Application.Services;
using EBikeManager.TestSupport;

namespace EBikeManager.UnitTests.Application.Services;

public class OidcDiscoveryServiceTests
{
    private const string Authority = "https://auth.example.com/application/o/ebike-manager/";

    [Fact]
    public async Task AProviderWithADiscoveryDocumentPasses()
    {
        var handler = new RecordingHandler(_ => RecordingHandler.Json("""{ "authorization_endpoint": "https://auth.example.com/authorize", "token_endpoint": "https://auth.example.com/token" }"""));

        var problem = await new OidcDiscoveryService(new StubHttpClientFactory(handler)).FindProblemAsync(Authority, TestContext.Current.CancellationToken);

        Assert.Null(problem);
        Assert.Equal(new Uri("https://auth.example.com/application/o/ebike-manager/.well-known/openid-configuration"), Assert.Single(handler.Requests).Uri);
    }

    [Fact]
    public async Task AnAddressThatIsNotAProviderIsExplained()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var missing = new OidcDiscoveryService(new StubHttpClientFactory(new RecordingHandler(_ => RecordingHandler.Json("{}", HttpStatusCode.NotFound))));
        var notDiscovery = new OidcDiscoveryService(new StubHttpClientFactory(new RecordingHandler(_ => RecordingHandler.Json("""{ "issuer": "https://auth.example.com" }"""))));
        var unreachable = new OidcDiscoveryService(new StubHttpClientFactory(new RecordingHandler(_ => throw new HttpRequestException("No such host"))));

        Assert.Contains("404", await missing.FindProblemAsync(Authority, cancellationToken), StringComparison.Ordinal);
        Assert.Contains("isn't an OpenID Connect discovery document", await notDiscovery.FindProblemAsync(Authority, cancellationToken), StringComparison.Ordinal);
        Assert.Contains("No such host", await unreachable.FindProblemAsync(Authority, cancellationToken), StringComparison.Ordinal);
    }
}
