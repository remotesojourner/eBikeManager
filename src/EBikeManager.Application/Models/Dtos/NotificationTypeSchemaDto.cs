namespace EBikeManager.Application.Models.Dtos;

public sealed class NotificationTypeSchemaDto
{
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<NotificationFieldSchemaDto> Fields { get; init; } = [];
}
