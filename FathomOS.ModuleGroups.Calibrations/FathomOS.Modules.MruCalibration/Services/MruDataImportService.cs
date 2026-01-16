using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using FathomOS.Modules.MruCalibration.Models;

namespace FathomOS.Modules.MruCalibration.Services;

/// <summary>
/// Service for importing MRU calibration data from NPD files
/// Uses Core NpdParser patterns but extracts calibration-specific columns
/// </summary>
public class MruDataImportService
{
    #region Public Methods
    
    /// <summary>
    /// Get all column headers from an NPD file
    /// </summary>
    public List<string> GetAvailableColumns(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("NPD file not found", filePath);
        
        var firstLine = File.ReadLines(filePath).FirstOrDefault();
        if (string.IsNullOrEmpty(firstLine))
            return new List<string>();
        
        return ParseCsvLine(firstLine);
    }
    
    /// <summary>
    /// Auto-detect columns that might contain MRU pitch/roll data
    /// </summary>
    public List<string> GetMruColumns(string filePath)
    {
        var allColumns = GetAvailableColumns(filePath);
        var patterns = new[] { "pitch", "roll", "mru", "motion", "incline", "tilt" };
        
        return allColumns
            .Where(c => patterns.Any(p => c.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
    
    /// <summary>
    /// Import MRU calibration data from NPD file
    /// </summary>
    public MruImportResult ImportData(string filePath, MruColumnMapping mapping)
    {
        var result = new MruImportResult();
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                result.Errors.Add("File contains no data rows");
                return result;
            }
            
            // Parse header to get column indices
            var headers = ParseCsvLine(lines[0]);
            var columnIndices = ResolveColumnIndices(headers, mapping);
            
            if (!columnIndices.IsValid)
            {
                result.Errors.AddRange(columnIndices.Errors);
                return result;
            }
            
            result.DetectedColumns = headers;
            
            // Parse data rows
            int rowIndex = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                try
                {
                    var values = ParseCsvLine(line);
                    
                    // Apply date/time offset for NaviPac files
                    int offset = mapping.HasDateTimeSplit ? 1 : 0;
                    
                    var dataPoint = new MruDataPoint
                    {
                        Index = rowIndex++
                    };
                    
                    // Parse timestamp
                    dataPoint.Timestamp = ParseTimestamp(values, columnIndices.TimeIndex, mapping);
                    
                    // Parse pitch values
                    if (columnIndices.ReferencePitchIndex >= 0)
                    {
                        var refPitch = ParseDouble(values, columnIndices.ReferencePitchIndex + offset);
                        var verPitch = ParseDouble(values, columnIndices.VerifiedPitchIndex + offset);
                        
                        if (refPitch.HasValue && verPitch.HasValue)
                        {
                            var pitchPoint = new MruDataPoint
                            {
                                Index = dataPoint.Index,
                                Timestamp = dataPoint.Timestamp,
                                ReferenceValue = refPitch.Value,
                                VerifiedValue = verPitch.Value
                            };
                            result.PitchPoints.Add(pitchPoint);
                        }
                    }
                    
                    // Parse roll values
                    if (columnIndices.ReferenceRollIndex >= 0)
                    {
                        var refRoll = ParseDouble(values, columnIndices.ReferenceRollIndex + offset);
                        var verRoll = ParseDouble(values, columnIndices.VerifiedRollIndex + offset);
                        
                        if (refRoll.HasValue && verRoll.HasValue)
                        {
                            var rollPoint = new MruDataPoint
                            {
                                Index = dataPoint.Index,
                                Timestamp = dataPoint.Timestamp,
                                ReferenceValue = refRoll.Value,
                                VerifiedValue = verRoll.Value
                            };
                            result.RollPoints.Add(rollPoint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Row {i}: {ex.Message}");
                }
            }
            
            // Apply midnight correction
            ApplyMidnightCorrection(result.PitchPoints);
            ApplyMidnightCorrection(result.RollPoints);
            
            result.Success = result.PitchPoints.Count > 0 || result.RollPoints.Count > 0;
            
            if (!result.Success)
            {
                result.Errors.Add("No valid data points were parsed. Check column mapping.");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to parse file: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Preview first N rows of data
    /// </summary>
    public List<Dictionary<string, string>> PreviewData(string filePath, int maxRows = 10)
    {
        var preview = new List<Dictionary<string, string>>();
        
        try
        {
            var lines = File.ReadLines(filePath).Take(maxRows + 1).ToList();
            if (lines.Count < 2) return preview;
            
            var headers = ParseCsvLine(lines[0]);
            
            for (int i = 1; i < lines.Count; i++)
            {
                var values = ParseCsvLine(lines[i]);
                var row = new Dictionary<string, string>();
                
                for (int j = 0; j < Math.Min(headers.Count, values.Count); j++)
                {
                    row[headers[j]] = values[j];
                }
                
                preview.Add(row);
            }
        }
        catch
        {
            // Return empty preview on error
        }
        
        return preview;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Parse a CSV line, handling quoted fields
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentValue = new System.Text.StringBuilder();
        
        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue.ToString().Trim());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }
        
        result.Add(currentValue.ToString().Trim());
        return result;
    }
    
    /// <summary>
    /// Resolve column indices from headers and mapping
    /// </summary>
    private ColumnIndices ResolveColumnIndices(List<string> headers, MruColumnMapping mapping)
    {
        var indices = new ColumnIndices();
        
        // Time column
        indices.TimeIndex = FindColumnIndex(headers, mapping.TimeColumn);
        if (indices.TimeIndex < 0)
        {
            // Try pattern matching
            indices.TimeIndex = FindColumnByPattern(headers, "Time|DateTime|UTC");
        }
        
        if (indices.TimeIndex < 0)
        {
            indices.Errors.Add("Time column not found");
        }
        
        // Reference Pitch
        if (!string.IsNullOrEmpty(mapping.ReferencePitchColumn))
        {
            indices.ReferencePitchIndex = FindColumnIndex(headers, mapping.ReferencePitchColumn);
            if (indices.ReferencePitchIndex < 0)
                indices.Errors.Add($"Reference Pitch column '{mapping.ReferencePitchColumn}' not found");
        }
        
        // Verified Pitch
        if (!string.IsNullOrEmpty(mapping.VerifiedPitchColumn))
        {
            indices.VerifiedPitchIndex = FindColumnIndex(headers, mapping.VerifiedPitchColumn);
            if (indices.VerifiedPitchIndex < 0)
                indices.Errors.Add($"Verified Pitch column '{mapping.VerifiedPitchColumn}' not found");
        }
        
        // Reference Roll
        if (!string.IsNullOrEmpty(mapping.ReferenceRollColumn))
        {
            indices.ReferenceRollIndex = FindColumnIndex(headers, mapping.ReferenceRollColumn);
            if (indices.ReferenceRollIndex < 0)
                indices.Errors.Add($"Reference Roll column '{mapping.ReferenceRollColumn}' not found");
        }
        
        // Verified Roll
        if (!string.IsNullOrEmpty(mapping.VerifiedRollColumn))
        {
            indices.VerifiedRollIndex = FindColumnIndex(headers, mapping.VerifiedRollColumn);
            if (indices.VerifiedRollIndex < 0)
                indices.Errors.Add($"Verified Roll column '{mapping.VerifiedRollColumn}' not found");
        }
        
        return indices;
    }
    
    /// <summary>
    /// Find exact column index by name
    /// </summary>
    private int FindColumnIndex(List<string> headers, string columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return -1;
        
        return headers.FindIndex(h => 
            h.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Find column index by regex pattern
    /// </summary>
    private int FindColumnByPattern(List<string> headers, string pattern)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return headers.FindIndex(h => regex.IsMatch(h));
        }
        catch
        {
            return -1;
        }
    }
    
    /// <summary>
    /// Parse timestamp from values, handling NaviPac date/time split
    /// </summary>
    private DateTime ParseTimestamp(List<string> values, int timeIndex, MruColumnMapping mapping)
    {
        if (timeIndex < 0 || timeIndex >= values.Count)
            return DateTime.MinValue;
        
        if (mapping.HasDateTimeSplit && timeIndex + 1 < values.Count)
        {
            // NaviPac format: separate Date and Time columns
            var dateStr = values[timeIndex];
            var timeStr = values[timeIndex + 1];
            
            var formats = new[]
            {
                $"{mapping.DateFormat} {mapping.TimeFormat}",
                "dd/MM/yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss"
            };
            
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact($"{dateStr} {timeStr}", format, 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
            }
            
            // Try generic parse
            if (DateTime.TryParse($"{dateStr} {timeStr}", out var dt2))
                return dt2;
        }
        else
        {
            // Single datetime column
            var dtStr = values[timeIndex];
            
            if (DateTime.TryParse(dtStr, out var dt))
                return dt;
            
            // Try time-only (will use today's date)
            if (TimeSpan.TryParse(dtStr, out var ts))
                return DateTime.Today.Add(ts);
        }
        
        return DateTime.MinValue;
    }
    
    /// <summary>
    /// Parse double from value at index
    /// </summary>
    private double? ParseDouble(List<string> values, int index)
    {
        if (index < 0 || index >= values.Count)
            return null;
        
        var value = values[index].Trim();
        if (string.IsNullOrEmpty(value))
            return null;
        
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;
        
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            return result;
        
        return null;
    }
    
    /// <summary>
    /// Apply midnight correction - handle time going backwards (crossing midnight)
    /// This is the same logic from the Excel VBA macro
    /// </summary>
    private void ApplyMidnightCorrection(List<MruDataPoint> points)
    {
        if (points.Count < 2) return;
        
        // Find where time goes backwards
        int midnightIndex = -1;
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].Timestamp < points[i - 1].Timestamp)
            {
                midnightIndex = i;
                break;
            }
        }
        
        // Add 1 day to all times after midnight
        if (midnightIndex > 0)
        {
            for (int i = midnightIndex; i < points.Count; i++)
            {
                points[i].Timestamp = points[i].Timestamp.AddDays(1);
            }
        }
    }
    
    #endregion
}

#region Helper Classes

/// <summary>
/// Column index resolution result
/// </summary>
internal class ColumnIndices
{
    public int TimeIndex { get; set; } = -1;
    public int ReferencePitchIndex { get; set; } = -1;
    public int VerifiedPitchIndex { get; set; } = -1;
    public int ReferenceRollIndex { get; set; } = -1;
    public int VerifiedRollIndex { get; set; } = -1;
    
    public List<string> Errors { get; } = new();
    
    public bool IsValid => Errors.Count == 0 && TimeIndex >= 0 &&
        ((ReferencePitchIndex >= 0 && VerifiedPitchIndex >= 0) ||
         (ReferenceRollIndex >= 0 && VerifiedRollIndex >= 0));
}

/// <summary>
/// Result of MRU data import
/// </summary>
public class MruImportResult
{
    public bool Success { get; set; }
    public List<MruDataPoint> PitchPoints { get; } = new();
    public List<MruDataPoint> RollPoints { get; } = new();
    public List<string> DetectedColumns { get; set; } = new();
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    
    public int TotalPitchPoints => PitchPoints.Count;
    public int TotalRollPoints => RollPoints.Count;
    
    public DateTime? PitchStartTime => PitchPoints.FirstOrDefault()?.Timestamp;
    public DateTime? PitchEndTime => PitchPoints.LastOrDefault()?.Timestamp;
    public DateTime? RollStartTime => RollPoints.FirstOrDefault()?.Timestamp;
    public DateTime? RollEndTime => RollPoints.LastOrDefault()?.Timestamp;
}

#endregion
