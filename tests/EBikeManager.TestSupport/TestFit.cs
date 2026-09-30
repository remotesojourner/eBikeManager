using Dynastream.Fit;
using DateTime = System.DateTime;
using FitDateTime = Dynastream.Fit.DateTime;

namespace EBikeManager.TestSupport;

internal static class TestFit
{
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

        foreach (var offset in new[] { 0, 30, 60 })
        {
            var record = new RecordMesg();
            record.SetTimestamp(new FitDateTime(start.AddSeconds(offset)));
            record.SetPower(150);
            record.SetCadence(70);
            if (withGps)
            {
                record.SetPositionLat(614_436_828);
                record.SetPositionLong(-1_480_571);
            }

            encoder.Write(record);
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
