namespace FathomOS.Core.Models;

/// <summary>
/// Configuration for mapping NPD file columns to survey data fields
/// </summary>
public class ColumnMapping
{
    /// <summary>
    /// Name of this mapping configuration (e.g., "NaviPac Default", "QINSy ROV")
    /// </summary>
    public string Name { get; set; } = "Custom";

    /// <summary>
    /// Description of when to use this mapping
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Pattern to match for the Time/DateTime column header
    /// </summary>
    public string TimeColumnPattern { get; set; } = "Time";

    /// <summary>
    /// Pattern to match for the Easting column header
    /// </summary>
    public string EastingColumnPattern { get; set; } = "East";

    /// <summary>
    /// Pattern to match for the Northing column header
    /// </summary>
    public string NorthingColumnPattern { get; set; } = "North";

    /// <summary>
    /// Pattern to match for the Depth column header (or exact column name)
    /// </summary>
    public string DepthColumnPattern { get; set; } = "Bathy";

    /// <summary>
    /// Pattern to match for the Altitude column header
    /// </summary>
    public string AltitudeColumnPattern { get; set; } = "Alt";

    /// <summary>
    /// Pattern to match for the Heading column header
    /// </summary>
    public string HeadingColumnPattern { get; set; } = "Heading";

    /// <summary>
    /// Whether this file uses the NaviPac date/time trick
    /// (header shows "Time" as one column but data has "date,time" as two values)
    /// </summary>
    public bool HasDateTimeSplit { get; set; } = true;

    /// <summary>
    /// Date format string for parsing (e.g., "dd/MM/yyyy" or "MM/dd/yyyy")
    /// </summary>
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    /// <summary>
    /// Time format string for parsing (e.g., "HH:mm:ss")
    /// </summary>
    public string TimeFormat { get; set; } = "HH:mm:ss";

    /// <summary>
    /// Specific column indices (0-based) if auto-detection fails
    /// Use -1 for auto-detect
    /// </summary>
    public int TimeColumnIndex { get; set; } = -1;
    public int EastingColumnIndex { get; set; } = -1;
    public int NorthingColumnIndex { get; set; } = -1;
    public int DepthColumnIndex { get; set; } = -1;
    public int AltitudeColumnIndex { get; set; } = -1;
    public int HeadingColumnIndex { get; set; } = -1;

    /// <summary>
    /// Create a deep copy of this mapping
    /// </summary>
    public ColumnMapping Clone()
    {
        return new ColumnMapping
        {
            Name = Name,
            Description = Description,
            TimeColumnPattern = TimeColumnPattern,
            EastingColumnPattern = EastingColumnPattern,
            NorthingColumnPattern = NorthingColumnPattern,
            DepthColumnPattern = DepthColumnPattern,
            AltitudeColumnPattern = AltitudeColumnPattern,
            HeadingColumnPattern = HeadingColumnPattern,
            HasDateTimeSplit = HasDateTimeSplit,
            DateFormat = DateFormat,
            TimeFormat = TimeFormat,
            TimeColumnIndex = TimeColumnIndex,
            EastingColumnIndex = EastingColumnIndex,
            NorthingColumnIndex = NorthingColumnIndex,
            DepthColumnIndex = DepthColumnIndex,
            AltitudeColumnIndex = AltitudeColumnIndex,
            HeadingColumnIndex = HeadingColumnIndex
        };
    }
}

/// <summary>
/// Pre-defined column mapping templates for common survey systems
/// </summary>
public static class ColumnMappingTemplates
{
    /// <summary>
    /// Default NaviPac HD11 Sprint template
    /// </summary>
    public static ColumnMapping NaviPacDefault => new()
    {
        Name = "NaviPac Default",
        Description = "NaviPac with HD11 Sprint positioning system",
        TimeColumnPattern = "Time",
        EastingColumnPattern = "East",
        NorthingColumnPattern = "North",
        DepthColumnPattern = "Bathy",
        AltitudeColumnPattern = "Alt",
        HeadingColumnPattern = "Heading",
        HasDateTimeSplit = true,
        DateFormat = "dd/MM/yyyy",
        TimeFormat = "HH:mm:ss"
    };

    /// <summary>
    /// QINSy ROV survey template
    /// </summary>
    public static ColumnMapping QINSyRov => new()
    {
        Name = "QINSy ROV",
        Description = "QINSy system for ROV surveys",
        TimeColumnPattern = "Time",
        EastingColumnPattern = "Easting",
        NorthingColumnPattern = "Northing",
        DepthColumnPattern = "Depth",
        AltitudeColumnPattern = "Altitude",
        HeadingColumnPattern = "Heading",
        HasDateTimeSplit = false,  // QINSy typically uses combined datetime
        DateFormat = "yyyy-MM-dd",
        TimeFormat = "HH:mm:ss.fff"
    };

    /// <summary>
    /// Generic CSV with standard column names
    /// </summary>
    public static ColumnMapping GenericCsv => new()
    {
        Name = "Generic CSV",
        Description = "Generic CSV with Easting, Northing, Depth columns",
        TimeColumnPattern = "Time|DateTime|Date",
        EastingColumnPattern = "Easting|East|X|E",
        NorthingColumnPattern = "Northing|North|Y|N",
        DepthColumnPattern = "Depth|Z|Elevation",
        AltitudeColumnPattern = "Altitude|Alt|Height",
        HeadingColumnPattern = "Heading|Hdg|Azimuth",
        HasDateTimeSplit = false,
        DateFormat = "yyyy-MM-dd",
        TimeFormat = "HH:mm:ss"
    };

    /// <summary>
    /// Get all available templates
    /// </summary>
    public static IReadOnlyList<ColumnMapping> AllTemplates => new List<ColumnMapping>
    {
        NaviPacDefault,
        QINSyRov,
        GenericCsv
    };

    /// <summary>
    /// Find a template by name
    /// </summary>
    public static ColumnMapping? FindByName(string name)
    {
        return AllTemplates.FirstOrDefault(t => 
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
