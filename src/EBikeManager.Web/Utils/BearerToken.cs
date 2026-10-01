namespace EBikeManager.Web.Utils;

public static class BearerToken
{
    private const string Scheme = "Bearer ";

    public static string? FromRequest(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase) ? header[Scheme.Length..].Trim() : null;
    }
}
