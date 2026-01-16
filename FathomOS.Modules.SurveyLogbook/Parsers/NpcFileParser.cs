// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Parsers/NpcFileParser.cs
// Purpose: Parser for EIVA NaviPac .npc position fix/calibration files
// ============================================================================

using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Parsers;

/// <summary>
/// Parses EIVA NaviPac .npc calibration/position fix report files.
/// 
/// Sample file structure:
/// EIVA XYZ calibration report: 19.06.2025 20:26:53 | Object being monitored: 0000 Island Performer
/// ------------------------------------------------------------------------------------------
///      Error    Average     Median    Minimum    Maximum  Std. dev. 95%err.range      Count 
/// ------------------------------------------------------------------------------------------
/// Easting     -1658.169  -1658.190  -1658.290  -1658.020      0.082        0.160   10 of 10
/// Northing   -20281.733 -20281.710 -20281.920 -20281.530      0.151        0.296   10 of 10
/// Height        -86.996    -87.170    -87.410    -86.350      0.398        0.781   10 of 10
/// ...
/// Date       Time         Observed X Observed Y Observed Z
/// ------------------------------------------------------------------------------------------
/// 20/06/2025 01:26:43.107 2129798.80 9765368.16     -87.24
/// </summary>
public class NpcFileParser
{
    // Regex patterns for parsing
    private static readonly Regex HeaderPattern = new(
        @"EIVA\s+XYZ\s+calibration\s+report:\s*(\d{2}\.\d{2}\.\d{4})\s+(\d{2}:\d{2}:\d{2})\s*\|\s*Object\s+being\s+monitored:\s*(.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex StatsPattern = new(
        @"^(Easting|Northing|Height)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s+(\d+)\s+of\s+(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex ObservationPattern = new(
        @"^(\d{2}/\d{2}/\d{4})\s+(\d{2}:\d{2}:\d{2}\.\d{3})\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)?",
        RegexOptions.Compiled);
    
    private static readonly Regex ObservedAvgPattern = new(
        @"^\s*[XYH]\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)",
        RegexOptions.Compiled);
    
    private static readonly Regex GeoderyPattern = new(
        @"Projection:\s*(.*?)(?:\s+PT:|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex EllipsoidPattern = new(
        @"Ellipsoid:\s*(.*?)(?:\s+Semi|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex RangeBearingPattern = new(
        @"Range\s+([-\d.]+)\s+([-\d.]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private readonly List<string> _parseWarnings = new();
    
    /// <summary>
    /// Gets any warnings generated during parsing.
    /// </summary>
    public IReadOnlyList<string> ParseWarnings => _parseWarnings.AsReadOnly();
    
    /// <summary>
    /// Parses an .npc file and returns a PositionFix object.
    /// </summary>
    /// <param name="filePath">Path to the .npc file</param>
    /// <returns>Parsed PositionFix or null if parsing fails</returns>
    public PositionFix? Parse(string filePath)
    {
        _parseWarnings.Clear();
        
        if (!File.Exists(filePath))
        {
            _parseWarnings.Add($"File not found: {filePath}");
            return null;
        }
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            return ParseLines(lines, filePath);
        }
        catch (Exception ex)
        {
            _parseWarnings.Add($"Error reading file: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parses .npc content from lines.
    /// </summary>
    public PositionFix? ParseLines(string[] lines, string sourceFile = "")
    {
        if (lines == null || lines.Length == 0)
        {
            _parseWarnings.Add("Empty file content");
            return null;
        }
        
        var fix = new PositionFix
        {
            SourceFile = sourceFile
        };
        
        bool inDataSection = false;
        bool foundStats = false;
        var observations = new List<FixObservation>();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("---"))
                continue;
            
            // Parse header line
            var headerMatch = HeaderPattern.Match(trimmedLine);
            if (headerMatch.Success)
            {
                if (DateTime.TryParseExact(headerMatch.Groups[1].Value, "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    fix.Date = date;
                }
                
                if (TimeSpan.TryParseExact(headerMatch.Groups[2].Value, @"hh\:mm\:ss",
                    CultureInfo.InvariantCulture, out var time))
                {
                    fix.Time = time;
                }
                
                fix.ObjectMonitored = headerMatch.Groups[3].Value.Trim();
                continue;
            }
            
            // Parse statistics lines (Easting, Northing, Height)
            var statsMatch = StatsPattern.Match(trimmedLine);
            if (statsMatch.Success)
            {
                foundStats = true;
                var component = statsMatch.Groups[1].Value.ToLowerInvariant();
                var stats = new FixStatistics
                {
                    Error = ParseDouble(statsMatch.Groups[2].Value),
                    Average = ParseDouble(statsMatch.Groups[3].Value),
                    Median = ParseDouble(statsMatch.Groups[4].Value),
                    Minimum = ParseDouble(statsMatch.Groups[5].Value),
                    Maximum = ParseDouble(statsMatch.Groups[6].Value),
                    StdDev = ParseDouble(statsMatch.Groups[7].Value),
                    Error95Percent = 0, // Not always present in this format
                    Count = int.Parse(statsMatch.Groups[8].Value),
                    TotalCount = int.Parse(statsMatch.Groups[9].Value)
                };
                
                switch (component)
                {
                    case "easting":
                        fix.EastingStats = stats;
                        fix.SdEasting = stats.StdDev;
                        break;
                    case "northing":
                        fix.NorthingStats = stats;
                        fix.SdNorthing = stats.StdDev;
                        break;
                    case "height":
                        fix.HeightStats = stats;
                        fix.SdHeight = stats.StdDev;
                        break;
                }
                continue;
            }
            
            // Parse observed average/median values
            if (trimmedLine.StartsWith("X") && trimmedLine.Contains("Observed"))
            {
                // This is the header for observed values section
                continue;
            }
            
            // Parse individual coordinates from "Observed Avg" section
            if (trimmedLine.Trim().StartsWith("X ") || trimmedLine.Trim().StartsWith("Y ") || 
                trimmedLine.Trim().StartsWith("H "))
            {
                var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var label = parts[0].ToUpperInvariant();
                    if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var avgValue))
                    {
                        switch (label)
                        {
                            case "X":
                                fix.Easting = avgValue;
                                break;
                            case "Y":
                                fix.Northing = avgValue;
                                break;
                            case "H":
                                fix.Height = avgValue;
                                break;
                        }
                    }
                    
                    // Check for Required value
                    if (parts.Length >= 4 && double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var reqValue))
                    {
                        switch (label)
                        {
                            case "X":
                                fix.RequiredX = reqValue;
                                break;
                            case "Y":
                                fix.RequiredY = reqValue;
                                break;
                            case "H":
                                fix.RequiredH = reqValue;
                                break;
                        }
                    }
                }
                continue;
            }
            
            // Parse Range/Bearing
            if (trimmedLine.Contains("Range") && trimmedLine.Contains("Bearing"))
            {
                var rangeParts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < rangeParts.Length - 1; i++)
                {
                    if (rangeParts[i].Equals("Range", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(rangeParts[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var range))
                            fix.Range = range;
                    }
                    if (rangeParts[i].StartsWith("Bearing", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(rangeParts[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out var bearing))
                            fix.Bearing = bearing;
                    }
                }
                continue;
            }
            
            // Parse Geodesy information
            var geodesyMatch = GeoderyPattern.Match(trimmedLine);
            if (geodesyMatch.Success)
            {
                fix.Projection = geodesyMatch.Groups[1].Value.Trim();
                continue;
            }
            
            var ellipsoidMatch = EllipsoidPattern.Match(trimmedLine);
            if (ellipsoidMatch.Success)
            {
                fix.Ellipsoid = ellipsoidMatch.Groups[1].Value.Trim();
                continue;
            }
            
            // Check for data section header
            if (trimmedLine.Contains("Date") && trimmedLine.Contains("Time") && 
                (trimmedLine.Contains("Observed") || trimmedLine.Contains("X")))
            {
                inDataSection = true;
                continue;
            }
            
            // Parse observation data rows
            if (inDataSection)
            {
                var obsMatch = ObservationPattern.Match(trimmedLine);
                if (obsMatch.Success)
                {
                    var obs = new FixObservation();
                    
                    if (DateTime.TryParseExact(obsMatch.Groups[1].Value, "dd/MM/yyyy",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var obsDate) &&
                        TimeSpan.TryParseExact(obsMatch.Groups[2].Value, @"hh\:mm\:ss\.fff",
                        CultureInfo.InvariantCulture, out var obsTime))
                    {
                        obs.Timestamp = obsDate.Add(obsTime);
                    }
                    
                    obs.ObservedX = ParseDouble(obsMatch.Groups[3].Value);
                    obs.ObservedY = ParseDouble(obsMatch.Groups[4].Value);
                    
                    if (obsMatch.Groups[5].Success)
                    {
                        obs.ObservedZ = ParseDouble(obsMatch.Groups[5].Value);
                    }
                    
                    observations.Add(obs);
                }
            }
        }
        
        // Set observations and number of fixes
        if (observations.Any())
        {
            fix.Observations = observations;
            fix.NumberOfFixes = observations.Count;
            
            // If no average coordinates were found, calculate from observations
            if (fix.Easting == 0 && fix.Northing == 0)
            {
                fix.Easting = observations.Average(o => o.ObservedX);
                fix.Northing = observations.Average(o => o.ObservedY);
                if (observations.All(o => o.ObservedZ.HasValue))
                {
                    fix.Height = observations.Average(o => o.ObservedZ!.Value);
                }
            }
        }
        
        // Validate we got meaningful data
        if (!foundStats && observations.Count == 0)
        {
            _parseWarnings.Add("No statistics or observations found in file");
        }
        
        return fix;
    }
    
    /// <summary>
    /// Determines the fix type from file name and content.
    /// </summary>
    public LogEntryType DetermineFixType(string fileName, PositionFix? fix)
    {
        var upperName = fileName.ToUpperInvariant();
        
        if (upperName.Contains("CAL") || upperName.Contains("CALIBRAT"))
            return LogEntryType.CalibrationFix;
        
        if (upperName.Contains("VER") || upperName.Contains("VERIF"))
            return LogEntryType.VerificationFix;
        
        if (upperName.Contains("SET") && (upperName.Contains("EAST") || upperName.Contains("NORTH")))
            return LogEntryType.SetEastingNorthing;
        
        // Check file content for clues
        if (fix?.ObjectMonitored?.ToUpperInvariant().Contains("CAL") == true)
            return LogEntryType.CalibrationFix;
        
        return LogEntryType.PositionFix;
    }
    
    /// <summary>
    /// Extracts vehicle name from object monitored string.
    /// </summary>
    public string ExtractVehicleName(string? objectMonitored)
    {
        if (string.IsNullOrWhiteSpace(objectMonitored))
            return string.Empty;
        
        // Remove leading numbers (e.g., "0000 Island Performer" -> "Island Performer")
        var cleaned = Regex.Replace(objectMonitored.Trim(), @"^\d+\s*", "");
        return cleaned;
    }
    
    /// <summary>
    /// Quick check if a file is a valid .npc file.
    /// </summary>
    public bool IsValidNpcFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;
            
            // Read first few lines to check format
            using var reader = new StreamReader(filePath);
            var firstLine = reader.ReadLine();
            
            return firstLine?.Contains("EIVA") == true || 
                   firstLine?.Contains("calibration") == true ||
                   firstLine?.Contains("XYZ") == true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Helper to parse double with invariant culture.
    /// </summary>
    private static double ParseDouble(string value)
    {
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0;
    }
}
