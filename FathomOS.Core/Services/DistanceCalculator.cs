namespace FathomOS.Core.Services;

/// <summary>
/// Service for calculating distances along survey tracks
/// </summary>
public class DistanceCalculator
{
    /// <summary>
    /// Calculate Distance Along Line (DAL) for each point in a list
    /// </summary>
    /// <param name="points">List of points with X, Y, Z coordinates</param>
    /// <param name="use3D">If true, include Z in distance calculation</param>
    /// <returns>Array of cumulative distances for each point</returns>
    public double[] CalculateDistanceAlongLine(
        IList<(double X, double Y, double Z)> points,
        bool use3D = true)
    {
        if (points == null || points.Count == 0)
            return Array.Empty<double>();

        var distances = new double[points.Count];
        distances[0] = 0;

        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i - 1].X;
            double dy = points[i].Y - points[i - 1].Y;
            
            double segmentLength;
            if (use3D)
            {
                double dz = points[i].Z - points[i - 1].Z;
                segmentLength = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            else
            {
                segmentLength = Math.Sqrt(dx * dx + dy * dy);
            }

            distances[i] = distances[i - 1] + segmentLength;
        }

        return distances;
    }

    /// <summary>
    /// Calculate segment lengths between consecutive points
    /// </summary>
    public double[] CalculateSegmentLengths(
        IList<(double X, double Y, double Z)> points,
        bool use3D = true)
    {
        if (points == null || points.Count < 2)
            return Array.Empty<double>();

        var lengths = new double[points.Count - 1];

        for (int i = 0; i < points.Count - 1; i++)
        {
            double dx = points[i + 1].X - points[i].X;
            double dy = points[i + 1].Y - points[i].Y;

            if (use3D)
            {
                double dz = points[i + 1].Z - points[i].Z;
                lengths[i] = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            else
            {
                lengths[i] = Math.Sqrt(dx * dx + dy * dy);
            }
        }

        return lengths;
    }

    /// <summary>
    /// Detect gaps in the survey track where consecutive points are too far apart
    /// </summary>
    /// <param name="points">List of points</param>
    /// <param name="gapThresholdMultiplier">Gap is detected if distance > median * this multiplier</param>
    /// <returns>Array of booleans indicating if there's a gap AFTER each point</returns>
    public bool[] DetectGaps(
        IList<(double X, double Y, double Z)> points,
        double gapThresholdMultiplier = 10.0)
    {
        if (points == null || points.Count < 2)
            return Array.Empty<bool>();

        var segmentLengths = CalculateSegmentLengths(points);
        
        // Calculate median segment length
        var sortedLengths = segmentLengths.OrderBy(x => x).ToArray();
        double medianLength = sortedLengths[sortedLengths.Length / 2];
        
        // Threshold for gap detection
        double gapThreshold = medianLength * gapThresholdMultiplier;

        // Mark gaps
        var gaps = new bool[points.Count];
        for (int i = 0; i < segmentLengths.Length; i++)
        {
            gaps[i] = segmentLengths[i] > gapThreshold;
        }
        gaps[points.Count - 1] = false; // Last point has no "after" segment

        return gaps;
    }

    /// <summary>
    /// Get contiguous segments (polylines) from points, splitting at gaps
    /// </summary>
    /// <param name="points">List of points</param>
    /// <param name="gapThresholdMultiplier">Gap detection multiplier</param>
    /// <returns>List of segments, each segment is a list of point indices</returns>
    public List<List<int>> GetContiguousSegments(
        IList<(double X, double Y, double Z)> points,
        double gapThresholdMultiplier = 10.0)
    {
        var segments = new List<List<int>>();
        
        if (points == null || points.Count == 0)
            return segments;

        var gaps = DetectGaps(points, gapThresholdMultiplier);
        
        var currentSegment = new List<int> { 0 };

        for (int i = 0; i < points.Count - 1; i++)
        {
            if (gaps[i])
            {
                // End current segment and start new one
                if (currentSegment.Count > 0)
                    segments.Add(currentSegment);
                currentSegment = new List<int> { i + 1 };
            }
            else
            {
                // Continue current segment
                currentSegment.Add(i + 1);
            }
        }

        // Add final segment
        if (currentSegment.Count > 0)
            segments.Add(currentSegment);

        return segments;
    }

    /// <summary>
    /// Generate points at regular distance intervals along a polyline
    /// </summary>
    /// <param name="points">Input polyline points</param>
    /// <param name="interval">Distance interval in coordinate units</param>
    /// <param name="startDistance">Starting distance (default 0)</param>
    /// <param name="endDistance">Ending distance (default = total length)</param>
    /// <returns>List of interpolated points at the specified intervals</returns>
    public List<(double X, double Y, double Z, double DAL)> GeneratePointsAtDistanceIntervals(
        IList<(double X, double Y, double Z)> points,
        double interval,
        double startDistance = 0,
        double? endDistance = null)
    {
        var result = new List<(double X, double Y, double Z, double DAL)>();
        
        if (points == null || points.Count < 2 || interval <= 0)
            return result;

        // Calculate cumulative distances
        var distances = CalculateDistanceAlongLine(points, use3D: true);
        double totalLength = distances[distances.Length - 1];
        
        double effectiveEnd = endDistance ?? totalLength;
        
        // Start at first interval boundary >= startDistance
        double currentDistance = Math.Ceiling(startDistance / interval) * interval;
        
        while (currentDistance <= effectiveEnd && currentDistance <= totalLength)
        {
            // Find the segment containing this distance
            int segmentIndex = 0;
            for (int i = 0; i < distances.Length - 1; i++)
            {
                if (distances[i] <= currentDistance && currentDistance <= distances[i + 1])
                {
                    segmentIndex = i;
                    break;
                }
            }

            // Interpolate position within segment
            double segmentStart = distances[segmentIndex];
            double segmentEnd = distances[segmentIndex + 1];
            double segmentLength = segmentEnd - segmentStart;

            if (segmentLength > 1e-10)
            {
                double t = (currentDistance - segmentStart) / segmentLength;

                double x = points[segmentIndex].X + t * (points[segmentIndex + 1].X - points[segmentIndex].X);
                double y = points[segmentIndex].Y + t * (points[segmentIndex + 1].Y - points[segmentIndex].Y);
                double z = points[segmentIndex].Z + t * (points[segmentIndex + 1].Z - points[segmentIndex].Z);

                result.Add((x, y, z, currentDistance));
            }

            currentDistance += interval;
        }

        return result;
    }

    /// <summary>
    /// Generate points at regular KP intervals along a polyline (when points have KP values)
    /// </summary>
    /// <param name="points">Input points with KP values</param>
    /// <param name="interval">KP interval</param>
    /// <param name="startKp">Starting KP</param>
    /// <param name="endKp">Ending KP</param>
    /// <returns>List of interpolated points at the specified KP intervals</returns>
    public List<(double X, double Y, double Z, double KP)> GeneratePointsAtKpIntervals(
        IList<(double X, double Y, double Z, double KP)> points,
        double interval,
        double startKp,
        double endKp)
    {
        var result = new List<(double X, double Y, double Z, double KP)>();

        if (points == null || points.Count < 2 || interval <= 0)
            return result;

        // Sort points by KP
        var sortedPoints = points.OrderBy(p => p.KP).ToList();
        
        // Start at first interval boundary >= startKp
        double currentKp = Math.Ceiling(startKp / interval) * interval;

        while (currentKp <= endKp)
        {
            // Find points bracketing this KP
            var before = sortedPoints.LastOrDefault(p => p.KP <= currentKp);
            var after = sortedPoints.FirstOrDefault(p => p.KP >= currentKp);

            if (before != default && after != default)
            {
                if (Math.Abs(before.KP - after.KP) < 1e-10)
                {
                    // Exact match
                    result.Add((before.X, before.Y, before.Z, currentKp));
                }
                else
                {
                    // Interpolate
                    double t = (currentKp - before.KP) / (after.KP - before.KP);
                    double x = before.X + t * (after.X - before.X);
                    double y = before.Y + t * (after.Y - before.Y);
                    double z = before.Z + t * (after.Z - before.Z);
                    result.Add((x, y, z, currentKp));
                }
            }

            currentKp += interval;
        }

        return result;
    }

    /// <summary>
    /// Calculate total length of a polyline
    /// </summary>
    public double CalculateTotalLength(IList<(double X, double Y, double Z)> points, bool use3D = true)
    {
        if (points == null || points.Count < 2)
            return 0;

        var distances = CalculateDistanceAlongLine(points, use3D);
        return distances[distances.Length - 1];
    }

    /// <summary>
    /// Get statistics about segment lengths (useful for gap threshold recommendation)
    /// </summary>
    public (double Min, double Max, double Mean, double Median, double StdDev) GetSegmentStatistics(
        IList<(double X, double Y, double Z)> points)
    {
        if (points == null || points.Count < 2)
            return (0, 0, 0, 0, 0);

        var lengths = CalculateSegmentLengths(points);
        
        if (lengths.Length == 0)
            return (0, 0, 0, 0, 0);

        double min = lengths.Min();
        double max = lengths.Max();
        double mean = lengths.Average();
        
        var sorted = lengths.OrderBy(x => x).ToArray();
        double median = sorted[sorted.Length / 2];
        
        double sumSquaredDiff = lengths.Sum(x => Math.Pow(x - mean, 2));
        double stdDev = Math.Sqrt(sumSquaredDiff / lengths.Length);

        return (min, max, mean, median, stdDev);
    }
}
