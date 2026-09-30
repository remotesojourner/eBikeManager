using EBikeManager.Application.Enums;
using EBikeManager.Application.Resources;

namespace EBikeManager.Application.Configuration;

public sealed record SettingDefinition(string Key, string Default, SettingVisibility Visibility, Func<string, string?>? Validate = null)
{
    public bool IsManagedOnSecurityTab => Visibility is SettingVisibility.SecurityTab or SettingVisibility.Secret;

    public string? ProblemWith(string? value) =>
        string.IsNullOrWhiteSpace(value) ? ApplicationStrings.SettingValueRequired : Validate?.Invoke(value);
}
