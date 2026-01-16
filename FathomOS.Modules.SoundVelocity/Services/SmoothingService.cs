using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.SoundVelocity.Models;

namespace FathomOS.Modules.SoundVelocity.Services;

/// <summary>
/// Service for smoothing CTD/SVP data using various algorithms
/// </summary>
public class SmoothingService
{
    /// <summary>
    /// Apply smoothing to data points using the specified method
    /// </summary>
    public List<CtdDataPoint> SmoothData(List<CtdDataPoint> data, SmoothingMethod method, int windowSize)
    {
        if (data.Count < 3 || windowSize < 1)
            return data.Select(p => p.Clone()).ToList();

        return method switch
        {
            SmoothingMethod.MovingAverage => ApplyMovingAverage(data, windowSize),
            SmoothingMethod.Gaussian => ApplyGaussianSmoothing(data, windowSize),
            SmoothingMethod.SavitzkyGolay => ApplySavitzkyGolay(data, windowSize),
            SmoothingMethod.MedianFilter => ApplyMedianFilter(data, windowSize),
            _ => data.Select(p => p.Clone()).ToList()
        };
    }

    /// <summary>
    /// Moving average smoothing
    /// </summary>
    private List<CtdDataPoint> ApplyMovingAverage(List<CtdDataPoint> data, int windowSize)
    {
        var result = new List<CtdDataPoint>();
        int halfWindow = windowSize / 2;

        for (int i = 0; i < data.Count; i++)
        {
            var newPoint = data[i].Clone();
            newPoint.IsSmoothed = true;

            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(data.Count - 1, i + halfWindow);
            int count = end - start + 1;

            double svSum = 0, tempSum = 0, salSum = 0, densSum = 0;
            for (int j = start; j <= end; j++)
            {
                svSum += data[j].SoundVelocity;
                tempSum += data[j].Temperature;
                salSum += data[j].SalinityOrConductivity;
                densSum += data[j].Density;
            }

            newPoint.SoundVelocity = svSum / count;
            newPoint.Temperature = tempSum / count;
            newPoint.SalinityOrConductivity = salSum / count;
            newPoint.Density = densSum / count;

            result.Add(newPoint);
        }

        return result;
    }

    /// <summary>
    /// Gaussian weighted smoothing
    /// </summary>
    private List<CtdDataPoint> ApplyGaussianSmoothing(List<CtdDataPoint> data, int windowSize)
    {
        var result = new List<CtdDataPoint>();
        int halfWindow = windowSize / 2;
        double sigma = windowSize / 6.0; // Standard deviation

        // Precompute Gaussian weights
        var weights = new double[windowSize];
        double weightSum = 0;
        for (int i = 0; i < windowSize; i++)
        {
            double x = i - halfWindow;
            weights[i] = Math.Exp(-(x * x) / (2 * sigma * sigma));
            weightSum += weights[i];
        }
        // Normalize weights
        for (int i = 0; i < windowSize; i++)
            weights[i] /= weightSum;

        for (int i = 0; i < data.Count; i++)
        {
            var newPoint = data[i].Clone();
            newPoint.IsSmoothed = true;

            double svSum = 0, tempSum = 0, salSum = 0, densSum = 0;
            double localWeightSum = 0;

            for (int j = 0; j < windowSize; j++)
            {
                int dataIdx = i - halfWindow + j;
                if (dataIdx < 0 || dataIdx >= data.Count) continue;

                svSum += data[dataIdx].SoundVelocity * weights[j];
                tempSum += data[dataIdx].Temperature * weights[j];
                salSum += data[dataIdx].SalinityOrConductivity * weights[j];
                densSum += data[dataIdx].Density * weights[j];
                localWeightSum += weights[j];
            }

            if (localWeightSum > 0)
            {
                newPoint.SoundVelocity = svSum / localWeightSum;
                newPoint.Temperature = tempSum / localWeightSum;
                newPoint.SalinityOrConductivity = salSum / localWeightSum;
                newPoint.Density = densSum / localWeightSum;
            }

            result.Add(newPoint);
        }

        return result;
    }

    /// <summary>
    /// Savitzky-Golay smoothing (polynomial fitting)
    /// </summary>
    private List<CtdDataPoint> ApplySavitzkyGolay(List<CtdDataPoint> data, int windowSize)
    {
        // Ensure odd window size
        if (windowSize % 2 == 0) windowSize++;
        int halfWindow = windowSize / 2;

        // Precompute Savitzky-Golay coefficients for quadratic polynomial
        var coefficients = ComputeSGCoefficients(windowSize);
        
        var result = new List<CtdDataPoint>();

        for (int i = 0; i < data.Count; i++)
        {
            var newPoint = data[i].Clone();
            newPoint.IsSmoothed = true;

            double svSum = 0, tempSum = 0, salSum = 0, densSum = 0;
            double coeffSum = 0;

            for (int j = 0; j < windowSize; j++)
            {
                int dataIdx = i - halfWindow + j;
                if (dataIdx < 0 || dataIdx >= data.Count) continue;

                svSum += data[dataIdx].SoundVelocity * coefficients[j];
                tempSum += data[dataIdx].Temperature * coefficients[j];
                salSum += data[dataIdx].SalinityOrConductivity * coefficients[j];
                densSum += data[dataIdx].Density * coefficients[j];
                coeffSum += coefficients[j];
            }

            if (Math.Abs(coeffSum) > 1e-10)
            {
                newPoint.SoundVelocity = svSum / coeffSum;
                newPoint.Temperature = tempSum / coeffSum;
                newPoint.SalinityOrConductivity = salSum / coeffSum;
                newPoint.Density = densSum / coeffSum;
            }

            result.Add(newPoint);
        }

        return result;
    }

    private double[] ComputeSGCoefficients(int windowSize)
    {
        // Simplified Savitzky-Golay coefficients for smoothing (quadratic, derivative order 0)
        var coefficients = new double[windowSize];
        int halfWindow = windowSize / 2;
        
        // For smoothing with quadratic polynomial
        double norm = 0;
        for (int i = 0; i < windowSize; i++)
        {
            int m = i - halfWindow;
            // Simplified coefficient calculation
            coefficients[i] = (3 * windowSize * windowSize - 7 - 20 * m * m) / 4.0;
            norm += coefficients[i];
        }

        // Normalize
        for (int i = 0; i < windowSize; i++)
            coefficients[i] /= norm;

        return coefficients;
    }

    /// <summary>
    /// Median filter smoothing (good for spike removal)
    /// </summary>
    private List<CtdDataPoint> ApplyMedianFilter(List<CtdDataPoint> data, int windowSize)
    {
        var result = new List<CtdDataPoint>();
        int halfWindow = windowSize / 2;

        for (int i = 0; i < data.Count; i++)
        {
            var newPoint = data[i].Clone();
            newPoint.IsSmoothed = true;

            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(data.Count - 1, i + halfWindow);

            var svValues = new List<double>();
            var tempValues = new List<double>();
            var salValues = new List<double>();
            var densValues = new List<double>();

            for (int j = start; j <= end; j++)
            {
                svValues.Add(data[j].SoundVelocity);
                tempValues.Add(data[j].Temperature);
                salValues.Add(data[j].SalinityOrConductivity);
                densValues.Add(data[j].Density);
            }

            newPoint.SoundVelocity = GetMedian(svValues);
            newPoint.Temperature = GetMedian(tempValues);
            newPoint.SalinityOrConductivity = GetMedian(salValues);
            newPoint.Density = GetMedian(densValues);

            result.Add(newPoint);
        }

        return result;
    }

    private double GetMedian(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int count = sorted.Count;
        if (count == 0) return 0;
        if (count % 2 == 1)
            return sorted[count / 2];
        return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
    }
}

/// <summary>
/// Available smoothing methods
/// </summary>
public enum SmoothingMethod
{
    None = 0,
    MovingAverage = 1,
    Gaussian = 2,
    SavitzkyGolay = 3,
    MedianFilter = 4
}
