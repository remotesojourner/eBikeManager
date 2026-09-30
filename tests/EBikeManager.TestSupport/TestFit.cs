using Dynastream.Fit;
using DateTime = System.DateTime;
using FitDateTime = Dynastream.Fit.DateTime;

namespace EBikeManager.TestSupport;

internal static class TestFit
{
    public const int StartLatitude = 614_436_828;
    public const int StartLongitude = -1_480_571;

    private const int RecordSeconds = 5;
    private const int RouteSpan = 596_523;

    public static byte[] Create(DateTime start, TimeSpan elapsed, ushort? calories = null, float distanceMeters = 12_500, bool withGps = true)
    {
        var end = start + elapsed;
        var fitStart = new FitDateTime(start);
        var fitEnd = new FitDateTime(end);

        using var stream = new MemoryStream();
        var encoder = new Encode(ProtocolVersion.V20);
        encoder.Open(stream);

        var fileId = new FileIdMesg();
        fileId.SetType(Dynastream.Fit.File.Activity);
        fileId.SetManufacturer(Manufacturer.Bosch);
        fileId.SetProduct(0);
        fileId.SetSerialNumber(1234);
        fileId.SetTimeCreated(fitStart);
        encoder.Write(fileId);

        var seconds = (int)elapsed.TotalSeconds;
        for (var offset = 0; offset <= seconds; offset += RecordSeconds)
        {
            var progress = offset / (double)seconds;
            var timestamp = new FitDateTime(start.AddSeconds(offset));
            if (withGps)
            {
                var position = new RecordMesg();
                position.SetTimestamp(timestamp);
                position.SetPositionLat(StartLatitude + (int)(progress * RouteSpan));
                position.SetPositionLong(StartLongitude + (int)(Math.Sin(progress * Math.PI) * RouteSpan / 2));
                encoder.Write(position);
            }

            var data = new RecordMesg();
            data.SetTimestamp(timestamp);
            data.SetDistance((float)(progress * distanceMeters));
            data.SetEnhancedSpeed((float)(5 + Math.Sin(progress * 12) * 1.5));
            data.SetEnhancedAltitude((float)(50 + Math.Sin(progress * Math.PI * 2) * 20));
            data.SetCadence((byte)(offset % 60 < 50 ? 70 : 0));
            data.SetPower((ushort)(offset % 60 < 50 ? 150 : 0));
            encoder.Write(data);
        }

        var lap = new LapMesg();
        lap.SetStartTime(fitStart);
        lap.SetTimestamp(fitEnd);
        lap.SetTotalElapsedTime((float)elapsed.TotalSeconds);
        encoder.Write(lap);

        var session = new SessionMesg();
        session.SetStartTime(fitStart);
        session.SetTimestamp(fitEnd);
        session.SetSport(Sport.EBiking);
        session.SetTotalElapsedTime((float)elapsed.TotalSeconds);
        session.SetTotalTimerTime((float)(elapsed.TotalSeconds - 120));
        session.SetTotalDistance(distanceMeters);
        session.SetAvgPower(147);
        if (calories is { } kcal) session.SetTotalCalories(kcal);
        session.SetNumLaps(1);
        encoder.Write(session);

        var activity = new ActivityMesg();
        activity.SetTimestamp(fitEnd);
        activity.SetNumSessions(1);
        activity.SetType(Activity.Manual);
        activity.SetEvent(Event.Activity);
        activity.SetEventType(EventType.Stop);
        encoder.Write(activity);
        encoder.Close();
        return stream.ToArray();
    }
}
