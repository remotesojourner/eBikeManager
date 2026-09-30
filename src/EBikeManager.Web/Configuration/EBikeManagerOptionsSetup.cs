using System.Globalization;
using EBikeManager.Application.Configuration;
using Microsoft.Extensions.Options;

namespace EBikeManager.Web.Configuration;

public sealed class EBikeManagerOptionsSetup : IConfigureOptions<EBikeManagerOptions>
{
    public const string PortVariable = "PORT";
    public const string DataDirectoryVariable = "DATA_DIRECTORY";
    public const string DisableAuthVariable = "DISABLE_AUTH";

    private readonly IConfiguration _configuration;

    public EBikeManagerOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static EBikeManagerOptions Read(IConfiguration configuration)
    {
        var options = new EBikeManagerOptions();
        new EBikeManagerOptionsSetup(configuration).Configure(options);
        return options;
    }

    public void Configure(EBikeManagerOptions options)
    {
        if (int.TryParse(Value(PortVariable), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)) options.Port = port;
        options.DataDirectory = Path.GetFullPath(Value(DataDirectoryVariable) ?? options.DataDirectory);
        options.DisableAuth = IsSwitchedOn(Value(DisableAuthVariable));
    }

    private string? Value(string variable) =>
        _configuration[variable] is { } value && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static bool IsSwitchedOn(string? value) =>
        value?.ToLowerInvariant() is "true" or "1" or "yes";
}
