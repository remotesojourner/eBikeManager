using System.Globalization;
using System.Text;

namespace EBikeManager.TestSupport;

internal static class TestGpx
{
    public static byte[] Create(DateTime start) => Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <gpx version="1.1" creator="Bosch eBike Flow" xmlns="http://www.topografix.com/GPX/1/1">
          <trk><name>Ride</name><trkseg>
            <trkpt lat="55.9296" lon="-3.1249"><ele>50.0</ele><time>{start:yyyy-MM-ddTHH:mm:ssZ}</time></trkpt>
            <trkpt lat="55.9301" lon="-3.1251"><ele>51.2</ele><time>{start.AddSeconds(5):yyyy-MM-ddTHH:mm:ssZ}</time></trkpt>
          </trkseg></trk>
        </gpx>
        """));
}
