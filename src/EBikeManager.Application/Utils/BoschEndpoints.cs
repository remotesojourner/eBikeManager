namespace EBikeManager.Application.Utils;

public static class BoschEndpoints
{
    public const string AuthorizeUrl = "https://p9.authz.bosch.com/auth/realms/obc/protocol/openid-connect/auth";
    public const string TokenUrl = "https://p9.authz.bosch.com/auth/realms/obc/protocol/openid-connect/token";

    public const string ClientId = "one-bike-app";
    public const string RedirectUri = "onebikeapp-ios://com.bosch.ebike.onebikeapp/oauth2redirect";
    public const string Scope = "openid offline_access";

    public static Uri ProfileApi { get; } = new("https://obc-rider-profile.prod.connected-biking.cloud/");

    public static Uri ActivityApi { get; } = new("https://obc-rider-activity.prod.connected-biking.cloud/");

    public static Uri BikePassApi { get; } = new("https://bike-pass.prod.connected-biking.cloud/");

    public static Uri TheftDetectionApi { get; } = new("https://theft-detection.prod.connected-biking.cloud/");

    public static Uri InAppPurchaseApi { get; } = new("https://in-app-purchase.prod.connected-biking.cloud/");
}
