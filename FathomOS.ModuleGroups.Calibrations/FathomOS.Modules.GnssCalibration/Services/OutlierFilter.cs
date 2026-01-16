using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Result of outlier filtering operation.
/// </summary>
public class FilterResult
{
    public int TotalPoints { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public double MeanRadial { get; set; }
    public double StdDevRadial { get; set; }
    public double Threshold { get; set; }
    public List<int> RejectedIndices { get; set; } = new();
}

/// <summary>
/// Implements outlier detection and filtering using sigma-based thresholds.
/// Based on Excel logic: Reject if |Standardized| > Threshold AND |Radial| > Mean
/// </summary>
public class OutlierFilter
{
    /// <summary>
    /// Applies sigma-based filtering to comparison points.
    /// Modifies the IsRejected and StandardizedScore properties in-place.
    /// </summary>
    /// <param name="points">Points to filter (modified in-place)</param>
    /// <param name="sigmaThreshold">Sigma threshold (default 3.0)</param>
    /// <returns>Filter result summary</returns>
    public FilterResult ApplyFilter(IList<GnssComparisonPoint> points, double sigmaThreshold = 3.0)
    {
        var result = new FilterResult
        {
            TotalPoints = points.Count,
            Threshold = sigmaThreshold
        };
        
        if (points.Count < 3)
        {
            // Not enough points for meaningful statistics
            return result;
        }
        
        // Get valid points only
        var validPoints = points.Where(p => p.IsValid).ToList();
        
        if (validPoints.Count < 3)
        {
            return result;
        }
        
        // Calculate radial distance statistics
        var radialDistances = validPoints.Select(p => p.RadialDistance).ToList();
        var meanRadial = radialDistances.Average();
        var stdDevRadial = CalculateStdDev(radialDistances);
        
        result.MeanRadial = meanRadial;
        result.StdDevRadial = stdDevRadial;
        
        // Apply filter to each point
        foreach (var point in points)
        {
            if (!point.IsValid)
            {
                point.IsRejected = true;
                point.RejectionReason = "Invalid coordinates";
                continue;
            }
            
            // Calculate standardized score: |Radial - Mean| / StdDev
            if (stdDevRadial > 0)
            {
                point.StandardizedScore = Math.Abs(point.RadialDistance - meanRadial) / stdDevRadial;
            }
            else
            {
                point.StandardizedScore = 0;
            }
            
            // Apply rejection criteria from Excel:
            // Reject if ABS(Standardized) > Threshold AND ABS(Radial) > Mean
            // The second condition ensures we reject outliers, not good data clustered near mean
            bool exceedsThreshold = Math.Abs(point.StandardizedScore) > sigmaThreshold;
            bool exceedsMean = Math.Abs(point.RadialDistance) > meanRadial;
            
            if (exceedsThreshold && exceedsMean)
            {
                point.IsRejected = true;
                point.RejectionReason = $"Exceeds {sigmaThreshold}σ threshold (Z={point.StandardizedScore:F2})";
                result.RejectedIndices.Add(point.Index);
            }
            else
            {
                point.IsRejected = false;
                point.RejectionReason = null;
            }
        }
        
        result.AcceptedCount = points.Count(p => !p.IsRejected);
        result.RejectedCount = points.Count(p => p.IsRejected);
        
        return result;
    }
    
    /// <summary>
    /// Resets all rejection flags.
    /// </summary>
    public void ClearFilter(IList<GnssComparisonPoint> points)
    {
        foreach (var point in points)
        {
            point.IsRejected = false;
            point.RejectionReason = null;
            point.StandardizedScore = 0;
        }
    }
    
    /// <summary>
    /// Manually rejects specific points by index.
    /// </summary>
    public void RejectPoints(IList<GnssComparisonPoint> points, IEnumerable<int> indices)
    {
        var indexSet = indices.ToHashSet();
        
        foreach (var point in points)
        {
            if (indexSet.Contains(point.Index))
            {
                point.IsRejected = true;
                point.RejectionReason = "Manual rejection";
            }
        }
    }
    
    /// <summary>
    /// Manually accepts specific points by index.
    /// </summary>
    public void AcceptPoints(IList<GnssComparisonPoint> points, IEnumerable<int> indices)
    {
        var indexSet = indices.ToHashSet();
        
        foreach (var point in points)
        {
            if (indexSet.Contains(point.Index))
            {
                point.IsRejected = false;
                point.RejectionReason = null;
            }
        }
    }
    
    /// <summary>
    /// Gets points flagged for potential rejection (high standardized score).
    /// </summary>
    public IEnumerable<GnssComparisonPoint> GetFlaggedPoints(
        IEnumerable<GnssComparisonPoint> points, 
        double minStandardizedScore = 2.0)
    {
        return points.Where(p => p.IsValid && Math.Abs(p.StandardizedScore) > minStandardizedScore)
                     .OrderByDescending(p => Math.Abs(p.StandardizedScore));
    }
    
    /// <summary>
    /// Performs iterative filtering until convergence or max iterations.
    /// Some datasets require multiple passes as statistics change after rejection.
    /// </summary>
    public FilterResult ApplyIterativeFilter(
        IList<GnssComparisonPoint> points, 
        double sigmaThreshold = 3.0,
        int maxIterations = 5)
    {
        FilterResult lastResult = new();
        int previousRejected = -1;
        
        for (int i = 0; i < maxIterations; i++)
        {
            // Clear previous rejections
            ClearFilter(points);
            
            // Apply filter
            lastResult = ApplyFilter(points, sigmaThreshold);
            
            // Check for convergence
            if (lastResult.RejectedCount == previousRejected)
                break;
            
            previousRejected = lastResult.RejectedCount;
        }
        
        return lastResult;
    }
    
    /// <summary>
    /// Calculates standard deviation (population).
    /// </summary>
    private double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        
        if (list.Count < 2)
            return 0;
        
        var mean = list.Average();
        var sumOfSquares = list.Sum(v => Math.Pow(v - mean, 2));
        
        return Math.Sqrt(sumOfSquares / list.Count);
    }
}
