namespace EBikeManager.Application.Models.Entities;

public class Bike
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime AddedAt { get; set; }

    public string? ProfileJson { get; set; }

    public string? StateOfChargeJson { get; set; }

    public string? PassJson { get; set; }

    public string? LocationJson { get; set; }

    public bool? HasFlowPlus { get; set; }

    public DateTime? DetailsUpdatedAt { get; set; }
}
