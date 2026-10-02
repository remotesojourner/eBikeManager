using EBikeManager.Application.Configuration;
using EBikeManager.Application.Enums;
using EBikeManager.Application.Exceptions;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;
using EBikeManager.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace EBikeManager.Application.Services;

public sealed partial class BoschAccountService
{
    public const string Provider = "bosch";

    private readonly PkceLoginService _logins;
    private readonly IBoschAuthService _auth;
    private readonly BoschConnectionService _connection;
    private readonly IBoschApiService _bosch;
    private readonly SettingsService _settings;
    private readonly ICurrentAccessService _access;
    private readonly NotificationStateService _notifications;
    private readonly ILogger<BoschAccountService> _logger;

    public BoschAccountService(
        PkceLoginService logins,
        IBoschAuthService auth,
        BoschConnectionService connection,
        IBoschApiService bosch,
        SettingsService settings,
        ICurrentAccessService access,
        NotificationStateService notifications,
        ILogger<BoschAccountService> logger)
    {
        _logins = logins;
        _auth = auth;
        _connection = connection;
        _bosch = bosch;
        _settings = settings;
        _access = access;
        _notifications = notifications;
        _logger = logger;
    }

    public Task<BoschConnectionStatus> GetStatusAsync(CancellationToken cancellationToken = default) => _connection.GetStatusAsync(cancellationToken);

    public OperationResult<string> StartLogin()
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();
        return OperationResult.Ok(_auth.BuildAuthorizeUrl(_logins.Start(Provider)));
    }

    public async Task<OperationResult> ConnectAsync(string? pasted, CancellationToken cancellationToken = default)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        var callback = OAuthCallback.Parse(pasted);
        if (callback.Error != null) return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.BoschLoginError, callback.Error));
        if (callback.Code == null || callback.State == null) return OperationResult.Invalid(ApplicationStrings.BoschPasteNotRedirect);
        if (_logins.Take(Provider, callback.State) is not { } login) return OperationResult.Invalid(ApplicationStrings.BoschLoginExpired);

        BoschTokens tokens;
        try
        {
            tokens = await _auth.ExchangeCodeAsync(callback.Code, login, cancellationToken);
        }
        catch (BoschReauthRequiredException)
        {
            return OperationResult.Invalid(ApplicationStrings.BoschCodeRejected);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.BoschUnreachable, ex.Message));
        }

        await _connection.SaveLoginAsync(tokens, cancellationToken);
        _notifications.SignedIn(SyncRunService.BoschService);
        await _settings.SaveAsync(new Dictionary<string, string> { [SettingDefinitions.BoschAccount] = tokens.AccountName ?? SettingDefinitions.Unset }, cancellationToken);
        LogConnected(tokens.AccountName ?? "(unknown)");
        return OperationResult.Ok();
    }

    public Task<OperationResult<IReadOnlyList<BoschBikeInfo>>> GetBikesAsync(CancellationToken cancellationToken = default) =>
        CallBoschAsync(_bosch.GetBikesAsync, cancellationToken);

    public Task<OperationResult<BoschActivity?>> GetLatestRideAsync(CancellationToken cancellationToken = default) =>
        CallBoschAsync(async token => (await _bosch.GetActivitiesAsync(0, 1, token)).Activities is [var latest, ..] ? latest : null, cancellationToken);

    private async Task<OperationResult<T>> CallBoschAsync<T>(Func<CancellationToken, Task<T>> call, CancellationToken cancellationToken)
    {
        if (!_access.HasFullAccess) return OperationResult.Denied();

        try
        {
            return OperationResult.Ok(await call(cancellationToken));
        }
        catch (BoschReauthRequiredException)
        {
            return OperationResult.Invalid(ApplicationStrings.BoschReconnectNeeded);
        }
        catch (HttpRequestException ex)
        {
            return OperationResult.Invalid(ApplicationStrings.Format(ApplicationStrings.BoschUnreachable, ex.Message));
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to Bosch eBike Flow as {Account}")]
    private partial void LogConnected(string account);
}
