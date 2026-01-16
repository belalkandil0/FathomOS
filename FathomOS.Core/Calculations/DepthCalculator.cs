namespace FathomOS.Core.Calculations;

using FathomOS.Core.Models;

/// <summary>
/// Calculates final depths applying various offsets and corrections
/// </summary>
public class DepthCalculator
{
    private readonly ProcessingOptions _options;

    public DepthCalculator(ProcessingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Calculate seabed depth from bathy depth and ROV altitude
    /// Formula from original Excel template:
    /// Seabed Depth = BathyDepth + Altitude + Bathy_Altimeter_Offset - Tide
    /// (Tide correction is applied separately)
    /// </summary>
    /// <param name="bathyDepth">Echo sounder depth (ROV depth below surface)</param>
    /// <param name="rovAltitude">ROV altitude above seabed (from altimeter)</param>
    /// <returns>Calculated seabed depth</returns>
    public double CalculateSeabedDepth(double bathyDepth, double rovAltitude)
    {
        // Seabed = ROV_Depth + Height_Above_Seabed + Sensor_Offset
        // If offset is not set (null), use 0
        double offset = _options.BathyToAltimeterOffset ?? 0.0;
        return bathyDepth + rovAltitude + offset;
    }

    /// <summary>
    /// Calculate ROV depth from bathy depth
    /// ROV Depth = Bathy + Offset
    /// </summary>
    /// <param name="bathyDepth">Echo sounder depth</param>
    /// <returns>ROV reference depth</returns>
    public double CalculateRovDepth(double bathyDepth)
    {
        // If offset is not set (null), use 0
        double offset = _options.BathyToRovRefOffset ?? 0.0;
        return bathyDepth + offset;
    }

    /// <summary>
    /// Apply depth exaggeration for CAD visualization
    /// </summary>
    /// <param name="depth">Original depth</param>
    /// <returns>Exaggerated depth for visualization</returns>
    public double ApplyExaggeration(double depth)
    {
        return depth * _options.DepthExaggeration;
    }

    /// <summary>
    /// Process all survey points applying vertical offsets
    /// </summary>
    /// <param name="points">Survey points to process</param>
    /// <param name="surveyType">Type of survey (affects calculation method)</param>
    /// <param name="progress">Optional progress reporter</param>
    public void ProcessAll(IList<SurveyPoint> points, SurveyType surveyType, IProgress<int>? progress = null)
    {
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            ProcessPoint(point, surveyType);

            if (progress != null && i % 100 == 0)
            {
                progress.Report((int)(100.0 * i / points.Count));
            }
        }
    }

    /// <summary>
    /// Process a single survey point
    /// </summary>
    private void ProcessPoint(SurveyPoint point, SurveyType surveyType)
    {
        if (!point.Depth.HasValue)
            return;

        double baseDepth = point.CorrectedDepth ?? point.Depth.Value;

        if (_options.ApplyVerticalOffsets)
        {
            if (surveyType == SurveyType.Seabed && point.Altitude.HasValue)
            {
                // Seabed survey: calculate seabed depth from bathy and altitude
                baseDepth = CalculateSeabedDepth(baseDepth, point.Altitude.Value);
            }
            else if (surveyType == SurveyType.RovDynamic)
            {
                // ROV survey: apply ROV reference offset
                baseDepth = CalculateRovDepth(baseDepth);
            }
        }

        point.CorrectedDepth = baseDepth;
    }

    /// <summary>
    /// Generate depth statistics for a set of points
    /// </summary>
    public DepthStatistics GetStatistics(IList<SurveyPoint> points)
    {
        var stats = new DepthStatistics();

        var depthValues = points
            .Where(p => p.CorrectedDepth.HasValue)
            .Select(p => p.CorrectedDepth!.Value)
            .ToList();

        if (depthValues.Count == 0)
            return stats;

        stats.MinDepth = depthValues.Min();
        stats.MaxDepth = depthValues.Max();
        stats.MeanDepth = depthValues.Average();
        stats.DepthRange = stats.MaxDepth - stats.MinDepth;
        stats.RecordCount = depthValues.Count;

        // Standard deviation
        double sumSquares = depthValues.Sum(d => Math.Pow(d - stats.MeanDepth, 2));
        stats.StdDeviation = Math.Sqrt(sumSquares / depthValues.Count);

        return stats;
    }
}

/// <summary>
/// Depth statistics for a survey
/// </summary>
public class DepthStatistics
{
    public double MinDepth { get; set; }
    public double MaxDepth { get; set; }
    public double MeanDepth { get; set; }
    public double DepthRange { get; set; }
    public double StdDeviation { get; set; }
    public int RecordCount { get; set; }
}
