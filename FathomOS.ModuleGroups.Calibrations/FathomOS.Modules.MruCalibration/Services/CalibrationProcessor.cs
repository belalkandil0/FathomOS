using FathomOS.Modules.MruCalibration.Models;

namespace FathomOS.Modules.MruCalibration.Services;

/// <summary>
/// Core calibration processing engine
/// Implements the calculation logic from the Excel VBA macros
/// </summary>
public class CalibrationProcessor
{
    #region Public Methods
    
    /// <summary>
    /// Process calibration data - calculate C-O values and apply 3-sigma rejection
    /// </summary>
    public ProcessingResult Process(SensorCalibrationData sensorData, double rejectionThreshold = 3.0)
    {
        var result = new ProcessingResult
        {
            SensorType = sensorData.SensorType,
            RejectionThreshold = rejectionThreshold
        };
        
        if (sensorData.DataPoints.Count == 0)
        {
            result.Errors.Add("No data points to process");
            return result;
        }
        
        try
        {
            var points = sensorData.DataPoints;
            
            // Step 1: Calculate initial C-O for all points
            CalculateInitialCO(points);
            result.Steps.Add("Calculated C-O values for all points");
            
            // Step 2: Calculate initial statistics (all points)
            var initialStats = CalculateStatistics(points, includeRejected: true);
            result.InitialMean = initialStats.Mean;
            result.InitialStdDev = initialStats.StdDev;
            result.Steps.Add($"Initial statistics: Mean={initialStats.Mean:F4}, SD={initialStats.StdDev:F4}");
            
            // Step 3: Calculate Z-scores and apply rejection
            int iterations = 0;
            int maxIterations = 10; // Prevent infinite loops
            bool changed = true;
            
            while (changed && iterations < maxIterations)
            {
                iterations++;
                changed = ApplyRejection(points, rejectionThreshold);
                
                if (changed)
                {
                    // Recalculate statistics with accepted points only
                    var stats = CalculateStatistics(points, includeRejected: false);
                    result.Steps.Add($"Iteration {iterations}: Recalculated stats, Mean={stats.Mean:F4}, SD={stats.StdDev:F4}");
                }
            }
            
            result.Iterations = iterations;
            result.Steps.Add($"Rejection completed after {iterations} iteration(s)");
            
            // Step 4: Calculate final statistics
            var finalStats = CalculateFinalStatistics(points);
            sensorData.Statistics = finalStats;
            
            // Update sensor data state
            sensorData.IsProcessed = true;
            
            // Populate result
            result.Success = true;
            result.TotalPoints = points.Count;
            result.AcceptedPoints = points.Count(p => p.Status == PointStatus.Accepted);
            result.RejectedPoints = points.Count(p => p.Status == PointStatus.Rejected);
            result.FinalMean = finalStats.MeanCO_Accepted;
            result.FinalStdDev = finalStats.StdDevCO_Accepted;
            result.Statistics = finalStats;
            
            result.Steps.Add($"Final: {result.AcceptedPoints} accepted, {result.RejectedPoints} rejected ({result.RejectionPercentage:F1}%)");
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Processing failed: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Reprocess with a different rejection threshold
    /// </summary>
    public ProcessingResult Reprocess(SensorCalibrationData sensorData, double newThreshold)
    {
        // Reset all points to pending
        foreach (var point in sensorData.DataPoints)
        {
            point.Status = PointStatus.Pending;
            point.StandardizedCO = 0;
        }
        
        sensorData.IsProcessed = false;
        
        return Process(sensorData, newThreshold);
    }
    
    /// <summary>
    /// Manually toggle rejection status of a point
    /// </summary>
    public void TogglePointRejection(MruDataPoint point)
    {
        point.Status = point.Status == PointStatus.Rejected 
            ? PointStatus.Accepted 
            : PointStatus.Rejected;
    }
    
    /// <summary>
    /// Recalculate statistics after manual changes
    /// </summary>
    public CalibrationStatistics RecalculateStatistics(SensorCalibrationData sensorData)
    {
        var stats = CalculateFinalStatistics(sensorData.DataPoints);
        sensorData.Statistics = stats;
        return stats;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Calculate C-O (Computed - Observed) for all points
    /// Formula: C-O = Reference Value - Verified Value
    /// </summary>
    private void CalculateInitialCO(List<MruDataPoint> points)
    {
        foreach (var point in points)
        {
            point.CO = point.ReferenceValue - point.VerifiedValue;
            point.Status = PointStatus.Pending;
        }
    }
    
    /// <summary>
    /// Calculate mean and standard deviation
    /// </summary>
    private (double Mean, double StdDev, int Count) CalculateStatistics(
        List<MruDataPoint> points, bool includeRejected)
    {
        var validPoints = includeRejected 
            ? points 
            : points.Where(p => p.Status != PointStatus.Rejected).ToList();
        
        if (validPoints.Count == 0)
            return (0, 0, 0);
        
        // Calculate mean
        double mean = validPoints.Average(p => p.CO);
        
        // Calculate standard deviation (sample)
        double variance = 0;
        if (validPoints.Count > 1)
        {
            variance = validPoints.Sum(p => Math.Pow(p.CO - mean, 2)) / (validPoints.Count - 1);
        }
        double stdDev = Math.Sqrt(variance);
        
        return (mean, stdDev, validPoints.Count);
    }
    
    /// <summary>
    /// Apply 3-sigma rejection rule
    /// Returns true if any points were newly rejected
    /// </summary>
    private bool ApplyRejection(List<MruDataPoint> points, double threshold)
    {
        // Get current accepted points for statistics
        var acceptedPoints = points.Where(p => p.Status != PointStatus.Rejected).ToList();
        
        if (acceptedPoints.Count < 3)
            return false; // Need at least 3 points for meaningful statistics
        
        var (mean, stdDev, _) = CalculateStatistics(points, includeRejected: false);
        
        if (stdDev <= 0)
        {
            // All points have same C-O, accept all
            foreach (var point in points.Where(p => p.Status == PointStatus.Pending))
            {
                point.Status = PointStatus.Accepted;
                point.StandardizedCO = 0;
            }
            return false;
        }
        
        bool anyChanged = false;
        
        foreach (var point in points)
        {
            if (point.Status == PointStatus.Rejected)
                continue; // Already rejected, don't reconsider
            
            // Calculate Z-score (standardized C-O)
            point.StandardizedCO = Math.Abs((point.CO - mean) / stdDev);
            
            // Apply rejection threshold
            if (point.StandardizedCO > threshold)
            {
                if (point.Status != PointStatus.Rejected)
                {
                    point.Status = PointStatus.Rejected;
                    anyChanged = true;
                }
            }
            else
            {
                point.Status = PointStatus.Accepted;
            }
        }
        
        return anyChanged;
    }
    
    /// <summary>
    /// Calculate comprehensive final statistics
    /// </summary>
    private CalibrationStatistics CalculateFinalStatistics(List<MruDataPoint> points)
    {
        var stats = new CalibrationStatistics();
        
        if (points.Count == 0)
            return stats;
        
        // All points statistics
        var allStats = CalculateStatistics(points, includeRejected: true);
        stats.MeanCO_All = allStats.Mean;
        stats.StdDevCO_All = allStats.StdDev;
        
        // Accepted points only
        var acceptedStats = CalculateStatistics(points, includeRejected: false);
        stats.MeanCO_Accepted = acceptedStats.Mean;
        stats.StdDevCO_Accepted = acceptedStats.StdDev;
        
        // Reference value ranges
        stats.MinReferenceValue = points.Min(p => p.ReferenceValue);
        stats.MaxReferenceValue = points.Max(p => p.ReferenceValue);
        
        // Verified value ranges
        stats.MinVerifiedValue = points.Min(p => p.VerifiedValue);
        stats.MaxVerifiedValue = points.Max(p => p.VerifiedValue);
        
        // C-O ranges and enhanced statistics (accepted only)
        var acceptedPoints = points.Where(p => p.Status == PointStatus.Accepted).ToList();
        if (acceptedPoints.Count > 0)
        {
            var coValues = acceptedPoints.Select(p => p.CO).OrderBy(x => x).ToList();
            
            stats.MinCO = coValues.First();
            stats.MaxCO = coValues.Last();
            
            // RMSE - Root Mean Square Error
            stats.RMSE = Math.Sqrt(acceptedPoints.Average(p => p.CO * p.CO));
            
            // 95% Confidence Interval
            if (acceptedPoints.Count > 1)
            {
                double standardError = stats.StdDevCO_Accepted / Math.Sqrt(acceptedPoints.Count);
                double tValue = GetTValue95(acceptedPoints.Count - 1);
                stats.ConfidenceInterval95_Lower = stats.MeanCO_Accepted - tValue * standardError;
                stats.ConfidenceInterval95_Upper = stats.MeanCO_Accepted + tValue * standardError;
            }
            
            // Median and Quartiles
            stats.Median = GetPercentile(coValues, 50);
            stats.Q1 = GetPercentile(coValues, 25);
            stats.Q3 = GetPercentile(coValues, 75);
            
            // Skewness and Kurtosis
            if (acceptedPoints.Count >= 3 && stats.StdDevCO_Accepted > 0)
            {
                double mean = stats.MeanCO_Accepted;
                double sd = stats.StdDevCO_Accepted;
                int n = acceptedPoints.Count;
                
                // Skewness (Fisher's)
                double sumCubed = acceptedPoints.Sum(p => Math.Pow((p.CO - mean) / sd, 3));
                stats.Skewness = (n / ((n - 1.0) * (n - 2.0))) * sumCubed;
                
                // Kurtosis (excess kurtosis)
                double sumFourth = acceptedPoints.Sum(p => Math.Pow((p.CO - mean) / sd, 4));
                stats.Kurtosis = ((n * (n + 1)) / ((n - 1.0) * (n - 2.0) * (n - 3.0))) * sumFourth
                               - (3 * (n - 1.0) * (n - 1.0)) / ((n - 2.0) * (n - 3.0));
            }
            
            // Shapiro-Wilk test for normality
            var swResult = ShapiroWilkTest(coValues);
            stats.ShapiroWilkW = swResult.W;
            stats.ShapiroWilkPValue = swResult.PValue;
            stats.IsNormallyDistributed = swResult.PValue > 0.05;
        }
        
        // Time range
        var sortedPoints = points.OrderBy(p => p.Timestamp).ToList();
        stats.StartTime = sortedPoints.First().Timestamp;
        stats.EndTime = sortedPoints.Last().Timestamp;
        
        // Counts
        stats.TotalObservations = points.Count;
        stats.AcceptedCount = points.Count(p => p.Status == PointStatus.Accepted);
        stats.RejectedCount = points.Count(p => p.Status == PointStatus.Rejected);
        
        return stats;
    }
    
    /// <summary>
    /// Get t-value for 95% confidence interval
    /// </summary>
    private double GetTValue95(int degreesOfFreedom)
    {
        // t-values for 95% CI (two-tailed, alpha = 0.05)
        double[] tValues = {
            12.706, 4.303, 3.182, 2.776, 2.571, // df 1-5
            2.447, 2.365, 2.306, 2.262, 2.228,  // df 6-10
            2.201, 2.179, 2.160, 2.145, 2.131,  // df 11-15
            2.120, 2.110, 2.101, 2.093, 2.086,  // df 16-20
            2.080, 2.074, 2.069, 2.064, 2.060,  // df 21-25
            2.056, 2.052, 2.048, 2.045, 2.042   // df 26-30
        };
        
        if (degreesOfFreedom <= 0) return 1.96;
        if (degreesOfFreedom <= 30) return tValues[degreesOfFreedom - 1];
        if (degreesOfFreedom <= 60) return 2.000;
        if (degreesOfFreedom <= 120) return 1.980;
        return 1.960; // Normal distribution approximation for large n
    }
    
    /// <summary>
    /// Get percentile from sorted data
    /// </summary>
    private double GetPercentile(List<double> sortedData, double percentile)
    {
        if (sortedData.Count == 0) return 0;
        if (sortedData.Count == 1) return sortedData[0];
        
        double n = (sortedData.Count - 1) * percentile / 100.0;
        int k = (int)Math.Floor(n);
        double d = n - k;
        
        if (k >= sortedData.Count - 1) return sortedData[sortedData.Count - 1];
        
        return sortedData[k] + d * (sortedData[k + 1] - sortedData[k]);
    }
    
    /// <summary>
    /// Simplified Shapiro-Wilk test for normality
    /// Returns (W statistic, approximate p-value)
    /// </summary>
    private (double W, double PValue) ShapiroWilkTest(List<double> data)
    {
        int n = data.Count;
        
        if (n < 3) return (1.0, 1.0); // Not enough data
        if (n > 5000) return (1.0, 1.0); // Too much data for this implementation
        
        // Calculate mean
        double mean = data.Average();
        
        // Calculate SS (sum of squared deviations)
        double ss = data.Sum(x => Math.Pow(x - mean, 2));
        
        if (ss < 1e-10) return (1.0, 1.0); // All values identical
        
        // Sorted data
        var sorted = data.OrderBy(x => x).ToList();
        
        // Calculate W statistic using simplified algorithm
        double b = 0;
        int halfN = n / 2;
        
        for (int i = 0; i < halfN; i++)
        {
            double a = GetShapiroWilkCoefficient(n, i + 1);
            b += a * (sorted[n - 1 - i] - sorted[i]);
        }
        
        double w = (b * b) / ss;
        
        // Approximate p-value using normal transformation
        double pValue = ApproximateShapiroWilkPValue(w, n);
        
        return (w, pValue);
    }
    
    /// <summary>
    /// Get Shapiro-Wilk coefficient (simplified approximation)
    /// </summary>
    private double GetShapiroWilkCoefficient(int n, int i)
    {
        // Simplified approximation for a_i coefficients
        double m = GetExpectedNormalOrderStatistic(n, i);
        double sumMSquared = 0;
        
        for (int j = 1; j <= n; j++)
        {
            sumMSquared += Math.Pow(GetExpectedNormalOrderStatistic(n, j), 2);
        }
        
        return m / Math.Sqrt(sumMSquared);
    }
    
    /// <summary>
    /// Expected value of i-th order statistic from standard normal
    /// </summary>
    private double GetExpectedNormalOrderStatistic(int n, int i)
    {
        // Approximation using inverse normal CDF
        double p = (i - 0.375) / (n + 0.25);
        return NormInv(p);
    }
    
    /// <summary>
    /// Inverse normal CDF (approximation)
    /// </summary>
    private double NormInv(double p)
    {
        // Rational approximation for inverse normal CDF
        if (p <= 0) return -10;
        if (p >= 1) return 10;
        
        double a1 = -3.969683028665376e+01;
        double a2 = 2.209460984245205e+02;
        double a3 = -2.759285104469687e+02;
        double a4 = 1.383577518672690e+02;
        double a5 = -3.066479806614716e+01;
        double a6 = 2.506628277459239e+00;
        
        double b1 = -5.447609879822406e+01;
        double b2 = 1.615858368580409e+02;
        double b3 = -1.556989798598866e+02;
        double b4 = 6.680131188771972e+01;
        double b5 = -1.328068155288572e+01;
        
        double c1 = -7.784894002430293e-03;
        double c2 = -3.223964580411365e-01;
        double c3 = -2.400758277161838e+00;
        double c4 = -2.549732539343734e+00;
        double c5 = 4.374664141464968e+00;
        double c6 = 2.938163982698783e+00;
        
        double d1 = 7.784695709041462e-03;
        double d2 = 3.224671290700398e-01;
        double d3 = 2.445134137142996e+00;
        double d4 = 3.754408661907416e+00;
        
        double pLow = 0.02425;
        double pHigh = 1 - pLow;
        double q, r;
        
        if (p < pLow)
        {
            q = Math.Sqrt(-2 * Math.Log(p));
            return (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                   ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
        else if (p <= pHigh)
        {
            q = p - 0.5;
            r = q * q;
            return (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q /
                   (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
        }
        else
        {
            q = Math.Sqrt(-2 * Math.Log(1 - p));
            return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                    ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
    }
    
    /// <summary>
    /// Approximate p-value for Shapiro-Wilk test
    /// </summary>
    private double ApproximateShapiroWilkPValue(double w, int n)
    {
        // Simple approximation - for more accuracy, would need tables
        if (w >= 1) return 1.0;
        if (w <= 0) return 0.0;
        
        // Transform W to approximately normal
        double logN = Math.Log(n);
        double mu = 0.0038915 * Math.Pow(logN, 3) - 0.083751 * Math.Pow(logN, 2) - 0.31082 * logN - 1.5861;
        double sigma = Math.Exp(0.0030302 * Math.Pow(logN, 2) - 0.082676 * logN - 0.4803);
        
        double logW = Math.Log(1 - w);
        double z = (logW - mu) / sigma;
        
        // Standard normal CDF
        return 1 - NormCdf(z);
    }
    
    /// <summary>
    /// Standard normal CDF
    /// </summary>
    private double NormCdf(double z)
    {
        double a1 = 0.254829592;
        double a2 = -0.284496736;
        double a3 = 1.421413741;
        double a4 = -1.453152027;
        double a5 = 1.061405429;
        double p = 0.3275911;
        
        int sign = z < 0 ? -1 : 1;
        z = Math.Abs(z) / Math.Sqrt(2);
        
        double t = 1.0 / (1.0 + p * z);
        double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-z * z);
        
        return 0.5 * (1.0 + sign * y);
    }
    
    #endregion
}

#region Processing Result

/// <summary>
/// Result of calibration processing
/// </summary>
public class ProcessingResult
{
    public bool Success { get; set; }
    public SensorType SensorType { get; set; }
    public double RejectionThreshold { get; set; }
    
    // Point counts
    public int TotalPoints { get; set; }
    public int AcceptedPoints { get; set; }
    public int RejectedPoints { get; set; }
    public double RejectionPercentage => TotalPoints > 0 
        ? (RejectedPoints * 100.0 / TotalPoints) 
        : 0;
    
    // Statistics
    public double InitialMean { get; set; }
    public double InitialStdDev { get; set; }
    public double FinalMean { get; set; }
    public double FinalStdDev { get; set; }
    public int Iterations { get; set; }
    
    public CalibrationStatistics? Statistics { get; set; }
    
    // Processing log
    public List<string> Steps { get; } = new();
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    // Convenience
    public bool HasErrors => Errors.Count > 0;
    public string Summary => Success 
        ? $"{AcceptedPoints}/{TotalPoints} accepted (Mean C-O: {FinalMean:F4}°)"
        : $"Failed: {string.Join("; ", Errors)}";
}

#endregion
