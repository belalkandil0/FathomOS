using FathomOS.Core.Models;
using FathomOS.Core.Services;

namespace FathomOS.Core.Interfaces;

/// <summary>
/// Unified smoothing service interface providing comprehensive signal processing
/// and data smoothing algorithms for survey data processing.
/// </summary>
public interface ISmoothingService
{
    #region Basic Smoothing Algorithms

    /// <summary>
    /// Apply simple moving average filter to data.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="windowSize">Window size (must be odd, minimum 3).</param>
    /// <returns>Smoothed data array.</returns>
    double[] MovingAverage(double[] data, int windowSize);

    /// <summary>
    /// Apply weighted moving average filter where weights decrease linearly from center.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="windowSize">Window size (must be odd, minimum 3).</param>
    /// <returns>Smoothed data array.</returns>
    double[] WeightedMovingAverage(double[] data, int windowSize);

    /// <summary>
    /// Apply exponential smoothing (single exponential smoothing).
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="alpha">Smoothing factor (0-1). Lower values = more smoothing.</param>
    /// <returns>Smoothed data array.</returns>
    double[] ExponentialSmoothing(double[] data, double alpha);

    #endregion

    #region Advanced Smoothing Algorithms

    /// <summary>
    /// Apply Savitzky-Golay smoothing filter - preserves peaks and valleys better than moving average.
    /// Uses polynomial fitting within the window.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="windowSize">Window size (must be odd, minimum 5).</param>
    /// <param name="polyOrder">Polynomial order (must be less than windowSize).</param>
    /// <returns>Smoothed data array.</returns>
    double[] SavitzkyGolay(double[] data, int windowSize, int polyOrder);

    /// <summary>
    /// Apply median filter - effective for removing outliers/spikes.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="windowSize">Window size (must be odd, minimum 3).</param>
    /// <returns>Filtered data array.</returns>
    double[] MedianFilter(double[] data, int windowSize);

    /// <summary>
    /// Apply Gaussian smoothing filter.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="sigma">Standard deviation of Gaussian kernel.</param>
    /// <returns>Smoothed data array.</returns>
    double[] GaussianSmooth(double[] data, double sigma);

    /// <summary>
    /// Apply Kalman filter smoothing for optimal state estimation.
    /// Uses forward-backward (RTS) smoothing for best results.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="processNoise">Process noise variance (Q).</param>
    /// <param name="measurementNoise">Measurement noise variance (R).</param>
    /// <returns>Smoothed data array.</returns>
    double[] KalmanSmooth(double[] data, double processNoise, double measurementNoise);

    #endregion

    #region Frequency Domain Filters

    /// <summary>
    /// Apply low-pass filter to remove high-frequency noise.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="cutoffFrequency">Cutoff frequency in Hz.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <returns>Filtered data array.</returns>
    double[] LowPassFilter(double[] data, double cutoffFrequency, double sampleRate);

    /// <summary>
    /// Apply high-pass filter to remove low-frequency drift.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="cutoffFrequency">Cutoff frequency in Hz.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <returns>Filtered data array.</returns>
    double[] HighPassFilter(double[] data, double cutoffFrequency, double sampleRate);

    /// <summary>
    /// Apply band-pass filter to isolate specific frequency range.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="lowCutoff">Low cutoff frequency in Hz.</param>
    /// <param name="highCutoff">High cutoff frequency in Hz.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    /// <returns>Filtered data array.</returns>
    double[] BandPassFilter(double[] data, double lowCutoff, double highCutoff, double sampleRate);

    #endregion

    #region Survey-Specific Operations

    /// <summary>
    /// Apply comprehensive smoothing to survey points based on options.
    /// Handles position (Easting/Northing), depth, and altitude separately.
    /// </summary>
    /// <param name="points">Survey points to smooth.</param>
    /// <param name="options">Smoothing configuration options.</param>
    /// <returns>Result containing statistics about modifications.</returns>
    SmoothingResult Smooth(List<SurveyPoint> points, SmoothingOptions options);

    /// <summary>
    /// Apply threshold-based spike detection and removal.
    /// Only smooths values that exceed the threshold deviation from local average.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="threshold">Maximum allowed deviation from local average.</param>
    /// <param name="windowSize">Window size for local average calculation.</param>
    /// <returns>Filtered data array.</returns>
    double[] ThresholdSmooth(double[] data, double threshold, int windowSize);

    /// <summary>
    /// Detect spikes/outliers in data using statistical methods.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="windowSize">Window size for local statistics.</param>
    /// <param name="threshold">Number of standard deviations to consider as spike.</param>
    /// <returns>Array of indices where spikes were detected.</returns>
    int[] DetectSpikes(double[] data, int windowSize, double threshold = 3.0);

    /// <summary>
    /// Remove detected spikes by interpolation.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="spikeIndices">Indices of spikes to remove.</param>
    /// <returns>Data array with spikes replaced by interpolated values.</returns>
    double[] RemoveSpikes(double[] data, int[] spikeIndices);

    #endregion

    #region Interpolation

    /// <summary>
    /// Apply cubic spline interpolation smoothing.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="tension">Spline tension (0 = natural cubic, 1 = linear).</param>
    /// <returns>Smoothed data array.</returns>
    double[] CubicSpline(double[] data, double tension = 0.0);

    /// <summary>
    /// Resample data to new length using interpolation.
    /// </summary>
    /// <param name="data">Input data array.</param>
    /// <param name="newLength">Desired output length.</param>
    /// <returns>Resampled data array.</returns>
    double[] Resample(double[] data, int newLength);

    #endregion
}

/// <summary>
/// Configuration for double exponential smoothing (Holt's method).
/// </summary>
public class DoubleExponentialOptions
{
    /// <summary>
    /// Level smoothing factor (0-1).
    /// </summary>
    public double Alpha { get; set; } = 0.3;

    /// <summary>
    /// Trend smoothing factor (0-1).
    /// </summary>
    public double Beta { get; set; } = 0.1;
}

/// <summary>
/// Configuration for Butterworth filter design.
/// </summary>
public class ButterworthFilterOptions
{
    /// <summary>
    /// Filter order (higher = sharper cutoff).
    /// </summary>
    public int Order { get; set; } = 4;

    /// <summary>
    /// Cutoff frequency in Hz.
    /// </summary>
    public double CutoffFrequency { get; set; }

    /// <summary>
    /// Sample rate in Hz.
    /// </summary>
    public double SampleRate { get; set; }

    /// <summary>
    /// Filter type (lowpass, highpass, bandpass).
    /// </summary>
    public FilterType Type { get; set; } = FilterType.LowPass;
}

/// <summary>
/// Filter type enumeration.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// Low-pass filter - removes high frequencies.
    /// </summary>
    LowPass,

    /// <summary>
    /// High-pass filter - removes low frequencies.
    /// </summary>
    HighPass,

    /// <summary>
    /// Band-pass filter - keeps frequencies in a range.
    /// </summary>
    BandPass,

    /// <summary>
    /// Band-stop (notch) filter - removes frequencies in a range.
    /// </summary>
    BandStop
}
