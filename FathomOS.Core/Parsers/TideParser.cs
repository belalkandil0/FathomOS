namespace FathomOS.Core.Parsers;

using FathomOS.Core.Models;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Parser for tide data files (7Tide format and generic tab-delimited)
/// 
/// 7Tide format:
/// - 10-line header with metadata (Software, Version, Latitude, Longitude, TimeZone, ListingDate)
/// - Tab-delimited data: DateTime, TideMeters, TideFeet
/// - DateTime format: M/D/YYYY HH:MM
/// </summary>
public class TideParser
{
    private const int HEADER_LINES = 10;

    /// <summary>
    /// Parse a tide file
    /// </summary>
    /// <param name="filePath">Path to the tide file</param>
    /// <returns>Parsed tide data</returns>
    public TideData Parse(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Tide file not found: {filePath}");

        var lines = File.ReadAllLines(filePath);

        var tideData = new TideData
        {
            SourceFile = filePath
        };

        // Try to parse as 7Tide format first
        if (Is7TideFormat(lines))
        {
            Parse7TideFormat(lines, tideData);
        }
        else
        {
            // Generic tab-delimited format
            ParseGenericFormat(lines, tideData);
        }

        return tideData;
    }

    /// <summary>
    /// Check if file is in 7Tide format
    /// </summary>
    private bool Is7TideFormat(string[] lines)
    {
        if (lines.Length < HEADER_LINES)
            return false;

        // Check for 7Tide signature
        return lines[0].Contains("7Tide") || 
               lines.Any(l => l.Contains("7Tide"));
    }

    /// <summary>
    /// Parse 7Tide format file
    /// </summary>
    private void Parse7TideFormat(string[] lines, TideData tideData)
    {
        // Parse header lines
        foreach (var line in lines.Take(HEADER_LINES))
        {
            ParseHeaderLine(line, tideData);
        }

        // Parse data lines
        for (int i = HEADER_LINES; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var record = ParseDataLine(line);
            if (record != null)
            {
                tideData.Records.Add(record);
            }
        }
    }

    /// <summary>
    /// Parse generic tab-delimited tide file
    /// </summary>
    private void ParseGenericFormat(string[] lines, TideData tideData)
    {
        int startLine = 0;

        // Skip header lines that don't look like data
        while (startLine < lines.Length && !IsDataLine(lines[startLine]))
        {
            ParseHeaderLine(lines[startLine], tideData);
            startLine++;
        }

        // Parse data lines
        for (int i = startLine; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var record = ParseDataLine(line);
            if (record != null)
            {
                tideData.Records.Add(record);
            }
        }
    }

    /// <summary>
    /// Parse a header line for metadata
    /// </summary>
    private void ParseHeaderLine(string line, TideData tideData)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        // Software and version
        var softwareMatch = Regex.Match(line, @"(\w+)\s+Version\s+([\d.]+)", RegexOptions.IgnoreCase);
        if (softwareMatch.Success)
        {
            tideData.Software = softwareMatch.Groups[1].Value;
            tideData.Version = softwareMatch.Groups[2].Value;
            return;
        }

        // Latitude
        var latMatch = Regex.Match(line, @"Latitude[:\s]+([-\d.]+)", RegexOptions.IgnoreCase);
        if (latMatch.Success && double.TryParse(latMatch.Groups[1].Value, NumberStyles.Float, 
            CultureInfo.InvariantCulture, out double lat))
        {
            tideData.Latitude = lat;
            return;
        }

        // Longitude
        var lonMatch = Regex.Match(line, @"Longitude[:\s]+([-\d.]+)", RegexOptions.IgnoreCase);
        if (lonMatch.Success && double.TryParse(lonMatch.Groups[1].Value, NumberStyles.Float, 
            CultureInfo.InvariantCulture, out double lon))
        {
            tideData.Longitude = lon;
            return;
        }

        // Time zone
        var tzMatch = Regex.Match(line, @"GMT\s*([-+]?\d+\.?\d*)", RegexOptions.IgnoreCase);
        if (tzMatch.Success && double.TryParse(tzMatch.Groups[1].Value, NumberStyles.Float, 
            CultureInfo.InvariantCulture, out double tz))
        {
            tideData.TimeZoneOffset = tz;
            return;
        }

        // Listing date
        var dateMatch = Regex.Match(line, @"Listing\s+date[:\s]+(.+)", RegexOptions.IgnoreCase);
        if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value.Trim(), out DateTime listingDate))
        {
            tideData.ListingDate = listingDate;
            return;
        }
    }

    /// <summary>
    /// Parse a tide data line
    /// Format: M/D/YYYY HH:MM[TAB]TideMeters[TAB]TideFeet
    /// </summary>
    private TideRecord? ParseDataLine(string line)
    {
        // Split by tab
        var parts = line.Split('\t');
        if (parts.Length < 2)
            return null;

        var record = new TideRecord();

        // Parse datetime (column 1)
        if (!TryParseDateTime(parts[0].Trim(), out DateTime dateTime))
            return null;
        record.DateTime = dateTime;

        // Parse tide in meters (column 2)
        if (!double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double tideMeters))
            return null;
        record.TideMeters = tideMeters;

        // Parse tide in feet (column 3) if available, otherwise calculate
        if (parts.Length >= 3 && double.TryParse(parts[2].Trim(), NumberStyles.Float, 
            CultureInfo.InvariantCulture, out double tideFeet))
        {
            record.TideFeet = tideFeet;
        }
        else
        {
            // Calculate feet from meters
            record.TideFeet = tideMeters * 3.28084;
        }

        return record;
    }

    /// <summary>
    /// Try to parse datetime from various formats
    /// </summary>
    private bool TryParseDateTime(string value, out DateTime result)
    {
        // Common formats for tide files
        string[] formats = {
            "M/d/yyyy H:mm",
            "M/d/yyyy HH:mm",
            "MM/dd/yyyy H:mm",
            "MM/dd/yyyy HH:mm",
            "d/M/yyyy H:mm",
            "d/M/yyyy HH:mm",
            "dd/MM/yyyy H:mm",
            "dd/MM/yyyy HH:mm",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd H:mm",
            "M/d/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        // Fallback to general parsing
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Check if a line looks like data (starts with a date)
    /// </summary>
    private bool IsDataLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var parts = line.Split('\t');
        if (parts.Length < 2)
            return false;

        // Check if first part looks like a date
        return TryParseDateTime(parts[0].Trim(), out _);
    }

    /// <summary>
    /// Get a quick summary of the tide file without full parsing
    /// </summary>
    public TideFileSummary? GetFileSummary(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath);
            
            // Find first and last data lines
            int firstDataLine = -1;
            int lastDataLine = -1;
            int dataCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (IsDataLine(lines[i]))
                {
                    if (firstDataLine < 0) firstDataLine = i;
                    lastDataLine = i;
                    dataCount++;
                }
            }

            if (firstDataLine < 0 || lastDataLine < 0)
                return null;

            // Parse first and last records
            var firstRecord = ParseDataLine(lines[firstDataLine]);
            var lastRecord = ParseDataLine(lines[lastDataLine]);

            if (firstRecord == null || lastRecord == null)
                return null;

            return new TideFileSummary
            {
                FilePath = filePath,
                RecordCount = dataCount,
                StartTime = firstRecord.DateTime,
                EndTime = lastRecord.DateTime
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Quick summary of a tide file
/// </summary>
public class TideFileSummary
{
    public string FilePath { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public override string ToString()
    {
        return $"{RecordCount} records from {StartTime:yyyy-MM-dd} to {EndTime:yyyy-MM-dd}";
    }
}
