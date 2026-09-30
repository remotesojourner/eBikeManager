namespace EBikeManager.Application.Utils;

public static class GoogleHealthEndpoints
{
    public const string AuthorizeUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    public const string TokenUrl = "https://oauth2.googleapis.com/token";
    public const string RevokeUrl = "https://oauth2.googleapis.com/revoke";

    public const string WriteScope = "https://www.googleapis.com/auth/googlehealth.activity_and_fitness.writeonly";
    public const string ReadScope = "https://www.googleapis.com/auth/googlehealth.activity_and_fitness.readonly";
    public const string Scope = "openid email " + WriteScope + " " + ReadScope;

    public const string ExercisePoints = "users/me/dataTypes/exercise/dataPoints";
    public const string WearablesFamily = "users/me/dataSourceFamilies/google-wearables";
    public const string DataPointPrefix = "ebike-";
    public const string ClientIdSuffix = ".apps.googleusercontent.com";

    public const string ConsoleUrl = "https://console.cloud.google.com/";
    public const string ApiLibraryUrl = "https://console.cloud.google.com/apis/library/health.googleapis.com";
    public const string AuthPlatformUrl = "https://console.cloud.google.com/auth/overview";
    public const string ClientsUrl = "https://console.cloud.google.com/auth/clients";

    public static Uri Api { get; } = new("https://health.googleapis.com/v4/");
}
