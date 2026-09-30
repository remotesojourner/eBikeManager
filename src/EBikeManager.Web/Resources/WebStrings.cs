using System.Globalization;

namespace EBikeManager.Web.Resources;

internal static partial class WebStrings
{
    public static string Format(string format, params ReadOnlySpan<object?> args) =>
        string.Format(CultureInfo.CurrentCulture, format, args);
}
