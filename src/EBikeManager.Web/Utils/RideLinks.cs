namespace EBikeManager.Web.Utils;

public static class RideLinks
{
    public static string For(string rideId) => $"rides/{Uri.EscapeDataString(rideId)}";
}
