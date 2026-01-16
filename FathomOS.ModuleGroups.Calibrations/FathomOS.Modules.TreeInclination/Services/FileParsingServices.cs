namespace FathomOS.Modules.TreeInclination.Services;

using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using FathomOS.Modules.TreeInclination.Models;

/// <summary>
/// NPD file parser with auto-detection and user override capability.
/// Follows NPD Parsing Guide patterns for consistency with other modules.
/// </summary>
public class NpdDepthParser
{
    // Depth column patterns for auto-detection
    private static readonly string[] DepthPatterns =
    {
        @"^z$", @"^depth$", @"^dq.*depth", @"^digiquartz", @"^bathy",
        @"^altitude$", @"^alt$", @"^pressure", @"^psa$", @"^mbar$",
        @"depth", @"z_", @"_z$", @"alt", @"sensor.*depth"
    };
    
    // Easting patterns
    private static readonly string[] EastingPatterns =
    {
        @"^easting$", @"^east$", @"^e$", @"^x$", @"easting", @"east", @"_e$", @"_x$"
    };
    
    // Northing patterns
    private static readonly string[] NorthingPatterns =
    {
        @"^northing$", @"^north$", @"^n$", @"^y$", @"northing", @"north", @"_n$", @"_y$"
    };
    
    // Height patterns
    private static readonly string[] HeightPatterns =
    {
        @"^height$", @"^h$", @"^altitude$", @"^alt$", @"^z$", @"height", @"_h$", @"_alt$"
    };
    
    // Time patterns
    private static readonly string[] TimePatterns =
    {
        @"^time$", @"^datetime$", @"^timestamp$", @"^utc", @"^date.*time", @"time.*utc", @"^t$"
    };
    
    // Heading/Gyro patterns
    private static readonly string[] HeadingPatterns =
    {
        @"^heading$", @"^hdg$", @"^gyro$", @"^bearing$", @"^azimuth$", @"^course$",
        @"heading", @"gyro", @"hdg", @"_hdg$", @"_heading$", @"compass"
    };

    /// <summary>Get all column headers from file for dropdown population</summary>
    public List<string> GetAllColumns(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        if (lines.Length > 0)
        {
            return ParseDelimitedLine(lines[0]).ToList();
        }
        return new List<string>();
    }

    /// <summary>Get columns that might contain depth values</summary>
    public List<string> GetAvailableDepthColumns(string filePath)
    {
        var allColumns = GetAllColumns(filePath);
        return allColumns.Where(c => MatchesAnyPattern(c, DepthPatterns)).ToList();
    }

    /// <summary>Auto-detect column mapping for file</summary>
    public ColumnDetectionResult AutoDetectColumns(string filePath)
    {
        var result = new ColumnDetectionResult();
        
        try
        {
            var columns = GetAllColumns(filePath);
            result.AllColumns = columns;
            
            // Detect each column type
            result.DetectedDepthColumn = DetectColumn(columns, DepthPatterns);
            result.DetectedTimeColumn = DetectColumn(columns, TimePatterns);
            result.DetectedEastingColumn = DetectColumn(columns, EastingPatterns);
            result.DetectedNorthingColumn = DetectColumn(columns, NorthingPatterns);
            result.DetectedHeightColumn = DetectColumn(columns, HeightPatterns);
            result.DetectedHeadingColumn = DetectColumn(columns, HeadingPatterns);
            
            // Detect if NaviPac date/time split format
            result.HasDateTimeSplit = DetectNaviPacFormat(filePath);
            
            result.IsValid = !string.IsNullOrEmpty(result.DetectedDepthColumn);
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
        
        return result;
    }

    /// <summary>Parse file with specified column selections</summary>
    public NpdFileInfo Parse(
        string filePath,
        ColumnSelection selection,
        DepthInputUnit depthUnit,
        double waterDensity = 1025.0,
        double atmPressure = 1013.25)
    {
        var info = new NpdFileInfo { FilePath = filePath, IsValid = false };
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                info.ValidationMessage = "File has insufficient data";
                return info;
            }
            
            string[] headers = ParseDelimitedLine(lines[0]);
            info.AvailableColumns = headers.ToList();
            info.SelectedDepthColumn = selection.DepthColumn ?? "";
            info.DetectedDepthColumn = selection.DepthColumn ?? "";
            
            // Get column indices
            int depthIndex = GetColumnIndex(headers, selection.DepthColumn);
            int timeIndex = GetColumnIndex(headers, selection.TimeColumn);
            int eastingIndex = GetColumnIndex(headers, selection.EastingColumn);
            int northingIndex = GetColumnIndex(headers, selection.NorthingColumn);
            int heightIndex = GetColumnIndex(headers, selection.HeightColumn);
            int headingIndex = GetColumnIndex(headers, selection.HeadingColumn);
            
            if (depthIndex < 0)
            {
                info.ValidationMessage = "Depth column not found. Please select a valid column.";
                return info;
            }
            
            // Calculate offset for NaviPac date/time split
            int offset = selection.HasDateTimeSplit ? 1 : 0;
            
            var readings = new List<DepthReading>();
            var positions = new List<(double E, double N, double? H)>();
            DateTime? firstTime = null, lastTime = null;
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = ParseDelimitedLine(lines[i]);
                
                // Apply offset to non-time columns (columns after time index)
                int effectiveDepthIndex = depthIndex + (depthIndex > timeIndex && timeIndex >= 0 ? offset : 0);
                int effectiveEastingIndex = eastingIndex >= 0 ? eastingIndex + (eastingIndex > timeIndex && timeIndex >= 0 ? offset : 0) : -1;
                int effectiveNorthingIndex = northingIndex >= 0 ? northingIndex + (northingIndex > timeIndex && timeIndex >= 0 ? offset : 0) : -1;
                int effectiveHeightIndex = heightIndex >= 0 ? heightIndex + (heightIndex > timeIndex && timeIndex >= 0 ? offset : 0) : -1;
                int effectiveHeadingIndex = headingIndex >= 0 ? headingIndex + (headingIndex > timeIndex && timeIndex >= 0 ? offset : 0) : -1;
                
                if (effectiveDepthIndex >= values.Length) continue;
                
                if (!double.TryParse(values[effectiveDepthIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out double rawDepth))
                    continue;
                
                var reading = new DepthReading
                {
                    RawValue = rawDepth,
                    ConvertedDepth = UnitConversions.ToMeters(rawDepth, depthUnit, waterDensity, atmPressure)
                };
                
                // Parse time
                if (timeIndex >= 0)
                {
                    if (selection.HasDateTimeSplit && values.Length > 1)
                    {
                        // NaviPac format: values[0] = date, values[1] = time
                        if (TryParseNaviPacDateTime(values[0], values[1], out DateTime dt))
                        {
                            reading.Timestamp = dt;
                            firstTime ??= dt;
                            lastTime = dt;
                        }
                    }
                    else if (timeIndex < values.Length)
                    {
                        if (TryParseDateTime(values[timeIndex], out DateTime dt))
                        {
                            reading.Timestamp = dt;
                            firstTime ??= dt;
                            lastTime = dt;
                        }
                    }
                }
                
                // Parse position if columns selected
                double easting = 0, northing = 0;
                double? height = null;
                
                if (effectiveEastingIndex >= 0 && effectiveEastingIndex < values.Length &&
                    double.TryParse(values[effectiveEastingIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out easting))
                {
                    reading.Easting = easting;
                }
                
                if (effectiveNorthingIndex >= 0 && effectiveNorthingIndex < values.Length &&
                    double.TryParse(values[effectiveNorthingIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out northing))
                {
                    reading.Northing = northing;
                }
                
                if (effectiveHeightIndex >= 0 && effectiveHeightIndex < values.Length &&
                    double.TryParse(values[effectiveHeightIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out double h))
                {
                    height = h;
                    reading.Height = height;
                }
                
                // Parse heading if column selected
                if (effectiveHeadingIndex >= 0 && effectiveHeadingIndex < values.Length &&
                    double.TryParse(values[effectiveHeadingIndex], NumberStyles.Any, CultureInfo.InvariantCulture, out double hdg))
                {
                    // Normalize heading to 0-360
                    while (hdg < 0) hdg += 360;
                    while (hdg >= 360) hdg -= 360;
                    reading.Heading = hdg;
                }
                
                if (reading.Easting.HasValue && reading.Northing.HasValue)
                {
                    positions.Add((reading.Easting.Value, reading.Northing.Value, height));
                }
                
                readings.Add(reading);
            }
            
            if (readings.Count == 0)
            {
                info.ValidationMessage = "No valid depth readings found";
                return info;
            }
            
            // Calculate depth statistics
            var validDepths = readings.Select(r => r.ConvertedDepth).ToList();
            info.RawDepthAverage = validDepths.Average();
            info.RawDepthMin = validDepths.Min();
            info.RawDepthMax = validDepths.Max();
            
            double variance = validDepths.Sum(d => Math.Pow(d - info.RawDepthAverage, 2)) / validDepths.Count;
            info.RawDepthStdDev = Math.Sqrt(variance);
            
            // Mark outliers (3-sigma)
            double threshold = info.RawDepthStdDev * 3;
            foreach (var reading in readings)
            {
                reading.IsOutlier = Math.Abs(reading.ConvertedDepth - info.RawDepthAverage) > threshold;
            }
            info.OutlierCount = readings.Count(r => r.IsOutlier);
            
            // Calculate position statistics if available
            if (positions.Count > 0)
            {
                info.AverageEasting = positions.Average(p => p.E);
                info.AverageNorthing = positions.Average(p => p.N);
                info.HasPositionData = true;
                
                var heights = positions.Where(p => p.H.HasValue).Select(p => p.H!.Value).ToList();
                if (heights.Count > 0)
                {
                    info.AverageHeight = heights.Average();
                }
                
                // Position standard deviations
                info.EastingStdDev = Math.Sqrt(positions.Sum(p => Math.Pow(p.E - info.AverageEasting!.Value, 2)) / positions.Count);
                info.NorthingStdDev = Math.Sqrt(positions.Sum(p => Math.Pow(p.N - info.AverageNorthing!.Value, 2)) / positions.Count);
            }
            
            // Calculate heading statistics if available
            var headings = readings.Where(r => r.Heading.HasValue).Select(r => r.Heading!.Value).ToList();
            if (headings.Count > 0)
            {
                info.HasHeadingData = true;
                info.HeadingMin = headings.Min();
                info.HeadingMax = headings.Max();
                
                // Calculate circular mean for headings (handles wrap-around at 0/360)
                double sumSin = headings.Sum(h => Math.Sin(h * Math.PI / 180));
                double sumCos = headings.Sum(h => Math.Cos(h * Math.PI / 180));
                double meanHeading = Math.Atan2(sumSin, sumCos) * 180 / Math.PI;
                if (meanHeading < 0) meanHeading += 360;
                info.AverageHeading = meanHeading;
                
                // Circular standard deviation
                double R = Math.Sqrt(sumSin * sumSin + sumCos * sumCos) / headings.Count;
                info.HeadingStdDev = Math.Sqrt(-2 * Math.Log(R)) * 180 / Math.PI;
                if (double.IsNaN(info.HeadingStdDev.Value) || double.IsInfinity(info.HeadingStdDev.Value))
                    info.HeadingStdDev = 0;
            }
            
            info.Readings = readings;
            info.RecordCount = readings.Count;
            info.LogStartTime = firstTime;
            info.LogEndTime = lastTime;
            info.IsValid = true;
            
            return info;
        }
        catch (Exception ex)
        {
            info.ValidationMessage = $"Parse error: {ex.Message}";
            return info;
        }
    }

    /// <summary>Get preview data for display</summary>
    public DataPreviewResult GetPreview(string filePath, int maxRows = 20)
    {
        var result = new DataPreviewResult();
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 1)
            {
                result.Error = "Empty file";
                return result;
            }
            
            result.Headers = ParseDelimitedLine(lines[0]).ToList();
            result.Rows = new List<List<string>>();
            
            int rowsToRead = Math.Min(maxRows, lines.Length - 1);
            for (int i = 1; i <= rowsToRead; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    result.Rows.Add(ParseDelimitedLine(lines[i]).ToList());
                }
            }
            
            result.TotalRows = lines.Length - 1;
            result.IsValid = true;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
        
        return result;
    }

    #region Private Helpers

    private string[] ParseDelimitedLine(string line)
    {
        // Handle quoted CSV fields
        if (line.Contains('"'))
        {
            return ParseQuotedCsv(line);
        }
        
        if (line.Contains('\t'))
            return line.Split('\t').Select(s => s.Trim()).ToArray();
        if (line.Contains(','))
            return line.Split(',').Select(s => s.Trim()).ToArray();
        return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private string[] ParseQuotedCsv(string line)
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
            else if (c == ',' && !inQuotes)
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

    private string? DetectColumn(List<string> columns, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = columns.FirstOrDefault(c => regex.IsMatch(c.Trim()));
            if (match != null) return match;
        }
        return null;
    }

    private bool MatchesAnyPattern(string column, string[] patterns)
    {
        return patterns.Any(p => new Regex(p, RegexOptions.IgnoreCase).IsMatch(column));
    }

    private int GetColumnIndex(string[] headers, string? columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return -1;
        return Array.FindIndex(headers, h => h.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    private bool DetectNaviPacFormat(string filePath)
    {
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2) return false;
            
            var headers = ParseDelimitedLine(lines[0]);
            var firstDataRow = ParseDelimitedLine(lines[1]);
            
            // NaviPac: header has fewer columns than data (Time column splits into Date,Time)
            if (firstDataRow.Length > headers.Length)
            {
                // Check if first column looks like a date and second like a time
                if (DateTime.TryParseExact(firstDataRow[0], new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    return true;
                }
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool TryParseNaviPacDateTime(string dateStr, string timeStr, out DateTime result)
    {
        result = DateTime.MinValue;
        string combined = $"{dateStr} {timeStr}";
        
        string[] formats = {
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss.fff",
            "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff"
        };
        
        return DateTime.TryParseExact(combined, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result) ||
               DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private bool TryParseDateTime(string value, out DateTime result)
    {
        result = DateTime.MinValue;
        
        string[] formats = {
            "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss.fff",
            "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss",
            "HH:mm:ss", "HH:mm:ss.fff"
        };
        
        return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result) ||
               DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    #endregion
}

#region Support Classes

public class ColumnDetectionResult
{
    public List<string> AllColumns { get; set; } = new();
    public string? DetectedDepthColumn { get; set; }
    public string? DetectedTimeColumn { get; set; }
    public string? DetectedEastingColumn { get; set; }
    public string? DetectedNorthingColumn { get; set; }
    public string? DetectedHeightColumn { get; set; }
    public string? DetectedHeadingColumn { get; set; }
    public bool HasDateTimeSplit { get; set; }
    public bool IsValid { get; set; }
    public string? Error { get; set; }
}

public class ColumnSelection
{
    public string? DepthColumn { get; set; }
    public string? TimeColumn { get; set; }
    public string? EastingColumn { get; set; }
    public string? NorthingColumn { get; set; }
    public string? HeightColumn { get; set; }
    public string? HeadingColumn { get; set; }
    public bool HasDateTimeSplit { get; set; }
}

public class DataPreviewResult
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public bool IsValid { get; set; }
    public string? Error { get; set; }
}

#endregion

#region Tide Parser

/// <summary>Tide file parser with auto-detection</summary>
public class TideFileParser
{
    public (List<TideRecord> Records, string? Error) Parse(string filePath, TideFileFormat format = TideFileFormat.Auto)
    {
        var records = new List<TideRecord>();
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
                return (records, "File has insufficient data");
            
            if (format == TideFileFormat.Auto)
                format = DetectFormat(lines);
            
            return format switch
            {
                TideFileFormat.SevenTide => Parse7TideFormat(lines),
                TideFileFormat.GenericCSV => ParseGenericFormat(lines, ','),
                TideFileFormat.TabDelimited => ParseGenericFormat(lines, '\t'),
                TideFileFormat.SpaceDelimited => ParseSpaceDelimited(lines),
                _ => ParseGenericFormat(lines, ',')
            };
        }
        catch (Exception ex)
        {
            return (records, $"Parse error: {ex.Message}");
        }
    }

    private TideFileFormat DetectFormat(string[] lines)
    {
        if (lines.Any(l => l.Contains("7Tide") || l.Contains("Listing")))
            return TideFileFormat.SevenTide;
        if (lines[0].Contains('\t'))
            return TideFileFormat.TabDelimited;
        if (lines[0].Contains(','))
            return TideFileFormat.GenericCSV;
        return TideFileFormat.SpaceDelimited;
    }

    private (List<TideRecord>, string?) Parse7TideFormat(string[] lines)
    {
        var records = new List<TideRecord>();
        bool dataSection = false;
        
        foreach (var line in lines)
        {
            if (line.StartsWith("---") || (line.Contains("Date") && line.Contains("Time")))
            {
                dataSection = true;
                continue;
            }
            
            if (!dataSection || string.IsNullOrWhiteSpace(line)) continue;
            
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;
            
            if (DateTime.TryParse($"{parts[0]} {parts[1]}", out DateTime dt) &&
                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double height))
            {
                records.Add(new TideRecord { DateTime = dt, TideHeight = height });
            }
        }
        
        return (records, records.Count > 0 ? null : "No tide records found");
    }

    private (List<TideRecord>, string?) ParseGenericFormat(string[] lines, char delimiter)
    {
        var records = new List<TideRecord>();
        string[] headers = lines[0].Split(delimiter);
        
        int dateIdx = Array.FindIndex(headers, h => h.ToLower().Contains("date") || h.ToLower().Contains("time"));
        int valueIdx = Array.FindIndex(headers, h => h.ToLower().Contains("tide") || h.ToLower().Contains("height") || h.ToLower().Contains("value"));
        
        if (dateIdx < 0) dateIdx = 0;
        if (valueIdx < 0) valueIdx = headers.Length > 1 ? 1 : 0;
        
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(delimiter);
            if (parts.Length <= Math.Max(dateIdx, valueIdx)) continue;
            
            if (DateTime.TryParse(parts[dateIdx], out DateTime dt) &&
                double.TryParse(parts[valueIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out double height))
            {
                records.Add(new TideRecord { DateTime = dt, TideHeight = height });
            }
        }
        
        return (records, records.Count > 0 ? null : "No tide records found");
    }

    private (List<TideRecord>, string?) ParseSpaceDelimited(string[] lines)
    {
        var records = new List<TideRecord>();
        
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            
            if (parts.Length >= 3 && DateTime.TryParse($"{parts[0]} {parts[1]}", out DateTime dt) &&
                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double h))
            {
                records.Add(new TideRecord { DateTime = dt, TideHeight = h });
            }
            else if (DateTime.TryParse(parts[0], out dt) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out h))
            {
                records.Add(new TideRecord { DateTime = dt, TideHeight = h });
            }
        }
        
        return (records, records.Count > 0 ? null : "No tide records found");
    }

    public double? InterpolateTide(List<TideRecord> records, DateTime targetTime)
    {
        if (records.Count == 0) return null;
        
        var sorted = records.OrderBy(r => r.DateTime).ToList();
        
        if (targetTime <= sorted[0].DateTime) return sorted[0].TideHeight;
        if (targetTime >= sorted[^1].DateTime) return sorted[^1].TideHeight;
        
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (targetTime >= sorted[i].DateTime && targetTime <= sorted[i + 1].DateTime)
            {
                double t = (targetTime - sorted[i].DateTime).TotalSeconds /
                          (sorted[i + 1].DateTime - sorted[i].DateTime).TotalSeconds;
                return sorted[i].TideHeight + t * (sorted[i + 1].TideHeight - sorted[i].TideHeight);
            }
        }
        
        return null;
    }
}

#endregion

#region Position Fix Parser

/// <summary>Position fix file parser</summary>
public class PositionFixParser
{
    public (List<PositionFix> Fixes, string? Error) Parse(string filePath)
    {
        var fixes = new List<PositionFix>();
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2) return (fixes, "File has insufficient data");
            
            string[] headers = ParseLine(lines[0]);
            
            int eastingIdx = FindColumn(headers, "easting", "east", "x", "e");
            int northingIdx = FindColumn(headers, "northing", "north", "y", "n");
            int timeIdx = FindColumn(headers, "time", "datetime", "timestamp");
            int depthIdx = FindColumn(headers, "depth", "z", "altitude");
            int headingIdx = FindColumn(headers, "heading", "hdg", "azimuth");
            int nameIdx = FindColumn(headers, "name", "point", "id", "label");
            
            if (eastingIdx < 0 || northingIdx < 0)
                return (fixes, "Could not identify coordinate columns");
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var values = ParseLine(lines[i]);
                if (values.Length <= Math.Max(eastingIdx, northingIdx)) continue;
                
                if (!double.TryParse(values[eastingIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out double easting) ||
                    !double.TryParse(values[northingIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out double northing))
                    continue;
                
                var fix = new PositionFix { Easting = easting, Northing = northing };
                
                if (timeIdx >= 0 && timeIdx < values.Length && DateTime.TryParse(values[timeIdx], out DateTime dt))
                    fix.Timestamp = dt;
                
                if (depthIdx >= 0 && depthIdx < values.Length &&
                    double.TryParse(values[depthIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out double depth))
                    fix.Depth = depth;
                
                if (headingIdx >= 0 && headingIdx < values.Length &&
                    double.TryParse(values[headingIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out double heading))
                    fix.Heading = heading;
                
                if (nameIdx >= 0 && nameIdx < values.Length)
                    fix.PointName = values[nameIdx];
                
                fixes.Add(fix);
            }
            
            return (fixes, fixes.Count > 0 ? null : "No position fixes found");
        }
        catch (Exception ex)
        {
            return (fixes, $"Parse error: {ex.Message}");
        }
    }

    private string[] ParseLine(string line)
    {
        if (line.Contains('\t')) return line.Split('\t').Select(s => s.Trim()).ToArray();
        if (line.Contains(',')) return line.Split(',').Select(s => s.Trim()).ToArray();
        return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private int FindColumn(string[] headers, params string[] patterns)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            string h = headers[i].ToLower().Trim();
            if (patterns.Any(p => h.Contains(p) || h.Equals(p)))
                return i;
        }
        return -1;
    }
}

#endregion
