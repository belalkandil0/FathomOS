namespace FathomOS.Core.Services;

using FathomOS.Core.Models;

/// <summary>
/// Service for applying tidal corrections and vertical offsets to survey data
/// </summary>
public class TideCorrectionService
{
    private readonly TideData? _tideData;
    private readonly ProcessingOptions _options;
    private readonly bool _useFeetForTide;

    public TideCorrectionService(TideData? tideData, ProcessingOptions options, bool useFeetForTide = false)
    {
        _tideData = tideData;
        _options = options;
        _useFeetForTide = useFeetForTide;
    }

    /// <summary>
    /// Apply all corrections to a list of survey points
    /// </summary>
    public CorrectionResult ApplyCorrections(IList<SurveyPoint> points, IProgress<int>? progress = null)
    {
        var result = new CorrectionResult();
        var warnings = new List<string>();

        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            
            try
            {
                ApplyCorrectionsToPoint(point, warnings);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                warnings.Add($"Point {point.RecordNumber}: {ex.Message}");
                result.FailedCount++;
            }

            if (progress != null && i % 100 == 0)
            {
                progress.Report((int)(100.0 * i / points.Count));
            }
        }

        result.Warnings = warnings;
        return result;
    }

    /// <summary>
    /// Apply corrections to a single point
    /// </summary>
    private void ApplyCorrectionsToPoint(SurveyPoint point, List<string> warnings)
    {
        if (!point.Depth.HasValue)
        {
            point.CorrectedDepth = null;
            return;
        }

        double correctedDepth = point.Depth.Value;

        // Apply tidal correction
        if (_options.ApplyTidalCorrections && _tideData != null)
        {
            double? tide = _useFeetForTide 
                ? _tideData.GetTideAtTimeInFeet(point.DateTime)
                : _tideData.GetTideAtTime(point.DateTime);

            if (tide.HasValue)
            {
                point.TideCorrection = tide.Value;
                correctedDepth += tide.Value; // Positive tide = higher water = deeper depth
            }
            else
            {
                warnings.Add($"Point {point.RecordNumber}: No tide data available for {point.DateTime:HH:mm:ss}");
            }
        }

        // Apply vertical offsets
        if (_options.ApplyVerticalOffsets)
        {
            // Bathy to Altimeter offset is applied when using altimeter-derived depths
            // Bathy to ROV Reference offset is applied for ROV positioning
            // These depend on the depth source - for now we'll apply a general offset

            // If depth source contains "Alt" or "Altimeter", apply altimeter offset
            if (point.DepthSource?.Contains("Alt", StringComparison.OrdinalIgnoreCase) == true
                && _options.BathyToAltimeterOffset.HasValue)
            {
                correctedDepth += _options.BathyToAltimeterOffset.Value;
            }
            // If depth source contains "ROV", apply ROV reference offset
            else if (point.DepthSource?.Contains("ROV", StringComparison.OrdinalIgnoreCase) == true
                && _options.BathyToRovRefOffset.HasValue)
            {
                correctedDepth += _options.BathyToRovRefOffset.Value;
            }
        }

        point.CorrectedDepth = correctedDepth;
    }

    /// <summary>
    /// Calculate the depth for CAD visualization with exaggeration
    /// </summary>
    public double GetExaggeratedDepth(double depth)
    {
        return depth * _options.DepthExaggeration;
    }

    /// <summary>
    /// Validate that tide data covers the survey time range
    /// </summary>
    public TideValidationResult ValidateTideCoverage(IList<SurveyPoint> points)
    {
        var result = new TideValidationResult { IsValid = true };

        if (_tideData == null)
        {
            result.IsValid = false;
            result.Message = "No tide data loaded";
            return result;
        }

        if (points.Count == 0)
        {
            result.IsValid = true;
            result.Message = "No survey points to validate";
            return result;
        }

        var surveyStart = points.Min(p => p.DateTime);
        var surveyEnd = points.Max(p => p.DateTime);

        if (!_tideData.CoversTimeRange(surveyStart, surveyEnd))
        {
            result.IsValid = false;
            result.Message = $"Tide data does not cover full survey period.\n" +
                $"Survey: {surveyStart:yyyy-MM-dd HH:mm} to {surveyEnd:HH:mm}\n" +
                $"Tide: {_tideData.StartTime:yyyy-MM-dd HH:mm} to {_tideData.EndTime:HH:mm}";
            result.SurveyStart = surveyStart;
            result.SurveyEnd = surveyEnd;
            result.TideStart = _tideData.StartTime;
            result.TideEnd = _tideData.EndTime;
        }
        else
        {
            result.Message = "Tide data covers survey period";
            result.SurveyStart = surveyStart;
            result.SurveyEnd = surveyEnd;
            result.TideStart = _tideData.StartTime;
            result.TideEnd = _tideData.EndTime;
        }

        return result;
    }

    /// <summary>
    /// Get tide correction statistics
    /// </summary>
    public TideCorrectionStats GetStatistics(IList<SurveyPoint> points)
    {
        var correctedPoints = points.Where(p => p.TideCorrection.HasValue).ToList();

        if (correctedPoints.Count == 0)
        {
            return new TideCorrectionStats();
        }

        return new TideCorrectionStats
        {
            PointsWithCorrection = correctedPoints.Count,
            MinTideCorrection = correctedPoints.Min(p => p.TideCorrection!.Value),
            MaxTideCorrection = correctedPoints.Max(p => p.TideCorrection!.Value),
            MeanTideCorrection = correctedPoints.Average(p => p.TideCorrection!.Value),
            MinCorrectedDepth = correctedPoints.Where(p => p.CorrectedDepth.HasValue).Min(p => p.CorrectedDepth!.Value),
            MaxCorrectedDepth = correctedPoints.Where(p => p.CorrectedDepth.HasValue).Max(p => p.CorrectedDepth!.Value)
        };
    }
}

/// <summary>
/// Result of applying corrections
/// </summary>
public class CorrectionResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Result of tide validation
/// </summary>
public class TideValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? SurveyStart { get; set; }
    public DateTime? SurveyEnd { get; set; }
    public DateTime? TideStart { get; set; }
    public DateTime? TideEnd { get; set; }
}

/// <summary>
/// Statistics about tide corrections
/// </summary>
public class TideCorrectionStats
{
    public int PointsWithCorrection { get; set; }
    public double MinTideCorrection { get; set; }
    public double MaxTideCorrection { get; set; }
    public double MeanTideCorrection { get; set; }
    public double MinCorrectedDepth { get; set; }
    public double MaxCorrectedDepth { get; set; }
}
