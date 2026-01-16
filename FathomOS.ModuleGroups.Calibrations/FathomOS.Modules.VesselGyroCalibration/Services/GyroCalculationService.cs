using FathomOS.Modules.VesselGyroCalibration.Models;

namespace FathomOS.Modules.VesselGyroCalibration.Services;

/// <summary>
/// Core calculation service for Vessel Gyro Calibration.
/// Implements the C-O calculation logic from GLSIFR0035.
/// </summary>
public class GyroCalculationService
{
    private const double OutlierThreshold = 3.0; // 3-sigma rule

    #region Angle Calculations

    /// <summary>
    /// Normalizes an angle to the 0-360 range
    /// </summary>
    public static double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 360;
        while (angle >= 360) angle -= 360;
        return angle;
    }

    /// <summary>
    /// Calculates C-O (Calculated minus Observed) with wrap-around handling.
    /// Formula: IF (ref - cal < -180) THEN +360
    ///          IF (ref - cal > 180) THEN -360
    /// </summary>
    public static double CalculateCO(double referenceHeading, double calibratedHeading)
    {
        double diff = referenceHeading - calibratedHeading;

        if (diff < -180) return diff + 360;
        if (diff > 180) return diff - 360;
        return diff;
    }

    /// <summary>
    /// Calculates the Z-score (standardized value) for outlier detection
    /// </summary>
    public static double CalculateZScore(double value, double mean, double standardDeviation)
    {
        if (standardDeviation <= 0) return 0;
        return Math.Abs((value - mean) / standardDeviation);
    }

    #endregion

    #region Statistics Calculations

    /// <summary>
    /// Calculates the arithmetic mean of a collection of values
    /// </summary>
    public static double CalculateMean(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return 0;
        return list.Average();
    }

    /// <summary>
    /// Calculates the sample standard deviation (n-1 denominator)
    /// </summary>
    public static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;

        double mean = list.Average();
        double sumSquares = list.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / (list.Count - 1));
    }

    #endregion

    #region Processing Pipeline

    /// <summary>
    /// Processes all data points: calculates C-O values, statistics, and marks outliers
    /// </summary>
    public void ProcessDataPoints(IList<GyroDataPoint> points)
    {
        if (points == null || points.Count == 0) return;

        // Step 1: Calculate all C-O values
        foreach (var point in points)
        {
            point.CO = CalculateCO(point.ReferenceHeading, point.CalibratedHeading);
        }

        // Step 2: Calculate initial statistics (all points)
        var coValues = points.Select(p => p.CO).ToList();
        double mean = CalculateMean(coValues);
        double sd = CalculateStandardDeviation(coValues);

        // Step 3: Calculate Z-scores and mark outliers
        foreach (var point in points)
        {
            point.StandardizedCO = sd > 0 ? CalculateZScore(point.CO, mean, sd) : 0;
            point.IsRejected = point.StandardizedCO > OutlierThreshold;
            point.AcceptedCO = point.IsRejected ? null : point.CO;
        }
    }

    /// <summary>
    /// Calculates the final results from processed data points
    /// </summary>
    public CalibrationResult CalculateResults(IList<GyroDataPoint> points)
    {
        if (points == null || points.Count == 0)
        {
            return new CalibrationResult();
        }

        var accepted = points.Where(p => !p.IsRejected).ToList();
        var acceptedCO = accepted.Select(p => p.CO).ToList();
        var allCO = points.Select(p => p.CO).ToList();

        var result = new CalibrationResult
        {
            // C-O Statistics
            MeanCOAll = CalculateMean(allCO),
            SDCOAll = CalculateStandardDeviation(allCO),
            MeanCOAccepted = acceptedCO.Any() ? CalculateMean(acceptedCO) : 0,
            SDCOAccepted = acceptedCO.Any() ? CalculateStandardDeviation(acceptedCO) : 0,

            // Heading Ranges
            MinReferenceHeading = points.Min(p => p.ReferenceHeading),
            MaxReferenceHeading = points.Max(p => p.ReferenceHeading),
            MinCalibratedHeading = points.Min(p => p.CalibratedHeading),
            MaxCalibratedHeading = points.Max(p => p.CalibratedHeading),

            // C-O Range (accepted only)
            MinCOAccepted = acceptedCO.Any() ? acceptedCO.Min() : 0,
            MaxCOAccepted = acceptedCO.Any() ? acceptedCO.Max() : 0,

            // Time Range
            StartTime = points.Min(p => p.Timestamp),
            EndTime = points.Max(p => p.Timestamp),

            // Counts
            TotalObservations = points.Count,
            RejectedCount = points.Count(p => p.IsRejected)
        };

        return result;
    }

    /// <summary>
    /// Full calculation pipeline with progress reporting
    /// </summary>
    public CalibrationResult Calculate(
        IList<GyroDataPoint> points, 
        CalibrationProject project, 
        QcCriteria criteria,
        IProgress<CalculationProgress>? progress = null)
    {
        progress?.Report(new CalculationProgress { Percent = 20, Message = "Calculating C-O values..." });
        
        // Calculate all C-O values
        foreach (var point in points)
        {
            point.CO = CalculateCO(point.ReferenceHeading, point.CalibratedHeading);
        }
        
        progress?.Report(new CalculationProgress { Percent = 50, Message = "Detecting outliers..." });
        
        // Perform outlier detection with iterations
        var iterations = new List<IterationResult>();
        for (int iter = 1; iter <= criteria.MaxIterations; iter++)
        {
            var accepted = points.Where(p => !p.IsRejected).ToList();
            if (accepted.Count < 3) break;
            
            var coValues = accepted.Select(p => p.CO).ToList();
            double mean = CalculateMean(coValues);
            double sd = CalculateStandardDeviation(coValues);
            double upper = mean + criteria.ZScoreThreshold * sd;
            double lower = mean - criteria.ZScoreThreshold * sd;
            
            int rejected = 0;
            foreach (var point in accepted)
            {
                point.StandardizedCO = sd > 0 ? CalculateZScore(point.CO, mean, sd) : 0;
                if (point.StandardizedCO > criteria.ZScoreThreshold)
                {
                    point.IsRejected = true;
                    point.AcceptedCO = null;
                    rejected++;
                }
                else
                {
                    point.AcceptedCO = point.CO;
                }
            }
            
            iterations.Add(new IterationResult
            {
                IterationNumber = iter,
                PointsInIteration = accepted.Count,
                PointsRejected = rejected,
                MeanCO = mean,
                StdDev = sd,
                UpperLimit = upper,
                LowerLimit = lower,
                IsConverged = rejected == 0
            });
            
            if (rejected == 0) break;
        }
        
        progress?.Report(new CalculationProgress { Percent = 80, Message = "Computing statistics..." });
        
        var result = CalculateResults(points);
        result.Iterations = iterations;
        
        progress?.Report(new CalculationProgress { Percent = 100, Message = "Complete" });
        
        return result;
    }

    #endregion

    #region QC Validation

    /// <summary>
    /// Performs quality control checks on the calibration results
    /// </summary>
    public ValidationResult ValidateResults(CalibrationResult result, CalibrationProject project)
    {
        var validation = new ValidationResult();

        // Check 1: Rejection rate should be < 5%
        validation.Checks.Add(new QcCheck
        {
            Name = "Rejection Rate",
            Description = "Percentage of outliers rejected (should be < 5%)",
            Value = result.RejectionPercentage,
            ThresholdValue = 5.0,
            Unit = "%",
            Status = result.RejectionPercentage < 5.0 ? QcStatus.Pass :
                     result.RejectionPercentage < 10.0 ? QcStatus.Warning : QcStatus.Fail,
            StatusMessage = result.RejectionPercentage < 5.0 ? "Within acceptable limits" :
                           result.RejectionPercentage < 10.0 ? "High rejection rate" : "Excessive rejections"
        });

        // Check 2: Standard deviation should be reasonable (< 0.5°)
        validation.Checks.Add(new QcCheck
        {
            Name = "Standard Deviation",
            Description = "SD of accepted C-O values (should be < 0.5°)",
            Value = result.SDCOAccepted,
            ThresholdValue = 0.5,
            Unit = "°",
            Status = result.SDCOAccepted < 0.5 ? QcStatus.Pass :
                     result.SDCOAccepted < 1.0 ? QcStatus.Warning : QcStatus.Fail,
            StatusMessage = result.SDCOAccepted < 0.5 ? "Good precision" :
                           result.SDCOAccepted < 1.0 ? "Marginal precision" : "Poor precision"
        });

        // Check 3: For verification, mean C-O should be close to 0
        if (project.Purpose == ExercisePurpose.Verification)
        {
            double tolerance = 0.5; // degrees
            double absMean = Math.Abs(result.MeanCOAccepted);
            validation.Checks.Add(new QcCheck
            {
                Name = "Mean C-O (Verification)",
                Description = $"Mean C-O should be ≈0° for verification (tolerance: ±{tolerance}°)",
                Value = result.MeanCOAccepted,
                ThresholdValue = tolerance,
                Unit = "°",
                Status = absMean < tolerance ? QcStatus.Pass :
                         absMean < tolerance * 2 ? QcStatus.Warning : QcStatus.Fail,
                StatusMessage = absMean < tolerance ? "Existing C-O is valid" :
                               absMean < tolerance * 2 ? "C-O may need adjustment" : "C-O requires recalibration"
            });
        }

        // Check 4: Sufficient number of observations (> 100)
        validation.Checks.Add(new QcCheck
        {
            Name = "Observations Count",
            Description = "Minimum number of data points required (> 100)",
            Value = result.TotalObservations,
            ThresholdValue = 100,
            Unit = "points",
            Status = result.TotalObservations > 100 ? QcStatus.Pass :
                     result.TotalObservations > 50 ? QcStatus.Warning : QcStatus.Fail,
            StatusMessage = result.TotalObservations > 100 ? "Sufficient data" :
                           result.TotalObservations > 50 ? "Limited data" : "Insufficient data"
        });

        // Check 5: Heading coverage (should cover significant range)
        double headingCoverage = result.MaxReferenceHeading - result.MinReferenceHeading;
        if (headingCoverage < 0) headingCoverage += 360; // Handle wrap-around
        validation.Checks.Add(new QcCheck
        {
            Name = "Heading Coverage",
            Description = "Range of headings observed (ideally > 90°)",
            Value = headingCoverage,
            ThresholdValue = 90,
            Unit = "°",
            Status = headingCoverage > 90 ? QcStatus.Pass :
                     headingCoverage > 45 ? QcStatus.Warning : QcStatus.Fail,
            StatusMessage = headingCoverage > 90 ? "Good heading coverage" :
                           headingCoverage > 45 ? "Limited heading range" : "Insufficient heading variation"
        });

        // Check 6: Duration of test
        double durationMinutes = result.Duration.TotalMinutes;
        validation.Checks.Add(new QcCheck
        {
            Name = "Test Duration",
            Description = "Duration of calibration test (ideally > 30 mins)",
            Value = durationMinutes,
            ThresholdValue = 30,
            Unit = "mins",
            Status = durationMinutes > 30 ? QcStatus.Pass :
                     durationMinutes > 15 ? QcStatus.Warning : QcStatus.Fail,
            StatusMessage = durationMinutes > 30 ? "Adequate test duration" :
                           durationMinutes > 15 ? "Short test duration" : "Very short test"
        });

        return validation;
    }

    #endregion
}
