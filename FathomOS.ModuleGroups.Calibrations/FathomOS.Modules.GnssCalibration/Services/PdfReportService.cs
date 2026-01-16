using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Export options for PDF report generation.
/// </summary>
public class PdfExportOptions
{
    public bool IncludeStatisticsTable { get; set; } = true;
    public bool IncludeDataTable { get; set; } = true;
    public bool IncludeChart { get; set; } = true;
    public int MaxDataRows { get; set; } = 500;
}

/// <summary>
/// Generates PDF reports for GNSS calibration results.
/// Uses QuestPDF library (from Core) with Svg() API.
/// </summary>
public class PdfReportService
{
    private readonly PdfExportOptions _options;
    
    // Subsea7 branding colors
    private const string PrimaryColor = "#1E3A5F";
    private const string AccentColor = "#E67E22";
    private const string SuccessColor = "#2ECC71";
    private const string ErrorColor = "#E74C3C";
    private const string LightGray = "#F5F7FA";
    private const string TextColor = "#333333";
    
    public PdfReportService() : this(new PdfExportOptions()) { }
    
    public PdfReportService(PdfExportOptions options)
    {
        _options = options;
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    /// <summary>
    /// Generates PDF report for GNSS comparison results.
    /// </summary>
    public void Generate(string filePath, GnssProject project,
        IEnumerable<GnssComparisonPoint> points, GnssStatisticsResult? statistics)
    {
        var pointList = points.ToList();
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextColor));
                
                page.Header().Element(c => ComposeHeader(c, project));
                page.Content().Element(c => ComposeContent(c, project, pointList, statistics));
                page.Footer().Element(c => ComposeFooter(c));
            });
        })
        .GeneratePdf(filePath);
    }
    
    /// <summary>
    /// Generates a PDF containing all data points (full data export).
    /// </summary>
    public void GenerateFullDataPdf(string filePath, GnssProject project, IEnumerable<GnssComparisonPoint> points)
    {
        var pointList = points.ToList();
        var rowsPerPage = 45; // Rows per page for data table
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextColor));
                
                page.Header().Element(c => ComposeFullDataHeader(c, project, pointList.Count));
                page.Content().Element(c => ComposeFullDataContent(c, project, pointList, rowsPerPage));
                page.Footer().Element(c => ComposeFooter(c));
            });
        })
        .GeneratePdf(filePath);
    }
    
    private void ComposeFullDataHeader(IContainer container, GnssProject project, int totalPoints)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("GNSS Calibration - Full Data Export")
                        .FontSize(16).Bold().FontColor(PrimaryColor);
                    col.Item().Text($"Project: {project.ProjectName} | Total Points: {totalPoints}")
                        .FontSize(10).FontColor(AccentColor);
                });
                
                row.ConstantItem(120).AlignRight().Column(col =>
                {
                    col.Item().Text("Fathom OS").Bold().FontColor(PrimaryColor);
                    col.Item().Text(DateTime.Now.ToString("yyyy-MM-dd")).FontSize(8);
                });
            });
            
            column.Item().PaddingTop(3).LineHorizontal(1).LineColor(PrimaryColor);
        });
    }
    
    private void ComposeFullDataContent(IContainer container, GnssProject project, 
        List<GnssComparisonPoint> points, int rowsPerPage)
    {
        var unit = project.UnitAbbreviation;
        
        container.Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(35);   // #
                cols.ConstantColumn(65);   // Time
                cols.RelativeColumn(1);    // ΔE
                cols.RelativeColumn(1);    // ΔN
                cols.RelativeColumn(1);    // ΔH
                cols.RelativeColumn(1);    // Radial
                cols.RelativeColumn(1.2f); // Sys1 E
                cols.RelativeColumn(1.2f); // Sys1 N
                cols.RelativeColumn(1.2f); // Sys2 E
                cols.RelativeColumn(1.2f); // Sys2 N
                cols.ConstantColumn(50);   // Status
            });
            
            // Header row
            table.Header(header =>
            {
                header.Cell().Background(PrimaryColor).Padding(4).Text("#").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).Text("Time").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text($"ΔE ({unit})").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text($"ΔN ({unit})").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text($"ΔH ({unit})").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("Radial").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("Sys1 E").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("Sys1 N").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("Sys2 E").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignRight().Text("Sys2 N").FontColor("#FFFFFF").Bold().FontSize(8);
                header.Cell().Background(PrimaryColor).Padding(4).AlignCenter().Text("Status").FontColor("#FFFFFF").Bold().FontSize(8);
            });
            
            // Data rows
            for (int i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var isAlt = i % 2 == 1;
                var bgColor = isAlt ? LightGray : "#FFFFFF";
                var statusColor = point.IsRejected ? ErrorColor : SuccessColor;
                var statusText = point.IsRejected ? "Rejected" : "OK";
                
                table.Cell().Background(bgColor).Padding(3).Text(point.Index.ToString()).FontSize(8);
                table.Cell().Background(bgColor).Padding(3).Text(point.DateTime.ToString("HH:mm:ss")).FontSize(8);
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.DeltaEasting.ToString("F4")).FontSize(8);
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.DeltaNorthing.ToString("F4")).FontSize(8);
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.DeltaHeight.ToString("F4")).FontSize(8);
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.RadialDistance.ToString("F4")).FontSize(8).Bold();
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.System1Easting.ToString("F3")).FontSize(7);
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.System1Northing.ToString("F3")).FontSize(7);
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.System2Easting.ToString("F3")).FontSize(7);
                table.Cell().Background(bgColor).Padding(3).AlignRight().Text(point.System2Northing.ToString("F3")).FontSize(7);
                table.Cell().Background(bgColor).Padding(3).AlignCenter().Text(statusText).FontSize(8).FontColor(statusColor).Bold();
            }
        });
    }
    
    private void ComposeHeader(IContainer container, GnssProject project)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("GNSS Calibration & Verification Report")
                        .FontSize(18).Bold().FontColor(PrimaryColor);
                    col.Item().Text($"Project: {project.ProjectName}")
                        .FontSize(12).FontColor(AccentColor);
                });
                
                row.ConstantItem(120).AlignRight().Column(col =>
                {
                    col.Item().Text("Fathom OS").Bold().FontColor(PrimaryColor);
                    col.Item().Text("Survey Solutions").FontSize(8);
                });
            });
            
            column.Item().PaddingTop(5).LineHorizontal(2).LineColor(PrimaryColor);
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generated: ").FontSize(8);
                text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(8).Bold();
            });
            
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8);
                text.CurrentPageNumber().FontSize(8);
                text.Span(" of ").FontSize(8);
                text.TotalPages().FontSize(8);
            });
        });
    }
    
    private void ComposeContent(IContainer container, GnssProject project,
        List<GnssComparisonPoint> points, GnssStatisticsResult? statistics)
    {
        container.Column(column =>
        {
            // Project Info
            column.Item().Element(c => ComposeProjectInfo(c, project, points));
            column.Item().PaddingTop(15);
            
            // Statistics
            if (_options.IncludeStatisticsTable && statistics != null)
            {
                column.Item().Element(c => ComposeStatistics(c, project, statistics));
                column.Item().PaddingTop(15);
            }
            
            // Chart (using SVG)
            if (_options.IncludeChart && statistics != null)
            {
                column.Item().Element(c => ComposeChartSvg(c, project, points, statistics.FilteredStatistics));
                column.Item().PaddingTop(15);
            }
            
            // Data Table
            if (_options.IncludeDataTable)
            {
                column.Item().Element(c => ComposeDataTable(c, project, points));
            }
        });
    }
    
    private void ComposeProjectInfo(IContainer container, GnssProject project, List<GnssComparisonPoint> points)
    {
        container.Background(LightGray).Padding(10).Column(column =>
        {
            column.Item().Text("Project Information").Bold().FontSize(12).FontColor(PrimaryColor);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Client: {project.ClientName}");
                    col.Item().Text($"Vessel: {project.VesselName}");
                    col.Item().Text($"Surveyor: {project.SurveyorName}");
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"System 1 (C): {project.System1Name}");
                    col.Item().Text($"System 2 (O): {project.System2Name}");
                    col.Item().Text($"Unit: {project.CoordinateUnit}");
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Date: {project.SurveyDate:yyyy-MM-dd}");
                    col.Item().Text($"Total Points: {points.Count}");
                    col.Item().Text($"Accepted: {points.Count(p => !p.IsRejected)}");
                });
            });
        });
    }
    
    private void ComposeStatistics(IContainer container, GnssProject project, GnssStatisticsResult statistics)
    {
        var stats = statistics.FilteredStatistics;
        string unit = project.CoordinateUnit ?? "m";
        
        container.Column(column =>
        {
            column.Item().Text("Statistical Summary (Filtered Data)").Bold().FontSize(12).FontColor(PrimaryColor);
            
            // 2DRMS highlight
            column.Item().PaddingTop(5).Background(PrimaryColor).Padding(10).Row(row =>
            {
                row.RelativeItem().Text($"2DRMS: {stats.Delta2DRMS:F3} {unit}")
                    .FontSize(16).Bold().FontColor(Colors.White);
                row.RelativeItem().AlignRight().Text($"Accepted: {statistics.AcceptedCount} / Rejected: {statistics.RejectedCount}")
                    .FontSize(10).FontColor(Colors.White);
            });
            
            // Statistics table
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                
                // Header
                table.Header(header =>
                {
                    header.Cell().Background(PrimaryColor).Padding(5).Text("Statistic").FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(5).Text($"ΔE ({unit})").FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(5).Text($"ΔN ({unit})").FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(5).Text($"ΔH ({unit})").FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(5).Text($"Radial ({unit})").FontColor(Colors.White).Bold();
                });
                
                // Data rows
                AddStatRow(table, "Mean", stats.DeltaMeanEasting, stats.DeltaMeanNorthing, stats.DeltaMeanHeight, stats.MeanRadialError, false);
                AddStatRow(table, "Std Dev (σ)", stats.DeltaSdEasting, stats.DeltaSdNorthing, stats.DeltaSdHeight, stats.StdDevRadialError, true);
                AddStatRow(table, "Max", stats.DeltaMaxEasting, stats.DeltaMaxNorthing, stats.DeltaMaxHeight, stats.MaxRadialError, false);
                AddStatRow(table, "Min", stats.DeltaMinEasting, stats.DeltaMinNorthing, stats.DeltaMinHeight, 0, true);
            });
            
            // Additional metrics
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Background(LightGray).Padding(5).Column(col =>
                {
                    col.Item().Text($"RMS Radial: {stats.RmsRadialError:F3} {unit}").FontSize(9);
                    col.Item().Text($"CEP 50%: {stats.Cep50:F3} {unit}").FontSize(9);
                    col.Item().Text($"CEP 95%: {stats.Cep95:F3} {unit}").FontSize(9);
                });
                row.RelativeItem().Background(LightGray).Padding(5).Column(col =>
                {
                    col.Item().Text($"System 1 2DRMS: {stats.System1_2DRMS:F3} {unit}").FontSize(9);
                    col.Item().Text($"System 2 2DRMS: {stats.System2_2DRMS:F3} {unit}").FontSize(9);
                    col.Item().Text($"Spread: {stats.Spread:F3} {unit}").FontSize(9);
                });
            });
        });
    }
    
    private void AddStatRow(TableDescriptor table, string label, double e, double n, double h, double r, bool alt)
    {
        string bg = alt ? LightGray : "#FFFFFF";
        table.Cell().Background(bg).Padding(3).Text(label).FontSize(9);
        table.Cell().Background(bg).Padding(3).AlignRight().Text($"{e:F3}").FontSize(9);
        table.Cell().Background(bg).Padding(3).AlignRight().Text($"{n:F3}").FontSize(9);
        table.Cell().Background(bg).Padding(3).AlignRight().Text($"{h:F3}").FontSize(9);
        table.Cell().Background(bg).Padding(3).AlignRight().Text($"{r:F3}").FontSize(9);
    }
    
    /// <summary>
    /// Compose chart using SVG (replaces deprecated Canvas API)
    /// </summary>
    private void ComposeChartSvg(IContainer container, GnssProject project,
        List<GnssComparisonPoint> points, GnssStatistics stats)
    {
        string unit = project.CoordinateUnit ?? "m";
        
        container.Column(column =>
        {
            column.Item().Text($"Delta E vs Delta N Scatter Plot ({unit})").Bold().FontSize(12).FontColor(PrimaryColor);
            
            // Generate SVG chart
            string svg = GenerateScatterSvg(points, stats, 500, 300, unit);
            column.Item().PaddingTop(5).Svg(svg);
        });
    }
    
    private string GenerateScatterSvg(List<GnssComparisonPoint> points, GnssStatistics stats, 
        int width, int height, string unit)
    {
        int margin = 50;
        int chartWidth = width - 2 * margin;
        int chartHeight = height - 2 * margin;
        
        var acceptedPoints = points.Where(p => !p.IsRejected).ToList();
        var rejectedPoints = points.Where(p => p.IsRejected).ToList();
        
        if (acceptedPoints.Count == 0)
        {
            return $"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'><text x='{width/2}' y='{height/2}' text-anchor='middle'>No data</text></svg>";
        }
        
        // Calculate bounds
        double minX = acceptedPoints.Min(p => p.DeltaEasting) - stats.Delta2DRMS * 0.3;
        double maxX = acceptedPoints.Max(p => p.DeltaEasting) + stats.Delta2DRMS * 0.3;
        double minY = acceptedPoints.Min(p => p.DeltaNorthing) - stats.Delta2DRMS * 0.3;
        double maxY = acceptedPoints.Max(p => p.DeltaNorthing) + stats.Delta2DRMS * 0.3;
        
        // Make square
        double range = Math.Max(maxX - minX, maxY - minY);
        double centerX = (minX + maxX) / 2;
        double centerY = (minY + maxY) / 2;
        minX = centerX - range / 2; maxX = centerX + range / 2;
        minY = centerY - range / 2; maxY = centerY + range / 2;
        
        // Transform functions
        double ToSvgX(double x) => margin + (x - minX) / (maxX - minX) * chartWidth;
        double ToSvgY(double y) => margin + chartHeight - (y - minY) / (maxY - minY) * chartHeight;
        
        var svg = new System.Text.StringBuilder();
        svg.AppendLine($"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>");
        
        // Background
        svg.AppendLine($"<rect x='{margin}' y='{margin}' width='{chartWidth}' height='{chartHeight}' fill='#F8F9FA' stroke='#DEE2E6'/>");
        
        // Grid lines
        for (int i = 0; i <= 4; i++)
        {
            double gx = margin + i * chartWidth / 4.0;
            double gy = margin + i * chartHeight / 4.0;
            svg.AppendLine($"<line x1='{gx:F1}' y1='{margin}' x2='{gx:F1}' y2='{margin + chartHeight}' stroke='#E9ECEF' stroke-width='1'/>");
            svg.AppendLine($"<line x1='{margin}' y1='{gy:F1}' x2='{margin + chartWidth}' y2='{gy:F1}' stroke='#E9ECEF' stroke-width='1'/>");
        }
        
        // Zero lines if in range
        if (minX < 0 && maxX > 0)
        {
            double zeroX = ToSvgX(0);
            svg.AppendLine($"<line x1='{zeroX:F1}' y1='{margin}' x2='{zeroX:F1}' y2='{margin + chartHeight}' stroke='#ADB5BD' stroke-width='1' stroke-dasharray='4,2'/>");
        }
        if (minY < 0 && maxY > 0)
        {
            double zeroY = ToSvgY(0);
            svg.AppendLine($"<line x1='{margin}' y1='{zeroY:F1}' x2='{margin + chartWidth}' y2='{zeroY:F1}' stroke='#ADB5BD' stroke-width='1' stroke-dasharray='4,2'/>");
        }
        
        // 2DRMS circle
        double cx = ToSvgX(stats.DeltaMeanEasting);
        double cy = ToSvgY(stats.DeltaMeanNorthing);
        double radius = stats.Delta2DRMS / (maxX - minX) * chartWidth;
        svg.AppendLine($"<circle cx='{cx:F1}' cy='{cy:F1}' r='{radius:F1}' fill='none' stroke='#9B59B6' stroke-width='2' stroke-dasharray='8,4'/>");
        
        // Rejected points (red X)
        foreach (var p in rejectedPoints)
        {
            double px = ToSvgX(p.DeltaEasting);
            double py = ToSvgY(p.DeltaNorthing);
            svg.AppendLine($"<line x1='{px-3:F1}' y1='{py-3:F1}' x2='{px+3:F1}' y2='{py+3:F1}' stroke='#E74C3C' stroke-width='2'/>");
            svg.AppendLine($"<line x1='{px+3:F1}' y1='{py-3:F1}' x2='{px-3:F1}' y2='{py+3:F1}' stroke='#E74C3C' stroke-width='2'/>");
        }
        
        // Accepted points (green)
        foreach (var p in acceptedPoints)
        {
            double px = ToSvgX(p.DeltaEasting);
            double py = ToSvgY(p.DeltaNorthing);
            svg.AppendLine($"<circle cx='{px:F1}' cy='{py:F1}' r='3' fill='#2ECC71'/>");
        }
        
        // Mean marker (orange diamond)
        svg.AppendLine($"<polygon points='{cx:F1},{cy-6:F1} {cx+6:F1},{cy:F1} {cx:F1},{cy+6:F1} {cx-6:F1},{cy:F1}' fill='#E67E22'/>");
        
        // Axis labels
        svg.AppendLine($"<text x='{width/2}' y='{height - 5}' text-anchor='middle' font-size='10' fill='#333'>ΔE ({unit})</text>");
        svg.AppendLine($"<text x='12' y='{height/2}' text-anchor='middle' font-size='10' fill='#333' transform='rotate(-90,12,{height/2})'>ΔN ({unit})</text>");
        
        // Axis values
        svg.AppendLine($"<text x='{margin}' y='{height - 25}' text-anchor='middle' font-size='8' fill='#666'>{minX:F2}</text>");
        svg.AppendLine($"<text x='{margin + chartWidth}' y='{height - 25}' text-anchor='middle' font-size='8' fill='#666'>{maxX:F2}</text>");
        svg.AppendLine($"<text x='{margin - 5}' y='{margin + chartHeight}' text-anchor='end' font-size='8' fill='#666'>{minY:F2}</text>");
        svg.AppendLine($"<text x='{margin - 5}' y='{margin + 5}' text-anchor='end' font-size='8' fill='#666'>{maxY:F2}</text>");
        
        // Legend
        int ly = margin + 10;
        svg.AppendLine($"<circle cx='{margin + 10}' cy='{ly}' r='4' fill='#2ECC71'/>");
        svg.AppendLine($"<text x='{margin + 20}' y='{ly + 4}' font-size='9' fill='#333'>Accepted</text>");
        svg.AppendLine($"<line x1='{margin + 7}' y1='{ly + 12}' x2='{margin + 13}' y2='{ly + 18}' stroke='#E74C3C' stroke-width='2'/>");
        svg.AppendLine($"<line x1='{margin + 13}' y1='{ly + 12}' x2='{margin + 7}' y2='{ly + 18}' stroke='#E74C3C' stroke-width='2'/>");
        svg.AppendLine($"<text x='{margin + 20}' y='{ly + 19}' font-size='9' fill='#333'>Rejected</text>");
        svg.AppendLine($"<circle cx='{margin + 10}' cy='{ly + 30}' r='4' fill='none' stroke='#9B59B6' stroke-width='1.5' stroke-dasharray='3,2'/>");
        svg.AppendLine($"<text x='{margin + 20}' y='{ly + 34}' font-size='9' fill='#333'>2DRMS = {stats.Delta2DRMS:F3}</text>");
        
        svg.AppendLine("</svg>");
        return svg.ToString();
    }
    
    private void ComposeDataTable(IContainer container, GnssProject project,
        List<GnssComparisonPoint> points)
    {
        var displayPoints = points.Take(_options.MaxDataRows).ToList();
        string unit = project.CoordinateUnit ?? "m";
        
        container.Column(column =>
        {
            column.Item().Text($"Data Points (showing {displayPoints.Count} of {points.Count})")
                .Bold().FontSize(12).FontColor(PrimaryColor);
            
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);  // Index
                    columns.RelativeColumn();    // DateTime
                    columns.RelativeColumn();    // ΔE
                    columns.RelativeColumn();    // ΔN
                    columns.RelativeColumn();    // ΔH
                    columns.RelativeColumn();    // Radial
                    columns.ConstantColumn(50);  // Status
                });
                
                // Header
                table.Header(header =>
                {
                    header.Cell().Background(PrimaryColor).Padding(3).Text("#").FontSize(8).FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(3).Text("DateTime").FontSize(8).FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(3).Text($"ΔE ({unit})").FontSize(8).FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(3).Text($"ΔN ({unit})").FontSize(8).FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(3).Text($"ΔH ({unit})").FontSize(8).FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(3).Text($"Radial ({unit})").FontSize(8).FontColor(Colors.White).Bold();
                    header.Cell().Background(PrimaryColor).Padding(3).Text("Status").FontSize(8).FontColor(Colors.White).Bold();
                });
                
                // Data rows
                int idx = 0;
                foreach (var point in displayPoints)
                {
                    string bg = idx % 2 == 0 ? "#FFFFFF" : LightGray;
                    var statusColor = point.IsRejected ? ErrorColor : SuccessColor;
                    var statusText = point.IsRejected ? "Rej" : "OK";
                    
                    table.Cell().Background(bg).Padding(2).Text($"{point.Index}").FontSize(7);
                    table.Cell().Background(bg).Padding(2).Text($"{point.DateTime:HH:mm:ss}").FontSize(7);
                    table.Cell().Background(bg).Padding(2).AlignRight().Text($"{point.DeltaEasting:F3}").FontSize(7);
                    table.Cell().Background(bg).Padding(2).AlignRight().Text($"{point.DeltaNorthing:F3}").FontSize(7);
                    table.Cell().Background(bg).Padding(2).AlignRight().Text($"{point.DeltaHeight:F3}").FontSize(7);
                    table.Cell().Background(bg).Padding(2).AlignRight().Text($"{point.RadialError:F3}").FontSize(7);
                    table.Cell().Background(statusColor).Padding(2).AlignCenter().Text(statusText).FontSize(7).FontColor(Colors.White);
                    
                    idx++;
                }
            });
        });
    }
    
    /// <summary>
    /// Generates PDF report with embedded chart images.
    /// </summary>
    public void GenerateWithCharts(string filePath, GnssProject project,
        IEnumerable<GnssComparisonPoint> points, GnssStatisticsResult? statistics,
        IEnumerable<(byte[] ImageData, string Title)> chartImages)
    {
        var pointList = points.ToList();
        var charts = chartImages.ToList();
        
        Document.Create(container =>
        {
            // Page 1: Summary with statistics
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextColor));
                
                page.Header().Element(c => ComposeHeader(c, project));
                page.Content().Element(c => ComposeContent(c, project, pointList, statistics));
                page.Footer().Element(c => ComposeFooter(c));
            });
            
            // Chart pages (2 charts per page)
            for (int i = 0; i < charts.Count; i += 2)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextColor));
                    
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Text("GNSS Calibration - Charts")
                            .FontSize(14).Bold().FontColor(PrimaryColor);
                        row.ConstantItem(100).AlignRight().Text($"Page {2 + i/2}")
                            .FontSize(9).FontColor(AccentColor);
                    });
                    
                    page.Content().Column(column =>
                    {
                        column.Spacing(15);
                        
                        // First chart
                        if (i < charts.Count)
                        {
                            var chart1 = charts[i];
                            column.Item().Column(c =>
                            {
                                c.Item().Text(chart1.Title)
                                    .FontSize(11).Bold().FontColor(PrimaryColor);
                                c.Item().PaddingTop(5)
                                    .Image(chart1.ImageData)
                                    .FitWidth();
                            });
                        }
                        
                        // Second chart
                        if (i + 1 < charts.Count)
                        {
                            var chart2 = charts[i + 1];
                            column.Item().Column(c =>
                            {
                                c.Item().Text(chart2.Title)
                                    .FontSize(11).Bold().FontColor(PrimaryColor);
                                c.Item().PaddingTop(5)
                                    .Image(chart2.ImageData)
                                    .FitWidth();
                            });
                        }
                    });
                    
                    page.Footer().Element(c => ComposeFooter(c));
                });
            }
        })
        .GeneratePdf(filePath);
    }
    
    /// <summary>
    /// Generates PDF report with supervisor approval information.
    /// </summary>
    public void GenerateReport(string filePath, GnssProject project, List<GnssComparisonPoint> points, 
        GnssStatisticsResult statistics, string supervisorName, string supervisorInitials, DateTime? approvalDateTime)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextColor));
                
                page.Header().Element(c => ComposeHeader(c, project));
                page.Content().Column(column =>
                {
                    // Approval badge
                    if (!string.IsNullOrEmpty(supervisorName))
                    {
                        column.Item().PaddingBottom(15).Row(row =>
                        {
                            row.RelativeItem().Background("#F0FDF4").Padding(12).Row(innerRow =>
                            {
                                innerRow.AutoItem().Text("✓").FontSize(16).Bold().FontColor(SuccessColor);
                                innerRow.RelativeItem().PaddingLeft(10).Column(col =>
                                {
                                    col.Item().Text($"APPROVED BY: {supervisorName} ({supervisorInitials})")
                                        .FontSize(11).Bold().FontColor("#166534");
                                    col.Item().Text($"Date: {approvalDateTime:yyyy-MM-dd HH:mm:ss}")
                                        .FontSize(9).FontColor("#166534");
                                });
                            });
                        });
                    }
                    
                    // Rest of content
                    column.Item().Element(container => ComposeContent(container, project, points, statistics));
                });
                page.Footer().Element(c => ComposeFooter(c));
            });
        })
        .GeneratePdf(filePath);
    }
    
    /// <summary>
    /// Generates a digital calibration certificate PDF.
    /// </summary>
    public void GenerateDigitalCertificate(string filePath, GnssProject project, 
        GnssStatisticsResult statistics, string supervisorName, string supervisorInitials, 
        DateTime? approvalDateTime, bool passed)
    {
        var statusColor = passed ? SuccessColor : ErrorColor;
        var statusText = passed ? "PASS" : "FAIL";
        var statusBg = passed ? "#F0FDF4" : "#FEF2F2";
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(TextColor));
                
                page.Content().Column(column =>
                {
                    // Header with logo/branding
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("SUBSEA 7").FontSize(24).Bold().FontColor(PrimaryColor);
                            col.Item().Text("Survey Solutions").FontSize(12).FontColor("#666666");
                        });
                        row.AutoItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("GNSS CALIBRATION").FontSize(14).Bold().FontColor(PrimaryColor);
                            col.Item().Text("Digital Certificate").FontSize(10).FontColor("#666666");
                        });
                    });
                    
                    column.Item().PaddingVertical(15).LineHorizontal(2).LineColor(PrimaryColor);
                    
                    // Certificate Title
                    column.Item().AlignCenter().PaddingVertical(20).Text("CALIBRATION CERTIFICATE")
                        .FontSize(28).Bold().FontColor(PrimaryColor);
                    
                    // Status Badge - Large centered
                    column.Item().AlignCenter().PaddingVertical(20).Background(statusBg).Padding(25).Row(row =>
                    {
                        row.AutoItem().AlignCenter().Column(col =>
                        {
                            col.Item().AlignCenter().Text(statusText)
                                .FontSize(48).ExtraBold().FontColor(statusColor);
                            col.Item().AlignCenter().Text("Calibration Status")
                                .FontSize(12).FontColor("#666666");
                        });
                    });
                    
                    // Project Information
                    column.Item().PaddingTop(25).Text("PROJECT INFORMATION")
                        .FontSize(12).Bold().FontColor(PrimaryColor);
                    column.Item().PaddingTop(5).LineHorizontal(1).LineColor("#E5E7EB");
                    
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                        });
                        
                        table.Cell().Padding(5).Text("Project:").Bold();
                        table.Cell().Padding(5).Text(project.ProjectName ?? "---");
                        table.Cell().Padding(5).Text("Vessel:").Bold();
                        table.Cell().Padding(5).Text(project.VesselName ?? "---");
                        
                        table.Cell().Padding(5).Text("Client:").Bold();
                        table.Cell().Padding(5).Text(project.ClientName ?? "---");
                        table.Cell().Padding(5).Text("Date:").Bold();
                        table.Cell().Padding(5).Text(project.SurveyDate?.ToString("yyyy-MM-dd") ?? "---");
                        
                        table.Cell().Padding(5).Text("GNSS 1:").Bold();
                        table.Cell().Padding(5).Text(project.System1Name ?? "System 1");
                        table.Cell().Padding(5).Text("GNSS 2:").Bold();
                        table.Cell().Padding(5).Text(project.System2Name ?? "System 2");
                    });
                    
                    // Calibration Results
                    column.Item().PaddingTop(25).Text("CALIBRATION RESULTS")
                        .FontSize(12).Bold().FontColor(PrimaryColor);
                    column.Item().PaddingTop(5).LineHorizontal(1).LineColor("#E5E7EB");
                    
                    column.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                        });
                        
                        table.Cell().Padding(8).Background(LightGray).Text("2DRMS:").Bold();
                        table.Cell().Padding(8).Background(LightGray).Text($"{statistics.FilteredStatistics.TwoDRMS:F4} {project.UnitAbbreviation}").Bold().FontColor(PrimaryColor);
                        table.Cell().Padding(8).Background(LightGray).Text("Tolerance:").Bold();
                        table.Cell().Padding(8).Background(LightGray).Text($"{project.ToleranceValue:F4} {project.UnitAbbreviation}").Bold();
                        
                        table.Cell().Padding(8).Text("Mean ΔE:");
                        table.Cell().Padding(8).Text($"{statistics.FilteredStatistics.DeltaMeanEasting:F4}");
                        table.Cell().Padding(8).Text("Std Dev ΔE:");
                        table.Cell().Padding(8).Text($"{statistics.FilteredStatistics.DeltaStdDevEasting:F4}");
                        
                        table.Cell().Padding(8).Text("Mean ΔN:");
                        table.Cell().Padding(8).Text($"{statistics.FilteredStatistics.DeltaMeanNorthing:F4}");
                        table.Cell().Padding(8).Text("Std Dev ΔN:");
                        table.Cell().Padding(8).Text($"{statistics.FilteredStatistics.DeltaStdDevNorthing:F4}");
                        
                        table.Cell().Padding(8).Text("Points Used:");
                        table.Cell().Padding(8).Text($"{statistics.FilteredStatistics.SampleCount:N0}");
                        table.Cell().Padding(8).Text("CEP 95%:");
                        table.Cell().Padding(8).Text($"{statistics.FilteredStatistics.Cep95:F4}");
                    });
                    
                    // Approval Section
                    column.Item().PaddingTop(30).Text("APPROVAL")
                        .FontSize(12).Bold().FontColor(PrimaryColor);
                    column.Item().PaddingTop(5).LineHorizontal(1).LineColor("#E5E7EB");
                    
                    column.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Approved By:").Bold();
                            col.Item().PaddingTop(5).Text(supervisorName ?? "---").FontSize(14);
                        });
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Initials:").Bold();
                            col.Item().PaddingTop(5).Text(supervisorInitials ?? "---").FontSize(14);
                        });
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Date/Time:").Bold();
                            col.Item().PaddingTop(5).Text(approvalDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "---").FontSize(14);
                        });
                    });
                    
                    // Signature Line
                    column.Item().PaddingTop(40).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().LineHorizontal(1).LineColor("#333333");
                            col.Item().PaddingTop(5).Text("Supervisor Signature").FontSize(9).FontColor("#666666");
                        });
                        row.ConstantItem(50);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().LineHorizontal(1).LineColor("#333333");
                            col.Item().PaddingTop(5).Text("Date").FontSize(9).FontColor("#666666");
                        });
                    });
                    
                    // Footer disclaimer
                    column.Item().PaddingTop(30).AlignCenter().Text(
                        "This certificate confirms that the GNSS calibration was performed in accordance with " +
                        "standard operating procedures. Results are valid for the equipment and conditions specified above.")
                        .FontSize(8).FontColor("#888888").Italic();
                    
                    // Document ID
                    column.Item().PaddingTop(20).AlignCenter().Text(
                        $"Certificate ID: GNSS-{project.ProjectName?.Replace(" ", "")}-{DateTime.Now:yyyyMMddHHmmss}")
                        .FontSize(8).FontColor("#AAAAAA");
                });
            });
        })
        .GeneratePdf(filePath);
    }
}
