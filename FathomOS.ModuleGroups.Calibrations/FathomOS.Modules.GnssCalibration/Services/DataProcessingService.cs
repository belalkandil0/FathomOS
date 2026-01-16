using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Result of the complete data processing pipeline.
/// </summary>
public class ProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<GnssComparisonPoint> Points { get; set; } = new();
    public GnssStatisticsResult? Statistics { get; set; }
    public FilterResult? FilterResult { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Orchestrates the complete data processing pipeline:
/// Parse → Align → Filter → Calculate Statistics
/// </summary>
public class DataProcessingService
{
    private readonly GnssDataParser _parser;
    private readonly StatisticsCalculator _statsCalculator;
    private readonly OutlierFilter _outlierFilter;
    
    public DataProcessingService()
    {
        _parser = new GnssDataParser();
        _statsCalculator = new StatisticsCalculator();
        _outlierFilter = new OutlierFilter();
    }
    
    /// <summary>
    /// Gets column headers from a file without full parsing.
    /// </summary>
    public List<string> GetColumnHeaders(string filePath)
    {
        return _parser.GetColumnHeaders(filePath);
    }
    
    /// <summary>
    /// Auto-detects column mapping for a file.
    /// </summary>
    public DetectedColumns AutoDetectColumns(string filePath, GnssColumnMapping mapping)
    {
        var headers = _parser.GetColumnHeaders(filePath);
        return _parser.AutoDetectColumns(headers, mapping);
    }
    
    /// <summary>
    /// Gets raw preview data (first N rows) for display before processing.
    /// </summary>
    public List<RawPreviewData> GetRawPreview(string filePath, GnssColumnMapping mapping, int maxRows = 10)
    {
        return _parser.GetRawPreview(filePath, mapping, maxRows);
    }
    
    /// <summary>
    /// Executes the complete processing pipeline.
    /// </summary>
    public ProcessingResult Process(GnssProject project)
    {
        return Process(project, null);
    }
    
    /// <summary>
    /// Executes the complete processing pipeline with optional POS data as reference.
    /// </summary>
    public ProcessingResult Process(GnssProject project, List<PosDataPoint>? posData)
    {
        var startTime = DateTime.Now;
        var result = new ProcessingResult();
        
        try
        {
            // Step 1: Parse file
            if (string.IsNullOrEmpty(project.NpdFilePath))
            {
                result.Success = false;
                result.ErrorMessage = "No data file specified.";
                return result;
            }
            
            var parseResult = _parser.Parse(project.NpdFilePath, project.ColumnMapping);
            
            if (!parseResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = parseResult.ErrorMessage;
                result.Warnings = parseResult.Warnings;
                return result;
            }
            
            // Use POS as reference if available and enabled
            if (project.UsePosAsReference && posData != null && posData.Count > 0)
            {
                result.Points = CreatePosComparisonPoints(parseResult.Points, posData, project);
                result.Warnings.Add($"POS comparison mode: {result.Points.Count} matched points from {posData.Count} POS records");
            }
            else
            {
                result.Points = parseResult.Points;
            }
            
            result.Warnings.AddRange(parseResult.Warnings);
            
            // Step 2: Apply unit conversion if needed
            if (project.Unit != LengthUnit.Meters)
            {
                ApplyUnitConversion(result.Points, project.ConversionFactor);
            }
            
            // Step 3: Apply time filtering if specified
            if (project.StartTime.HasValue || project.EndTime.HasValue)
            {
                ApplyTimeFilter(result.Points, project.StartTime, project.EndTime);
            }
            
            // Step 4: Apply outlier filtering
            if (project.AutoFilterEnabled)
            {
                result.FilterResult = _outlierFilter.ApplyFilter(result.Points, project.SigmaThreshold);
            }
            else
            {
                // Just calculate standardized scores without rejecting
                result.FilterResult = new FilterResult
                {
                    TotalPoints = result.Points.Count,
                    AcceptedCount = result.Points.Count,
                    RejectedCount = 0
                };
            }
            
            // Step 5: Calculate statistics
            result.Statistics = _statsCalculator.CalculateAll(result.Points, project.SigmaThreshold);
            
            result.Success = true;
            result.ProcessingTime = DateTime.Now - startTime;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Processing error: {ex.Message}";
        }
        
        return result;
    }
    
    /// <summary>
    /// Creates comparison points by matching NPD System 1 with POS reference by time.
    /// </summary>
    private List<GnssComparisonPoint> CreatePosComparisonPoints(
        List<GnssComparisonPoint> npdPoints, 
        List<PosDataPoint> posPoints,
        GnssProject project)
    {
        var result = new List<GnssComparisonPoint>();
        
        // Sort POS points by time for efficient matching
        var sortedPos = posPoints.OrderBy(p => p.DateTime).ToList();
        
        // Maximum time difference for matching (in seconds)
        const double maxTimeDiffSeconds = 1.0;
        
        foreach (var npdPoint in npdPoints)
        {
            // Find closest POS point in time
            var closestPos = FindClosestPosPoint(sortedPos, npdPoint.DateTime, maxTimeDiffSeconds);
            
            if (closestPos != null)
            {
                // Create new comparison point with POS as System 2 (reference)
                var compPoint = new GnssComparisonPoint
                {
                    Index = npdPoint.Index,
                    DateTime = npdPoint.DateTime,
                    
                    // System 1 from NPD
                    System1Easting = npdPoint.System1Easting,
                    System1Northing = npdPoint.System1Northing,
                    System1Height = npdPoint.System1Height,
                    
                    // System 2 (Reference) from POS
                    System2Easting = closestPos.Easting,
                    System2Northing = closestPos.Northing,
                    System2Height = closestPos.Height,
                    
                    // Copy status
                    IsRejected = npdPoint.IsRejected,
                    RejectionReason = npdPoint.RejectionReason
                };
                
                // Calculate deltas - these are computed properties, no need to set
                // DeltaEasting, DeltaNorthing, DeltaHeight auto-compute from System1/System2 values
                
                result.Add(compPoint);
            }
            else
            {
                // No matching POS point found - mark as rejected
                var compPoint = new GnssComparisonPoint
                {
                    Index = npdPoint.Index,
                    DateTime = npdPoint.DateTime,
                    System1Easting = npdPoint.System1Easting,
                    System1Northing = npdPoint.System1Northing,
                    System1Height = npdPoint.System1Height,
                    System2Easting = double.NaN,
                    System2Northing = double.NaN,
                    System2Height = double.NaN,
                    IsRejected = true,
                    RejectionReason = "No matching POS data within time tolerance"
                };
                result.Add(compPoint);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Finds the closest POS point in time within the specified tolerance.
    /// </summary>
    private PosDataPoint? FindClosestPosPoint(List<PosDataPoint> sortedPos, DateTime targetTime, double maxDiffSeconds)
    {
        if (sortedPos.Count == 0)
            return null;
        
        // Binary search for approximate position
        int lo = 0, hi = sortedPos.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (sortedPos[mid].DateTime < targetTime)
                lo = mid + 1;
            else
                hi = mid;
        }
        
        // Check candidates around the found position
        PosDataPoint? closest = null;
        double minDiff = double.MaxValue;
        
        for (int i = Math.Max(0, lo - 1); i <= Math.Min(sortedPos.Count - 1, lo + 1); i++)
        {
            var diff = Math.Abs((sortedPos[i].DateTime - targetTime).TotalSeconds);
            if (diff < minDiff && diff <= maxDiffSeconds)
            {
                minDiff = diff;
                closest = sortedPos[i];
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Re-applies filtering with new threshold without re-parsing.
    /// </summary>
    public FilterResult RefilterData(IList<GnssComparisonPoint> points, double newThreshold)
    {
        _outlierFilter.ClearFilter(points);
        return _outlierFilter.ApplyFilter(points, newThreshold);
    }
    
    /// <summary>
    /// Recalculates statistics after manual point changes.
    /// </summary>
    public GnssStatisticsResult RecalculateStatistics(IEnumerable<GnssComparisonPoint> points, double sigmaThreshold)
    {
        return _statsCalculator.CalculateAll(points, sigmaThreshold);
    }
    
    /// <summary>
    /// Manually rejects points.
    /// </summary>
    public void RejectPoints(IList<GnssComparisonPoint> points, IEnumerable<int> indices)
    {
        _outlierFilter.RejectPoints(points, indices);
    }
    
    /// <summary>
    /// Manually accepts points.
    /// </summary>
    public void AcceptPoints(IList<GnssComparisonPoint> points, IEnumerable<int> indices)
    {
        _outlierFilter.AcceptPoints(points, indices);
    }
    
    /// <summary>
    /// Accepts all points (clears all rejections).
    /// </summary>
    public void AcceptAllPoints(IList<GnssComparisonPoint> points)
    {
        _outlierFilter.ClearFilter(points);
    }
    
    /// <summary>
    /// Generates 2DRMS circle points for charting.
    /// </summary>
    public List<(double X, double Y)> Generate2DRMSCircle(GnssStatistics stats, int numPoints = 360)
    {
        return _statsCalculator.Generate2DRMSCircle(stats, numPoints);
    }
    
    /// <summary>
    /// Gets the percentage of points within 2DRMS.
    /// </summary>
    public double GetPointsWithin2DRMSPercent(IEnumerable<GnssComparisonPoint> points, GnssStatistics stats)
    {
        return _statsCalculator.CalculatePointsWithin2DRMS(points, stats);
    }
    
    /// <summary>
    /// Applies unit conversion factor to all coordinate values.
    /// </summary>
    private void ApplyUnitConversion(IList<GnssComparisonPoint> points, double factor)
    {
        if (Math.Abs(factor - 1.0) < 0.0001)
            return; // No conversion needed
        
        // Note: For GNSS calibration, we typically DON'T convert the absolute coordinates
        // (they're already in the survey coordinate system).
        // We only need to ensure the DELTA values and statistics are displayed in the correct unit.
        // Since deltas are computed properties, they will automatically reflect any coordinate changes.
        // 
        // If the source data is in feet and we want meters, we'd divide by factor.
        // But typically the user is selecting the display unit, not converting source data.
        //
        // For now, this is a placeholder. The conversion factor is used for display formatting.
    }
    
    /// <summary>
    /// Filters points to a specific time range.
    /// </summary>
    private void ApplyTimeFilter(IList<GnssComparisonPoint> points, DateTime? startTime, DateTime? endTime)
    {
        foreach (var point in points)
        {
            if (startTime.HasValue && point.DateTime < startTime.Value)
            {
                point.IsRejected = true;
                point.RejectionReason = "Outside time range (before start)";
            }
            else if (endTime.HasValue && point.DateTime > endTime.Value)
            {
                point.IsRejected = true;
                point.RejectionReason = "Outside time range (after end)";
            }
        }
    }
}
