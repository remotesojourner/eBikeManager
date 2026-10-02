using System.Net;
using System.Text;
using System.Text.Json;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Models.Events;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Notifications.Interfaces;

namespace EBikeManager.Application.Services.Notifications;

internal abstract class HttpNotificationService : INotificationTypeService
{
    public const string HttpClientName = "notifications";

    private const int ReplyExcerptLength = 200;

    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(15);

    private readonly IHttpClientFactory _httpClientFactory;

    protected HttpNotificationService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public abstract string Name { get; }

    public abstract NotificationTypeSchemaDto Schema { get; }

    public abstract Task<NotificationResult> HandleAsync(NotificationEvent notificationEvent, NotificationContext context, CancellationToken cancellationToken);

    public abstract Task<NotificationResult> SendTestAsync(NotificationContext context, RideDto sample, CancellationToken cancellationToken);

    protected async Task<NotificationResult> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpStatusCode, string, NotificationResult?>? judgeReply = null)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientName);
        client.Timeout = _requestTimeout;
        try
        {
            using (request)
            using (var response = await client.SendAsync(request, cancellationToken))
            {
                var reply = await response.Content.ReadAsStringAsync(cancellationToken);
                if (judgeReply?.Invoke(response.StatusCode, reply) is { } judged) return judged;

                return response.IsSuccessStatusCode ? NotificationResult.Sent : NotificationResult.Failed(Answered(response.StatusCode, reply));
            }
        }
        catch (HttpRequestException ex)
        {
            return NotificationResult.Failed(ApplicationStrings.Format(ApplicationStrings.NotificationUnreachable, Schema.Title, ex.Message));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return NotificationResult.Failed(ApplicationStrings.Format(ApplicationStrings.NotificationTimeout, Schema.Title, _requestTimeout.TotalSeconds));
        }
    }

    protected static HttpRequestMessage JsonPost(string url, object? payload) => new(HttpMethod.Post, url)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };

    protected static HttpRequestMessage TextPost(string url, string text) => new(HttpMethod.Post, url)
    {
        Content = new StringContent(text, Encoding.UTF8, "text/plain")
    };

    protected static bool IsSuccess(HttpStatusCode status) => (int)status is >= 200 and < 300;

    protected string Answered(HttpStatusCode status, string reply)
    {
        var excerpt = Excerpt(reply);
        return excerpt.Length == 0
            ? ApplicationStrings.Format(ApplicationStrings.NotificationAnswered, Schema.Title, (int)status)
            : ApplicationStrings.Format(ApplicationStrings.NotificationAnsweredWithReply, Schema.Title, (int)status, excerpt);
    }

    private static string Excerpt(string reply)
    {
        var singleLine = string.Join(' ', reply.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine.Length <= ReplyExcerptLength ? singleLine : singleLine[..ReplyExcerptLength] + "…";
    }
}
