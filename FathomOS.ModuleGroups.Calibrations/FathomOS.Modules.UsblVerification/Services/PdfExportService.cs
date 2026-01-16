using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.UsblVerification.Models;
using SkiaSharp;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// PDF export service for USBL verification reports
/// </summary>
public class PdfExportService
{
    public void Export(string filePath, UsblVerificationProject project, VerificationResults results)
    {
        // Set QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(container => CreateHeader(container, project));
                page.Content().Element(container => CreateContent(container, project, results));
                page.Footer().Element(CreateFooter);
            });
        }).GeneratePdf(filePath);
    }
    
    private void CreateHeader(IContainer container, UsblVerificationProject project)
    {
        container.Column(headerCol =>
        {
            headerCol.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("USBL VERIFICATION REPORT").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Project: {project.ProjectName}").FontSize(12);
                    col.Item().Text($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                });
            });
            
            headerCol.Item().PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
        });
    }
    
    private void CreateContent(IContainer container, UsblVerificationProject project, VerificationResults results)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Project Information
            col.Item().Element(c => CreateProjectInfoSection(c, project));
            col.Item().PaddingTop(15);
            
            // Spin Verification Results
            col.Item().Element(c => CreateSpinResultsSection(c, results));
            col.Item().PaddingTop(15);
            
            // Transit Verification Results
            col.Item().Element(c => CreateTransitResultsSection(c, results));
            col.Item().PaddingTop(15);
            
            // Alignment Verification
            col.Item().Element(c => CreateAlignmentSection(c, project, results));
            col.Item().PaddingTop(15);
            
            // Overall Result
            col.Item().Element(c => CreateOverallResultSection(c, results));
            col.Item().PaddingTop(15);
            
            // Charts
            col.Item().Element(c => CreateChartsSection(c, project, results));
        });
    }
    
    private void CreateProjectInfoSection(IContainer container, UsblVerificationProject project)
    {
        container.Column(col =>
        {
            col.Item().Text("PROJECT INFORMATION").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().PaddingTop(5);
            
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(innerCol =>
            {
                innerCol.Item().Row(row =>
                {
                    row.RelativeItem().Text("Project Name:").Bold();
                    row.RelativeItem(2).Text(project.ProjectName);
                    row.RelativeItem().Text("Client:").Bold();
                    row.RelativeItem(2).Text(project.ClientName);
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Vessel:").Bold();
                    row.RelativeItem(2).Text(project.VesselName);
                    row.RelativeItem().Text("Transponder:").Bold();
                    row.RelativeItem(2).Text(project.TransponderName);
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Transducer:").Bold();
                    row.RelativeItem(2).Text(project.TransducerLocation);
                    row.RelativeItem().Text("Survey Date:").Bold();
                    row.RelativeItem(2).Text(project.SurveyDate?.ToString("dd/MM/yyyy") ?? "");
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Processor:").Bold();
                    row.RelativeItem(2).Text(project.ProcessorName);
                    row.RelativeItem().Text("Comments:").Bold();
                    row.RelativeItem(2).Text(project.Comments);
                });
            });
        });
    }
    
    private void CreateSpinResultsSection(IContainer container, VerificationResults results)
    {
        container.Column(col =>
        {
            col.Item().Text("SPIN POSITION VERIFICATION").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().Text("Tolerance: ±0.5m or 0.2% of slant range").FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(5);
            
            // Per-heading results table
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                
                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Heading").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Easting (m)").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Northing (m)").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Depth (m)").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Diff E").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Diff N").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Radial").Bold();
                });
                
                foreach (var heading in results.HeadingResults)
                {
                    table.Cell().Padding(3).Text(heading.Heading);
                    table.Cell().Padding(3).Text($"{heading.Easting:F3}");
                    table.Cell().Padding(3).Text($"{heading.Northing:F3}");
                    table.Cell().Padding(3).Text($"{heading.Depth:F3}");
                    table.Cell().Padding(3).Text($"{heading.DiffEasting:F3}");
                    table.Cell().Padding(3).Text($"{heading.DiffNorthing:F3}");
                    table.Cell().Padding(3).Text($"{heading.DiffRadial:F3}");
                }
            });
            
            col.Item().PaddingTop(8);
            
            // Summary stats
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(innerCol =>
            {
                innerCol.Item().Row(row =>
                {
                    row.RelativeItem().Text("Overall Average:").Bold();
                    row.RelativeItem().Text($"E: {results.SpinOverallAverageEasting:F3}m");
                    row.RelativeItem().Text($"N: {results.SpinOverallAverageNorthing:F3}m");
                    row.RelativeItem().Text($"D: {results.SpinOverallAverageDepth:F3}m");
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("2σ Std Dev:").Bold();
                    row.RelativeItem().Text($"E: {results.SpinStdDevEasting2Sigma:F3}m");
                    row.RelativeItem().Text($"N: {results.SpinStdDevNorthing2Sigma:F3}m");
                    row.RelativeItem().Text($"D: {results.SpinStdDevDepth2Sigma:F3}m");
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Max Radial Diff:").Bold();
                    row.RelativeItem().Text($"{results.SpinMaxDiffRadial:F3}m");
                    row.RelativeItem().Text("2DRMS:").Bold();
                    row.RelativeItem().Text($"{results.Spin2DRMS:F3}m");
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Max Allowable:").Bold();
                    row.RelativeItem().Text($"{results.SpinMaxAllowableDiff:F3}m");
                    row.RelativeItem().Text("Result:").Bold();
                    row.RelativeItem().Text(results.SpinPassFail ? "PASS" : "FAIL")
                        .Bold()
                        .FontColor(results.SpinPassFail ? Colors.Green.Darken2 : Colors.Red.Darken2);
                });
            });
        });
    }
    
    private void CreateTransitResultsSection(IContainer container, VerificationResults results)
    {
        container.Column(col =>
        {
            col.Item().Text("TRANSIT POSITION VERIFICATION").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().Text("Tolerance: ±1m or 0.5% of slant range").FontSize(9).FontColor(Colors.Grey.Darken1);
            col.Item().PaddingTop(5);
            
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(innerCol =>
            {
                innerCol.Item().Row(row =>
                {
                    row.RelativeItem().Text("Combined Average:").Bold();
                    row.RelativeItem().Text($"E: {results.TransitCombinedAverageEasting:F3}m");
                    row.RelativeItem().Text($"N: {results.TransitCombinedAverageNorthing:F3}m");
                    row.RelativeItem().Text($"D: {results.TransitCombinedAverageDepth:F3}m");
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("2σ Std Dev:").Bold();
                    row.RelativeItem().Text($"E: {results.TransitStdDevEasting2Sigma:F3}m");
                    row.RelativeItem().Text($"N: {results.TransitStdDevNorthing2Sigma:F3}m");
                    row.RelativeItem().Text("");
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Max Diff from Spin:").Bold();
                    row.RelativeItem().Text($"{results.TransitMaxDiffFromSpin:F3}m");
                    row.RelativeItem().Text("2DRMS:").Bold();
                    row.RelativeItem().Text($"{results.Transit2DRMS:F3}m");
                });
                
                innerCol.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("Max Allowable:").Bold();
                    row.RelativeItem().Text($"{results.TransitMaxAllowableSpread:F3}m");
                    row.RelativeItem().Text("Result:").Bold();
                    row.RelativeItem().Text(results.TransitPassFail ? "PASS" : "FAIL")
                        .Bold()
                        .FontColor(results.TransitPassFail ? Colors.Green.Darken2 : Colors.Red.Darken2);
                });
            });
        });
    }
    
    private void CreateAlignmentSection(IContainer container, UsblVerificationProject project, VerificationResults results)
    {
        container.Column(col =>
        {
            col.Item().Text("ALIGNMENT VERIFICATION").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().PaddingTop(5);
            
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Parameter").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Line 1 (Roll)").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Line 2 (Pitch)").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text("Tolerance").Bold();
                });
                
                table.Cell().Padding(3).Text("Residual Alignment (°)");
                table.Cell().Padding(3).Text($"{results.Line1ResidualAlignment:F3}");
                table.Cell().Padding(3).Text($"{results.Line2ResidualAlignment:F3}");
                table.Cell().Padding(3).Text($"±{project.AlignmentTolerance:F1}°");
                
                table.Cell().Padding(3).Text("Alignment Result");
                table.Cell().Padding(3).Text(results.Line1AlignmentPass ? "PASS" : "FAIL")
                    .FontColor(results.Line1AlignmentPass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                table.Cell().Padding(3).Text(results.Line2AlignmentPass ? "PASS" : "FAIL")
                    .FontColor(results.Line2AlignmentPass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                table.Cell().Padding(3).Text("");
                
                table.Cell().Padding(3).Text("Scale Factor");
                table.Cell().Padding(3).Text($"{results.Line1ScaleFactor:F4}");
                table.Cell().Padding(3).Text($"{results.Line2ScaleFactor:F4}");
                table.Cell().Padding(3).Text($"1 ± {project.ScaleFactorTolerance:F3}");
                
                table.Cell().Padding(3).Text("Scale Factor Result");
                table.Cell().Padding(3).Text(results.Line1ScalePass ? "PASS" : "FAIL")
                    .FontColor(results.Line1ScalePass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                table.Cell().Padding(3).Text(results.Line2ScalePass ? "PASS" : "FAIL")
                    .FontColor(results.Line2ScalePass ? Colors.Green.Darken2 : Colors.Red.Darken2);
                table.Cell().Padding(3).Text("");
            });
        });
    }
    
    private void CreateOverallResultSection(IContainer container, VerificationResults results)
    {
        var bgColor = results.OverallPass ? Colors.Green.Lighten4 : Colors.Red.Lighten4;
        var textColor = results.OverallPass ? Colors.Green.Darken2 : Colors.Red.Darken2;
        
        container.Background(bgColor).Padding(15).Column(col =>
        {
            col.Item().AlignCenter().Text("OVERALL VERIFICATION RESULT").FontSize(14).Bold().FontColor(textColor);
            col.Item().PaddingTop(5).AlignCenter().Text(results.OverallStatus).FontSize(24).Bold().FontColor(textColor);
        });
    }
    
    private void CreateChartsSection(IContainer container, UsblVerificationProject project, VerificationResults results)
    {
        container.Column(col =>
        {
            col.Item().Text("POSITION PLOTS").FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().PaddingTop(5);
            col.Item().Text("Note: For detailed position plots, please export charts as PNG from the application.")
                .FontSize(9).FontColor(Colors.Grey.Darken1);
            
            // Create simple visual indicator
            col.Item().PaddingTop(10).Height(150).Canvas((object canvasObj, QuestPDF.Infrastructure.Size size) =>
            {
                var canvas = (SKCanvas)canvasObj;
                
                using var bgPaint = new SKPaint { Color = SKColors.WhiteSmoke };
                canvas.DrawRect(0, 0, (float)size.Width, (float)size.Height, bgPaint);
                
                float cx = (float)size.Width / 4;
                float cy = (float)size.Height / 2;
                float radius = Math.Min(cx, cy) - 20;
                
                // Spin circle
                using var circlePaint = new SKPaint 
                { 
                    Color = SKColors.Red, 
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2
                };
                canvas.DrawCircle(cx, cy, radius, circlePaint);
                
                // Center marker
                using var centerPaint = new SKPaint 
                { 
                    Color = SKColors.Black,
                    StrokeWidth = 2
                };
                canvas.DrawLine(cx - 10, cy, cx + 10, cy, centerPaint);
                canvas.DrawLine(cx, cy - 10, cx, cy + 10, centerPaint);
                
                // Label
                using var textPaint = new SKPaint 
                { 
                    Color = SKColors.Black,
                    TextSize = 12,
                    IsAntialias = true
                };
                canvas.DrawText("Spin Tolerance Circle", cx - 50, cy + radius + 15, textPaint);
                
                // Transit
                float tx = (float)size.Width * 3 / 4;
                canvas.DrawCircle(tx, cy, radius, circlePaint);
                canvas.DrawLine(tx - 10, cy, tx + 10, cy, centerPaint);
                canvas.DrawLine(tx, cy - 10, tx, cy + 10, centerPaint);
                canvas.DrawText("Transit Tolerance Circle", tx - 55, cy + radius + 15, textPaint);
            });
        });
    }
    
    private void CreateFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten1).PaddingTop(5).Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Fathom OS - USBL Verification Module").FontSize(8).FontColor(Colors.Grey.Darken1);
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
}
