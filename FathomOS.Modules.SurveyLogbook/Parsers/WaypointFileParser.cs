// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Parsers/WaypointFileParser.cs
// Purpose: Parser for EIVA NaviPac .wp2 waypoint files
// ============================================================================

using System.Globalization;
using System.IO;

namespace FathomOS.Modules.SurveyLogbook.Parsers;

/// <summary>
/// Parses EIVA NaviPac .wp2 waypoint files.
/// 
/// Sample file format (semicolon-delimited):
/// ""; 2520289.710; 10130994.900; 0.000; 7.1; 3.1; 7.1; ""; 0.00; -8.1; ""; 0.00; ""; 1; 0.000; 0.000; 0.000; 0; 0.05
/// 
/// Fields:
/// 0: Name (string in quotes)
/// 1: Easting (double)
/// 2: Northing (double)
/// 3: Height/Depth (double)
/// 4-6: Additional coordinates or parameters
/// 7: Description (string in quotes)
/// ... additional fields vary by NaviPac version
/// </summary>
public class WaypointFileParser
{
    private readonly List<string> _parseWarnings = new();
    
    /// <summary>
    /// Gets any warnings generated during parsing.
    /// </summary>
    public IReadOnlyList<string> ParseWarnings => _parseWarnings.AsReadOnly();
    
    /// <summary>
    /// Parses a .wp2 file and returns a list of waypoints.
    /// </summary>
    /// <param name="filePath">Path to the .wp2 file</param>
    /// <returns>List of parsed waypoints</returns>
    public List<Waypoint> Parse(string filePath)
    {
        _parseWarnings.Clear();
        var waypoints = new List<Waypoint>();
        
        if (!File.Exists(filePath))
        {
            _parseWarnings.Add($"File not found: {filePath}");
            return waypoints;
        }
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            int lineNumber = 0;
            
            foreach (var line in lines)
            {
                lineNumber++;
                
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var waypoint = ParseLine(line, lineNumber);
                if (waypoint != null)
                {
                    waypoint.SourceFile = filePath;
                    waypoint.LineNumber = lineNumber;
                    waypoints.Add(waypoint);
                }
            }
        }
        catch (Exception ex)
        {
            _parseWarnings.Add($"Error reading file: {ex.Message}");
        }
        
        return waypoints;
    }
    
    /// <summary>
    /// Parses a single line from a .wp2 file.
    /// </summary>
    private Waypoint? ParseLine(string line, int lineNumber)
    {
        try
        {
            // Split by semicolon
            var fields = SplitWp2Line(line);
            
            if (fields.Count < 3)
            {
                _parseWarnings.Add($"Line {lineNumber}: Not enough fields (expected at least 3, got {fields.Count})");
                return null;
            }
            
            var waypoint = new Waypoint
            {
                Name = CleanQuotedString(fields[0]),
                Easting = ParseDouble(fields[1]),
                Northing = ParseDouble(fields[2])
            };
            
            // Parse optional fields
            if (fields.Count > 3)
                waypoint.Height = ParseDouble(fields[3]);
            
            // Fields 4-6 could be additional coordinates or parameters
            if (fields.Count > 4)
                waypoint.Parameter1 = ParseDouble(fields[4]);
            if (fields.Count > 5)
                waypoint.Parameter2 = ParseDouble(fields[5]);
            if (fields.Count > 6)
                waypoint.Parameter3 = ParseDouble(fields[6]);
            
            // Field 7 is often a description
            if (fields.Count > 7)
                waypoint.Description = CleanQuotedString(fields[7]);
            
            // Field 8-9 additional parameters
            if (fields.Count > 8)
                waypoint.Parameter4 = ParseDouble(fields[8]);
            if (fields.Count > 9)
                waypoint.Parameter5 = ParseDouble(fields[9]);
            
            // Field 10 might be another string
            if (fields.Count > 10)
                waypoint.AdditionalInfo = CleanQuotedString(fields[10]);
            
            // Additional numeric parameters
            if (fields.Count > 13)
                waypoint.TypeCode = ParseInt(fields[13]);
            
            return waypoint;
        }
        catch (Exception ex)
        {
            _parseWarnings.Add($"Line {lineNumber}: Parse error - {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Splits a .wp2 line by semicolon, handling quoted strings.
    /// </summary>
    private List<string> SplitWp2Line(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;
        
        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                currentField.Append(c);
            }
            else if (c == ';' && !inQuotes)
            {
                fields.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }
        
        // Add the last field
        fields.Add(currentField.ToString().Trim());
        
        return fields;
    }
    
    /// <summary>
    /// Removes quotes from a string field.
    /// </summary>
    private string CleanQuotedString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        
        return value.Trim().Trim('"').Trim();
    }
    
    /// <summary>
    /// Parses a double value.
    /// </summary>
    private double ParseDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        
        var cleaned = value.Trim().Trim('"');
        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        
        return 0;
    }
    
    /// <summary>
    /// Parses an integer value.
    /// </summary>
    private int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        
        var cleaned = value.Trim().Trim('"');
        if (int.TryParse(cleaned, out var result))
            return result;
        
        return 0;
    }
    
    /// <summary>
    /// Quick check if a file is a valid .wp2 file.
    /// </summary>
    public bool IsValidWp2File(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;
            
            using var reader = new StreamReader(filePath);
            var firstLine = reader.ReadLine();
            
            // Check if it has semicolon-separated format
            return firstLine?.Contains(';') == true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Compares two waypoint files and returns the differences.
    /// </summary>
    public WaypointDifference CompareFiles(string oldFilePath, string newFilePath)
    {
        var oldWaypoints = Parse(oldFilePath);
        var newWaypoints = Parse(newFilePath);
        
        var diff = new WaypointDifference();
        
        // Find added waypoints (in new but not in old)
        foreach (var newWp in newWaypoints)
        {
            var oldWp = oldWaypoints.FirstOrDefault(w => 
                w.Name == newWp.Name && 
                Math.Abs(w.Easting - newWp.Easting) < 0.001 &&
                Math.Abs(w.Northing - newWp.Northing) < 0.001);
            
            if (oldWp == null)
            {
                diff.Added.Add(newWp);
            }
        }
        
        // Find deleted waypoints (in old but not in new)
        foreach (var oldWp in oldWaypoints)
        {
            var newWp = newWaypoints.FirstOrDefault(w =>
                w.Name == oldWp.Name &&
                Math.Abs(w.Easting - oldWp.Easting) < 0.001 &&
                Math.Abs(w.Northing - oldWp.Northing) < 0.001);
            
            if (newWp == null)
            {
                diff.Deleted.Add(oldWp);
            }
        }
        
        // Find modified waypoints (same name but different coordinates)
        foreach (var newWp in newWaypoints)
        {
            var oldWp = oldWaypoints.FirstOrDefault(w => w.Name == newWp.Name);
            if (oldWp != null)
            {
                if (Math.Abs(oldWp.Easting - newWp.Easting) > 0.001 ||
                    Math.Abs(oldWp.Northing - newWp.Northing) > 0.001 ||
                    Math.Abs(oldWp.Height - newWp.Height) > 0.001)
                {
                    diff.Modified.Add(new WaypointModification
                    {
                        OldWaypoint = oldWp,
                        NewWaypoint = newWp
                    });
                }
            }
        }
        
        return diff;
    }
}

/// <summary>
/// Represents a waypoint from a .wp2 file.
/// </summary>
public class Waypoint
{
    /// <summary>Waypoint name/identifier.</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Easting coordinate.</summary>
    public double Easting { get; set; }
    
    /// <summary>Northing coordinate.</summary>
    public double Northing { get; set; }
    
    /// <summary>Height or depth value.</summary>
    public double Height { get; set; }
    
    /// <summary>Description or comment.</summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>Additional information field.</summary>
    public string AdditionalInfo { get; set; } = string.Empty;
    
    /// <summary>Waypoint type code.</summary>
    public int TypeCode { get; set; }
    
    /// <summary>Parameter fields from wp2 format.</summary>
    public double Parameter1 { get; set; }
    public double Parameter2 { get; set; }
    public double Parameter3 { get; set; }
    public double Parameter4 { get; set; }
    public double Parameter5 { get; set; }
    
    /// <summary>Source file path.</summary>
    public string SourceFile { get; set; } = string.Empty;
    
    /// <summary>Line number in source file.</summary>
    public int LineNumber { get; set; }
    
    /// <summary>Timestamp when waypoint was detected/modified.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Gets a display-friendly coordinate string.
    /// </summary>
    public string CoordinateDisplay => $"E: {Easting:F3}, N: {Northing:F3}";
    
    /// <summary>
    /// Gets a display-friendly full description.
    /// </summary>
    public string FullDisplay => string.IsNullOrEmpty(Name) 
        ? CoordinateDisplay 
        : $"{Name}: {CoordinateDisplay}";
    
    public override string ToString() => FullDisplay;
}

/// <summary>
/// Represents differences between two waypoint files.
/// </summary>
public class WaypointDifference
{
    /// <summary>Waypoints added (present in new file only).</summary>
    public List<Waypoint> Added { get; } = new();
    
    /// <summary>Waypoints deleted (present in old file only).</summary>
    public List<Waypoint> Deleted { get; } = new();
    
    /// <summary>Waypoints modified (same name, different values).</summary>
    public List<WaypointModification> Modified { get; } = new();
    
    /// <summary>Returns true if there are any changes.</summary>
    public bool HasChanges => Added.Count > 0 || Deleted.Count > 0 || Modified.Count > 0;
    
    /// <summary>Total number of changes.</summary>
    public int TotalChanges => Added.Count + Deleted.Count + Modified.Count;
}

/// <summary>
/// Represents a modification to a waypoint.
/// </summary>
public class WaypointModification
{
    public Waypoint OldWaypoint { get; set; } = null!;
    public Waypoint NewWaypoint { get; set; } = null!;
    
    public string ChangeDescription
    {
        get
        {
            var changes = new List<string>();
            
            if (Math.Abs(OldWaypoint.Easting - NewWaypoint.Easting) > 0.001)
                changes.Add($"E: {OldWaypoint.Easting:F3} → {NewWaypoint.Easting:F3}");
            
            if (Math.Abs(OldWaypoint.Northing - NewWaypoint.Northing) > 0.001)
                changes.Add($"N: {OldWaypoint.Northing:F3} → {NewWaypoint.Northing:F3}");
            
            if (Math.Abs(OldWaypoint.Height - NewWaypoint.Height) > 0.001)
                changes.Add($"H: {OldWaypoint.Height:F3} → {NewWaypoint.Height:F3}");
            
            return string.Join(", ", changes);
        }
    }
}
