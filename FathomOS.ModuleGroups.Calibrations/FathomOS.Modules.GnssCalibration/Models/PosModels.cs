namespace FathomOS.Modules.GnssCalibration.Models;

/// <summary>
/// Supported POS file formats.
/// </summary>
public enum PosFileFormat
{
    /// <summary>Auto-detect format from file content.</summary>
    AutoDetect,
    
    /// <summary>
    /// RTKLIB format: %GPST, Lat, Lon, Height, Q, ns, sdn, sde, sdu, ...
    /// Header starts with '%' character.
    /// </summary>
    RTKLIB,
    
    /// <summary>
    /// NovAtel/Waypoint format: GPSWeek, GPSSec, Lat, Lon, Height, sigN, sigE, sigU
    /// Comma-separated with GPS time.
    /// </summary>
    NovAtel,
    
    /// <summary>
    /// Trimble/Leica format: Custom CSV with UTM coordinates.
    /// Date, Time, Easting, Northing, Height, ...
    /// </summary>
    Trimble,
    
    /// <summary>
    /// Generic CSV format with configurable columns.
    /// User specifies which columns contain which data.
    /// </summary>
    CustomCSV
}

/// <summary>
/// Coordinate system used in the POS file.
/// </summary>
public enum PosCoordinateSystem
{
    /// <summary>Geographic coordinates (Latitude/Longitude in degrees).</summary>
    Geographic,
    
    /// <summary>Projected coordinates (Easting/Northing in meters, e.g., UTM).</summary>
    Projected,
    
    /// <summary>Auto-detect based on value ranges.</summary>
    AutoDetect
}

/// <summary>
/// Time format used in the POS file.
/// </summary>
public enum PosTimeFormat
{
    /// <summary>Auto-detect time format.</summary>
    AutoDetect,
    
    /// <summary>GPS Week + Seconds of Week (e.g., 2345, 356400.000)</summary>
    GpsWeekSeconds,
    
    /// <summary>UTC DateTime string (e.g., 2024/12/19 14:30:00.000)</summary>
    UtcDateTime,
    
    /// <summary>Unix timestamp (seconds since 1970-01-01)</summary>
    UnixTimestamp,
    
    /// <summary>Modified Julian Date</summary>
    ModifiedJulianDate,
    
    /// <summary>ISO 8601 format (e.g., 2024-12-19T14:30:00.000Z)</summary>
    Iso8601
}

/// <summary>
/// Quality indicator for POS solution.
/// </summary>
public enum PosSolutionQuality
{
    /// <summary>Unknown or not specified.</summary>
    Unknown = 0,
    
    /// <summary>Fixed RTK solution (highest quality).</summary>
    Fix = 1,
    
    /// <summary>Float RTK solution.</summary>
    Float = 2,
    
    /// <summary>SBAS corrected.</summary>
    SBAS = 3,
    
    /// <summary>DGPS corrected.</summary>
    DGPS = 4,
    
    /// <summary>Single point positioning (lowest quality).</summary>
    Single = 5,
    
    /// <summary>PPP (Precise Point Positioning).</summary>
    PPP = 6
}

/// <summary>
/// Represents a single position record from a POS file.
/// </summary>
public class PosDataPoint
{
    /// <summary>Record index (1-based row number).</summary>
    public int Index { get; set; }
    
    /// <summary>Timestamp of the position.</summary>
    public DateTime DateTime { get; set; }
    
    /// <summary>GPS Week number (if available).</summary>
    public int? GpsWeek { get; set; }
    
    /// <summary>GPS Seconds of Week (if available).</summary>
    public double? GpsSecondsOfWeek { get; set; }
    
    // === Geographic Coordinates ===
    
    /// <summary>Latitude in degrees (WGS84).</summary>
    public double? Latitude { get; set; }
    
    /// <summary>Longitude in degrees (WGS84).</summary>
    public double? Longitude { get; set; }
    
    // === Projected Coordinates ===
    
    /// <summary>Easting in meters (UTM or local grid).</summary>
    public double Easting { get; set; }
    
    /// <summary>Northing in meters (UTM or local grid).</summary>
    public double Northing { get; set; }
    
    /// <summary>Ellipsoidal height in meters.</summary>
    public double Height { get; set; }
    
    // === Accuracy/Quality ===
    
    /// <summary>Solution quality indicator.</summary>
    public PosSolutionQuality Quality { get; set; } = PosSolutionQuality.Unknown;
    
    /// <summary>Number of satellites used.</summary>
    public int? SatelliteCount { get; set; }
    
    /// <summary>Standard deviation in North direction (meters).</summary>
    public double? SigmaNorth { get; set; }
    
    /// <summary>Standard deviation in East direction (meters).</summary>
    public double? SigmaEast { get; set; }
    
    /// <summary>Standard deviation in Up/Height direction (meters).</summary>
    public double? SigmaUp { get; set; }
    
    /// <summary>PDOP (Position Dilution of Precision).</summary>
    public double? PDOP { get; set; }
    
    /// <summary>HDOP (Horizontal Dilution of Precision).</summary>
    public double? HDOP { get; set; }
    
    /// <summary>VDOP (Vertical Dilution of Precision).</summary>
    public double? VDOP { get; set; }
    
    // === Computed Properties ===
    
    /// <summary>Horizontal accuracy (2D RMS of SigmaNorth and SigmaEast).</summary>
    public double? HorizontalAccuracy => 
        SigmaNorth.HasValue && SigmaEast.HasValue
            ? Math.Sqrt(SigmaNorth.Value * SigmaNorth.Value + SigmaEast.Value * SigmaEast.Value)
            : null;
    
    /// <summary>3D accuracy (RMS of all three sigma values).</summary>
    public double? Accuracy3D =>
        SigmaNorth.HasValue && SigmaEast.HasValue && SigmaUp.HasValue
            ? Math.Sqrt(SigmaNorth.Value * SigmaNorth.Value + 
                       SigmaEast.Value * SigmaEast.Value + 
                       SigmaUp.Value * SigmaUp.Value)
            : null;
    
    /// <summary>Check if point has valid coordinates.</summary>
    public bool IsValid => !double.IsNaN(Easting) && !double.IsNaN(Northing) && 
                          !double.IsInfinity(Easting) && !double.IsInfinity(Northing);
    
    /// <summary>Check if point has geographic coordinates.</summary>
    public bool HasGeographic => Latitude.HasValue && Longitude.HasValue;
    
    /// <summary>Check if point has accuracy information.</summary>
    public bool HasAccuracy => SigmaNorth.HasValue || SigmaEast.HasValue || SigmaUp.HasValue;
    
    // === Status Flags ===
    
    /// <summary>Whether this point was excluded from analysis.</summary>
    public bool IsExcluded { get; set; }
    
    /// <summary>Reason for exclusion (if excluded).</summary>
    public string? ExclusionReason { get; set; }
    
    /// <summary>Raw line from file (for debugging).</summary>
    public string? RawLine { get; set; }
}

/// <summary>
/// Configuration for POS file parsing.
/// </summary>
public class PosParseSettings
{
    /// <summary>File format to use (or AutoDetect).</summary>
    public PosFileFormat Format { get; set; } = PosFileFormat.AutoDetect;
    
    /// <summary>Coordinate system in the file.</summary>
    public PosCoordinateSystem CoordinateSystem { get; set; } = PosCoordinateSystem.AutoDetect;
    
    /// <summary>Time format in the file.</summary>
    public PosTimeFormat TimeFormat { get; set; } = PosTimeFormat.AutoDetect;
    
    /// <summary>Field delimiter (for CSV formats).</summary>
    public char Delimiter { get; set; } = ',';
    
    /// <summary>Number of header lines to skip.</summary>
    public int HeaderLines { get; set; } = 1;
    
    /// <summary>Comment line prefix (lines starting with this are skipped).</summary>
    public string CommentPrefix { get; set; } = "#";
    
    // === Column Indices for Custom CSV (0-based, -1 = not used) ===
    
    /// <summary>Column index for date (or combined datetime).</summary>
    public int DateColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for time (if separate from date).</summary>
    public int TimeColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for GPS Week.</summary>
    public int GpsWeekColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for GPS Seconds of Week.</summary>
    public int GpsSecColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for Latitude or Easting.</summary>
    public int LatOrEastColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for Longitude or Northing.</summary>
    public int LonOrNorthColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for Height.</summary>
    public int HeightColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for Quality indicator.</summary>
    public int QualityColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for Satellite count.</summary>
    public int SatCountColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for North sigma.</summary>
    public int SigmaNorthColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for East sigma.</summary>
    public int SigmaEastColumnIndex { get; set; } = -1;
    
    /// <summary>Column index for Up sigma.</summary>
    public int SigmaUpColumnIndex { get; set; } = -1;
    
    // === Date/Time Format Strings ===
    
    /// <summary>Date format string (e.g., "yyyy/MM/dd").</summary>
    public string DateFormat { get; set; } = "yyyy/MM/dd";
    
    /// <summary>Time format string (e.g., "HH:mm:ss.fff").</summary>
    public string TimeFormatString { get; set; } = "HH:mm:ss.fff";
    
    // === UTM Settings (for coordinate conversion) ===
    
    /// <summary>UTM Zone number (1-60) for coordinate conversion.</summary>
    public int UtmZone { get; set; } = 32;
    
    /// <summary>UTM Hemisphere (true = North, false = South).</summary>
    public bool UtmNorthernHemisphere { get; set; } = true;
    
    // === Quality Filter Settings ===
    
    /// <summary>Minimum solution quality to accept (0 = accept all).</summary>
    public int MinQuality { get; set; } = 0;
    
    /// <summary>Maximum horizontal accuracy to accept (meters, 0 = no limit).</summary>
    public double MaxHorizontalAccuracy { get; set; } = 0;
    
    /// <summary>Minimum satellite count to accept (0 = no limit).</summary>
    public int MinSatellites { get; set; } = 0;
    
    /// <summary>Create default settings for a specific format.</summary>
    public static PosParseSettings ForFormat(PosFileFormat format)
    {
        return format switch
        {
            PosFileFormat.RTKLIB => new PosParseSettings
            {
                Format = PosFileFormat.RTKLIB,
                CoordinateSystem = PosCoordinateSystem.Geographic,
                TimeFormat = PosTimeFormat.UtcDateTime,
                Delimiter = ' ',
                CommentPrefix = "%",
                HeaderLines = 0  // RTKLIB uses % prefix for header
            },
            PosFileFormat.NovAtel => new PosParseSettings
            {
                Format = PosFileFormat.NovAtel,
                CoordinateSystem = PosCoordinateSystem.Geographic,
                TimeFormat = PosTimeFormat.GpsWeekSeconds,
                Delimiter = ',',
                HeaderLines = 1
            },
            PosFileFormat.Trimble => new PosParseSettings
            {
                Format = PosFileFormat.Trimble,
                CoordinateSystem = PosCoordinateSystem.Projected,
                TimeFormat = PosTimeFormat.UtcDateTime,
                Delimiter = ',',
                HeaderLines = 1
            },
            _ => new PosParseSettings()
        };
    }
}

/// <summary>
/// Result of parsing a POS file.
/// </summary>
public class PosParseResult
{
    /// <summary>Whether parsing was successful.</summary>
    public bool Success { get; set; }
    
    /// <summary>Error message if parsing failed.</summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>Warnings generated during parsing.</summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>Parsed data points.</summary>
    public List<PosDataPoint> Points { get; set; } = new();
    
    /// <summary>Detected file format.</summary>
    public PosFileFormat DetectedFormat { get; set; }
    
    /// <summary>Detected coordinate system.</summary>
    public PosCoordinateSystem DetectedCoordinateSystem { get; set; }
    
    /// <summary>Detected time format.</summary>
    public PosTimeFormat DetectedTimeFormat { get; set; }
    
    /// <summary>Column headers from file (if present).</summary>
    public List<string> Headers { get; set; } = new();
    
    /// <summary>Start time of data.</summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>End time of data.</summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>Total records in file.</summary>
    public int TotalRecords { get; set; }
    
    /// <summary>Records successfully parsed.</summary>
    public int ParsedRecords => Points.Count;
    
    /// <summary>Records skipped due to quality filter.</summary>
    public int FilteredRecords { get; set; }
    
    /// <summary>Records with parse errors.</summary>
    public int ErrorRecords { get; set; }
    
    /// <summary>Duration of the dataset.</summary>
    public TimeSpan? Duration => StartTime.HasValue && EndTime.HasValue 
        ? EndTime.Value - StartTime.Value 
        : null;
    
    /// <summary>Average data rate (Hz).</summary>
    public double? DataRate => Duration.HasValue && Duration.Value.TotalSeconds > 0 && Points.Count > 1
        ? (Points.Count - 1) / Duration.Value.TotalSeconds
        : null;
}
