namespace EBikeManager.Application.Models.Entities;

public class Bike
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime AddedAt { get; set; }
}
