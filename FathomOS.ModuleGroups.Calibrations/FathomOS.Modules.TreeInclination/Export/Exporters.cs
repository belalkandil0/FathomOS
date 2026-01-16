namespace FathomOS.Modules.TreeInclination.Export;

using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.TreeInclination.Models;
using SkiaSharp;

/// <summary>Excel export using ClosedXML</summary>
public class ExcelExporter
{
    public void Export(string filePath, InclinationProject project)
    {
        using var workbook = new XLWorkbook();

        CreateSummarySheet(workbook, project);
        CreateInputDataSheet(workbook, project);
        CreateCalculationsSheet(workbook, project);

        workbook.SaveAs(filePath);
    }

    private void CreateSummarySheet(XLWorkbook workbook, InclinationProject project)
    {
        var ws = workbook.Worksheets.Add("Summary");
        var result = project.Result;

        ws.Cell("A1").Value = "TREE INCLINATION REPORT";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Range("A1:D1").Merge();

        int row = 3;
        ws.Cell(row, 1).Value = "Project Information";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        ws.Cell(row, 1).Value = "Project:"; ws.Cell(row, 2).Value = project.ProjectName; row++;
        ws.Cell(row, 1).Value = "Client:"; ws.Cell(row, 2).Value = project.ClientName; row++;
        ws.Cell(row, 1).Value = "Vessel:"; ws.Cell(row, 2).Value = project.VesselName; row++;
        ws.Cell(row, 1).Value = "Structure:"; ws.Cell(row, 2).Value = project.StructureName; row++;
        ws.Cell(row, 1).Value = "Date:"; ws.Cell(row, 2).Value = project.SurveyDate.ToString("yyyy-MM-dd"); row++;
        ws.Cell(row, 1).Value = "Surveyor:"; ws.Cell(row, 2).Value = project.SurveyorName; row++;

        row += 2;

        if (result != null)
        {
            ws.Cell(row, 1).Value = "Results";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Cell(row, 1).Value = "Total Inclination:";
            ws.Cell(row, 2).Value = $"{result.TotalInclination:F3}°";
            ws.Cell(row, 3).Value = result.InclinationStatus.ToString();
            StyleStatus(ws.Cell(row, 3), result.InclinationStatus);
            row++;

            ws.Cell(row, 1).Value = "Heading:";
            ws.Cell(row, 2).Value = $"{result.InclinationHeading:F2}° ({result.HeadingDescription})";
            row++;

            ws.Cell(row, 1).Value = "Pitch:";
            ws.Cell(row, 2).Value = $"{result.AveragePitch:F3}°";
            row++;

            ws.Cell(row, 1).Value = "Roll:";
            ws.Cell(row, 2).Value = $"{result.AverageRoll:F3}°";
            row++;

            ws.Cell(row, 1).Value = "Misclosure:";
            ws.Cell(row, 2).Value = $"{result.Misclosure:F3} m";
            ws.Cell(row, 3).Value = result.MisclosureStatus.ToString();
            StyleStatus(ws.Cell(row, 3), result.MisclosureStatus);
            row++;

            ws.Cell(row, 1).Value = "Out of Plane:";
            ws.Cell(row, 2).Value = $"{result.OutOfPlane:F3} m";
            ws.Cell(row, 3).Value = result.OutOfPlaneStatus.ToString();
            StyleStatus(ws.Cell(row, 3), result.OutOfPlaneStatus);
            row++;

            ws.Cell(row, 1).Value = "Method:";
            ws.Cell(row, 2).Value = result.CalculationMethod;
        }

        ws.Columns().AdjustToContents();
    }

    private void CreateInputDataSheet(XLWorkbook workbook, InclinationProject project)
    {
        var ws = workbook.Worksheets.Add("Input Data");

        // Header
        ws.Cell("A1").Value = "Corner";
        ws.Cell("B1").Value = "X (m)";
        ws.Cell("C1").Value = "Y (m)";
        ws.Cell("D1").Value = "Raw Depth (m)";
        ws.Cell("E1").Value = "Tide Corr (m)";
        ws.Cell("F1").Value = "Corrected Depth (m)";
        ws.Cell("G1").Value = "Std Dev";
        ws.Cell("H1").Value = "Source File";

        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var corner in project.Corners.OrderBy(c => c.Index))
        {
            ws.Cell(row, 1).Value = corner.Name + (corner.IsClosurePoint ? " (CLS)" : "");
            ws.Cell(row, 2).Value = corner.X;
            ws.Cell(row, 3).Value = corner.Y;
            ws.Cell(row, 4).Value = corner.RawDepthAverage;
            ws.Cell(row, 5).Value = corner.TideCorrection ?? 0;
            ws.Cell(row, 6).Value = corner.CorrectedDepth;
            ws.Cell(row, 7).Value = corner.SourceFile?.RawDepthStdDev ?? 0;
            ws.Cell(row, 8).Value = corner.SourceFile?.FileName ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void CreateCalculationsSheet(XLWorkbook workbook, InclinationProject project)
    {
        var ws = workbook.Worksheets.Add("Calculations");

        ws.Cell("A1").Value = "Calculation Parameters";
        ws.Cell("A1").Style.Font.Bold = true;

        int row = 3;
        ws.Cell(row, 1).Value = "Geometry:"; ws.Cell(row, 2).Value = project.Geometry.ToString(); row++;
        ws.Cell(row, 1).Value = "Dimension Unit:"; ws.Cell(row, 2).Value = project.DimensionUnit.ToString(); row++;
        ws.Cell(row, 1).Value = "Depth Unit:"; ws.Cell(row, 2).Value = project.DepthUnit.ToString(); row++;
        ws.Cell(row, 1).Value = "Water Density:"; ws.Cell(row, 2).Value = $"{project.WaterDensity} kg/m³"; row++;
        ws.Cell(row, 1).Value = "Tide Applied:"; ws.Cell(row, 2).Value = project.ApplyTideCorrection ? "Yes" : "No"; row++;
        ws.Cell(row, 1).Value = "Manual Tide Value:"; ws.Cell(row, 2).Value = $"{project.TideValue:F3} m"; row++;
        ws.Cell(row, 1).Value = "Draft Offset:"; ws.Cell(row, 2).Value = $"{project.DraftOffset:F3} m"; row++;

        row += 2;
        ws.Cell(row, 1).Value = "Tolerances";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        ws.Cell(row, 1).Value = "Max Inclination:"; ws.Cell(row, 2).Value = $"{project.Tolerances.MaxInclination}°"; row++;
        ws.Cell(row, 1).Value = "Max Misclosure:"; ws.Cell(row, 2).Value = $"{project.Tolerances.MaxMisclosure} m"; row++;
        ws.Cell(row, 1).Value = "Max Out of Plane:"; ws.Cell(row, 2).Value = $"{project.Tolerances.MaxOutOfPlane} m"; row++;

        ws.Columns().AdjustToContents();
    }

    private void StyleStatus(IXLCell cell, QualityStatus status)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = status switch
        {
            QualityStatus.OK => XLColor.LightGreen,
            QualityStatus.Warning => XLColor.LightYellow,
            QualityStatus.Failed => XLColor.LightPink,
            _ => XLColor.White
        };
    }
}

/// <summary>PDF report generation using QuestPDF</summary>
public class PdfReportGenerator
{
    public void Generate(string filePath, InclinationProject project)
    {
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, project));
                page.Content().Element(c => ComposeContent(c, project));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf(filePath);
    }

    private void ComposeHeader(IContainer container, InclinationProject project)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("TREE INCLINATION REPORT").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().Text($"Structure: {project.StructureName}").FontSize(12);
                col.Item().Text($"Date: {project.SurveyDate:yyyy-MM-dd}");
            });

            row.RelativeItem().AlignRight().Column(col =>
            {
                col.Item().Text($"Client: {project.ClientName}");
                col.Item().Text($"Vessel: {project.VesselName}");
                col.Item().Text($"Surveyor: {project.SurveyorName}");
            });
        });

        container.PaddingTop(10).LineHorizontal(1).LineColor(Colors.Blue.Darken2);
    }

    private void ComposeContent(IContainer container, InclinationProject project)
    {
        container.PaddingVertical(10).Column(col =>
        {
            // Results section
            if (project.Result != null)
            {
                col.Item().Text("RESULTS").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                col.Item().PaddingVertical(5);

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                    });

                    table.Cell().Element(CellStyle).Text("Parameter").Bold();
                    table.Cell().Element(CellStyle).Text("Value").Bold();
                    table.Cell().Element(CellStyle).Text("Status").Bold();

                    var r = project.Result;
                    AddResultRow(table, "Total Inclination", $"{r.TotalInclination:F3}°", r.InclinationStatus);
                    AddResultRow(table, "Heading", $"{r.InclinationHeading:F2}° ({r.HeadingDescription})", null);
                    AddResultRow(table, "Pitch", $"{r.AveragePitch:F3}°", null);
                    AddResultRow(table, "Roll", $"{r.AverageRoll:F3}°", null);
                    AddResultRow(table, "Misclosure", $"{r.Misclosure:F3} m", r.MisclosureStatus);
                    AddResultRow(table, "Out of Plane", $"{r.OutOfPlane:F3} m", r.OutOfPlaneStatus);
                    AddResultRow(table, "Method", r.CalculationMethod, null);
                });
            }

            col.Item().PaddingVertical(15);

            // Corner data section
            col.Item().Text("CORNER DATA").FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
            col.Item().PaddingVertical(5);

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                });

                table.Cell().Element(CellStyle).Text("Corner").Bold();
                table.Cell().Element(CellStyle).Text("X (m)").Bold();
                table.Cell().Element(CellStyle).Text("Y (m)").Bold();
                table.Cell().Element(CellStyle).Text("Raw Z (m)").Bold();
                table.Cell().Element(CellStyle).Text("Tide (m)").Bold();
                table.Cell().Element(CellStyle).Text("Corr Z (m)").Bold();

                foreach (var corner in project.Corners.Where(c => !c.IsClosurePoint).OrderBy(c => c.Index))
                {
                    table.Cell().Element(CellStyle).Text(corner.Name);
                    table.Cell().Element(CellStyle).Text($"{corner.X:F3}");
                    table.Cell().Element(CellStyle).Text($"{corner.Y:F3}");
                    table.Cell().Element(CellStyle).Text($"{corner.RawDepthAverage:F3}");
                    table.Cell().Element(CellStyle).Text($"{corner.TideCorrection ?? 0:F3}");
                    table.Cell().Element(CellStyle).Text($"{corner.CorrectedDepth:F3}");
                }
            });
        });
    }

    private void AddResultRow(TableDescriptor table, string parameter, string value, QualityStatus? status)
    {
        table.Cell().Element(CellStyle).Text(parameter);
        table.Cell().Element(CellStyle).Text(value);

        if (status.HasValue)
        {
            var color = status.Value switch
            {
                QualityStatus.OK => Colors.Green.Darken1,
                QualityStatus.Warning => Colors.Orange.Darken1,
                QualityStatus.Failed => Colors.Red.Darken1,
                _ => Colors.Grey.Darken1
            };
            table.Cell().Element(CellStyle).Text(status.Value.ToString()).FontColor(color).Bold();
        }
        else
        {
            table.Cell().Element(CellStyle).Text("");
        }
    }

    private IContainer CellStyle(IContainer c) =>
        c.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5);

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(8);
                x.CurrentPageNumber().FontSize(8);
                x.Span(" of ").FontSize(8);
                x.TotalPages().FontSize(8);
            });
        });
    }
}
