using System.Text;

namespace EBikeManager.Application.Utils;

public static class GpxFile
{
    private const int CheckedBytes = 2048;

    public static bool LooksLikeGpx(byte[] content) =>
        Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, CheckedBytes)).Contains("<gpx", StringComparison.Ordinal);
}
