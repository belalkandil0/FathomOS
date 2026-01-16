// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/SurveyLogFile.cs
// Purpose: .slog file format model - complete survey log export structure
// ============================================================================

using System.Text.Json.Serialization;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Represents a complete Survey Log file (.slog format).
/// This is the root object for serialization/deserialization of survey log files.
/// </summary>
public class SurveyLogFile
{
    /// <summary>
    /// File format version for compatibility checking.
    /// </summary>
    public string FileVersion { get; set; } = "1.0";
    
    /// <summary>
    /// Format type identifier.
    /// </summary>
    public string FormatType { get; set; } = "FathomOS.SurveyLog";
    
    /// <summary>
    /// Date/time when this file was exported.
    /// </summary>
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Name of the person who exported the file.
    /// </summary>
    public string ExportedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Project information.
    /// </summary>
    public ProjectInfo ProjectInfo { get; set; } = new();
    
    /// <summary>
    /// Survey log data container.
    /// </summary>
    public SurveyLogData SurveyLog { get; set; } = new();
    
    /// <summary>
    /// Collection of DPR reports.
    /// </summary>
    public List<DprReport> DprReports { get; set; } = new();
    
    /// <summary>
    /// File metadata for validation and statistics.
    /// </summary>
    public SurveyLogMetadata Metadata { get; set; } = new();
    
    // ========================================================================
    // Computed Properties (for convenience/compatibility)
    // ========================================================================
    
    /// <summary>
    /// Gets all log entries aggregated from all data types.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<SurveyLogEntry> LogEntries
    {
        get
        {
            var entries = new List<SurveyLogEntry>();
            
            // Add manual entries directly
            if (SurveyLog.ManualEntries?.Any() == true)
                entries.AddRange(SurveyLog.ManualEntries);
            
            // Convert other types to SurveyLogEntry
            if (SurveyLog.DvrRecordings?.Any() == true)
            {
                foreach (var dvr in SurveyLog.DvrRecordings)
                {
                    entries.Add(new SurveyLogEntry(LogEntryType.DvrRecordingStart, 
                        $"DVR: {dvr.Description}", dvr.Vehicle, dvr.StartDateTime));
                }
            }
            
            if (SurveyLog.PositionFixes?.Any() == true)
            {
                foreach (var fix in SurveyLog.PositionFixes)
                {
                    entries.Add(new SurveyLogEntry(LogEntryType.PositionFix,
                        $"Fix #{fix.FixNumber}: E={fix.Easting:F2}, N={fix.Northing:F2}",
                        fix.Vehicle, fix.Timestamp));
                }
            }
            
            if (SurveyLog.EivaDataLogs?.Any() == true)
            {
                foreach (var eiva in SurveyLog.EivaDataLogs)
                {
                    entries.Add(new SurveyLogEntry(LogEntryType.NaviPacLoggingStart,
                        $"EIVA: {eiva.FileName} - {eiva.Runline}",
                        eiva.Vehicle, eiva.StartDateTime));
                }
            }
            
            return entries.OrderBy(e => e.Timestamp);
        }
    }
    
    /// <summary>
    /// Gets all position fixes (delegate to SurveyLog).
    /// </summary>
    [JsonIgnore]
    public List<PositionFix> PositionFixes => SurveyLog.PositionFixes ?? new List<PositionFix>();
    
    /// <summary>
    /// Gets all DVR recordings (delegate to SurveyLog).
    /// </summary>
    [JsonIgnore]
    public List<DvrRecording> DvrRecordings => SurveyLog.DvrRecordings ?? new List<DvrRecording>();
    
    /// <summary>
    /// Gets the SurveyLogData (alias for SurveyLog for compatibility).
    /// </summary>
    [JsonIgnore]
    public SurveyLogData SurveyLogData => SurveyLog;
    
    /// <summary>
    /// Gets the project name (convenience accessor).
    /// </summary>
    [JsonIgnore]
    public string ProjectName => ProjectInfo?.ProjectName ?? string.Empty;
    
    /// <summary>
    /// Gets the vessel name (convenience accessor).
    /// </summary>
    [JsonIgnore]
    public string VesselName => ProjectInfo?.Vessel ?? string.Empty;
    
    /// <summary>
    /// Gets the client name (convenience accessor).
    /// </summary>
    [JsonIgnore]
    public string ClientName => ProjectInfo?.Client ?? string.Empty;
    
    /// <summary>
    /// Creates metadata from current content.
    /// </summary>
    public void UpdateMetadata()
    {
        Metadata.TotalEntries = 
            (SurveyLog.DvrRecordings?.Count ?? 0) +
            (SurveyLog.PositionFixes?.Count ?? 0) +
            (SurveyLog.EivaDataLogs?.Count ?? 0) +
            (SurveyLog.ManualEntries?.Count ?? 0);
        
        // Calculate date range
        var allDates = new List<DateTime>();
        
        if (SurveyLog.DvrRecordings?.Any() == true)
            allDates.AddRange(SurveyLog.DvrRecordings.Select(d => d.Date));
        if (SurveyLog.PositionFixes?.Any() == true)
            allDates.AddRange(SurveyLog.PositionFixes.Select(p => p.Date));
        if (SurveyLog.EivaDataLogs?.Any() == true)
            allDates.AddRange(SurveyLog.EivaDataLogs.Select(e => e.Date));
        if (SurveyLog.ManualEntries?.Any() == true)
            allDates.AddRange(SurveyLog.ManualEntries.Select(m => m.Timestamp.Date));
        
        if (allDates.Any())
        {
            Metadata.DateRange = new DateRange
            {
                Start = allDates.Min(),
                End = allDates.Max()
            };
        }
        
        Metadata.DvrRecordingCount = SurveyLog.DvrRecordings?.Count ?? 0;
        Metadata.PositionFixCount = SurveyLog.PositionFixes?.Count ?? 0;
        Metadata.EivaDataLogCount = SurveyLog.EivaDataLogs?.Count ?? 0;
        Metadata.ManualEntryCount = SurveyLog.ManualEntries?.Count ?? 0;
        Metadata.DprReportCount = DprReports?.Count ?? 0;
    }
}

/// <summary>
/// Container for all survey log data types.
/// </summary>
public class SurveyLogData
{
    /// <summary>
    /// DVR recording sessions.
    /// </summary>
    public List<DvrRecording> DvrRecordings { get; set; } = new();
    
    /// <summary>
    /// Position fix records.
    /// </summary>
    public List<PositionFix> PositionFixes { get; set; } = new();
    
    /// <summary>
    /// EIVA NaviPac/NaviScan data logs.
    /// </summary>
    public List<EivaDataLog> EivaDataLogs { get; set; } = new();
    
    /// <summary>
    /// Manual log entries.
    /// </summary>
    public List<SurveyLogEntry> ManualEntries { get; set; } = new();
}

/// <summary>
/// Metadata for the survey log file.
/// </summary>
public class SurveyLogMetadata
{
    /// <summary>
    /// Total number of entries across all types.
    /// </summary>
    public int TotalEntries { get; set; }
    
    /// <summary>
    /// Date range covered by the log.
    /// </summary>
    public DateRange? DateRange { get; set; }
    
    /// <summary>
    /// Number of DVR recording entries.
    /// </summary>
    public int DvrRecordingCount { get; set; }
    
    /// <summary>
    /// Number of position fix entries.
    /// </summary>
    public int PositionFixCount { get; set; }
    
    /// <summary>
    /// Number of EIVA data log entries.
    /// </summary>
    public int EivaDataLogCount { get; set; }
    
    /// <summary>
    /// Number of manual entries.
    /// </summary>
    public int ManualEntryCount { get; set; }
    
    /// <summary>
    /// Number of DPR reports.
    /// </summary>
    public int DprReportCount { get; set; }
    
    /// <summary>
    /// SHA256 checksum for data integrity.
    /// </summary>
    public string? Checksum { get; set; }
    
    /// <summary>
    /// Application version that created this file.
    /// </summary>
    public string? CreatedByVersion { get; set; }
}

/// <summary>
/// Represents a date range.
/// </summary>
public class DateRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    
    [JsonIgnore]
    public TimeSpan Duration => End - Start;
    
    [JsonIgnore]
    public int Days => (int)Duration.TotalDays + 1;
    
    public override string ToString() => $"{Start:dd/MM/yyyy} - {End:dd/MM/yyyy}";
}
