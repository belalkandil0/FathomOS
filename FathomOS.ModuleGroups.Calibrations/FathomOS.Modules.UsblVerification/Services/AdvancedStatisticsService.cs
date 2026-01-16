using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Advanced statistical analysis service for USBL verification
/// Includes CEP, confidence ellipse, Monte Carlo simulation, and trend analysis
/// </summary>
public class AdvancedStatisticsService
{
    private readonly Random _random = new();
    
    #region Radial Statistics
    
    /// <summary>
    /// Calculate comprehensive radial statistics for a dataset
    /// </summary>
    public RadialStatistics CalculateRadialStatistics(List<UsblObservation> observations, string datasetName)
    {
        var stats = new RadialStatistics { DatasetName = datasetName };
        
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count < 3) return stats;
        
        // Calculate mean position
        stats.MeanEasting = validObs.Average(o => o.TransponderEasting);
        stats.MeanNorthing = validObs.Average(o => o.TransponderNorthing);
        stats.PointCount = validObs.Count;
        
        // Calculate radial distances from mean
        var radialDistances = validObs
            .Select(o => Math.Sqrt(
                Math.Pow(o.TransponderEasting - stats.MeanEasting, 2) +
                Math.Pow(o.TransponderNorthing - stats.MeanNorthing, 2)))
            .OrderBy(d => d)
            .ToList();
        
        // Basic radial stats
        stats.MinRadial = radialDistances.Min();
        stats.MaxRadial = radialDistances.Max();
        stats.MeanRadial = radialDistances.Average();
        stats.MedianRadial = GetMedian(radialDistances);
        
        // Standard deviations
        stats.StdDevEasting = CalculateStdDev(validObs.Select(o => o.TransponderEasting));
        stats.StdDevNorthing = CalculateStdDev(validObs.Select(o => o.TransponderNorthing));
        stats.StdDevRadial = CalculateStdDev(radialDistances);
        
        // 2-sigma radius (95.45% for normal distribution)
        stats.TwoSigmaRadius = 2 * Math.Sqrt(
            Math.Pow(stats.StdDevEasting, 2) + Math.Pow(stats.StdDevNorthing, 2));
        
        // CEP - Circular Error Probable (50% of points within this radius)
        stats.CEP = CalculateCEP(radialDistances);
        
        // R95 - 95% of points within this radius
        stats.R95 = CalculatePercentileRadius(radialDistances, 0.95);
        
        // R99 - 99% of points within this radius
        stats.R99 = CalculatePercentileRadius(radialDistances, 0.99);
        
        // DRMS - Distance Root Mean Square
        stats.DRMS = Math.Sqrt(
            Math.Pow(stats.StdDevEasting, 2) + Math.Pow(stats.StdDevNorthing, 2));
        
        // 2DRMS (often used as 95% confidence in navigation)
        stats.TwoDRMS = 2 * stats.DRMS;
        
        // Confidence ellipse parameters
        var ellipse = CalculateConfidenceEllipse(validObs, 0.95);
        stats.EllipseSemiMajor = ellipse.SemiMajor;
        stats.EllipseSemiMinor = ellipse.SemiMinor;
        stats.EllipseRotation = ellipse.Rotation;
        
        return stats;
    }
    
    /// <summary>
    /// Calculate CEP (Circular Error Probable) - radius containing 50% of points
    /// </summary>
    public double CalculateCEP(List<double> sortedRadialDistances)
    {
        if (sortedRadialDistances.Count == 0) return 0;
        return CalculatePercentileRadius(sortedRadialDistances, 0.50);
    }
    
    /// <summary>
    /// Calculate radius containing specified percentile of points
    /// </summary>
    public double CalculatePercentileRadius(List<double> sortedDistances, double percentile)
    {
        if (sortedDistances.Count == 0) return 0;
        
        var sorted = sortedDistances.OrderBy(d => d).ToList();
        int index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        index = Math.Max(0, Math.Min(index, sorted.Count - 1));
        return sorted[index];
    }
    
    #endregion
    
    #region Confidence Ellipse
    
    /// <summary>
    /// Calculate 2D confidence ellipse parameters using eigenvalue decomposition
    /// </summary>
    public ConfidenceEllipse CalculateConfidenceEllipse(List<UsblObservation> observations, double confidenceLevel = 0.95)
    {
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count < 3)
            return new ConfidenceEllipse();
        
        // Calculate mean
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        // Calculate covariance matrix elements
        double varE = 0, varN = 0, covEN = 0;
        foreach (var obs in validObs)
        {
            double dE = obs.TransponderEasting - meanE;
            double dN = obs.TransponderNorthing - meanN;
            varE += dE * dE;
            varN += dN * dN;
            covEN += dE * dN;
        }
        
        int n = validObs.Count;
        varE /= (n - 1);
        varN /= (n - 1);
        covEN /= (n - 1);
        
        // Eigenvalue decomposition for 2x2 covariance matrix
        // |varE   covEN |
        // |covEN  varN  |
        
        double trace = varE + varN;
        double det = varE * varN - covEN * covEN;
        double discriminant = Math.Sqrt(Math.Max(0, trace * trace / 4 - det));
        
        double eigenvalue1 = trace / 2 + discriminant;
        double eigenvalue2 = trace / 2 - discriminant;
        
        // Ensure eigenvalues are positive
        eigenvalue1 = Math.Max(eigenvalue1, 0.0001);
        eigenvalue2 = Math.Max(eigenvalue2, 0.0001);
        
        // Chi-squared value for confidence level (2 degrees of freedom)
        double chiSquared = GetChiSquared(confidenceLevel, 2);
        
        // Semi-axes lengths
        double semiMajor = Math.Sqrt(chiSquared * eigenvalue1);
        double semiMinor = Math.Sqrt(chiSquared * eigenvalue2);
        
        // Rotation angle (angle of major axis from east)
        double rotation = 0;
        if (Math.Abs(covEN) > 1e-10)
        {
            rotation = 0.5 * Math.Atan2(2 * covEN, varE - varN);
        }
        else if (varE < varN)
        {
            rotation = Math.PI / 2;
        }
        
        return new ConfidenceEllipse
        {
            CenterEasting = meanE,
            CenterNorthing = meanN,
            SemiMajor = semiMajor,
            SemiMinor = semiMinor,
            Rotation = rotation * 180 / Math.PI, // Convert to degrees
            ConfidenceLevel = confidenceLevel
        };
    }
    
    /// <summary>
    /// Generate points for drawing confidence ellipse
    /// </summary>
    public List<(double E, double N)> GenerateEllipsePoints(ConfidenceEllipse ellipse, int pointCount = 100)
    {
        var points = new List<(double, double)>();
        double rotRad = ellipse.Rotation * Math.PI / 180;
        
        for (int i = 0; i <= pointCount; i++)
        {
            double angle = 2 * Math.PI * i / pointCount;
            
            // Point on unrotated ellipse
            double x = ellipse.SemiMajor * Math.Cos(angle);
            double y = ellipse.SemiMinor * Math.Sin(angle);
            
            // Rotate and translate
            double e = ellipse.CenterEasting + x * Math.Cos(rotRad) - y * Math.Sin(rotRad);
            double n = ellipse.CenterNorthing + x * Math.Sin(rotRad) + y * Math.Cos(rotRad);
            
            points.Add((e, n));
        }
        
        return points;
    }
    
    #endregion
    
    #region Monte Carlo Simulation
    
    /// <summary>
    /// Run Monte Carlo simulation to estimate confidence bounds
    /// </summary>
    public MonteCarloResult RunMonteCarloSimulation(
        List<UsblObservation> observations, 
        int iterations = 10000,
        double confidenceLevel = 0.95)
    {
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count < 3)
            return new MonteCarloResult();
        
        // Calculate observed statistics
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        double stdE = CalculateStdDev(validObs.Select(o => o.TransponderEasting));
        double stdN = CalculateStdDev(validObs.Select(o => o.TransponderNorthing));
        
        // Run bootstrap resampling
        var bootstrapMeanEs = new List<double>(iterations);
        var bootstrapMeanNs = new List<double>(iterations);
        var bootstrapCEPs = new List<double>(iterations);
        
        for (int i = 0; i < iterations; i++)
        {
            // Resample with replacement
            var resample = new List<UsblObservation>();
            for (int j = 0; j < validObs.Count; j++)
            {
                int idx = _random.Next(validObs.Count);
                resample.Add(validObs[idx]);
            }
            
            // Calculate statistics for this resample
            double sampleMeanE = resample.Average(o => o.TransponderEasting);
            double sampleMeanN = resample.Average(o => o.TransponderNorthing);
            
            bootstrapMeanEs.Add(sampleMeanE);
            bootstrapMeanNs.Add(sampleMeanN);
            
            // Calculate CEP for resample
            var radials = resample.Select(o => Math.Sqrt(
                Math.Pow(o.TransponderEasting - sampleMeanE, 2) +
                Math.Pow(o.TransponderNorthing - sampleMeanN, 2)))
                .OrderBy(d => d).ToList();
            bootstrapCEPs.Add(CalculateCEP(radials));
        }
        
        // Sort for percentile calculation
        bootstrapMeanEs.Sort();
        bootstrapMeanNs.Sort();
        bootstrapCEPs.Sort();
        
        double alpha = (1 - confidenceLevel) / 2;
        int lowerIdx = (int)(alpha * iterations);
        int upperIdx = (int)((1 - alpha) * iterations) - 1;
        
        return new MonteCarloResult
        {
            Iterations = iterations,
            ConfidenceLevel = confidenceLevel,
            
            MeanEasting = meanE,
            MeanNorthing = meanN,
            
            EastingLowerBound = bootstrapMeanEs[lowerIdx],
            EastingUpperBound = bootstrapMeanEs[upperIdx],
            NorthingLowerBound = bootstrapMeanNs[lowerIdx],
            NorthingUpperBound = bootstrapMeanNs[upperIdx],
            
            CEPMean = bootstrapCEPs.Average(),
            CEPLowerBound = bootstrapCEPs[lowerIdx],
            CEPUpperBound = bootstrapCEPs[upperIdx],
            
            EastingStdError = CalculateStdDev(bootstrapMeanEs),
            NorthingStdError = CalculateStdDev(bootstrapMeanNs)
        };
    }
    
    #endregion
    
    #region Trend Analysis
    
    /// <summary>
    /// Analyze position drift trend over time
    /// </summary>
    public TrendAnalysisResult AnalyzeTrend(List<UsblObservation> observations)
    {
        var validObs = observations.Where(o => !o.IsExcluded)
            .OrderBy(o => o.DateTime)
            .ToList();
        
        if (validObs.Count < 5)
            return new TrendAnalysisResult { HasSufficientData = false };
        
        var result = new TrendAnalysisResult { HasSufficientData = true };
        
        // Convert to relative time in seconds
        var startTime = validObs.First().DateTime;
        var times = validObs.Select(o => (o.DateTime - startTime).TotalSeconds).ToList();
        var eastings = validObs.Select(o => o.TransponderEasting).ToList();
        var northings = validObs.Select(o => o.TransponderNorthing).ToList();
        var depths = validObs.Select(o => o.TransponderDepth).ToList();
        
        // Linear regression for each coordinate
        result.EastingTrend = CalculateLinearTrend(times, eastings);
        result.NorthingTrend = CalculateLinearTrend(times, northings);
        result.DepthTrend = CalculateLinearTrend(times, depths);
        
        // Calculate radial drift rate (combined E/N)
        double radialDriftRate = Math.Sqrt(
            Math.Pow(result.EastingTrend.Slope, 2) +
            Math.Pow(result.NorthingTrend.Slope, 2));
        result.RadialDriftRatePerMinute = radialDriftRate * 60; // Convert to per minute
        
        // Duration
        result.DurationMinutes = times.Last() / 60;
        result.TotalDrift = radialDriftRate * times.Last();
        
        // Determine if drift is significant (>1mm/min threshold)
        result.HasSignificantDrift = result.RadialDriftRatePerMinute > 0.001;
        
        // Drift direction
        result.DriftDirection = Math.Atan2(
            result.NorthingTrend.Slope, 
            result.EastingTrend.Slope) * 180 / Math.PI;
        if (result.DriftDirection < 0) result.DriftDirection += 360;
        
        return result;
    }
    
    /// <summary>
    /// Calculate linear regression trend
    /// </summary>
    public LinearTrend CalculateLinearTrend(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count < 2)
            return new LinearTrend();
        
        int n = x.Count;
        double sumX = x.Sum();
        double sumY = y.Sum();
        double sumXY = x.Zip(y, (a, b) => a * b).Sum();
        double sumX2 = x.Sum(v => v * v);
        double sumY2 = y.Sum(v => v * v);
        
        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;
        
        // R-squared
        double meanY = sumY / n;
        double ssTotal = y.Sum(v => Math.Pow(v - meanY, 2));
        double ssResidual = x.Zip(y, (xi, yi) => Math.Pow(yi - (slope * xi + intercept), 2)).Sum();
        double rSquared = ssTotal > 0 ? 1 - ssResidual / ssTotal : 0;
        
        return new LinearTrend
        {
            Slope = slope,
            Intercept = intercept,
            RSquared = rSquared
        };
    }
    
    #endregion
    
    #region Time Series Data
    
    /// <summary>
    /// Generate time series data for charting
    /// </summary>
    public TimeSeriesData GenerateTimeSeriesData(List<UsblObservation> observations)
    {
        var validObs = observations.Where(o => !o.IsExcluded)
            .OrderBy(o => o.DateTime)
            .ToList();
        
        if (validObs.Count == 0)
            return new TimeSeriesData();
        
        var data = new TimeSeriesData();
        
        // Calculate mean position as reference
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        foreach (var obs in validObs)
        {
            // Offset from mean
            double offsetE = obs.TransponderEasting - meanE;
            double offsetN = obs.TransponderNorthing - meanN;
            double radialOffset = Math.Sqrt(offsetE * offsetE + offsetN * offsetN);
            
            data.Timestamps.Add(obs.DateTime);
            data.EastingOffsets.Add(offsetE);
            data.NorthingOffsets.Add(offsetN);
            data.RadialOffsets.Add(radialOffset);
            data.Depths.Add(obs.TransponderDepth);
            data.GyroReadings.Add(obs.VesselGyro);
        }
        
        return data;
    }
    
    #endregion
    
    #region Histogram Data
    
    /// <summary>
    /// Generate histogram data for error distribution
    /// </summary>
    public HistogramData GenerateHistogram(List<UsblObservation> observations, int binCount = 20)
    {
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count == 0)
            return new HistogramData();
        
        // Calculate radial distances from mean
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        var radialDistances = validObs
            .Select(o => Math.Sqrt(
                Math.Pow(o.TransponderEasting - meanE, 2) +
                Math.Pow(o.TransponderNorthing - meanN, 2)))
            .ToList();
        
        double min = 0; // Start from 0 for radial distances
        double max = radialDistances.Max() * 1.1; // Add 10% margin
        double binWidth = (max - min) / binCount;
        
        var histogram = new HistogramData
        {
            BinCount = binCount,
            BinWidth = binWidth,
            MinValue = min,
            MaxValue = max
        };
        
        // Initialize bins
        for (int i = 0; i < binCount; i++)
        {
            histogram.BinEdges.Add(min + i * binWidth);
            histogram.BinCounts.Add(0);
        }
        histogram.BinEdges.Add(max);
        
        // Count values in each bin
        foreach (var distance in radialDistances)
        {
            int binIndex = (int)((distance - min) / binWidth);
            binIndex = Math.Min(binIndex, binCount - 1);
            histogram.BinCounts[binIndex]++;
        }
        
        // Calculate frequencies
        int total = radialDistances.Count;
        foreach (var count in histogram.BinCounts)
        {
            histogram.BinFrequencies.Add((double)count / total);
        }
        
        return histogram;
    }
    
    #endregion
    
    #region Box Plot Data
    
    /// <summary>
    /// Generate box plot statistics for outlier visualization
    /// </summary>
    public BoxPlotData GenerateBoxPlotData(List<UsblObservation> observations, string datasetName)
    {
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count == 0)
            return new BoxPlotData { DatasetName = datasetName };
        
        // Calculate radial distances
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        var radials = validObs
            .Select(o => Math.Sqrt(
                Math.Pow(o.TransponderEasting - meanE, 2) +
                Math.Pow(o.TransponderNorthing - meanN, 2)))
            .OrderBy(d => d)
            .ToList();
        
        var boxPlot = new BoxPlotData
        {
            DatasetName = datasetName,
            Minimum = radials.First(),
            Maximum = radials.Last(),
            Median = GetMedian(radials),
            Q1 = GetPercentile(radials, 0.25),
            Q3 = GetPercentile(radials, 0.75)
        };
        
        // IQR and whiskers
        boxPlot.IQR = boxPlot.Q3 - boxPlot.Q1;
        boxPlot.LowerWhisker = Math.Max(boxPlot.Minimum, boxPlot.Q1 - 1.5 * boxPlot.IQR);
        boxPlot.UpperWhisker = Math.Min(boxPlot.Maximum, boxPlot.Q3 + 1.5 * boxPlot.IQR);
        
        // Find outliers
        foreach (var (obs, radial) in validObs.Zip(radials))
        {
            if (radial < boxPlot.LowerWhisker || radial > boxPlot.UpperWhisker)
            {
                boxPlot.Outliers.Add((obs.Index, radial));
            }
        }
        
        return boxPlot;
    }
    
    #endregion
    
    #region Polar Plot Data
    
    /// <summary>
    /// Generate polar plot data (position offsets in polar coordinates)
    /// </summary>
    public List<PolarPoint> GeneratePolarPlotData(List<UsblObservation> observations)
    {
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count == 0)
            return new List<PolarPoint>();
        
        // Calculate mean position
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        return validObs.Select(obs =>
        {
            double dE = obs.TransponderEasting - meanE;
            double dN = obs.TransponderNorthing - meanN;
            double radius = Math.Sqrt(dE * dE + dN * dN);
            double angle = Math.Atan2(dN, dE) * 180 / Math.PI;
            if (angle < 0) angle += 360;
            
            return new PolarPoint
            {
                Radius = radius,
                Angle = angle,
                Index = obs.Index,
                DateTime = obs.DateTime,
                VesselHeading = obs.VesselGyro
            };
        }).ToList();
    }
    
    #endregion
    
    #region Helper Methods
    
    private double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;
        
        double mean = list.Average();
        double sumSquares = list.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / (list.Count - 1));
    }
    
    private double GetMedian(List<double> sortedValues)
    {
        if (sortedValues.Count == 0) return 0;
        var sorted = sortedValues.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }
    
    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        var sorted = sortedValues.OrderBy(v => v).ToList();
        double index = percentile * (sorted.Count - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);
        if (lower == upper) return sorted[lower];
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (index - lower);
    }
    
    private double GetChiSquared(double confidence, int degreesOfFreedom)
    {
        // Approximation for chi-squared values (2 DOF)
        // For exact values, would need to use special functions library
        return degreesOfFreedom switch
        {
            2 => confidence switch
            {
                >= 0.99 => 9.21,
                >= 0.95 => 5.991,
                >= 0.90 => 4.605,
                >= 0.80 => 3.219,
                >= 0.50 => 1.386,
                _ => 5.991
            },
            _ => 5.991
        };
    }
    
    #endregion
}
