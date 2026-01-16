using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for calculating quality metrics and dashboard data
/// </summary>
public class QualityDashboardService
{
    private readonly DataValidationService _validationService;
    
    public QualityDashboardService()
    {
        _validationService = new DataValidationService();
    }
    
    /// <summary>
    /// Calculate quality metrics for a spin test dataset
    /// </summary>
    public QualityMetrics CalculateMetrics(SpinTestData spinData)
    {
        // Use DisplayName which shows actual heading (e.g., "Spin 45.2°")
        return CalculateMetrics(spinData.Observations, spinData.DisplayName);
    }
    
    /// <summary>
    /// Calculate quality metrics for a transit test dataset
    /// </summary>
    public QualityMetrics CalculateMetrics(TransitTestData transitData)
    {
        return CalculateMetrics(transitData.Observations, transitData.Name);
    }
    
    /// <summary>
    /// Calculate quality metrics for a list of observations
    /// </summary>
    public QualityMetrics CalculateMetrics(List<UsblObservation> observations, string datasetName)
    {
        var metrics = new QualityMetrics
        {
            DatasetName = datasetName,
            TotalPoints = observations.Count,
            ExcludedPoints = observations.Count(o => o.IsExcluded),
            ValidPoints = observations.Count(o => !o.IsExcluded)
        };
        
        if (metrics.ValidPoints == 0)
        {
            metrics.OverallScore = 0;
            return metrics;
        }
        
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        
        // Calculate basic statistics
        metrics.MeanEasting = validObs.Average(o => o.TransponderEasting);
        metrics.MeanNorthing = validObs.Average(o => o.TransponderNorthing);
        
        // Standard deviations
        metrics.StdDevEasting = CalculateStdDev(validObs.Select(o => o.TransponderEasting));
        metrics.StdDevNorthing = CalculateStdDev(validObs.Select(o => o.TransponderNorthing));
        
        // Radial spread
        var radialDistances = validObs.Select(o => 
            Math.Sqrt(Math.Pow(o.TransponderEasting - metrics.MeanEasting, 2) + 
                      Math.Pow(o.TransponderNorthing - metrics.MeanNorthing, 2))).ToList();
        
        metrics.MaxRadialSpread = radialDistances.Max();
        metrics.MinRadialSpread = radialDistances.Min();
        metrics.TwoSigmaRadius = 2 * Math.Sqrt(
            Math.Pow(metrics.StdDevEasting, 2) + Math.Pow(metrics.StdDevNorthing, 2));
        
        // Time analysis
        var timestamps = validObs.OrderBy(o => o.DateTime).Select(o => o.DateTime).ToList();
        if (timestamps.Count >= 2)
        {
            metrics.FirstTimestamp = timestamps.First();
            metrics.LastTimestamp = timestamps.Last();
            
            var avgInterval = (metrics.LastTimestamp.Value - metrics.FirstTimestamp.Value).TotalSeconds / (timestamps.Count - 1);
            metrics.AverageUpdateRate = avgInterval > 0 ? 1.0 / avgInterval : 0;
        }
        
        // Detect outliers
        var outlierResult = _validationService.DetectOutliers(validObs);
        metrics.OutliersDetected = outlierResult.OutlierIndices.Count;
        
        // Calculate component scores
        metrics.PointCountScore = CalculatePointCountScore(metrics.ValidPoints);
        metrics.OutlierScore = CalculateOutlierScore(metrics.OutliersDetected, metrics.ValidPoints);
        metrics.SpreadScore = CalculateSpreadScore(metrics.MaxRadialSpread, metrics.TwoSigmaRadius);
        metrics.ConsistencyScore = CalculateConsistencyScore(metrics.StdDevEasting, metrics.StdDevNorthing);
        
        // Overall score (weighted average)
        metrics.OverallScore = 
            metrics.PointCountScore * 0.2 +
            metrics.OutlierScore * 0.3 +
            metrics.SpreadScore * 0.3 +
            metrics.ConsistencyScore * 0.2;
        
        return metrics;
    }
    
    /// <summary>
    /// Calculate quality metrics for all spin tests
    /// </summary>
    public List<QualityMetrics> CalculateAllSpinMetrics(UsblVerificationProject project)
    {
        var metrics = new List<QualityMetrics>();
        
        foreach (var spin in project.AllSpinTests)
        {
            if (spin.HasData)
            {
                metrics.Add(CalculateMetrics(spin));
            }
        }
        
        return metrics;
    }
    
    /// <summary>
    /// Calculate quality metrics for all transit tests
    /// </summary>
    public List<QualityMetrics> CalculateAllTransitMetrics(UsblVerificationProject project)
    {
        var metrics = new List<QualityMetrics>();
        
        foreach (var transit in project.AllTransitTests)
        {
            if (transit.HasData)
            {
                metrics.Add(CalculateMetrics(transit));
            }
        }
        
        return metrics;
    }
    
    /// <summary>
    /// Calculate overall project quality
    /// </summary>
    public QualityMetrics CalculateOverallQuality(UsblVerificationProject project)
    {
        var allMetrics = CalculateAllSpinMetrics(project)
            .Concat(CalculateAllTransitMetrics(project))
            .ToList();
        
        if (!allMetrics.Any())
        {
            return new QualityMetrics { DatasetName = "Overall", OverallScore = 0 };
        }
        
        return new QualityMetrics
        {
            DatasetName = "Overall",
            TotalPoints = allMetrics.Sum(m => m.TotalPoints),
            ValidPoints = allMetrics.Sum(m => m.ValidPoints),
            ExcludedPoints = allMetrics.Sum(m => m.ExcludedPoints),
            OutliersDetected = allMetrics.Sum(m => m.OutliersDetected),
            OverallScore = allMetrics.Average(m => m.OverallScore),
            PointCountScore = allMetrics.Average(m => m.PointCountScore),
            OutlierScore = allMetrics.Average(m => m.OutlierScore),
            SpreadScore = allMetrics.Average(m => m.SpreadScore),
            ConsistencyScore = allMetrics.Average(m => m.ConsistencyScore)
        };
    }
    
    private double CalculatePointCountScore(int validPoints)
    {
        // Ideal: 20+ points = 100, 10 points = 80, 5 points = 50, <3 = 0
        if (validPoints >= 20) return 100;
        if (validPoints >= 10) return 80 + (validPoints - 10) * 2;
        if (validPoints >= 5) return 50 + (validPoints - 5) * 6;
        if (validPoints >= 3) return validPoints * 16.67;
        return 0;
    }
    
    private double CalculateOutlierScore(int outliers, int total)
    {
        if (total == 0) return 0;
        
        double outlierRatio = (double)outliers / total;
        // 0% outliers = 100, 10% = 70, 20% = 40, 30%+ = 0
        if (outlierRatio == 0) return 100;
        if (outlierRatio <= 0.1) return 100 - (outlierRatio * 300);
        if (outlierRatio <= 0.2) return 70 - ((outlierRatio - 0.1) * 300);
        if (outlierRatio <= 0.3) return 40 - ((outlierRatio - 0.2) * 400);
        return 0;
    }
    
    private double CalculateSpreadScore(double maxSpread, double twoSigma)
    {
        // Compare max spread to expected spread (2σ)
        // If maxSpread <= 2σ = 100, if maxSpread = 3σ = 70, if > 4σ = low
        if (twoSigma <= 0) return 50;
        
        double ratio = maxSpread / twoSigma;
        if (ratio <= 1.0) return 100;
        if (ratio <= 1.5) return 100 - (ratio - 1.0) * 60;
        if (ratio <= 2.0) return 70 - (ratio - 1.5) * 60;
        if (ratio <= 3.0) return 40 - (ratio - 2.0) * 20;
        return Math.Max(0, 20 - (ratio - 3.0) * 10);
    }
    
    private double CalculateConsistencyScore(double stdDevE, double stdDevN)
    {
        // Lower standard deviation = better
        // < 0.1m = 100, 0.2m = 80, 0.5m = 50, > 1m = 20
        double avgStdDev = (stdDevE + stdDevN) / 2;
        
        if (avgStdDev <= 0.1) return 100;
        if (avgStdDev <= 0.2) return 100 - (avgStdDev - 0.1) * 200;
        if (avgStdDev <= 0.5) return 80 - (avgStdDev - 0.2) * 100;
        if (avgStdDev <= 1.0) return 50 - (avgStdDev - 0.5) * 60;
        return Math.Max(0, 20 - (avgStdDev - 1.0) * 10);
    }
    
    private static double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;
        
        double mean = list.Average();
        double sumSquares = list.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / (list.Count - 1));
    }
}
