namespace FathomOS.Core.Models;

/// <summary>
/// Represents the type of route segment
/// </summary>
public enum SegmentType
{
    Straight = 64,   // Straight line segment
    Arc = 128        // Circular arc segment
}

/// <summary>
/// Represents a single segment of a route (straight line or arc)
/// </summary>
public class RouteSegment
{
    /// <summary>
    /// Start point Easting coordinate
    /// </summary>
    public double StartEasting { get; set; }

    /// <summary>
    /// Start point Northing coordinate
    /// </summary>
    public double StartNorthing { get; set; }

    /// <summary>
    /// End point Easting coordinate
    /// </summary>
    public double EndEasting { get; set; }

    /// <summary>
    /// End point Northing coordinate
    /// </summary>
    public double EndNorthing { get; set; }

    /// <summary>
    /// KP value at segment start
    /// </summary>
    public double StartKp { get; set; }

    /// <summary>
    /// KP value at segment end
    /// </summary>
    public double EndKp { get; set; }

    /// <summary>
    /// Radius for arc segments (0 for straight lines)
    /// Positive = counter-clockwise arc
    /// Negative = clockwise arc
    /// </summary>
    public double Radius { get; set; }

    /// <summary>
    /// Segment status (typically 1 = active)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Segment type code (64 = straight, 128 = arc)
    /// </summary>
    public int TypeCode { get; set; }

    /// <summary>
    /// Segment type (straight or arc)
    /// </summary>
    public SegmentType Type => Radius == 0.0 ? SegmentType.Straight : SegmentType.Arc;

    /// <summary>
    /// Is this a straight line segment?
    /// </summary>
    public bool IsStraight => Math.Abs(Radius) < 0.0001;

    /// <summary>
    /// Is this an arc segment?
    /// </summary>
    public bool IsArc => !IsStraight;

    /// <summary>
    /// Is the arc clockwise (negative radius)?
    /// </summary>
    public bool IsClockwise => Radius < 0;

    /// <summary>
    /// Length of the segment along the route
    /// </summary>
    public double Length => EndKp - StartKp;

    /// <summary>
    /// Calculate the bearing (azimuth) of a straight segment in radians
    /// </summary>
    public double GetBearing()
    {
        double dE = EndEasting - StartEasting;
        double dN = EndNorthing - StartNorthing;
        return Math.Atan2(dE, dN);
    }

    /// <summary>
    /// Calculate the direct distance between start and end points
    /// </summary>
    public double GetDirectDistance()
    {
        double dE = EndEasting - StartEasting;
        double dN = EndNorthing - StartNorthing;
        return Math.Sqrt(dE * dE + dN * dN);
    }

    /// <summary>
    /// Get arc center point (only valid for arc segments)
    /// </summary>
    public (double Easting, double Northing) GetArcCenter()
    {
        if (IsStraight)
            throw new InvalidOperationException("Cannot get arc center for straight segment");

        // Calculate perpendicular direction from chord midpoint
        double midE = (StartEasting + EndEasting) / 2.0;
        double midN = (StartNorthing + EndNorthing) / 2.0;

        double chordE = EndEasting - StartEasting;
        double chordN = EndNorthing - StartNorthing;
        double chordLen = Math.Sqrt(chordE * chordE + chordN * chordN);

        // Perpendicular unit vector (rotated 90 degrees)
        double perpE = -chordN / chordLen;
        double perpN = chordE / chordLen;

        // Distance from chord midpoint to center
        double r = Math.Abs(Radius);
        double halfChord = chordLen / 2.0;
        double distToCenter = Math.Sqrt(r * r - halfChord * halfChord);

        // Direction depends on arc direction (sign of radius)
        double sign = Radius > 0 ? 1.0 : -1.0;

        double centerE = midE + sign * perpE * distToCenter;
        double centerN = midN + sign * perpN * distToCenter;

        return (centerE, centerN);
    }

    public override string ToString()
    {
        string typeStr = IsStraight ? "Straight" : $"Arc (R={Radius:F2})";
        return $"Segment: {typeStr}, KP {StartKp:F3} to {EndKp:F3}";
    }
}
