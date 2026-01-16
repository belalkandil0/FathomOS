// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Export/PdfReportGenerator.cs
// Purpose: PDF report generation using QuestPDF (from Core)
// Note: Uses QuestPDF as per integration guide - NO PdfSharp or iTextSharp
// ============================================================================

using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.SurveyLogbook.Models;

// Alias for QuestPDF Color to avoid conflicts with System.Drawing.Color
using QuestColor = QuestPDF.Infrastructure.Color;

namespace FathomOS.Modules.SurveyLogbook.Export;

/// <summary>
/// PDF report generator for survey logbook data using QuestPDF.
/// Creates professionally formatted PDF reports.
/// </summary>
public class PdfReportGenerator
{
    #region Fields

    private readonly PdfReportOptions _options;
    private readonly string _primaryColor = "#1E3A5F";
    private readonly string _accentColor = "#4A90D9";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the PdfReportGenerator class.
    /// </summary>
    public PdfReportGenerator() : this(new PdfReportOptions()) { }

    /// <summary>
    /// Initializes a new instance of the PdfReportGenerator class with specified options.
    /// </summary>
    public PdfReportGenerator(PdfReportOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        
        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates a PDF report from survey log data.
    /// </summary>
    public void Generate(string filePath, SurveyLogFile logFile)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        if (logFile == null)
            throw new ArgumentNullException(nameof(logFile));

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(container => ComposeHeader(container, logFile));
                page.Content().Element(container => ComposeContent(container, logFile));
                page.Footer().Element(ComposeFooter);
            });
        });

        document.GeneratePdf(filePath);
    }

    /// <summary>
    /// Generates a DPR (Daily Progress Report) PDF.
    /// </summary>
    public void GenerateDprReport(string filePath, DprReport report)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        if (report == null)
            throw new ArgumentNullException(nameof(report));

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(container => ComposeDprHeader(container, report));
                page.Content().Element(container => ComposeDprContent(container, report));
                page.Footer().Element(ComposeFooter);
            });
        });

        document.GeneratePdf(filePath);
    }

    #endregion

    #region Private Methods - Survey Log Report

    private void ComposeHeader(IContainer container, SurveyLogFile logFile)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item()
                    .Text("SURVEY ELECTRONIC LOGBOOK")
                    .FontSize(18)
                    .Bold()
                    .FontColor(QuestColor.FromHex(_primaryColor));

                col.Item()
                    .Text(logFile.ProjectName ?? "Survey Log Report")
                    .FontSize(14)
                    .FontColor(QuestColor.FromHex(_accentColor));

                col.Item()
                    .PaddingTop(5)
                    .Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(100).Column(col =>
            {
                col.Item().AlignRight().Text($"Vessel: {logFile.VesselName ?? "-"}").FontSize(9);
                col.Item().AlignRight().Text($"Client: {logFile.ClientName ?? "-"}").FontSize(9);
            });
        });

        container.PaddingVertical(10).LineHorizontal(1).LineColor(QuestColor.FromHex(_primaryColor));
    }

    private void ComposeContent(IContainer container, SurveyLogFile logFile)
    {
        container.Column(col =>
        {
            // Summary section
            col.Item().PaddingBottom(15).Element(c => ComposeSummarySection(c, logFile));

            // Log entries table
            if (logFile.LogEntries.Any())
            {
                col.Item().Element(c => ComposeLogEntriesTable(c, logFile.LogEntries));
            }
        });
    }

    private void ComposeSummarySection(IContainer container, SurveyLogFile logFile)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
        {
            col.Item().Text("Summary").FontSize(12).Bold().FontColor(QuestColor.FromHex(_primaryColor));
            col.Item().PaddingTop(5);

            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Total Entries: {logFile.LogEntries.Count()}");
                    c.Item().Text($"Position Fixes: {logFile.PositionFixes.Count}");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"DVR Recordings: {logFile.DvrRecordings.Count}");
                    c.Item().Text($"DPR Reports: {logFile.DprReports.Count}");
                });
                row.RelativeItem().Column(c =>
                {
                    if (logFile.LogEntries.Any())
                    {
                        var firstEntry = logFile.LogEntries.Min(e => e.Timestamp);
                        var lastEntry = logFile.LogEntries.Max(e => e.Timestamp);
                        c.Item().Text($"First Entry: {firstEntry:dd/MM/yyyy HH:mm}");
                        c.Item().Text($"Last Entry: {lastEntry:dd/MM/yyyy HH:mm}");
                    }
                });
            });
        });
    }

    private void ComposeLogEntriesTable(IContainer container, IEnumerable<SurveyLogEntry> entries)
    {
        container.Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(65);  // Date
                columns.ConstantColumn(55);  // Time
                columns.ConstantColumn(80);  // Type
                columns.RelativeColumn(3);   // Description
                columns.ConstantColumn(70);  // Easting
                columns.ConstantColumn(80);  // Northing
                columns.ConstantColumn(50);  // Source
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Date").Bold();
                header.Cell().Element(CellStyle).Text("Time").Bold();
                header.Cell().Element(CellStyle).Text("Type").Bold();
                header.Cell().Element(CellStyle).Text("Description").Bold();
                header.Cell().Element(CellStyle).Text("Easting").Bold();
                header.Cell().Element(CellStyle).Text("Northing").Bold();
                header.Cell().Element(CellStyle).Text("Source").Bold();

                static IContainer CellStyle(IContainer container) =>
                    container.DefaultTextStyle(x => x.FontColor(Colors.White))
                             .Background(QuestColor.FromHex("#1E3A5F"))
                             .Padding(5);
            });

            // Data rows
            var limitedEntries = entries.Take(_options.MaxLogEntries);
            foreach (var entry in limitedEntries)
            {
                table.Cell().Element(DataCellStyle).Text(entry.Timestamp.ToString("dd/MM/yyyy"));
                table.Cell().Element(DataCellStyle).Text(entry.Timestamp.ToString("HH:mm:ss"));
                table.Cell().Element(DataCellStyle).Text(GetShortTypeName(entry.EntryType));
                table.Cell().Element(DataCellStyle).Text(TruncateText(entry.Description, 50));
                table.Cell().Element(DataCellStyle).Text(entry.Easting?.ToString("F2") ?? "-");
                table.Cell().Element(DataCellStyle).Text(entry.Northing?.ToString("F2") ?? "-");
                table.Cell().Element(DataCellStyle).Text(TruncateText(entry.Source, 10));
            }

            static IContainer DataCellStyle(IContainer container) =>
                container.BorderBottom(1)
                         .BorderColor(Colors.Grey.Lighten2)
                         .Padding(4);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("Fathom OS Survey Electronic Logbook").FontSize(8).FontColor(Colors.Grey.Medium);
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

    #endregion

    #region Private Methods - DPR Report

    private void ComposeDprHeader(IContainer container, DprReport report)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item()
                        .Text("DAILY PROGRESS REPORT")
                        .FontSize(16)
                        .Bold()
                        .FontColor(QuestColor.FromHex(_primaryColor));
                    c.Item()
                        .Text($"Date: {report.ReportDate:dd MMMM yyyy} - {report.Shift}")
                        .FontSize(12);
                });

                row.ConstantItem(120).AlignRight().Column(c =>
                {
                    c.Item().Text($"Report #{report.ReportNumber}").FontSize(10);
                });
            });

            col.Item().PaddingVertical(8).LineHorizontal(2).LineColor(QuestColor.FromHex(_primaryColor));
        });
    }

    private void ComposeDprContent(IContainer container, DprReport report)
    {
        container.Column(col =>
        {
            // Project Info Section
            col.Item().Element(c => ComposeDprProjectInfo(c, report));

            // Personnel Section
            col.Item().PaddingTop(10).Element(c => ComposeDprPersonnel(c, report));

            // Crew Section
            if (report.CrewOnDuty?.Any() == true)
            {
                col.Item().PaddingTop(10).Element(c => ComposeDprCrew(c, report));
            }

            // Operations Section
            col.Item().PaddingTop(10).Element(c => ComposeDprOperations(c, report));
        });
    }

    private void ComposeDprProjectInfo(IContainer container, DprReport report)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Column(col =>
        {
            col.Item().Background(QuestColor.FromHex(_primaryColor)).Padding(5)
                .Text("PROJECT INFORMATION").FontSize(10).Bold().FontColor(Colors.White);

            col.Item().Padding(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Client: {report.Client ?? "-"}");
                    c.Item().Text($"Vessel: {report.Vessel ?? "-"}");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Project No: {report.ProjectNumber ?? "-"}");
                    c.Item().Text($"Location: {report.LocationDepth ?? "-"}");
                });
            });
        });
    }

    private void ComposeDprPersonnel(IContainer container, DprReport report)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Column(col =>
        {
            col.Item().Background(QuestColor.FromHex(_primaryColor)).Padding(5)
                .Text("KEY PERSONNEL").FontSize(10).Bold().FontColor(Colors.White);

            col.Item().Padding(8).Row(row =>
            {
                row.RelativeItem().Text($"Offshore Manager: {report.OffshoreManager ?? "-"}");
                row.RelativeItem().Text($"Project Surveyor: {report.ProjectSurveyor ?? "-"}");
                row.RelativeItem().Text($"Party Chief: {report.PartyChief ?? "-"}");
            });
        });
    }

    private void ComposeDprCrew(IContainer container, DprReport report)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Column(col =>
        {
            col.Item().Background(QuestColor.FromHex(_primaryColor)).Padding(5)
                .Text($"CREW ON DUTY ({report.SurveyCrew?.Count ?? 0})").FontSize(10).Bold().FontColor(Colors.White);

            col.Item().Padding(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                foreach (var crew in report.SurveyCrew ?? Enumerable.Empty<CrewMember>())
                {
                    table.Cell().Padding(2).Text($"{crew.Name} ({crew.Rank})").FontSize(8);
                }
            });
        });
    }

    private void ComposeDprOperations(IContainer container, DprReport report)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Column(col =>
        {
            col.Item().Background(QuestColor.FromHex(_primaryColor)).Padding(5)
                .Text("OPERATIONS SUMMARY").FontSize(10).Bold().FontColor(Colors.White);

            col.Item().Padding(8).Column(c =>
            {
                c.Item().Text("Last 24 Hours Highlights:").Bold().FontSize(9);
                c.Item().PaddingLeft(10).Text(report.Last24HrsHighlights ?? "No highlights recorded.").FontSize(9);

                c.Item().PaddingTop(8).Text("Known Issues / Outstanding Actions:").Bold().FontSize(9);
                c.Item().PaddingLeft(10).Text(report.KnownIssues ?? "No issues recorded.").FontSize(9);

                if (!string.IsNullOrEmpty(report.GeneralSurveyComments))
                {
                    c.Item().PaddingTop(8).Text("General Survey Comments:").Bold().FontSize(9);
                    c.Item().PaddingLeft(10).Text(report.GeneralSurveyComments).FontSize(9);
                }
            });
        });
    }

    #endregion

    #region Helper Methods

    private static string GetShortTypeName(LogEntryType entryType)
    {
        return entryType switch
        {
            LogEntryType.DvrRecordingStart => "DVR Start",
            LogEntryType.DvrRecordingEnd => "DVR End",
            LogEntryType.DvrImageCaptured => "DVR Image",
            LogEntryType.PositionFix => "Pos Fix",
            LogEntryType.CalibrationFix => "Cal Fix",
            LogEntryType.VerificationFix => "Ver Fix",
            LogEntryType.SetEastingNorthing => "Set E/N",
            LogEntryType.NaviPacLoggingStart => "NP Start",
            LogEntryType.NaviPacLoggingEnd => "NP End",
            LogEntryType.NaviPacEvent => "NP Event",
            LogEntryType.WaypointAdded => "WP Add",
            LogEntryType.WaypointDeleted => "WP Del",
            LogEntryType.WaypointModified => "WP Mod",
            LogEntryType.ManualEntry => "Manual",
            _ => entryType.ToString()
        };
    }

    private static string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "-";
        return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
    }

    #endregion
}

/// <summary>
/// Options for PDF report generation.
/// </summary>
public class PdfReportOptions
{
    /// <summary>
    /// Maximum number of log entries to include in the report.
    /// </summary>
    public int MaxLogEntries { get; set; } = 500;

    /// <summary>
    /// Include position fixes in the report.
    /// </summary>
    public bool IncludePositionFixes { get; set; } = true;

    /// <summary>
    /// Include DVR recordings in the report.
    /// </summary>
    public bool IncludeDvrRecordings { get; set; } = true;

    /// <summary>
    /// Include DPR reports in the report.
    /// </summary>
    public bool IncludeDprReports { get; set; } = true;
}
