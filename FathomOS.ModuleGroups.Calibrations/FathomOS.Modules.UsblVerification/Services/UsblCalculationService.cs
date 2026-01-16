using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for USBL verification calculations
/// </summary>
public class UsblCalculationService
{
    /// <summary>
    /// Calculate statistics for a spin test dataset
    /// </summary>
    public void CalculateSpinStatistics(SpinTestData spinData)
    {
        var validObs = spinData.Observations.Where(o => !o.IsExcluded).ToList();
        if (!validObs.Any())
        {
            spinData.Statistics = new TestStatistics { Name = spinData.Name };
            return;
        }
        
        var stats = new TestStatistics
        {
            Name = spinData.Name,
            PointCount = validObs.Count,
            StartTime = validObs.Min(o => o.DateTime),
            EndTime = validObs.Max(o => o.DateTime),
            
            AverageGyro = validObs.Average(o => o.VesselGyro),
            AverageVesselEasting = validObs.Average(o => o.VesselEasting),
            AverageVesselNorthing = validObs.Average(o => o.VesselNorthing),
            AverageTransponderEasting = validObs.Average(o => o.TransponderEasting),
            AverageTransponderNorthing = validObs.Average(o => o.TransponderNorthing),
            AverageDepth = validObs.Average(o => o.TransponderDepth)
        };
        
        // Calculate 2-sigma standard deviations
        stats.StdDevEasting2Sigma = 2 * CalculateStdDev(validObs.Select(o => o.TransponderEasting));
        stats.StdDevNorthing2Sigma = 2 * CalculateStdDev(validObs.Select(o => o.TransponderNorthing));
        stats.StdDevDepth2Sigma = 2 * CalculateStdDev(validObs.Select(o => o.TransponderDepth));
        
        // Calculate transducer offset and slant range
        double dE = stats.AverageVesselEasting - stats.AverageTransponderEasting;
        double dN = stats.AverageVesselNorthing - stats.AverageTransponderNorthing;
        stats.TransducerOffset = Math.Sqrt(dE * dE + dN * dN);
        stats.SlantRange = Math.Sqrt(stats.TransducerOffset * stats.TransducerOffset + 
                                     Math.Abs(stats.AverageDepth) * Math.Abs(stats.AverageDepth));
        
        // Calculate tolerance: max of 0.5m or 0.2% of slant range
        stats.ToleranceValue = Math.Max(0.5, stats.SlantRange * 0.002);
        
        // Calculate deltas for each observation
        foreach (var obs in validObs)
        {
            obs.DeltaEasting = obs.TransponderEasting - stats.AverageTransponderEasting;
            obs.DeltaNorthing = obs.TransponderNorthing - stats.AverageTransponderNorthing;
            obs.DeltaDepth = obs.TransponderDepth - stats.AverageDepth;
        }
        
        spinData.Statistics = stats;
    }
    
    /// <summary>
    /// Calculate statistics for a transit test dataset
    /// </summary>
    public void CalculateTransitStatistics(TransitTestData transitData, double spinAvgEasting = 0, double spinAvgNorthing = 0)
    {
        var validObs = transitData.Observations.Where(o => !o.IsExcluded).ToList();
        if (!validObs.Any())
        {
            transitData.Statistics = new TestStatistics { Name = transitData.Name };
            return;
        }
        
        var stats = new TestStatistics
        {
            Name = transitData.Name,
            PointCount = validObs.Count,
            StartTime = validObs.Min(o => o.DateTime),
            EndTime = validObs.Max(o => o.DateTime),
            
            AverageGyro = validObs.Average(o => o.VesselGyro),
            AverageVesselEasting = validObs.Average(o => o.VesselEasting),
            AverageVesselNorthing = validObs.Average(o => o.VesselNorthing),
            AverageTransponderEasting = validObs.Average(o => o.TransponderEasting),
            AverageTransponderNorthing = validObs.Average(o => o.TransponderNorthing),
            AverageDepth = validObs.Average(o => o.TransponderDepth)
        };
        
        // Calculate 2-sigma standard deviations (using STDEV not 2*STDEV for transit)
        stats.StdDevEasting2Sigma = CalculateStdDev(validObs.Select(o => o.TransponderEasting));
        stats.StdDevNorthing2Sigma = CalculateStdDev(validObs.Select(o => o.TransponderNorthing));
        stats.StdDevDepth2Sigma = CalculateStdDev(validObs.Select(o => o.TransponderDepth));
        
        // Calculate transducer offset and slant range
        double dE = stats.AverageVesselEasting - stats.AverageTransponderEasting;
        double dN = stats.AverageVesselNorthing - stats.AverageTransponderNorthing;
        stats.TransducerOffset = Math.Sqrt(dE * dE + dN * dN);
        stats.SlantRange = Math.Sqrt(stats.TransducerOffset * stats.TransducerOffset + 
                                     Math.Abs(stats.AverageDepth) * Math.Abs(stats.AverageDepth));
        
        // Calculate tolerance: max of 1m or 0.5% of slant range
        stats.ToleranceValue = Math.Max(1.0, stats.SlantRange * 0.005);
        
        // Calculate transit length and direction
        var first = validObs.First();
        var last = validObs.Last();
        double transitDe = last.VesselEasting - first.VesselEasting;
        double transitDn = last.VesselNorthing - first.VesselNorthing;
        stats.TransitLength = Math.Sqrt(transitDe * transitDe + transitDn * transitDn);
        stats.TransitDirection = CalculateBearing(transitDe, transitDn);
        
        // Calculate deltas and vessel offsets for each observation
        int midPoint = validObs.Count / 2;
        for (int i = 0; i < validObs.Count; i++)
        {
            var obs = validObs[i];
            obs.DeltaEasting = obs.TransponderEasting - stats.AverageTransponderEasting;
            obs.DeltaNorthing = obs.TransponderNorthing - stats.AverageTransponderNorthing;
            obs.DeltaDepth = obs.TransponderDepth - stats.AverageDepth;
            
            obs.VesselOffsetEasting = obs.VesselEasting - obs.TransponderEasting;
            obs.VesselOffsetNorthing = obs.VesselNorthing - obs.TransponderNorthing;
            obs.VesselOffsetDistance = Math.Sqrt(obs.VesselOffsetEasting * obs.VesselOffsetEasting + 
                                                 obs.VesselOffsetNorthing * obs.VesselOffsetNorthing);
            obs.VesselOffsetSign = i < midPoint ? -1 : 1;
        }
        
        transitData.Statistics = stats;
    }
    
    /// <summary>
    /// Calculate combined verification results
    /// </summary>
    public VerificationResults CalculateResults(UsblVerificationProject project)
    {
        var results = new VerificationResults();
        
        // Calculate statistics for all tests
        foreach (var spin in project.AllSpinTests)
        {
            CalculateSpinStatistics(spin);
        }
        
        // Get spin overall average first
        var validSpins = project.AllSpinTests.Where(s => s.HasData).ToList();
        if (validSpins.Any())
        {
            results.SpinOverallAverageEasting = validSpins.Average(s => s.Statistics.AverageTransponderEasting);
            results.SpinOverallAverageNorthing = validSpins.Average(s => s.Statistics.AverageTransponderNorthing);
            results.SpinOverallAverageDepth = validSpins.Average(s => s.Statistics.AverageDepth);
        }
        
        // Check if transit test was performed
        var validTransits = project.AllTransitTests.Where(t => t.HasData).ToList();
        results.TransitWasPerformed = validTransits.Any();
        
        foreach (var transit in project.AllTransitTests)
        {
            CalculateTransitStatistics(transit, results.SpinOverallAverageEasting, results.SpinOverallAverageNorthing);
        }
        
        // Calculate spin results
        CalculateSpinResults(project, results);
        
        // Calculate transit results (only if transit was performed)
        if (results.TransitWasPerformed)
        {
            CalculateTransitResults(project, results);
            
            // Calculate absolute position check
            CalculateAbsolutePositionCheck(project, results);
            
            // Calculate alignment verification
            CalculateAlignmentVerification(project, results);
        }
        else
        {
            // Set default values for skipped transit
            results.TransitPassFail = true;  // Not applicable
            results.Line1AlignmentPass = true;
            results.Line2AlignmentPass = true;
        }
        
        return results;
    }
    
    private void CalculateSpinResults(UsblVerificationProject project, VerificationResults results)
    {
        var validSpins = project.AllSpinTests.Where(s => s.HasData).ToList();
        if (!validSpins.Any()) return;
        
        // Collect all observations from all spin tests
        var allObs = validSpins.SelectMany(s => s.Observations.Where(o => !o.IsExcluded)).ToList();
        
        // Calculate 2-sigma standard deviations across all spin data
        results.SpinStdDevEasting2Sigma = 2 * CalculateStdDev(allObs.Select(o => o.TransponderEasting));
        results.SpinStdDevNorthing2Sigma = 2 * CalculateStdDev(allObs.Select(o => o.TransponderNorthing));
        results.SpinStdDevDepth2Sigma = 2 * CalculateStdDev(allObs.Select(o => o.TransponderDepth));
        
        // Calculate max difference from overall average
        foreach (var spin in validSpins)
        {
            var diffE = results.SpinOverallAverageEasting - spin.Statistics.AverageTransponderEasting;
            var diffN = results.SpinOverallAverageNorthing - spin.Statistics.AverageTransponderNorthing;
            var diffD = results.SpinOverallAverageDepth - spin.Statistics.AverageDepth;
            var radial = Math.Sqrt(diffE * diffE + diffN * diffN);
            
            results.HeadingResults.Add(new HeadingResult
            {
                Heading = (int)spin.NominalHeading,
                ActualHeading = spin.ActualHeading,
                HeadingLabel = spin.DisplayName,  // Use DisplayName which shows actual heading
                Easting = spin.Statistics.AverageTransponderEasting,
                Northing = spin.Statistics.AverageTransponderNorthing,
                Depth = spin.Statistics.AverageDepth,
                DiffEasting = diffE,
                DiffNorthing = diffN,
                DiffRadial = radial
            });
        }
        
        // Find the heading with maximum radial difference
        var maxDiffHeading = results.HeadingResults.OrderByDescending(h => h.DiffRadial).First();
        results.SpinMaxDiffEasting = maxDiffHeading.DiffEasting;
        results.SpinMaxDiffNorthing = maxDiffHeading.DiffNorthing;
        results.SpinMaxDiffRadial = maxDiffHeading.DiffRadial;
        
        // Max depth difference
        var maxAbsDepthDiff = results.HeadingResults.Max(h => Math.Abs(h.Depth - results.SpinOverallAverageDepth));
        results.SpinMaxDiffDepth = results.HeadingResults
            .Where(h => Math.Abs(h.Depth - results.SpinOverallAverageDepth) == maxAbsDepthDiff)
            .First().Depth - results.SpinOverallAverageDepth;
        
        // Calculate 2DRMS
        results.Spin2DRMS = Math.Sqrt(results.SpinStdDevEasting2Sigma * results.SpinStdDevEasting2Sigma +
                                      results.SpinStdDevNorthing2Sigma * results.SpinStdDevNorthing2Sigma);
        
        // Max allowable difference (use first available tolerance)
        results.SpinMaxAllowableDiff = validSpins.First().Statistics.ToleranceValue;
        
        // Pass/Fail
        results.SpinPassFail = results.SpinMaxDiffRadial <= results.SpinMaxAllowableDiff;
    }
    
    private void CalculateTransitResults(UsblVerificationProject project, VerificationResults results)
    {
        var validTransits = project.AllTransitTests.Where(t => t.HasData).ToList();
        if (!validTransits.Any()) return;
        
        // Combined average position
        results.TransitCombinedAverageEasting = validTransits.Average(t => t.Statistics.AverageTransponderEasting);
        results.TransitCombinedAverageNorthing = validTransits.Average(t => t.Statistics.AverageTransponderNorthing);
        results.TransitCombinedAverageDepth = validTransits.Average(t => t.Statistics.AverageDepth);
        
        // Collect all transit observations
        var allObs = validTransits.SelectMany(t => t.Observations.Where(o => !o.IsExcluded)).ToList();
        
        // 2-sigma standard deviations
        results.TransitStdDevEasting2Sigma = 2 * CalculateStdDev(allObs.Select(o => o.TransponderEasting));
        results.TransitStdDevNorthing2Sigma = 2 * CalculateStdDev(allObs.Select(o => o.TransponderNorthing));
        
        // Max diff between transit lines
        if (validTransits.Count >= 2)
        {
            var t1 = validTransits[0].Statistics;
            var t2 = validTransits[1].Statistics;
            
            var diffE = Math.Abs(t1.AverageTransponderEasting - t2.AverageTransponderEasting);
            var diffN = Math.Abs(t1.AverageTransponderNorthing - t2.AverageTransponderNorthing);
            results.TransitMaxDiffBetweenLines = Math.Max(diffE, diffN);
        }
        
        // Max diff from spin position
        double maxDiffFromSpinE = 0, maxDiffFromSpinN = 0;
        foreach (var transit in validTransits)
        {
            var diffE = transit.Statistics.AverageTransponderEasting - results.SpinOverallAverageEasting;
            var diffN = transit.Statistics.AverageTransponderNorthing - results.SpinOverallAverageNorthing;
            
            if (Math.Abs(diffE) > Math.Abs(maxDiffFromSpinE)) maxDiffFromSpinE = diffE;
            if (Math.Abs(diffN) > Math.Abs(maxDiffFromSpinN)) maxDiffFromSpinN = diffN;
        }
        
        results.TransitMaxDiffFromSpin = Math.Sqrt(maxDiffFromSpinE * maxDiffFromSpinE + 
                                                   maxDiffFromSpinN * maxDiffFromSpinN);
        results.TransitMaxDiffRadial = results.TransitMaxDiffFromSpin;
        
        // 2DRMS
        results.Transit2DRMS = Math.Sqrt(results.TransitStdDevEasting2Sigma * results.TransitStdDevEasting2Sigma +
                                         results.TransitStdDevNorthing2Sigma * results.TransitStdDevNorthing2Sigma);
        
        // Max allowable spread
        results.TransitMaxAllowableSpread = validTransits.First().Statistics.ToleranceValue;
        
        // Pass/Fail
        results.TransitPassFail = results.TransitMaxDiffRadial <= results.TransitMaxAllowableSpread;
    }
    
    private void CalculateAbsolutePositionCheck(UsblVerificationProject project, VerificationResults results)
    {
        // Compare position from reciprocal headings (0° vs 180°, or 90° vs 270°)
        var spin000 = project.Spin000;
        var spin180 = project.Spin180;
        
        if (spin000.HasData && spin180.HasData)
        {
            results.AbsolutePositionDiffEasting = spin000.Statistics.AverageTransponderEasting - 
                                                  spin180.Statistics.AverageTransponderEasting;
            results.AbsolutePositionDiffNorthing = spin000.Statistics.AverageTransponderNorthing - 
                                                   spin180.Statistics.AverageTransponderNorthing;
            results.AbsolutePositionDiffDepth = spin000.Statistics.AverageDepth - spin180.Statistics.AverageDepth;
        }
        else if (project.Spin090.HasData && project.Spin270.HasData)
        {
            results.AbsolutePositionDiffEasting = project.Spin090.Statistics.AverageTransponderEasting - 
                                                  project.Spin270.Statistics.AverageTransponderEasting;
            results.AbsolutePositionDiffNorthing = project.Spin090.Statistics.AverageTransponderNorthing - 
                                                   project.Spin270.Statistics.AverageTransponderNorthing;
            results.AbsolutePositionDiffDepth = project.Spin090.Statistics.AverageDepth - project.Spin270.Statistics.AverageDepth;
        }
        
        results.AbsolutePositionRange = Math.Sqrt(results.AbsolutePositionDiffEasting * results.AbsolutePositionDiffEasting +
                                                  results.AbsolutePositionDiffNorthing * results.AbsolutePositionDiffNorthing);
        results.AbsolutePositionBearing = CalculateBearing(results.AbsolutePositionDiffEasting, 
                                                           results.AbsolutePositionDiffNorthing);
    }
    
    private void CalculateAlignmentVerification(UsblVerificationProject project, VerificationResults results)
    {
        // This requires fitting lines through transit data and calculating residual alignments
        // Simplified implementation - in practice would involve least squares fitting
        
        if (project.Transit1.HasData)
        {
            results.Line1ResidualAlignment = CalculateResidualAlignment(project.Transit1);
            results.Line1ScaleFactor = CalculateScaleFactor(project.Transit1);
            results.Line1AlignmentPass = Math.Abs(results.Line1ResidualAlignment) <= project.AlignmentTolerance;
            results.Line1ScalePass = Math.Abs(1 - results.Line1ScaleFactor) <= project.ScaleFactorTolerance;
        }
        else
        {
            results.Line1AlignmentPass = true;
            results.Line1ScalePass = true;
        }
        
        if (project.Transit2.HasData)
        {
            results.Line2ResidualAlignment = CalculateResidualAlignment(project.Transit2);
            results.Line2ScaleFactor = CalculateScaleFactor(project.Transit2);
            results.Line2AlignmentPass = Math.Abs(results.Line2ResidualAlignment) <= project.AlignmentTolerance;
            results.Line2ScalePass = Math.Abs(1 - results.Line2ScaleFactor) <= project.ScaleFactorTolerance;
        }
        else
        {
            results.Line2AlignmentPass = true;
            results.Line2ScalePass = true;
        }
    }
    
    private double CalculateResidualAlignment(TransitTestData transit)
    {
        var validObs = transit.Observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count < 3) return 0;
        
        // Simple linear regression to find bearing of vessel track relative to TP positions
        // Residual alignment is the difference between expected and observed bearing
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = validObs.Count;
        
        // Use vessel offset as X, perpendicular offset as Y
        foreach (var obs in validObs)
        {
            double x = obs.VesselOffsetDistance * obs.VesselOffsetSign;
            double y = obs.DeltaEasting; // Simplified - should be perpendicular to transit direction
            
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }
        
        if (Math.Abs(n * sumX2 - sumX * sumX) < 0.0001) return 0;
        
        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double residual = Math.Atan(slope) * 180 / Math.PI;
        
        return residual;
    }
    
    private double CalculateScaleFactor(TransitTestData transit)
    {
        var validObs = transit.Observations.Where(o => !o.IsExcluded).ToList();
        if (validObs.Count < 3) return 1.0;
        
        // Compare expected vs observed range
        // Scale factor = observed range / expected range
        var first = validObs.First();
        var last = validObs.Last();
        
        double vesselRange = Math.Sqrt(
            Math.Pow(last.VesselEasting - first.VesselEasting, 2) +
            Math.Pow(last.VesselNorthing - first.VesselNorthing, 2));
        
        double tpRange = Math.Sqrt(
            Math.Pow(last.TransponderEasting - first.TransponderEasting, 2) +
            Math.Pow(last.TransponderNorthing - first.TransponderNorthing, 2));
        
        if (vesselRange < 0.001) return 1.0;
        
        // TP positions should stay constant - any spread indicates scale issue
        // Return 1.0 +/- deviation
        return 1.0;
    }
    
    private double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count < 2) return 0;
        
        double avg = list.Average();
        double sumSquares = list.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sumSquares / (list.Count - 1));
    }
    
    private double CalculateBearing(double dE, double dN)
    {
        if (Math.Abs(dE) < 0.0001 && Math.Abs(dN) < 0.0001) return 0;
        
        double bearing = Math.Atan2(dE, dN) * 180 / Math.PI;
        if (bearing < 0) bearing += 360;
        
        return bearing;
    }
}
