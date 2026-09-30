using System.Globalization;

namespace EBikeManager.Application.Resources;

internal static partial class ApplicationStrings
{
    public static string Format(string format, params ReadOnlySpan<object?> args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);
}
