using EBikeManager.Application.Models;
using EBikeManager.Application.Models.Dtos;

namespace EBikeManager.Application.Utils;

public static class RideTrackBuilder
{
    public const int MaxSamples = 1000;
    public const int MaxRoutePoints = 4000;

    private const double MinimumSplitFraction = 0.1;
    private const double LongestMovingGapSeconds = 30;
    private const double MetresPerSecondToKmh = 3.6;
    private const double SpeedSmoothingSeconds = 2;
    private const double EffortSmoothingSeconds = 5;
    private const double GradientHalfWindowMetres = 50;
    private const double ShortestGradientMetres = 20;

    public static RideTrackDto Build(IReadOnlyList<TrackRecord> records, double metresPerSplit = UnitConversion.MetresPerKilometre)
    {
        var points = Merge(records);
        var (route, values) = Route(points);
        return new RideTrackDto(route, values, Series(points), Splits(points, metresPerSplit));
    }

    private static List<TrackRecord> Merge(IReadOnlyList<TrackRecord> records)
    {
        var merged = new List<TrackRecord>();
        foreach (var record in records.OrderBy(record => record.Time))
        {
            if (merged.Count == 0) merged.Add(record);
            else if (merged[^1].Time == record.Time) merged[^1] = Carry(merged[^1], record);
            else merged.Add(Carry(merged[^1], record));
        }

        return merged;
    }

    private static TrackRecord Carry(TrackRecord carried, TrackRecord next)
    {
        var hasPosition = next.Latitude != null && next.Longitude != null;
        return new TrackRecord(
            next.Time,
            hasPosition ? next.Latitude : carried.Latitude,
            hasPosition ? next.Longitude : carried.Longitude,
            next.AltitudeMeters ?? carried.AltitudeMeters,
            next.SpeedMetresPerSecond ?? carried.SpeedMetresPerSecond,
            next.Cadence ?? carried.Cadence,
            next.PowerWatts ?? carried.PowerWatts,
            next.HeartRate ?? carried.HeartRate,
            next.DistanceMeters ?? carried.DistanceMeters);
    }

    private static (List<double[]> Route, RideRouteValuesDto Values) Route(List<TrackRecord> points)
    {
        var speed = Smooth(points, point => point.SpeedMetresPerSecond * MetresPerSecondToKmh, SpeedSmoothingSeconds);
        var power = Smooth(points, point => point.PowerWatts, EffortSmoothingSeconds);
        var cadence = Smooth(points, point => point.Cadence, EffortSmoothingSeconds);
        var heartRate = Smooth(points, point => point.HeartRate, SpeedSmoothingSeconds);
        var gradient = Gradients(points);

        var kept = new List<(double[] Position, int Index)>();
        for (var index = 0; index < points.Count; index++)
        {
            if (points[index].Latitude is not { } latitude || points[index].Longitude is not { } longitude) continue;

            var position = new[] { Math.Round(longitude, 6), Math.Round(latitude, 6) };
            if (kept.Count > 0 && kept[^1].Position.SequenceEqual(position)) continue;
            kept.Add((position, index));
        }

        var thinned = Thin(kept, MaxRoutePoints);
        return (
            [.. thinned.Select(point => point.Position)],
            new RideRouteValuesDto(
                [.. thinned.Select(point => Rounded(speed[point.Index], 1))],
                [.. thinned.Select(point => Rounded(power[point.Index], 0))],
                [.. thinned.Select(point => Rounded(cadence[point.Index], 0))],
                [.. thinned.Select(point => Rounded(gradient[point.Index], 1))],
                [.. thinned.Select(point => Rounded(heartRate[point.Index], 0))]));
    }

    private static double?[] Gradients(List<TrackRecord> points)
    {
        var gradients = new double?[points.Count];
        var behind = 0;
        var ahead = 0;
        for (var index = 0; index < points.Count; index++)
        {
            if (points[index].DistanceMeters is not { } distance || points[index].AltitudeMeters == null) continue;

            while (behind < index && !(points[behind + 1].DistanceMeters > distance - GradientHalfWindowMetres)) behind++;
            ahead = Math.Max(ahead, index);
            while (ahead < points.Count - 1 && points[ahead].DistanceMeters < distance + GradientHalfWindowMetres) ahead++;

            if (points[behind] is { DistanceMeters: { } from, AltitudeMeters: { } startAltitude } && points[ahead] is { DistanceMeters: { } to, AltitudeMeters: { } endAltitude }
                && to - from >= ShortestGradientMetres)
            {
                gradients[index] = (endAltitude - startAltitude) / (to - from) * 100;
            }
        }

        return gradients;
    }

    private static double?[] Smooth(List<TrackRecord> points, Func<TrackRecord, double?> value, double halfWindowSeconds)
    {
        var smoothed = new double?[points.Count];
        var sum = 0.0;
        var count = 0;
        var first = 0;
        var next = 0;
        for (var index = 0; index < points.Count; index++)
        {
            var time = points[index].Time;
            while (next < points.Count && (points[next].Time - time).TotalSeconds <= halfWindowSeconds)
            {
                if (value(points[next]) is { } added)
                {
                    sum += added;
                    count++;
                }

                next++;
            }

            while ((time - points[first].Time).TotalSeconds > halfWindowSeconds)
            {
                if (value(points[first]) is { } removed)
                {
                    sum -= removed;
                    count--;
                }

                first++;
            }

            smoothed[index] = value(points[index]) == null || count == 0 ? null : sum / count;
        }

        return smoothed;
    }

    private static RideSeriesDto Series(List<TrackRecord> points)
    {
        var measured = points.Where(point => point.DistanceMeters != null).ToList();
        if (measured.Count == 0) return RideSeriesDto.Empty;

        var bucketMetres = Math.Max(measured.Max(point => point.DistanceMeters!.Value) / MaxSamples, 1);
        var buckets = measured.GroupBy(point => (int)(point.DistanceMeters!.Value / bucketMetres)).OrderBy(bucket => bucket.Key).ToList();

        return new RideSeriesDto(
            [.. buckets.Select(bucket => Math.Round(bucket.Max(point => point.DistanceMeters!.Value) / 1000, 3))],
            [.. buckets.Select(bucket => Average(bucket, point => point.AltitudeMeters, 1))],
            [.. buckets.Select(bucket => Average(bucket, point => point.SpeedMetresPerSecond * MetresPerSecondToKmh, 1))],
            [.. buckets.Select(bucket => Average(bucket, point => point.Cadence, 0))],
            [.. buckets.Select(bucket => Average(bucket, point => point.PowerWatts, 0))],
            [.. buckets.Select(bucket => Average(bucket, point => point.HeartRate, 0))],
            [.. buckets.Select(bucket => Rounded(bucket.Last().Latitude, 6))],
            [.. buckets.Select(bucket => Rounded(bucket.Last().Longitude, 6))]);
    }

    private static List<RideSplitDto> Splits(List<TrackRecord> points, double metresPerSplit)
    {
        var measured = points.Where(point => point.DistanceMeters != null).ToList();
        var splits = new List<RideSplitDto>();
        if (measured.Count < 2) return splits;

        var split = new SplitTotals(measured[0].DistanceMeters!.Value);
        var nextBoundary = metresPerSplit;
        var lastAltitude = measured[0].AltitudeMeters;

        for (var index = 1; index < measured.Count; index++)
        {
            var previous = measured[index - 1];
            var current = measured[index];
            var distance = current.DistanceMeters!.Value;
            var seconds = (current.Time - previous.Time).TotalSeconds;

            if (distance > previous.DistanceMeters!.Value && seconds <= LongestMovingGapSeconds) split.AddMoving(seconds, current);
            if (current.AltitudeMeters is { } altitude)
            {
                if (lastAltitude is { } before && altitude > before) split.Climb += altitude - before;
                lastAltitude = altitude;
            }

            var isLast = index == measured.Count - 1;
            if (distance < nextBoundary && !isLast) continue;

            var length = distance - split.StartMetres;
            if (length >= metresPerSplit * MinimumSplitFraction || splits.Count == 0) splits.Add(split.ToDto(splits.Count + 1, length));
            split = new SplitTotals(distance);
            nextBoundary = (Math.Floor(distance / metresPerSplit) + 1) * metresPerSplit;
        }

        return splits;
    }

    private static List<T> Thin<T>(List<T> items, int max)
    {
        if (items.Count <= max) return items;

        var step = (double)(items.Count - 1) / (max - 1);
        return [.. Enumerable.Range(0, max).Select(index => items[(int)Math.Round(index * step)])];
    }

    private static double? Average(IEnumerable<TrackRecord> points, Func<TrackRecord, double?> value, int digits)
    {
        var values = points.Select(value).OfType<double>().ToList();
        return values.Count == 0 ? null : Math.Round(values.Average(), digits);
    }

    private static double? Rounded(double? value, int digits) => value is { } number ? Math.Round(number, digits) : null;

    private sealed class SplitTotals
    {
        private readonly List<double> _cadence = [];
        private readonly List<double> _power = [];
        private double _movingSeconds;

        public SplitTotals(double startMetres)
        {
            StartMetres = startMetres;
        }

        public double StartMetres { get; }

        public double Climb { get; set; }

        public void AddMoving(double seconds, TrackRecord point)
        {
            _movingSeconds += seconds;
            if (point.Cadence is > 0 and { } cadence) _cadence.Add(cadence);
            if (point.PowerWatts is > 0 and { } power) _power.Add(power);
        }

        public RideSplitDto ToDto(int number, double lengthMetres) => new(
            number,
            Math.Round(lengthMetres / 1000, 2),
            (int)Math.Round(_movingSeconds),
            _movingSeconds > 0 ? Math.Round(lengthMetres / _movingSeconds * MetresPerSecondToKmh, 1) : null,
            Math.Round(Climb),
            _cadence.Count > 0 ? Math.Round(_cadence.Average()) : null,
            _power.Count > 0 ? Math.Round(_power.Average()) : null);
    }
}
