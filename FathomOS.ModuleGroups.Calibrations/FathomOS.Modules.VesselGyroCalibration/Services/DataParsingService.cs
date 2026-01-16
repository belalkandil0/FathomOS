using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FathomOS.Modules.VesselGyroCalibration.Models;

namespace FathomOS.Modules.VesselGyroCalibration.Services;

/// <summary>
/// Service for parsing NPD/CSV data files for gyro calibration.
/// Handles NaviPac format with date/time column split.
/// </summary>
public class DataParsingService
{
    #region Raw Data Loading (Step 2)

    /// <summary>
    /// Load file and return raw data without interpretation.
    /// Used in Step 2 to show file contents and detect columns.
    /// </summary>
    public RawFileData LoadRawFile(string filePath)
    {
        var result = new RawFileData
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Data file not found", filePath);

        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0) return result;

        // Parse header row
        result.Headers = ParseCsvLine(lines[0]).ToList();
        result.TotalRows = lines.Length - 1;

        // Parse all data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length > 0)
            {
                result.DataRows.Add(values);
            }
        }

        // Auto-detect column candidates for heading data
        result.DetectedColumns = DetectHeadingColumns(result.Headers);

        return result;
    }

    /// <summary>
    /// Detect which columns likely contain heading data
    /// </summary>
    private List<ColumnCandidate> DetectHeadingColumns(List<string> headers)
    {
        var candidates = new List<ColumnCandidate>();
        var headingPatterns = new[] { "heading", "hdg", "gyro", "azimuth", "bearing", "course" };
        var timePatterns = new[] { "time", "date", "utc", "timestamp" };

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i].ToLowerInvariant();
            var candidate = new ColumnCandidate
            {
                Index = i,
                Name = headers[i],
                IsTimeColumn = timePatterns.Any(p => header.Contains(p)),
                IsHeadingColumn = headingPatterns.Any(p => header.Contains(p)),
                IsReferenceCandidate = header.Contains("ref") || header.Contains("reference") || header.Contains("vessel"),
                IsCalibratedCandidate = header.Contains("cal") || header.Contains("system") || header.Contains("rov")
            };
            candidates.Add(candidate);
        }

        return candidates;
    }

    #endregion

    #region Parsed Data (Step 3-4)

    /// <summary>
    /// Parse file using explicit column mapping from user selection.
    /// Returns data points with C-O calculated.
    /// </summary>
    public List<GyroDataPoint> ParseWithMapping(RawFileData rawData, GyroColumnMapping mapping)
    {
        var points = new List<GyroDataPoint>();
        
        // Determine offset for NaviPac date/time split
        // NaviPac: Header "Time" = 1 column, Data "Date,Time" = 2 values
        int offset = mapping.HasDateTimeSplit ? 1 : 0;

        for (int i = 0; i < rawData.DataRows.Count; i++)
        {
            try
            {
                var values = rawData.DataRows[i];
                if (values.Length < 3) continue;

                var point = new GyroDataPoint { Index = i + 1 };

                // Parse DateTime
                point.Timestamp = ParseDateTime(values, mapping);

                // Parse Reference Heading (apply offset after time column)
                int refIdx = mapping.ReferenceHeadingColumnIndex;
                if (refIdx >= 0)
                {
                    int actualIdx = refIdx + offset;
                    if (actualIdx < values.Length)
                    {
                        if (double.TryParse(values[actualIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var rh))
                            point.ReferenceHeading = NormalizeHeading(rh);
                    }
                }

                // Parse Calibrated Heading
                int calIdx = mapping.CalibratedHeadingColumnIndex;
                if (calIdx >= 0)
                {
                    int actualIdx = calIdx + offset;
                    if (actualIdx < values.Length)
                    {
                        if (double.TryParse(values[actualIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var ch))
                            point.CalibratedHeading = NormalizeHeading(ch);
                    }
                }

                // Calculate C-O with 360° wrap-around (matching Excel formula)
                // =IF(B-C<-180, B-C+360, IF(B-C>180, B-C-360, B-C))
                point.CO = CalculateCO(point.ReferenceHeading, point.CalibratedHeading);

                points.Add(point);
            }
            catch
            {
                // Skip malformed lines
            }
        }

        return points;
    }

    /// <summary>
    /// Parse DateTime handling NaviPac split format
    /// </summary>
    private DateTime ParseDateTime(string[] values, GyroColumnMapping mapping)
    {
        if (mapping.HasDateTimeSplit && values.Length >= 2)
        {
            // NaviPac: First two values are Date and Time
            var dateStr = values[0];
            var timeStr = values[1];

            // Try combined parsing
            if (DateTime.TryParse($"{dateStr} {timeStr}", out var dt))
                return dt;

            // Try with explicit formats
            var formats = new[]
            {
                $"{mapping.DateFormat} {mapping.TimeFormat}",
                "dd/MM/yyyy HH:mm:ss",
                "MM/dd/yyyy HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss"
            };

            foreach (var fmt in formats)
            {
                if (DateTime.TryParseExact($"{dateStr} {timeStr}", fmt,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt2))
                    return dt2;
            }
        }
        else if (mapping.TimeColumnIndex >= 0 && mapping.TimeColumnIndex < values.Length)
        {
            if (DateTime.TryParse(values[mapping.TimeColumnIndex], out var dt))
                return dt;
        }

        return DateTime.MinValue;
    }

    /// <summary>
    /// Normalize heading to 0-360 range
    /// </summary>
    private static double NormalizeHeading(double heading)
    {
        while (heading < 0) heading += 360;
        while (heading >= 360) heading -= 360;
        return heading;
    }

    /// <summary>
    /// Calculate C-O with 360° wrap-around (matching Excel GLSIFR0035 formula)
    /// =IF(B-C<-180, B-C+360, IF(B-C>180, B-C-360, B-C))
    /// </summary>
    public static double CalculateCO(double referenceHeading, double calibratedHeading)
    {
        double diff = referenceHeading - calibratedHeading;
        if (diff < -180) return diff + 360;
        if (diff > 180) return diff - 360;
        return diff;
    }

    #endregion

    #region Legacy Methods (for compatibility)

    /// <summary>
    /// Auto-parse NPD file with auto-detection (legacy support)
    /// </summary>
    public List<GyroDataPoint> ParseNpdFile(string filePath)
    {
        var rawData = LoadRawFile(filePath);
        var mapping = AutoDetectMapping(rawData);
        return ParseWithMapping(rawData, mapping);
    }

    /// <summary>
    /// Get file preview info
    /// </summary>
    public FilePreviewInfo GetFilePreview(string filePath, int maxRows = 10)
    {
        var info = new FilePreviewInfo
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath)
        };

        if (!File.Exists(filePath)) return info;

        var lines = File.ReadAllLines(filePath);
        info.TotalRows = lines.Length - 1;

        if (lines.Length > 0)
        {
            info.Columns = ParseCsvLine(lines[0]).ToList();
            for (int i = 1; i < Math.Min(lines.Length, maxRows + 1); i++)
            {
                info.PreviewRows.Add(ParseCsvLine(lines[i]));
            }
        }

        return info;
    }

    /// <summary>
    /// Parse file with column mapping (legacy)
    /// </summary>
    public List<GyroDataPoint> ParseFile(string filePath, GyroColumnMapping mapping)
    {
        var rawData = LoadRawFile(filePath);
        return ParseWithMapping(rawData, mapping);
    }

    /// <summary>
    /// Auto-detect column mapping from headers
    /// </summary>
    public GyroColumnMapping AutoDetectMapping(RawFileData rawData)
    {
        var mapping = new GyroColumnMapping { HasDateTimeSplit = true };
        var headers = rawData.Headers.Select(h => h.ToLowerInvariant()).ToArray();

        // Find time column
        mapping.TimeColumnIndex = Array.FindIndex(headers, h => 
            h.Contains("time") || h.Contains("utc"));

        // Find reference heading (vessel gyro, reference system)
        mapping.ReferenceHeadingColumnIndex = Array.FindIndex(headers, h =>
            (h.Contains("reference") && h.Contains("heading")) ||
            (h.Contains("vessel") && h.Contains("heading")) ||
            (h.Contains("ref") && (h.Contains("hdg") || h.Contains("gyro"))));

        // Fallback: first heading column
        if (mapping.ReferenceHeadingColumnIndex < 0)
        {
            mapping.ReferenceHeadingColumnIndex = Array.FindIndex(headers, h =>
                h.Contains("heading") || h.Contains("hdg") || h.Contains("gyro"));
        }

        // Find calibrated heading (system being checked)
        mapping.CalibratedHeadingColumnIndex = Array.FindIndex(headers, h =>
            (h.Contains("calibrated") || h.Contains("system") || h.Contains("cal")) &&
            (h.Contains("heading") || h.Contains("hdg")));

        // Fallback: second heading column
        if (mapping.CalibratedHeadingColumnIndex < 0)
        {
            int firstHeading = mapping.ReferenceHeadingColumnIndex;
            for (int i = 0; i < headers.Length; i++)
            {
                if (i != firstHeading && (headers[i].Contains("heading") || headers[i].Contains("hdg") || headers[i].Contains("gyro")))
                {
                    mapping.CalibratedHeadingColumnIndex = i;
                    break;
                }
            }
        }

        // Set column names for display
        if (mapping.TimeColumnIndex >= 0 && mapping.TimeColumnIndex < rawData.Headers.Count)
            mapping.TimeColumn = rawData.Headers[mapping.TimeColumnIndex];
        if (mapping.ReferenceHeadingColumnIndex >= 0 && mapping.ReferenceHeadingColumnIndex < rawData.Headers.Count)
            mapping.ReferenceHeadingColumn = rawData.Headers[mapping.ReferenceHeadingColumnIndex];
        if (mapping.CalibratedHeadingColumnIndex >= 0 && mapping.CalibratedHeadingColumnIndex < rawData.Headers.Count)
            mapping.CalibratedHeadingColumn = rawData.Headers[mapping.CalibratedHeadingColumnIndex];

        return mapping;
    }

    /// <summary>
    /// Auto-detect columns from column list (legacy compatibility)
    /// </summary>
    public GyroColumnMapping AutoDetectColumns(List<string> columns)
    {
        var rawData = new RawFileData { Headers = columns };
        return AutoDetectMapping(rawData);
    }

    #endregion

    #region CSV Parsing

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = "";

        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.Trim()); current = ""; }
            else { current += c; }
        }
        result.Add(current.Trim());
        return result.ToArray();
    }

    #endregion
}

#region Support Classes

/// <summary>
/// Raw file data before column mapping is applied
/// </summary>
public class RawFileData
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public List<string> Headers { get; set; } = new();
    public List<string[]> DataRows { get; set; } = new();
    public int TotalRows { get; set; }
    public List<ColumnCandidate> DetectedColumns { get; set; } = new();

    /// <summary>
    /// Get preview of first N rows
    /// </summary>
    public List<string[]> GetPreview(int maxRows = 50)
    {
        return DataRows.Take(maxRows).ToList();
    }

    /// <summary>
    /// Get all data rows
    /// </summary>
    public List<string[]> GetAllRows()
    {
        return DataRows;
    }
}

/// <summary>
/// Information about a detected column
/// </summary>
public class ColumnCandidate
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public bool IsTimeColumn { get; set; }
    public bool IsHeadingColumn { get; set; }
    public bool IsReferenceCandidate { get; set; }
    public bool IsCalibratedCandidate { get; set; }

    public override string ToString() => $"[{Index}] {Name}";
}

#endregion
