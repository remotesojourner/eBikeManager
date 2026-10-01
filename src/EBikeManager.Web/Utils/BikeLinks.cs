namespace EBikeManager.Web.Utils;

public static class BikeLinks
{
    public const string BikeParameter = "bike";

    public static string For(string bikeId) => $"bikes?{BikeParameter}={Uri.EscapeDataString(bikeId)}";
}
