namespace EBikeManager.Application.Models.Dtos;

public sealed class NotificationFieldSchemaDto
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "text";
    public string? Regex { get; init; }
    public bool Required { get; init; }
    public string? Placeholder { get; init; }
    public object? Default { get; init; }
}
