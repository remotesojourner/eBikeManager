namespace EBikeManager.Application.Models.Entities;

public class SecretEntry
{
    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; }
}
