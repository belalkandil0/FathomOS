namespace FathomOS.Core.Parsers;

using FathomOS.Core.Models;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Parser for NaviPac NPD survey data files.
/// 
/// CRITICAL: The NPD format has a date/time parsing trick:
/// - Header line shows "Time" as ONE column
/// - Data lines have "Date,Time" as TWO comma-separated values
/// 
/// Example:
/// Header: "Time,Position: HD11 Sprint: East,North,..."
/// Data:   "22/08/2025,22:57:08, 2105742.1057,9768181.7685,..."
/// 
/// This means all column indices after Time are offset by +1 in the data!
/// </summary>
public class NpdParser
{
    private readonly List<string> _parseWarnings = new();

    /// <summary>
    /// Warnings generated during parsing
    /// </summary>
    public IReadOnlyList<string> ParseWarnings => _parseWarnings;

    /// <summary>
    /// Parse an NPD file using the specified column mapping
    /// </summary>
    public NpdParseResult Parse(string filePath, ColumnMapping mapping)
    {
        _parseWarnings.Clear();

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"NPD file not found: {filePath}");

        var lines = File.ReadAllLines(filePath);
        
        if (lines.Length < 2)
            throw new FormatException("NPD file must contain at least a header and one data row");

        var result = new NpdParseResult
        {
            SourceFile = filePath
        };

        // Parse header line to detect columns
        var headerLine = lines[0];
        var headerColumns = ParseCsvLine(headerLine);
        result.HeaderColumns = headerColumns.ToList();

        // Detect column indices from header
        var columnIndices = DetectColumnIndices(headerColumns, mapping);
        result.DetectedMapping = columnIndices;

        // Parse data lines
        int recordNumber = 1;
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var point = ParseDataLine(line, columnIndices, mapping, recordNumber, i + 1);
                if (point != null)
                {
                    result.Points.Add(point);
                    recordNumber++;
                }
            }
            catch (Exception ex)
            {
                _parseWarnings.Add($"Line {i + 1}: {ex.Message}");
            }
        }

        // Calculate statistics
        result.CalculateStatistics();

        return result;
    }

    /// <summary>
    /// Detect column indices from header line
    /// </summary>
    private DetectedColumnIndices DetectColumnIndices(string[] headers, ColumnMapping mapping)
    {
        var indices = new DetectedColumnIndices();

        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim();

            // Time column
            if (indices.TimeIndex < 0 && MatchesPattern(header, mapping.TimeColumnPattern))
            {
                indices.TimeIndex = i;
                indices.TimeColumnName = header;
            }

            // Easting column
            if (indices.EastingIndex < 0 && MatchesPattern(header, mapping.EastingColumnPattern))
            {
                indices.EastingIndex = i;
                indices.EastingColumnName = header;
            }

            // Northing column
            if (indices.NorthingIndex < 0 && MatchesPattern(header, mapping.NorthingColumnPattern))
            {
                indices.NorthingIndex = i;
                indices.NorthingColumnName = header;
            }

            // Depth column - may need specific matching
            if (indices.DepthIndex < 0 && MatchesPattern(header, mapping.DepthColumnPattern))
            {
                indices.DepthIndex = i;
                indices.DepthColumnName = header;
            }

            // Altitude column
            if (indices.AltitudeIndex < 0 && MatchesPattern(header, mapping.AltitudeColumnPattern))
            {
                indices.AltitudeIndex = i;
                indices.AltitudeColumnName = header;
            }

            // Heading column
            if (indices.HeadingIndex < 0 && MatchesPattern(header, mapping.HeadingColumnPattern))
            {
                indices.HeadingIndex = i;
                indices.HeadingColumnName = header;
            }
        }

        // Apply manual overrides if specified
        if (mapping.TimeColumnIndex >= 0) indices.TimeIndex = mapping.TimeColumnIndex;
        if (mapping.EastingColumnIndex >= 0) indices.EastingIndex = mapping.EastingColumnIndex;
        if (mapping.NorthingColumnIndex >= 0) indices.NorthingIndex = mapping.NorthingColumnIndex;
        if (mapping.DepthColumnIndex >= 0) indices.DepthIndex = mapping.DepthColumnIndex;
        if (mapping.AltitudeColumnIndex >= 0) indices.AltitudeIndex = mapping.AltitudeColumnIndex;
        if (mapping.HeadingColumnIndex >= 0) indices.HeadingIndex = mapping.HeadingColumnIndex;

        indices.HasDateTimeSplit = mapping.HasDateTimeSplit;

        return indices;
    }

    /// <summary>
    /// Parse a single data line
    /// </summary>
    private SurveyPoint? ParseDataLine(string line, DetectedColumnIndices indices, 
        ColumnMapping mapping, int recordNumber, int lineNumber)
    {
        var values = ParseCsvLine(line);

        // THE CRITICAL TRICK:
        // If HasDateTimeSplit is true, the "Time" column in header becomes "Date,Time" in data
        // So we need to offset all column indices by +1 for columns AFTER the time column
        int offset = indices.HasDateTimeSplit ? 1 : 0;

        var point = new SurveyPoint
        {
            RecordNumber = recordNumber
        };

        // Parse DateTime
        if (indices.TimeIndex >= 0)
        {
            if (indices.HasDateTimeSplit)
            {
                // Date is at TimeIndex, Time is at TimeIndex+1
                string dateStr = GetValueSafe(values, indices.TimeIndex);
                string timeStr = GetValueSafe(values, indices.TimeIndex + 1);
                point.DateTime = ParseDateTime(dateStr, timeStr, mapping.DateFormat, mapping.TimeFormat, lineNumber);
            }
            else
            {
                // Combined datetime
                string dateTimeStr = GetValueSafe(values, indices.TimeIndex);
                point.DateTime = ParseCombinedDateTime(dateTimeStr, mapping, lineNumber);
            }
        }

        // Parse Easting (apply offset for columns after Time)
        if (indices.EastingIndex >= 0)
        {
            int actualIndex = indices.EastingIndex > indices.TimeIndex 
                ? indices.EastingIndex + offset 
                : indices.EastingIndex;
            point.Easting = ParseDoubleValue(values, actualIndex, "Easting", lineNumber);
        }

        // Parse Northing
        if (indices.NorthingIndex >= 0)
        {
            int actualIndex = indices.NorthingIndex > indices.TimeIndex 
                ? indices.NorthingIndex + offset 
                : indices.NorthingIndex;
            point.Northing = ParseDoubleValue(values, actualIndex, "Northing", lineNumber);
        }

        // Parse Depth
        if (indices.DepthIndex >= 0)
        {
            int actualIndex = indices.DepthIndex > indices.TimeIndex 
                ? indices.DepthIndex + offset 
                : indices.DepthIndex;
            point.Depth = ParseOptionalDouble(values, actualIndex);
            point.DepthSource = indices.DepthColumnName;
        }

        // Parse Altitude
        if (indices.AltitudeIndex >= 0)
        {
            int actualIndex = indices.AltitudeIndex > indices.TimeIndex 
                ? indices.AltitudeIndex + offset 
                : indices.AltitudeIndex;
            point.Altitude = ParseOptionalDouble(values, actualIndex);
        }

        // Parse Heading
        if (indices.HeadingIndex >= 0)
        {
            int actualIndex = indices.HeadingIndex > indices.TimeIndex 
                ? indices.HeadingIndex + offset 
                : indices.HeadingIndex;
            point.Heading = ParseOptionalDouble(values, actualIndex);
        }

        // Validate minimum required data
        if (!point.HasValidCoordinates)
        {
            _parseWarnings.Add($"Line {lineNumber}: Invalid or missing coordinates");
            return null;
        }

        return point;
    }

    /// <summary>
    /// Parse CSV line handling quoted values
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    /// <summary>
    /// Check if a header matches a pattern (supports regex-like alternation with |)
    /// </summary>
    private bool MatchesPattern(string header, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        // Support pipe-separated alternatives
        var alternatives = pattern.Split('|');
        foreach (var alt in alternatives)
        {
            if (header.IndexOf(alt.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Safely get a value from array
    /// </summary>
    private string GetValueSafe(string[] values, int index)
    {
        if (index < 0 || index >= values.Length)
            return string.Empty;
        return values[index].Trim();
    }

    /// <summary>
    /// Parse split date and time strings
    /// </summary>
    private DateTime ParseDateTime(string dateStr, string timeStr, 
        string dateFormat, string timeFormat, int lineNumber)
    {
        dateStr = dateStr.Trim();
        timeStr = timeStr.Trim();

        // Try standard formats first
        string[] dateFormats = { dateFormat, "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "d/M/yyyy" };
        string[] timeFormats = { timeFormat, "HH:mm:ss", "H:mm:ss", "HH:mm:ss.fff" };

        foreach (var df in dateFormats)
        {
            foreach (var tf in timeFormats)
            {
                string combined = $"{dateStr} {timeStr}";
                string format = $"{df} {tf}";
                
                if (DateTime.TryParseExact(combined, format, CultureInfo.InvariantCulture, 
                    DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }
        }

        // Fallback to general parsing
        if (DateTime.TryParse($"{dateStr} {timeStr}", out DateTime fallback))
            return fallback;

        _parseWarnings.Add($"Line {lineNumber}: Could not parse date/time '{dateStr}' '{timeStr}'");
        return DateTime.MinValue;
    }

    /// <summary>
    /// Parse combined datetime string
    /// </summary>
    private DateTime ParseCombinedDateTime(string dateTimeStr, ColumnMapping mapping, int lineNumber)
    {
        string format = $"{mapping.DateFormat} {mapping.TimeFormat}";
        
        if (DateTime.TryParseExact(dateTimeStr.Trim(), format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        if (DateTime.TryParse(dateTimeStr.Trim(), out DateTime fallback))
            return fallback;

        _parseWarnings.Add($"Line {lineNumber}: Could not parse datetime '{dateTimeStr}'");
        return DateTime.MinValue;
    }

    /// <summary>
    /// Parse required double value
    /// </summary>
    private double ParseDoubleValue(string[] values, int index, string fieldName, int lineNumber)
    {
        string value = GetValueSafe(values, index);
        
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;

        throw new FormatException($"Invalid {fieldName} value '{value}' at index {index}");
    }

    /// <summary>
    /// Parse optional double value (returns null if empty or invalid)
    /// </summary>
    private double? ParseOptionalDouble(string[] values, int index)
    {
        string value = GetValueSafe(values, index);
        
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;

        return null;
    }

    /// <summary>
    /// Get list of all columns that could be depth sources
    /// </summary>
    public List<string> GetAvailableDepthColumns(string filePath)
    {
        var depthColumns = new List<string>();
        var depthKeywords = new[] { "Depth", "Bathy", "Z", "Elevation", "CTD", "SVX", "ROV" };

        var firstLine = File.ReadLines(filePath).FirstOrDefault();
        if (string.IsNullOrEmpty(firstLine))
            return depthColumns;

        var headers = ParseCsvLine(firstLine);
        
        foreach (var header in headers)
        {
            foreach (var keyword in depthKeywords)
            {
                if (header.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    depthColumns.Add(header.Trim());
                    break;
                }
            }
        }

        return depthColumns;
    }

    /// <summary>
    /// Get all column headers from file
    /// </summary>
    public List<string> GetAllColumns(string filePath)
    {
        var firstLine = File.ReadLines(filePath).FirstOrDefault();
        if (string.IsNullOrEmpty(firstLine))
            return new List<string>();

        return ParseCsvLine(firstLine).Select(h => h.Trim()).ToList();
    }
}

/// <summary>
/// Detected column indices for NPD parsing
/// </summary>
public class DetectedColumnIndices
{
    public int TimeIndex { get; set; } = -1;
    public string TimeColumnName { get; set; } = string.Empty;

    public int EastingIndex { get; set; } = -1;
    public string EastingColumnName { get; set; } = string.Empty;

    public int NorthingIndex { get; set; } = -1;
    public string NorthingColumnName { get; set; } = string.Empty;

    public int DepthIndex { get; set; } = -1;
    public string DepthColumnName { get; set; } = string.Empty;

    public int AltitudeIndex { get; set; } = -1;
    public string AltitudeColumnName { get; set; } = string.Empty;

    public int HeadingIndex { get; set; } = -1;
    public string HeadingColumnName { get; set; } = string.Empty;

    public bool HasDateTimeSplit { get; set; } = true;

    public bool IsValid => TimeIndex >= 0 && EastingIndex >= 0 && NorthingIndex >= 0;

    public override string ToString()
    {
        return $"Time:{TimeIndex} E:{EastingIndex} N:{NorthingIndex} D:{DepthIndex} Alt:{AltitudeIndex} Hdg:{HeadingIndex}";
    }
}

/// <summary>
/// Result of parsing an NPD file
/// </summary>
public class NpdParseResult
{
    public string SourceFile { get; set; } = string.Empty;
    public List<string> HeaderColumns { get; set; } = new();
    public DetectedColumnIndices DetectedMapping { get; set; } = new();
    public List<SurveyPoint> Points { get; set; } = new();

    // Statistics
    public DateTime? StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public double? MinDepth { get; private set; }
    public double? MaxDepth { get; private set; }
    public double? MinEasting { get; private set; }
    public double? MaxEasting { get; private set; }
    public double? MinNorthing { get; private set; }
    public double? MaxNorthing { get; private set; }

    public int TotalRecords => Points.Count;
    public int RecordsWithDepth => Points.Count(p => p.HasValidDepth);

    public void CalculateStatistics()
    {
        if (Points.Count == 0)
            return;

        var validPoints = Points.Where(p => p.DateTime != DateTime.MinValue).ToList();
        if (validPoints.Any())
        {
            StartTime = validPoints.Min(p => p.DateTime);
            EndTime = validPoints.Max(p => p.DateTime);
        }

        var pointsWithDepth = Points.Where(p => p.HasValidDepth).ToList();
        if (pointsWithDepth.Any())
        {
            MinDepth = pointsWithDepth.Min(p => p.Depth!.Value);
            MaxDepth = pointsWithDepth.Max(p => p.Depth!.Value);
        }

        var validCoords = Points.Where(p => p.HasValidCoordinates).ToList();
        if (validCoords.Any())
        {
            MinEasting = validCoords.Min(p => p.Easting);
            MaxEasting = validCoords.Max(p => p.Easting);
            MinNorthing = validCoords.Min(p => p.Northing);
            MaxNorthing = validCoords.Max(p => p.Northing);
        }
    }
}
