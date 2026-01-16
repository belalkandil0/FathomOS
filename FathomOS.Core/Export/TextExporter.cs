using System.IO;
using System.Text;
using FathomOS.Core.Models;

namespace FathomOS.Core.Export;

/// <summary>
/// Exports survey data to text formats (CSV, Tab-delimited, Fixed-width)
/// </summary>
public class TextExporter
{
    public enum TextFormat
    {
        Csv,
        TabDelimited,
        FixedWidth
    }

    public enum ExportMode
    {
        /// <summary>
        /// Survey Listing format: KP, DCC, X, Y, Z
        /// </summary>
        SurveyListing,
        
        /// <summary>
        /// Full calculated data with all columns
        /// </summary>
        FullData,
        
        /// <summary>
        /// Raw parsed data (original NPD values)
        /// </summary>
        RawData,
        
        /// <summary>
        /// Smoothed vs Original comparison
        /// </summary>
        SmoothedComparison,
        
        /// <summary>
        /// Interval points only (DAL, X, Y, Z)
        /// </summary>
        IntervalPoints
    }

    private readonly TextFormat _format;
    private readonly bool _includeHeader;
    private readonly int _decimalPlaces;
    private readonly ExportMode _mode;

    public TextExporter(TextFormat format = TextFormat.Csv, bool includeHeader = true, 
        int decimalPlaces = 4, ExportMode mode = ExportMode.SurveyListing)
    {
        _format = format;
        _includeHeader = includeHeader;
        _decimalPlaces = decimalPlaces;
        _mode = mode;
    }

    /// <summary>
    /// Export survey points to a text file
    /// </summary>
    public void Export(string filePath, IList<SurveyPoint> points, Project project)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        Export(writer, points, project);
    }
    
    /// <summary>
    /// Export interval points to a text file
    /// </summary>
    public void ExportIntervalPoints(string filePath, IList<(double X, double Y, double Z, double Distance)> intervalPoints, Project project)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        
        // Write metadata
        string comment = _format == TextFormat.Csv ? "#" : ";";
        writer.WriteLine($"{comment} Interval Points Export");
        writer.WriteLine($"{comment} Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"{comment} Project: {project.ProjectName}");
        writer.WriteLine($"{comment} Client: {project.ClientName}");
        writer.WriteLine($"{comment} Total Points: {intervalPoints.Count}");
        if (intervalPoints.Count > 1)
        {
            var interval = intervalPoints[1].Distance - intervalPoints[0].Distance;
            writer.WriteLine($"{comment} Point Interval: {interval:F2}m");
        }
        writer.WriteLine($"{comment}");
        
        // Write header
        string[] headers = { "Index", "DAL", "X", "Y", "Z" };
        WriteHeaderLine(writer, headers);
        
        // Write data
        string fmt = $"F{_decimalPlaces}";
        for (int i = 0; i < intervalPoints.Count; i++)
        {
            var point = intervalPoints[i];
            string[] values = {
                (i + 1).ToString(),
                point.Distance.ToString(fmt),
                point.X.ToString(fmt),
                point.Y.ToString(fmt),
                point.Z.ToString(fmt)
            };
            WriteDataLine(writer, values);
        }
    }

    /// <summary>
    /// Export survey points to a stream
    /// </summary>
    public void Export(StreamWriter writer, IList<SurveyPoint> points, Project project)
    {
        // Write file header/metadata
        WriteMetadata(writer, project, points.Count);

        // Write column header
        if (_includeHeader)
        {
            WriteHeader(writer);
        }

        // Write data
        foreach (var point in points)
        {
            WriteLine(writer, point);
        }
    }

    private void WriteMetadata(StreamWriter writer, Project project, int pointCount)
    {
        string comment = _format == TextFormat.Csv ? "#" : ";";
        
        writer.WriteLine($"{comment} Survey Listing Export");
        writer.WriteLine($"{comment} Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"{comment} Project: {project.ProjectName}");
        writer.WriteLine($"{comment} Client: {project.ClientName}");
        writer.WriteLine($"{comment} Vessel: {project.VesselName}");
        writer.WriteLine($"{comment} Processor: {project.ProcessorName}");
        writer.WriteLine($"{comment} Coordinate System: {project.CoordinateSystem}");
        writer.WriteLine($"{comment} Units: {project.CoordinateUnit.GetDisplayName()}");
        writer.WriteLine($"{comment} KP Units: {project.KpUnit.GetDisplayName()}");
        writer.WriteLine($"{comment} Export Mode: {_mode}");
        writer.WriteLine($"{comment} Total Points: {pointCount}");
        writer.WriteLine($"{comment}");
    }

    private void WriteHeader(StreamWriter writer)
    {
        string[] columns = _mode switch
        {
            ExportMode.SurveyListing => new[] { "KP", "DCC", "X", "Y", "Z" },
            ExportMode.RawData => new[] { "RecNo", "DateTime", "Easting", "Northing", "Depth", "Altitude", "Heading" },
            ExportMode.FullData => new[] { "RecNo", "DateTime", "KP", "DCC", "X", "Y", "Z", "RawE", "RawN", "RawD", "RawA", "Tide", "Heading" },
            ExportMode.SmoothedComparison => new[] { "RecNo", "OriginalX", "OriginalY", "SmoothedX", "SmoothedY", "DeltaX", "DeltaY", "ShiftDist" },
            _ => new[] { "KP", "DCC", "X", "Y", "Z" }
        };

        WriteHeaderLine(writer, columns);
    }
    
    private void WriteHeaderLine(StreamWriter writer, string[] columns)
    {
        switch (_format)
        {
            case TextFormat.Csv:
                writer.WriteLine(string.Join(",", columns));
                break;
            case TextFormat.TabDelimited:
                writer.WriteLine(string.Join("\t", columns));
                break;
            case TextFormat.FixedWidth:
                WriteFixedWidthHeader(writer, columns);
                break;
        }
    }

    private void WriteFixedWidthHeader(StreamWriter writer, string[] columns)
    {
        var sb = new StringBuilder();
        foreach (var col in columns)
        {
            int width = GetColumnWidth(col);
            sb.Append(col.PadRight(width));
        }
        writer.WriteLine(sb.ToString());
    }

    private int GetColumnWidth(string columnName)
    {
        return columnName switch
        {
            "RecNo" or "Index" => 8,
            "DateTime" => 22,
            "Easting" or "Northing" or "RawE" or "RawN" => 15,
            "OriginalX" or "OriginalY" or "SmoothedX" or "SmoothedY" => 15,
            "DeltaX" or "DeltaY" or "ShiftDist" => 12,
            "KP" => 14,
            "DCC" or "DAL" => 12,
            "X" or "Y" => 15,
            "Z" or "Depth" or "RawD" => 12,
            "Altitude" or "RawA" => 10,
            "Tide" => 10,
            "Heading" => 10,
            _ => 12
        };
    }

    private void WriteLine(StreamWriter writer, SurveyPoint point)
    {
        string fmt = $"F{_decimalPlaces}";
        string[] values;

        switch (_mode)
        {
            case ExportMode.SurveyListing:
                // KP, DCC, X, Y, Z - the main survey listing output
                values = new[]
                {
                    point.Kp?.ToString("F6") ?? "",
                    point.Dcc?.ToString("F3") ?? "",
                    point.X.ToString(fmt),
                    point.Y.ToString(fmt),
                    point.CalculatedZ?.ToString(fmt) ?? ""
                };
                break;

            case ExportMode.RawData:
                // Original parsed data
                values = new[]
                {
                    point.RecordNumber.ToString(),
                    point.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    point.Easting.ToString(fmt),
                    point.Northing.ToString(fmt),
                    point.Depth?.ToString(fmt) ?? "",
                    point.Altitude?.ToString(fmt) ?? "",
                    point.Heading?.ToString("F1") ?? ""
                };
                break;
                
            case ExportMode.SmoothedComparison:
                // Smoothed vs Original comparison
                double smoothedX = point.SmoothedEasting ?? point.Easting;
                double smoothedY = point.SmoothedNorthing ?? point.Northing;
                double deltaX = smoothedX - point.Easting;
                double deltaY = smoothedY - point.Northing;
                double shiftDist = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                
                values = new[]
                {
                    point.RecordNumber.ToString(),
                    point.Easting.ToString(fmt),
                    point.Northing.ToString(fmt),
                    smoothedX.ToString(fmt),
                    smoothedY.ToString(fmt),
                    deltaX.ToString(fmt),
                    deltaY.ToString(fmt),
                    shiftDist.ToString(fmt)
                };
                break;

            case ExportMode.FullData:
            default:
                // Full data with all columns
                values = new[]
                {
                    point.RecordNumber.ToString(),
                    point.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    point.Kp?.ToString("F6") ?? "",
                    point.Dcc?.ToString("F3") ?? "",
                    point.X.ToString(fmt),
                    point.Y.ToString(fmt),
                    point.CalculatedZ?.ToString(fmt) ?? "",
                    point.Easting.ToString(fmt),
                    point.Northing.ToString(fmt),
                    point.Depth?.ToString(fmt) ?? "",
                    point.Altitude?.ToString(fmt) ?? "",
                    point.TideCorrection?.ToString("F3") ?? "",
                    point.Heading?.ToString("F1") ?? ""
                };
                break;
        }

        WriteDataLine(writer, values);
    }
    
    private void WriteDataLine(StreamWriter writer, string[] values)
    {
        switch (_format)
        {
            case TextFormat.Csv:
                writer.WriteLine(string.Join(",", values));
                break;
            case TextFormat.TabDelimited:
                writer.WriteLine(string.Join("\t", values));
                break;
            case TextFormat.FixedWidth:
                WriteFixedWidthLine(writer, values);
                break;
        }
    }

    private void WriteFixedWidthLine(StreamWriter writer, string[] values)
    {
        string[] columnNames = _mode switch
        {
            ExportMode.SurveyListing => new[] { "KP", "DCC", "X", "Y", "Z" },
            ExportMode.RawData => new[] { "RecNo", "DateTime", "Easting", "Northing", "Depth", "Altitude", "Heading" },
            ExportMode.FullData => new[] { "RecNo", "DateTime", "KP", "DCC", "X", "Y", "Z", "RawE", "RawN", "RawD", "RawA", "Tide", "Heading" },
            ExportMode.SmoothedComparison => new[] { "RecNo", "OriginalX", "OriginalY", "SmoothedX", "SmoothedY", "DeltaX", "DeltaY", "ShiftDist" },
            ExportMode.IntervalPoints => new[] { "Index", "DAL", "X", "Y", "Z" },
            _ => new[] { "KP", "DCC", "X", "Y", "Z" }
        };

        var sb = new StringBuilder();
        for (int i = 0; i < values.Length && i < columnNames.Length; i++)
        {
            int width = GetColumnWidth(columnNames[i]);
            sb.Append(values[i].PadRight(width));
        }
        writer.WriteLine(sb.ToString());
    }

    /// <summary>
    /// Parse text format from string
    /// </summary>
    public static TextFormat ParseFormat(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "csv" => TextFormat.Csv,
            "tab" or "tab-delimited" or "tabdelimited" => TextFormat.TabDelimited,
            "fixed" or "fixed-width" or "fixedwidth" => TextFormat.FixedWidth,
            _ => TextFormat.Csv
        };
    }
}
