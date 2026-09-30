using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

internal sealed class BoschApiService : IBoschApiService
{
    private readonly HttpClient _http;

    public BoschApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<BoschBikeInfo>> GetBikesAsync(CancellationToken cancellationToken = default)
    {
        using var json = await GetJsonAsync(new Uri(BoschEndpoints.ProfileApi, "v1/bike-profile"), cancellationToken);
        return BoschJson.ParseBikes(json.RootElement);
    }

    public async Task<BoschActivityPage> GetActivitiesAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var address = new Uri(BoschEndpoints.ActivityApi, FormattableString.Invariant($"v1/activity?page={page}&size={pageSize}&sort=-startTime&include-polyline=false"));
        using var json = await GetJsonAsync(address, cancellationToken);
        return BoschJson.ParseActivityPage(json.RootElement);
    }

    public async Task<byte[]?> DownloadFitAsync(string activityId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BoschEndpoints.ActivityApi, $"v1/activity/{Uri.EscapeDataString(activityId)}/export/fit"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<JsonDocument> GetJsonAsync(Uri address, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(address, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            ApplicationStrings.Format(
                ApplicationStrings.BoschRequestFailed,
                (int)response.StatusCode,
                response.ReasonPhrase,
                response.RequestMessage?.RequestUri?.AbsolutePath,
                body.Length > 300 ? body[..300] + "…" : body),
            null,
            response.StatusCode);
    }
}
