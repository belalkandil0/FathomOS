using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Data smoothing service with multiple filter algorithms
/// </summary>
public class SmoothingService
{
    #region Smoothing Methods
    
    /// <summary>
    /// Apply smoothing to position data
    /// </summary>
    public SmoothingResult ApplySmoothing(
        List<UsblObservation> observations, 
        SmoothingMethod method, 
        SmoothingOptions options)
    {
        var result = new SmoothingResult
        {
            Method = method,
            OriginalCount = observations.Count
        };
        
        var validObs = observations.Where(o => !o.IsExcluded).OrderBy(o => o.DateTime).ToList();
        if (validObs.Count < 3)
        {
            result.Success = false;
            result.Message = "Insufficient data for smoothing (minimum 3 points required)";
            return result;
        }
        
        var eastings = validObs.Select(o => o.TransponderEasting).ToList();
        var northings = validObs.Select(o => o.TransponderNorthing).ToList();
        var depths = validObs.Select(o => o.TransponderDepth).ToList();
        
        List<double> smoothedE, smoothedN, smoothedD;
        
        switch (method)
        {
            case SmoothingMethod.MovingAverage:
                smoothedE = ApplyMovingAverage(eastings, options.WindowSize);
                smoothedN = ApplyMovingAverage(northings, options.WindowSize);
                smoothedD = ApplyMovingAverage(depths, options.WindowSize);
                break;
                
            case SmoothingMethod.Gaussian:
                smoothedE = ApplyGaussianFilter(eastings, options.Sigma);
                smoothedN = ApplyGaussianFilter(northings, options.Sigma);
                smoothedD = ApplyGaussianFilter(depths, options.Sigma);
                break;
                
            case SmoothingMethod.SavitzkyGolay:
                smoothedE = ApplySavitzkyGolay(eastings, options.WindowSize, options.PolynomialOrder);
                smoothedN = ApplySavitzkyGolay(northings, options.WindowSize, options.PolynomialOrder);
                smoothedD = ApplySavitzkyGolay(depths, options.WindowSize, options.PolynomialOrder);
                break;
                
            case SmoothingMethod.MedianFilter:
                smoothedE = ApplyMedianFilter(eastings, options.WindowSize);
                smoothedN = ApplyMedianFilter(northings, options.WindowSize);
                smoothedD = ApplyMedianFilter(depths, options.WindowSize);
                break;
                
            case SmoothingMethod.ExponentialMovingAverage:
                smoothedE = ApplyEMA(eastings, options.Alpha);
                smoothedN = ApplyEMA(northings, options.Alpha);
                smoothedD = ApplyEMA(depths, options.Alpha);
                break;
                
            default:
                smoothedE = eastings;
                smoothedN = northings;
                smoothedD = depths;
                break;
        }
        
        // Apply smoothed values back to observations
        for (int i = 0; i < validObs.Count; i++)
        {
            validObs[i].SmoothedEasting = smoothedE[i];
            validObs[i].SmoothedNorthing = smoothedN[i];
            validObs[i].SmoothedDepth = smoothedD[i];
        }
        
        // Calculate residuals
        var residuals = new List<double>();
        for (int i = 0; i < validObs.Count; i++)
        {
            double residual = Math.Sqrt(
                Math.Pow(eastings[i] - smoothedE[i], 2) +
                Math.Pow(northings[i] - smoothedN[i], 2));
            residuals.Add(residual);
        }
        
        result.Success = true;
        result.SmoothedCount = validObs.Count;
        result.PointsSmoothed = validObs.Count;
        result.MaxResidual = residuals.Max();
        result.MeanResidual = residuals.Average();
        result.RmsResidual = Math.Sqrt(residuals.Average(r => r * r));
        result.Message = $"Applied {method} smoothing to {validObs.Count} points";
        
        return result;
    }
    
    #endregion
    
    #region Moving Average
    
    /// <summary>
    /// Simple moving average filter
    /// </summary>
    public List<double> ApplyMovingAverage(List<double> values, int windowSize)
    {
        if (windowSize < 1) windowSize = 1;
        if (windowSize > values.Count) windowSize = values.Count;
        if (windowSize % 2 == 0) windowSize++; // Ensure odd window
        
        var result = new List<double>(values.Count);
        int halfWindow = windowSize / 2;
        
        for (int i = 0; i < values.Count; i++)
        {
            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(values.Count - 1, i + halfWindow);
            double sum = 0;
            int count = 0;
            
            for (int j = start; j <= end; j++)
            {
                sum += values[j];
                count++;
            }
            
            result.Add(sum / count);
        }
        
        return result;
    }
    
    /// <summary>
    /// Weighted moving average with linear weights
    /// </summary>
    public List<double> ApplyWeightedMovingAverage(List<double> values, int windowSize)
    {
        if (windowSize < 1) windowSize = 1;
        if (windowSize > values.Count) windowSize = values.Count;
        if (windowSize % 2 == 0) windowSize++;
        
        var result = new List<double>(values.Count);
        int halfWindow = windowSize / 2;
        
        for (int i = 0; i < values.Count; i++)
        {
            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(values.Count - 1, i + halfWindow);
            double weightedSum = 0;
            double weightSum = 0;
            
            for (int j = start; j <= end; j++)
            {
                double weight = halfWindow + 1 - Math.Abs(j - i);
                weightedSum += values[j] * weight;
                weightSum += weight;
            }
            
            result.Add(weightedSum / weightSum);
        }
        
        return result;
    }
    
    #endregion
    
    #region Gaussian Filter
    
    /// <summary>
    /// Gaussian smoothing filter
    /// </summary>
    public List<double> ApplyGaussianFilter(List<double> values, double sigma)
    {
        if (sigma <= 0) sigma = 1;
        
        // Kernel radius = 3*sigma (captures 99.7% of Gaussian)
        int radius = (int)Math.Ceiling(3 * sigma);
        int kernelSize = 2 * radius + 1;
        
        // Generate Gaussian kernel
        var kernel = new double[kernelSize];
        double sum = 0;
        for (int i = 0; i < kernelSize; i++)
        {
            double x = i - radius;
            kernel[i] = Math.Exp(-x * x / (2 * sigma * sigma));
            sum += kernel[i];
        }
        
        // Normalize kernel
        for (int i = 0; i < kernelSize; i++)
            kernel[i] /= sum;
        
        // Apply convolution
        var result = new List<double>(values.Count);
        
        for (int i = 0; i < values.Count; i++)
        {
            double smoothed = 0;
            double weightSum = 0;
            
            for (int k = 0; k < kernelSize; k++)
            {
                int j = i + k - radius;
                if (j >= 0 && j < values.Count)
                {
                    smoothed += values[j] * kernel[k];
                    weightSum += kernel[k];
                }
            }
            
            result.Add(smoothed / weightSum);
        }
        
        return result;
    }
    
    #endregion
    
    #region Savitzky-Golay Filter
    
    /// <summary>
    /// Savitzky-Golay polynomial smoothing filter
    /// </summary>
    public List<double> ApplySavitzkyGolay(List<double> values, int windowSize, int polynomialOrder)
    {
        if (windowSize < 3) windowSize = 3;
        if (windowSize > values.Count) windowSize = values.Count;
        if (windowSize % 2 == 0) windowSize++;
        if (polynomialOrder >= windowSize) polynomialOrder = windowSize - 1;
        
        var coefficients = CalculateSavitzkyGolayCoefficients(windowSize, polynomialOrder);
        
        var result = new List<double>(values.Count);
        int halfWindow = windowSize / 2;
        
        for (int i = 0; i < values.Count; i++)
        {
            double smoothed = 0;
            
            for (int j = -halfWindow; j <= halfWindow; j++)
            {
                int idx = i + j;
                if (idx < 0) idx = 0;
                if (idx >= values.Count) idx = values.Count - 1;
                
                smoothed += values[idx] * coefficients[j + halfWindow];
            }
            
            result.Add(smoothed);
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculate Savitzky-Golay coefficients
    /// </summary>
    private double[] CalculateSavitzkyGolayCoefficients(int windowSize, int polyOrder)
    {
        int halfWindow = windowSize / 2;
        var coeffs = new double[windowSize];
        
        // For simplicity, use pre-calculated coefficients for common cases
        // or quadratic approximation
        if (polyOrder == 2 && windowSize == 5)
        {
            coeffs = new double[] { -3, 12, 17, 12, -3 };
            double sum = coeffs.Sum();
            for (int i = 0; i < coeffs.Length; i++) coeffs[i] /= sum;
            return coeffs;
        }
        else if (polyOrder == 2 && windowSize == 7)
        {
            coeffs = new double[] { -2, 3, 6, 7, 6, 3, -2 };
            double sum = coeffs.Sum();
            for (int i = 0; i < coeffs.Length; i++) coeffs[i] /= sum;
            return coeffs;
        }
        else if (polyOrder == 3 && windowSize == 5)
        {
            coeffs = new double[] { -3, 12, 17, 12, -3 };
            double sum = coeffs.Sum();
            for (int i = 0; i < coeffs.Length; i++) coeffs[i] /= sum;
            return coeffs;
        }
        
        // Default: simple moving average coefficients
        for (int i = 0; i < windowSize; i++)
            coeffs[i] = 1.0 / windowSize;
        
        return coeffs;
    }
    
    #endregion
    
    #region Median Filter
    
    /// <summary>
    /// Median filter (good for spike removal)
    /// </summary>
    public List<double> ApplyMedianFilter(List<double> values, int windowSize)
    {
        if (windowSize < 1) windowSize = 1;
        if (windowSize > values.Count) windowSize = values.Count;
        if (windowSize % 2 == 0) windowSize++;
        
        var result = new List<double>(values.Count);
        int halfWindow = windowSize / 2;
        
        for (int i = 0; i < values.Count; i++)
        {
            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(values.Count - 1, i + halfWindow);
            
            var window = values.Skip(start).Take(end - start + 1).OrderBy(v => v).ToList();
            result.Add(window[window.Count / 2]);
        }
        
        return result;
    }
    
    #endregion
    
    #region Exponential Moving Average
    
    /// <summary>
    /// Exponential moving average (EMA)
    /// </summary>
    public List<double> ApplyEMA(List<double> values, double alpha)
    {
        if (alpha <= 0) alpha = 0.1;
        if (alpha >= 1) alpha = 0.9;
        
        var result = new List<double>(values.Count);
        double ema = values.First();
        
        foreach (var value in values)
        {
            ema = alpha * value + (1 - alpha) * ema;
            result.Add(ema);
        }
        
        return result;
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Remove smoothed values from observations
    /// </summary>
    public void ClearSmoothing(List<UsblObservation> observations)
    {
        foreach (var obs in observations)
        {
            obs.SmoothedEasting = null;
            obs.SmoothedNorthing = null;
            obs.SmoothedDepth = null;
        }
    }
    
    /// <summary>
    /// Get recommended smoothing parameters based on data characteristics
    /// </summary>
    public SmoothingOptions GetRecommendedOptions(List<UsblObservation> observations)
    {
        var validObs = observations.Where(o => !o.IsExcluded).OrderBy(o => o.DateTime).ToList();
        
        // Default options
        var options = new SmoothingOptions();
        
        if (validObs.Count < 5)
            return options;
        
        // Calculate average time interval
        var intervals = new List<double>();
        for (int i = 1; i < validObs.Count; i++)
        {
            intervals.Add((validObs[i].DateTime - validObs[i - 1].DateTime).TotalSeconds);
        }
        double avgInterval = intervals.Average();
        
        // Calculate position noise level
        var eastings = validObs.Select(o => o.TransponderEasting).ToList();
        var smoothedE = ApplyMovingAverage(eastings, 5);
        var residuals = eastings.Zip(smoothedE, (e, s) => Math.Abs(e - s)).ToList();
        double noiseLevel = residuals.Average();
        
        // Recommend window size based on noise and sample rate
        // Higher noise -> larger window
        // Lower sample rate -> smaller window
        int recommendedWindow = Math.Max(3, Math.Min(15, (int)(noiseLevel / avgInterval * 10)));
        if (recommendedWindow % 2 == 0) recommendedWindow++;
        
        options.WindowSize = recommendedWindow;
        options.Sigma = recommendedWindow / 3.0;
        options.Alpha = 2.0 / (recommendedWindow + 1);
        
        return options;
    }
    
    #endregion
}

/// <summary>
/// Smoothing method types
/// </summary>
public enum SmoothingMethod
{
    None,
    MovingAverage,
    WeightedMovingAverage,
    Gaussian,
    SavitzkyGolay,
    MedianFilter,
    ExponentialMovingAverage
}

/// <summary>
/// Options for smoothing algorithms
/// </summary>
public class SmoothingOptions
{
    public int WindowSize { get; set; } = 5;       // For MA, S-G, Median
    public double Sigma { get; set; } = 1.5;        // For Gaussian
    public int PolynomialOrder { get; set; } = 2;   // For Savitzky-Golay
    public double Alpha { get; set; } = 0.3;        // For EMA (0-1)
    
    public bool SmoothEasting { get; set; } = true;
    public bool SmoothNorthing { get; set; } = true;
    public bool SmoothDepth { get; set; } = true;
}

/// <summary>
/// Result of smoothing operation
/// </summary>
public class SmoothingResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public SmoothingMethod Method { get; set; }
    
    public int OriginalCount { get; set; }
    public int SmoothedCount { get; set; }
    public int PointsSmoothed { get; set; }  // Number of points that were modified
    
    // Residual statistics
    public double MaxResidual { get; set; }
    public double MeanResidual { get; set; }
    public double RmsResidual { get; set; }
}
