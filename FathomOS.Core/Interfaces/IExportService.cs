using FathomOS.Core.Models;

namespace FathomOS.Core.Interfaces;

/// <summary>
/// Unified export service interface providing consistent export functionality
/// across all supported formats (Excel, PDF, DXF, CSV, JSON).
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exports data to Excel format (.xlsx).
    /// </summary>
    /// <typeparam name="T">Type of data to export.</typeparam>
    /// <param name="data">Collection of data to export.</param>
    /// <param name="options">Excel export options.</param>
    /// <returns>Byte array containing the Excel file.</returns>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportSettings options);

    /// <summary>
    /// Exports data to PDF format.
    /// </summary>
    /// <param name="data">Data to export (can be any supported type).</param>
    /// <param name="template">PDF template configuration.</param>
    /// <returns>Byte array containing the PDF file.</returns>
    Task<byte[]> ExportToPdfAsync(object data, PdfExportSettings template);

    /// <summary>
    /// Exports geometry data to DXF format.
    /// </summary>
    /// <param name="data">Geometry data to export.</param>
    /// <param name="options">DXF export options.</param>
    /// <returns>Byte array containing the DXF file.</returns>
    Task<byte[]> ExportToDxfAsync(IEnumerable<GeometryData> data, DxfExportSettings options);

    /// <summary>
    /// Exports data to CSV format.
    /// </summary>
    /// <typeparam name="T">Type of data to export.</typeparam>
    /// <param name="data">Collection of data to export.</param>
    /// <param name="options">CSV export options.</param>
    /// <returns>Byte array containing the CSV file.</returns>
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CsvExportSettings options);

    /// <summary>
    /// Exports data to JSON format.
    /// </summary>
    /// <typeparam name="T">Type of data to export.</typeparam>
    /// <param name="data">Data to export.</param>
    /// <param name="options">JSON export options (optional).</param>
    /// <returns>Byte array containing the JSON file.</returns>
    Task<byte[]> ExportToJsonAsync<T>(T data, JsonExportSettings? options = null);

    /// <summary>
    /// Exports data to a file.
    /// </summary>
    /// <typeparam name="T">Type of data to export.</typeparam>
    /// <param name="data">Data to export.</param>
    /// <param name="filePath">Output file path.</param>
    /// <param name="format">Export format.</param>
    /// <param name="settings">Format-specific settings.</param>
    Task ExportToFileAsync<T>(T data, string filePath, ExportFormat format, ExportSettings? settings = null);

    /// <summary>
    /// Gets supported export formats for a data type.
    /// </summary>
    /// <typeparam name="T">Data type to check.</typeparam>
    /// <returns>Collection of supported formats.</returns>
    IEnumerable<ExportFormat> GetSupportedFormats<T>();
}

/// <summary>
/// Supported export formats.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Microsoft Excel (.xlsx) format.
    /// </summary>
    Excel,

    /// <summary>
    /// Portable Document Format (.pdf).
    /// </summary>
    Pdf,

    /// <summary>
    /// AutoCAD Drawing Exchange Format (.dxf).
    /// </summary>
    Dxf,

    /// <summary>
    /// Comma-Separated Values (.csv).
    /// </summary>
    Csv,

    /// <summary>
    /// JavaScript Object Notation (.json).
    /// </summary>
    Json,

    /// <summary>
    /// Plain text format (.txt).
    /// </summary>
    Text,

    /// <summary>
    /// XML format (.xml).
    /// </summary>
    Xml
}

/// <summary>
/// Base class for export settings.
/// </summary>
public abstract class ExportSettings
{
    /// <summary>
    /// Output file name (without path).
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Title for the export.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Author/creator name.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Include timestamp in output.
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;
}

/// <summary>
/// Settings for Excel export.
/// </summary>
public class ExcelExportSettings : ExportSettings
{
    /// <summary>
    /// Sheet name for the data.
    /// </summary>
    public string SheetName { get; set; } = "Data";

    /// <summary>
    /// Include column headers.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Auto-fit column widths.
    /// </summary>
    public bool AutoFitColumns { get; set; } = true;

    /// <summary>
    /// Freeze header row.
    /// </summary>
    public bool FreezeHeaderRow { get; set; } = true;

    /// <summary>
    /// Apply alternating row colors.
    /// </summary>
    public bool AlternateRowColors { get; set; } = true;

    /// <summary>
    /// Include filters on header row.
    /// </summary>
    public bool EnableFilters { get; set; } = false;

    /// <summary>
    /// Column-specific formatting.
    /// </summary>
    public Dictionary<string, string> ColumnFormats { get; set; } = new();

    /// <summary>
    /// Columns to exclude from export.
    /// </summary>
    public HashSet<string> ExcludedColumns { get; set; } = new();

    /// <summary>
    /// Custom column headers (property name -> display name).
    /// </summary>
    public Dictionary<string, string> ColumnHeaders { get; set; } = new();
}

/// <summary>
/// Settings for PDF export.
/// </summary>
public class PdfExportSettings : ExportSettings
{
    /// <summary>
    /// Page size.
    /// </summary>
    public PageSize PageSize { get; set; } = PageSize.A4;

    /// <summary>
    /// Page orientation.
    /// </summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    /// <summary>
    /// Page margins in points.
    /// </summary>
    public PageMargins Margins { get; set; } = new();

    /// <summary>
    /// Include page numbers.
    /// </summary>
    public bool IncludePageNumbers { get; set; } = true;

    /// <summary>
    /// Include header on each page.
    /// </summary>
    public bool IncludeHeader { get; set; } = true;

    /// <summary>
    /// Include footer on each page.
    /// </summary>
    public bool IncludeFooter { get; set; } = true;

    /// <summary>
    /// Header text.
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// Footer text.
    /// </summary>
    public string? FooterText { get; set; }

    /// <summary>
    /// Logo image path for header.
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Font family name.
    /// </summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>
    /// Base font size in points.
    /// </summary>
    public float FontSize { get; set; } = 10f;
}

/// <summary>
/// Settings for DXF export.
/// </summary>
public class DxfExportSettings : ExportSettings
{
    /// <summary>
    /// Include layer for route centerline.
    /// </summary>
    public bool IncludeRoute { get; set; } = true;

    /// <summary>
    /// Include layer for survey points.
    /// </summary>
    public bool IncludePoints { get; set; } = true;

    /// <summary>
    /// Include KP labels.
    /// </summary>
    public bool IncludeKpLabels { get; set; } = true;

    /// <summary>
    /// Include title block.
    /// </summary>
    public bool IncludeTitleBlock { get; set; } = true;

    /// <summary>
    /// Include 3D track with depth.
    /// </summary>
    public bool Include3DTrack { get; set; } = false;

    /// <summary>
    /// Depth exaggeration factor for 3D.
    /// </summary>
    public double DepthExaggeration { get; set; } = 10.0;

    /// <summary>
    /// KP label interval in kilometers.
    /// </summary>
    public double KpLabelInterval { get; set; } = 1.0;

    /// <summary>
    /// Text height for labels.
    /// </summary>
    public double TextHeight { get; set; } = 10.0;

    /// <summary>
    /// Point marker size.
    /// </summary>
    public double PointMarkerSize { get; set; } = 5.0;

    /// <summary>
    /// Custom layer colors (layer name -> ACI color index).
    /// </summary>
    public Dictionary<string, int> LayerColors { get; set; } = new();
}

/// <summary>
/// Settings for CSV export.
/// </summary>
public class CsvExportSettings : ExportSettings
{
    /// <summary>
    /// Field delimiter character.
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Include column headers.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Quote all fields.
    /// </summary>
    public bool QuoteAllFields { get; set; } = false;

    /// <summary>
    /// Text encoding.
    /// </summary>
    public string Encoding { get; set; } = "UTF-8";

    /// <summary>
    /// Line ending style.
    /// </summary>
    public LineEnding LineEnding { get; set; } = LineEnding.CrLf;

    /// <summary>
    /// Date/time format string.
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Number decimal places.
    /// </summary>
    public int DecimalPlaces { get; set; } = 6;

    /// <summary>
    /// Columns to exclude from export.
    /// </summary>
    public HashSet<string> ExcludedColumns { get; set; } = new();
}

/// <summary>
/// Settings for JSON export.
/// </summary>
public class JsonExportSettings : ExportSettings
{
    /// <summary>
    /// Indent the JSON output.
    /// </summary>
    public bool Indent { get; set; } = true;

    /// <summary>
    /// Ignore null values.
    /// </summary>
    public bool IgnoreNullValues { get; set; } = false;

    /// <summary>
    /// Use camelCase property names.
    /// </summary>
    public bool CamelCasePropertyNames { get; set; } = false;

    /// <summary>
    /// Include type information.
    /// </summary>
    public bool IncludeTypeInfo { get; set; } = false;
}

/// <summary>
/// Page size enumeration.
/// </summary>
public enum PageSize
{
    A4,
    A3,
    Letter,
    Legal,
    Tabloid
}

/// <summary>
/// Page orientation.
/// </summary>
public enum PageOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Line ending style.
/// </summary>
public enum LineEnding
{
    /// <summary>
    /// Carriage return + line feed (Windows).
    /// </summary>
    CrLf,

    /// <summary>
    /// Line feed only (Unix/Linux/Mac).
    /// </summary>
    Lf,

    /// <summary>
    /// Carriage return only (legacy Mac).
    /// </summary>
    Cr
}

/// <summary>
/// Page margins configuration.
/// </summary>
public class PageMargins
{
    public float Top { get; set; } = 50f;
    public float Bottom { get; set; } = 50f;
    public float Left { get; set; } = 50f;
    public float Right { get; set; } = 50f;
}

/// <summary>
/// Generic geometry data for DXF export.
/// </summary>
public class GeometryData
{
    /// <summary>
    /// Geometry type.
    /// </summary>
    public GeometryType Type { get; set; }

    /// <summary>
    /// X coordinate (or center X for circles/arcs).
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate (or center Y for circles/arcs).
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z coordinate (optional).
    /// </summary>
    public double? Z { get; set; }

    /// <summary>
    /// End X coordinate (for lines).
    /// </summary>
    public double? EndX { get; set; }

    /// <summary>
    /// End Y coordinate (for lines).
    /// </summary>
    public double? EndY { get; set; }

    /// <summary>
    /// End Z coordinate (for lines).
    /// </summary>
    public double? EndZ { get; set; }

    /// <summary>
    /// Radius (for circles/arcs).
    /// </summary>
    public double? Radius { get; set; }

    /// <summary>
    /// Start angle in degrees (for arcs).
    /// </summary>
    public double? StartAngle { get; set; }

    /// <summary>
    /// End angle in degrees (for arcs).
    /// </summary>
    public double? EndAngle { get; set; }

    /// <summary>
    /// Text content (for text entities).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Text height.
    /// </summary>
    public double? TextHeight { get; set; }

    /// <summary>
    /// Layer name.
    /// </summary>
    public string Layer { get; set; } = "0";

    /// <summary>
    /// Color index (ACI).
    /// </summary>
    public int? ColorIndex { get; set; }

    /// <summary>
    /// Additional points for polylines.
    /// </summary>
    public List<(double X, double Y, double? Z)> Points { get; set; } = new();

    /// <summary>
    /// Additional attributes/properties.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// Geometry type enumeration.
/// </summary>
public enum GeometryType
{
    Point,
    Line,
    Polyline,
    Polyline3D,
    Circle,
    Arc,
    Text,
    Block
}
