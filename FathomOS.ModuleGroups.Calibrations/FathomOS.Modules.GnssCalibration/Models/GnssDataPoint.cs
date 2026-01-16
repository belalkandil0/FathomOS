namespace FathomOS.Modules.GnssCalibration.Models;

/// <summary>
/// Represents a single GNSS position observation with timestamp.
/// </summary>
public class GnssDataPoint
{
    /// <summary>Row index from source file.</summary>
    public int Index { get; set; }
    
    /// <summary>Date and time of observation.</summary>
    public DateTime DateTime { get; set; }
    
    /// <summary>Easting coordinate.</summary>
    public double Easting { get; set; }
    
    /// <summary>Northing coordinate.</summary>
    public double Northing { get; set; }
    
    /// <summary>Height/Elevation.</summary>
    public double Height { get; set; }
    
    /// <summary>Source system identifier (e.g., "DGNSS1", "DGNSS2").</summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>Indicates if this point has valid coordinates.</summary>
    public bool IsValid => !double.IsNaN(Easting) && !double.IsNaN(Northing);
}

/// <summary>
/// Represents the comparison result between two GNSS observations at the same time.
/// </summary>
public class GnssComparisonPoint
{
    /// <summary>Row index.</summary>
    public int Index { get; set; }
    
    /// <summary>Observation timestamp.</summary>
    public DateTime DateTime { get; set; }
    
    // System 1 (Reference/Control)
    public double System1Easting { get; set; }
    public double System1Northing { get; set; }
    public double System1Height { get; set; }
    
    // System 2 (Observed)
    public double System2Easting { get; set; }
    public double System2Northing { get; set; }
    public double System2Height { get; set; }
    
    // Calculated Deltas (System1 - System2)
    public double DeltaEasting => System1Easting - System2Easting;
    public double DeltaNorthing => System1Northing - System2Northing;
    public double DeltaHeight => System1Height - System2Height;
    
    /// <summary>Radial distance in horizontal plane = sqrt(dE² + dN²)</summary>
    public double RadialDistance => Math.Sqrt(DeltaEasting * DeltaEasting + DeltaNorthing * DeltaNorthing);
    
    /// <summary>Alias for RadialDistance for compatibility.</summary>
    public double RadialError => RadialDistance;
    
    /// <summary>Standardized score for outlier detection.</summary>
    public double StandardizedScore { get; set; }
    
    /// <summary>Whether this point is rejected by the filter.</summary>
    public bool IsRejected { get; set; }
    
    /// <summary>Rejection reason if rejected.</summary>
    public string? RejectionReason { get; set; }
    
    /// <summary>Status text for display (OK or Rejected).</summary>
    public string StatusText => IsRejected ? "Rejected" : "OK";
    
    /// <summary>Whether this point has valid data.</summary>
    public bool IsValid => !double.IsNaN(System1Easting) && !double.IsNaN(System2Easting);
}

/// <summary>
/// Filter/rejection status for a comparison point.
/// </summary>
public enum PointStatus
{
    Pending,
    Accepted,
    RejectedAuto,      // Rejected by automatic filter (e.g., 3-sigma)
    RejectedManual,    // Rejected manually by user
    Excluded           // Excluded from analysis (e.g., outside time range)
}

/// <summary>
/// Represents a row of data preview from the loaded file.
/// </summary>
public class DataPreviewRow
{
    public int Index { get; set; }
    public int RowNumber { get; set; }
    public DateTime? DateTime { get; set; }
    public string Time { get; set; } = string.Empty;
    public string System1E { get; set; } = string.Empty;
    public string System1N { get; set; } = string.Empty;
    public string System1H { get; set; } = string.Empty;
    public string System2E { get; set; } = string.Empty;
    public string System2N { get; set; } = string.Empty;
    public string System2H { get; set; } = string.Empty;
    
    /// <summary>Display string for row.</summary>
    public string Display => $"Row {Index}: {Time}";
}
