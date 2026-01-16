using System.IO;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;
using FathomOS.Modules.RovGyroCalibration.Models;

namespace FathomOS.Modules.RovGyroCalibration.Services;

/// <summary>
/// Report export service for ROV Gyro Calibration.
/// Uses SVG for charts (QuestPDF 2024.3.0+ compatible).
/// </summary>
public class ReportExportService
{
    #region PDF Export
    
    public void ExportPdf(string outputPath, CalibrationProject project, 
        List<RovGyroDataPoint> dataPoints, CalibrationResult result, ValidationResult validation)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(c => ComposeHeader(c, project));
                page.Content().Element(c => ComposeContent(c, project, dataPoints, result, validation));
                page.Footer().Element(ComposeFooter);
            });
        });
        
        document.GeneratePdf(outputPath);
    }
    
    private void ComposeHeader(IContainer container, CalibrationProject project)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("ROV GYRO CALIBRATION REPORT")
                    .FontSize(18).Bold().FontColor(Colors.Orange.Darken2);
                col.Item().Text($"{project.Purpose} - {project.ProjectName}")
                    .FontSize(12).FontColor(Colors.Grey.Darken1);
            });
            
            row.ConstantItem(100).AlignRight().Column(col =>
            {
                col.Item().Text("FATHOM OS").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text(DateTime.Now.ToString("yyyy-MM-dd")).FontSize(9);
            });
        });
    }
    
    private void ComposeContent(IContainer container, CalibrationProject project,
        List<RovGyroDataPoint> dataPoints, CalibrationResult result, ValidationResult validation)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Spacing(12);
            
            // Project Info
            col.Item().Element(c => ComposeProjectInfo(c, project));
            
            // ROV Configuration
            col.Item().Element(c => ComposeRovConfiguration(c, project, result));
            
            // Results Summary
            col.Item().Element(c => ComposeResultsSummary(c, result, project));
            
            // QC Validation
            col.Item().Element(c => ComposeQcValidation(c, validation));
            
            // Statistics
            col.Item().Element(c => ComposeStatistics(c, result));
            
            // Chart - Using SVG instead of deprecated Canvas
            col.Item().Element(c => ComposeChartAsSvg(c, dataPoints, result));
            
            // Decision
            col.Item().Element(c => ComposeDecision(c, validation, result, project));
            
            // Data Table
            col.Item().Element(c => ComposeDataTable(c, dataPoints.Take(40).ToList()));
        });
    }
    
    private void ComposeProjectInfo(IContainer container, CalibrationProject project)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
        {
            col.Item().Text("PROJECT INFORMATION").FontSize(12).Bold().FontColor(Colors.Orange.Darken2);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Project: {project.ProjectName}");
                    c.Item().Text($"Client: {project.ClientName}");
                    c.Item().Text($"Vessel: {project.VesselName}");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"ROV: {project.RovName}");
                    c.Item().Text($"Survey Date: {project.SurveyDate:yyyy-MM-dd}");
                    c.Item().Text($"Mode: {project.Purpose}");
                });
            });
        });
    }
    
    private void ComposeRovConfiguration(IContainer container, CalibrationProject project, CalibrationResult result)
    {
        var config = project.RovConfig;
        
        container.Border(1).BorderColor(Colors.Orange.Darken1).Background(Colors.Orange.Lighten5).Padding(10).Column(col =>
        {
            col.Item().Text("ROV GEOMETRIC CONFIGURATION").FontSize(12).Bold().FontColor(Colors.Orange.Darken2);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Facing Direction: {config.FacingDirection}");
                    c.Item().Text($"Baseline Side: {config.BaselineSide}");
                    c.Item().Text($"Baseline Distance: {config.BaselineDistance:F2} m");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Baseline Offset (D): {config.BaselineOffset:F2}°");
                    c.Item().Text($"Port Measurement (P): {config.PortMeasurement:F3} m");
                    c.Item().Text($"Starboard Measurement (S): {config.StarboardMeasurement:F3} m");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Calculated Corrections:").Bold();
                    c.Item().Text($"θ (Baseline Angle): {result.BaselineAngleTheta:F4}°");
                    c.Item().Text($"Facing Offset: {config.FacingDirectionOffset:F1}°");
                });
            });
            
            // Formula explanation
            col.Item().PaddingTop(8).Background(Colors.White).Padding(8).Column(formula =>
            {
                formula.Item().Text("Correction Formula: C = V + D + θ + Facing").FontSize(10).Bold();
                formula.Item().Text($"Where: V=Vessel Heading, D={config.BaselineOffset:F2}°, θ={result.BaselineAngleTheta:F4}°, Facing={config.FacingDirectionOffset:F1}°")
                    .FontSize(9).FontColor(Colors.Grey.Darken1);
            });
        });
    }
    
    private void ComposeResultsSummary(IContainer container, CalibrationResult result, CalibrationProject project)
    {
        container.Border(1).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten5).Padding(15).Column(col =>
        {
            col.Item().Text("CALIBRATION RESULTS").FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Mean C-O (Accepted):").FontSize(10);
                    c.Item().Text($"{result.MeanCOAccepted:F4}°").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Standard Deviation:").FontSize(10);
                    c.Item().Text($"{result.StdDevAccepted:F4}°").FontSize(24).Bold().FontColor(Colors.Grey.Darken1);
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Rejection Rate:").FontSize(10);
                    var color = result.RejectionPercentage < 5 ? Colors.Green.Darken1 : 
                               result.RejectionPercentage < 10 ? Colors.Orange.Darken1 : Colors.Red.Darken1;
                    c.Item().Text($"{result.RejectionPercentage:F1}%").FontSize(24).Bold().FontColor(color);
                });
            });
            
            if (project.Purpose == CalibrationPurpose.Calibration)
            {
                col.Item().PaddingTop(10).Text($"→ Apply {result.MeanCOAccepted:F4}° as new ROV gyro correction value")
                    .FontSize(11).Bold().FontColor(Colors.Green.Darken2);
            }
        });
    }
    
    private void ComposeQcValidation(IContainer container, ValidationResult validation)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
        {
            var statusColor = validation.OverallStatus switch
            {
                QcStatus.Pass => Colors.Green.Darken1,
                QcStatus.Warning => Colors.Orange.Darken1,
                _ => Colors.Red.Darken1
            };
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("QC VALIDATION").FontSize(12).Bold().FontColor(Colors.Orange.Darken2);
                row.ConstantItem(100).AlignRight().Text(validation.OverallStatus.ToString().ToUpper())
                    .FontSize(12).Bold().FontColor(statusColor);
            });
            
            if (validation.Checks != null && validation.Checks.Any())
            {
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(60);
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Text("Check").Bold();
                        header.Cell().Text("Actual").Bold();
                        header.Cell().Text("Threshold").Bold();
                        header.Cell().Text("Status").Bold();
                    });
                    
                    foreach (var check in validation.Checks)
                    {
                        var checkColor = check.Status switch
                        {
                            QcStatus.Pass => Colors.Green.Darken1,
                            QcStatus.Warning => Colors.Orange.Darken1,
                            _ => Colors.Red.Darken1
                        };
                        
                        table.Cell().Text(check.Name);
                        table.Cell().Text(check.ActualValue);
                        table.Cell().Text(check.Threshold);
                        table.Cell().Text(check.Status.ToString()).FontColor(checkColor);
                    }
                });
            }
        });
    }
    
    private void ComposeStatistics(IContainer container, CalibrationResult result)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
        {
            col.Item().Text("DETAILED STATISTICS").FontSize(12).Bold().FontColor(Colors.Orange.Darken2);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Total Observations: {result.TotalObservations}");
                    c.Item().Text($"Accepted: {result.AcceptedCount}");
                    c.Item().Text($"Rejected: {result.RejectedCount}");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Mean C-O (All): {result.MeanCOAll:F4}°");
                    c.Item().Text($"Std Dev (All): {result.StdDevAll:F4}°");
                    c.Item().Text($"Min/Max C-O: {result.MinCO:F4}° / {result.MaxCO:F4}°");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Heading Range: {result.MinReferenceHeading:F1}° - {result.MaxReferenceHeading:F1}°");
                    c.Item().Text($"Coverage: {result.HeadingCoverage:F1}°");
                    c.Item().Text($"θ Applied: {result.BaselineAngleTheta:F4}°");
                });
            });
        });
    }
    
    /// <summary>
    /// Creates chart as SVG instead of using deprecated Canvas API
    /// </summary>
    private void ComposeChartAsSvg(IContainer container, List<RovGyroDataPoint> dataPoints, CalibrationResult result)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
        {
            col.Item().Text("C-O TIME SERIES (with Geometric Corrections)").FontSize(12).Bold().FontColor(Colors.Orange.Darken2);
            
            // Generate SVG chart
            var svgContent = GenerateTimeSeriesSvg(dataPoints, result, 500, 140);
            col.Item().PaddingTop(5).Svg(svgContent);
        });
    }
    
    private string GenerateTimeSeriesSvg(List<RovGyroDataPoint> dataPoints, CalibrationResult result, int width, int height)
    {
        var sb = new StringBuilder();
        int margin = 40;
        int plotWidth = width - 2 * margin;
        int plotHeight = height - 2 * margin;
        
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\">");
        
        // Background
        sb.AppendLine($"<rect x=\"0\" y=\"0\" width=\"{width}\" height=\"{height}\" fill=\"white\"/>");
        
        // Axes
        sb.AppendLine($"<line x1=\"{margin}\" y1=\"{margin}\" x2=\"{margin}\" y2=\"{height - margin}\" stroke=\"#888\" stroke-width=\"1\"/>");
        sb.AppendLine($"<line x1=\"{margin}\" y1=\"{height - margin}\" x2=\"{width - margin}\" y2=\"{height - margin}\" stroke=\"#888\" stroke-width=\"1\"/>");
        
        if (dataPoints.Count < 2)
        {
            sb.AppendLine($"<text x=\"{width / 2}\" y=\"{height / 2}\" text-anchor=\"middle\" font-size=\"12\">No data to display</text>");
            sb.AppendLine("</svg>");
            return sb.ToString();
        }
        
        // Data range
        var minTime = dataPoints.Min(p => p.DateTime).Ticks;
        var maxTime = dataPoints.Max(p => p.DateTime).Ticks;
        var timeRange = maxTime - minTime;
        if (timeRange == 0) timeRange = 1;
        
        double stdDev = Math.Max(result.StdDevAccepted, 0.1);
        double minCO = result.MeanCOAccepted - 3 * stdDev;
        double maxCO = result.MeanCOAccepted + 3 * stdDev;
        double coRange = maxCO - minCO;
        if (coRange == 0) coRange = 1;
        
        // Mean line (orange for ROV)
        double meanY = margin + plotHeight * (1 - (result.MeanCOAccepted - minCO) / coRange);
        sb.AppendLine($"<line x1=\"{margin}\" y1=\"{meanY:F1}\" x2=\"{width - margin}\" y2=\"{meanY:F1}\" stroke=\"#FF9800\" stroke-width=\"2\" stroke-dasharray=\"5,3\"/>");
        
        // ±3σ bands
        double upperY = margin + plotHeight * (1 - (result.MeanCOAccepted + 3 * stdDev - minCO) / coRange);
        double lowerY = margin + plotHeight * (1 - (result.MeanCOAccepted - 3 * stdDev - minCO) / coRange);
        sb.AppendLine($"<line x1=\"{margin}\" y1=\"{upperY:F1}\" x2=\"{width - margin}\" y2=\"{upperY:F1}\" stroke=\"#E74C3C\" stroke-width=\"1\" stroke-dasharray=\"3,3\"/>");
        sb.AppendLine($"<line x1=\"{margin}\" y1=\"{lowerY:F1}\" x2=\"{width - margin}\" y2=\"{lowerY:F1}\" stroke=\"#E74C3C\" stroke-width=\"1\" stroke-dasharray=\"3,3\"/>");
        
        // Data points
        foreach (var point in dataPoints)
        {
            double x = margin + plotWidth * (double)(point.DateTime.Ticks - minTime) / timeRange;
            double y = margin + plotHeight * (1 - (point.CalculatedCO - minCO) / coRange);
            
            // Clamp y to visible range
            y = Math.Max(margin, Math.Min(height - margin, y));
            
            string color = point.Status == PointStatus.Accepted ? "#2ECC71" : "#E74C3C";
            sb.AppendLine($"<circle cx=\"{x:F1}\" cy=\"{y:F1}\" r=\"2\" fill=\"{color}\"/>");
        }
        
        // Y-axis labels
        sb.AppendLine($"<text x=\"{margin - 5}\" y=\"{margin + 4}\" text-anchor=\"end\" font-size=\"8\">{maxCO:F2}°</text>");
        sb.AppendLine($"<text x=\"{margin - 5}\" y=\"{height - margin}\" text-anchor=\"end\" font-size=\"8\">{minCO:F2}°</text>");
        sb.AppendLine($"<text x=\"{margin - 5}\" y=\"{meanY:F1}\" text-anchor=\"end\" font-size=\"8\" fill=\"#FF9800\">Mean</text>");
        
        // Legend
        sb.AppendLine($"<circle cx=\"{width - 80}\" cy=\"{margin}\" r=\"4\" fill=\"#2ECC71\"/>");
        sb.AppendLine($"<text x=\"{width - 72}\" y=\"{margin + 4}\" font-size=\"8\">Accepted</text>");
        sb.AppendLine($"<circle cx=\"{width - 80}\" cy=\"{margin + 12}\" r=\"4\" fill=\"#E74C3C\"/>");
        sb.AppendLine($"<text x=\"{width - 72}\" y=\"{margin + 16}\" font-size=\"8\">Rejected</text>");
        
        sb.AppendLine("</svg>");
        return sb.ToString();
    }
    
    private void ComposeDecision(IContainer container, ValidationResult validation, CalibrationResult result, CalibrationProject project)
    {
        var bgColor = validation.Decision == "ACCEPTED" ? Colors.Green.Lighten4 : Colors.Red.Lighten4;
        var borderColor = validation.Decision == "ACCEPTED" ? Colors.Green.Darken1 : Colors.Red.Darken1;
        
        container.Border(2).BorderColor(borderColor).Background(bgColor).Padding(15).Column(col =>
        {
            col.Item().Text("DECISION").FontSize(14).Bold().FontColor(borderColor);
            col.Item().PaddingTop(5).Text(validation.Decision).FontSize(20).Bold().FontColor(borderColor);
            
            if (!string.IsNullOrEmpty(validation.Notes))
                col.Item().PaddingTop(5).Text($"Notes: {validation.Notes}").FontSize(10);
        });
    }
    
    private void ComposeDataTable(IContainer container, List<RovGyroDataPoint> dataPoints)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(col =>
        {
            col.Item().Text("DATA SUMMARY (First 40 Observations)").FontSize(12).Bold().FontColor(Colors.Orange.Darken2);
            
            if (!dataPoints.Any())
            {
                col.Item().Text("No data available").FontSize(10).FontColor(Colors.Grey.Medium);
                return;
            }
            
            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // Index
                    columns.RelativeColumn(2);     // Time
                    columns.RelativeColumn(1);     // Vessel Hdg
                    columns.RelativeColumn(1);     // Corrected Ref
                    columns.RelativeColumn(1);     // ROV Hdg
                    columns.RelativeColumn(1);     // C-O
                    columns.ConstantColumn(40);    // Status
                });
                
                table.Header(header =>
                {
                    header.Cell().Text("#").Bold().FontSize(7);
                    header.Cell().Text("Time").Bold().FontSize(7);
                    header.Cell().Text("Vessel").Bold().FontSize(7);
                    header.Cell().Text("Corr Ref").Bold().FontSize(7);
                    header.Cell().Text("ROV").Bold().FontSize(7);
                    header.Cell().Text("C-O").Bold().FontSize(7);
                    header.Cell().Text("St").Bold().FontSize(7);
                });
                
                foreach (var point in dataPoints)
                {
                    var statusColor = point.Status == PointStatus.Accepted ? Colors.Green.Darken1 : Colors.Red.Darken1;
                    
                    table.Cell().Text(point.Index.ToString()).FontSize(6);
                    table.Cell().Text(point.DateTime.ToString("HH:mm:ss")).FontSize(6);
                    table.Cell().Text($"{point.VesselHeading:F1}").FontSize(6);
                    table.Cell().Text($"{point.CorrectedReference:F1}").FontSize(6);
                    table.Cell().Text($"{point.RovHeading:F1}").FontSize(6);
                    table.Cell().Text($"{point.CalculatedCO:F3}").FontSize(6);
                    table.Cell().Text(point.Status == PointStatus.Accepted ? "✓" : "✗").FontColor(statusColor).FontSize(6);
                }
            });
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generated by Fathom OS - ROV Gyro Calibration Module").FontSize(8).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(100).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8);
                text.CurrentPageNumber().FontSize(8);
                text.Span(" of ").FontSize(8);
                text.TotalPages().FontSize(8);
            });
        });
    }
    
    #endregion
    
    #region Excel Export
    
    public void ExportExcel(string outputPath, CalibrationProject project,
        List<RovGyroDataPoint> dataPoints, CalibrationResult result, ValidationResult validation)
    {
        using var workbook = new XLWorkbook();
        
        CreateSummarySheet(workbook, project, result, validation);
        CreateConfigurationSheet(workbook, project, result);
        CreateDataSheet(workbook, dataPoints);
        CreateStatisticsSheet(workbook, result);
        
        workbook.SaveAs(outputPath);
    }
    
    private void CreateSummarySheet(XLWorkbook workbook, CalibrationProject project, 
        CalibrationResult result, ValidationResult validation)
    {
        var ws = workbook.Worksheets.Add("Summary");
        
        ws.Cell("A1").Value = "ROV GYRO CALIBRATION REPORT";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Range("A1:D1").Merge();
        
        int row = 3;
        ws.Cell(row, 1).Value = "PROJECT INFORMATION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Project Name:"; ws.Cell(row, 2).Value = project.ProjectName; row++;
        ws.Cell(row, 1).Value = "Client:"; ws.Cell(row, 2).Value = project.ClientName; row++;
        ws.Cell(row, 1).Value = "Vessel:"; ws.Cell(row, 2).Value = project.VesselName; row++;
        ws.Cell(row, 1).Value = "ROV:"; ws.Cell(row, 2).Value = project.RovName; row++;
        ws.Cell(row, 1).Value = "Survey Date:"; ws.Cell(row, 2).Value = project.SurveyDate; row++;
        ws.Cell(row, 1).Value = "Mode:"; ws.Cell(row, 2).Value = project.Purpose.ToString(); row++;
        
        row += 2;
        ws.Cell(row, 1).Value = "CALIBRATION RESULTS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Mean C-O (Accepted):"; 
        ws.Cell(row, 2).Value = result.MeanCOAccepted;
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0000°";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontSize = 14;
        row++;
        
        ws.Cell(row, 1).Value = "Standard Deviation:"; 
        ws.Cell(row, 2).Value = result.StdDevAccepted;
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0000°"; row++;
        
        ws.Cell(row, 1).Value = "Baseline Angle θ:"; 
        ws.Cell(row, 2).Value = result.BaselineAngleTheta;
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0000°"; row++;
        
        ws.Cell(row, 1).Value = "Rejection Rate:"; 
        ws.Cell(row, 2).Value = result.RejectionPercentage / 100;
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0%"; row++;
        
        row += 2;
        ws.Cell(row, 1).Value = "DECISION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = validation.Decision;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = validation.Decision == "ACCEPTED" 
            ? XLColor.LightGreen : XLColor.LightCoral;
        
        ws.Columns().AdjustToContents();
    }
    
    private void CreateConfigurationSheet(XLWorkbook workbook, CalibrationProject project, CalibrationResult result)
    {
        var ws = workbook.Worksheets.Add("ROV Configuration");
        var config = project.RovConfig;
        
        ws.Cell("A1").Value = "ROV GEOMETRIC CONFIGURATION";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        
        int row = 3;
        ws.Cell(row, 1).Value = "Setup Parameters"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = "Facing Direction:"; ws.Cell(row, 2).Value = config.FacingDirection.ToString(); row++;
        ws.Cell(row, 1).Value = "Baseline Side:"; ws.Cell(row, 2).Value = config.BaselineSide.ToString(); row++;
        ws.Cell(row, 1).Value = "Baseline Distance:"; ws.Cell(row, 2).Value = config.BaselineDistance; ws.Cell(row, 3).Value = "m"; row++;
        ws.Cell(row, 1).Value = "Baseline Offset (D):"; ws.Cell(row, 2).Value = config.BaselineOffset; ws.Cell(row, 3).Value = "°"; row++;
        ws.Cell(row, 1).Value = "Port Measurement (P):"; ws.Cell(row, 2).Value = config.PortMeasurement; ws.Cell(row, 3).Value = "m"; row++;
        ws.Cell(row, 1).Value = "Starboard Measurement (S):"; ws.Cell(row, 2).Value = config.StarboardMeasurement; ws.Cell(row, 3).Value = "m"; row++;
        
        row += 2;
        ws.Cell(row, 1).Value = "Calculated Corrections"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = "Baseline Angle (θ):"; ws.Cell(row, 2).Value = result.BaselineAngleTheta; ws.Cell(row, 3).Value = "°"; row++;
        ws.Cell(row, 1).Value = "Facing Direction Offset:"; ws.Cell(row, 2).Value = config.FacingDirectionOffset; ws.Cell(row, 3).Value = "°"; row++;
        
        row += 2;
        ws.Cell(row, 1).Value = "Correction Formula"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = "C = V + D + θ + Facing";
        ws.Cell(row, 1).Style.Font.FontName = "Consolas"; row++;
        ws.Cell(row, 1).Value = $"C = V + {config.BaselineOffset:F2}° + {result.BaselineAngleTheta:F4}° + {config.FacingDirectionOffset:F1}°";
        
        ws.Columns().AdjustToContents();
    }
    
    private void CreateDataSheet(XLWorkbook workbook, List<RovGyroDataPoint> dataPoints)
    {
        var ws = workbook.Worksheets.Add("Data");
        
        ws.Cell(1, 1).Value = "Index";
        ws.Cell(1, 2).Value = "DateTime";
        ws.Cell(1, 3).Value = "Vessel Heading (°)";
        ws.Cell(1, 4).Value = "V+D (°)";
        ws.Cell(1, 5).Value = "V+D+θ (°)";
        ws.Cell(1, 6).Value = "Corrected Ref (°)";
        ws.Cell(1, 7).Value = "ROV Heading (°)";
        ws.Cell(1, 8).Value = "C-O (°)";
        ws.Cell(1, 9).Value = "Z-Score";
        ws.Cell(1, 10).Value = "Status";
        
        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.Orange;
        
        int row = 2;
        foreach (var point in dataPoints)
        {
            ws.Cell(row, 1).Value = point.Index;
            ws.Cell(row, 2).Value = point.DateTime;
            ws.Cell(row, 3).Value = point.VesselHeading;
            ws.Cell(row, 4).Value = point.VPlusD;
            ws.Cell(row, 5).Value = point.VPlusDPlusTheta;
            ws.Cell(row, 6).Value = point.CorrectedReference;
            ws.Cell(row, 7).Value = point.RovHeading;
            ws.Cell(row, 8).Value = point.CalculatedCO;
            ws.Cell(row, 9).Value = point.ZScore;
            ws.Cell(row, 10).Value = point.Status.ToString();
            
            if (point.Status == PointStatus.Rejected)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightCoral;
            
            row++;
        }
        
        ws.Column(2).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
        for (int c = 3; c <= 8; c++)
            ws.Column(c).Style.NumberFormat.Format = "0.0000";
        ws.Column(9).Style.NumberFormat.Format = "0.00";
        
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }
    
    private void CreateStatisticsSheet(XLWorkbook workbook, CalibrationResult result)
    {
        var ws = workbook.Worksheets.Add("Statistics");
        
        ws.Cell("A1").Value = "ROV CALIBRATION STATISTICS";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        
        int row = 3;
        ws.Cell(row, 1).Value = "All Observations"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = "Count:"; ws.Cell(row, 2).Value = result.TotalObservations; row++;
        ws.Cell(row, 1).Value = "Mean C-O:"; ws.Cell(row, 2).Value = result.MeanCOAll; row++;
        ws.Cell(row, 1).Value = "Std Dev:"; ws.Cell(row, 2).Value = result.StdDevAll; row++;
        
        row += 2;
        ws.Cell(row, 1).Value = "Accepted Observations"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = "Count:"; ws.Cell(row, 2).Value = result.AcceptedCount; row++;
        ws.Cell(row, 1).Value = "Mean C-O:"; ws.Cell(row, 2).Value = result.MeanCOAccepted; row++;
        ws.Cell(row, 1).Value = "Std Dev:"; ws.Cell(row, 2).Value = result.StdDevAccepted; row++;
        
        row += 2;
        ws.Cell(row, 1).Value = "Geometric Corrections"; ws.Cell(row, 1).Style.Font.Bold = true; row++;
        ws.Cell(row, 1).Value = "Baseline Angle θ:"; ws.Cell(row, 2).Value = result.BaselineAngleTheta; row++;
        ws.Cell(row, 1).Value = "Baseline Offset D:"; ws.Cell(row, 2).Value = result.BaselineOffsetD; row++;
        ws.Cell(row, 1).Value = "Facing Offset:"; ws.Cell(row, 2).Value = result.FacingDirectionOffset; row++;
        
        ws.Column(2).Style.NumberFormat.Format = "0.0000";
        ws.Columns().AdjustToContents();
    }
    
    #endregion
    
    #region CSV Export
    
    public void ExportCsv(string outputPath, List<RovGyroDataPoint> dataPoints)
    {
        using var writer = new StreamWriter(outputPath);
        
        writer.WriteLine("Index,DateTime,VesselHeading,VPlusD,VPlusDPlusTheta,CorrectedRef,RovHeading,C-O,ZScore,Status");
        
        foreach (var point in dataPoints)
        {
            writer.WriteLine($"{point.Index},{point.DateTime:yyyy-MM-dd HH:mm:ss}," +
                           $"{point.VesselHeading:F4},{point.VPlusD:F4},{point.VPlusDPlusTheta:F4}," +
                           $"{point.CorrectedReference:F4},{point.RovHeading:F4}," +
                           $"{point.CalculatedCO:F4},{point.ZScore:F4},{point.Status}");
        }
    }
    
    #endregion
}
