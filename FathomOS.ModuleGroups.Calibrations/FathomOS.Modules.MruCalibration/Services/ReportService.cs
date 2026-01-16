using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using FathomOS.Modules.MruCalibration.Models;

namespace FathomOS.Modules.MruCalibration.Services;

/// <summary>
/// PDF Report generator using QuestPDF with Subsea7 branding
/// </summary>
public class ReportService
{
    // Subsea7 brand colors
    private static readonly string Subsea7Orange = "#E85D00";
    private static readonly string Subsea7Teal = "#009688";
    private static readonly string HeaderBg = "#1A1A1A";
    private static readonly string TableHeaderBg = "#2D2D30";
    private static readonly string TableRowAlt = "#252526";
    private static readonly string TextPrimary = "#FFFFFF";
    private static readonly string TextSecondary = "#B0B0B0";
    
    #region Public Methods
    
    /// <summary>
    /// Generate PDF calibration report
    /// </summary>
    public void GenerateReport(string outputPath, CalibrationSession session)
    {
        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));
                
                page.Header().Element(c => ComposeHeader(c, session));
                page.Content().Element(c => ComposeContent(c, session));
                page.Footer().Element(ComposeFooter);
            });
        });
        
        document.GeneratePdf(outputPath);
    }
    
    #endregion
    
    #region Header
    
    private void ComposeHeader(IContainer container, CalibrationSession session)
    {
        container.Column(column =>
        {
            // Title bar
            column.Item().Background(QuestPDF.Infrastructure.Color.FromHex(Subsea7Orange)).Padding(15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("MRU CALIBRATION REPORT")
                        .FontSize(18).Bold().FontColor(Colors.White);
                    col.Item().Text($"{session.ProjectInfo.Purpose} Exercise")
                        .FontSize(12).FontColor(Colors.White);
                });
                
                row.ConstantItem(120).AlignRight().AlignMiddle().Text("FATHOM OS")
                    .FontSize(14).Bold().FontColor(Colors.White);
            });
            
            // Project info bar
            column.Item().Background(QuestPDF.Infrastructure.Color.FromHex(HeaderBg)).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Project: {session.ProjectInfo.ProjectTitle}")
                        .FontSize(10).FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary));
                    col.Item().Text($"Vessel: {session.ProjectInfo.VesselName}")
                        .FontSize(9).FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary));
                });
                
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text($"Date: {session.CreatedAt:dd MMM yyyy}")
                        .FontSize(10).FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary));
                    col.Item().Text($"Report Generated: {DateTime.Now:dd MMM yyyy HH:mm}")
                        .FontSize(9).FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary));
                });
            });
            
            column.Item().Height(10);
        });
    }
    
    #endregion
    
    #region Content
    
    private void ComposeContent(IContainer container, CalibrationSession session)
    {
        container.Column(column =>
        {
            // Project Details Section
            column.Item().Element(c => ComposeProjectDetails(c, session.ProjectInfo));
            column.Item().Height(15);
            
            // MRU Systems Section
            column.Item().Element(c => ComposeMruSystems(c, session.ProjectInfo));
            column.Item().Height(15);
            
            // Pitch Results (if available)
            if (session.PitchData.IsProcessed && session.PitchData.Statistics != null)
            {
                column.Item().Element(c => ComposeSensorResults(c, session.PitchData, "PITCH"));
                column.Item().Height(15);
            }
            
            // Roll Results (if available)
            if (session.RollData.IsProcessed && session.RollData.Statistics != null)
            {
                column.Item().Element(c => ComposeSensorResults(c, session.RollData, "ROLL"));
                column.Item().Height(15);
            }
            
            // Sign-off Section
            column.Item().Element(c => ComposeSignOff(c, session));
        });
    }
    
    private void ComposeProjectDetails(IContainer container, ProjectInfo info)
    {
        container.Column(column =>
        {
            column.Item().Text("PROJECT DETAILS")
                .FontSize(12).Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(Subsea7Orange));
            column.Item().Height(5);
            
            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(2);
                });
                
                // Row 1
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(5)
                    .Text("Project Title:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                table.Cell().Padding(5).Text(info.ProjectTitle ?? "-").FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(5)
                    .Text("Project No:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                table.Cell().Padding(5).Text(info.ProjectNumber ?? "-").FontSize(9);
                
                // Row 2
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(5)
                    .Text("Vessel:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(5)
                    .Text(info.VesselName ?? "-").FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(5)
                    .Text("Location:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(5)
                    .Text(info.Location ?? "-").FontSize(9);
                
                // Row 3
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(5)
                    .Text("Observed By:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                table.Cell().Padding(5).Text(info.ObservedBy ?? "-").FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(5)
                    .Text("Checked By:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                table.Cell().Padding(5).Text(info.CheckedBy ?? "-").FontSize(9);
            });
        });
    }
    
    private void ComposeMruSystems(IContainer container, ProjectInfo info)
    {
        container.Column(column =>
        {
            column.Item().Text("MRU SYSTEMS")
                .FontSize(12).Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(Subsea7Orange));
            column.Item().Height(5);
            
            column.Item().Row(row =>
            {
                // Reference System
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Column(col =>
                {
                    col.Item().Background(QuestPDF.Infrastructure.Color.FromHex(Subsea7Teal)).Padding(8)
                        .Text("REFERENCE SYSTEM").Bold().FontColor(Colors.White).FontSize(10);
                    col.Item().Padding(8).Column(inner =>
                    {
                        inner.Item().Text($"Model: {info.ReferenceSystem.Model ?? "-"}").FontSize(9);
                        inner.Item().Text($"Serial: {info.ReferenceSystem.SerialNumber ?? "-"}").FontSize(9);
                    });
                });
                
                row.ConstantItem(10);
                
                // Verified System
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Column(col =>
                {
                    col.Item().Background(QuestPDF.Infrastructure.Color.FromHex(Subsea7Orange)).Padding(8)
                        .Text("VERIFIED SYSTEM").Bold().FontColor(Colors.White).FontSize(10);
                    col.Item().Padding(8).Column(inner =>
                    {
                        inner.Item().Text($"Model: {info.VerifiedSystem.Model ?? "-"}").FontSize(9);
                        inner.Item().Text($"Serial: {info.VerifiedSystem.SerialNumber ?? "-"}").FontSize(9);
                    });
                });
            });
        });
    }
    
    private void ComposeSensorResults(IContainer container, SensorCalibrationData data, string sensorName)
    {
        var stats = data.Statistics!;
        
        container.Column(column =>
        {
            column.Item().Text($"{sensorName} CALIBRATION RESULTS")
                .FontSize(12).Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(Subsea7Orange));
            column.Item().Height(5);
            
            // Results table
            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                });
                
                // Header row
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("Start Time").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("End Time").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("Observations").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("Rejections").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                
                // Data row
                table.Cell().Padding(8).Text(stats.StartTime.ToString("HH:mm:ss")).FontSize(9);
                table.Cell().Padding(8).Text(stats.EndTime.ToString("HH:mm:ss")).FontSize(9);
                table.Cell().Padding(8).Text(stats.TotalObservations.ToString()).FontSize(9);
                table.Cell().Padding(8).Text($"{stats.RejectedCount} ({stats.RejectionPercentage:F1}%)").FontSize(9);
            });
            
            column.Item().Height(5);
            
            // Statistics table
            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                });
                
                // Header row
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("Min C-O").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("Max C-O").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("Mean C-O").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                    .Text("Std Dev").Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(TextPrimary)).FontSize(9);
                
                // Data row
                table.Cell().Padding(8).Text($"{stats.MinCO:F4}°").FontSize(9);
                table.Cell().Padding(8).Text($"{stats.MaxCO:F4}°").FontSize(9);
                table.Cell().Padding(8).Text($"{stats.MeanCO_Accepted:F4}°").FontSize(9)
                    .Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(Subsea7Orange));
                table.Cell().Padding(8).Text($"{stats.StdDevCO_Accepted:F4}°").FontSize(9);
            });
            
            // Decision box
            column.Item().Height(10);
            column.Item().Background(data.Decision == AcceptanceDecision.Accepted 
                    ? QuestPDF.Infrastructure.Color.FromHex("#4CAF50") 
                    : data.Decision == AcceptanceDecision.Rejected 
                        ? QuestPDF.Infrastructure.Color.FromHex("#F44336") 
                        : QuestPDF.Infrastructure.Color.FromHex("#808080"))
                .Padding(10).Row(row =>
                {
                    row.RelativeItem().Text($"DECISION: {data.Decision.ToString().ToUpper()}")
                        .Bold().FontColor(Colors.White).FontSize(11);
                    row.ConstantItem(150).AlignRight()
                        .Text($"C-O Value: {stats.MeanCO_Accepted:F4}°")
                        .Bold().FontColor(Colors.White).FontSize(11);
                });
        });
    }
    
    private void ComposeSignOff(IContainer container, CalibrationSession session)
    {
        container.Column(column =>
        {
            column.Item().Text("SIGN-OFF")
                .FontSize(12).Bold().FontColor(QuestPDF.Infrastructure.Color.FromHex(Subsea7Orange));
            column.Item().Height(5);
            
            column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(2);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(2);
                });
                
                // Pitch sign-off
                if (session.PitchData.IsProcessed)
                {
                    table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                        .Text("Pitch Surveyor:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                    table.Cell().Padding(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Text(session.PitchData.SurveyorInitials ?? "________________").FontSize(9);
                    table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableHeaderBg)).Padding(8)
                        .Text("Witness:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                    table.Cell().Padding(8).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Text(session.PitchData.WitnessInitials ?? "________________").FontSize(9);
                }
                
                // Roll sign-off
                if (session.RollData.IsProcessed)
                {
                    table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(8)
                        .Text("Roll Surveyor:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                    table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(8)
                        .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Text(session.RollData.SurveyorInitials ?? "________________").FontSize(9);
                    table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(8)
                        .Text("Witness:").FontColor(QuestPDF.Infrastructure.Color.FromHex(TextSecondary)).FontSize(9);
                    table.Cell().Background(QuestPDF.Infrastructure.Color.FromHex(TableRowAlt)).Padding(8)
                        .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Text(session.RollData.WitnessInitials ?? "________________").FontSize(9);
                }
            });
            
            // Comments
            if (!string.IsNullOrEmpty(session.PitchData.Comments) || !string.IsNullOrEmpty(session.RollData.Comments))
            {
                column.Item().Height(10);
                column.Item().Text("COMMENTS").FontSize(10).Bold();
                column.Item().Height(3);
                
                if (!string.IsNullOrEmpty(session.PitchData.Comments))
                {
                    column.Item().Text($"Pitch: {session.PitchData.Comments}").FontSize(9);
                }
                if (!string.IsNullOrEmpty(session.RollData.Comments))
                {
                    column.Item().Text($"Roll: {session.RollData.Comments}").FontSize(9);
                }
            }
        });
    }
    
    #endregion
    
    #region Footer
    
    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Generated by Fathom OS MRU Calibration Module")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
            
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" of ");
                text.TotalPages();
            });
        });
    }
    
    #endregion
}
