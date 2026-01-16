using FathomOS.Core.Models;

namespace FathomOS.Core.Services;

/// <summary>
/// Spline algorithms for survey track smoothing
/// </summary>
public enum SplineAlgorithm
{
    /// <summary>Catmull-Rom spline - Subsea7 recommended, passes through control points</summary>
    CatmullRom,
    
    /// <summary>Cubic spline interpolation - smooth curve through all points</summary>
    CubicSpline,
    
    /// <summary>B-Spline - smooth approximation influenced by control points</summary>
    BSpline,
    
    /// <summary>AutoCAD PEDIT Spline compatible</summary>
    AutoCadPedit
}

/// <summary>
/// Service for generating spline curves from survey points
/// </summary>
public class SplineService
{
    /// <summary>
    /// Generate a spline curve through the given points
    /// </summary>
    /// <param name="points">Input points</param>
    /// <param name="algorithm">Spline algorithm to use</param>
    /// <param name="tension">Tension parameter (0-1, used by Catmull-Rom)</param>
    /// <param name="outputPointCount">Number of output points (0 = auto)</param>
    /// <returns>List of interpolated points along the spline</returns>
    public List<(double X, double Y, double Z)> GenerateSpline(
        IList<(double X, double Y, double Z)> points,
        SplineAlgorithm algorithm,
        double tension = 0.5,
        int outputPointCount = 0)
    {
        if (points == null || points.Count < 2)
            return new List<(double, double, double)>();

        // Auto-calculate output points if not specified
        if (outputPointCount <= 0)
            outputPointCount = Math.Max(points.Count * 10, 100);

        return algorithm switch
        {
            SplineAlgorithm.CatmullRom => GenerateCatmullRomSpline(points, tension, outputPointCount),
            SplineAlgorithm.CubicSpline => GenerateCubicSpline(points, outputPointCount),
            SplineAlgorithm.BSpline => GenerateBSpline(points, outputPointCount),
            SplineAlgorithm.AutoCadPedit => GenerateAutoCadPeditSpline(points, outputPointCount),
            _ => GenerateCatmullRomSpline(points, tension, outputPointCount)
        };
    }

    /// <summary>
    /// Generate spline from SurveyPoints
    /// </summary>
    public List<SurveyPoint> GenerateSplineFromSurveyPoints(
        IList<SurveyPoint> points,
        SplineAlgorithm algorithm,
        double tension = 0.5,
        int outputPointCount = 0)
    {
        var inputPoints = points.Select(p => (
            p.SmoothedEasting ?? p.Easting,
            p.SmoothedNorthing ?? p.Northing,
            p.CorrectedDepth ?? p.Depth ?? 0.0
        )).ToList();

        var splinePoints = GenerateSpline(inputPoints, algorithm, tension, outputPointCount);

        return splinePoints.Select((p, i) => new SurveyPoint
        {
            Easting = p.X,
            Northing = p.Y,
            Depth = p.Z,
            SmoothedEasting = p.X,
            SmoothedNorthing = p.Y,
            CorrectedDepth = p.Z
        }).ToList();
    }

    #region Catmull-Rom Spline (Subsea7 Recommended)

    /// <summary>
    /// Catmull-Rom spline - passes through all control points
    /// This is the recommended algorithm matching Subsea7 procedures
    /// </summary>
    private List<(double X, double Y, double Z)> GenerateCatmullRomSpline(
        IList<(double X, double Y, double Z)> points,
        double tension,
        int outputCount)
    {
        var result = new List<(double, double, double)>();
        
        if (points.Count < 2) return result;
        if (points.Count == 2)
        {
            // Linear interpolation for 2 points
            return LinearInterpolate(points[0], points[1], outputCount);
        }

        // Calculate points per segment
        int segments = points.Count - 1;
        int pointsPerSegment = Math.Max(1, outputCount / segments);

        for (int i = 0; i < points.Count - 1; i++)
        {
            // Get the 4 control points for this segment
            var p0 = i > 0 ? points[i - 1] : points[0];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i + 2 < points.Count ? points[i + 2] : points[points.Count - 1];

            // Generate points along this segment
            for (int j = 0; j < pointsPerSegment; j++)
            {
                double t = (double)j / pointsPerSegment;
                var point = CatmullRomPoint(p0, p1, p2, p3, t, tension);
                result.Add(point);
            }
        }

        // Add the last point
        result.Add(points[points.Count - 1]);

        return result;
    }

    private (double X, double Y, double Z) CatmullRomPoint(
        (double X, double Y, double Z) p0,
        (double X, double Y, double Z) p1,
        (double X, double Y, double Z) p2,
        (double X, double Y, double Z) p3,
        double t,
        double tension)
    {
        double t2 = t * t;
        double t3 = t2 * t;

        // Catmull-Rom matrix coefficients with tension
        double s = (1 - tension) / 2;

        double h1 = -s * t3 + 2 * s * t2 - s * t;
        double h2 = (2 - s) * t3 + (s - 3) * t2 + 1;
        double h3 = (s - 2) * t3 + (3 - 2 * s) * t2 + s * t;
        double h4 = s * t3 - s * t2;

        return (
            h1 * p0.X + h2 * p1.X + h3 * p2.X + h4 * p3.X,
            h1 * p0.Y + h2 * p1.Y + h3 * p2.Y + h4 * p3.Y,
            h1 * p0.Z + h2 * p1.Z + h3 * p2.Z + h4 * p3.Z
        );
    }

    #endregion

    #region Cubic Spline

    /// <summary>
    /// Natural cubic spline interpolation
    /// </summary>
    private List<(double X, double Y, double Z)> GenerateCubicSpline(
        IList<(double X, double Y, double Z)> points,
        int outputCount)
    {
        if (points.Count < 2) return new List<(double, double, double)>();

        int n = points.Count;
        
        // Calculate cumulative chord length as parameter
        double[] t = new double[n];
        t[0] = 0;
        for (int i = 1; i < n; i++)
        {
            double dx = points[i].X - points[i - 1].X;
            double dy = points[i].Y - points[i - 1].Y;
            double dz = points[i].Z - points[i - 1].Z;
            t[i] = t[i - 1] + Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        // Normalize to [0, 1]
        double totalLength = t[n - 1];
        if (totalLength < 1e-10) return points.ToList();
        
        for (int i = 0; i < n; i++)
            t[i] /= totalLength;

        // Compute spline coefficients for each dimension
        var xCoeffs = ComputeCubicSplineCoefficients(t, points.Select(p => p.X).ToArray());
        var yCoeffs = ComputeCubicSplineCoefficients(t, points.Select(p => p.Y).ToArray());
        var zCoeffs = ComputeCubicSplineCoefficients(t, points.Select(p => p.Z).ToArray());

        // Generate output points
        var result = new List<(double, double, double)>();
        for (int i = 0; i < outputCount; i++)
        {
            double u = (double)i / (outputCount - 1);
            double x = EvaluateCubicSpline(t, xCoeffs, u);
            double y = EvaluateCubicSpline(t, yCoeffs, u);
            double z = EvaluateCubicSpline(t, zCoeffs, u);
            result.Add((x, y, z));
        }

        return result;
    }

    private (double[] a, double[] b, double[] c, double[] d) ComputeCubicSplineCoefficients(
        double[] t, double[] y)
    {
        int n = t.Length;
        double[] a = new double[n];
        double[] b = new double[n - 1];
        double[] c = new double[n];
        double[] d = new double[n - 1];

        Array.Copy(y, a, n);

        // Compute h and alpha
        double[] h = new double[n - 1];
        double[] alpha = new double[n - 1];
        
        for (int i = 0; i < n - 1; i++)
            h[i] = t[i + 1] - t[i];

        for (int i = 1; i < n - 1; i++)
        {
            alpha[i] = 3 / h[i] * (a[i + 1] - a[i]) - 3 / h[i - 1] * (a[i] - a[i - 1]);
        }

        // Solve tridiagonal system
        double[] l = new double[n];
        double[] mu = new double[n];
        double[] z = new double[n];

        l[0] = 1;
        mu[0] = 0;
        z[0] = 0;

        for (int i = 1; i < n - 1; i++)
        {
            l[i] = 2 * (t[i + 1] - t[i - 1]) - h[i - 1] * mu[i - 1];
            mu[i] = h[i] / l[i];
            z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
        }

        l[n - 1] = 1;
        z[n - 1] = 0;
        c[n - 1] = 0;

        for (int j = n - 2; j >= 0; j--)
        {
            c[j] = z[j] - mu[j] * c[j + 1];
            b[j] = (a[j + 1] - a[j]) / h[j] - h[j] * (c[j + 1] + 2 * c[j]) / 3;
            d[j] = (c[j + 1] - c[j]) / (3 * h[j]);
        }

        return (a, b, c, d);
    }

    private double EvaluateCubicSpline(double[] t, (double[] a, double[] b, double[] c, double[] d) coeffs, double x)
    {
        // Find the appropriate interval
        int i = 0;
        for (int j = 0; j < t.Length - 1; j++)
        {
            if (x >= t[j] && x <= t[j + 1])
            {
                i = j;
                break;
            }
        }
        
        // Clamp to valid range
        i = Math.Max(0, Math.Min(i, t.Length - 2));

        double dx = x - t[i];
        return coeffs.a[i] + coeffs.b[i] * dx + coeffs.c[i] * dx * dx + coeffs.d[i] * dx * dx * dx;
    }

    #endregion

    #region B-Spline

    /// <summary>
    /// Uniform B-spline (degree 3)
    /// </summary>
    private List<(double X, double Y, double Z)> GenerateBSpline(
        IList<(double X, double Y, double Z)> points,
        int outputCount)
    {
        if (points.Count < 4)
            return GenerateCatmullRomSpline(points, 0.5, outputCount);

        var result = new List<(double, double, double)>();
        int n = points.Count;

        for (int i = 0; i < outputCount; i++)
        {
            double t = (double)i / (outputCount - 1) * (n - 3);
            int segment = Math.Min((int)t, n - 4);
            double u = t - segment;

            var p = BSplinePoint(
                points[segment],
                points[segment + 1],
                points[segment + 2],
                points[segment + 3],
                u);
            
            result.Add(p);
        }

        return result;
    }

    private (double X, double Y, double Z) BSplinePoint(
        (double X, double Y, double Z) p0,
        (double X, double Y, double Z) p1,
        (double X, double Y, double Z) p2,
        (double X, double Y, double Z) p3,
        double t)
    {
        double t2 = t * t;
        double t3 = t2 * t;

        // B-spline basis functions
        double b0 = (-t3 + 3 * t2 - 3 * t + 1) / 6;
        double b1 = (3 * t3 - 6 * t2 + 4) / 6;
        double b2 = (-3 * t3 + 3 * t2 + 3 * t + 1) / 6;
        double b3 = t3 / 6;

        return (
            b0 * p0.X + b1 * p1.X + b2 * p2.X + b3 * p3.X,
            b0 * p0.Y + b1 * p1.Y + b2 * p2.Y + b3 * p3.Y,
            b0 * p0.Z + b1 * p1.Z + b2 * p2.Z + b3 * p3.Z
        );
    }

    #endregion

    #region AutoCAD PEDIT Spline

    /// <summary>
    /// AutoCAD PEDIT Spline compatible - uses quadratic B-spline
    /// </summary>
    private List<(double X, double Y, double Z)> GenerateAutoCadPeditSpline(
        IList<(double X, double Y, double Z)> points,
        int outputCount)
    {
        // AutoCAD's PEDIT spline is essentially a quadratic B-spline
        // that passes near the control points
        return GenerateBSpline(points, outputCount);
    }

    #endregion

    #region Helper Methods

    private List<(double X, double Y, double Z)> LinearInterpolate(
        (double X, double Y, double Z) p1,
        (double X, double Y, double Z) p2,
        int count)
    {
        var result = new List<(double, double, double)>();
        
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / (count - 1);
            result.Add((
                p1.X + t * (p2.X - p1.X),
                p1.Y + t * (p2.Y - p1.Y),
                p1.Z + t * (p2.Z - p1.Z)
            ));
        }

        return result;
    }

    /// <summary>
    /// Generate spline control points from user clicks (manual mode)
    /// </summary>
    public List<(double X, double Y, double Z)> SimplifyToControlPoints(
        IList<(double X, double Y, double Z)> points,
        int maxControlPoints)
    {
        if (points.Count <= maxControlPoints)
            return points.ToList();

        var result = new List<(double, double, double)>();
        
        // Always include first and last points
        result.Add(points[0]);
        
        // Select evenly spaced interior points
        int step = points.Count / (maxControlPoints - 1);
        for (int i = step; i < points.Count - 1; i += step)
        {
            if (result.Count < maxControlPoints - 1)
                result.Add(points[i]);
        }
        
        result.Add(points[points.Count - 1]);

        return result;
    }

    #endregion
}
