namespace FathomOS.Core.Models;

/// <summary>
/// Represents complete route data parsed from an RLX file
/// </summary>
public class RouteData
{
    /// <summary>
    /// Route name from file header
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Runline type code from header
    /// </summary>
    public int RunlineType { get; set; }

    /// <summary>
    /// Route offset value from header
    /// </summary>
    public double Offset { get; set; }

    /// <summary>
    /// Coordinate unit as specified in file
    /// </summary>
    public LengthUnit CoordinateUnit { get; set; } = LengthUnit.Meter;

    /// <summary>
    /// Original unit string from file
    /// </summary>
    public string OriginalUnitString { get; set; } = string.Empty;

    /// <summary>
    /// Source file path
    /// </summary>
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>
    /// List of route segments
    /// </summary>
    public List<RouteSegment> Segments { get; set; } = new();

    /// <summary>
    /// Start KP of the route
    /// </summary>
    public double StartKp => Segments.Count > 0 ? Segments.First().StartKp : 0;

    /// <summary>
    /// End KP of the route
    /// </summary>
    public double EndKp => Segments.Count > 0 ? Segments.Last().EndKp : 0;

    /// <summary>
    /// Total route length (in KP units)
    /// </summary>
    public double TotalLength => EndKp - StartKp;

    /// <summary>
    /// Number of straight segments
    /// </summary>
    public int StraightSegmentCount => Segments.Count(s => s.IsStraight);

    /// <summary>
    /// Number of arc segments
    /// </summary>
    public int ArcSegmentCount => Segments.Count(s => s.IsArc);

    /// <summary>
    /// Get the bounding box of the route
    /// </summary>
    public (double MinE, double MaxE, double MinN, double MaxN) GetBoundingBox()
    {
        if (Segments.Count == 0)
            return (0, 0, 0, 0);

        double minE = double.MaxValue, maxE = double.MinValue;
        double minN = double.MaxValue, maxN = double.MinValue;

        foreach (var seg in Segments)
        {
            minE = Math.Min(minE, Math.Min(seg.StartEasting, seg.EndEasting));
            maxE = Math.Max(maxE, Math.Max(seg.StartEasting, seg.EndEasting));
            minN = Math.Min(minN, Math.Min(seg.StartNorthing, seg.EndNorthing));
            maxN = Math.Max(maxN, Math.Max(seg.StartNorthing, seg.EndNorthing));
        }

        return (minE, maxE, minN, maxN);
    }

    /// <summary>
    /// Find the segment containing a given KP value
    /// </summary>
    public RouteSegment? FindSegmentAtKp(double kp)
    {
        return Segments.FirstOrDefault(s => kp >= s.StartKp && kp <= s.EndKp);
    }

    /// <summary>
    /// Get coordinates at a given KP along the route
    /// </summary>
    public (double Easting, double Northing)? GetCoordinatesAtKp(double kp)
    {
        var segment = FindSegmentAtKp(kp);
        if (segment == null)
            return null;

        if (segment.IsStraight)
        {
            // Linear interpolation along straight segment
            double fraction = (kp - segment.StartKp) / segment.Length;
            double e = segment.StartEasting + fraction * (segment.EndEasting - segment.StartEasting);
            double n = segment.StartNorthing + fraction * (segment.EndNorthing - segment.StartNorthing);
            return (e, n);
        }
        else
        {
            // Arc interpolation
            var (centerE, centerN) = segment.GetArcCenter();
            double r = Math.Abs(segment.Radius);

            // Calculate start and end angles
            double startAngle = Math.Atan2(segment.StartEasting - centerE, segment.StartNorthing - centerN);
            double endAngle = Math.Atan2(segment.EndEasting - centerE, segment.EndNorthing - centerN);

            // Handle angle wrap-around
            double deltaAngle = endAngle - startAngle;
            if (segment.IsClockwise)
            {
                if (deltaAngle > 0) deltaAngle -= 2 * Math.PI;
            }
            else
            {
                if (deltaAngle < 0) deltaAngle += 2 * Math.PI;
            }

            // Interpolate angle
            double fraction = (kp - segment.StartKp) / segment.Length;
            double angle = startAngle + fraction * deltaAngle;

            // Convert back to coordinates
            double e = centerE + r * Math.Sin(angle);
            double n = centerN + r * Math.Cos(angle);
            return (e, n);
        }
    }

    /// <summary>
    /// Validate the route data for consistency
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();

        if (string.IsNullOrEmpty(Name))
            issues.Add("Route name is empty");

        if (Segments.Count == 0)
        {
            issues.Add("No route segments found");
            return issues;
        }

        // Check segment continuity
        for (int i = 1; i < Segments.Count; i++)
        {
            var prev = Segments[i - 1];
            var curr = Segments[i];

            // Check KP continuity
            if (Math.Abs(prev.EndKp - curr.StartKp) > 0.001)
            {
                issues.Add($"KP discontinuity between segments {i} and {i + 1}: {prev.EndKp:F6} vs {curr.StartKp:F6}");
            }

            // Check coordinate continuity
            double distGap = Math.Sqrt(
                Math.Pow(prev.EndEasting - curr.StartEasting, 2) +
                Math.Pow(prev.EndNorthing - curr.StartNorthing, 2));

            if (distGap > 0.01) // 1cm tolerance
            {
                issues.Add($"Coordinate gap between segments {i} and {i + 1}: {distGap:F3} units");
            }
        }

        return issues;
    }

    public override string ToString()
    {
        return $"Route: {Name}, {Segments.Count} segments, KP {StartKp:F3} to {EndKp:F3} ({CoordinateUnit.GetDisplayName()})";
    }
}
