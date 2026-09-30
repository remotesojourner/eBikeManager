using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;
using EBikeManager.Application.Repositories.Interfaces;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class GoogleHealthAccountService
{
    public const string Provider = "googleHealth";

    private readonly PkceLoginService _logins;
    private readonly IGoogleHealthAuthService _auth;
    private readonly GoogleHealthConnectionService _connection;
    private readonly SettingsService _settings;
    private readonly IRideExportRepository _exports;
    private readonly IntegrationSyncService _integrations;
    private readonly ICurrentAccessService _access;
    private readonly ILogger<GoogleHealthAccountService> _logger;

    public GoogleHealthAccountService(
        PkceLoginService logins,
        IGoogleHealthAuthService auth,
        GoogleHealthConnectionService connection,
        SettingsService settings,
        IRideExportRepository exports,
        IntegrationSyncService integrations,
        ICurrentAccessService access,
        ILogger<GoogleHealthAccountService> logger)
    {
        _logins = logins;
        _auth = auth;
        _connection = connection;
        _settings = settings;
        _exports = exports;
        _integrations = integrations;
        _access = access;
        _logger = logger;
    }

    public Task<GoogleHealthConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default) => _connection.GetStatusAsync(cancellationToken);

    public async Task<bool> HasClientSecretAsync(CancellationToken cancellationToken = default) => (await _connection.GetClientAsync(cancellationToken)) != null;

    public string? TakeSignInProblem() => _connection.TakeSignInProblem();

    public async Task<OperationResult<string>> StartSignInAsync(
        string? clientId,
        string? clientSecret,
        GoogleHealthOptions options,
        Uri redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        if (!GoogleHealthRedirect.IsAllowed(redirectUri)) return OperationResult.Invalid(ApplicationStrings.GoogleHealthNeedsHttps);
        if (clientId == null || !GoogleHealthSettings.IsValidClientId(clientId)) return OperationResult.Invalid(ApplicationStrings.GoogleHealthClientIdInvalid);
        if (!GoogleHealthSettings.IsValidUploadFrom(options.UploadFrom)) return OperationResult.Invalid(ApplicationStrings.GoogleHealthUploadChoiceInvalid);

        if (!string.IsNullOrWhiteSpace(clientSecret)) await _connection.SaveClientSecretAsync(clientSecret.Trim(), cancellationToken);
        else if (!await HasClientSecretAsync(cancellationToken)) return OperationResult.Invalid(ApplicationStrings.GoogleHealthClientSecretMissing);

        var saved = await _settings.SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.GoogleHealthClientId] = clientId.Trim(),
            [SettingDefinitions.GoogleHealthUploadFrom] = options.UploadFrom
        }, cancellationToken);
        if (!saved.Succeeded) return saved;

        return OperationResult.Ok(_auth.BuildAuthorizeUrl(_logins.Start(Provider, redirectUri.AbsoluteUri), clientId));
    }

    public async Task<OperationResult> CompleteSignInAsync(string? code, string? state, string? error, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var result = await ExchangeAsync(code, state, error, cancellationToken);
        _connection.RecordSignInProblem(result.Succeeded ? null : result.Message);
        return result;
    }

    public Task<OperationResult> SaveOptionsAsync(GoogleHealthOptions options, CancellationToken cancellationToken = default)
    {
        if (!GoogleHealthSettings.IsValidUploadFrom(options.UploadFrom)) return Task.FromResult(OperationResult.Invalid(ApplicationStrings.GoogleHealthUploadChoiceInvalid));

        return _settings.SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.GoogleHealthUploadFrom] = options.UploadFrom
        }, cancellationToken);
    }

    public async Task<OperationResult> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        if (await _connection.ForgetAsync(cancellationToken) is { } refreshToken)
        {
            try
            {
                await _auth.RevokeAsync(refreshToken, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                LogRevokeFailed(ex);
            }
        }

        return await _settings.SaveAsync(new Dictionary<string, string> { [SettingDefinitions.GoogleHealthAccount] = SettingDefinitions.Unset }, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, RideExportDto>> GetUploadsByRideAsync(CancellationToken cancellationToken = default) =>
        (await _exports.GetAllAsync(GoogleHealthIntegration.IntegrationKey, cancellationToken)).ToDictionary(export => export.RideId);

    public async Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default) =>
        await _connection.GetStatusAsync(cancellationToken) == GoogleHealthConnectionStatus.Connected;

    public async Task<OperationResult> UploadRideAsync(string rideId, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var result = await _integrations.ExportOneAsync(GoogleHealthIntegration.IntegrationKey, rideId, cancellationToken);
        if (result.Succeeded) LogRideUploaded(rideId);
        return result;
    }

    public Task<RideExportCountsDto> GetCountsAsync(CancellationToken cancellationToken = default) =>
        _exports.CountAsync(GoogleHealthIntegration.IntegrationKey, cancellationToken);

    private async Task<OperationResult> ExchangeAsync(string? code, string? state, string? error, CancellationToken cancellationToken)
    {
        if (error != null) return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.GoogleHealthLoginError, error));
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state)) return OperationResult.Invalid(ApplicationStrings.GoogleHealthCallbackIncomplete);
        if (_logins.Take(Provider, state) is not { } login) return OperationResult.Invalid(ApplicationStrings.GoogleHealthLoginExpired);
        if (await _connection.GetClientAsync(cancellationToken) is not { } client) return OperationResult.Invalid(ApplicationStrings.GoogleHealthClientSecretMissing);

        GoogleHealthTokens tokens;
        try
        {
            tokens = await _auth.ExchangeCodeAsync(code, login, client, cancellationToken);
        }
        catch (IntegrationSignInRequiredException ex)
        {
            return OperationResult.Invalid(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.GoogleHealthUnreachable, ex.Message));
        }

        await _connection.SaveLoginAsync(tokens, cancellationToken);
        LogConnected(tokens.AccountName ?? "(unknown)");
        return await _settings.SaveAsync(new Dictionary<string, string>
        {
            [SettingDefinitions.GoogleHealthAccount] = tokens.AccountName ?? SettingDefinitions.Unset
        }, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to Google Health as {Account}")]
    private partial void LogConnected(string account);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded ride {RideId} to Google Health on request")]
    private partial void LogRideUploaded(string rideId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Could not tell Google to forget eBike Manager's access")]
    private partial void LogRevokeFailed(Exception exception);
}
