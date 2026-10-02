using EBikeManager.Application.Configuration;
using EBikeManager.Application.Installers;
using EBikeManager.Web.Configuration;
using EBikeManager.Web.Installers;
using EBikeManager.Web.Services;
using EBikeManager.Web.Ui;
using EBikeManager.Web.Utils;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://0.0.0.0:{EBikeManagerOptionsSetup.Read(builder.Configuration).Port}");

builder.Services
    .AddHosting()
    .AddApplication()
    .AddAccessControl()
    .AddWebUi();

var app = builder.Build();

var hosting = app.Services.GetRequiredService<IOptions<EBikeManagerOptions>>().Value;
Directory.CreateDirectory(hosting.DataDirectory);
Directory.CreateDirectory(hosting.KeysDirectory);
Directory.CreateDirectory(hosting.FitDirectory);

await app.Services.InitializeDatabaseAsync();
await app.Services.GetRequiredService<AuthSettingsService>().ReloadAsync();

app.UseForwardedHeaders();
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapControllers();
app.MapHealthChecks(HealthEndpoint.Path).AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
