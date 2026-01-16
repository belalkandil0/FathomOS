using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FathomOS.Core.Parsers;
using FathomOS.Core.Models;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Parsers;

/// <summary>
/// Parser for NPD (NaviPac Data) files containing USBL verification data.
/// Uses the Core NpdParser for reliable parsing with NaviPac date/time offset handling.
/// </summary>
public class UsblNpdParser
{
    private readonly NpdParser _coreParser = new();
    private readonly List<string> _parseWarnings = new();
    
    public IReadOnlyList<string> ParseWarnings => _parseWarnings;
    
    /// <summary>
    /// Parse an NPD file with auto-detected column mapping
    /// </summary>
    public List<UsblObservation> Parse(string filePath)
    {
        var mapping = CreateUsblColumnMapping(filePath);
        return Parse(filePath, mapping);
    }
    
    /// <summary>
    /// Parse an NPD file using explicit column indices
    /// </summary>
    public List<UsblObservation> Parse(string filePath, UsblColumnMapping usblMapping)
    {
        _parseWarnings.Clear();
        var observations = new List<UsblObservation>();
        
        if (!File.Exists(filePath))
        {
            _parseWarnings.Add($"File not found: {filePath}");
            return observations;
        }
        
        try
        {
            // Create Core ColumnMapping from USBL mapping
            var coreMapping = new ColumnMapping
            {
                Name = "USBL Verification",
                HasDateTimeSplit = usblMapping.HasDateTimeSplit,
                DateFormat = usblMapping.DateFormat,
                TimeFormat = usblMapping.TimeFormat,
                TimeColumnIndex = usblMapping.DateColumn,
                EastingColumnIndex = usblMapping.VesselEastingColumn,
                NorthingColumnIndex = usblMapping.VesselNorthingColumn,
                HeadingColumnIndex = usblMapping.VesselGyroColumn
            };
            
            // Use Core parser to get base data
            var result = _coreParser.Parse(filePath, coreMapping);
            
            // Copy warnings from core parser
            foreach (var warning in _coreParser.ParseWarnings)
            {
                _parseWarnings.Add(warning);
            }
            
            if (result.Points.Count == 0)
            {
                _parseWarnings.Add("No data points parsed from file.");
                return observations;
            }
            
            // Read file lines for extracting USBL-specific columns
            var lines = File.ReadAllLines(filePath);
            int dataStartRow = usblMapping.HeaderRows;
            
            // Calculate the date/time offset for NaviPac files
            int columnOffset = usblMapping.HasDateTimeSplit ? 1 : 0;
            
            int pointIndex = 0;
            for (int lineNum = dataStartRow; lineNum < lines.Length && pointIndex < result.Points.Count; lineNum++)
            {
                var line = lines[lineNum].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                try
                {
                    var parts = SplitCsvLine(line, usblMapping.Delimiter);
                    var basePoint = result.Points[pointIndex];
                    
                    // Extract USBL-specific values with offset applied
                    var observation = new UsblObservation
                    {
                        Index = pointIndex + 1,
                        DateTime = basePoint.Timestamp ?? DateTime.MinValue,
                        VesselEasting = basePoint.Easting,
                        VesselNorthing = basePoint.Northing,
                        VesselGyro = basePoint.Heading ?? 0,
                        
                        // Extract transponder data (apply offset for NaviPac)
                        TransponderEasting = TryGetDouble(parts, usblMapping.TransponderEastingColumn + columnOffset),
                        TransponderNorthing = TryGetDouble(parts, usblMapping.TransponderNorthingColumn + columnOffset),
                        TransponderDepth = TryGetDouble(parts, usblMapping.TransponderDepthColumn + columnOffset)
                    };
                    
                    observations.Add(observation);
                    pointIndex++;
                }
                catch (Exception ex)
                {
                    _parseWarnings.Add($"Line {lineNum + 1}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _parseWarnings.Add($"Error reading file: {ex.Message}");
        }
        
        return observations;
    }
    
    /// <summary>
    /// Get column headers from an NPD file
    /// </summary>
    public List<string> GetHeaders(string filePath, char delimiter = '\t')
    {
        try
        {
            // Use Core parser method if available
            var columns = _coreParser.GetAllColumns(filePath);
            return columns.Select((h, i) => $"[{i}] {h}").ToList();
        }
        catch
        {
            // Fallback to simple parsing
            try
            {
                var firstLine = File.ReadLines(filePath).FirstOrDefault();
                if (string.IsNullOrEmpty(firstLine))
                    return new List<string>();
                
                return firstLine.Split(delimiter)
                    .Select((h, i) => $"[{i}] {h.Trim()}")
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
    
    /// <summary>
    /// Preview file data (first N rows)
    /// </summary>
    public List<string[]> PreviewFile(string filePath, int maxRows = 10, char delimiter = '\t')
    {
        var result = new List<string[]>();
        try
        {
            var lines = File.ReadLines(filePath).Take(maxRows);
            foreach (var line in lines)
            {
                result.Add(SplitCsvLine(line, delimiter));
            }
        }
        catch { }
        
        return result;
    }
    
    /// <summary>
    /// Auto-detect USBL column mapping based on header patterns
    /// Enhanced with multiple pattern matching for better accuracy
    /// </summary>
    public UsblColumnMapping AutoDetectMapping(string filePath, char delimiter = '\t')
    {
        var mapping = new UsblColumnMapping 
        { 
            Delimiter = delimiter,
            HasDateTimeSplit = true // Default for NaviPac files
        };
        
        try
        {
            var headers = File.ReadLines(filePath).FirstOrDefault()?.Split(delimiter) ?? Array.Empty<string>();
            
            // Pattern lists for each column type (order matters - more specific first)
            var dateTimePatterns = new[] { "time", "date", "datetime", "timestamp" };
            var gyroPatterns = new[] { "gyro", "^hdg$", "^heading$", "vessel.*heading", "ship.*heading", "vsl.*heading" };
            var vesselEastPatterns = new[] { "vessel.*east", "vsl.*east", "ship.*east", "vessel.*x", "surface.*east" };
            var vesselNorthPatterns = new[] { "vessel.*north", "vsl.*north", "ship.*north", "vessel.*y", "surface.*north" };
            var tpEastPatterns = new[] { "tp.*east", "transponder.*east", "usbl.*east", "beacon.*east", "acoustic.*east", "subsea.*east", "target.*east", "rov.*east" };
            var tpNorthPatterns = new[] { "tp.*north", "transponder.*north", "usbl.*north", "beacon.*north", "acoustic.*north", "subsea.*north", "target.*north", "rov.*north" };
            var tpDepthPatterns = new[] { "tp.*depth", "tp.*height", "tp.*z", "transponder.*depth", "transponder.*height", "usbl.*depth", "usbl.*height", "beacon.*depth", "beacon.*height", "acoustic.*depth", "subsea.*depth", "target.*depth", "rov.*depth" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim();
                var headerLower = header.ToLowerInvariant();
                
                // Date/Time detection
                if (mapping.DateColumn < 0 && MatchesAnyPattern(headerLower, dateTimePatterns))
                {
                    mapping.DateColumn = i;
                    mapping.TimeColumn = i + 1; // NaviPac: Time header = Date,Time data
                }
                
                // Gyro/Heading (must not contain "tp" or "transponder")
                if (mapping.VesselGyroColumn < 0 && MatchesAnyPattern(headerLower, gyroPatterns) &&
                    !headerLower.Contains("tp") && !headerLower.Contains("transponder"))
                {
                    mapping.VesselGyroColumn = i;
                }
                
                // Vessel Easting
                if (mapping.VesselEastingColumn < 0 && MatchesAnyPattern(headerLower, vesselEastPatterns))
                {
                    mapping.VesselEastingColumn = i;
                }
                
                // Vessel Northing
                if (mapping.VesselNorthingColumn < 0 && MatchesAnyPattern(headerLower, vesselNorthPatterns))
                {
                    mapping.VesselNorthingColumn = i;
                }
                
                // Transponder Easting
                if (mapping.TransponderEastingColumn < 0 && MatchesAnyPattern(headerLower, tpEastPatterns))
                {
                    mapping.TransponderEastingColumn = i;
                }
                
                // Transponder Northing
                if (mapping.TransponderNorthingColumn < 0 && MatchesAnyPattern(headerLower, tpNorthPatterns))
                {
                    mapping.TransponderNorthingColumn = i;
                }
                
                // Transponder Depth
                if (mapping.TransponderDepthColumn < 0 && MatchesAnyPattern(headerLower, tpDepthPatterns))
                {
                    mapping.TransponderDepthColumn = i;
                }
            }
            
            // Fallback: if we didn't find vessel-specific columns, look for generic east/north
            if (mapping.VesselEastingColumn < 0 || mapping.VesselNorthingColumn < 0)
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    var header = headers[i].ToLowerInvariant().Trim();
                    
                    // Skip if this is already the transponder column
                    if (i == mapping.TransponderEastingColumn || i == mapping.TransponderNorthingColumn)
                        continue;
                    
                    // Generic easting (but not transponder)
                    if (mapping.VesselEastingColumn < 0 && 
                        (header.Contains("east") || header == "e" || header == "x") &&
                        !header.Contains("tp") && !header.Contains("transponder") && !header.Contains("usbl") && !header.Contains("beacon"))
                    {
                        mapping.VesselEastingColumn = i;
                    }
                    
                    // Generic northing (but not transponder)
                    if (mapping.VesselNorthingColumn < 0 && 
                        (header.Contains("north") || header == "n" || header == "y") &&
                        !header.Contains("tp") && !header.Contains("transponder") && !header.Contains("usbl") && !header.Contains("beacon"))
                    {
                        mapping.VesselNorthingColumn = i;
                    }
                }
            }
            
            // Detect NaviPac date/time split by examining first data line
            DetectNaviPacFormat(filePath, mapping, delimiter);
        }
        catch { }
        
        return mapping;
    }
    
    /// <summary>
    /// Check if header matches any of the regex patterns
    /// </summary>
    private bool MatchesAnyPattern(string header, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith("^"))
            {
                // Regex pattern
                if (System.Text.RegularExpressions.Regex.IsMatch(header, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
            }
            else if (pattern.Contains(".*"))
            {
                // Wildcard pattern - convert to regex
                var regexPattern = "^" + pattern.Replace(".*", ".*") + "$";
                if (System.Text.RegularExpressions.Regex.IsMatch(header, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
                
                // Also check for contains
                if (header.Contains(pattern.Replace(".*", "")))
                    return true;
            }
            else
            {
                // Simple contains check
                if (header.Contains(pattern))
                    return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Detect if file uses NaviPac date/time split format
    /// </summary>
    private void DetectNaviPacFormat(string filePath, UsblColumnMapping mapping, char delimiter)
    {
        try
        {
            var lines = File.ReadLines(filePath).Take(3).ToList();
            if (lines.Count < 2) return;
            
            var headers = lines[0].Split(delimiter);
            var firstDataLine = lines[1].Split(delimiter);
            
            // NaviPac indicator: data line has more columns than header
            if (firstDataLine.Length > headers.Length)
            {
                mapping.HasDateTimeSplit = true;
                return;
            }
            
            // Check if first two data values look like date and time
            if (firstDataLine.Length >= 2)
            {
                var val1 = firstDataLine[0].Trim();
                var val2 = firstDataLine[1].Trim();
                
                // Check for date patterns: dd/MM/yyyy, MM/dd/yyyy, yyyy-MM-dd
                bool val1IsDate = System.Text.RegularExpressions.Regex.IsMatch(val1, @"^\d{1,4}[/-]\d{1,2}[/-]\d{1,4}$");
                
                // Check for time patterns: HH:mm:ss, HH:mm:ss.fff
                bool val2IsTime = System.Text.RegularExpressions.Regex.IsMatch(val2, @"^\d{1,2}:\d{2}(:\d{2})?(\.\d+)?$");
                
                if (val1IsDate && val2IsTime)
                {
                    mapping.HasDateTimeSplit = true;
                }
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Create a USBL column mapping with sensible defaults based on file analysis
    /// </summary>
    private UsblColumnMapping CreateUsblColumnMapping(string filePath)
    {
        return AutoDetectMapping(filePath);
    }
    
    /// <summary>
    /// Split a CSV line handling quoted fields
    /// </summary>
    private string[] SplitCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        
        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        
        return result.ToArray();
    }
    
    /// <summary>
    /// Safely extract a double value from array at specified index
    /// </summary>
    private double TryGetDouble(string[] parts, int index)
    {
        if (index < 0 || index >= parts.Length)
            return 0;
            
        if (double.TryParse(parts[index].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
            
        return 0;
    }
}
