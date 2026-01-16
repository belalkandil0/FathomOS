using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Calculates statistical metrics for GNSS comparison data.
/// Implements 2DRMS, mean, standard deviation, min/max calculations.
/// </summary>
public class StatisticsCalculator
{
    /// <summary>
    /// Calculates complete statistics for a set of comparison points.
    /// </summary>
    public GnssStatistics Calculate(IEnumerable<GnssComparisonPoint> points, string name = "Statistics")
    {
        var pointList = points.Where(p => p.IsValid && !p.IsRejected).ToList();
        var stats = new GnssStatistics { Name = name };
        
        if (pointList.Count == 0)
            return stats;
        
        stats.PointCount = pointList.Count;
        stats.RejectedCount = points.Count(p => p.IsRejected);
        
        // System 1 Statistics
        stats.System1MeanEasting = pointList.Average(p => p.System1Easting);
        stats.System1MeanNorthing = pointList.Average(p => p.System1Northing);
        stats.System1MeanHeight = pointList.Average(p => p.System1Height);
        stats.System1SdEasting = CalculateStdDev(pointList.Select(p => p.System1Easting));
        stats.System1SdNorthing = CalculateStdDev(pointList.Select(p => p.System1Northing));
        stats.System1SdHeight = CalculateStdDev(pointList.Select(p => p.System1Height));
        stats.System1MaxEasting = pointList.Max(p => p.System1Easting);
        stats.System1MaxNorthing = pointList.Max(p => p.System1Northing);
        stats.System1MaxHeight = pointList.Max(p => p.System1Height);
        stats.System1MinEasting = pointList.Min(p => p.System1Easting);
        stats.System1MinNorthing = pointList.Min(p => p.System1Northing);
        stats.System1MinHeight = pointList.Min(p => p.System1Height);
        
        // System 1 Spread (max radial distance from mean)
        stats.System1Spread = pointList.Max(p => 
            Math.Sqrt(Math.Pow(p.System1Easting - stats.System1MeanEasting, 2) + 
                      Math.Pow(p.System1Northing - stats.System1MeanNorthing, 2)));
        
        // System 2 Statistics
        stats.System2MeanEasting = pointList.Average(p => p.System2Easting);
        stats.System2MeanNorthing = pointList.Average(p => p.System2Northing);
        stats.System2MeanHeight = pointList.Average(p => p.System2Height);
        stats.System2SdEasting = CalculateStdDev(pointList.Select(p => p.System2Easting));
        stats.System2SdNorthing = CalculateStdDev(pointList.Select(p => p.System2Northing));
        stats.System2SdHeight = CalculateStdDev(pointList.Select(p => p.System2Height));
        stats.System2MaxEasting = pointList.Max(p => p.System2Easting);
        stats.System2MaxNorthing = pointList.Max(p => p.System2Northing);
        stats.System2MaxHeight = pointList.Max(p => p.System2Height);
        stats.System2MinEasting = pointList.Min(p => p.System2Easting);
        stats.System2MinNorthing = pointList.Min(p => p.System2Northing);
        stats.System2MinHeight = pointList.Min(p => p.System2Height);
        
        // System 2 Spread (max radial distance from mean)
        stats.System2Spread = pointList.Max(p => 
            Math.Sqrt(Math.Pow(p.System2Easting - stats.System2MeanEasting, 2) + 
                      Math.Pow(p.System2Northing - stats.System2MeanNorthing, 2)));
        
        // Delta (C-O) Statistics
        stats.DeltaMeanEasting = pointList.Average(p => p.DeltaEasting);
        stats.DeltaMeanNorthing = pointList.Average(p => p.DeltaNorthing);
        stats.DeltaMeanHeight = pointList.Average(p => p.DeltaHeight);
        stats.DeltaSdEasting = CalculateStdDev(pointList.Select(p => p.DeltaEasting));
        stats.DeltaSdNorthing = CalculateStdDev(pointList.Select(p => p.DeltaNorthing));
        stats.DeltaSdHeight = CalculateStdDev(pointList.Select(p => p.DeltaHeight));
        stats.DeltaMaxEasting = pointList.Max(p => p.DeltaEasting);
        stats.DeltaMaxNorthing = pointList.Max(p => p.DeltaNorthing);
        stats.DeltaMaxHeight = pointList.Max(p => p.DeltaHeight);
        stats.DeltaMinEasting = pointList.Min(p => p.DeltaEasting);
        stats.DeltaMinNorthing = pointList.Min(p => p.DeltaNorthing);
        stats.DeltaMinHeight = pointList.Min(p => p.DeltaHeight);
        
        // Radial distance statistics
        var radialDistances = pointList.Select(p => p.RadialDistance).OrderBy(r => r).ToList();
        stats.MeanRadialDistance = radialDistances.Average();
        stats.SdRadialDistance = CalculateStdDev(radialDistances);
        stats.Spread = radialDistances.Max();
        stats.MaxRadial = radialDistances.Max();
        stats.MinRadial = radialDistances.Min();
        
        // RMS Radial = sqrt(mean of squared radials)
        stats.RmsRadial = Math.Sqrt(radialDistances.Select(r => r * r).Average());
        
        // RMS of Delta components
        stats.RmsDeltaEasting = Math.Sqrt(pointList.Select(p => p.DeltaEasting * p.DeltaEasting).Average());
        stats.RmsDeltaNorthing = Math.Sqrt(pointList.Select(p => p.DeltaNorthing * p.DeltaNorthing).Average());
        stats.RmsDeltaHeight = Math.Sqrt(pointList.Select(p => p.DeltaHeight * p.DeltaHeight).Average());
        
        // CEP (Circular Error Probable) calculations
        // CEP50 = median radial distance (50% of points within this radius)
        if (radialDistances.Count > 0)
        {
            stats.Cep50 = radialDistances[radialDistances.Count / 2];
            
            // CEP95 = 95th percentile
            var index95 = (int)(radialDistances.Count * 0.95);
            stats.Cep95 = radialDistances[Math.Min(index95, radialDistances.Count - 1)];
        }
        
        // MaxRadialFromAverage - maximum distance from the mean delta position
        // This is the critical value shown in the reference image
        var distancesFromAverage = pointList.Select(p =>
            Math.Sqrt(Math.Pow(p.DeltaEasting - stats.DeltaMeanEasting, 2) +
                      Math.Pow(p.DeltaNorthing - stats.DeltaMeanNorthing, 2))).ToList();
        
        if (distancesFromAverage.Count > 0)
        {
            stats.MaxRadialFromAverage = distancesFromAverage.Max();
        }
        
        return stats;
    }
    
    /// <summary>
    /// Calculates statistics for both raw and filtered datasets.
    /// </summary>
    public GnssStatisticsResult CalculateAll(IEnumerable<GnssComparisonPoint> allPoints, double sigmaThreshold)
    {
        var pointList = allPoints.ToList();
        
        var result = new GnssStatisticsResult
        {
            FilterThreshold = sigmaThreshold,
            CalculatedAt = DateTime.Now
        };
        
        // Raw statistics (all valid points, regardless of rejection status)
        var rawPoints = pointList.Where(p => p.IsValid).ToList();
        result.RawStatistics = Calculate(rawPoints.Select(p => 
        {
            // Create copy without rejection flag for raw stats
            return new GnssComparisonPoint
            {
                Index = p.Index,
                DateTime = p.DateTime,
                System1Easting = p.System1Easting,
                System1Northing = p.System1Northing,
                System1Height = p.System1Height,
                System2Easting = p.System2Easting,
                System2Northing = p.System2Northing,
                System2Height = p.System2Height,
                IsRejected = false
            };
        }), "Raw Data");
        
        result.RawStatistics.PointCount = rawPoints.Count;
        result.RawStatistics.RejectedCount = 0;
        
        // Store raw MaxRadialFromAverage
        result.RawStatistics.MaxRadialFromAverageRaw = result.RawStatistics.MaxRadialFromAverage;
        
        // Filtered statistics (only accepted points)
        result.FilteredStatistics = Calculate(pointList, "Filtered Data");
        
        // Store filtered MaxRadialFromAverage
        result.FilteredStatistics.MaxRadialFromAverageFiltered = result.FilteredStatistics.MaxRadialFromAverage;
        
        // Also store both values in filtered stats for easy access
        result.FilteredStatistics.MaxRadialFromAverageRaw = result.RawStatistics.MaxRadialFromAverage;
        
        return result;
    }
    
    /// <summary>
    /// Calculates standard deviation of a sequence.
    /// Uses population standard deviation (N denominator).
    /// </summary>
    private double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        
        if (list.Count < 2)
            return 0;
        
        var mean = list.Average();
        var sumOfSquares = list.Sum(v => Math.Pow(v - mean, 2));
        
        // Population standard deviation (N)
        return Math.Sqrt(sumOfSquares / list.Count);
    }
    
    /// <summary>
    /// Generates points for drawing a 2DRMS circle.
    /// </summary>
    public List<(double X, double Y)> Generate2DRMSCircle(GnssStatistics stats, int numPoints = 360)
    {
        var circle = new List<(double X, double Y)>();
        
        var centerX = stats.DeltaMeanEasting;
        var centerY = stats.DeltaMeanNorthing;
        var radius = stats.Delta2DRMS;
        
        for (int i = 0; i <= numPoints; i++)
        {
            var angle = 2 * Math.PI * i / numPoints;
            var x = centerX + radius * Math.Sin(angle);
            var y = centerY + radius * Math.Cos(angle);
            circle.Add((x, y));
        }
        
        return circle;
    }
    
    /// <summary>
    /// Calculates the percentage of points within the 2DRMS circle.
    /// </summary>
    public double CalculatePointsWithin2DRMS(IEnumerable<GnssComparisonPoint> points, GnssStatistics stats)
    {
        var validPoints = points.Where(p => p.IsValid && !p.IsRejected).ToList();
        
        if (validPoints.Count == 0)
            return 0;
        
        var centerX = stats.DeltaMeanEasting;
        var centerY = stats.DeltaMeanNorthing;
        var radius = stats.Delta2DRMS;
        
        var within = validPoints.Count(p =>
        {
            var dx = p.DeltaEasting - centerX;
            var dy = p.DeltaNorthing - centerY;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            return dist <= radius;
        });
        
        return (double)within / validPoints.Count * 100;
    }
}
