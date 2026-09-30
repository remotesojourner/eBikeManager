using EBikeManager.Application.Utils;

namespace EBikeManager.Web.Utils;

public static class GoogleHealthCallback
{
    public const string Path = "integrations/google-health/callback";
    public const string Page = "settings/google-health";
    public const string ResultParameter = "googleHealth";
    public const string Connected = "connected";
    public const string Failed = "failed";

    public static Uri? For(string baseUri) =>
        Uri.TryCreate(new Uri(baseUri), Path, out var address) && GoogleHealthRedirect.IsAllowed(address) ? address : null;

    public static string ResultPage(string result) => $"/{Page}?{ResultParameter}={result}";
}
