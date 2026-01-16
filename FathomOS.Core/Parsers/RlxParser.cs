using System.IO;
using System.Globalization;
using FathomOS.Core.Models;

namespace FathomOS.Core.Parsers;

/// <summary>
/// Parser for EIVA RLX route files.
/// 
/// RLX Format (from EIVA documentation):
/// Header line: "Route Name"; runline_type; offset; "Unit"
/// Segment lines: start_E; start_N; end_E; end_N; start_KP; end_KP; radius; status; segment_type
/// 
/// Segment types: 64 = straight line, 128 = circular arc
/// Radius: 0 for straight, positive for counter-clockwise arc, negative for clockwise arc
/// </summary>
public class RlxParser
{
    /// <summary>
    /// Parse an RLX route file
    /// </summary>
    /// <param name="filePath">Path to the RLX file</param>
    /// <returns>Parsed route data</returns>
    public RouteData Parse(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"RLX file not found: {filePath}");

        var lines = File.ReadAllLines(filePath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count < 2)
            throw new FormatException("RLX file must contain at least a header and one segment");

        var routeData = new RouteData
        {
            SourceFile = filePath
        };

        // Parse header line
        ParseHeader(lines[0], routeData);

        // Parse segment lines
        for (int i = 1; i < lines.Count; i++)
        {
            var segment = ParseSegment(lines[i], i + 1);
            if (segment != null)
            {
                routeData.Segments.Add(segment);
            }
        }

        return routeData;
    }

    /// <summary>
    /// Parse an RLX route file from a stream
    /// </summary>
    public RouteData Parse(Stream stream, string fileName = "unknown.rlx")
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, content);
        
        try
        {
            var result = Parse(tempPath);
            result.SourceFile = fileName;
            return result;
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Parse the header line of an RLX file
    /// Format: "Route Name"; runline_type; offset; "Unit"
    /// </summary>
    private void ParseHeader(string headerLine, RouteData routeData)
    {
        // Split by semicolon
        var parts = SplitRlxLine(headerLine);

        if (parts.Length < 4)
            throw new FormatException($"Invalid RLX header format. Expected 4 fields, got {parts.Length}");

        // Parse route name (may be quoted)
        routeData.Name = parts[0].Trim().Trim('"');

        // Parse runline type
        if (!int.TryParse(parts[1].Trim(), out int runlineType))
            throw new FormatException($"Invalid runline type: {parts[1]}");
        routeData.RunlineType = runlineType;

        // Parse offset
        if (!double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
            throw new FormatException($"Invalid offset value: {parts[2]}");
        routeData.Offset = offset;

        // Parse unit
        string unitStr = parts[3].Trim().Trim('"');
        routeData.OriginalUnitString = unitStr;
        routeData.CoordinateUnit = LengthUnitExtensions.ParseFromRlx(unitStr);
    }

    /// <summary>
    /// Parse a segment line
    /// Format: start_E; start_N; end_E; end_N; start_KP; end_KP; radius; status; segment_type
    /// </summary>
    private RouteSegment? ParseSegment(string line, int lineNumber)
    {
        var parts = SplitRlxLine(line);

        if (parts.Length < 9)
        {
            // Skip malformed lines with a warning
            Console.WriteLine($"Warning: Skipping malformed segment on line {lineNumber}, expected 9 fields, got {parts.Length}");
            return null;
        }

        try
        {
            var segment = new RouteSegment
            {
                StartEasting = ParseDouble(parts[0], "StartEasting", lineNumber),
                StartNorthing = ParseDouble(parts[1], "StartNorthing", lineNumber),
                EndEasting = ParseDouble(parts[2], "EndEasting", lineNumber),
                EndNorthing = ParseDouble(parts[3], "EndNorthing", lineNumber),
                StartKp = ParseDouble(parts[4], "StartKP", lineNumber),
                EndKp = ParseDouble(parts[5], "EndKP", lineNumber),
                Radius = ParseDouble(parts[6], "Radius", lineNumber),
                Status = ParseInt(parts[7], "Status", lineNumber),
                TypeCode = ParseInt(parts[8], "SegmentType", lineNumber)
            };

            return segment;
        }
        catch (Exception ex)
        {
            throw new FormatException($"Error parsing segment on line {lineNumber}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Split an RLX line by semicolons, handling potential edge cases
    /// </summary>
    private string[] SplitRlxLine(string line)
    {
        // RLX uses semicolon as delimiter
        return line.Split(';');
    }

    /// <summary>
    /// Parse a double value with proper error reporting
    /// </summary>
    private double ParseDouble(string value, string fieldName, int lineNumber)
    {
        if (double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;

        throw new FormatException($"Invalid {fieldName} value '{value}' on line {lineNumber}");
    }

    /// <summary>
    /// Parse an integer value with proper error reporting
    /// </summary>
    private int ParseInt(string value, string fieldName, int lineNumber)
    {
        if (int.TryParse(value.Trim(), out int result))
            return result;

        throw new FormatException($"Invalid {fieldName} value '{value}' on line {lineNumber}");
    }

    /// <summary>
    /// Validate that a file is a valid RLX file without fully parsing it
    /// </summary>
    public bool IsValidRlxFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var firstLine = File.ReadLines(filePath).FirstOrDefault();
            if (string.IsNullOrEmpty(firstLine))
                return false;

            var parts = SplitRlxLine(firstLine);
            return parts.Length >= 4;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get a quick summary of an RLX file without full parsing
    /// </summary>
    public (string RouteName, string Unit, int SegmentCount)? GetFileSummary(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (lines.Count < 1)
                return null;

            var headerParts = SplitRlxLine(lines[0]);
            if (headerParts.Length < 4)
                return null;

            string name = headerParts[0].Trim().Trim('"');
            string unit = headerParts[3].Trim().Trim('"');
            int segmentCount = lines.Count - 1;

            return (name, unit, segmentCount);
        }
        catch
        {
            return null;
        }
    }
}
