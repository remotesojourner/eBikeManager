using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EBikeManager.Application.Extensions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

internal sealed class BoschApiService : IBoschApiService
{
    public const string MediaHttpClientName = "bosch-media";

    private readonly HttpClient _http;
    private readonly IHttpClientFactory _httpClientFactory;

    public BoschApiService(HttpClient http, IHttpClientFactory httpClientFactory)
    {
        _http = http;
        _httpClientFactory = httpClientFactory;
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

    public Task<byte[]?> DownloadFitAsync(string activityId, CancellationToken cancellationToken = default) =>
        DownloadExportAsync(activityId, "fit", "application/octet-stream", cancellationToken);

    public Task<byte[]?> DownloadGpxAsync(string activityId, CancellationToken cancellationToken = default) =>
        DownloadExportAsync(activityId, "gpx", "application/xml", cancellationToken);

    private async Task<byte[]?> DownloadExportAsync(string activityId, string format, string mediaType, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BoschEndpoints.ActivityApi, $"v1/activity/{Uri.EscapeDataString(activityId)}/export/{format}"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<string?> GetBikeProfileJsonAsync(string bikeId, CancellationToken cancellationToken = default)
    {
        using var json = await GetOptionalJsonAsync(new Uri(BoschEndpoints.ProfileApi, $"v2/bike-profile/{Uri.EscapeDataString(bikeId)}"), cancellationToken);
        return json == null ? null : BoschJson.UnwrapProfile(json.RootElement);
    }

    public async Task<string?> GetStateOfChargeJsonAsync(string bikeId, CancellationToken cancellationToken = default)
    {
        using var json = await GetOptionalJsonAsync(new Uri(BoschEndpoints.ProfileApi, $"v1/state-of-charge/{Uri.EscapeDataString(bikeId)}"), cancellationToken);
        return json?.RootElement.ValueKind == JsonValueKind.Object ? json.RootElement.GetRawText() : null;
    }

    public async Task<string?> GetBikePassJsonAsync(string bikeId, CancellationToken cancellationToken = default)
    {
        using var json = await GetOptionalJsonAsync(new Uri(BoschEndpoints.BikePassApi, "v1/bike-passes"), cancellationToken);
        return json == null ? null : BoschJson.PassFor(json.RootElement, bikeId);
    }

    public async Task<string?> GetLatestLocationJsonAsync(string bikeId, CancellationToken cancellationToken = default)
    {
        using var json = await GetOptionalJsonAsync(new Uri(BoschEndpoints.TheftDetectionApi, $"v0/latest-locations?bikeId={Uri.EscapeDataString(bikeId)}"), cancellationToken);
        return json == null ? null : BoschJson.LatestLocation(json.RootElement);
    }

    public async Task<bool?> HasFlowPlusAsync(CancellationToken cancellationToken = default)
    {
        using var json = await GetOptionalJsonAsync(new Uri(BoschEndpoints.InAppPurchaseApi, "v1/subscription/status"), cancellationToken);
        return json?.RootElement.Flag("status");
    }

    public async Task<byte[]?> DownloadBikePictureAsync(Uri address, CancellationToken cancellationToken = default)
    {
        using var http = _httpClientFactory.CreateClient(MediaHttpClientName);
        using var response = await http.GetAsync(address, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]?> DownloadBikePassFileAsync(string bikeId, string fileId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(new Uri(BoschEndpoints.BikePassApi, $"v1/files/{Uri.EscapeDataString(bikeId)}/{Uri.EscapeDataString(fileId)}"), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task<JsonDocument?> GetOptionalJsonAsync(Uri address, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(address, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.NoContent) return null;

        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
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
