using Dynastream.Fit;
using EBikeManager.Application.Models;
using EBikeManager.Application.Resources;

namespace EBikeManager.Application.Utils;

public static class FitDecoder
{
    public static FitSummary ReadSummary(byte[] fit)
    {
        var decoder = new Decode();
        using (var check = new MemoryStream(fit, writable: false))
        {
            if (!decoder.IsFIT(check)) throw new InvalidDataException(ApplicationStrings.FitNotFitFile);

            check.Position = 0;
            if (!decoder.CheckIntegrity(check)) throw new InvalidDataException(ApplicationStrings.FitDamaged);
        }

        var listener = new FitListener();
        decoder.MesgEvent += listener.OnMesg;
        try
        {
            using var stream = new MemoryStream(fit, writable: false);
            decoder.Read(stream);
        }
        catch (FitException ex)
        {
            throw new InvalidDataException(ApplicationStrings.Format(ApplicationStrings.FitUnreadable, ex.Message), ex);
        }

        var messages = listener.FitMessages;
        var sessions = messages.SessionMesgs;
        if (sessions.Count == 0) throw new InvalidDataException(ApplicationStrings.FitNoSession);

        var start = sessions.Min(session => TimeZones.AsUtc(session.GetStartTime().GetDateTime()));
        var end = sessions.Max(session => TimeZones.AsUtc(session.GetStartTime().GetDateTime()).AddSeconds(session.GetTotalElapsedTime() ?? 0));

        return new FitSummary(
            start,
            end,
            Sum(sessions.Select(session => (double?)session.GetTotalTimerTime())),
            Sum(sessions.Select(session => (double?)session.GetTotalDistance())),
            Sum(sessions.Select(session => (double?)session.GetTotalCalories())),
            sessions[0].GetAvgHeartRate(),
            sessions[0].GetAvgPower(),
            messages.LapMesgs.Count,
            messages.RecordMesgs.Any(record => record.GetPositionLat() != null));
    }

    private static double? Sum(IEnumerable<double?> values)
    {
        var present = values.OfType<double>().ToList();
        return present.Count == 0 ? null : present.Sum();
    }
}
