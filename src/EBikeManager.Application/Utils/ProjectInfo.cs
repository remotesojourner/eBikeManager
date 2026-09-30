using System.Reflection;

namespace EBikeManager.Application.Utils;

public static class ProjectInfo
{
    public const string Name = "eBike Manager";

    private const string Owner = "remotesojourner";

    public const string Repository = $"{Owner}/eBikeManager";

    public const string RepositoryUrl = $"https://github.com/{Repository}";

    public const string SponsorUrl = $"https://github.com/sponsors/{Owner}";

    public static string Version { get; } =
        (typeof(ProjectInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];
}
