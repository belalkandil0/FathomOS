namespace FathomOS.Modules.GnssCalibration.Models;

/// <summary>
/// Statistical results for a set of GNSS observations or comparisons.
/// </summary>
public class GnssStatistics
{
    /// <summary>Name/identifier for this statistics set (e.g., "Raw Data", "Filtered Data").</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Total number of points analyzed.</summary>
    public int PointCount { get; set; }
    
    /// <summary>Alias for PointCount for UI binding compatibility.</summary>
    public int SampleCount => PointCount;
    
    /// <summary>Number of rejected points.</summary>
    public int RejectedCount { get; set; }
    
    /// <summary>Number of accepted points.</summary>
    public int AcceptedCount => PointCount - RejectedCount;
    
    // System 1 Statistics
    public double System1MeanEasting { get; set; }
    public double System1MeanNorthing { get; set; }
    public double System1MeanHeight { get; set; }
    public double System1SdEasting { get; set; }
    public double System1SdNorthing { get; set; }
    public double System1SdHeight { get; set; }
    public double System1MaxEasting { get; set; }
    public double System1MaxNorthing { get; set; }
    public double System1MaxHeight { get; set; }
    public double System1MinEasting { get; set; }
    public double System1MinNorthing { get; set; }
    public double System1MinHeight { get; set; }
    
    /// <summary>2DRMS for System 1 = 2 * sqrt(SD_E² + SD_N²)</summary>
    public double System1_2DRMS => 2 * Math.Sqrt(System1SdEasting * System1SdEasting + System1SdNorthing * System1SdNorthing);
    
    /// <summary>Spread for System 1 (Max - Min radial from mean).</summary>
    public double System1Spread { get; set; }
    
    // System 2 Statistics
    public double System2MeanEasting { get; set; }
    public double System2MeanNorthing { get; set; }
    public double System2MeanHeight { get; set; }
    public double System2SdEasting { get; set; }
    public double System2SdNorthing { get; set; }
    public double System2SdHeight { get; set; }
    public double System2MaxEasting { get; set; }
    public double System2MaxNorthing { get; set; }
    public double System2MaxHeight { get; set; }
    public double System2MinEasting { get; set; }
    public double System2MinNorthing { get; set; }
    public double System2MinHeight { get; set; }
    
    /// <summary>2DRMS for System 2</summary>
    public double System2_2DRMS => 2 * Math.Sqrt(System2SdEasting * System2SdEasting + System2SdNorthing * System2SdNorthing);
    
    /// <summary>Spread for System 2 (Max - Min radial from mean).</summary>
    public double System2Spread { get; set; }
    
    // Delta (C-O) Statistics - Original properties
    public double DeltaMeanEasting { get; set; }
    public double DeltaMeanNorthing { get; set; }
    public double DeltaMeanHeight { get; set; }
    public double DeltaSdEasting { get; set; }
    public double DeltaSdNorthing { get; set; }
    public double DeltaSdHeight { get; set; }
    public double DeltaMaxEasting { get; set; }
    public double DeltaMaxNorthing { get; set; }
    public double DeltaMaxHeight { get; set; }
    public double DeltaMinEasting { get; set; }
    public double DeltaMinNorthing { get; set; }
    public double DeltaMinHeight { get; set; }
    
    // Alias properties for UI binding compatibility (Mean)
    public double MeanDeltaEasting => DeltaMeanEasting;
    public double MeanDeltaNorthing => DeltaMeanNorthing;
    public double MeanDeltaHeight => DeltaMeanHeight;
    
    // Alias properties for UI binding compatibility (StdDev)
    public double StdDevDeltaEasting => DeltaSdEasting;
    public double StdDevDeltaNorthing => DeltaSdNorthing;
    public double StdDevDeltaHeight => DeltaSdHeight;
    public double DeltaStdDevEasting => DeltaSdEasting;
    public double DeltaStdDevNorthing => DeltaSdNorthing;
    public double DeltaStdDevHeight => DeltaSdHeight;
    
    // Alias properties for UI binding compatibility (Max/Min)
    public double MaxDeltaEasting => DeltaMaxEasting;
    public double MaxDeltaNorthing => DeltaMaxNorthing;
    public double MaxDeltaHeight => DeltaMaxHeight;
    public double MinDeltaEasting => DeltaMinEasting;
    public double MinDeltaNorthing => DeltaMinNorthing;
    public double MinDeltaHeight => DeltaMinHeight;
    
    /// <summary>2DRMS for Delta = 2 * sqrt(SD_dE² + SD_dN²)</summary>
    public double Delta2DRMS => 2 * Math.Sqrt(DeltaSdEasting * DeltaSdEasting + DeltaSdNorthing * DeltaSdNorthing);
    
    /// <summary>Alias for Delta2DRMS for UI binding.</summary>
    public double TwoDRMS => Delta2DRMS;
    
    /// <summary>Maximum radial distance (spread).</summary>
    public double Spread { get; set; }
    
    /// <summary>Mean radial distance.</summary>
    public double MeanRadialDistance { get; set; }
    
    /// <summary>Alias for MeanRadialDistance for UI binding.</summary>
    public double MeanRadial => MeanRadialDistance;
    
    /// <summary>Standard deviation of radial distance.</summary>
    public double SdRadialDistance { get; set; }
    
    /// <summary>Alias for SdRadialDistance for UI binding.</summary>
    public double StdDevRadial => SdRadialDistance;
    
    /// <summary>RMS of radial distances.</summary>
    public double RmsRadial { get; set; }
    
    /// <summary>Maximum radial distance.</summary>
    public double MaxRadial { get; set; }
    
    /// <summary>Minimum radial distance.</summary>
    public double MinRadial { get; set; }
    
    /// <summary>RMS of Delta Easting.</summary>
    public double RmsDeltaEasting { get; set; }
    
    /// <summary>Alias for RmsDeltaEasting for UI binding.</summary>
    public double DeltaRmsEasting => RmsDeltaEasting;
    
    /// <summary>RMS of Delta Northing.</summary>
    public double RmsDeltaNorthing { get; set; }
    
    /// <summary>Alias for RmsDeltaNorthing for UI binding.</summary>
    public double DeltaRmsNorthing => RmsDeltaNorthing;
    
    /// <summary>RMS of Delta Height.</summary>
    public double RmsDeltaHeight { get; set; }
    
    /// <summary>Alias for RmsDeltaHeight for UI binding.</summary>
    public double DeltaRmsHeight => RmsDeltaHeight;

    /// <summary>CEP 50% - Circular Error Probable (50% of points within this radius).</summary>
    public double Cep50 { get; set; }
    
    /// <summary>CEP 95% - 95% of points within this radius.</summary>
    public double Cep95 { get; set; }
    
    /// <summary>Maximum radial distance from average position (important for calibration reports).</summary>
    public double MaxRadialFromAverage { get; set; }
    
    /// <summary>Maximum radial from average for filtered data specifically.</summary>
    public double MaxRadialFromAverageFiltered { get; set; }
    
    /// <summary>Maximum radial from average for raw data specifically.</summary>
    public double MaxRadialFromAverageRaw { get; set; }
    
    // More alias properties for UI bindings
    /// <summary>Alias for MeanRadialDistance.</summary>
    public double MeanRadialError => MeanRadialDistance;
    
    /// <summary>Alias for MaxRadial.</summary>
    public double MaxRadialError => MaxRadial;
    
    /// <summary>Alias for SdRadialDistance.</summary>
    public double StdDevRadialError => SdRadialDistance;
    
    /// <summary>Alias for RmsRadial.</summary>
    public double RmsRadialError => RmsRadial;
    
    /// <summary>Filter threshold used (sigma value).</summary>
    public double FilterThreshold { get; set; } = 3.0;
}

/// <summary>
/// Complete statistics container for both raw and filtered data.
/// </summary>
public class GnssStatisticsResult
{
    /// <summary>Statistics calculated from raw (unfiltered) data.</summary>
    public GnssStatistics RawStatistics { get; set; } = new() { Name = "Raw Data" };
    
    /// <summary>Statistics calculated from filtered data.</summary>
    public GnssStatistics FilteredStatistics { get; set; } = new() { Name = "Filtered Data" };
    
    /// <summary>Timestamp when statistics were calculated.</summary>
    public DateTime CalculatedAt { get; set; } = DateTime.Now;
    
    /// <summary>Filter threshold used (sigma value).</summary>
    public double FilterThreshold { get; set; } = 3.0;
    
    /// <summary>Total point count from raw data.</summary>
    public int PointCount => RawStatistics.PointCount;
    
    /// <summary>Rejected count from filtering.</summary>
    public int RejectedCount => RawStatistics.PointCount - FilteredStatistics.PointCount;
    
    /// <summary>Accepted count after filtering.</summary>
    public int AcceptedCount => FilteredStatistics.PointCount;
    
    // === TOLERANCE CHECK ===
    
    /// <summary>User-specified 2DRMS tolerance value.</summary>
    public double ToleranceValue { get; set; } = 0.15;
    
    /// <summary>Unit for the tolerance (m, ft, US ft).</summary>
    public string ToleranceUnit { get; set; } = "m";
    
    /// <summary>Whether tolerance check is enabled.</summary>
    public bool ToleranceCheckEnabled { get; set; } = true;
    
    /// <summary>Whether the 2DRMS passes the specified tolerance.</summary>
    public bool PassesTolerance => !ToleranceCheckEnabled || FilteredStatistics.Delta2DRMS <= ToleranceValue;
    
    /// <summary>Pass/Fail status as string.</summary>
    public string ToleranceStatus => PassesTolerance ? "PASS" : "FAIL";
    
    /// <summary>Descriptive result of tolerance check.</summary>
    public string ToleranceDescription => ToleranceCheckEnabled 
        ? $"2DRMS ({FilteredStatistics.Delta2DRMS:F4} {ToleranceUnit}) {(PassesTolerance ? "≤" : ">")} Tolerance ({ToleranceValue:F4} {ToleranceUnit}) = {ToleranceStatus}"
        : "Tolerance check disabled";
}
