using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FathomOS.Modules.RovGyroCalibration.Models;

namespace FathomOS.Modules.RovGyroCalibration.Services;

/// <summary>
/// Service for parsing NPD/CSV data files for ROV gyro calibration.
/// Handles NaviPac format with date/time column split.
/// </summary>
public class DataParsingService
{
    #region Raw Data Loading (Step 2)

    /// <summary>
    /// Load file and return raw data without interpretation.
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

        result.Headers = ParseCsvLine(lines[0]).ToList();
        result.TotalRows = lines.Length - 1;

        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length > 0)
                result.DataRows.Add(values);
        }

        result.DetectedColumns = DetectHeadingColumns(result.Headers);
        return result;
    }

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
                IsReferenceCandidate = header.Contains("vessel") || header.Contains("ref"),
                IsCalibratedCandidate = header.Contains("rov") || header.Contains("system")
            };
            candidates.Add(candidate);
        }
        return candidates;
    }

    #endregion

    #region Parsed Data (Step 3-4)

    /// <summary>
    /// Parse file using explicit column mapping.
    /// Returns ROV data points with vessel and ROV headings.
    /// </summary>
    public List<RovGyroDataPoint> ParseWithMapping(RawFileData rawData, RovGyroColumnMapping mapping)
    {
        var points = new List<RovGyroDataPoint>();
        int offset = mapping.HasDateTimeSplit ? 1 : 0;

        for (int i = 0; i < rawData.DataRows.Count; i++)
        {
            try
            {
                var values = rawData.DataRows[i];
                if (values.Length < 3) continue;

                var point = new RovGyroDataPoint { Index = i + 1 };

                // Parse DateTime
                point.DateTime = ParseDateTime(values, mapping);

                // Parse Vessel Heading (Reference)
                int vesselIdx = mapping.VesselHeadingColumnIndex;
                if (vesselIdx >= 0)
                {
                    int actualIdx = vesselIdx + offset;
                    if (actualIdx < values.Length)
                    {
                        if (double.TryParse(values[actualIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var vh))
                            point.VesselHeading = NormalizeHeading(vh);
                    }
                }

                // Parse ROV Heading
                int rovIdx = mapping.RovHeadingColumnIndex;
                if (rovIdx >= 0)
                {
                    int actualIdx = rovIdx + offset;
                    if (actualIdx < values.Length)
                    {
                        if (double.TryParse(values[actualIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var rh))
                            point.RovHeading = NormalizeHeading(rh);
                    }
                }

                // Calculate basic C-O (before geometric corrections)
                point.CalculatedCO = CalculateCO(point.VesselHeading, point.RovHeading);

                points.Add(point);
            }
            catch { /* Skip malformed lines */ }
        }

        return points;
    }

    private DateTime ParseDateTime(string[] values, RovGyroColumnMapping mapping)
    {
        if (mapping.HasDateTimeSplit && values.Length >= 2)
        {
            var dateStr = values[0];
            var timeStr = values[1];
            if (DateTime.TryParse($"{dateStr} {timeStr}", out var dt))
                return dt;

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

    private static double NormalizeHeading(double heading)
    {
        while (heading < 0) heading += 360;
        while (heading >= 360) heading -= 360;
        return heading;
    }

    public static double CalculateCO(double referenceHeading, double observedHeading)
    {
        double diff = referenceHeading - observedHeading;
        if (diff < -180) return diff + 360;
        if (diff > 180) return diff - 360;
        return diff;
    }

    #endregion

    #region Legacy Methods

    public List<RovGyroDataPoint> ParseNpdFile(string filePath)
    {
        var rawData = LoadRawFile(filePath);
        var mapping = AutoDetectMapping(rawData);
        return ParseWithMapping(rawData, mapping);
    }

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

    public List<RovGyroDataPoint> ParseFile(string filePath, RovGyroColumnMapping mapping)
    {
        var rawData = LoadRawFile(filePath);
        return ParseWithMapping(rawData, mapping);
    }

    public RovGyroColumnMapping AutoDetectMapping(RawFileData rawData)
    {
        var mapping = new RovGyroColumnMapping { HasDateTimeSplit = true };
        var headers = rawData.Headers.Select(h => h.ToLowerInvariant()).ToArray();

        mapping.TimeColumnIndex = Array.FindIndex(headers, h => 
            h.Contains("time") || h.Contains("utc"));

        // Find vessel heading
        mapping.VesselHeadingColumnIndex = Array.FindIndex(headers, h =>
            (h.Contains("vessel") && h.Contains("heading")) ||
            (h.Contains("ref") && h.Contains("heading")));

        if (mapping.VesselHeadingColumnIndex < 0)
        {
            mapping.VesselHeadingColumnIndex = Array.FindIndex(headers, h =>
                h.Contains("heading") || h.Contains("hdg") || h.Contains("gyro"));
        }

        // Find ROV heading
        mapping.RovHeadingColumnIndex = Array.FindIndex(headers, h =>
            (h.Contains("rov") && h.Contains("heading")) ||
            (h.Contains("rov") && h.Contains("hdg")));

        if (mapping.RovHeadingColumnIndex < 0)
        {
            int firstHeading = mapping.VesselHeadingColumnIndex;
            for (int i = 0; i < headers.Length; i++)
            {
                if (i != firstHeading && (headers[i].Contains("heading") || headers[i].Contains("hdg")))
                {
                    mapping.RovHeadingColumnIndex = i;
                    break;
                }
            }
        }

        // Set column names for display
        if (mapping.TimeColumnIndex >= 0 && mapping.TimeColumnIndex < rawData.Headers.Count)
            mapping.TimeColumn = rawData.Headers[mapping.TimeColumnIndex];
        if (mapping.VesselHeadingColumnIndex >= 0 && mapping.VesselHeadingColumnIndex < rawData.Headers.Count)
            mapping.VesselHeadingColumn = rawData.Headers[mapping.VesselHeadingColumnIndex];
        if (mapping.RovHeadingColumnIndex >= 0 && mapping.RovHeadingColumnIndex < rawData.Headers.Count)
            mapping.RovHeadingColumn = rawData.Headers[mapping.RovHeadingColumnIndex];

        return mapping;
    }

    public RovGyroColumnMapping AutoDetectColumns(List<string> columns)
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

public class RawFileData
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public List<string> Headers { get; set; } = new();
    public List<string[]> DataRows { get; set; } = new();
    public int TotalRows { get; set; }
    public List<ColumnCandidate> DetectedColumns { get; set; } = new();

    public List<string[]> GetPreview(int maxRows = 50) => DataRows.Take(maxRows).ToList();
    
    public List<string[]> GetAllRows() => DataRows;
}

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
