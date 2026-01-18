using FathomOS.Core.Interfaces;
using FathomOS.Core.Models;

namespace FathomOS.Core.Services;

/// <summary>
/// Comprehensive smoothing service implementation providing various signal processing
/// and data smoothing algorithms for survey data processing.
/// </summary>
public class UnifiedSmoothingService : ISmoothingService
{
    #region Basic Smoothing Algorithms

    /// <inheritdoc />
    public double[] MovingAverage(double[] data, int windowSize)
    {
        if (data.Length == 0) return data;

        // Ensure window size is odd and at least 3
        windowSize = Math.Max(3, windowSize | 1);
        int halfWindow = windowSize / 2;

        var result = new double[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(data.Length - 1, i + halfWindow);
            int count = end - start + 1;

            double sum = 0;
            for (int j = start; j <= end; j++)
                sum += data[j];

            result[i] = sum / count;
        }

        return result;
    }

    /// <inheritdoc />
    public double[] WeightedMovingAverage(double[] data, int windowSize)
    {
        if (data.Length == 0) return data;

        windowSize = Math.Max(3, windowSize | 1);
        int halfWindow = windowSize / 2;

        var result = new double[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            double weightedSum = 0;
            double weightSum = 0;

            for (int j = -halfWindow; j <= halfWindow; j++)
            {
                int idx = i + j;
                if (idx >= 0 && idx < data.Length)
                {
                    // Weight decreases linearly from center
                    double weight = halfWindow - Math.Abs(j) + 1;
                    weightedSum += data[idx] * weight;
                    weightSum += weight;
                }
            }

            result[i] = weightSum > 0 ? weightedSum / weightSum : data[i];
        }

        return result;
    }

    /// <inheritdoc />
    public double[] ExponentialSmoothing(double[] data, double alpha)
    {
        if (data.Length == 0) return data;

        alpha = Math.Clamp(alpha, 0.01, 1.0);
        var result = new double[data.Length];
        result[0] = data[0];

        for (int i = 1; i < data.Length; i++)
        {
            result[i] = alpha * data[i] + (1 - alpha) * result[i - 1];
        }

        return result;
    }

    #endregion

    #region Advanced Smoothing Algorithms

    /// <inheritdoc />
    public double[] SavitzkyGolay(double[] data, int windowSize, int polyOrder)
    {
        if (data.Length == 0) return data;

        windowSize = Math.Max(5, windowSize | 1);
        polyOrder = Math.Clamp(polyOrder, 2, windowSize - 2);

        int halfWindow = windowSize / 2;
        var result = new double[data.Length];

        var coeffs = CalculateSGCoefficients(windowSize, polyOrder);

        for (int i = 0; i < data.Length; i++)
        {
            double sum = 0;
            double coeffSum = 0;

            for (int j = -halfWindow; j <= halfWindow; j++)
            {
                int idx = i + j;
                if (idx >= 0 && idx < data.Length)
                {
                    sum += coeffs[j + halfWindow] * data[idx];
                    coeffSum += coeffs[j + halfWindow];
                }
            }

            result[i] = coeffSum > 0 ? sum / coeffSum * coeffs.Sum() : data[i];
        }

        return result;
    }

    private double[] CalculateSGCoefficients(int windowSize, int polyOrder)
    {
        int halfWindow = windowSize / 2;

        // Standard Savitzky-Golay coefficients for common configurations
        if (windowSize == 5 && polyOrder == 2)
            return new double[] { -3, 12, 17, 12, -3 }.Select(x => x / 35.0).ToArray();
        if (windowSize == 7 && polyOrder == 2)
            return new double[] { -2, 3, 6, 7, 6, 3, -2 }.Select(x => x / 21.0).ToArray();
        if (windowSize == 9 && polyOrder == 2)
            return new double[] { -21, 14, 39, 54, 59, 54, 39, 14, -21 }.Select(x => x / 231.0).ToArray();
        if (windowSize == 11 && polyOrder == 2)
            return new double[] { -36, 9, 44, 69, 84, 89, 84, 69, 44, 9, -36 }.Select(x => x / 429.0).ToArray();

        // Fallback: generate approximation using weighted average
        var coeffs = new double[windowSize];
        for (int i = 0; i < windowSize; i++)
        {
            int dist = Math.Abs(i - halfWindow);
            coeffs[i] = 1.0 / (1.0 + dist);
        }
        double sum = coeffs.Sum();
        return coeffs.Select(c => c / sum).ToArray();
    }

    /// <inheritdoc />
    public double[] MedianFilter(double[] data, int windowSize)
    {
        if (data.Length == 0) return data;

        windowSize = Math.Max(3, windowSize | 1);
        int halfWindow = windowSize / 2;

        var result = new double[data.Length];
        var window = new List<double>(windowSize);

        for (int i = 0; i < data.Length; i++)
        {
            window.Clear();

            for (int j = i - halfWindow; j <= i + halfWindow; j++)
            {
                if (j >= 0 && j < data.Length)
                    window.Add(data[j]);
            }

            window.Sort();
            result[i] = window[window.Count / 2];
        }

        return result;
    }

    /// <inheritdoc />
    public double[] GaussianSmooth(double[] data, double sigma)
    {
        if (data.Length == 0 || sigma <= 0) return data;

        int kernelRadius = (int)Math.Ceiling(3 * sigma);
        int kernelSize = 2 * kernelRadius + 1;

        // Create Gaussian kernel
        var kernel = new double[kernelSize];
        double sum = 0;
        for (int i = 0; i < kernelSize; i++)
        {
            int x = i - kernelRadius;
            kernel[i] = Math.Exp(-(x * x) / (2 * sigma * sigma));
            sum += kernel[i];
        }
        // Normalize
        for (int i = 0; i < kernelSize; i++)
            kernel[i] /= sum;

        // Apply convolution
        var result = new double[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            double value = 0;
            double weightSum = 0;

            for (int j = 0; j < kernelSize; j++)
            {
                int idx = i + j - kernelRadius;
                if (idx >= 0 && idx < data.Length)
                {
                    value += kernel[j] * data[idx];
                    weightSum += kernel[j];
                }
            }

            result[i] = value / weightSum;
        }

        return result;
    }

    /// <inheritdoc />
    public double[] KalmanSmooth(double[] data, double processNoise, double measurementNoise)
    {
        if (data.Length == 0) return data;

        var forward = new double[data.Length];
        var errorEstimates = new double[data.Length];

        // Forward pass
        double estimate = data[0];
        double errorEstimate = 1.0;

        for (int i = 0; i < data.Length; i++)
        {
            // Prediction
            double predictedEstimate = estimate;
            double predictedError = errorEstimate + processNoise;

            // Update
            double kalmanGain = predictedError / (predictedError + measurementNoise);
            estimate = predictedEstimate + kalmanGain * (data[i] - predictedEstimate);
            errorEstimate = (1 - kalmanGain) * predictedError;

            forward[i] = estimate;
            errorEstimates[i] = errorEstimate;
        }

        // Backward pass (RTS smoother)
        var smoothed = new double[data.Length];
        smoothed[data.Length - 1] = forward[data.Length - 1];

        for (int i = data.Length - 2; i >= 0; i--)
        {
            double smoothingGain = errorEstimates[i] / (errorEstimates[i] + processNoise);
            smoothed[i] = forward[i] + smoothingGain * (smoothed[i + 1] - forward[i]);
        }

        return smoothed;
    }

    #endregion

    #region Frequency Domain Filters

    /// <inheritdoc />
    public double[] LowPassFilter(double[] data, double cutoffFrequency, double sampleRate)
    {
        if (data.Length == 0) return data;

        // Use a simple IIR low-pass filter (first-order Butterworth approximation)
        double rc = 1.0 / (2.0 * Math.PI * cutoffFrequency);
        double dt = 1.0 / sampleRate;
        double alpha = dt / (rc + dt);

        var result = new double[data.Length];
        result[0] = data[0];

        for (int i = 1; i < data.Length; i++)
        {
            result[i] = result[i - 1] + alpha * (data[i] - result[i - 1]);
        }

        return result;
    }

    /// <inheritdoc />
    public double[] HighPassFilter(double[] data, double cutoffFrequency, double sampleRate)
    {
        if (data.Length == 0) return data;

        // High-pass = Original - Low-pass
        var lowPass = LowPassFilter(data, cutoffFrequency, sampleRate);
        var result = new double[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = data[i] - lowPass[i];
        }

        return result;
    }

    /// <inheritdoc />
    public double[] BandPassFilter(double[] data, double lowCutoff, double highCutoff, double sampleRate)
    {
        if (data.Length == 0) return data;

        // Band-pass = High-pass then Low-pass
        var highPassed = HighPassFilter(data, lowCutoff, sampleRate);
        return LowPassFilter(highPassed, highCutoff, sampleRate);
    }

    #endregion

    #region Survey-Specific Operations

    /// <inheritdoc />
    public SmoothingResult Smooth(List<SurveyPoint> points, SmoothingOptions options)
    {
        var result = new SmoothingResult { TotalPoints = points.Count };

        if (options == null || points.Count < 3)
            return result;

        var originalEastings = points.Select(p => p.Easting).ToArray();
        var originalNorthings = points.Select(p => p.Northing).ToArray();
        var originalDepths = points.Select(p => p.SmoothedDepth ?? p.Z ?? 0.0).ToArray();
        var originalAltitudes = points.Select(p => p.SmoothedAltitude ?? p.Altitude ?? 0.0).ToArray();

        // Smooth position
        if (options.SmoothPosition)
        {
            var smoothedEastings = ApplyMethod(originalEastings, options.PositionMethod, options.PositionWindowSize, options.PositionThreshold, options.ProcessNoise, options.MeasurementNoise);
            var smoothedNorthings = ApplyMethod(originalNorthings, options.PositionMethod, options.PositionWindowSize, options.PositionThreshold, options.ProcessNoise, options.MeasurementNoise);

            for (int i = 0; i < points.Count; i++)
            {
                double posDiff = Math.Sqrt(Math.Pow(smoothedEastings[i] - originalEastings[i], 2) +
                                           Math.Pow(smoothedNorthings[i] - originalNorthings[i], 2));
                if (posDiff > 0.0001)
                {
                    result.PositionPointsModified++;
                    result.MaxPositionCorrection = Math.Max(result.MaxPositionCorrection, posDiff);
                    if (!result.ModifiedPointIndices.Contains(i))
                        result.ModifiedPointIndices.Add(i);
                }
                points[i].SmoothedEasting = smoothedEastings[i];
                points[i].SmoothedNorthing = smoothedNorthings[i];
            }
        }

        // Smooth depth
        if (options.SmoothDepth)
        {
            var smoothedDepths = ApplyMethod(originalDepths, options.DepthMethod, options.DepthWindowSize, options.DepthThreshold, options.ProcessNoise, options.MeasurementNoise);

            for (int i = 0; i < points.Count; i++)
            {
                double depthDiff = Math.Abs(smoothedDepths[i] - originalDepths[i]);
                if (depthDiff > 0.0001)
                {
                    result.DepthPointsModified++;
                    result.MaxDepthCorrection = Math.Max(result.MaxDepthCorrection, depthDiff);
                    if (!result.ModifiedPointIndices.Contains(i))
                        result.ModifiedPointIndices.Add(i);
                }
                points[i].SmoothedDepth = smoothedDepths[i];
            }
        }

        // Smooth altitude
        if (options.SmoothAltitude)
        {
            var smoothedAltitudes = ApplyMethod(originalAltitudes, options.DepthMethod, options.DepthWindowSize, options.DepthThreshold, options.ProcessNoise, options.MeasurementNoise);

            for (int i = 0; i < points.Count; i++)
            {
                double altDiff = Math.Abs(smoothedAltitudes[i] - originalAltitudes[i]);
                if (altDiff > 0.0001)
                {
                    result.AltitudePointsModified++;
                    result.MaxAltitudeCorrection = Math.Max(result.MaxAltitudeCorrection, altDiff);
                    if (!result.ModifiedPointIndices.Contains(i))
                        result.ModifiedPointIndices.Add(i);
                }
                points[i].SmoothedAltitude = smoothedAltitudes[i];
            }
        }

        result.SpikesRemoved = result.ModifiedPointIndices.Count;
        return result;
    }

    private double[] ApplyMethod(double[] data, SmoothingMethod method, int windowSize, double threshold, double processNoise, double measurementNoise)
    {
        return method switch
        {
            SmoothingMethod.MovingAverage => MovingAverage(data, windowSize),
            SmoothingMethod.SavitzkyGolay => SavitzkyGolay(data, windowSize, 2),
            SmoothingMethod.SplineFit => CubicSpline(data, 0.5),
            SmoothingMethod.Gaussian => GaussianSmooth(data, windowSize / 3.0),
            SmoothingMethod.MedianFilter => MedianFilter(data, windowSize),
            SmoothingMethod.ThresholdBased => ThresholdSmooth(data, threshold, windowSize),
            SmoothingMethod.KalmanFilter => KalmanSmooth(data, processNoise, measurementNoise),
            _ => data
        };
    }

    /// <inheritdoc />
    public double[] ThresholdSmooth(double[] data, double threshold, int windowSize)
    {
        if (data.Length == 0) return data;

        windowSize = Math.Max(3, windowSize | 1);
        int halfWindow = windowSize / 2;

        var result = new double[data.Length];
        Array.Copy(data, result, data.Length);

        for (int i = 0; i < data.Length; i++)
        {
            double sum = 0;
            int count = 0;

            for (int j = i - halfWindow; j <= i + halfWindow; j++)
            {
                if (j >= 0 && j < data.Length && j != i)
                {
                    sum += data[j];
                    count++;
                }
            }

            if (count > 0)
            {
                double localAvg = sum / count;
                double deviation = Math.Abs(data[i] - localAvg);

                if (deviation > threshold)
                {
                    result[i] = localAvg;
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public int[] DetectSpikes(double[] data, int windowSize, double threshold = 3.0)
    {
        if (data.Length < 3) return Array.Empty<int>();

        windowSize = Math.Max(3, windowSize | 1);
        int halfWindow = windowSize / 2;

        var spikes = new List<int>();

        for (int i = 0; i < data.Length; i++)
        {
            var window = new List<double>();

            for (int j = i - halfWindow; j <= i + halfWindow; j++)
            {
                if (j >= 0 && j < data.Length && j != i)
                    window.Add(data[j]);
            }

            if (window.Count > 2)
            {
                double mean = window.Average();
                double stdDev = Math.Sqrt(window.Average(x => Math.Pow(x - mean, 2)));

                if (stdDev > 0 && Math.Abs(data[i] - mean) > threshold * stdDev)
                {
                    spikes.Add(i);
                }
            }
        }

        return spikes.ToArray();
    }

    /// <inheritdoc />
    public double[] RemoveSpikes(double[] data, int[] spikeIndices)
    {
        if (data.Length == 0 || spikeIndices.Length == 0) return data;

        var result = new double[data.Length];
        Array.Copy(data, result, data.Length);

        var spikeSet = new HashSet<int>(spikeIndices);

        foreach (int i in spikeIndices)
        {
            if (i < 0 || i >= data.Length) continue;

            // Find nearest non-spike neighbors
            int left = i - 1;
            while (left >= 0 && spikeSet.Contains(left)) left--;

            int right = i + 1;
            while (right < data.Length && spikeSet.Contains(right)) right++;

            // Interpolate
            if (left >= 0 && right < data.Length)
            {
                double t = (double)(i - left) / (right - left);
                result[i] = data[left] + t * (data[right] - data[left]);
            }
            else if (left >= 0)
            {
                result[i] = data[left];
            }
            else if (right < data.Length)
            {
                result[i] = data[right];
            }
        }

        return result;
    }

    #endregion

    #region Interpolation

    /// <inheritdoc />
    public double[] CubicSpline(double[] data, double tension = 0.0)
    {
        if (data.Length < 4) return data;

        var result = new double[data.Length];
        var x = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();

        var spline = new CubicSplineInterpolator(x, data, tension);

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = spline.Interpolate(i);
        }

        return result;
    }

    /// <inheritdoc />
    public double[] Resample(double[] data, int newLength)
    {
        if (data.Length == 0 || newLength <= 0) return Array.Empty<double>();
        if (newLength == data.Length) return (double[])data.Clone();

        var result = new double[newLength];
        double scale = (double)(data.Length - 1) / (newLength - 1);

        for (int i = 0; i < newLength; i++)
        {
            double srcIndex = i * scale;
            int srcIndexInt = (int)srcIndex;
            double fraction = srcIndex - srcIndexInt;

            if (srcIndexInt >= data.Length - 1)
            {
                result[i] = data[data.Length - 1];
            }
            else
            {
                result[i] = data[srcIndexInt] * (1 - fraction) + data[srcIndexInt + 1] * fraction;
            }
        }

        return result;
    }

    #endregion
}
