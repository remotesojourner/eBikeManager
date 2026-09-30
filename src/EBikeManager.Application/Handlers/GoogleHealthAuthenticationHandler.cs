using System.Net;
using System.Net.Http.Headers;
using EBikeManager.Application.Services;

namespace EBikeManager.Application.Handlers;

internal sealed class GoogleHealthAuthenticationHandler : DelegatingHandler
{
    private readonly GoogleHealthConnectionService _connection;

    public GoogleHealthAuthenticationHandler(GoogleHealthConnectionService connection)
    {
        _connection = connection;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _connection.GetAccessTokenAsync(cancellationToken: cancellationToken));
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;

        response.Dispose();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _connection.GetAccessTokenAsync(forceRefresh: true, cancellationToken));
        return await base.SendAsync(request, cancellationToken);
    }
}
