using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using FathomOS.Core.Interfaces;

namespace FathomOS.Core.Services;

/// <summary>
/// Unified export service implementation providing consistent export functionality
/// across all supported formats (Excel, PDF, DXF, CSV, JSON).
/// </summary>
public class ExportService : IExportService
{
    private readonly IExcelExportService _excelExportService;

    public ExportService(IExcelExportService excelExportService)
    {
        _excelExportService = excelExportService;

        // Ensure QuestPDF license is set
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    /// <inheritdoc/>
    public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportSettings options)
    {
        return await Task.Run(() =>
        {
            var dataList = data.ToList();
            if (!dataList.Any())
            {
                return Array.Empty<byte>();
            }

            // For non-class types, wrap in a simple class
            if (typeof(T).IsValueType || typeof(T) == typeof(string))
            {
                var wrapped = dataList.Select((item, idx) => new { Index = idx, Value = item }).ToList();
                return _excelExportService.ExportToBytes(wrapped, options.SheetName);
            }

            // Use reflection to call ExportToBytes for class types
            var method = typeof(IExcelExportService).GetMethod(nameof(IExcelExportService.ExportToBytes));
            if (method != null)
            {
                var genericMethod = method.MakeGenericMethod(typeof(T));
                return (byte[])(genericMethod.Invoke(_excelExportService, new object[] { dataList, options.SheetName }) ?? Array.Empty<byte>());
            }

            return Array.Empty<byte>();
        });
    }

    /// <inheritdoc/>
    public Task<byte[]> ExportToPdfAsync(object data, PdfExportSettings template)
    {
        return Task.Run(() =>
        {
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Set page size
                    var pageSize = GetQuestPdfPageSize(template.PageSize);
                    if (template.Orientation == PageOrientation.Landscape)
                    {
                        page.Size(pageSize.Landscape());
                    }
                    else
                    {
                        page.Size(pageSize);
                    }

                    page.Margin(template.Margins.Left);
                    page.DefaultTextStyle(x => x.FontSize(template.FontSize));

                    // Header
                    if (template.IncludeHeader)
                    {
                        page.Header()
                            .Height(50)
                            .Background(Colors.Grey.Lighten3)
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(template.HeaderText ?? template.Title ?? "Export Report")
                            .Bold()
                            .FontSize(14);
                    }

                    // Content
                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            if (!string.IsNullOrEmpty(template.Title))
                            {
                                column.Item().Text(template.Title).Bold().FontSize(16);
                                column.Item().PaddingBottom(10);
                            }

                            // Serialize data to display
                            var jsonOptions = new JsonSerializerOptions
                            {
                                WriteIndented = true,
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            };

                            var content = data switch
                            {
                                string s => s,
                                _ => JsonSerializer.Serialize(data, jsonOptions)
                            };

                            column.Item().Text(content).FontSize(template.FontSize);
                        });

                    // Footer
                    if (template.IncludeFooter)
                    {
                        page.Footer()
                            .Height(30)
                            .AlignCenter()
                            .Column(col =>
                            {
                                if (template.IncludePageNumbers)
                                {
                                    col.Item().AlignCenter().Text(text =>
                                    {
                                        text.Span("Page ");
                                        text.CurrentPageNumber();
                                        text.Span(" of ");
                                        text.TotalPages();
                                    });
                                }

                                if (!string.IsNullOrEmpty(template.FooterText))
                                {
                                    col.Item().AlignCenter().Text(template.FooterText).FontSize(8);
                                }
                            });
                    }
                });
            }).GeneratePdf(stream);

            return stream.ToArray();
        });
    }

    /// <inheritdoc/>
    public Task<byte[]> ExportToDxfAsync(IEnumerable<GeometryData> data, DxfExportSettings options)
    {
        return Task.Run(() =>
        {
            // Create a minimal DXF file with geometry data
            var sb = new StringBuilder();

            // DXF header section
            sb.AppendLine("0");
            sb.AppendLine("SECTION");
            sb.AppendLine("2");
            sb.AppendLine("HEADER");
            sb.AppendLine("0");
            sb.AppendLine("ENDSEC");

            // Entities section
            sb.AppendLine("0");
            sb.AppendLine("SECTION");
            sb.AppendLine("2");
            sb.AppendLine("ENTITIES");

            foreach (var geom in data)
            {
                switch (geom.Type)
                {
                    case GeometryType.Point:
                        sb.AppendLine("0");
                        sb.AppendLine("POINT");
                        sb.AppendLine("8");
                        sb.AppendLine(geom.Layer);
                        sb.AppendLine("10");
                        sb.AppendLine(geom.X.ToString("F6"));
                        sb.AppendLine("20");
                        sb.AppendLine(geom.Y.ToString("F6"));
                        if (geom.Z.HasValue)
                        {
                            sb.AppendLine("30");
                            sb.AppendLine(geom.Z.Value.ToString("F6"));
                        }
                        break;

                    case GeometryType.Line:
                        if (geom.EndX.HasValue && geom.EndY.HasValue)
                        {
                            sb.AppendLine("0");
                            sb.AppendLine("LINE");
                            sb.AppendLine("8");
                            sb.AppendLine(geom.Layer);
                            sb.AppendLine("10");
                            sb.AppendLine(geom.X.ToString("F6"));
                            sb.AppendLine("20");
                            sb.AppendLine(geom.Y.ToString("F6"));
                            sb.AppendLine("11");
                            sb.AppendLine(geom.EndX.Value.ToString("F6"));
                            sb.AppendLine("21");
                            sb.AppendLine(geom.EndY.Value.ToString("F6"));
                        }
                        break;

                    case GeometryType.Circle:
                        if (geom.Radius.HasValue)
                        {
                            sb.AppendLine("0");
                            sb.AppendLine("CIRCLE");
                            sb.AppendLine("8");
                            sb.AppendLine(geom.Layer);
                            sb.AppendLine("10");
                            sb.AppendLine(geom.X.ToString("F6"));
                            sb.AppendLine("20");
                            sb.AppendLine(geom.Y.ToString("F6"));
                            sb.AppendLine("40");
                            sb.AppendLine(geom.Radius.Value.ToString("F6"));
                        }
                        break;

                    case GeometryType.Text:
                        if (!string.IsNullOrEmpty(geom.Text))
                        {
                            sb.AppendLine("0");
                            sb.AppendLine("TEXT");
                            sb.AppendLine("8");
                            sb.AppendLine(geom.Layer);
                            sb.AppendLine("10");
                            sb.AppendLine(geom.X.ToString("F6"));
                            sb.AppendLine("20");
                            sb.AppendLine(geom.Y.ToString("F6"));
                            sb.AppendLine("40");
                            sb.AppendLine((geom.TextHeight ?? options.TextHeight).ToString("F6"));
                            sb.AppendLine("1");
                            sb.AppendLine(geom.Text);
                        }
                        break;

                    case GeometryType.Polyline:
                    case GeometryType.Polyline3D:
                        if (geom.Points.Any())
                        {
                            sb.AppendLine("0");
                            sb.AppendLine("LWPOLYLINE");
                            sb.AppendLine("8");
                            sb.AppendLine(geom.Layer);
                            sb.AppendLine("90");
                            sb.AppendLine(geom.Points.Count.ToString());
                            sb.AppendLine("70");
                            sb.AppendLine("0"); // Open polyline

                            foreach (var pt in geom.Points)
                            {
                                sb.AppendLine("10");
                                sb.AppendLine(pt.X.ToString("F6"));
                                sb.AppendLine("20");
                                sb.AppendLine(pt.Y.ToString("F6"));
                            }
                        }
                        break;
                }
            }

            sb.AppendLine("0");
            sb.AppendLine("ENDSEC");
            sb.AppendLine("0");
            sb.AppendLine("EOF");

            return Encoding.ASCII.GetBytes(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CsvExportSettings options)
    {
        return Task.Run(() =>
        {
            var sb = new StringBuilder();
            var dataList = data.ToList();

            if (!dataList.Any())
            {
                return Array.Empty<byte>();
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !options.ExcludedColumns.Contains(p.Name))
                .ToList();

            // Headers
            if (options.IncludeHeaders)
            {
                var headers = properties.Select(p => FormatCsvField(p.Name, options));
                sb.AppendLine(string.Join(options.Delimiter, headers));
            }

            // Data rows
            foreach (var item in dataList)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item);
                    return FormatCsvValue(value, options);
                });
                sb.AppendLine(string.Join(options.Delimiter, values));
            }

            var encoding = GetEncoding(options.Encoding);
            return encoding.GetBytes(sb.ToString());
        });
    }

    /// <inheritdoc/>
    public Task<byte[]> ExportToJsonAsync<T>(T data, JsonExportSettings? options = null)
    {
        return Task.Run(() =>
        {
            options ??= new JsonExportSettings();

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = options.Indent,
                DefaultIgnoreCondition = options.IgnoreNullValues
                    ? System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    : System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                PropertyNamingPolicy = options.CamelCasePropertyNames
                    ? JsonNamingPolicy.CamelCase
                    : null
            };

            var json = JsonSerializer.Serialize(data, jsonOptions);
            return Encoding.UTF8.GetBytes(json);
        });
    }

    /// <inheritdoc/>
    public async Task ExportToFileAsync<T>(T data, string filePath, ExportFormat format, ExportSettings? settings = null)
    {
        byte[] bytes;

        switch (format)
        {
            case ExportFormat.Excel:
                var excelSettings = settings as ExcelExportSettings ?? new ExcelExportSettings();
                if (data is IEnumerable<object> excelData)
                {
                    bytes = await ExportToExcelAsync(excelData, excelSettings);
                }
                else
                {
                    bytes = await ExportToExcelAsync(new[] { data! }, excelSettings);
                }
                break;

            case ExportFormat.Pdf:
                var pdfSettings = settings as PdfExportSettings ?? new PdfExportSettings();
                bytes = await ExportToPdfAsync(data!, pdfSettings);
                break;

            case ExportFormat.Dxf:
                var dxfSettings = settings as DxfExportSettings ?? new DxfExportSettings();
                if (data is IEnumerable<GeometryData> geometryData)
                {
                    bytes = await ExportToDxfAsync(geometryData, dxfSettings);
                }
                else
                {
                    throw new ArgumentException("DXF export requires IEnumerable<GeometryData>");
                }
                break;

            case ExportFormat.Csv:
                var csvSettings = settings as CsvExportSettings ?? new CsvExportSettings();
                if (data is IEnumerable<object> csvData)
                {
                    bytes = await ExportToCsvAsync(csvData, csvSettings);
                }
                else
                {
                    bytes = await ExportToCsvAsync(new[] { data! }, csvSettings);
                }
                break;

            case ExportFormat.Json:
                var jsonSettings = settings as JsonExportSettings ?? new JsonExportSettings();
                bytes = await ExportToJsonAsync(data, jsonSettings);
                break;

            case ExportFormat.Text:
                var text = data?.ToString() ?? string.Empty;
                bytes = Encoding.UTF8.GetBytes(text);
                break;

            case ExportFormat.Xml:
                var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                using (var ms = new MemoryStream())
                {
                    xmlSerializer.Serialize(ms, data);
                    bytes = ms.ToArray();
                }
                break;

            default:
                throw new NotSupportedException($"Export format {format} is not supported.");
        }

        await File.WriteAllBytesAsync(filePath, bytes);
    }

    /// <inheritdoc/>
    public IEnumerable<ExportFormat> GetSupportedFormats<T>()
    {
        var formats = new List<ExportFormat>
        {
            ExportFormat.Json,
            ExportFormat.Csv,
            ExportFormat.Excel,
            ExportFormat.Text,
            ExportFormat.Pdf
        };

        // Add DXF support for geometry data
        if (typeof(T) == typeof(GeometryData) ||
            typeof(IEnumerable<GeometryData>).IsAssignableFrom(typeof(T)))
        {
            formats.Add(ExportFormat.Dxf);
        }

        // Add XML support for types with XmlRoot attribute
        if (typeof(T).GetCustomAttribute<System.Xml.Serialization.XmlRootAttribute>() != null)
        {
            formats.Add(ExportFormat.Xml);
        }

        return formats;
    }

    #region Private Helpers

    private static QuestPDF.Helpers.PageSize GetQuestPdfPageSize(Interfaces.PageSize pageSize)
    {
        return pageSize switch
        {
            Interfaces.PageSize.A4 => PageSizes.A4,
            Interfaces.PageSize.A3 => PageSizes.A3,
            Interfaces.PageSize.Letter => PageSizes.Letter,
            Interfaces.PageSize.Legal => PageSizes.Legal,
            Interfaces.PageSize.Tabloid => PageSizes.A3, // Use A3 as fallback for Tabloid
            _ => PageSizes.A4
        };
    }

    private static string FormatCsvField(string value, CsvExportSettings options)
    {
        if (options.QuoteAllFields || value.Contains(options.Delimiter) ||
            value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static string FormatCsvValue(object? value, CsvExportSettings options)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var stringValue = value switch
        {
            DateTime dt => dt.ToString(options.DateTimeFormat),
            DateTimeOffset dto => dto.ToString(options.DateTimeFormat),
            double d => d.ToString($"F{options.DecimalPlaces}"),
            float f => f.ToString($"F{options.DecimalPlaces}"),
            decimal m => m.ToString($"F{options.DecimalPlaces}"),
            _ => value.ToString() ?? string.Empty
        };

        return FormatCsvField(stringValue, options);
    }

    private static Encoding GetEncoding(string encodingName)
    {
        return encodingName.ToUpperInvariant() switch
        {
            "UTF-8" or "UTF8" => Encoding.UTF8,
            "UTF-16" or "UNICODE" => Encoding.Unicode,
            "ASCII" => Encoding.ASCII,
            "UTF-32" => Encoding.UTF32,
            _ => Encoding.UTF8
        };
    }

    #endregion
}
