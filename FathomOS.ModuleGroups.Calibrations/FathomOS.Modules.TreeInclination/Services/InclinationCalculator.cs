namespace FathomOS.Modules.TreeInclination.Services;

using FathomOS.Modules.TreeInclination.Models;

/// <summary>
/// Core inclination calculation service for subsea structures.
/// 
/// Coordinate Convention:
/// - X axis: Positive towards corner 2 (typically East or structure-relative)
/// - Y axis: Positive towards corner 4 (typically North or structure-relative)  
/// - Z axis: Positive downwards (depth)
/// 
/// Corner Layout (viewed from above):
///     1 -------- 2
///     |          |
///     |          |
///     4 -------- 3
/// 
/// Pitch: Rotation about the X-axis (tilt fore/aft)
///   - Positive pitch = corners 4,3 are deeper than corners 1,2 (bow up)
/// 
/// Roll: Rotation about the Y-axis (tilt port/starboard)
///   - Positive roll = corners 2,3 are deeper than corners 1,4 (starboard down)
/// </summary>
public class InclinationCalculator
{
    private readonly InclinationProject _project;

    public InclinationCalculator(InclinationProject project)
    {
        _project = project;
    }

    public InclinationResult Calculate()
    {
        // Get corners excluding closure point, sorted by index
        var corners = _project.Corners
            .Where(c => !c.IsClosurePoint)
            .OrderBy(c => c.Index)
            .ToList();

        if (corners.Count < 3)
            throw new InvalidOperationException("At least 3 corner points required for inclination calculation.");

        // Validate coordinates
        ValidateCoordinates(corners);
        
        // Apply misclosure correction if closure point exists
        ApplyMisclosureCorrection(corners);

        var result = new InclinationResult { CalculatedAt = DateTime.Now };

        // Choose calculation method based on corner count
        if (corners.Count == 4 && _project.Geometry != GeometryType.CustomPolygon)
        {
            Calculate4PointInclination(corners, result);
            result.CalculationMethod = "4-Point Diagonal Method";
        }
        else
        {
            CalculateBestFitPlaneInclination(corners, result);
            result.CalculationMethod = "Best-Fit Plane (Least Squares)";
        }

        EvaluateQualityStatus(result);
        
        // Calculate true bearing if structure heading is known
        CalculateTrueBearing(result);
        
        _project.Result = result;
        return result;
    }

    /// <summary>
    /// Calculate the true bearing of the inclination direction based on structure heading.
    /// </summary>
    private void CalculateTrueBearing(InclinationResult result)
    {
        if (!_project.StructureHeading.HasValue)
            return;
        
        double structureHeading = _project.StructureHeading.Value;
        double relativeHeading = result.InclinationHeading;  // Heading relative to structure X-axis
        
        // True bearing = structure heading + relative heading
        // The relative heading is measured from +X axis (structure "right")
        // If structure heading is the direction of the 1→2 edge (X-axis in structure coords)
        double trueBearing = structureHeading + relativeHeading;
        
        // Normalize to 0-360
        while (trueBearing < 0) trueBearing += 360;
        while (trueBearing >= 360) trueBearing -= 360;
        
        result.TrueInclinationBearing = trueBearing;
        result.StructureHeadingUsed = structureHeading;
        
        // Calculate compass direction description
        result.TrueBearingDescription = trueBearing switch
        {
            >= 337.5 or < 22.5 => "N",
            >= 22.5 and < 67.5 => "NE",
            >= 67.5 and < 112.5 => "E",
            >= 112.5 and < 157.5 => "SE",
            >= 157.5 and < 202.5 => "S",
            >= 202.5 and < 247.5 => "SW",
            >= 247.5 and < 292.5 => "W",
            _ => "NW"
        };
        
        // Calculate orientation-referenced pitch/roll
        // Forward pitch = pitch along structure heading direction
        // Starboard roll = roll perpendicular to heading
        double pitchRad = result.AveragePitch * Math.PI / 180;
        double rollRad = result.AverageRoll * Math.PI / 180;
        
        result.ForwardPitch = result.AveragePitch;  // Already aligned with structure X-axis
        result.StarboardRoll = result.AverageRoll;  // Already aligned with structure Y-axis
    }

    private void ValidateCoordinates(List<CornerMeasurement> corners)
    {
        // Check if all corners have coordinates set
        bool allZero = corners.All(c => Math.Abs(c.X) < 0.001 && Math.Abs(c.Y) < 0.001);
        
        if (allZero)
        {
            throw new InvalidOperationException(
                "Corner coordinates are not set.\n\n" +
                "Options:\n" +
                "1. Enter X,Y positions manually in the Corners tab\n" +
                "2. Load a position fix file with corner coordinates\n" +
                "3. Use typical structure dimensions (e.g., X=0,0,5,5 Y=0,5,5,0 for 5m square)");
        }

        // Check for duplicate positions (with reasonable tolerance)
        // Use 10cm tolerance - corners should be meters apart, not centimeters
        const double minDistanceMeters = 0.10; 
        
        for (int i = 0; i < corners.Count; i++)
        {
            for (int j = i + 1; j < corners.Count; j++)
            {
                double dist = CalculateDistance2D(corners[i], corners[j]);
                if (dist < minDistanceMeters)
                {
                    throw new InvalidOperationException(
                        $"Corners {corners[i].Name} and {corners[j].Name} have identical or nearly identical positions " +
                        $"(distance: {dist:F3}m).\n\n" +
                        $"{corners[i].Name}: X={corners[i].X:F3}, Y={corners[i].Y:F3}\n" +
                        $"{corners[j].Name}: X={corners[j].X:F3}, Y={corners[j].Y:F3}\n\n" +
                        "The Easting/Northing from NPD files may be vessel positions, not corner positions.\n" +
                        "Please enter actual corner coordinates manually in the Corners tab,\n" +
                        "or load a position fix file with the structure corner locations.");
                }
            }
        }
    }

    private void ApplyMisclosureCorrection(List<CornerMeasurement> corners)
    {
        var closurePoint = _project.Corners.FirstOrDefault(c => c.IsClosurePoint);
        
        if (closurePoint == null)
        {
            // No closure point - corrected depths should already be set by ApplyTideCorrections
            // Just validate they're not zero
            foreach (var corner in corners)
            {
                if (Math.Abs(corner.CorrectedDepth) < 0.001)
                {
                    corner.CorrectedDepth = corner.RawDepthAverage;
                }
            }
            return;
        }

        // Find first point (should match closure point measurement location)
        var firstPoint = corners.FirstOrDefault();
        if (firstPoint == null) return;

        // Calculate misclosure: difference between first measurement and closure measurement at same location
        // Both should already have tide+draft corrections applied in their CorrectedDepth
        double firstDepth = firstPoint.CorrectedDepth;
        double closureDepth = closurePoint.CorrectedDepth;
        double misclosure = closureDepth - firstDepth; // Positive = drift caused deeper readings over time

        int n = corners.Count;

        // Distribute misclosure linearly based on measurement order
        // Assumption: measurements were taken in sequence (corner 1, 2, 3, 4, closure)
        // First point gets 0 correction, each subsequent point gets proportionally more
        for (int i = 0; i < n; i++)
        {
            double proportion = (double)i / n;
            double correction = misclosure * proportion;
            corners[i].CorrectedDepth = corners[i].CorrectedDepth - correction;
        }
    }

    /// <summary>
    /// Calculate inclination using the 4-point method for rectangular structures.
    /// 
    /// Corner layout (viewed from above):
    ///     1 -------- 2       (TOP edge)
    ///     |          |
    ///     |          |
    ///     4 -------- 3       (BOTTOM edge)
    ///   (LEFT)     (RIGHT)
    /// 
    /// Pitch: Tilt about horizontal axis parallel to 1-2 edge
    ///   = (avg depth of bottom edge - avg depth of top edge) / vertical distance
    /// 
    /// Roll: Tilt about horizontal axis parallel to 1-4 edge  
    ///   = (avg depth of right edge - avg depth of left edge) / horizontal distance
    /// </summary>
    private void Calculate4PointInclination(List<CornerMeasurement> corners, InclinationResult result)
    {
        if (corners.Count != 4) return;

        // Get corners by array index (0=corner1, 1=corner2, 2=corner3, 3=corner4)
        var c1 = corners[0];
        var c2 = corners[1];
        var c3 = corners[2];
        var c4 = corners[3];

        // Get depths (positive downward)
        double z1 = c1.CorrectedDepth;
        double z2 = c2.CorrectedDepth;
        double z3 = c3.CorrectedDepth;
        double z4 = c4.CorrectedDepth;

        // Calculate structure dimensions
        // Horizontal distance (1-2 and 4-3 edges) - "width"
        double width = (CalculateDistance2D(c1, c2) + CalculateDistance2D(c4, c3)) / 2.0;
        // Vertical distance (1-4 and 2-3 edges) - "length"  
        double length = (CalculateDistance2D(c1, c4) + CalculateDistance2D(c2, c3)) / 2.0;

        if (width < 0.001 || length < 0.001)
        {
            throw new InvalidOperationException("Structure dimensions are too small for accurate calculation.");
        }

        // PITCH: Tilt in the fore-aft direction (comparing top vs bottom edges)
        // Positive pitch = bottom deeper than top = tilting towards bottom
        double avgDepthTop = (z1 + z2) / 2.0;      // Top edge (corners 1,2)
        double avgDepthBottom = (z4 + z3) / 2.0;   // Bottom edge (corners 4,3)
        double dzPitch = avgDepthBottom - avgDepthTop;
        result.AveragePitch = Math.Atan2(dzPitch, length) * 180.0 / Math.PI;

        // ROLL: Tilt in the port-starboard direction (comparing left vs right edges)
        // Positive roll = right deeper than left = tilting towards right
        double avgDepthLeft = (z1 + z4) / 2.0;     // Left edge (corners 1,4)
        double avgDepthRight = (z2 + z3) / 2.0;    // Right edge (corners 2,3)
        double dzRoll = avgDepthRight - avgDepthLeft;
        result.AverageRoll = Math.Atan2(dzRoll, width) * 180.0 / Math.PI;

        // Total inclination: angle between plane normal and vertical
        // For small angles: total ≈ sqrt(pitch² + roll²) but this isn't exact
        // Proper calculation uses the direction cosines
        double pitchRad = result.AveragePitch * Math.PI / 180.0;
        double rollRad = result.AverageRoll * Math.PI / 180.0;
        
        // Total inclination from vertical
        result.TotalInclination = Math.Acos(Math.Cos(pitchRad) * Math.Cos(rollRad)) * 180.0 / Math.PI;
        
        // Heading: direction of maximum slope (azimuth towards deepest point)
        // atan2(roll, pitch) gives angle from pitch axis
        result.InclinationHeading = Math.Atan2(result.AverageRoll, result.AveragePitch) * 180.0 / Math.PI;
        if (result.InclinationHeading < 0) result.InclinationHeading += 360.0;

        // Mean depth
        result.MeanDepth = (z1 + z2 + z3 + z4) / 4.0;

        // Misclosure (if closure point exists)
        // Misclosure = difference between first corner depth and closure re-measurement of same corner
        var closurePoint = _project.Corners.FirstOrDefault(c => c.IsClosurePoint);
        if (closurePoint != null)
        {
            // Use corrected depths (already have tide+draft applied)
            result.Misclosure = Math.Abs(closurePoint.CorrectedDepth - c1.CorrectedDepth);
        }

        // Out-of-plane: Check if 4 points are coplanar using diagonals
        // For a perfect plane, the center point (intersection of diagonals) 
        // should have the same depth when calculated from either diagonal
        double expectedCenter13 = (z1 + z3) / 2.0;  // Center from diagonal 1-3
        double expectedCenter24 = (z2 + z4) / 2.0;  // Center from diagonal 2-4
        result.OutOfPlane = Math.Abs(expectedCenter13 - expectedCenter24);
    }

    /// <summary>
    /// Calculate inclination by fitting a plane to all corner points using least squares.
    /// Used for 5+ corners or irregular shapes.
    /// 
    /// Plane equation: z = ax + by + c
    /// Where a = dz/dx (slope in X), b = dz/dy (slope in Y), c = z-intercept
    /// </summary>
    private void CalculateBestFitPlaneInclination(List<CornerMeasurement> corners, InclinationResult result)
    {
        int n = corners.Count;
        
        // Accumulate sums for least squares
        double sumX = 0, sumY = 0, sumZ = 0;
        double sumXX = 0, sumYY = 0, sumXY = 0;
        double sumXZ = 0, sumYZ = 0;

        foreach (var corner in corners)
        {
            double x = UnitConversions.LengthToMeters(corner.X, _project.DimensionUnit);
            double y = UnitConversions.LengthToMeters(corner.Y, _project.DimensionUnit);
            double z = corner.CorrectedDepth;

            sumX += x; sumY += y; sumZ += z;
            sumXX += x * x; sumYY += y * y; sumXY += x * y;
            sumXZ += x * z; sumYZ += y * z;
        }

        // Solve 3x3 system using Cramer's rule:
        // [sumXX sumXY sumX] [a]   [sumXZ]
        // [sumXY sumYY sumY] [b] = [sumYZ]
        // [sumX  sumY  n   ] [c]   [sumZ ]

        double det = sumXX * (sumYY * n - sumY * sumY) -
                     sumXY * (sumXY * n - sumY * sumX) +
                     sumX * (sumXY * sumY - sumYY * sumX);

        if (Math.Abs(det) < 1e-10)
        {
            throw new InvalidOperationException("Cannot fit plane - points may be collinear.");
        }

        double a = (sumXZ * (sumYY * n - sumY * sumY) -
                   sumXY * (sumYZ * n - sumY * sumZ) +
                   sumX * (sumYZ * sumY - sumYY * sumZ)) / det;

        double b = (sumXX * (sumYZ * n - sumY * sumZ) -
                   sumXZ * (sumXY * n - sumY * sumX) +
                   sumX * (sumXY * sumZ - sumYZ * sumX)) / det;

        double c = (sumXX * (sumYY * sumZ - sumYZ * sumY) -
                   sumXY * (sumXY * sumZ - sumYZ * sumX) +
                   sumXZ * (sumXY * sumY - sumYY * sumX)) / det;

        // Store plane parameters (plane: ax + by - z + c = 0, or z = ax + by + c)
        result.BestFitPlane = new PlaneParameters { A = a, B = b, C = -1, D = c };

        // Convert gradients to angles
        // Roll = angle of slope in X direction (dz/dx)
        // Pitch = angle of slope in Y direction (dz/dy)
        result.AverageRoll = Math.Atan(a) * 180.0 / Math.PI;
        result.AveragePitch = Math.Atan(b) * 180.0 / Math.PI;

        // Total inclination: angle between plane normal (a, b, -1) and vertical (0, 0, -1)
        // cos(theta) = |dot(n1, n2)| / (|n1| * |n2|)
        // n1 = (a, b, -1), n2 = (0, 0, -1)
        // dot = 1, |n1| = sqrt(a² + b² + 1), |n2| = 1
        result.TotalInclination = Math.Acos(1.0 / Math.Sqrt(a * a + b * b + 1)) * 180.0 / Math.PI;
        
        // Heading: direction of steepest descent
        result.InclinationHeading = Math.Atan2(a, b) * 180.0 / Math.PI;
        if (result.InclinationHeading < 0) result.InclinationHeading += 360.0;

        result.MeanDepth = sumZ / n;

        // Calculate residuals (how well points fit the plane)
        double sumResidualsSq = 0;
        double maxResidual = 0;

        foreach (var corner in corners)
        {
            double x = UnitConversions.LengthToMeters(corner.X, _project.DimensionUnit);
            double y = UnitConversions.LengthToMeters(corner.Y, _project.DimensionUnit);
            double expectedZ = a * x + b * y + c;
            double residual = Math.Abs(corner.CorrectedDepth - expectedZ);
            sumResidualsSq += residual * residual;
            maxResidual = Math.Max(maxResidual, residual);
        }

        result.BestFitPlane.RmsError = Math.Sqrt(sumResidualsSq / n);
        result.BestFitPlane.MaxResidual = maxResidual;
        result.OutOfPlane = maxResidual;

        // Misclosure = difference between first corner depth and closure re-measurement
        var closurePoint = _project.Corners.FirstOrDefault(c => c.IsClosurePoint);
        var firstPoint = corners.FirstOrDefault();
        if (closurePoint != null && firstPoint != null)
        {
            // Use corrected depths (already have tide+draft applied)
            result.Misclosure = Math.Abs(closurePoint.CorrectedDepth - firstPoint.CorrectedDepth);
        }
    }

    private void EvaluateQualityStatus(InclinationResult result)
    {
        var tol = _project.Tolerances;

        // Inclination status
        if (result.TotalInclination <= tol.WarningInclination)
            result.InclinationStatus = QualityStatus.OK;
        else if (result.TotalInclination <= tol.MaxInclination)
            result.InclinationStatus = QualityStatus.Warning;
        else
            result.InclinationStatus = QualityStatus.Failed;

        // Misclosure status
        if (result.Misclosure <= tol.WarningMisclosure)
            result.MisclosureStatus = QualityStatus.OK;
        else if (result.Misclosure <= tol.MaxMisclosure)
            result.MisclosureStatus = QualityStatus.Warning;
        else
            result.MisclosureStatus = QualityStatus.Failed;

        // Out of plane status
        if (result.OutOfPlane <= tol.WarningOutOfPlane)
            result.OutOfPlaneStatus = QualityStatus.OK;
        else if (result.OutOfPlane <= tol.MaxOutOfPlane)
            result.OutOfPlaneStatus = QualityStatus.Warning;
        else
            result.OutOfPlaneStatus = QualityStatus.Failed;
    }

    private double CalculateDistance2D(CornerMeasurement c1, CornerMeasurement c2)
    {
        double dx = UnitConversions.LengthToMeters(c2.X - c1.X, _project.DimensionUnit);
        double dy = UnitConversions.LengthToMeters(c2.Y - c1.Y, _project.DimensionUnit);
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
