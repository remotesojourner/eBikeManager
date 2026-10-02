using System.Text.Json;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;

namespace EBikeManager.Application.Services;

public sealed class NotificationService
{
    private const string UnsavedId = "unsaved";
    private const int SampleCandidates = 10;

    private readonly INotificationChannelRepository _channels;
    private readonly INotificationDispatchService _dispatcher;
    private readonly IRideRepository _rides;
    private readonly ICurrentAccessService _access;

    public NotificationService(INotificationChannelRepository channels, INotificationDispatchService dispatcher, IRideRepository rides, ICurrentAccessService access)
    {
        _channels = channels;
        _dispatcher = dispatcher;
        _rides = rides;
        _access = access;
    }

    public static RideDto SampleRide { get; } = new(
        "sample", "sample-bike", "My eBike", "Bosch (Performance Line)", "Westminster to Blackfriars and back",
        new DateTime(2026, 9, 27, 7, 14, 0, DateTimeKind.Utc), new DateTime(2026, 9, 27, 8, 14, 0, DateTimeKind.Unspecified),
        4080, 612, 104, 18, 24.0, 64, 137, BackupStatus.Saved, null, null, BackupStatus.Saved, null);

    public IReadOnlyDictionary<string, NotificationTypeSchemaDto> Schemas => _dispatcher.Schemas;

    public async Task<IReadOnlyList<NotificationChannelDto>> GetChannelsAsync(CancellationToken cancellationToken = default) =>
        [.. (await _channels.GetAllAsync(cancellationToken)).Select(channel =>
            new NotificationChannelDto(channel.Id, channel.Type, channel.DisplayName, ReadValues(channel.Data), channel.LastActivity, channel.ActivityFailed))];

    public async Task<OperationResult<string>> CreateAsync(string type, string? displayName, IReadOnlyDictionary<string, JsonElement> settings, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!_dispatcher.Schemas.TryGetValue(type, out var schema)) return OperationResult.NotFound(ApplicationStrings.NotificationNotFound);
        if (NotificationSettingsRules.ProblemWith(schema, settings, settings.Keys) is { } problem) return OperationResult.Invalid(problem);

        return OperationResult.Ok(await _channels.CreateAsync(type, displayName ?? "", JsonSerializer.Serialize(settings), cancellationToken));
    }

    public async Task<OperationResult> UpdateAsync(string id, string? displayName, IReadOnlyDictionary<string, JsonElement> settings, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (await _channels.FindAsync(id, cancellationToken) is not { } existing) return OperationResult.NotFound(ApplicationStrings.NotificationNotFound);
        if (!_dispatcher.Schemas.TryGetValue(existing.Type, out var schema))
            return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.NotificationUnknownType, existing.Type));

        var merged = ReadSettings(existing.Data);
        foreach (var (key, value) in settings) merged[key] = value;
        if (NotificationSettingsRules.ProblemWith(schema, merged, settings.Keys) is { } problem) return OperationResult.Invalid(problem);

        await _channels.UpdateAsync(id, displayName, JsonSerializer.Serialize(merged), cancellationToken);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        return await _channels.DeleteAsync(id, cancellationToken) ? OperationResult.Ok() : OperationResult.NotFound(ApplicationStrings.NotificationNotFound);
    }

    public async Task<OperationResult<NotificationTestResultDto>> SendTestAsync(
        string type, IReadOnlyDictionary<string, JsonElement> settings, string? id, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!_dispatcher.Schemas.TryGetValue(type, out var schema)) return OperationResult.NotFound(ApplicationStrings.NotificationNotFound);
        if (NotificationSettingsRules.ProblemWith(schema, settings, settings.Keys) is { } problem) return OperationResult.Invalid(problem);

        var sample = (await _rides.GetListAsync(SampleCandidates, cancellationToken)).FirstOrDefault(ride => ride.EndTime != null) is { } latest
            ? RideDto.From(latest)
            : SampleRide;
        var result = await _dispatcher.TestAsync(type, id ?? UnsavedId, JsonSerializer.Serialize(settings), sample, cancellationToken);
        return OperationResult.Ok(new NotificationTestResultDto(result.Outcome != NotificationOutcome.Failed, result.Error));
    }

    private static Dictionary<string, JsonElement> ReadSettings(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static Dictionary<string, object?> ReadValues(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
