namespace EBikeManager.TestSupport;

internal sealed record RecordedRequest(HttpMethod Method, Uri? Uri, string Body)
{
    public IReadOnlyList<string> Headers { get; init; } = [];
}
