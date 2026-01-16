namespace FathomOS.Core.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Survey types supported by the application
/// </summary>
public enum SurveyType
{
    Seabed,
    RovDynamic,
    Pipelay,
    EFL,        // Engineering Final Lay
    SFL,        // Structure Final Lay / Seabed Final Lay
    Umbilical,
    Cable,
    Touchdown,
    AsBuilt,
    PreLay,
    PostLay,
    FreeSpan,
    Inspection,
    Custom
}

/// <summary>
/// KP/DCC calculation mode
/// </summary>
public enum KpDccMode
{
    Both,       // Calculate both KP and DCC (typical for pipeline)
    KpOnly,     // Calculate KP only
    DccOnly,    // Calculate DCC only
    None        // Skip KP/DCC calculation
}

/// <summary>
/// Coordinate system projection types
/// </summary>
public enum ProjectionType
{
    None,
    UTM_WGS84,
    UTM_NAD27,
    UTM_NAD83,
    StatePlane_NAD27,
    StatePlane_NAD83,
    Custom
}

/// <summary>
/// Coordinate conversion settings
/// </summary>
public class CoordinateConversionSettings
{
    /// <summary>
    /// Whether to apply coordinate conversion
    /// </summary>
    public bool EnableConversion { get; set; } = false;
    
    /// <summary>
    /// Source projection type
    /// </summary>
    public ProjectionType SourceProjection { get; set; } = ProjectionType.None;
    
    /// <summary>
    /// Source zone (e.g., UTM zone number or State Plane zone)
    /// </summary>
    public string SourceZone { get; set; } = string.Empty;
    
    /// <summary>
    /// Source hemisphere (N/S for UTM)
    /// </summary>
    public string SourceHemisphere { get; set; } = "N";
    
    /// <summary>
    /// Target projection type
    /// </summary>
    public ProjectionType TargetProjection { get; set; } = ProjectionType.None;
    
    /// <summary>
    /// Target zone
    /// </summary>
    public string TargetZone { get; set; } = string.Empty;
    
    /// <summary>
    /// Target hemisphere
    /// </summary>
    public string TargetHemisphere { get; set; } = "N";
    
    /// <summary>
    /// Custom EPSG code for source (when using Custom projection)
    /// </summary>
    public int? SourceEpsgCode { get; set; }
    
    /// <summary>
    /// Custom EPSG code for target (when using Custom projection)
    /// </summary>
    public int? TargetEpsgCode { get; set; }
}

/// <summary>
/// Helper class for projection information
/// </summary>
public static class ProjectionHelper
{
    /// <summary>
    /// Get display name for projection type
    /// </summary>
    public static string GetDisplayName(ProjectionType projection)
    {
        return projection switch
        {
            ProjectionType.None => "None (No Conversion)",
            ProjectionType.UTM_WGS84 => "UTM (WGS84)",
            ProjectionType.UTM_NAD27 => "UTM (NAD27)",
            ProjectionType.UTM_NAD83 => "UTM (NAD83)",
            ProjectionType.StatePlane_NAD27 => "State Plane (NAD27)",
            ProjectionType.StatePlane_NAD83 => "State Plane (NAD83)",
            ProjectionType.Custom => "Custom (EPSG Code)",
            _ => projection.ToString()
        };
    }
    
    /// <summary>
    /// Get available UTM zones (1-60)
    /// </summary>
    public static List<string> GetUtmZones()
    {
        var zones = new List<string>();
        for (int i = 1; i <= 60; i++)
        {
            zones.Add(i.ToString());
        }
        return zones;
    }
    
    /// <summary>
    /// Get common State Plane zones for Gulf of Mexico / offshore US
    /// </summary>
    public static List<(string Code, string Name)> GetStatePlaneZones()
    {
        return new List<(string, string)>
        {
            ("TX-27S", "Texas South (4205)"),
            ("TX-27SC", "Texas South Central (4204)"),
            ("TX-27C", "Texas Central (4203)"),
            ("TX-27NC", "Texas North Central (4202)"),
            ("TX-27N", "Texas North (4201)"),
            ("LA-27S", "Louisiana South (1702)"),
            ("LA-27N", "Louisiana North (1701)"),
            ("LA-27OS", "Louisiana Offshore South (1703)"),
            ("MS-27E", "Mississippi East (2302)"),
            ("MS-27W", "Mississippi West (2301)"),
            ("AL-27E", "Alabama East (0102)"),
            ("AL-27W", "Alabama West (0101)"),
            ("FL-27E", "Florida East (0902)"),
            ("FL-27W", "Florida West (0902)"),
            ("FL-27N", "Florida North (0903)"),
        };
    }
}

/// <summary>
/// Processing options for the survey data
/// </summary>
public class ProcessingOptions
{
    public bool ApplyTidalCorrections { get; set; } = false;
    public bool ApplyVerticalOffsets { get; set; } = false;
    public bool FitSplineToTrack { get; set; } = false;
    public double PointInterval { get; set; } = 1.0; // meters
    
    /// <summary>
    /// KP/DCC calculation mode
    /// </summary>
    public KpDccMode KpDccMode { get; set; } = KpDccMode.Both;
    
    /// <summary>
    /// Legacy property for compatibility
    /// </summary>
    [JsonIgnore]
    public bool CalculateKpDcc 
    { 
        get => KpDccMode != KpDccMode.None;
        set => KpDccMode = value ? KpDccMode.Both : KpDccMode.None;
    }
    
    /// <summary>
    /// Bathy to Altimeter offset (feet) - nullable, blank by default
    /// </summary>
    public double? BathyToAltimeterOffset { get; set; } = null;
    
    /// <summary>
    /// Bathy to ROV Reference offset (feet) - nullable, blank by default
    /// </summary>
    public double? BathyToRovRefOffset { get; set; } = null;
    
    /// <summary>
    /// Depth exaggeration factor for CAD visualization
    /// </summary>
    public double DepthExaggeration { get; set; } = 10.0;
}

/// <summary>
/// Helper class for survey type configuration
/// </summary>
public static class SurveyTypeConfig
{
    /// <summary>
    /// Get recommended KP/DCC mode based on survey type
    /// </summary>
    public static KpDccMode GetRecommendedKpDccMode(SurveyType surveyType)
    {
        return surveyType switch
        {
            SurveyType.Pipelay => KpDccMode.Both,
            SurveyType.EFL => KpDccMode.Both,
            SurveyType.SFL => KpDccMode.Both,
            SurveyType.Umbilical => KpDccMode.Both,
            SurveyType.Cable => KpDccMode.Both,
            SurveyType.Touchdown => KpDccMode.Both,
            SurveyType.AsBuilt => KpDccMode.Both,
            SurveyType.PreLay => KpDccMode.KpOnly,
            SurveyType.PostLay => KpDccMode.Both,
            SurveyType.FreeSpan => KpDccMode.Both,
            SurveyType.Inspection => KpDccMode.KpOnly,
            SurveyType.Seabed => KpDccMode.KpOnly,
            SurveyType.RovDynamic => KpDccMode.Both,
            SurveyType.Custom => KpDccMode.Both,
            _ => KpDccMode.Both
        };
    }

    /// <summary>
    /// Get display name for survey type
    /// </summary>
    public static string GetDisplayName(SurveyType surveyType)
    {
        return surveyType switch
        {
            SurveyType.Seabed => "Seabed Survey",
            SurveyType.RovDynamic => "ROV Dynamic Survey",
            SurveyType.Pipelay => "Pipelay Survey",
            SurveyType.EFL => "EFL (Engineering Final Lay)",
            SurveyType.SFL => "SFL (Seabed Final Lay)",
            SurveyType.Umbilical => "Umbilical Survey",
            SurveyType.Cable => "Cable Survey",
            SurveyType.Touchdown => "Touchdown Monitoring",
            SurveyType.AsBuilt => "As-Built Survey",
            SurveyType.PreLay => "Pre-Lay Survey",
            SurveyType.PostLay => "Post-Lay Survey",
            SurveyType.FreeSpan => "Free Span Survey",
            SurveyType.Inspection => "Inspection Survey",
            SurveyType.Custom => "Custom Survey",
            _ => surveyType.ToString()
        };
    }
}

/// <summary>
/// Output format options
/// </summary>
public class OutputOptions
{
    public string OutputFolder { get; set; } = string.Empty;
    
    // Text file options
    public bool ExportTextFile { get; set; } = true;
    public string TextFormat { get; set; } = "CSV"; // CSV, Tab, Fixed
    public bool TextIncludeHeader { get; set; } = true;
    
    // Excel options
    public bool ExportExcel { get; set; } = true;
    public bool ExcelIncludeRawData { get; set; } = true;
    public bool ExcelIncludeCalculations { get; set; } = true;
    public bool ExcelApplyFormatting { get; set; } = true;
    
    // CAD options
    public bool ExportDxf { get; set; } = true;
    public bool ExportCadScript { get; set; } = true;
    public string DwgTemplatePath { get; set; } = string.Empty;
    public double KpLabelInterval { get; set; } = 1.0; // km
    
    // 3D Polyline options
    public bool Export3DPolyline { get; set; } = true;
    public double DepthExaggeration { get; set; } = 10.0;
    
    // Raw data export
    public bool ExportRawData { get; set; } = false;
    
    // PDF options
    public bool ExportPdfReport { get; set; } = false;
}

/// <summary>
/// Complete project settings and state
/// </summary>
public class Project
{
    /// <summary>
    /// Project file version for compatibility
    /// </summary>
    public int FileVersion { get; set; } = 1;

    /// <summary>
    /// Date/time the project was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Date/time the project was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    // Step 1: Project Information
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string ProcessorName { get; set; } = string.Empty;
    public string? ProductName { get; set; } = string.Empty;
    public string? RovName { get; set; } = string.Empty;
    public DateTime? SurveyDate { get; set; } = DateTime.Today;
    public SurveyType SurveyType { get; set; } = SurveyType.Seabed;
    public string CoordinateSystem { get; set; } = string.Empty;
    public LengthUnit CoordinateUnit { get; set; } = LengthUnit.USSurveyFeet;
    
    /// <summary>
    /// Input data coordinate units (source survey data)
    /// </summary>
    public LengthUnit InputUnit { get; set; } = LengthUnit.USSurveyFeet;
    
    /// <summary>
    /// Output data coordinate units (exported listings)
    /// </summary>
    public LengthUnit OutputUnit { get; set; } = LengthUnit.USSurveyFeet;
    
    public LengthUnit KpUnit { get; set; } = LengthUnit.Kilometer;
    
    /// <summary>
    /// Coordinate conversion settings
    /// </summary>
    public CoordinateConversionSettings CoordinateConversion { get; set; } = new();

    // Step 2: Route File
    public string RouteFilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional field layout DXF file for background display
    /// </summary>
    public string FieldLayoutDxfPath { get; set; } = string.Empty;
    
    // Step 3: Survey Data Files (supports batch processing)
    public List<string> SurveyDataFiles { get; set; } = new();
    public ColumnMapping ColumnMapping { get; set; } = new();
    public string SelectedDepthColumn { get; set; } = string.Empty;
    public string SelectedAltitudeColumn { get; set; } = string.Empty;

    // Step 5: Tide & Corrections
    public string TideFilePath { get; set; } = string.Empty;
    public bool UseFeetForTide { get; set; } = false; // Convert tide from meters to feet
    public ProcessingOptions ProcessingOptions { get; set; } = new();
    
    /// <summary>
    /// Survey fixes (known points)
    /// </summary>
    public List<SurveyFix> SurveyFixes { get; set; } = new();

    // Step 7: Output Options
    public OutputOptions OutputOptions { get; set; } = new();

    /// <summary>
    /// Path to the project file (set when saved/loaded)
    /// </summary>
    [JsonIgnore]
    public string? ProjectFilePath { get; set; }

    /// <summary>
    /// Create a display name for the project
    /// </summary>
    [JsonIgnore]
    public string DisplayName => string.IsNullOrEmpty(ProjectName) 
        ? "Untitled Project" 
        : ProjectName;

    /// <summary>
    /// Validate project settings
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(ProjectName))
            issues.Add("Project name is required");

        if (string.IsNullOrWhiteSpace(RouteFilePath))
            issues.Add("Route file is required");
        else if (!File.Exists(RouteFilePath))
            issues.Add($"Route file not found: {RouteFilePath}");

        if (SurveyDataFiles.Count == 0)
            issues.Add("At least one survey data file is required");
        else
        {
            foreach (var file in SurveyDataFiles)
            {
                if (!File.Exists(file))
                    issues.Add($"Survey file not found: {file}");
            }
        }

        if (!string.IsNullOrEmpty(TideFilePath) && !File.Exists(TideFilePath))
            issues.Add($"Tide file not found: {TideFilePath}");

        if (string.IsNullOrWhiteSpace(OutputOptions.OutputFolder))
            issues.Add("Output folder is required");

        return issues;
    }

    /// <summary>
    /// Clone this project
    /// </summary>
    public Project Clone()
    {
        var json = JsonSerializer.Serialize(this);
        var clone = JsonSerializer.Deserialize<Project>(json)!;
        clone.ProjectFilePath = null; // Don't copy the file path
        return clone;
    }
}

/// <summary>
/// Survey fix point (known position/depth)
/// </summary>
public class SurveyFix
{
    public string Name { get; set; } = string.Empty;
    public double Kp { get; set; }
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double? Depth { get; set; }
    public string Notes { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Name}: KP {Kp:F6}, E:{Easting:F2} N:{Northing:F2}";
    }
}
