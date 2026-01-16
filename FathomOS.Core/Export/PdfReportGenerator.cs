using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Core.Models;
using SkiaSharp;

namespace FathomOS.Core.Export;

/// <summary>
/// Generates PDF reports for survey data
/// </summary>
public class PdfReportGenerator
{
    private readonly PdfReportOptions _options;
    private ReportTemplate? _template;
    private string? _logoPath;
    private Project? _currentProject;

    public PdfReportGenerator(PdfReportOptions? options = null)
    {
        _options = options ?? new PdfReportOptions();
        
        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    /// <summary>
    /// Set report template for branding
    /// </summary>
    public void SetTemplate(ReportTemplate template, string? logoPath = null)
    {
        _template = template;
        _logoPath = logoPath;
    }

    /// <summary>
    /// Generate a PDF report for the survey data
    /// </summary>
    public void Generate(string filePath, IList<SurveyPoint> points, Project project, RouteData? route = null, 
                         ProcessingTrackerData? tracker = null)
    {
        _currentProject = project;
        
        Document.Create(container =>
        {
            // Main report page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, project));
                page.Content().Element(c => ComposeContent(c, points, project, route));
                page.Footer().Element(c => ComposeFooter(c));
            });
            
            // Depth Profile Chart page
            if (_options.IncludeDepthChart && points.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(c => ComposeHeader(c, project, "DEPTH PROFILE"));
                    page.Content().Element(c => ComposeDepthChart(c, points));
                    page.Footer().Element(c => ComposeFooter(c));
                });
            }
            
            // Full Data Table pages (if enabled)
            if (_options.IncludeFullDataTable && points.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Element(c => ComposeHeader(c, project, "FULL SURVEY LISTING"));
                    page.Content().Element(c => ComposeFullDataTable(c, points));
                    page.Footer().Element(c => ComposeFooter(c));
                });
            }
            
            // Crib Sheet / Process Log page
            if (_options.IncludeCribSheet)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Element(c => ComposeHeader(c, project, "PROCESSING LOG"));
                    page.Content().Element(c => ComposeCribSheet(c, project, tracker));
                    page.Footer().Element(c => ComposeFooter(c));
                });
            }
        }).GeneratePdf(filePath);
    }

    private void ComposeHeader(IContainer container, Project project, string? subtitle = null)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Logo on left
                if (_template?.Header.ShowLogo == true && !string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                {
                    try
                    {
                        row.ConstantItem(_template.Header.LogoWidth).AlignLeft().Image(_logoPath);
                    }
                    catch
                    {
                        // Logo failed, show company name instead
                        row.ConstantItem(100).AlignLeft().Text(_template?.Company.Name ?? "").FontSize(10);
                    }
                }
                else if (_template?.Header.ShowCompanyName == true)
                {
                    row.ConstantItem(150).AlignLeft().Column(c =>
                    {
                        c.Item().Text(_template?.Company.Name ?? "Fathom OS").FontSize(10).Bold();
                    });
                }
                
                // Title in center
                row.RelativeItem().AlignCenter().Column(column =>
                {
                    string title = subtitle ?? _template?.Header.Title ?? "SURVEY LISTING REPORT";
                    column.Item().Text(title)
                        .Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    
                    column.Item().Text($"Project: {project.ProjectName}")
                        .FontSize(11).SemiBold();
                    
                    column.Item().Text($"Client: {project.ClientName}")
                        .FontSize(10);
                });

                // Date/time on right
                row.ConstantItem(100).AlignRight().Column(column =>
                {
                    column.Item().Text($"Date: {DateTime.Now:yyyy-MM-dd}").FontSize(9);
                    column.Item().Text($"Time: {DateTime.Now:HH:mm:ss}").FontSize(9);
                    if (project.SurveyDate.HasValue)
                    {
                        column.Item().Text($"Survey: {project.SurveyDate:yyyy-MM-dd}").FontSize(9);
                    }
                });
            });

            col.Item().PaddingTop(10).LineHorizontal(2).LineColor(Colors.Blue.Darken2);
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(5).Row(row =>
            {
                // Left footer
                string leftText = ReportTemplate.ReplacePlaceholders(
                    _template?.Footer.LeftText ?? "{ProjectName} | {ClientName}", 
                    _currentProject ?? new Project());
                row.RelativeItem().AlignLeft().Text(leftText).FontSize(8);
                
                // Center footer (page numbers handled by QuestPDF)
                row.RelativeItem().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" of ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
                
                // Right footer
                string rightText = ReportTemplate.ReplacePlaceholders(
                    _template?.Footer.RightText ?? "Generated: {GeneratedDate}",
                    _currentProject ?? new Project());
                row.RelativeItem().AlignRight().Text(rightText).FontSize(8);
            });
        });
    }
    
    /// <summary>
    /// Compose depth profile chart
    /// </summary>
    private void ComposeDepthChart(IContainer container, IList<SurveyPoint> points)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(10).Text("Depth Profile Along Route").Bold().FontSize(14);
            
            // Get data for chart
            var chartPoints = points
                .Where(p => p.Kp.HasValue && p.CalculatedZ.HasValue)
                .OrderBy(p => p.Kp)
                .ToList();
            
            if (chartPoints.Count == 0)
            {
                column.Item().Text("No depth data available for chart").FontColor(Colors.Red.Medium);
                return;
            }
            
            double minKp = chartPoints.Min(p => p.Kp!.Value);
            double maxKp = chartPoints.Max(p => p.Kp!.Value);
            double minZ = chartPoints.Min(p => p.CalculatedZ!.Value);
            double maxZ = chartPoints.Max(p => p.CalculatedZ!.Value);
            
            // Add some padding to Z range
            double zRange = maxZ - minZ;
            if (zRange < 1) zRange = 1;
            minZ -= zRange * 0.1;
            maxZ += zRange * 0.1;
            
            double kpRange = maxKp - minKp;
            if (kpRange < 0.001) kpRange = 0.001;
            
            // Chart dimensions
            float chartWidth = 700;
            float chartHeight = 300;
            float marginLeft = 60;
            float marginBottom = 40;
            float marginTop = 20;
            float marginRight = 20;
            
            float plotWidth = chartWidth - marginLeft - marginRight;
            float plotHeight = chartHeight - marginTop - marginBottom;
            
            column.Item().Height(chartHeight).Canvas((object canvasObj, Size size) =>
            {
                var canvas = (SKCanvas)canvasObj;
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    StrokeWidth = 1
                };
                
                // Draw axes
                paint.Color = SKColors.Black;
                paint.Style = SKPaintStyle.Stroke;
                
                // Y-axis
                canvas.DrawLine(marginLeft, marginTop, marginLeft, chartHeight - marginBottom, paint);
                // X-axis
                canvas.DrawLine(marginLeft, chartHeight - marginBottom, chartWidth - marginRight, chartHeight - marginBottom, paint);
                
                // Draw grid lines and labels
                paint.StrokeWidth = 0.5f;
                paint.Color = SKColors.LightGray;
                
                // Horizontal grid lines (Z values)
                int zSteps = 5;
                for (int i = 0; i <= zSteps; i++)
                {
                    float y = marginTop + (plotHeight * i / zSteps);
                    canvas.DrawLine(marginLeft, y, chartWidth - marginRight, y, paint);
                    
                    // Z label
                    double zValue = maxZ - (maxZ - minZ) * i / zSteps;
                    paint.Color = SKColors.Black;
                    paint.Style = SKPaintStyle.Fill;
                    paint.TextSize = 10;
                    canvas.DrawText($"{zValue:F1}", 5, y + 4, paint);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = SKColors.LightGray;
                }
                
                // Vertical grid lines (KP values)
                int kpSteps = 10;
                for (int i = 0; i <= kpSteps; i++)
                {
                    float x = marginLeft + (plotWidth * i / kpSteps);
                    canvas.DrawLine(x, marginTop, x, chartHeight - marginBottom, paint);
                    
                    // KP label
                    double kpValue = minKp + (maxKp - minKp) * i / kpSteps;
                    paint.Color = SKColors.Black;
                    paint.Style = SKPaintStyle.Fill;
                    paint.TextSize = 10;
                    canvas.DrawText($"{kpValue:F3}", x - 15, chartHeight - marginBottom + 15, paint);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = SKColors.LightGray;
                }
                
                // Axis labels
                paint.Color = SKColors.Black;
                paint.Style = SKPaintStyle.Fill;
                paint.TextSize = 12;
                
                // Y-axis label
                canvas.Save();
                canvas.Translate(15, chartHeight / 2);
                canvas.RotateDegrees(-90);
                canvas.DrawText("Depth (Z)", 0, 0, paint);
                canvas.Restore();
                
                // X-axis label
                canvas.DrawText("KP", chartWidth / 2 - 10, chartHeight - 5, paint);
                
                // Draw the depth profile line
                paint.Color = SKColors.Blue;
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 2;
                
                var path = new SKPath();
                bool first = true;
                
                // Sample points to avoid too many in the chart
                int step = Math.Max(1, chartPoints.Count / 500);
                
                for (int i = 0; i < chartPoints.Count; i += step)
                {
                    var pt = chartPoints[i];
                    float x = marginLeft + (float)((pt.Kp!.Value - minKp) / kpRange) * plotWidth;
                    float y = marginTop + (float)((maxZ - pt.CalculatedZ!.Value) / (maxZ - minZ)) * plotHeight;
                    
                    if (first)
                    {
                        path.MoveTo(x, y);
                        first = false;
                    }
                    else
                    {
                        path.LineTo(x, y);
                    }
                }
                
                canvas.DrawPath(path, paint);
                
                // Draw legend
                paint.Style = SKPaintStyle.Fill;
                paint.TextSize = 10;
                paint.Color = SKColors.Black;
                canvas.DrawText($"Points: {chartPoints.Count:N0}  |  KP Range: {minKp:F4} - {maxKp:F4}  |  Z Range: {minZ:F2} - {maxZ:F2}", 
                    marginLeft, chartHeight - 5, paint);
            });
            
            // Statistics table below chart
            column.Item().PaddingTop(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                
                table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Min Z").Bold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Max Z").Bold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Avg Z").Bold();
                table.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Std Dev").Bold();
                
                var zValues = chartPoints.Select(p => p.CalculatedZ!.Value).ToList();
                double avg = zValues.Average();
                double stdDev = Math.Sqrt(zValues.Sum(z => Math.Pow(z - avg, 2)) / zValues.Count);
                
                table.Cell().Padding(5).Text($"{zValues.Min():F3}");
                table.Cell().Padding(5).Text($"{zValues.Max():F3}");
                table.Cell().Padding(5).Text($"{avg:F3}");
                table.Cell().Padding(5).Text($"{stdDev:F3}");
            });
        });
    }
    
    /// <summary>
    /// Compose full data table (all points)
    /// </summary>
    private void ComposeFullDataTable(IContainer container, IList<SurveyPoint> points)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(5).Text($"Complete Survey Listing - {points.Count:N0} Points").Bold().FontSize(12);
            
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);   // #
                    columns.ConstantColumn(70);   // KP
                    columns.ConstantColumn(50);   // DCC
                    columns.ConstantColumn(90);   // X
                    columns.ConstantColumn(90);   // Y
                    columns.ConstantColumn(60);   // Z
                    columns.ConstantColumn(90);   // DateTime
                    columns.ConstantColumn(50);   // Raw Depth
                    columns.ConstantColumn(50);   // Altitude
                    columns.ConstantColumn(50);   // Tide
                });
                
                // Header
                table.Header(header =>
                {
                    void HeaderCell(string text) => 
                        header.Cell().Background(Colors.Blue.Darken2).Padding(3)
                            .Text(text).FontSize(7).Bold().FontColor(Colors.White);
                    
                    HeaderCell("#");
                    HeaderCell("KP");
                    HeaderCell("DCC");
                    HeaderCell("X (Easting)");
                    HeaderCell("Y (Northing)");
                    HeaderCell("Z");
                    HeaderCell("DateTime");
                    HeaderCell("Depth");
                    HeaderCell("Alt");
                    HeaderCell("Tide");
                });
                
                // Data rows
                int rowNum = 0;
                foreach (var point in points)
                {
                    rowNum++;
                    var bgColor = rowNum % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                    
                    void DataCell(string text) =>
                        table.Cell().Background(bgColor).Padding(2).Text(text).FontSize(7);
                    
                    DataCell(rowNum.ToString());
                    DataCell(point.Kp?.ToString("F6") ?? "-");
                    DataCell(point.Dcc?.ToString("F3") ?? "-");
                    DataCell(point.X.ToString("F2"));
                    DataCell(point.Y.ToString("F2"));
                    DataCell(point.CalculatedZ?.ToString("F2") ?? "-");
                    DataCell(point.DateTime.ToString("HH:mm:ss"));
                    DataCell(point.Depth?.ToString("F2") ?? "-");
                    DataCell(point.Altitude?.ToString("F2") ?? "-");
                    DataCell(point.TideCorrection?.ToString("F3") ?? "-");
                }
            });
        });
    }

    private void ComposeContent(IContainer container, IList<SurveyPoint> points, Project project, RouteData? route)
    {
        container.Column(column =>
        {
            // Project Information Section
            column.Item().Element(c => ComposeProjectInfo(c, project));
            column.Item().PaddingVertical(10);

            // Survey Statistics Section
            column.Item().Element(c => ComposeSurveyStatistics(c, points, route));
            column.Item().PaddingVertical(10);

            // Data Summary Table
            if (_options.IncludeDataSummary)
            {
                column.Item().Element(c => ComposeDataSummary(c, points));
                column.Item().PaddingVertical(10);
            }

            // Survey Listing Table (sample)
            if (_options.IncludeSampleData && points.Count > 0)
            {
                column.Item().Element(c => ComposeSampleListing(c, points));
            }
        });
    }

    private void ComposeProjectInfo(IContainer container, Project project)
    {
        container.Column(column =>
        {
            column.Item().Text("PROJECT INFORMATION").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                });

                void AddRow(string label1, string value1, string label2, string value2)
                {
                    table.Cell().Text(label1).SemiBold();
                    table.Cell().Text(value1);
                    table.Cell().Text(label2).SemiBold();
                    table.Cell().Text(value2);
                }

                AddRow("Project:", project.ProjectName, "Client:", project.ClientName);
                AddRow("Vessel:", project.VesselName, "Processor:", project.ProcessorName);
                AddRow("Survey Type:", project.SurveyType.ToString(), "Coordinate System:", project.CoordinateSystem);
                AddRow("Coordinate Unit:", project.CoordinateUnit.GetDisplayName(), "KP Unit:", project.KpUnit.GetDisplayName());
            });
        });
    }

    private void ComposeSurveyStatistics(IContainer container, IList<SurveyPoint> points, RouteData? route)
    {
        container.Column(column =>
        {
            column.Item().Text("SURVEY STATISTICS").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5);

            column.Item().Row(row =>
            {
                // Left column - Survey Data Stats
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(statsCol =>
                {
                    statsCol.Item().Text("Survey Data").Bold();
                    statsCol.Item().PaddingTop(5);

                    statsCol.Item().Text($"Total Points: {points.Count:N0}");
                    
                    if (points.Count > 0)
                    {
                        var startTime = points.Min(p => p.DateTime);
                        var endTime = points.Max(p => p.DateTime);
                        var duration = endTime - startTime;

                        statsCol.Item().Text($"Start Time: {startTime:yyyy-MM-dd HH:mm:ss}");
                        statsCol.Item().Text($"End Time: {endTime:yyyy-MM-dd HH:mm:ss}");
                        statsCol.Item().Text($"Duration: {duration:hh\\:mm\\:ss}");

                        // Coordinate range
                        statsCol.Item().PaddingTop(5);
                        statsCol.Item().Text($"X Range: {points.Min(p => p.X):F2} to {points.Max(p => p.X):F2}");
                        statsCol.Item().Text($"Y Range: {points.Min(p => p.Y):F2} to {points.Max(p => p.Y):F2}");

                        // Depth range
                        var zValues = points.Where(p => p.CalculatedZ.HasValue).Select(p => p.CalculatedZ!.Value).ToList();
                        if (zValues.Count > 0)
                        {
                            statsCol.Item().PaddingTop(5);
                            statsCol.Item().Text($"Z (Seabed) Range: {zValues.Min():F2} to {zValues.Max():F2}");
                            statsCol.Item().Text($"Average Z: {zValues.Average():F2}");
                        }
                    }
                });

                row.ConstantItem(20);

                // Right column - KP/DCC Stats
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(kpCol =>
                {
                    kpCol.Item().Text("KP/DCC Statistics").Bold();
                    kpCol.Item().PaddingTop(5);

                    var kpValues = points.Where(p => p.Kp.HasValue).Select(p => p.Kp!.Value).ToList();
                    if (kpValues.Count > 0)
                    {
                        kpCol.Item().Text($"Start KP: {kpValues.Min():F6}");
                        kpCol.Item().Text($"End KP: {kpValues.Max():F6}");
                        kpCol.Item().Text($"KP Coverage: {kpValues.Max() - kpValues.Min():F6}");

                        var dccValues = points.Where(p => p.Dcc.HasValue).Select(p => Math.Abs(p.Dcc!.Value)).ToList();
                        if (dccValues.Count > 0)
                        {
                            kpCol.Item().PaddingTop(5);
                            kpCol.Item().Text($"Min DCC: {dccValues.Min():F3}");
                            kpCol.Item().Text($"Max DCC: {dccValues.Max():F3}");
                            kpCol.Item().Text($"Average DCC: {dccValues.Average():F3}");
                        }
                    }
                    else
                    {
                        kpCol.Item().Text("No KP data available");
                    }

                    // Route info
                    if (route != null)
                    {
                        kpCol.Item().PaddingTop(10);
                        kpCol.Item().Text("Route Information").Bold();
                        kpCol.Item().Text($"Route: {route.Name}");
                        kpCol.Item().Text($"Route KP: {route.StartKp:F6} to {route.EndKp:F6}");
                        kpCol.Item().Text($"Segments: {route.Segments.Count}");
                    }
                });
            });
        });
    }

    private void ComposeDataSummary(IContainer container, IList<SurveyPoint> points)
    {
        container.Column(column =>
        {
            column.Item().Text("DATA QUALITY SUMMARY").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5);

            int totalPoints = points.Count;
            int pointsWithKp = points.Count(p => p.Kp.HasValue);
            int pointsWithZ = points.Count(p => p.CalculatedZ.HasValue);
            int pointsWithDepth = points.Count(p => p.Depth.HasValue);
            int pointsWithAltitude = points.Count(p => p.Altitude.HasValue);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(80);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Data Field").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(5).AlignRight().Text("Count").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(5).AlignRight().Text("Percentage").Bold();
                });

                void AddDataRow(string field, int count)
                {
                    double percentage = totalPoints > 0 ? (double)count / totalPoints * 100 : 0;
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(field);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{count:N0}");
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignRight().Text($"{percentage:F1}%");
                }

                AddDataRow("Total Points", totalPoints);
                AddDataRow("Points with KP", pointsWithKp);
                AddDataRow("Points with Calculated Z", pointsWithZ);
                AddDataRow("Points with Raw Depth", pointsWithDepth);
                AddDataRow("Points with Altitude", pointsWithAltitude);
            });
        });
    }

    private void ComposeSampleListing(IContainer container, IList<SurveyPoint> points)
    {
        container.Column(column =>
        {
            int sampleSize = Math.Min(_options.SampleDataRows, points.Count);
            column.Item().Text($"SAMPLE DATA (First {sampleSize} records)").Bold().FontSize(12).FontColor(Colors.Blue.Darken1);
            column.Item().PaddingTop(5);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);   // KP
                    columns.ConstantColumn(50);   // DCC
                    columns.ConstantColumn(90);   // X
                    columns.ConstantColumn(100);  // Y
                    columns.ConstantColumn(60);   // Z
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("KP").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("DCC").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("X").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Y").Bold().FontColor(Colors.White);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(4).Text("Z").Bold().FontColor(Colors.White);
                });

                // Data rows
                foreach (var point in points.Take(sampleSize))
                {
                    var bgColor = points.IndexOf(point) % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                    
                    table.Cell().Background(bgColor).Padding(3).Text(point.Kp?.ToString("F6") ?? "-").FontSize(8);
                    table.Cell().Background(bgColor).Padding(3).Text(point.Dcc?.ToString("F3") ?? "-").FontSize(8);
                    table.Cell().Background(bgColor).Padding(3).Text(point.X.ToString("F2")).FontSize(8);
                    table.Cell().Background(bgColor).Padding(3).Text(point.Y.ToString("F2")).FontSize(8);
                    table.Cell().Background(bgColor).Padding(3).Text(point.CalculatedZ?.ToString("F2") ?? "-").FontSize(8);
                }
            });

            if (points.Count > sampleSize)
            {
                column.Item().PaddingTop(5).Text($"... and {points.Count - sampleSize:N0} more records")
                    .Italic().FontSize(9).FontColor(Colors.Grey.Darken1);
            }
        });
    }
    
    private void ComposeCribSheet(IContainer container, Project project, ProcessingTrackerData? tracker)
    {
        container.Column(column =>
        {
            // Title
            column.Item().Text("CRIB SHEET / PROCESS LOG").Bold().FontSize(16);
            column.Item().PaddingVertical(10);
            
            // Header Table - Project Info
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                    columns.ConstantColumn(80);
                    columns.RelativeColumn();
                });
                
                // Row 1: Project
                table.Cell().Border(1).Padding(5).Text("Project:").Bold();
                table.Cell().ColumnSpan(3).Border(1).Padding(5).Text(project.ProjectName);
                
                // Row 2: Product | Vessel
                table.Cell().Border(1).Padding(5).Text("Product:").Bold();
                table.Cell().Border(1).Padding(5).Text(project.ProductName ?? "-");
                table.Cell().Border(1).Padding(5).Text("Vessel:").Bold();
                table.Cell().Border(1).Padding(5).Text(project.VesselName);
                
                // Row 3: Survey Date | ROV
                table.Cell().Border(1).Padding(5).Text("Survey Date:").Bold();
                table.Cell().Border(1).Padding(5).Text(project.SurveyDate?.ToString("yyyy-MM-dd") ?? "-");
                table.Cell().Border(1).Padding(5).Text("ROV:").Bold();
                table.Cell().Border(1).Padding(5).Text(project.RovName ?? "-");
            });
            
            column.Item().PaddingVertical(10);
            
            // Process Log Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);      // Task
                    columns.RelativeColumn(2);      // Filename
                    columns.ConstantColumn(60);     // Initials
                    columns.RelativeColumn(2);      // Comments
                });
                
                // Header Row
                table.Cell().Background(Colors.Grey.Lighten2).Border(1).Padding(5).Text("").Bold();
                table.Cell().Background(Colors.Grey.Lighten2).Border(1).Padding(5).Text("Filename").Bold();
                table.Cell().Background(Colors.Grey.Lighten2).Border(1).Padding(5).Text("Initials").Bold();
                table.Cell().Background(Colors.Grey.Lighten2).Border(1).Padding(5).Text("Comments").Bold();
                
                // Section: Source Data / Support Files
                AddSectionHeader(table, "Source Data / Support Files");
                AddCribRow(table, "Product / Lay Route", 
                    tracker?.ProductLayRouteFile ?? "", 
                    tracker?.ProductLayRouteLoaded == true ? "✓" : "", "");
                AddCribRow(table, "Tidal Data", 
                    tracker?.TidalDataFile ?? "", 
                    tracker?.TidalDataLoaded == true ? "✓" : "", "");
                AddCribRow(table, "Raw Data files (Start/End)", 
                    CombineFiles(tracker?.RawDataStartFile, tracker?.RawDataEndFile), 
                    tracker?.RawDataFilesLoaded == true ? "✓" : "", "");
                AddCribRow(table, "CAD Background Drawing", 
                    tracker?.CadBackgroundFile ?? "", 
                    tracker?.CadBackgroundLoaded == true ? "✓" : "", "");
                
                // Section: Raw Data and Working Excel Workbook
                AddSectionHeader(table, "Raw Data and Working Excel Workbook");
                AddCribRow(table, "1.Raw Data - Add raw data Cols A:H", "", 
                    tracker?.RawDataAdded == true ? "✓" : "", "");
                AddCribRow(table, "3.Tide - Add tide data – Check time datum", "", 
                    tracker?.TideDataAdded == true ? "✓" : "", "");
                AddCribRow(table, "4.Calculations – Update references Cols A:L", "", 
                    tracker?.CalculationsUpdated == true ? "✓" : "", "");
                AddCribRow(table, "Parameters – Update Offsets", "", 
                    tracker?.ParametersUpdated == true ? "✓" : "", "");
                
                // Section: Process Product Position & Depth
                AddSectionHeader(table, "Process Product Position & Depth");
                AddCribRow(table, "Create Working Track Drawing", "", 
                    tracker?.WorkingTrackCreated == true ? "✓" : "", "");
                AddCribRow(table, "2.Fixes – Add and copy Col E: to CAD (OSNAP OFF)", "", 
                    tracker?.FixesAddedToCad == true ? "✓" : "", "");
                AddCribRow(table, "4. Calculations – Create XYZ script", "", 
                    tracker?.XyzScriptCreated == true ? "✓" : "", "");
                AddCribRow(table, "Add Track XYZ to BricsCAD", "", 
                    tracker?.TrackAddedToBricsCAD == true ? "✓" : "", "");
                AddCribRow(table, "Review Data Coverage", "", 
                    tracker?.DataCoverageReviewed == true ? "✓" : "", "");
                AddCribRow(table, "Create Spline Fit", "", 
                    tracker?.SplineFitCreated == true ? "✓" : "", "");
                AddCribRow(table, "Review/Refine Product Position", "", 
                    tracker?.ProductPositionReviewed == true ? "✓" : "", "");
                AddCribRow(table, "Create Points at 1m Interval", "", 
                    tracker?.PointsAt1mCreated == true ? "✓" : "", "");
                AddCribRow(table, "Extract Points XYZ", "", 
                    tracker?.PointsXyzExtracted == true ? "✓" : "", "");
                
                // Section: 7KPCalc
                AddSectionHeader(table, "7KPCalc");
                AddCribRow(table, "Seabed Depth - Calculate KP/DCC for XYZ.\n(Interpolated to 1m by KP)", "", 
                    tracker?.SeabedDepthKpDccCalculated == true ? "✓" : "", "");
                AddCribRow(table, "ROV Depth – Calculate KP/DCC for XYZ.\n(No interpolation)", "", 
                    tracker?.RovDepthKpDccCalculated == true ? "✓" : "", "");
                
                // Section: Final
                AddSectionHeader(table, "Produce Final Listing");
                AddCribRow(table, "Generate final output files", "", 
                    tracker?.FinalListingProduced == true ? "✓" : "", "");
            });
            
            // Processing timestamp
            if (tracker?.ProcessingStartTime.HasValue == true)
            {
                column.Item().PaddingTop(15).Text($"Processing completed: {tracker.ProcessingEndTime:yyyy-MM-dd HH:mm:ss}")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
            }
        });
    }
    
    private static void AddSectionHeader(TableDescriptor table, string text)
    {
        table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten2).Border(1).Padding(5)
            .Text(text).Bold();
    }
    
    private static void AddCribRow(TableDescriptor table, string task, string filename, string initials, string comments)
    {
        table.Cell().Border(1).Padding(3).Text(task).FontSize(9);
        table.Cell().Border(1).Padding(3).Background(Colors.Grey.Lighten4).Text(filename).FontSize(8);
        table.Cell().Border(1).Padding(3).Background(Colors.Grey.Lighten4).AlignCenter().Text(initials).FontSize(9);
        table.Cell().Border(1).Padding(3).Text(comments).FontSize(8);
    }
    
    private static string CombineFiles(string? start, string? end)
    {
        if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end))
            return "";
        if (string.IsNullOrEmpty(end) || start == end)
            return start ?? "";
        return $"{start} - {end}";
    }
}

/// <summary>
/// Data class for processing tracker information to be included in PDF
/// </summary>
public class ProcessingTrackerData
{
    // Source Data / Support Files
    public bool ProductLayRouteLoaded { get; set; }
    public bool TidalDataLoaded { get; set; }
    public bool RawDataFilesLoaded { get; set; }
    public bool CadBackgroundLoaded { get; set; }
    
    // Filenames
    public string? ProductLayRouteFile { get; set; }
    public string? TidalDataFile { get; set; }
    public string? RawDataStartFile { get; set; }
    public string? RawDataEndFile { get; set; }
    public string? CadBackgroundFile { get; set; }
    
    // Raw Data and Working Excel Workbook
    public bool RawDataAdded { get; set; }
    public bool TideDataAdded { get; set; }
    public bool CalculationsUpdated { get; set; }
    public bool ParametersUpdated { get; set; }
    
    // Process Product Position & Depth
    public bool WorkingTrackCreated { get; set; }
    public bool FixesAddedToCad { get; set; }
    public bool XyzScriptCreated { get; set; }
    public bool TrackAddedToBricsCAD { get; set; }
    public bool DataCoverageReviewed { get; set; }
    public bool SplineFitCreated { get; set; }
    public bool ProductPositionReviewed { get; set; }
    public bool PointsAt1mCreated { get; set; }
    public bool PointsXyzExtracted { get; set; }
    
    // 7KPCalc
    public bool SeabedDepthKpDccCalculated { get; set; }
    public bool RovDepthKpDccCalculated { get; set; }
    
    // Final
    public bool FinalListingProduced { get; set; }
    
    // Timestamps
    public DateTime? ProcessingStartTime { get; set; }
    public DateTime? ProcessingEndTime { get; set; }
}

/// <summary>
/// Options for PDF report generation
/// </summary>
public class PdfReportOptions
{
    public bool IncludeDataSummary { get; set; } = true;
    public bool IncludeSampleData { get; set; } = true;
    public int SampleDataRows { get; set; } = 20;
    public bool IncludeTrackPlot { get; set; } = false; // Future enhancement
    public bool IncludeCribSheet { get; set; } = true;
    
    /// <summary>
    /// Include full data table with all points (can be many pages)
    /// </summary>
    public bool IncludeFullDataTable { get; set; } = false;
    
    /// <summary>
    /// Include depth profile chart
    /// </summary>
    public bool IncludeDepthChart { get; set; } = true;
}
