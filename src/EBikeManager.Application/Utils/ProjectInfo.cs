using System.Reflection;

namespace EBikeManager.Application.Utils;

public static class ProjectInfo
{
    public const string Name = "eBike Manager";

    public static string Version { get; } =
        (typeof(ProjectInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0").Split('+')[0];
}
