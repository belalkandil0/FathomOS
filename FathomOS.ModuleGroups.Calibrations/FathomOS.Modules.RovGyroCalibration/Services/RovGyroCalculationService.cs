using FathomOS.Modules.RovGyroCalibration.Models;

namespace FathomOS.Modules.RovGyroCalibration.Services;

/// <summary>
/// ROV Gyro calculation service implementing geometric corrections.
/// Based on GLSIFR0034_ROV_Gyro_GUIDE.xlsm formulas.
/// </summary>
public class RovGyroCalculationService
{
    private const double ZScoreThreshold = 3.0;
    
    #region Geometric Corrections
    
    /// <summary>
    /// Apply geometric corrections to calculate corrected reference heading.
    /// Pipeline: V → V+D → V+D+θ → C (with facing direction)
    /// </summary>
    public void ApplyGeometricCorrections(List<RovGyroDataPoint> points, RovConfiguration config)
    {
        double D = config.BaselineOffset;
        double theta = config.CalculateBaselineAngle();
        double facingOffset = config.FacingDirectionOffset;
        
        foreach (var point in points)
        {
            // Step 1: V + D (vessel heading + baseline offset)
            point.VPlusD = NormalizeAngle(point.VesselHeading + D);
            
            // Step 2: V + D + θ (apply baseline angle correction)
            point.VPlusDPlusTheta = NormalizeAngle(point.VPlusD + theta);
            
            // Step 3: Apply ROV facing direction to get corrected reference C
            point.CorrectedReference = NormalizeAngle(point.VPlusDPlusTheta + facingOffset);
        }
    }
    
    #endregion
    
    #region C-O Calculation
    
    /// <summary>
    /// Calculate C-O values for all data points.
    /// C-O = CorrectedReference - RovHeading (with 360° wrap-around)
    /// </summary>
    public void CalculateCO(List<RovGyroDataPoint> points)
    {
        foreach (var point in points)
        {
            double diff = point.CorrectedReference - point.RovHeading;
            
            // Apply 360° wrap-around
            if (diff < -180) diff += 360;
            if (diff > 180) diff -= 360;
            
            point.CalculatedCO = diff;
        }
    }
    
    /// <summary>
    /// Full calculation pipeline with progress reporting
    /// </summary>
    public CalibrationResult Calculate(
        List<RovGyroDataPoint> points, 
        RovConfiguration config, 
        QcCriteria criteria,
        IProgress<CalculationProgress>? progress = null)
    {
        progress?.Report(new CalculationProgress { Percent = 10, Message = "Applying geometric corrections..." });
        ApplyGeometricCorrections(points, config);
        
        progress?.Report(new CalculationProgress { Percent = 30, Message = "Calculating C-O values..." });
        CalculateCO(points);
        
        progress?.Report(new CalculationProgress { Percent = 50, Message = "Detecting outliers..." });
        var iterations = PerformOutlierDetection(points, criteria.ZScoreThreshold, criteria.MaxIterations);
        
        progress?.Report(new CalculationProgress { Percent = 80, Message = "Computing statistics..." });
        var result = CalculateResults(points, config);
        result.Iterations = iterations;
        
        progress?.Report(new CalculationProgress { Percent = 100, Message = "Complete" });
        return result;
    }
    
    /// <summary>
    /// Perform outlier detection and return iteration results
    /// </summary>
    private List<IterationResult> PerformOutlierDetection(List<RovGyroDataPoint> points, double zThreshold, int maxIter)
    {
        var iterations = new List<IterationResult>();
        
        for (int iter = 1; iter <= maxIter; iter++)
        {
            var accepted = points.Where(p => p.Status != PointStatus.Rejected).ToList();
            if (accepted.Count < 3) break;
            
            var coValues = accepted.Select(p => p.CalculatedCO).ToList();
            double mean = coValues.Average();
            double stdDev = CalculateStdDev(coValues);
            double upper = mean + zThreshold * stdDev;
            double lower = mean - zThreshold * stdDev;
            
            int rejected = 0;
            foreach (var point in accepted)
            {
                double zScore = stdDev > 0 ? Math.Abs(point.CalculatedCO - mean) / stdDev : 0;
                point.ZScore = zScore;
                
                if (zScore > zThreshold)
                {
                    point.Status = PointStatus.Rejected;
                    point.RejectionReason = $"Z-score {zScore:F2} > {zThreshold}";
                    rejected++;
                }
                else
                {
                    point.Status = PointStatus.Accepted;
                }
            }
            
            iterations.Add(new IterationResult
            {
                IterationNumber = iter,
                PointsInIteration = accepted.Count,
                PointsRejected = rejected,
                MeanCO = mean,
                StdDev = stdDev,
                UpperLimit = upper,
                LowerLimit = lower,
                IsConverged = rejected == 0
            });
            
            if (rejected == 0) break;
        }
        
        return iterations;
    }
    
    #endregion
    
    #region Outlier Detection
    
    /// <summary>
    /// Apply 3-sigma outlier detection
    /// </summary>
    public void ApplyOutlierDetection(List<RovGyroDataPoint> points)
    {
        if (points.Count < 3) return;
        
        // Calculate mean and std dev of C-O
        var coValues = points.Select(p => p.CalculatedCO).ToList();
        double mean = coValues.Average();
        double stdDev = CalculateStdDev(coValues);
        
        if (stdDev < 0.0001) stdDev = 0.0001; // Prevent division by zero
        
        // Calculate Z-scores and mark outliers
        foreach (var point in points)
        {
            point.ZScore = Math.Abs(point.CalculatedCO - mean) / stdDev;
            
            if (point.ZScore > ZScoreThreshold)
            {
                point.Status = PointStatus.Rejected;
                point.RejectionReason = $"Z-score {point.ZScore:F2} > {ZScoreThreshold}";
            }
            else
            {
                point.Status = PointStatus.Accepted;
                point.RejectionReason = null;
            }
        }
    }
    
    #endregion
    
    #region Full Processing Pipeline
    
    /// <summary>
    /// Process all data points through complete pipeline
    /// </summary>
    public void ProcessDataPoints(List<RovGyroDataPoint> points, RovConfiguration config)
    {
        if (points.Count == 0) return;
        
        // Step 1: Apply geometric corrections
        ApplyGeometricCorrections(points, config);
        
        // Step 2: Calculate C-O values
        CalculateCO(points);
        
        // Step 3: Apply outlier detection
        ApplyOutlierDetection(points);
    }
    
    #endregion
    
    #region Statistics
    
    /// <summary>
    /// Calculate comprehensive results
    /// </summary>
    public CalibrationResult CalculateResults(List<RovGyroDataPoint> points, RovConfiguration config)
    {
        var result = new CalibrationResult();
        
        if (points.Count == 0) return result;
        
        // All observations
        var allCO = points.Select(p => p.CalculatedCO).ToList();
        result.TotalCount = points.Count;
        result.MeanCOAll = allCO.Average();
        result.StdDevAll = CalculateStdDev(allCO);
        result.MinCO = allCO.Min();
        result.MaxCO = allCO.Max();
        
        // Accepted only
        var accepted = points.Where(p => p.Status == PointStatus.Accepted).ToList();
        result.AcceptedCount = accepted.Count;
        
        if (accepted.Count > 0)
        {
            var acceptedCO = accepted.Select(p => p.CalculatedCO).ToList();
            result.MeanCOAccepted = acceptedCO.Average();
            result.StdDevAccepted = CalculateStdDev(acceptedCO);
        }
        
        // Rejected
        result.RejectedCount = points.Count(p => p.Status == PointStatus.Rejected);
        
        // Heading coverage (use vessel heading for coverage analysis)
        var headings = points.Select(p => p.VesselHeading).ToList();
        result.MinHeading = headings.Min();
        result.MaxHeading = headings.Max();
        result.HeadingCoverage = CalculateHeadingCoverage(headings);
        
        // Time range
        result.StartTime = points.Min(p => p.DateTime);
        result.EndTime = points.Max(p => p.DateTime);
        
        // Geometric corrections applied
        result.BaselineAngleTheta = config.CalculateBaselineAngle();
        result.BaselineOffsetD = config.BaselineOffset;
        result.FacingDirectionOffset = config.FacingDirectionOffset;
        
        return result;
    }
    
    #endregion
    
    #region Validation
    
    /// <summary>
    /// Run QC validation checks
    /// </summary>
    public ValidationResult ValidateResults(CalibrationResult result, CalibrationProject project)
    {
        var validation = new ValidationResult();
        
        // Check 1: Rejection rate
        var rejectionCheck = new QcCheck
        {
            Name = "Rejection Rate",
            Description = "Percentage of observations rejected by 3-sigma test",
            ActualValue = $"{result.RejectionRate:F1}%",
            Threshold = "< 5% (warning < 10%)"
        };
        rejectionCheck.Status = result.RejectionRate < 5 ? QcStatus.Pass :
                                result.RejectionRate < 10 ? QcStatus.Warning : QcStatus.Fail;
        validation.Checks.Add(rejectionCheck);
        
        // Check 2: Standard deviation
        var stdDevCheck = new QcCheck
        {
            Name = "Standard Deviation",
            Description = "Spread of accepted C-O values",
            ActualValue = $"{result.StdDevAccepted:F4}°",
            Threshold = "< 0.5° (warning < 1.0°)"
        };
        stdDevCheck.Status = result.StdDevAccepted < 0.5 ? QcStatus.Pass :
                             result.StdDevAccepted < 1.0 ? QcStatus.Warning : QcStatus.Fail;
        validation.Checks.Add(stdDevCheck);
        
        // Check 3: Verification mean (only for verification mode)
        if (project.Purpose == CalibrationPurpose.Verification)
        {
            var verifyCheck = new QcCheck
            {
                Name = "Verification Mean",
                Description = "Mean C-O should be near zero for verification",
                ActualValue = $"{result.MeanCOAccepted:F4}°",
                Threshold = "≈ 0° (±0.5°)"
            };
            verifyCheck.Status = Math.Abs(result.MeanCOAccepted) < 0.5 ? QcStatus.Pass :
                                 Math.Abs(result.MeanCOAccepted) < 1.0 ? QcStatus.Warning : QcStatus.Fail;
            validation.Checks.Add(verifyCheck);
        }
        
        // Check 4: Observation count
        var countCheck = new QcCheck
        {
            Name = "Observation Count",
            Description = "Minimum observations for statistical validity",
            ActualValue = result.AcceptedCount.ToString(),
            Threshold = "> 100 (warning > 50)"
        };
        countCheck.Status = result.AcceptedCount > 100 ? QcStatus.Pass :
                           result.AcceptedCount > 50 ? QcStatus.Warning : QcStatus.Fail;
        validation.Checks.Add(countCheck);
        
        // Check 5: Heading coverage
        var coverageCheck = new QcCheck
        {
            Name = "Heading Coverage",
            Description = "Range of headings observed during test",
            ActualValue = $"{result.HeadingCoverage:F1}°",
            Threshold = "> 90° (warning > 45°)"
        };
        coverageCheck.Status = result.HeadingCoverage > 90 ? QcStatus.Pass :
                               result.HeadingCoverage > 45 ? QcStatus.Warning : QcStatus.Fail;
        validation.Checks.Add(coverageCheck);
        
        // Check 6: Test duration
        var durationCheck = new QcCheck
        {
            Name = "Test Duration",
            Description = "Total duration of calibration test",
            ActualValue = $"{result.Duration.TotalMinutes:F0} min",
            Threshold = "> 30 min (warning > 15 min)"
        };
        durationCheck.Status = result.Duration.TotalMinutes > 30 ? QcStatus.Pass :
                               result.Duration.TotalMinutes > 15 ? QcStatus.Warning : QcStatus.Fail;
        validation.Checks.Add(durationCheck);
        
        // OverallStatus is computed automatically from Checks
        // No need to set it manually
        
        return validation;
    }
    
    #endregion
    
    #region Helper Methods
    
    private static double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 360;
        while (angle >= 360) angle -= 360;
        return angle;
    }
    
    private static double CalculateStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        double mean = values.Average();
        double sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }
    
    private static double CalculateHeadingCoverage(List<double> headings)
    {
        if (headings.Count < 2) return 0;
        
        var sorted = headings.OrderBy(h => h).ToList();
        double maxGap = 0;
        
        for (int i = 1; i < sorted.Count; i++)
        {
            double gap = sorted[i] - sorted[i - 1];
            if (gap > maxGap) maxGap = gap;
        }
        
        double wrapGap = (360 - sorted.Last()) + sorted.First();
        if (wrapGap > maxGap) maxGap = wrapGap;
        
        return 360 - maxGap;
    }
    
    #endregion
}
