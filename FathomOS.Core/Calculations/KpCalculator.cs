namespace FathomOS.Core.Calculations;

using FathomOS.Core.Models;

/// <summary>
/// Calculates KP (Kilometer Point / Chainage) and DCC (Distance to Closest point on Curve)
/// for survey points relative to a route.
/// 
/// This replaces the legacy 7KPCalc VB6 application.
/// </summary>
public class KpCalculator
{
    private readonly RouteData _route;
    private readonly LengthUnit _outputKpUnit;

    public KpCalculator(RouteData route, LengthUnit outputKpUnit = LengthUnit.Kilometer)
    {
        _route = route ?? throw new ArgumentNullException(nameof(route));
        _outputKpUnit = outputKpUnit;
    }

    /// <summary>
    /// Calculate KP and DCC for a single point
    /// </summary>
    /// <param name="easting">Point easting in route coordinate units</param>
    /// <param name="northing">Point northing in route coordinate units</param>
    /// <returns>Tuple of (KP, DCC) where DCC is positive if point is to the right of route direction</returns>
    public (double Kp, double Dcc) Calculate(double easting, double northing)
    {
        double bestKp = 0;
        double bestDcc = double.MaxValue;
        double bestDistance = double.MaxValue;

        foreach (var segment in _route.Segments)
        {
            var (kp, dcc, distance) = CalculateForSegment(easting, northing, segment);

            // Keep the result with the smallest perpendicular distance
            if (Math.Abs(dcc) < Math.Abs(bestDcc))
            {
                bestKp = kp;
                bestDcc = dcc;
                bestDistance = distance;
            }
        }

        // Convert KP to output units if needed
        // Route KP is typically in kilometers, convert to user's preferred unit
        double outputKp = ConvertKp(bestKp);

        return (outputKp, bestDcc);
    }

    /// <summary>
    /// Calculate KP and DCC for a point relative to a single segment
    /// </summary>
    private (double Kp, double Dcc, double Distance) CalculateForSegment(
        double easting, double northing, RouteSegment segment)
    {
        if (segment.IsStraight)
        {
            return CalculateForStraightSegment(easting, northing, segment);
        }
        else
        {
            return CalculateForArcSegment(easting, northing, segment);
        }
    }

    /// <summary>
    /// Calculate for straight line segment using point-to-line projection
    /// </summary>
    private (double Kp, double Dcc, double Distance) CalculateForStraightSegment(
        double easting, double northing, RouteSegment segment)
    {
        // Vector from segment start to end
        double segDx = segment.EndEasting - segment.StartEasting;
        double segDy = segment.EndNorthing - segment.StartNorthing;
        double segLength = Math.Sqrt(segDx * segDx + segDy * segDy);

        if (segLength < 1e-10)
        {
            // Degenerate segment
            double distToStart = Math.Sqrt(
                Math.Pow(easting - segment.StartEasting, 2) +
                Math.Pow(northing - segment.StartNorthing, 2));
            return (segment.StartKp, distToStart, distToStart);
        }

        // Unit vector along segment
        double ux = segDx / segLength;
        double uy = segDy / segLength;

        // Vector from segment start to point
        double px = easting - segment.StartEasting;
        double py = northing - segment.StartNorthing;

        // Project point onto segment line (dot product)
        double alongSegment = px * ux + py * uy;

        // Clamp to segment bounds
        double clampedAlong = Math.Max(0, Math.Min(segLength, alongSegment));

        // Calculate fraction along segment for KP interpolation
        double fraction = clampedAlong / segLength;

        // KP at closest point
        double kp = segment.StartKp + fraction * segment.Length;

        // Closest point on segment
        double closestX = segment.StartEasting + clampedAlong * ux;
        double closestY = segment.StartNorthing + clampedAlong * uy;

        // Distance from point to closest point on segment
        double distance = Math.Sqrt(
            Math.Pow(easting - closestX, 2) +
            Math.Pow(northing - closestY, 2));

        // DCC sign: positive if point is to the right of route direction
        // Cross product gives signed distance
        double crossProduct = (easting - closestX) * uy - (northing - closestY) * ux;
        double dcc = crossProduct >= 0 ? distance : -distance;

        return (kp, dcc, distance);
    }

    /// <summary>
    /// Calculate for arc segment using point-to-arc projection
    /// </summary>
    private (double Kp, double Dcc, double Distance) CalculateForArcSegment(
        double easting, double northing, RouteSegment segment)
    {
        // Get arc center
        var (centerE, centerN) = segment.GetArcCenter();
        double radius = Math.Abs(segment.Radius);

        // Vector from center to point
        double dx = easting - centerE;
        double dy = northing - centerN;
        double distToCenter = Math.Sqrt(dx * dx + dy * dy);

        // Angle of point relative to center
        double pointAngle = Math.Atan2(dx, dy); // Note: Atan2(E, N) for surveying convention

        // Calculate start and end angles
        double startAngle = Math.Atan2(
            segment.StartEasting - centerE,
            segment.StartNorthing - centerN);
        double endAngle = Math.Atan2(
            segment.EndEasting - centerE,
            segment.EndNorthing - centerN);

        // Determine arc direction and angular span
        bool clockwise = segment.IsClockwise;
        double angularSpan = CalculateAngularSpan(startAngle, endAngle, clockwise);

        // Check if point angle is within arc span
        double angleFromStart = CalculateAngularSpan(startAngle, pointAngle, clockwise);
        
        double kp, closestE, closestN;

        if (angleFromStart >= 0 && angleFromStart <= Math.Abs(angularSpan))
        {
            // Point projects onto arc
            double fraction = angleFromStart / Math.Abs(angularSpan);
            kp = segment.StartKp + fraction * segment.Length;

            // Closest point is on arc at same angle as point
            closestE = centerE + radius * Math.Sin(pointAngle);
            closestN = centerN + radius * Math.Cos(pointAngle);
        }
        else
        {
            // Point is outside arc span - closest to one of the endpoints
            double distToStart = Math.Sqrt(
                Math.Pow(easting - segment.StartEasting, 2) +
                Math.Pow(northing - segment.StartNorthing, 2));
            double distToEnd = Math.Sqrt(
                Math.Pow(easting - segment.EndEasting, 2) +
                Math.Pow(northing - segment.EndNorthing, 2));

            if (distToStart < distToEnd)
            {
                kp = segment.StartKp;
                closestE = segment.StartEasting;
                closestN = segment.StartNorthing;
            }
            else
            {
                kp = segment.EndKp;
                closestE = segment.EndEasting;
                closestN = segment.EndNorthing;
            }
        }

        // Distance from point to closest point
        double distance = Math.Sqrt(
            Math.Pow(easting - closestE, 2) +
            Math.Pow(northing - closestN, 2));

        // DCC: positive if outside arc (further from center), negative if inside
        // For clockwise arcs, "outside" is to the right; for counter-clockwise, it's to the left
        double dcc;
        if (clockwise)
        {
            dcc = radius - distToCenter; // Inside arc is positive DCC for clockwise
        }
        else
        {
            dcc = distToCenter - radius; // Outside arc is positive DCC for counter-clockwise
        }

        return (kp, dcc, distance);
    }

    /// <summary>
    /// Calculate angular span between two angles considering direction
    /// </summary>
    private double CalculateAngularSpan(double fromAngle, double toAngle, bool clockwise)
    {
        double span = toAngle - fromAngle;

        // Normalize to -PI to PI range
        while (span > Math.PI) span -= 2 * Math.PI;
        while (span < -Math.PI) span += 2 * Math.PI;

        if (clockwise)
        {
            // Clockwise: negative span expected
            if (span > 0) span -= 2 * Math.PI;
            return -span; // Return positive value
        }
        else
        {
            // Counter-clockwise: positive span expected
            if (span < 0) span += 2 * Math.PI;
            return span;
        }
    }

    /// <summary>
    /// Convert KP value to output units
    /// </summary>
    private double ConvertKp(double kp)
    {
        // Input KP from route file is typically in kilometers
        // Convert to user's preferred unit
        return _outputKpUnit switch
        {
            LengthUnit.Kilometer => kp,
            LengthUnit.Meter => kp * 1000.0,
            LengthUnit.USSurveyFeet => kp * 1000.0 / 0.3048006096012192,
            LengthUnit.NauticalMile => kp / 1.852,
            _ => kp
        };
    }

    /// <summary>
    /// Calculate KP/DCC for all points in a list
    /// </summary>
    public void CalculateAll(IList<SurveyPoint> points, IProgress<int>? progress = null)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var (kp, dcc) = Calculate(point.Easting, point.Northing);
            point.Kp = kp;
            point.Dcc = dcc;

            if (progress != null && i % 100 == 0)
            {
                progress.Report((int)(100.0 * i / points.Count));
            }
        }
    }

    /// <summary>
    /// Generate points at regular intervals along the route
    /// </summary>
    /// <param name="interval">Interval in route coordinate units</param>
    /// <returns>List of generated points</returns>
    public List<(double Kp, double Easting, double Northing)> GeneratePointsAtInterval(double interval)
    {
        var points = new List<(double, double, double)>();

        double currentKp = _route.StartKp;
        double endKp = _route.EndKp;

        while (currentKp <= endKp)
        {
            var coords = _route.GetCoordinatesAtKp(currentKp);
            if (coords.HasValue)
            {
                points.Add((ConvertKp(currentKp), coords.Value.Easting, coords.Value.Northing));
            }

            // Increment based on interval (converted from meters to KP units)
            // If interval is 1 meter and KP is in km, increment is 0.001
            currentKp += interval / 1000.0; // Assuming KP in km, interval in meters
        }

        return points;
    }

    /// <summary>
    /// Get the perpendicular offset at a given KP (useful for parallel routes)
    /// </summary>
    public (double Easting, double Northing)? GetOffsetPoint(double kp, double offset)
    {
        var segment = _route.FindSegmentAtKp(kp);
        if (segment == null)
            return null;

        var coords = _route.GetCoordinatesAtKp(kp);
        if (!coords.HasValue)
            return null;

        double perpE, perpN;

        if (segment.IsStraight)
        {
            // Perpendicular direction
            double dx = segment.EndEasting - segment.StartEasting;
            double dy = segment.EndNorthing - segment.StartNorthing;
            double len = Math.Sqrt(dx * dx + dy * dy);

            // Perpendicular unit vector (rotated 90 degrees to the right)
            perpE = dy / len;
            perpN = -dx / len;
        }
        else
        {
            // For arc, perpendicular is radial direction
            var (centerE, centerN) = segment.GetArcCenter();
            double dx = coords.Value.Easting - centerE;
            double dy = coords.Value.Northing - centerN;
            double len = Math.Sqrt(dx * dx + dy * dy);

            perpE = dx / len;
            perpN = dy / len;

            // Flip direction for clockwise arcs
            if (segment.IsClockwise)
            {
                perpE = -perpE;
                perpN = -perpN;
            }
        }

        return (coords.Value.Easting + offset * perpE, coords.Value.Northing + offset * perpN);
    }
}
