# CLAUDE.md

eBike Manager is a self-hosted app for Bosch eBike Flow riders. It signs in to Bosch the way the Flow app does, syncs rides on a cron schedule, keeps a backup of every ride's FIT file, and shows the rides in a Blazor dashboard. Google Health export is planned as an integration. It is built with .NET 10, Blazor Interactive Server, MudBlazor 9 with the vendored Modernity CSS patch, EF Core (SQLite) and the Garmin FIT SDK.

The code follows the owner's other project, Speedtest Watcher (`E:\Projects\Myspeed\dotnet`, see its CLAUDE.md). When something here isn't covered, do what Speedtest Watcher does.

## Commands

Run these from this folder, which contains `EBikeManager.slnx`.

```bash
dotnet build
dotnet test
dotnet test --project tests/EBikeManager.UnitTests
dotnet test --project tests/EBikeManager.IntegrationTests --filter-not-trait "Category=Browser"
dotnet test --project tests/EBikeManager.IntegrationTests --filter-trait "Category=Browser"
pwsh tests/EBikeManager.IntegrationTests/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet run --project src/EBikeManager.Web
dotnet ef migrations add <Name> --project src/EBikeManager.Application --startup-project src/EBikeManager.Web --output-dir Data/Migrations
docker compose up -d --build
```

| Environment variable | Effect |
| --- | --- |
| `PORT` | Listening port (default `2004`). |
| `DATA_DIRECTORY` | Folder for the database, data-protection keys and FIT archive (default `data` in the working directory, `/app/data` in Docker). |
| `DISABLE_AUTH=true` | Turns the UI password off (recovery from a forgotten password). |

These are hosting settings only. Everything else is configured in the UI and stored in the database; don't add environment variables for app behaviour. All three are read into `EBikeManagerOptions` by `EBikeManagerOptionsSetup`.

## Architecture

| Project | Contents |
| --- | --- |
| `src/EBikeManager.Application` | Everything the app does. No ASP.NET Core. |
| `src/EBikeManager.Web` | The website. `Program.cs` only composes the installers and middleware. |
| `tests/EBikeManager.UnitTests` | Fakes only: no database, files, host or browser. |
| `tests/EBikeManager.IntegrationTests` | SQLite, temporary files, the real host and Playwright. |
| `tests/EBikeManager.TestSupport` | Shared test helpers: `FakeBoschApi`, `FakeBoschAuth`, `TestFit`, `RecordingHandler`, `FixedAccess`. |

Classes go in the folder of their kind, never a feature folder: `Services/` (+ `Interfaces/`), `Repositories/` (+ `Interfaces/`), `BackgroundServices/`, `Handlers/`, `Exceptions/`, `Models/` (`Entities/`, `Dtos/`), `Enums/`, `Configuration/`, `Installers/`, `Utils/`, `Data/` (`Converters/`, `Migrations/`), `Resources/`. The website adds `Controllers/`, `Services/` (circuit state and access), `Utils/`, `Resources/` and `Ui/` (`Layout/`, `Pages/`, `Pages/Settings/`, `Controls/`).

Services, models and the interfaces services take are `public`. The DbContext, repositories, HTTP clients (`BoschApiService`, `BoschAuthService`), the Bosch authentication handler and background services are `internal`. `ArchitectureTests` enforces the rules Speedtest Watcher has: Web references only Application, Application has no ASP.NET Core, only repositories/`Data`/installers touch the DbContext, types using `HttpClient` are internal, components never inject repositories, unit tests open no database or host.

### Bosch eBike Flow

- **Sign-in** copies the ha-bosch-ebike-flow Home Assistant integration: Keycloak at `p9.authz.bosch.com`, client `one-bike-app`, redirect `onebikeapp-ios://…/oauth2redirect`, scope `openid offline_access`, PKCE, `kc_idp_hint=skid`. The redirect only reaches the Flow app, so the user copies it from the browser's Network tab and pastes it into `BoschSignIn`. `PkceLoginService` keeps each pending login's verifier keyed by `state`; the code expires after about a minute.
- **Tokens:** `BoschConnectionService` (singleton) caches the access token and stores the refresh token encrypted through `SecretStoreService` (`secrets` table, protected by `ISecretProtectionService`, which the Web implements with Data Protection). A rotated refresh token is saved at once. `invalid_grant` becomes `BoschReauthRequiredException` and the status `ReauthRequired`. `BoschAuthenticationHandler` adds the bearer token and retries once after a 401.
- **Endpoints** (`BoschEndpoints`): bikes from `obc-rider-profile…/v1/bike-profile`, rides from `obc-rider-activity…/v1/activity?page=&size=&sort=-startTime`, FIT files from `…/v1/activity/{id}/export/fit`. `startTime` is epoch seconds (milliseconds are accepted too). Distance is metres, `durationWithoutStops` seconds, `averageSpeed` km/h.
- **FIT files** from Bosch have no calories, and their `local_timestamp` equals UTC, so ride times use Bosch's `timeZoneOfActivity`. Records come in interleaved streams at the same timestamps (GPS-only records; speed/power/cadence/altitude records several times a second). Calories always come from Bosch's summary (`caloriesBurnt`), which accounts for the motor; that is also the figure to export.

### Sync

`SyncSchedulerService` waits for the next `syncCron` occurrence (hourly by default, evaluated in UTC), restarts its wait when `syncCron` changes, and calls `SyncRunService.RunScheduledAsync`, which does nothing until setup is complete. `SyncStateService` is the only run lock and publishes every status change through `IAppEventService.SyncStatusChanged`. Manual syncs (`StartManualSyncAsync`) check access, take the lock, and run in their own scope until the app stops.

`RideSyncService` reads Bosch's ride list newest first. The first sync, or one after the bike selection changed (`boschFullScan`), reads every page; later ones stop at the first page with no new rides and nothing from the last 7 days. It then downloads each missing FIT file, checks it with `FitDecoder`, and saves it through `FitArchiveService` as `fit/yyyy/MM/yyyy-MM-dd_HHmm_{id}.fit` (ride-local time) with Bosch's summary JSON beside it. A 404 marks the ride `FitUnavailable`; other failures are retried on the next sync.

### Settings and access

Settings are key/value rows in the `config` table, defined in `SettingDefinitions` and read as the typed `AppSettings`. Save through `SettingsService` (or `SettingsStateService` in the UI) so `SettingsChanged` follows. Sign-in keys are saved only by `SignInService`.

The UI password is optional. With it on, `AccessPolicy.Decide` grants full access only to a sign-in cookie whose stamp matches the current password; changing or removing the password logs everyone out. `MainLayout` shows `SignInCard` when access is `None`, and sends the owner to `/welcome` until setup is complete. The password is checked in the circuit, then `SignInTicketService` hands a one-minute ticket to `AuthController` (`/auth/complete`), the only place that can set the cookie. Services that change data start with an access check.

## Conventions

The Speedtest Watcher conventions apply, including:

- No comments of any kind (vendored `modernity.patch.css`, EF `.Designer.cs` files and the model snapshot are exempt).
- Constructor injection into readonly fields, file-scoped namespaces, one type per file, `_camelCase` private fields.
- UI text in `Web/Resources/WebStrings.resx`; service messages in `Application/Resources/ApplicationStrings.resx`. Fill placeholders with `WebStrings.Format` / `ApplicationStrings.Format`. Units stay in code.
- Fallible service methods return `OperationResult`/`OperationResult<T>`; show `result.Message ?? fallback`.
- Log through `[LoggerMessage]` partial methods. Catch specific exceptions.
- Warnings are errors and the recommended analyzers run. Package versions live in `Directory.Packages.props`.
- In Razor, a `string` component parameter needs `@` to pass a field (`ErrorMessage="@_error"`); without it Blazor passes the literal text.
- Custom CSS goes in `wwwroot/css/ebike-manager.css` with `em-` classes. Timestamps are stored in UTC (`UtcDateTimeConverter`); the UI shows ride times in the ride's own time zone and everything else in the browser's (`PreferencesService`).
- A new migration: run `dotnet format style EBikeManager.slnx --diagnostics IDE0005 IDE0161 --severity warn`, then delete its `#nullable disable` and `/// <inheritdoc />` lines.

## Tests

- xUnit v3 and FakeItEasy on Microsoft.Testing.Platform. Test names are PascalCase sentences. Pass `TestContext.Current.CancellationToken` to every call that takes one.
- Integration tests use `TestDatabase` (in-memory SQLite, `EnsureCreated`) and `TestApp` (`WebApplicationFactory` with its own temporary `DATA_DIRECTORY`, no background services, `FakeBoschApi`/`FakeBoschAuth`, and no network).
- Browser tests (`Browser/`) use `BrowserApp` on a free Kestrel port. Give each class `[Collection(BrowserTestGroup.Name)]` and `[Trait("Category", "Browser")]`, find elements by role, label or text, and end with `AssertNoBrowserErrors()`. `PageLayoutTests` saves every page in both themes at 1280px and 390px to `bin/Debug/net10.0/TestResults/browser`; look at them after UI changes. Set `EBIKE_MANAGER_BROWSER_TESTS=required` to fail instead of skipping when Chromium is missing.
- Tests never reach Bosch or any other real service.
