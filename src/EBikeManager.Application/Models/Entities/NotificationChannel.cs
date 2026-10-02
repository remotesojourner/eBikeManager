namespace EBikeManager.Application.Models.Entities;

public class NotificationChannel
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Data { get; set; } = "{}";

    public DateTime? LastActivity { get; set; }

    public bool ActivityFailed { get; set; }
}
