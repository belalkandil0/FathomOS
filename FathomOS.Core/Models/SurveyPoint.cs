namespace FathomOS.Core.Models;

/// <summary>
/// Primary survey point data model used across all Fathom OS modules.
/// Contains core survey data - module-specific fields should be in separate models.
/// </summary>
public class SurveyPoint
{
    // ═══════════════════════════════════════════════════════════════════
    // IDENTIFICATION
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Sequential record/index number from source file
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Legacy property for compatibility - maps to Index
    /// </summary>
    public int RecordNumber
    {
        get => Index;
        set => Index = value;
    }

    /// <summary>
    /// Date and time of the survey point measurement
    /// </summary>
    public DateTime? Timestamp { get; set; }
    
    /// <summary>
    /// Legacy property for compatibility - maps to Timestamp
    /// </summary>
    public DateTime DateTime
    {
        get => Timestamp ?? DateTime.MinValue;
        set => Timestamp = value;
    }

    /// <summary>
    /// Point name/label (optional, for named control points)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Source file identifier
    /// </summary>
    public string? Source { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // RAW COORDINATES (from parsed file)
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Raw Easting coordinate (X) in meters from source file
    /// </summary>
    public double Easting { get; set; }

    /// <summary>
    /// Raw Northing coordinate (Y) in meters from source file
    /// </summary>
    public double Northing { get; set; }

    /// <summary>
    /// Raw Z/Depth value from source file (typically depth below transducer)
    /// </summary>
    public double? Z { get; set; }
    
    /// <summary>
    /// Legacy property for compatibility - maps to Z
    /// </summary>
    public double? Depth
    {
        get => Z;
        set => Z = value;
    }

    /// <summary>
    /// Name of the depth source column
    /// </summary>
    public string? DepthSource { get; set; }

    /// <summary>
    /// Raw Altitude value from file (Altimeter sensor reading)
    /// </summary>
    public double? Altitude { get; set; }

    /// <summary>
    /// Heading in degrees (0-360, clockwise from North)
    /// </summary>
    public double? Heading { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // SMOOTHED DATA (after filtering/smoothing applied)
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Smoothed Easting (X) after filtering
    /// </summary>
    public double? SmoothedEasting { get; set; }

    /// <summary>
    /// Smoothed Northing (Y) after filtering
    /// </summary>
    public double? SmoothedNorthing { get; set; }

    /// <summary>
    /// Smoothed Depth after filtering
    /// </summary>
    public double? SmoothedDepth { get; set; }

    /// <summary>
    /// Smoothed Altitude after filtering
    /// </summary>
    public double? SmoothedAltitude { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // CALCULATED VALUES (from processing pipeline)
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Final X coordinate (SmoothedEasting if available, otherwise Easting)
    /// </summary>
    public double X => SmoothedEasting ?? Easting;

    /// <summary>
    /// Final Y coordinate (SmoothedNorthing if available, otherwise Northing)
    /// </summary>
    public double Y => SmoothedNorthing ?? Northing;

    /// <summary>
    /// Calculated/processed Z value (seabed depth after corrections)
    /// </summary>
    public double? ProcessedDepth { get; set; }
    
    /// <summary>
    /// Legacy property for compatibility - maps to ProcessedDepth
    /// </summary>
    public double? CalculatedZ
    {
        get => ProcessedDepth;
        set => ProcessedDepth = value;
    }

    /// <summary>
    /// Kilometre Post - distance along route alignment in kilometers
    /// </summary>
    public double? Kp { get; set; }

    /// <summary>
    /// Distance to route centerline (positive = right, negative = left)
    /// </summary>
    public double? Dcc { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // CORRECTION FACTORS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Tide correction value applied (meters)
    /// </summary>
    public double? TideCorrection { get; set; }

    /// <summary>
    /// Draft/transducer depth correction applied (meters)
    /// </summary>
    public double? DraftCorrection { get; set; }
    
    /// <summary>
    /// Bathy to Altimeter offset applied (meters)
    /// </summary>
    public double? OffsetApplied { get; set; }

    /// <summary>
    /// Legacy: Corrected depth (kept for compatibility)
    /// </summary>
    public double? CorrectedDepth { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // STATUS FLAGS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Point is excluded from processing/export
    /// </summary>
    public bool IsExcluded { get; set; }

    /// <summary>
    /// Point was interpolated (not original measurement)
    /// </summary>
    public bool IsInterpolated { get; set; }

    // ═══════════════════════════════════════════════════════════════════
    // RAW DATA PRESERVATION
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Additional data columns from the source file
    /// </summary>
    public Dictionary<string, string> AdditionalData { get; set; } = new();

    /// <summary>
    /// All original columns from source file (alternative storage)
    /// </summary>
    public Dictionary<string, object> RawData { get; set; } = new();

    // ═══════════════════════════════════════════════════════════════════
    // HELPER PROPERTIES
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Flag indicating if this point has valid coordinate data
    /// </summary>
    public bool HasValidCoordinates => !double.IsNaN(Easting) && !double.IsNaN(Northing);

    /// <summary>
    /// Flag indicating if this point has valid depth data
    /// </summary>
    public bool HasValidDepth => Z.HasValue && !double.IsNaN(Z.Value);

    /// <summary>
    /// Get best available Easting (Smoothed if available, otherwise raw)
    /// </summary>
    public double BestEasting => SmoothedEasting ?? Easting;

    /// <summary>
    /// Get best available Northing (Smoothed if available, otherwise raw)
    /// </summary>
    public double BestNorthing => SmoothedNorthing ?? Northing;

    /// <summary>
    /// Get best available depth (Processed > Smoothed > Raw)
    /// </summary>
    public double? BestDepth => ProcessedDepth ?? SmoothedDepth ?? Z;

    // ═══════════════════════════════════════════════════════════════════
    // METHODS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Create a deep copy of this survey point
    /// </summary>
    public SurveyPoint Clone()
    {
        return new SurveyPoint
        {
            Index = Index,
            Timestamp = Timestamp,
            Name = Name,
            Source = Source,
            Easting = Easting,
            Northing = Northing,
            Z = Z,
            DepthSource = DepthSource,
            Altitude = Altitude,
            Heading = Heading,
            SmoothedEasting = SmoothedEasting,
            SmoothedNorthing = SmoothedNorthing,
            SmoothedDepth = SmoothedDepth,
            SmoothedAltitude = SmoothedAltitude,
            ProcessedDepth = ProcessedDepth,
            Kp = Kp,
            Dcc = Dcc,
            TideCorrection = TideCorrection,
            DraftCorrection = DraftCorrection,
            OffsetApplied = OffsetApplied,
            CorrectedDepth = CorrectedDepth,
            IsExcluded = IsExcluded,
            IsInterpolated = IsInterpolated,
            AdditionalData = new Dictionary<string, string>(AdditionalData),
            RawData = new Dictionary<string, object>(RawData)
        };
    }

    public override string ToString()
    {
        string kpStr = Kp.HasValue ? $"KP:{Kp:F6}" : "KP:--";
        string zStr = ProcessedDepth.HasValue ? $"Z:{ProcessedDepth:F2}" : "Z:--";
        string timeStr = Timestamp.HasValue ? Timestamp.Value.ToString("HH:mm:ss") : "--:--:--";
        return $"[{Index}] {timeStr} X:{X:F2} Y:{Y:F2} {zStr} {kpStr}";
    }
}
