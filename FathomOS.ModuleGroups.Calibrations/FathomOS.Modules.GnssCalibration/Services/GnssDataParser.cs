using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Result container for parsed GNSS data.
/// </summary>
public class GnssParseResult
{
    public List<GnssComparisonPoint> Points { get; set; } = new();
    public List<string> Headers { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalRecords { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Detected column indices from file analysis.
/// </summary>
public class DetectedColumns
{
    public int DateIndex { get; set; } = -1;
    public int TimeIndex { get; set; } = -1;
    public int System1EastingIndex { get; set; } = -1;
    public int System1NorthingIndex { get; set; } = -1;
    public int System1HeightIndex { get; set; } = -1;
    public int System2EastingIndex { get; set; } = -1;
    public int System2NorthingIndex { get; set; } = -1;
    public int System2HeightIndex { get; set; } = -1;
    
    public bool IsValid => System1EastingIndex >= 0 && System1NorthingIndex >= 0 &&
                           System2EastingIndex >= 0 && System2NorthingIndex >= 0;
}

/// <summary>
/// Parses NPD/CSV files for GNSS calibration data.
/// Handles NaviPac date/time column offset automatically.
/// </summary>
public class GnssDataParser
{
    private readonly List<string> _warnings = new();
    
    /// <summary>
    /// Gets all column headers from a file.
    /// </summary>
    public List<string> GetColumnHeaders(string filePath)
    {
        if (!File.Exists(filePath))
            return new List<string>();
        
        try
        {
            using var reader = new StreamReader(filePath);
            var headerLine = reader.ReadLine();
            
            if (string.IsNullOrWhiteSpace(headerLine))
                return new List<string>();
            
            return ParseCsvLine(headerLine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GnssDataParser] GetColumnHeaders error: {ex.Message}");
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Auto-detects column indices based on common naming patterns.
    /// </summary>
    public DetectedColumns AutoDetectColumns(List<string> headers, GnssColumnMapping mapping)
    {
        var detected = new DetectedColumns();
        
        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].Trim();
            
            // Time/Date detection
            if (Regex.IsMatch(header, @"^Time$|^DateTime$", RegexOptions.IgnoreCase))
                detected.TimeIndex = i;
            if (Regex.IsMatch(header, @"^Date$", RegexOptions.IgnoreCase))
                detected.DateIndex = i;
            
            // System 1 detection (DGNSS 1, GNSS 1, Primary, Reference)
            if (Regex.IsMatch(header, mapping.System1EastingPattern, RegexOptions.IgnoreCase))
                detected.System1EastingIndex = i;
            if (Regex.IsMatch(header, mapping.System1NorthingPattern, RegexOptions.IgnoreCase))
                detected.System1NorthingIndex = i;
            if (Regex.IsMatch(header, mapping.System1HeightPattern, RegexOptions.IgnoreCase))
                detected.System1HeightIndex = i;
            
            // System 2 detection (DGNSS 2, GNSS 2, Secondary, Observed)
            if (Regex.IsMatch(header, mapping.System2EastingPattern, RegexOptions.IgnoreCase))
                detected.System2EastingIndex = i;
            if (Regex.IsMatch(header, mapping.System2NorthingPattern, RegexOptions.IgnoreCase))
                detected.System2NorthingIndex = i;
            if (Regex.IsMatch(header, mapping.System2HeightPattern, RegexOptions.IgnoreCase))
                detected.System2HeightIndex = i;
        }
        
        return detected;
    }
    
    /// <summary>
    /// Parses an NPD/CSV file into GNSS comparison points.
    /// </summary>
    public GnssParseResult Parse(string filePath, GnssColumnMapping mapping)
    {
        var result = new GnssParseResult();
        _warnings.Clear();
        
        if (!File.Exists(filePath))
        {
            result.Success = false;
            result.ErrorMessage = $"File not found: {filePath}";
            return result;
        }
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length < 2)
            {
                result.Success = false;
                result.ErrorMessage = "File contains no data (less than 2 lines).";
                return result;
            }
            
            // Parse headers
            result.Headers = ParseCsvLine(lines[0]);
            
            // IMPORTANT: User-selected indices (mapping) take absolute priority over auto-detection
            // Only auto-detect columns that weren't explicitly set by user
            var detected = AutoDetectColumns(result.Headers, mapping);
            
            // Debug: Log what user set vs what was auto-detected
            System.Diagnostics.Debug.WriteLine($"[GNSS Parse] User mapping indices:");
            System.Diagnostics.Debug.WriteLine($"  Sys1E={mapping.System1EastingIndex}, Sys1N={mapping.System1NorthingIndex}, Sys1H={mapping.System1HeightIndex}");
            System.Diagnostics.Debug.WriteLine($"  Sys2E={mapping.System2EastingIndex}, Sys2N={mapping.System2NorthingIndex}, Sys2H={mapping.System2HeightIndex}");
            System.Diagnostics.Debug.WriteLine($"[GNSS Parse] Auto-detected indices:");
            System.Diagnostics.Debug.WriteLine($"  Sys1E={detected.System1EastingIndex}, Sys1N={detected.System1NorthingIndex}, Sys1H={detected.System1HeightIndex}");
            System.Diagnostics.Debug.WriteLine($"  Sys2E={detected.System2EastingIndex}, Sys2N={detected.System2NorthingIndex}, Sys2H={detected.System2HeightIndex}");
            
            _warnings.Add($"User mapping: Sys1E={mapping.System1EastingIndex}, Sys1N={mapping.System1NorthingIndex}, Sys1H={mapping.System1HeightIndex}, Sys2E={mapping.System2EastingIndex}, Sys2N={mapping.System2NorthingIndex}, Sys2H={mapping.System2HeightIndex}");
            _warnings.Add($"Auto-detected: Sys1E={detected.System1EastingIndex}, Sys1N={detected.System1NorthingIndex}, Sys1H={detected.System1HeightIndex}, Sys2E={detected.System2EastingIndex}, Sys2N={detected.System2NorthingIndex}, Sys2H={detected.System2HeightIndex}");
            
            // User mapping ALWAYS takes priority when index is valid (>= 0)
            // Only fall back to auto-detection when user hasn't selected (-1)
            int sys1EIdx = mapping.System1EastingIndex >= 0 ? mapping.System1EastingIndex : detected.System1EastingIndex;
            int sys1NIdx = mapping.System1NorthingIndex >= 0 ? mapping.System1NorthingIndex : detected.System1NorthingIndex;
            int sys1HIdx = mapping.System1HeightIndex >= 0 ? mapping.System1HeightIndex : detected.System1HeightIndex;
            int sys2EIdx = mapping.System2EastingIndex >= 0 ? mapping.System2EastingIndex : detected.System2EastingIndex;
            int sys2NIdx = mapping.System2NorthingIndex >= 0 ? mapping.System2NorthingIndex : detected.System2NorthingIndex;
            int sys2HIdx = mapping.System2HeightIndex >= 0 ? mapping.System2HeightIndex : detected.System2HeightIndex;
            int timeIdx = mapping.TimeColumnIndex >= 0 ? mapping.TimeColumnIndex : detected.TimeIndex;
            int dateIdx = mapping.DateColumnIndex >= 0 ? mapping.DateColumnIndex : detected.DateIndex;
            
            // Log final resolved indices
            System.Diagnostics.Debug.WriteLine($"[GNSS Parse] Final resolved indices (user priority):");
            System.Diagnostics.Debug.WriteLine($"  Sys1E={sys1EIdx}, Sys1N={sys1NIdx}, Sys1H={sys1HIdx}");
            System.Diagnostics.Debug.WriteLine($"  Sys2E={sys2EIdx}, Sys2N={sys2NIdx}, Sys2H={sys2HIdx}");
            System.Diagnostics.Debug.WriteLine($"  Headers: {string.Join(", ", result.Headers.Take(10).Select((h, i) => $"[{i}]{h}"))}...");
            
            _warnings.Add($"Final resolved: Sys1E={sys1EIdx}, Sys1N={sys1NIdx}, Sys1H={sys1HIdx}, Sys2E={sys2EIdx}, Sys2N={sys2NIdx}, Sys2H={sys2HIdx}");
            _warnings.Add($"Headers: {string.Join(", ", result.Headers.Select((h, i) => $"[{i}]{h}"))}");
            
            // Validate required columns found
            if (sys1EIdx < 0 || sys1NIdx < 0 || sys2EIdx < 0 || sys2NIdx < 0)
            {
                result.Success = false;
                result.ErrorMessage = "Could not detect required columns (System 1/2 Easting and Northing). Please select columns manually.";
                return result;
            }
            
            // CRITICAL: Validate that System1 and System2 columns are DIFFERENT
            // If they're the same, deltas will all be zero!
            if (sys1EIdx == sys2EIdx)
            {
                result.Success = false;
                result.ErrorMessage = $"System 1 and System 2 Easting columns are the same (column {sys1EIdx}: '{result.Headers[sys1EIdx]}'). Please select different columns.";
                return result;
            }
            if (sys1NIdx == sys2NIdx)
            {
                result.Success = false;
                result.ErrorMessage = $"System 1 and System 2 Northing columns are the same (column {sys1NIdx}: '{result.Headers[sys1NIdx]}'). Please select different columns.";
                return result;
            }
            
            // Validate Height columns are different if both are specified
            if (sys1HIdx >= 0 && sys2HIdx >= 0 && sys1HIdx == sys2HIdx)
            {
                result.Success = false;
                result.ErrorMessage = $"System 1 and System 2 Height columns are the same (column {sys1HIdx}: '{result.Headers[sys1HIdx]}'). Please select different columns for height comparison.";
                return result;
            }
            
            // Calculate column offset for NaviPac date/time split
            // When header has "Time" (1 column) but data has "Date,Time" (2 columns)
            int columnOffset = mapping.HasDateTimeSplit ? 1 : 0;
            
            // Parse data rows
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var fields = ParseCsvLine(line);
                
                try
                {
                    var point = new GnssComparisonPoint
                    {
                        Index = i,
                        DateTime = ParseDateTime(fields, dateIdx, timeIdx, mapping, columnOffset),
                        System1Easting = ParseDouble(fields, sys1EIdx + columnOffset),
                        System1Northing = ParseDouble(fields, sys1NIdx + columnOffset),
                        System1Height = sys1HIdx >= 0 ? ParseDouble(fields, sys1HIdx + columnOffset) : 0,
                        System2Easting = ParseDouble(fields, sys2EIdx + columnOffset),
                        System2Northing = ParseDouble(fields, sys2NIdx + columnOffset),
                        System2Height = sys2HIdx >= 0 ? ParseDouble(fields, sys2HIdx + columnOffset) : 0
                    };
                    
                    if (point.IsValid)
                    {
                        result.Points.Add(point);
                    }
                    else
                    {
                        _warnings.Add($"Row {i}: Invalid coordinates, skipped.");
                    }
                }
                catch (Exception ex)
                {
                    _warnings.Add($"Row {i}: Parse error - {ex.Message}");
                }
            }
            
            result.TotalRecords = lines.Length - 1;
            result.Warnings = _warnings.ToList();
            
            if (result.Points.Count > 0)
            {
                result.StartTime = result.Points.Min(p => p.DateTime);
                result.EndTime = result.Points.Max(p => p.DateTime);
                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "No valid data points parsed from file.";
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Parse error: {ex.Message}";
        }
        
        return result;
    }
    
    /// <summary>
    /// Parses a CSV line handling quoted fields.
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        fields.Add(currentField.Trim());
        return fields;
    }
    
    /// <summary>
    /// Parses date/time from fields handling NaviPac format.
    /// </summary>
    private DateTime ParseDateTime(List<string> fields, int dateIdx, int timeIdx, 
                                    GnssColumnMapping mapping, int offset)
    {
        try
        {
            if (mapping.HasDateTimeSplit && fields.Count > 1)
            {
                // NaviPac format: first two fields are Date, Time
                var dateStr = fields[0].Trim();
                var timeStr = fields[1].Trim();
                
                if (DateTime.TryParseExact($"{dateStr} {timeStr}", 
                    $"{mapping.DateFormat} {mapping.TimeFormat}",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
                
                // Try other common formats
                if (DateTime.TryParse($"{dateStr} {timeStr}", out dt))
                    return dt;
            }
            else if (timeIdx >= 0 && timeIdx < fields.Count)
            {
                // Single DateTime column
                if (DateTime.TryParse(fields[timeIdx], out var dt))
                    return dt;
            }
        }
        catch 
        { 
            // Parsing failed - return MinValue as fallback
        }
        
        return DateTime.MinValue;
    }
    
    /// <summary>
    /// Safely parses a double from field array.
    /// </summary>
    private double ParseDouble(List<string> fields, int index)
    {
        if (index < 0 || index >= fields.Count)
            return double.NaN;
        
        var value = fields[index].Trim();
        
        if (string.IsNullOrEmpty(value))
            return double.NaN;
        
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        
        return double.NaN;
    }
    
    /// <summary>
    /// Gets raw preview data from a file (first N rows).
    /// Returns list of (RowNumber, Time, Sys1E, Sys1N, Sys1H, Sys2E, Sys2N, Sys2H).
    /// </summary>
    public List<RawPreviewData> GetRawPreview(string filePath, GnssColumnMapping mapping, int maxRows = 10)
    {
        var preview = new List<RawPreviewData>();
        
        if (!File.Exists(filePath))
            return preview;
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length < 2)
                return preview;
            
            // Parse headers
            var headers = ParseCsvLine(lines[0]);
            
            // Debug: Log incoming mapping state
            System.Diagnostics.Debug.WriteLine($"[GNSS GetRawPreview] Mapping hash: {mapping.GetHashCode()}");
            System.Diagnostics.Debug.WriteLine($"[GNSS GetRawPreview] Incoming mapping:");
            System.Diagnostics.Debug.WriteLine($"  Sys1E={mapping.System1EastingIndex}, Sys1N={mapping.System1NorthingIndex}, Sys1H={mapping.System1HeightIndex}");
            System.Diagnostics.Debug.WriteLine($"  Sys2E={mapping.System2EastingIndex}, Sys2N={mapping.System2NorthingIndex}, Sys2H={mapping.System2HeightIndex}");
            
            // Auto-detect columns (for fallback only)
            var detected = AutoDetectColumns(headers, mapping);
            
            System.Diagnostics.Debug.WriteLine($"[GNSS GetRawPreview] Auto-detected:");
            System.Diagnostics.Debug.WriteLine($"  Sys1E={detected.System1EastingIndex}, Sys1N={detected.System1NorthingIndex}, Sys1H={detected.System1HeightIndex}");
            System.Diagnostics.Debug.WriteLine($"  Sys2E={detected.System2EastingIndex}, Sys2N={detected.System2NorthingIndex}, Sys2H={detected.System2HeightIndex}");
            
            // User mapping takes priority (>= 0), otherwise use auto-detected
            int sys1EIdx = mapping.System1EastingIndex >= 0 ? mapping.System1EastingIndex : detected.System1EastingIndex;
            int sys1NIdx = mapping.System1NorthingIndex >= 0 ? mapping.System1NorthingIndex : detected.System1NorthingIndex;
            int sys1HIdx = mapping.System1HeightIndex >= 0 ? mapping.System1HeightIndex : detected.System1HeightIndex;
            int sys2EIdx = mapping.System2EastingIndex >= 0 ? mapping.System2EastingIndex : detected.System2EastingIndex;
            int sys2NIdx = mapping.System2NorthingIndex >= 0 ? mapping.System2NorthingIndex : detected.System2NorthingIndex;
            int sys2HIdx = mapping.System2HeightIndex >= 0 ? mapping.System2HeightIndex : detected.System2HeightIndex;
            int timeIdx = mapping.TimeColumnIndex >= 0 ? mapping.TimeColumnIndex : detected.TimeIndex;
            int dateIdx = mapping.DateColumnIndex >= 0 ? mapping.DateColumnIndex : detected.DateIndex;
            
            System.Diagnostics.Debug.WriteLine($"[GNSS GetRawPreview] Final resolved indices:");
            System.Diagnostics.Debug.WriteLine($"  Sys1E={sys1EIdx}, Sys1N={sys1NIdx}, Sys1H={sys1HIdx}");
            System.Diagnostics.Debug.WriteLine($"  Sys2E={sys2EIdx}, Sys2N={sys2NIdx}, Sys2H={sys2HIdx}");
            System.Diagnostics.Debug.WriteLine($"  Headers[{sys1EIdx}]={headers.ElementAtOrDefault(sys1EIdx) ?? "?"}, Headers[{sys2EIdx}]={headers.ElementAtOrDefault(sys2EIdx) ?? "?"}");
            
            int columnOffset = mapping.HasDateTimeSplit ? 1 : 0;
            System.Diagnostics.Debug.WriteLine($"[GNSS GetRawPreview] Column offset: {columnOffset}");
            
            // Parse data rows (limit to maxRows, -1 means all rows)
            int rowCount = maxRows < 0 ? lines.Length - 1 : Math.Min(lines.Length - 1, maxRows);
            
            for (int i = 1; i <= rowCount; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var fields = ParseCsvLine(line);
                
                try
                {
                    var row = new RawPreviewData
                    {
                        RowNumber = i,
                        Time = ParseDateTime(fields, dateIdx, timeIdx, mapping, columnOffset),
                        System1Easting = ParseDouble(fields, sys1EIdx + columnOffset),
                        System1Northing = ParseDouble(fields, sys1NIdx + columnOffset),
                        System1Height = sys1HIdx >= 0 ? ParseDouble(fields, sys1HIdx + columnOffset) : double.NaN,
                        System2Easting = ParseDouble(fields, sys2EIdx + columnOffset),
                        System2Northing = ParseDouble(fields, sys2NIdx + columnOffset),
                        System2Height = sys2HIdx >= 0 ? ParseDouble(fields, sys2HIdx + columnOffset) : double.NaN
                    };
                    
                    // Debug first row
                    if (i == 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GNSS GetRawPreview] First row values:");
                        System.Diagnostics.Debug.WriteLine($"  Sys1: E={row.System1Easting}, N={row.System1Northing}, H={row.System1Height}");
                        System.Diagnostics.Debug.WriteLine($"  Sys2: E={row.System2Easting}, N={row.System2Northing}, H={row.System2Height}");
                    }
                    
                    preview.Add(row);
                }
                catch
                {
                    // Skip rows that fail to parse in preview
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GNSS GetRawPreview] Error: {ex.Message}");
        }
        
        return preview;
    }
}

/// <summary>
/// Raw preview data row for display before full processing.
/// </summary>
public class RawPreviewData
{
    public int RowNumber { get; set; }
    public DateTime Time { get; set; }
    public double System1Easting { get; set; }
    public double System1Northing { get; set; }
    public double System1Height { get; set; }
    public double System2Easting { get; set; }
    public double System2Northing { get; set; }
    public double System2Height { get; set; }
    
    // Formatted for display
    public string TimeDisplay => Time != DateTime.MinValue ? Time.ToString("HH:mm:ss") : "-";
    public string Sys1E => !double.IsNaN(System1Easting) ? System1Easting.ToString("F3") : "-";
    public string Sys1N => !double.IsNaN(System1Northing) ? System1Northing.ToString("F3") : "-";
    public string Sys1H => !double.IsNaN(System1Height) ? System1Height.ToString("F3") : "-";
    public string Sys2E => !double.IsNaN(System2Easting) ? System2Easting.ToString("F3") : "-";
    public string Sys2N => !double.IsNaN(System2Northing) ? System2Northing.ToString("F3") : "-";
    public string Sys2H => !double.IsNaN(System2Height) ? System2Height.ToString("F3") : "-";
}
