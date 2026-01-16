using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for validating USBL observation data and detecting outliers
/// </summary>
public class DataValidationService
{
    /// <summary>
    /// Validation result with details
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public int TotalPoints { get; set; }
        public int ValidPoints { get; set; }
        public int OutlierCount { get; set; }
        public int ExcludedCount { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<int> OutlierIndices { get; set; } = new();
    }

    /// <summary>
    /// Validate observations and detect outliers using IQR method
    /// </summary>
    public ValidationResult ValidateObservations(List<UsblObservation> observations, double outlierThreshold = 1.5)
    {
        var result = new ValidationResult
        {
            TotalPoints = observations.Count,
            ValidPoints = observations.Count(o => !o.IsExcluded)
        };

        if (observations.Count < 3)
        {
            result.Errors.Add("Insufficient data points (minimum 3 required)");
            result.IsValid = false;
            return result;
        }

        // Check for valid coordinates
        var invalidCoords = observations.Where(o => 
            double.IsNaN(o.TransponderEasting) || double.IsInfinity(o.TransponderEasting) ||
            double.IsNaN(o.TransponderNorthing) || double.IsInfinity(o.TransponderNorthing) ||
            double.IsNaN(o.TransponderDepth) || double.IsInfinity(o.TransponderDepth)).ToList();

        if (invalidCoords.Any())
        {
            result.Warnings.Add($"{invalidCoords.Count} points have invalid coordinates");
            foreach (var inv in invalidCoords)
            {
                inv.IsExcluded = true;
            }
        }

        // Detect outliers using IQR method for each dimension
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count < 3)
        {
            result.Errors.Add("Insufficient valid data points after filtering");
            result.IsValid = false;
            return result;
        }

        var eastingOutliers = DetectOutliersIQR(validObs.Select(o => o.TransponderEasting).ToList(), outlierThreshold);
        var northingOutliers = DetectOutliersIQR(validObs.Select(o => o.TransponderNorthing).ToList(), outlierThreshold);
        var depthOutliers = DetectOutliersIQR(validObs.Select(o => o.TransponderDepth).ToList(), outlierThreshold);

        // Mark outliers
        for (int i = 0; i < validObs.Count; i++)
        {
            if (eastingOutliers.Contains(i) || northingOutliers.Contains(i) || depthOutliers.Contains(i))
            {
                result.OutlierIndices.Add(validObs[i].Index);
            }
        }

        result.OutlierCount = result.OutlierIndices.Count;
        result.ExcludedCount = observations.Count(o => o.IsExcluded);

        if (result.OutlierCount > 0)
        {
            result.Warnings.Add($"{result.OutlierCount} potential outliers detected (threshold: {outlierThreshold}× IQR)");
        }

        // Check time gaps
        var timeGaps = CheckTimeGaps(validObs);
        if (timeGaps.Any())
        {
            result.Warnings.AddRange(timeGaps);
        }

        // Check gyro consistency
        var gyroIssues = CheckGyroConsistency(validObs);
        if (gyroIssues.Any())
        {
            result.Warnings.AddRange(gyroIssues);
        }

        result.ValidPoints = observations.Count(o => !o.IsExcluded);
        result.IsValid = result.ValidPoints >= 3 && !result.Errors.Any();

        return result;
    }

    /// <summary>
    /// Detect outliers using Interquartile Range method
    /// </summary>
    private List<int> DetectOutliersIQR(List<double> values, double threshold)
    {
        var outlierIndices = new List<int>();
        if (values.Count < 4) return outlierIndices;

        var sorted = values.OrderBy(v => v).ToList();
        int n = sorted.Count;

        double q1 = sorted[n / 4];
        double q3 = sorted[(3 * n) / 4];
        double iqr = q3 - q1;

        double lowerBound = q1 - threshold * iqr;
        double upperBound = q3 + threshold * iqr;

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] < lowerBound || values[i] > upperBound)
            {
                outlierIndices.Add(i);
            }
        }

        return outlierIndices;
    }

    /// <summary>
    /// Check for significant time gaps in data
    /// </summary>
    private List<string> CheckTimeGaps(List<UsblObservation> observations)
    {
        var warnings = new List<string>();
        if (observations.Count < 2) return warnings;

        var sorted = observations.OrderBy(o => o.DateTime).ToList();
        var intervals = new List<double>();

        for (int i = 1; i < sorted.Count; i++)
        {
            intervals.Add((sorted[i].DateTime - sorted[i - 1].DateTime).TotalSeconds);
        }

        if (!intervals.Any()) return warnings;

        double avgInterval = intervals.Average();
        double maxInterval = intervals.Max();

        // Warn if max gap is more than 5x average
        if (maxInterval > avgInterval * 5 && maxInterval > 60)
        {
            warnings.Add($"Large time gap detected: {maxInterval:F0}s (avg: {avgInterval:F1}s)");
        }

        return warnings;
    }

    /// <summary>
    /// Check gyro heading consistency
    /// </summary>
    private List<string> CheckGyroConsistency(List<UsblObservation> observations)
    {
        var warnings = new List<string>();
        if (observations.Count < 2) return warnings;

        var gyros = observations.Select(o => o.VesselGyro).ToList();
        double avgGyro = gyros.Average();
        double maxDev = gyros.Max(g => Math.Abs(NormalizeAngleDiff(g - avgGyro)));

        // For spin tests, gyro should be relatively stable (within ±5°)
        if (maxDev > 5)
        {
            warnings.Add($"Gyro heading varies by ±{maxDev:F1}° (expected ±5° for spin test)");
        }

        return warnings;
    }

    /// <summary>
    /// Normalize angle difference to -180 to +180
    /// </summary>
    private double NormalizeAngleDiff(double diff)
    {
        while (diff > 180) diff -= 360;
        while (diff < -180) diff += 360;
        return diff;
    }

    /// <summary>
    /// Calculate data quality score (0-100)
    /// </summary>
    public double CalculateQualityScore(List<UsblObservation> observations)
    {
        if (!observations.Any()) return 0;

        double score = 100;
        var validObs = observations.Where(o => !o.IsExcluded).ToList();

        // Penalize for excluded points
        double excludedRatio = (observations.Count - validObs.Count) / (double)observations.Count;
        score -= excludedRatio * 20;

        // Penalize for low point count
        if (validObs.Count < 10) score -= (10 - validObs.Count) * 2;

        // Penalize for high spread
        if (validObs.Count >= 3)
        {
            var eastingSpread = validObs.Max(o => o.TransponderEasting) - validObs.Min(o => o.TransponderEasting);
            var northingSpread = validObs.Max(o => o.TransponderNorthing) - validObs.Min(o => o.TransponderNorthing);
            var spread = Math.Sqrt(eastingSpread * eastingSpread + northingSpread * northingSpread);

            // Penalize if spread > 2m
            if (spread > 2) score -= Math.Min(20, (spread - 2) * 5);
        }

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Detect outliers in observations (wrapper for ValidateObservations for convenience)
    /// </summary>
    public ValidationResult DetectOutliers(List<UsblObservation> observations, double threshold = 1.5)
    {
        return ValidateObservations(observations, threshold);
    }

    /// <summary>
    /// Auto-exclude outliers from observations
    /// </summary>
    public int AutoExcludeOutliers(List<UsblObservation> observations, double threshold = 2.0)
    {
        var result = ValidateObservations(observations, threshold);
        int excluded = 0;

        foreach (var index in result.OutlierIndices)
        {
            var obs = observations.FirstOrDefault(o => o.Index == index);
            if (obs != null && !obs.IsExcluded)
            {
                obs.IsExcluded = true;
                excluded++;
            }
        }

        return excluded;
    }
}
