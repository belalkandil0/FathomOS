using FathomOS.Core.Models;

namespace FathomOS.Core.Services;

/// <summary>
/// Smoothing algorithm types
/// </summary>
public enum SmoothingMethod
{
    None,
    MovingAverage,
    SavitzkyGolay,
    SplineFit,
    Gaussian,
    MedianFilter,
    ThresholdBased,
    KalmanFilter
}

/// <summary>
/// Settings for smoothing operations
/// </summary>
public class SmoothingSettings
{
    public SmoothingMethod Method { get; set; } = SmoothingMethod.None;
    
    /// <summary>
    /// Window size for moving average and filters (odd number)
    /// </summary>
    public int WindowSize { get; set; } = 5;
    
    /// <summary>
    /// Polynomial order for Savitzky-Golay (must be less than window size)
    /// </summary>
    public int PolynomialOrder { get; set; } = 2;
    
    /// <summary>
    /// Sigma for Gaussian smoothing
    /// </summary>
    public double GaussianSigma { get; set; } = 1.0;
    
    /// <summary>
    /// Spline tension (0 = cubic spline, 1 = linear)
    /// </summary>
    public double SplineTension { get; set; } = 0.5;
    
    /// <summary>
    /// Threshold for threshold-based smoothing (max distance from median)
    /// </summary>
    public double Threshold { get; set; } = 1.0;
    
    /// <summary>
    /// Whether to smooth Easting
    /// </summary>
    public bool SmoothEasting { get; set; } = true;
    
    /// <summary>
    /// Whether to smooth Northing
    /// </summary>
    public bool SmoothNorthing { get; set; } = true;
    
    /// <summary>
    /// Whether to smooth Depth
    /// </summary>
    public bool SmoothDepth { get; set; } = false;
}

/// <summary>
/// Service for applying various smoothing algorithms to survey data
/// </summary>
public class SmoothingService
{
    private readonly SmoothingOptions? _options;
    
    /// <summary>
    /// Create a smoothing service with default settings
    /// </summary>
    public SmoothingService()
    {
        _options = null;
    }
    
    /// <summary>
    /// Create a smoothing service with specific options
    /// </summary>
    public SmoothingService(SmoothingOptions options)
    {
        _options = options;
    }
    
    /// <summary>
    /// Apply smoothing using the options provided in constructor
    /// </summary>
    public SmoothingResult Smooth(List<SurveyPoint> points)
    {
        var result = new SmoothingResult { TotalPoints = points.Count };
        
        if (_options == null || points.Count < 3)
            return result;
        
        var originalEastings = points.Select(p => p.Easting).ToArray();
        var originalNorthings = points.Select(p => p.Northing).ToArray();
        var originalDepths = points.Select(p => p.SmoothedDepth ?? p.Z ?? 0.0).ToArray();
        var originalAltitudes = points.Select(p => p.SmoothedAltitude ?? p.Altitude ?? 0.0).ToArray();
        
        // Smooth position (Easting/Northing)
        if (_options.SmoothPosition)
        {
            var smoothedEastings = ApplyMethod(originalEastings, _options.PositionMethod, _options.PositionWindowSize, _options.PositionThreshold, _options.ProcessNoise, _options.MeasurementNoise);
            var smoothedNorthings = ApplyMethod(originalNorthings, _options.PositionMethod, _options.PositionWindowSize, _options.PositionThreshold, _options.ProcessNoise, _options.MeasurementNoise);
            
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
        if (_options.SmoothDepth)
        {
            var smoothedDepths = ApplyMethod(originalDepths, _options.DepthMethod, _options.DepthWindowSize, _options.DepthThreshold, _options.ProcessNoise, _options.MeasurementNoise);
            
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
        if (_options.SmoothAltitude)
        {
            var smoothedAltitudes = ApplyMethod(originalAltitudes, _options.DepthMethod, _options.DepthWindowSize, _options.DepthThreshold, _options.ProcessNoise, _options.MeasurementNoise);
            
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

    /// <summary>
    /// Apply smoothing to survey points based on settings
    /// </summary>
    public void ApplySmoothing(IList<SurveyPoint> points, SmoothingSettings settings)
    {
        if (points.Count < 3 || settings.Method == SmoothingMethod.None)
        {
            // No smoothing - copy raw to smoothed
            foreach (var pt in points)
            {
                pt.SmoothedEasting = pt.Easting;
                pt.SmoothedNorthing = pt.Northing;
            }
            return;
        }

        // Extract arrays for smoothing (handle nullable Depth)
        var eastings = points.Select(p => p.Easting).ToArray();
        var northings = points.Select(p => p.Northing).ToArray();
        var depths = points.Select(p => p.Depth ?? 0.0).ToArray();

        double[] smoothedEastings;
        double[] smoothedNorthings;
        double[] smoothedDepths;

        switch (settings.Method)
        {
            case SmoothingMethod.MovingAverage:
                smoothedEastings = settings.SmoothEasting ? MovingAverage(eastings, settings.WindowSize) : eastings;
                smoothedNorthings = settings.SmoothNorthing ? MovingAverage(northings, settings.WindowSize) : northings;
                smoothedDepths = settings.SmoothDepth ? MovingAverage(depths, settings.WindowSize) : depths;
                break;

            case SmoothingMethod.SavitzkyGolay:
                smoothedEastings = settings.SmoothEasting ? SavitzkyGolay(eastings, settings.WindowSize, settings.PolynomialOrder) : eastings;
                smoothedNorthings = settings.SmoothNorthing ? SavitzkyGolay(northings, settings.WindowSize, settings.PolynomialOrder) : northings;
                smoothedDepths = settings.SmoothDepth ? SavitzkyGolay(depths, settings.WindowSize, settings.PolynomialOrder) : depths;
                break;

            case SmoothingMethod.SplineFit:
                smoothedEastings = settings.SmoothEasting ? CubicSpline(eastings, settings.SplineTension) : eastings;
                smoothedNorthings = settings.SmoothNorthing ? CubicSpline(northings, settings.SplineTension) : northings;
                smoothedDepths = settings.SmoothDepth ? CubicSpline(depths, settings.SplineTension) : depths;
                break;

            case SmoothingMethod.Gaussian:
                smoothedEastings = settings.SmoothEasting ? GaussianSmooth(eastings, settings.GaussianSigma) : eastings;
                smoothedNorthings = settings.SmoothNorthing ? GaussianSmooth(northings, settings.GaussianSigma) : northings;
                smoothedDepths = settings.SmoothDepth ? GaussianSmooth(depths, settings.GaussianSigma) : depths;
                break;

            case SmoothingMethod.MedianFilter:
                smoothedEastings = settings.SmoothEasting ? MedianFilter(eastings, settings.WindowSize) : eastings;
                smoothedNorthings = settings.SmoothNorthing ? MedianFilter(northings, settings.WindowSize) : northings;
                smoothedDepths = settings.SmoothDepth ? MedianFilter(depths, settings.WindowSize) : depths;
                break;

            default:
                smoothedEastings = eastings;
                smoothedNorthings = northings;
                smoothedDepths = depths;
                break;
        }

        // Apply smoothed values back to points
        for (int i = 0; i < points.Count; i++)
        {
            points[i].SmoothedEasting = smoothedEastings[i];
            points[i].SmoothedNorthing = smoothedNorthings[i];
            // Note: SmoothedDepth would need to be added to model if needed
        }
    }

    /// <summary>
    /// Simple moving average filter
    /// </summary>
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

    /// <summary>
    /// Savitzky-Golay smoothing filter - preserves peaks better than moving average
    /// </summary>
    public double[] SavitzkyGolay(double[] data, int windowSize, int polyOrder)
    {
        if (data.Length == 0) return data;
        
        // Ensure valid parameters
        windowSize = Math.Max(5, windowSize | 1);
        polyOrder = Math.Min(polyOrder, windowSize - 2);
        polyOrder = Math.Max(2, polyOrder);
        
        int halfWindow = windowSize / 2;
        var result = new double[data.Length];
        
        // Calculate Savitzky-Golay coefficients
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
        var coeffs = new double[windowSize];
        
        // Build Vandermonde matrix
        var A = new double[windowSize, polyOrder + 1];
        for (int i = 0; i < windowSize; i++)
        {
            int x = i - halfWindow;
            for (int j = 0; j <= polyOrder; j++)
            {
                A[i, j] = Math.Pow(x, j);
            }
        }
        
        // Calculate (A'A)^-1 * A' for the smoothing row (zeroth derivative)
        // Simplified: use polynomial least squares to get central coefficients
        // For simplicity, use pre-calculated standard coefficients
        
        // Standard quadratic/cubic Savitzky-Golay coefficients
        if (windowSize == 5 && polyOrder == 2)
        {
            return new double[] { -3, 12, 17, 12, -3 }.Select(x => x / 35.0).ToArray();
        }
        else if (windowSize == 7 && polyOrder == 2)
        {
            return new double[] { -2, 3, 6, 7, 6, 3, -2 }.Select(x => x / 21.0).ToArray();
        }
        else if (windowSize == 9 && polyOrder == 2)
        {
            return new double[] { -21, 14, 39, 54, 59, 54, 39, 14, -21 }.Select(x => x / 231.0).ToArray();
        }
        else if (windowSize == 11 && polyOrder == 2)
        {
            return new double[] { -36, 9, 44, 69, 84, 89, 84, 69, 44, 9, -36 }.Select(x => x / 429.0).ToArray();
        }
        else
        {
            // Fallback to simple weights
            for (int i = 0; i < windowSize; i++)
            {
                int dist = Math.Abs(i - halfWindow);
                coeffs[i] = 1.0 / (1.0 + dist);
            }
            double sum = coeffs.Sum();
            return coeffs.Select(c => c / sum).ToArray();
        }
    }

    /// <summary>
    /// Cubic spline interpolation smoothing
    /// </summary>
    public double[] CubicSpline(double[] data, double tension)
    {
        if (data.Length < 4) return data;
        
        var result = new double[data.Length];
        var x = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();
        
        // Calculate spline coefficients
        var spline = new CubicSplineInterpolator(x, data, tension);
        
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = spline.Interpolate(i);
        }
        
        return result;
    }

    /// <summary>
    /// Gaussian smoothing filter
    /// </summary>
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

    /// <summary>
    /// Median filter - good for removing outliers/spikes
    /// </summary>
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

    /// <summary>
    /// Threshold-based smoothing - only smooths values that exceed a threshold deviation
    /// </summary>
    public double[] ThresholdSmooth(double[] data, double threshold, int windowSize)
    {
        if (data.Length == 0) return data;
        
        windowSize = Math.Max(3, windowSize | 1);
        int halfWindow = windowSize / 2;
        
        var result = new double[data.Length];
        Array.Copy(data, result, data.Length);
        
        for (int i = 0; i < data.Length; i++)
        {
            // Calculate local average
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
                
                // Only smooth if deviation exceeds threshold
                if (deviation > threshold)
                {
                    result[i] = localAvg;
                }
            }
        }
        
        return result;
    }

    /// <summary>
    /// Kalman filter smoothing for optimal estimation
    /// </summary>
    public double[] KalmanSmooth(double[] data, double processNoise, double measurementNoise)
    {
        if (data.Length == 0) return data;
        
        var result = new double[data.Length];
        
        // Initial estimates
        double estimate = data[0];
        double errorEstimate = 1.0;
        
        // Process each measurement
        for (int i = 0; i < data.Length; i++)
        {
            // Prediction step
            double predictedEstimate = estimate;
            double predictedError = errorEstimate + processNoise;
            
            // Update step
            double kalmanGain = predictedError / (predictedError + measurementNoise);
            estimate = predictedEstimate + kalmanGain * (data[i] - predictedEstimate);
            errorEstimate = (1 - kalmanGain) * predictedError;
            
            result[i] = estimate;
        }
        
        // Optional: Backward pass for smoother results (RTS smoother)
        var smoothed = new double[data.Length];
        smoothed[data.Length - 1] = result[data.Length - 1];
        
        for (int i = data.Length - 2; i >= 0; i--)
        {
            double smoothingGain = errorEstimate / (errorEstimate + processNoise);
            smoothed[i] = result[i] + smoothingGain * (smoothed[i + 1] - result[i]);
        }
        
        return smoothed;
    }

    /// <summary>
    /// Calculate smoothing statistics for comparison
    /// </summary>
    public SmoothingStatistics CalculateStatistics(IList<SurveyPoint> points)
    {
        var stats = new SmoothingStatistics();
        
        if (points.Count == 0) return stats;
        
        var eastingDiffs = new List<double>();
        var northingDiffs = new List<double>();
        
        foreach (var pt in points)
        {
            if (pt.SmoothedEasting.HasValue)
                eastingDiffs.Add(pt.SmoothedEasting.Value - pt.Easting);
            if (pt.SmoothedNorthing.HasValue)
                northingDiffs.Add(pt.SmoothedNorthing.Value - pt.Northing);
        }
        
        if (eastingDiffs.Count > 0)
        {
            stats.MeanEastingDiff = eastingDiffs.Average();
            stats.MaxEastingDiff = eastingDiffs.Max(Math.Abs);
            stats.RmsEastingDiff = Math.Sqrt(eastingDiffs.Average(d => d * d));
        }
        
        if (northingDiffs.Count > 0)
        {
            stats.MeanNorthingDiff = northingDiffs.Average();
            stats.MaxNorthingDiff = northingDiffs.Max(Math.Abs);
            stats.RmsNorthingDiff = Math.Sqrt(northingDiffs.Average(d => d * d));
        }
        
        // Calculate total displacement
        var displacements = new List<double>();
        for (int i = 0; i < eastingDiffs.Count && i < northingDiffs.Count; i++)
        {
            displacements.Add(Math.Sqrt(eastingDiffs[i] * eastingDiffs[i] + northingDiffs[i] * northingDiffs[i]));
        }
        
        if (displacements.Count > 0)
        {
            stats.MeanDisplacement = displacements.Average();
            stats.MaxDisplacement = displacements.Max();
            stats.RmsDisplacement = Math.Sqrt(displacements.Average(d => d * d));
        }
        
        return stats;
    }
}

/// <summary>
/// Statistics about smoothing results
/// </summary>
public class SmoothingStatistics
{
    public double MeanEastingDiff { get; set; }
    public double MaxEastingDiff { get; set; }
    public double RmsEastingDiff { get; set; }
    
    public double MeanNorthingDiff { get; set; }
    public double MaxNorthingDiff { get; set; }
    public double RmsNorthingDiff { get; set; }
    
    public double MeanDisplacement { get; set; }
    public double MaxDisplacement { get; set; }
    public double RmsDisplacement { get; set; }
}

/// <summary>
/// Options for smoothing operations with separate settings per data type
/// </summary>
public class SmoothingOptions
{
    public bool SmoothPosition { get; set; }
    public SmoothingMethod PositionMethod { get; set; } = SmoothingMethod.MovingAverage;
    public int PositionWindowSize { get; set; } = 5;
    public double PositionThreshold { get; set; } = 0.5;
    
    public bool SmoothDepth { get; set; }
    public SmoothingMethod DepthMethod { get; set; } = SmoothingMethod.MovingAverage;
    public int DepthWindowSize { get; set; } = 5;
    public double DepthThreshold { get; set; } = 0.1;
    
    public bool SmoothAltitude { get; set; }
    
    public double ProcessNoise { get; set; } = 0.01;
    public double MeasurementNoise { get; set; } = 0.1;
}

/// <summary>
/// Result of a smoothing operation
/// </summary>
public class SmoothingResult
{
    public int TotalPoints { get; set; }
    public int PositionPointsModified { get; set; }
    public double MaxPositionCorrection { get; set; }
    public int DepthPointsModified { get; set; }
    public double MaxDepthCorrection { get; set; }
    public int AltitudePointsModified { get; set; }
    public double MaxAltitudeCorrection { get; set; }
    public int SpikesRemoved { get; set; }
    public List<int> ModifiedPointIndices { get; set; } = new();
}

/// <summary>
/// Cubic spline interpolator with tension control
/// </summary>
internal class CubicSplineInterpolator
{
    private readonly double[] _x;
    private readonly double[] _y;
    private readonly double[] _a;
    private readonly double[] _b;
    private readonly double[] _c;
    private readonly double[] _d;
    private readonly double _tension;

    public CubicSplineInterpolator(double[] x, double[] y, double tension)
    {
        _x = x;
        _y = y;
        _tension = Math.Max(0, Math.Min(1, tension));
        
        int n = x.Length;
        _a = new double[n];
        _b = new double[n];
        _c = new double[n];
        _d = new double[n];
        
        if (n < 2) return;
        
        // Copy y values to a
        for (int i = 0; i < n; i++)
            _a[i] = y[i];
        
        if (n == 2)
        {
            // Linear interpolation
            _b[0] = (y[1] - y[0]) / (x[1] - x[0]);
            return;
        }
        
        // Build tridiagonal system for natural cubic spline
        var h = new double[n - 1];
        var alpha = new double[n - 1];
        
        for (int i = 0; i < n - 1; i++)
            h[i] = x[i + 1] - x[i];
        
        for (int i = 1; i < n - 1; i++)
            alpha[i] = 3.0 / h[i] * (y[i + 1] - y[i]) - 3.0 / h[i - 1] * (y[i] - y[i - 1]);
        
        var l = new double[n];
        var mu = new double[n];
        var z = new double[n];
        
        l[0] = 1;
        
        for (int i = 1; i < n - 1; i++)
        {
            l[i] = 2 * (x[i + 1] - x[i - 1]) - h[i - 1] * mu[i - 1];
            mu[i] = h[i] / l[i];
            z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i];
        }
        
        l[n - 1] = 1;
        
        for (int j = n - 2; j >= 0; j--)
        {
            _c[j] = z[j] - mu[j] * _c[j + 1];
            _b[j] = (y[j + 1] - y[j]) / h[j] - h[j] * (_c[j + 1] + 2 * _c[j]) / 3;
            _d[j] = (_c[j + 1] - _c[j]) / (3 * h[j]);
        }
        
        // Apply tension (blend towards linear)
        if (_tension > 0)
        {
            for (int i = 0; i < n - 1; i++)
            {
                double linear_b = (y[i + 1] - y[i]) / h[i];
                _b[i] = _b[i] * (1 - _tension) + linear_b * _tension;
                _c[i] *= (1 - _tension);
                _d[i] *= (1 - _tension);
            }
        }
    }

    public double Interpolate(double xVal)
    {
        int n = _x.Length;
        if (n == 0) return 0;
        if (n == 1) return _y[0];
        
        // Find segment
        int i = 0;
        for (i = 0; i < n - 1; i++)
        {
            if (xVal <= _x[i + 1]) break;
        }
        i = Math.Min(i, n - 2);
        
        double dx = xVal - _x[i];
        return _a[i] + _b[i] * dx + _c[i] * dx * dx + _d[i] * dx * dx * dx;
    }
}
