using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FathomOS.Modules.SoundVelocity.Models;

namespace FathomOS.Modules.SoundVelocity.Services;

/// <summary>
/// Parses various CTD/SVP file formats with intelligent auto-detection
/// Supports: Valeport MIDAS SVX2, QINSy, EIVA, CSV, and other formats
/// </summary>
public class FileParserService
{
    /// <summary>
    /// Auto-detect file type and parse the file with smart column detection
    /// </summary>
    public ParsedFileResult ParseFile(string filePath)
    {
        var result = new ParsedFileResult
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Success = false
        };

        try
        {
            // Detect file type
            result.DetectedFileType = DetectFileType(filePath);
            
            // Read all lines
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
            {
                result.ErrorMessage = "File is empty";
                return result;
            }

            // Parse based on detected type
            switch (result.DetectedFileType)
            {
                case InputFileType.Valeport:
                    ParseValeportFile(lines, result);
                    break;
                case InputFileType.QINSyExport:
                    ParseDelimitedFile(lines, result, ',');
                    break;
                case InputFileType.QINSyLog:
                    ParseSpaceDelimitedFile(lines, result);
                    break;
                case InputFileType.Semicolon:
                    ParseDelimitedFile(lines, result, ';');
                    break;
                case InputFileType.FixedWidth:
                    ParseSpaceDelimitedFile(lines, result);
                    break;
            }

            // Generate column info for display
            if (result.DataRows.Count > 0)
            {
                GenerateColumnInfos(result);
                result.Success = true;
            }
            else
            {
                result.ErrorMessage = "No data rows found in file";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Parse error: {ex.Message}";
            result.Success = false;
        }

        return result;
    }

    /// <summary>
    /// Detect file type from extension and content analysis
    /// </summary>
    public InputFileType DetectFileType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Check extension first - Valeport files use .000, .001, etc.
        if (ext == ".000" || ext == ".001" || ext == ".002" || ext == ".003" || 
            ext == ".004" || ext == ".005")
            return InputFileType.Valeport;
        
        if (ext == ".bp3")
            return InputFileType.FixedWidth;
        
        if (ext == ".log")
            return InputFileType.QINSyLog;
        
        if (ext == ".npd")
            return InputFileType.QINSyExport;

        // For .txt/.csv files, analyze content
        if (ext == ".txt" || ext == ".csv" || ext == ".dat")
        {
            var lines = File.ReadAllLines(filePath).Take(30).ToArray();
            return AnalyzeFileContent(lines);
        }

        return InputFileType.QINSyExport; // Default
    }

    private InputFileType AnalyzeFileContent(string[] lines)
    {
        // Check for Valeport MIDAS header patterns
        foreach (var line in lines)
        {
            if (line.Contains("MIDAS") || line.Contains("Valeport") || 
                line.Contains("Model Name") || line.Contains("Serial No.") ||
                line.Contains("SOUND VELOCITY") || line.Contains("Site Information"))
                return InputFileType.Valeport;
        }

        // Find data lines and count delimiters
        var dataLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && 
                                          !l.StartsWith("[") && 
                                          !l.StartsWith("#") &&
                                          !l.StartsWith("//")).Take(10).ToList();
        
        if (dataLines.Count == 0) return InputFileType.QINSyExport;

        int tabCount = dataLines.Sum(l => l.Count(c => c == '\t'));
        int commaCount = dataLines.Sum(l => l.Count(c => c == ','));
        int semicolonCount = dataLines.Sum(l => l.Count(c => c == ';'));

        if (tabCount > commaCount && tabCount > semicolonCount)
            return InputFileType.Valeport;
        if (semicolonCount > commaCount)
            return InputFileType.Semicolon;
        if (commaCount > 0)
            return InputFileType.QINSyExport;

        return InputFileType.QINSyLog;
    }

    /// <summary>
    /// Parse Valeport MIDAS SVX2 format files
    /// Format: Metadata header lines followed by tab-delimited data
    /// Header: "Key :\tValue" format
    /// Data header: "Date / Time\tSOUND VELOCITY;M/SEC\tPRESSURE;M\t..."
    /// </summary>
    private void ParseValeportFile(string[] lines, ParsedFileResult result)
    {
        var dataRows = new List<DataRow>();
        int headerLineIndex = -1;
        bool parsingData = false;
        string[] columnHeaders = Array.Empty<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Check if this is the column header row (contains data column names)
            if (!parsingData && IsValeportHeaderRow(line))
            {
                // Split by tabs, clean up header names
                columnHeaders = line.Split('\t')
                    .Select(h => CleanHeaderName(h))
                    .ToArray();
                
                result.DetectedHeaders = columnHeaders;
                result.ColumnCount = columnHeaders.Length;
                headerLineIndex = i;
                parsingData = true;
                
                // Auto-detect column mappings based on header names
                AutoDetectColumnsFromHeaders(result, columnHeaders);
                continue;
            }

            // Parse metadata from header section (before data)
            if (!parsingData)
            {
                ParseValeportMetadataLine(line, result);
                continue;
            }

            // Parse data rows
            var parts = line.Split('\t');
            if (parts.Length >= 2)
            {
                // First column is typically DateTime, rest are numeric
                var row = new DataRow
                {
                    RowIndex = dataRows.Count + 1,
                    RawValues = parts,
                    Values = ParseDataRowValues(parts)
                };
                
                // Try to extract timestamp
                if (!string.IsNullOrEmpty(parts[0]))
                {
                    row.Timestamp = parts[0];
                    if (DateTime.TryParseExact(parts[0], new[] { 
                        "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", 
                        "dd/MM/yyyy HH:mm", "yyyy-MM-dd HH:mm",
                        "MM/dd/yyyy HH:mm:ss", "yyyy/MM/dd HH:mm:ss" }, 
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        row.ParsedTimestamp = dt;
                    }
                }
                
                dataRows.Add(row);
            }
        }

        result.DataRows = dataRows;
        result.HeaderRowIndex = headerLineIndex;
        result.TotalRowCount = dataRows.Count;
    }

    /// <summary>
    /// Check if line is the Valeport column header row
    /// </summary>
    private bool IsValeportHeaderRow(string line)
    {
        var lower = line.ToLowerInvariant();
        return (lower.Contains("date") && lower.Contains("time")) ||
               (lower.Contains("sound velocity") && (lower.Contains("pressure") || lower.Contains("depth"))) ||
               (lower.Contains("temperature") && lower.Contains("conductivity"));
    }

    /// <summary>
    /// Clean header name - remove units in parentheses or after semicolons
    /// </summary>
    private string CleanHeaderName(string header)
    {
        header = header.Trim();
        
        // "SOUND VELOCITY;M/SEC" -> "Sound Velocity"
        var semiIndex = header.IndexOf(';');
        if (semiIndex > 0)
            header = header.Substring(0, semiIndex);
        
        // Title case
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(header.ToLowerInvariant());
    }

    /// <summary>
    /// Parse metadata line from Valeport file header
    /// </summary>
    private void ParseValeportMetadataLine(string line, ParsedFileResult result)
    {
        var colonIdx = line.IndexOf(':');
        if (colonIdx > 0)
        {
            var key = line.Substring(0, colonIdx).Trim();
            var value = line.Substring(colonIdx + 1).Trim().TrimStart('\t');
            
            if (!string.IsNullOrEmpty(key))
            {
                result.Metadata[key] = value;
                
                var keyLower = key.ToLowerInvariant();
                
                if (keyLower.Contains("time stamp") || keyLower.Contains("timestamp"))
                {
                    if (DateTime.TryParseExact(value, new[] { 
                        "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" },
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        result.ExtractedDateTime = dt;
                    }
                }
                else if (keyLower.Contains("site") || keyLower.Contains("location"))
                {
                    result.ExtractedStation = value;
                }
                else if (keyLower.Contains("model"))
                {
                    result.ExtractedEquipment = value;
                }
                else if (keyLower.Contains("serial"))
                {
                    if (!string.IsNullOrEmpty(result.ExtractedEquipment))
                        result.ExtractedEquipment += $" (S/N: {value})";
                    else
                        result.ExtractedEquipment = $"S/N: {value}";
                }
            }
        }
    }

    /// <summary>
    /// Auto-detect column mappings based on header names
    /// </summary>
    private void AutoDetectColumnsFromHeaders(ParsedFileResult result, string[] headers)
    {
        var mapping = new ColumnMapping();
        int measuredSvColumn = -1;
        int calcSvColumn = -1;
        
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].ToLowerInvariant();
            
            // Skip DateTime column
            if (header.Contains("date") && header.Contains("time"))
                continue;
            
            // Depth or Pressure
            if (header.Contains("pressure") || header.Contains("depth"))
            {
                if (mapping.DepthColumn < 0)
                {
                    mapping.DepthColumn = i;
                    result.DepthUnit = header.Contains("dbar") ? "dbar" : "m";
                }
            }
            // Sound Velocity - track both measured and calculated
            else if (header.Contains("sound velocity") || header.Contains("sv"))
            {
                if (header.Contains("calc"))
                    calcSvColumn = i;
                else
                    measuredSvColumn = i;
            }
            // Temperature
            else if (header.Contains("temp"))
            {
                if (mapping.TemperatureColumn < 0)
                    mapping.TemperatureColumn = i;
            }
            // Salinity
            else if (header.Contains("salinity") || header.Contains("sal"))
            {
                if (mapping.SalinityColumn < 0)
                    mapping.SalinityColumn = i;
            }
            // Conductivity (use for salinity if not found)
            else if (header.Contains("conductivity") || header.Contains("cond"))
            {
                if (mapping.SalinityColumn < 0)
                    mapping.SalinityColumn = i;
            }
            // Density
            else if (header.Contains("density") || header.Contains("dens"))
            {
                if (mapping.DensityColumn < 0)
                    mapping.DensityColumn = i;
            }
        }
        
        // Prefer calculated SV over measured (measured may be 0 at surface)
        mapping.SoundVelocityColumn = calcSvColumn >= 0 ? calcSvColumn : measuredSvColumn;
        
        result.ColumnMappings = mapping;
    }

    /// <summary>
    /// Parse data row values - first value may be DateTime string
    /// </summary>
    private double[] ParseDataRowValues(string[] parts)
    {
        var values = new double[parts.Length];
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            if (double.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                values[i] = val;
            else
                values[i] = double.NaN;
        }
        
        return values;
    }

    /// <summary>
    /// Parse comma or semicolon delimited files
    /// </summary>
    private void ParseDelimitedFile(string[] lines, ParsedFileResult result, char delimiter)
    {
        var dataRows = new List<DataRow>();
        bool foundHeaders = false;
        int headerLineIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#") || line.StartsWith("//")) continue;

            var parts = SplitLine(line, delimiter);
            if (parts.Length < 2) continue;

            if (!foundHeaders && HasTextContent(parts))
            {
                result.DetectedHeaders = parts;
                result.ColumnCount = parts.Length;
                foundHeaders = true;
                headerLineIndex = i;
                AutoDetectColumnsFromHeaders(result, parts);
                continue;
            }

            if (IsNumericRow(parts))
            {
                if (!foundHeaders)
                {
                    result.DetectedHeaders = CreateGenericHeaders(parts.Length);
                    result.ColumnCount = parts.Length;
                    foundHeaders = true;
                }

                var row = new DataRow
                {
                    RowIndex = dataRows.Count + 1,
                    RawValues = parts,
                    Values = ParseNumericValues(parts)
                };
                dataRows.Add(row);
            }
        }

        result.DataRows = dataRows;
        result.HeaderRowIndex = headerLineIndex;
        result.TotalRowCount = dataRows.Count;
        
        if (result.ColumnMappings.DepthColumn < 0 && dataRows.Count > 0)
            DetectColumnsByValueRanges(result);
    }

    /// <summary>
    /// Parse space-delimited files
    /// </summary>
    private void ParseSpaceDelimitedFile(string[] lines, ParsedFileResult result)
    {
        var dataRows = new List<DataRow>();
        bool foundHeaders = false;
        int headerLineIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#") || line.StartsWith("//")) continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            if (!foundHeaders && HasTextContent(parts) && !IsNumericRow(parts))
            {
                result.DetectedHeaders = parts;
                result.ColumnCount = parts.Length;
                foundHeaders = true;
                headerLineIndex = i;
                AutoDetectColumnsFromHeaders(result, parts);
                continue;
            }

            if (IsNumericRow(parts))
            {
                if (!foundHeaders)
                {
                    result.DetectedHeaders = CreateGenericHeaders(parts.Length);
                    result.ColumnCount = parts.Length;
                    foundHeaders = true;
                }

                var row = new DataRow
                {
                    RowIndex = dataRows.Count + 1,
                    RawValues = parts,
                    Values = ParseNumericValues(parts)
                };
                dataRows.Add(row);
            }
        }

        result.DataRows = dataRows;
        result.HeaderRowIndex = headerLineIndex;
        result.TotalRowCount = dataRows.Count;
        
        if (result.ColumnMappings.DepthColumn < 0 && dataRows.Count > 0)
            DetectColumnsByValueRanges(result);
    }

    /// <summary>
    /// Generate column info for UI display
    /// </summary>
    private void GenerateColumnInfos(ParsedFileResult result)
    {
        result.ColumnInfos = new List<ColumnInfo>();
        
        for (int col = 0; col < result.ColumnCount; col++)
        {
            var info = new ColumnInfo
            {
                Index = col,
                HeaderName = col < result.DetectedHeaders.Length 
                    ? result.DetectedHeaders[col] 
                    : $"Column {col + 1}"
            };
            
            var values = result.DataRows
                .Take(1000)
                .Where(r => col < r.Values.Length && !double.IsNaN(r.Values[col]))
                .Select(r => r.Values[col])
                .ToList();
            
            if (values.Count > 0)
            {
                info.MinValue = values.Min();
                info.MaxValue = values.Max();
                info.SampleValues = values.Take(5).ToArray();
            }
            
            info.DetectedType = GetDetectedType(col, result.ColumnMappings);
            
            result.ColumnInfos.Add(info);
        }
    }

    /// <summary>
    /// Detect columns by analyzing value ranges
    /// </summary>
    private void DetectColumnsByValueRanges(ParsedFileResult result)
    {
        var mapping = result.ColumnMappings;
        
        for (int col = 0; col < result.ColumnCount; col++)
        {
            var values = result.DataRows
                .Take(500)
                .Where(r => col < r.Values.Length && !double.IsNaN(r.Values[col]))
                .Select(r => r.Values[col])
                .ToList();
            
            if (values.Count == 0) continue;
            
            double min = values.Min();
            double max = values.Max();

            if (mapping.DepthColumn < 0 && min >= -10 && max <= 12000 && IsMonotonicallyIncreasing(values))
                mapping.DepthColumn = col;
            else if (mapping.SoundVelocityColumn < 0 && min >= 1400 && max <= 1600)
                mapping.SoundVelocityColumn = col;
            else if (mapping.TemperatureColumn < 0 && min >= -3 && max <= 40 && 
                     mapping.SoundVelocityColumn != col && mapping.DepthColumn != col)
                mapping.TemperatureColumn = col;
            else if (mapping.SalinityColumn < 0 && min >= 0 && max <= 50 &&
                     mapping.TemperatureColumn != col && mapping.DepthColumn != col)
                mapping.SalinityColumn = col;
            else if (mapping.DensityColumn < 0 && 
                     ((min >= 990 && max <= 1080) || (min >= 20 && max <= 50)))
                mapping.DensityColumn = col;
        }
    }

    private bool IsMonotonicallyIncreasing(List<double> values)
    {
        if (values.Count < 10) return false;
        int increasing = 0, decreasing = 0;
        for (int i = 1; i < Math.Min(values.Count, 200); i++)
        {
            if (values[i] > values[i - 1]) increasing++;
            else if (values[i] < values[i - 1]) decreasing++;
        }
        return increasing > decreasing * 2;
    }

    private string GetDetectedType(int columnIndex, ColumnMapping mapping)
    {
        if (mapping.DepthColumn == columnIndex) return "Depth/Pressure";
        if (mapping.SoundVelocityColumn == columnIndex) return "Sound Velocity";
        if (mapping.TemperatureColumn == columnIndex) return "Temperature";
        if (mapping.SalinityColumn == columnIndex) return "Salinity/Conductivity";
        if (mapping.DensityColumn == columnIndex) return "Density";
        return "";
    }

    #region Helper Methods

    private string[] SplitLine(string line, char delimiter)
    {
        return line.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
                   .Select(p => p.Trim().Trim('"')).ToArray();
    }

    private bool HasTextContent(string[] parts)
    {
        return parts.Any(p => !string.IsNullOrEmpty(p) && p.Any(c => char.IsLetter(c)));
    }

    private bool IsNumericRow(string[] parts)
    {
        if (parts.Length == 0) return false;
        int numericCount = parts.Count(p => 
            double.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out _));
        return numericCount >= parts.Length * 0.5;
    }

    private double[] ParseNumericValues(string[] parts)
    {
        var values = new double[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                values[i] = val;
            else
                values[i] = double.NaN;
        }
        return values;
    }

    private string[] CreateGenericHeaders(int count)
    {
        return Enumerable.Range(1, count).Select(i => $"Column {i}").ToArray();
    }

    #endregion

    /// <summary>
    /// Convert parsed result to CTD data points using the column mappings
    /// </summary>
    public List<CtdDataPoint> ConvertToDataPoints(ParsedFileResult parseResult, ColumnMapping? overrideMapping = null)
    {
        var points = new List<CtdDataPoint>();
        var mapping = overrideMapping ?? parseResult.ColumnMappings;

        for (int i = 0; i < parseResult.DataRows.Count; i++)
        {
            var row = parseResult.DataRows[i];
            var point = new CtdDataPoint { Index = i + 1 };

            if (row.ParsedTimestamp.HasValue)
                point.Timestamp = row.ParsedTimestamp;

            if (mapping.DepthColumn >= 0 && mapping.DepthColumn < row.Values.Length)
            {
                var val = row.Values[mapping.DepthColumn];
                if (!double.IsNaN(val)) point.Depth = val;
            }
            
            if (mapping.SoundVelocityColumn >= 0 && mapping.SoundVelocityColumn < row.Values.Length)
            {
                var val = row.Values[mapping.SoundVelocityColumn];
                if (!double.IsNaN(val)) point.SoundVelocity = val;
            }
            
            if (mapping.TemperatureColumn >= 0 && mapping.TemperatureColumn < row.Values.Length)
            {
                var val = row.Values[mapping.TemperatureColumn];
                if (!double.IsNaN(val)) point.Temperature = val;
            }
            
            if (mapping.SalinityColumn >= 0 && mapping.SalinityColumn < row.Values.Length)
            {
                var val = row.Values[mapping.SalinityColumn];
                if (!double.IsNaN(val)) point.SalinityOrConductivity = val;
            }
            
            if (mapping.DensityColumn >= 0 && mapping.DensityColumn < row.Values.Length)
            {
                var val = row.Values[mapping.DensityColumn];
                if (!double.IsNaN(val)) point.Density = val;
            }

            points.Add(point);
        }

        return points;
    }
}

#region Result Classes

/// <summary>
/// Complete result from file parsing with auto-detection info
/// </summary>
public class ParsedFileResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public InputFileType DetectedFileType { get; set; }
    
    public string[] DetectedHeaders { get; set; } = Array.Empty<string>();
    public int ColumnCount { get; set; }
    public int HeaderRowIndex { get; set; } = -1;
    public int TotalRowCount { get; set; }
    
    public List<DataRow> DataRows { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    public ColumnMapping ColumnMappings { get; set; } = new();
    public List<ColumnInfo> ColumnInfos { get; set; } = new();
    
    public DateTime? ExtractedDateTime { get; set; }
    public string? ExtractedLatitude { get; set; }
    public string? ExtractedLongitude { get; set; }
    public string? ExtractedVessel { get; set; }
    public string? ExtractedStation { get; set; }
    public string? ExtractedEquipment { get; set; }
    
    public string DepthUnit { get; set; } = "m";
}

/// <summary>
/// Single data row with parsed values
/// </summary>
public class DataRow
{
    public int RowIndex { get; set; }
    public string[] RawValues { get; set; } = Array.Empty<string>();
    public double[] Values { get; set; } = Array.Empty<double>();
    public string? Timestamp { get; set; }
    public DateTime? ParsedTimestamp { get; set; }
}

/// <summary>
/// Information about a detected column
/// </summary>
public class ColumnInfo
{
    public int Index { get; set; }
    public string HeaderName { get; set; } = string.Empty;
    public string DetectedType { get; set; } = "";
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double[] SampleValues { get; set; } = Array.Empty<double>();
    
    public string DisplayName => $"{(char)('A' + Index)}: {HeaderName}";
    public string RangeDisplay => double.IsNaN(MinValue) ? "N/A" : $"{MinValue:F2} - {MaxValue:F2}";
}

// Note: ColumnMapping and InputFileType are defined in Models/DataModels.cs and Models/Enums.cs

#endregion
