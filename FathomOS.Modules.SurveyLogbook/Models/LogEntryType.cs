// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/LogEntryType.cs
// Purpose: Enumeration of all supported log entry types
// ============================================================================

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Enumeration of all log entry types captured by the Survey Logbook.
/// Each type corresponds to a specific data source or user action.
/// </summary>
public enum LogEntryType
{
    // ========================================================================
    // DVR Recording Events (from VisualWorks folder monitoring)
    // ========================================================================
    
    /// <summary>DVR recording session started</summary>
    DvrRecordingStart = 100,
    
    /// <summary>DVR recording session ended</summary>
    DvrRecordingEnd = 101,
    
    /// <summary>DVR video clip created</summary>
    DvrClipCreated = 102,
    
    /// <summary>DVR still image captured</summary>
    DvrImageCaptured = 103,
    
    // ========================================================================
    // Position Fix Events (from .npc file monitoring)
    // ========================================================================
    
    /// <summary>Position fix taken</summary>
    PositionFix = 200,
    
    /// <summary>Calibration fix (GPS, Gyro, MRU, etc.)</summary>
    CalibrationFix = 201,
    
    /// <summary>Verification fix</summary>
    VerificationFix = 202,
    
    /// <summary>Set Easting/Northing calibration</summary>
    SetEastingNorthing = 203,
    
    // ========================================================================
    // NaviPac Events (from TCP connection)
    // ========================================================================
    
    /// <summary>NaviPac logging session started</summary>
    NaviPacLoggingStart = 300,
    
    /// <summary>NaviPac logging session ended</summary>
    NaviPacLoggingEnd = 301,
    
    /// <summary>NaviScan logging session started</summary>
    NaviScanLoggingStart = 302,
    
    /// <summary>NaviScan logging session ended</summary>
    NaviScanLoggingEnd = 303,
    
    /// <summary>ROVINS logging session started</summary>
    RovinsLoggingStart = 304,
    
    /// <summary>ROVINS logging session ended</summary>
    RovinsLoggingEnd = 305,
    
    /// <summary>Runline change event</summary>
    RunlineChange = 310,
    
    /// <summary>Event marker from NaviPac</summary>
    NaviPacEvent = 320,
    
    /// <summary>Position update from NaviPac UDO (real-time stream)</summary>
    PositionUpdate = 321,
    
    // ========================================================================
    // Waypoint Events (from .wp2 file monitoring)
    // ========================================================================
    
    /// <summary>Waypoint added</summary>
    WaypointAdded = 400,
    
    /// <summary>Waypoint modified</summary>
    WaypointModified = 401,
    
    /// <summary>Waypoint deleted</summary>
    WaypointDeleted = 402,
    
    // ========================================================================
    // Vessel Events
    // ========================================================================
    
    /// <summary>Vessel position update (midnight position)</summary>
    VesselPosition = 500,
    
    /// <summary>Vessel departed location</summary>
    VesselDeparture = 501,
    
    /// <summary>Vessel arrived at location</summary>
    VesselArrival = 502,
    
    /// <summary>Vessel moored</summary>
    VesselMoored = 503,
    
    /// <summary>Vessel in transit</summary>
    VesselTransit = 504,
    
    // ========================================================================
    // ROV Events
    // ========================================================================
    
    /// <summary>ROV deployed/launched</summary>
    RovDeployed = 600,
    
    /// <summary>ROV recovered</summary>
    RovRecovered = 601,
    
    /// <summary>ROV on bottom</summary>
    RovOnBottom = 602,
    
    /// <summary>ROV off bottom</summary>
    RovOffBottom = 603,
    
    // ========================================================================
    // Survey Operations
    // ========================================================================
    
    /// <summary>Survey operation started</summary>
    SurveyStart = 700,
    
    /// <summary>Survey operation ended</summary>
    SurveyEnd = 701,
    
    /// <summary>Survey line started</summary>
    SurveyLineStart = 702,
    
    /// <summary>Survey line ended</summary>
    SurveyLineEnd = 703,
    
    /// <summary>Calibration operation started</summary>
    CalibrationStart = 710,
    
    /// <summary>Calibration operation ended</summary>
    CalibrationEnd = 711,
    
    /// <summary>Patch test started</summary>
    PatchTestStart = 720,
    
    /// <summary>Patch test ended</summary>
    PatchTestEnd = 721,
    
    // ========================================================================
    // Manual/User Events
    // ========================================================================
    
    /// <summary>Manual log entry by user</summary>
    ManualEntry = 900,
    
    /// <summary>User comment or note</summary>
    UserComment = 901,
    
    /// <summary>General comment entry</summary>
    Comment = 902,
    
    /// <summary>Equipment setup event</summary>
    EquipmentSetup = 903,
    
    /// <summary>Equipment failure event</summary>
    EquipmentFailure = 904,
    
    /// <summary>Weather condition update</summary>
    WeatherCondition = 905,
    
    /// <summary>Vessel movement event</summary>
    VesselMovement = 906,
    
    /// <summary>Personnel change event</summary>
    PersonnelChange = 907,
    
    /// <summary>Safety incident event</summary>
    SafetyIncident = 908,
    
    /// <summary>Operation started</summary>
    OperationStart = 909,
    
    /// <summary>Operation ended</summary>
    OperationEnd = 910,
    
    /// <summary>Shift change event</summary>
    ShiftChange = 920,
    
    /// <summary>Warning message</summary>
    Warning = 990,
    
    /// <summary>Error message</summary>
    Error = 991,
    
    /// <summary>System event (connection, error, etc.)</summary>
    SystemEvent = 999
}

/// <summary>
/// Extension methods for LogEntryType enumeration.
/// </summary>
public static class LogEntryTypeExtensions
{
    /// <summary>
    /// Gets a human-readable display name for the log entry type.
    /// </summary>
    public static string GetDisplayName(this LogEntryType type) => type switch
    {
        LogEntryType.DvrRecordingStart => "DVR Recording Started",
        LogEntryType.DvrRecordingEnd => "DVR Recording Ended",
        LogEntryType.DvrClipCreated => "DVR Clip Created",
        LogEntryType.DvrImageCaptured => "DVR Image Captured",
        
        LogEntryType.PositionFix => "Position Fix",
        LogEntryType.CalibrationFix => "Calibration Fix",
        LogEntryType.VerificationFix => "Verification Fix",
        LogEntryType.SetEastingNorthing => "Set Easting/Northing",
        
        LogEntryType.NaviPacLoggingStart => "NaviPac Logging Started",
        LogEntryType.NaviPacLoggingEnd => "NaviPac Logging Ended",
        LogEntryType.NaviScanLoggingStart => "NaviScan Logging Started",
        LogEntryType.NaviScanLoggingEnd => "NaviScan Logging Ended",
        LogEntryType.RovinsLoggingStart => "ROVINS Logging Started",
        LogEntryType.RovinsLoggingEnd => "ROVINS Logging Ended",
        LogEntryType.RunlineChange => "Runline Changed",
        LogEntryType.NaviPacEvent => "NaviPac Event",
        LogEntryType.PositionUpdate => "Position Update",
        
        LogEntryType.WaypointAdded => "Waypoint Added",
        LogEntryType.WaypointModified => "Waypoint Modified",
        LogEntryType.WaypointDeleted => "Waypoint Deleted",
        
        LogEntryType.VesselPosition => "Vessel Position",
        LogEntryType.VesselDeparture => "Vessel Departed",
        LogEntryType.VesselArrival => "Vessel Arrived",
        LogEntryType.VesselMoored => "Vessel Moored",
        LogEntryType.VesselTransit => "Vessel In Transit",
        
        LogEntryType.RovDeployed => "ROV Deployed",
        LogEntryType.RovRecovered => "ROV Recovered",
        LogEntryType.RovOnBottom => "ROV On Bottom",
        LogEntryType.RovOffBottom => "ROV Off Bottom",
        
        LogEntryType.SurveyStart => "Survey Started",
        LogEntryType.SurveyEnd => "Survey Ended",
        LogEntryType.SurveyLineStart => "Survey Line Started",
        LogEntryType.SurveyLineEnd => "Survey Line Ended",
        LogEntryType.CalibrationStart => "Calibration Started",
        LogEntryType.CalibrationEnd => "Calibration Ended",
        LogEntryType.PatchTestStart => "Patch Test Started",
        LogEntryType.PatchTestEnd => "Patch Test Ended",
        
        LogEntryType.ManualEntry => "Manual Entry",
        LogEntryType.UserComment => "User Comment",
        LogEntryType.Comment => "Comment",
        LogEntryType.EquipmentSetup => "Equipment Setup",
        LogEntryType.EquipmentFailure => "Equipment Failure",
        LogEntryType.WeatherCondition => "Weather Condition",
        LogEntryType.VesselMovement => "Vessel Movement",
        LogEntryType.PersonnelChange => "Personnel Change",
        LogEntryType.SafetyIncident => "Safety Incident",
        LogEntryType.OperationStart => "Operation Started",
        LogEntryType.OperationEnd => "Operation Ended",
        LogEntryType.ShiftChange => "Shift Change",
        LogEntryType.Warning => "Warning",
        LogEntryType.Error => "Error",
        LogEntryType.SystemEvent => "System Event",
        
        _ => type.ToString()
    };
    
    /// <summary>
    /// Gets the category of the log entry type.
    /// </summary>
    public static string GetCategory(this LogEntryType type)
    {
        int code = (int)type;
        return code switch
        {
            >= 100 and < 200 => "DVR",
            >= 200 and < 300 => "Position Fix",
            >= 300 and < 400 => "NaviPac",
            >= 400 and < 500 => "Waypoint",
            >= 500 and < 600 => "Vessel",
            >= 600 and < 700 => "ROV",
            >= 700 and < 900 => "Survey",
            >= 900 => "Manual",
            _ => "Other"
        };
    }
    
    /// <summary>
    /// Gets an icon key for the log entry type (for UI display).
    /// </summary>
    public static string GetIconKey(this LogEntryType type)
    {
        int code = (int)type;
        return code switch
        {
            >= 100 and < 200 => "Video",
            >= 200 and < 300 => "Crosshairs",
            >= 300 and < 400 => "Navigation",
            >= 400 and < 500 => "MapMarker",
            >= 500 and < 600 => "Ferry",
            >= 600 and < 700 => "Robot",
            >= 700 and < 900 => "Clipboard",
            >= 900 => "Pencil",
            _ => "Circle"
        };
    }
}
