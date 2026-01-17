using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.EquipmentInventory.Models;

// Alias for QuestPDF types to avoid conflicts with System.Windows.Media
using QuestColors = QuestPDF.Helpers.Colors;

namespace FathomOS.Modules.EquipmentInventory.Export;

/// <summary>
/// PDF export service using QuestPDF from S7Fathom.Core
/// </summary>
public class PdfExportService
{
    private readonly string _companyName;
    private readonly string _logoPath;
    
    public PdfExportService(string companyName = "S7 Solutions", string? logoPath = null)
    {
        _companyName = companyName;
        _logoPath = logoPath ?? string.Empty;
        
        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    /// <summary>
    /// Generate equipment register PDF report
    /// </summary>
    public void GenerateEquipmentRegister(string filePath, IEnumerable<Equipment> equipment, string? title = null)
    {
        var items = equipment.ToList();
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));
                
                page.Header().Element(c => ComposeHeader(c, title ?? "Equipment Register"));
                
                page.Content().Element(c => ComposeEquipmentTable(c, items));
                
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf(filePath);
    }
    
    /// <summary>
    /// Generate manifest PDF
    /// </summary>
    public void GenerateManifest(string filePath, Manifest manifest)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(c => ComposeManifestHeader(c, manifest));
                
                page.Content().Element(c => ComposeManifestContent(c, manifest));
                
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf(filePath);
    }
    
    private void ComposeHeader(IContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_companyName).FontSize(14).Bold().FontColor(QuestColors.Blue.Darken3);
                col.Item().Text(title).FontSize(18).Bold();
                col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor(QuestColors.Grey.Medium);
            });
            
            if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
            {
                row.ConstantItem(80).Image(_logoPath);
            }
        });
        
        container.PaddingBottom(10).LineHorizontal(1).LineColor(QuestColors.Blue.Darken3);
    }
    
    private void ComposeManifestHeader(IContainer container, Manifest manifest)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(_companyName).FontSize(12).Bold().FontColor(QuestColors.Blue.Darken3);
                    c.Item().Text("TRANSFER MANIFEST").FontSize(20).Bold();
                });
                
                row.ConstantItem(150).Column(c =>
                {
                    c.Item().AlignRight().Text(manifest.ManifestNumber).FontSize(14).Bold();
                    c.Item().AlignRight().Text(manifest.Type.ToString().ToUpper()).FontSize(10);
                    c.Item().AlignRight().Text(manifest.Status.ToString()).FontSize(10)
                        .FontColor(manifest.Status == ManifestStatus.Completed ? QuestColors.Green.Darken2 : QuestColors.Orange.Darken2);
                });
            });
            
            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(QuestColors.Blue.Darken3);
            
            // From/To section
            col.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().BorderRight(1).BorderColor(QuestColors.Grey.Lighten2).Padding(5).Column(c =>
                {
                    c.Item().Text("FROM:").Bold().FontSize(9);
                    c.Item().Text(manifest.FromLocation?.Name ?? "-").FontSize(11);
                    c.Item().Text(manifest.FromContactName ?? "").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                    c.Item().Text(manifest.FromContactPhone ?? "").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                });
                
                row.RelativeItem().Padding(5).Column(c =>
                {
                    c.Item().Text("TO:").Bold().FontSize(9);
                    c.Item().Text(manifest.ToLocation?.Name ?? "-").FontSize(11);
                    c.Item().Text(manifest.ToContactName ?? "").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                    c.Item().Text(manifest.ToContactPhone ?? "").FontSize(9).FontColor(QuestColors.Grey.Darken1);
                });
            });
            
            col.Item().PaddingVertical(5).LineHorizontal(0.5f).LineColor(QuestColors.Grey.Lighten1);
        });
    }
    
    private void ComposeEquipmentTable(IContainer container, List<Equipment> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(70);   // Asset #
                columns.RelativeColumn(2);    // Name
                columns.RelativeColumn(1);    // Category
                columns.RelativeColumn(1);    // Location
                columns.ConstantColumn(60);   // Status
                columns.ConstantColumn(70);   // Serial #
                columns.ConstantColumn(65);   // Cert Expiry
            });
            
            // Header
            table.Header(header =>
            {
                header.Cell().Background(QuestColors.Blue.Darken3).Padding(5).Text("Asset #").Bold().FontColor(QuestColors.White);
                header.Cell().Background(QuestColors.Blue.Darken3).Padding(5).Text("Name").Bold().FontColor(QuestColors.White);
                header.Cell().Background(QuestColors.Blue.Darken3).Padding(5).Text("Category").Bold().FontColor(QuestColors.White);
                header.Cell().Background(QuestColors.Blue.Darken3).Padding(5).Text("Location").Bold().FontColor(QuestColors.White);
                header.Cell().Background(QuestColors.Blue.Darken3).Padding(5).Text("Status").Bold().FontColor(QuestColors.White);
                header.Cell().Background(QuestColors.Blue.Darken3).Padding(5).Text("Serial #").Bold().FontColor(QuestColors.White);
                header.Cell().Background(QuestColors.Blue.Darken3).Padding(5).Text("Cert Exp").Bold().FontColor(QuestColors.White);
            });
            
            // Data rows
            foreach (var (eq, index) in items.Select((e, i) => (e, i)))
            {
                var bgColor = index % 2 == 0 ? QuestColors.White : QuestColors.Grey.Lighten4;
                
                table.Cell().Background(bgColor).Padding(3).Text(eq.AssetNumber);
                table.Cell().Background(bgColor).Padding(3).Text(eq.Name);
                table.Cell().Background(bgColor).Padding(3).Text(eq.Category?.Name ?? "");
                table.Cell().Background(bgColor).Padding(3).Text(eq.CurrentLocation?.Name ?? "");
                table.Cell().Background(bgColor).Padding(3).Text(eq.Status.ToString());
                table.Cell().Background(bgColor).Padding(3).Text(eq.SerialNumber ?? "");
                
                var certCell = table.Cell().Background(bgColor).Padding(3);
                if (eq.IsCertificationExpired)
                    certCell.Background(QuestColors.Red.Lighten3).Text(eq.CertificationExpiryDate?.ToString("yyyy-MM-dd") ?? "");
                else if (eq.IsCertificationExpiring)
                    certCell.Background(QuestColors.Yellow.Lighten3).Text(eq.CertificationExpiryDate?.ToString("yyyy-MM-dd") ?? "");
                else
                    certCell.Text(eq.CertificationExpiryDate?.ToString("yyyy-MM-dd") ?? "");
            }
        });
        
        container.PaddingTop(10).Text($"Total: {items.Count} items").FontSize(9).Bold();
    }
    
    private void ComposeManifestContent(IContainer container, Manifest manifest)
    {
        container.Column(col =>
        {
            // Items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);   // #
                    columns.ConstantColumn(80);   // Asset #
                    columns.RelativeColumn(2);    // Name
                    columns.ConstantColumn(40);   // Qty
                    columns.ConstantColumn(60);   // Condition
                    columns.ConstantColumn(50);   // Received
                    columns.ConstantColumn(70);   // Discrepancy
                });
                
                table.Header(header =>
                {
                    header.Cell().Background(QuestColors.Blue.Darken3).Padding(4).Text("#").Bold().FontColor(QuestColors.White).FontSize(9);
                    header.Cell().Background(QuestColors.Blue.Darken3).Padding(4).Text("Asset #").Bold().FontColor(QuestColors.White).FontSize(9);
                    header.Cell().Background(QuestColors.Blue.Darken3).Padding(4).Text("Description").Bold().FontColor(QuestColors.White).FontSize(9);
                    header.Cell().Background(QuestColors.Blue.Darken3).Padding(4).Text("Qty").Bold().FontColor(QuestColors.White).FontSize(9);
                    header.Cell().Background(QuestColors.Blue.Darken3).Padding(4).Text("Condition").Bold().FontColor(QuestColors.White).FontSize(9);
                    header.Cell().Background(QuestColors.Blue.Darken3).Padding(4).Text("Rcvd").Bold().FontColor(QuestColors.White).FontSize(9);
                    header.Cell().Background(QuestColors.Blue.Darken3).Padding(4).Text("Discrepancy").Bold().FontColor(QuestColors.White).FontSize(9);
                });
                
                var itemNum = 1;
                foreach (var item in manifest.Items)
                {
                    var bgColor = item.HasDiscrepancy ? QuestColors.Red.Lighten4 : (itemNum % 2 == 0 ? QuestColors.White : QuestColors.Grey.Lighten4);
                    
                    table.Cell().Background(bgColor).Padding(3).Text(itemNum.ToString()).FontSize(9);
                    table.Cell().Background(bgColor).Padding(3).Text(item.AssetNumber ?? "").FontSize(9);
                    table.Cell().Background(bgColor).Padding(3).Text(item.Name ?? "").FontSize(9);
                    table.Cell().Background(bgColor).Padding(3).Text(item.Quantity.ToString("F0")).FontSize(9);
                    table.Cell().Background(bgColor).Padding(3).Text(item.ConditionAtSend ?? "").FontSize(9);
                    table.Cell().Background(bgColor).Padding(3).Text(item.IsReceived ? "✓" : "").FontSize(9);
                    table.Cell().Background(bgColor).Padding(3).Text(item.HasDiscrepancy ? (item.DiscrepancyType?.ToString() ?? "Yes") : "").FontSize(9);
                    
                    itemNum++;
                }
            });
            
            // Summary
            col.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Total Items: {manifest.TotalItems}").Bold();
                    if (manifest.TotalWeight.HasValue)
                        c.Item().Text($"Total Weight: {manifest.TotalWeight:F2} kg");
                    if (manifest.HasDiscrepancies)
                        c.Item().Text("⚠ This manifest has discrepancies").FontColor(QuestColors.Red.Darken2).Bold();
                });
            });
            
            // Signatures section
            col.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(QuestColors.Grey.Medium).Padding(10).MinHeight(80).Column(c =>
                {
                    c.Item().Text("Sender Signature:").FontSize(9).Bold();
                    c.Item().PaddingTop(40).Text(manifest.SenderSignedAt?.ToString("yyyy-MM-dd HH:mm") ?? "").FontSize(8);
                });
                
                row.ConstantItem(20);
                
                row.RelativeItem().Border(1).BorderColor(QuestColors.Grey.Medium).Padding(10).MinHeight(80).Column(c =>
                {
                    c.Item().Text("Receiver Signature:").FontSize(9).Bold();
                    c.Item().PaddingTop(40).Text(manifest.ReceiverSignedAt?.ToString("yyyy-MM-dd HH:mm") ?? "").FontSize(8);
                });
            });
            
            // Notes
            if (!string.IsNullOrEmpty(manifest.Notes))
            {
                col.Item().PaddingTop(15).Column(c =>
                {
                    c.Item().Text("Notes:").Bold().FontSize(9);
                    c.Item().Text(manifest.Notes).FontSize(9);
                });
            }
        });
    }
    
    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }
    
    /// <summary>
    /// Generate defect report PDF (Equipment Failure Notification)
    /// </summary>
    public void GenerateDefectReport(string filePath, DefectReport defect)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(c => ComposeHeader(c, "Equipment Failure Notification"));
                
                page.Content().Element(c => ComposeDefectReportContent(c, defect));
                
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf(filePath);
    }
    
    private void ComposeDefectReportContent(IContainer container, DefectReport defect)
    {
        container.Column(col =>
        {
            col.Spacing(10);
            
            // Report Header
            col.Item().Background(QuestColors.Grey.Lighten3).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Report Number: {defect.ReportNumber}").Bold().FontSize(14);
                    c.Item().Text($"Status: {defect.Status}").FontSize(10);
                });
                row.RelativeItem().AlignRight().Column(c =>
                {
                    c.Item().Text($"Created: {defect.CreatedAt:yyyy-MM-dd HH:mm}").FontSize(9);
                    c.Item().Text($"Urgency: {defect.ReplacementUrgency}").FontSize(9).FontColor(
                        defect.ReplacementUrgency == ReplacementUrgency.High ? QuestColors.Red.Medium : QuestColors.Black);
                });
            });
            
            // Failure Details Section
            col.Item().Text("FAILURE DETAILS").Bold().FontSize(11);
            col.Item().Border(1).BorderColor(QuestColors.Grey.Medium).Padding(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });
                
                AddTableRow(table, "Created By:", defect.CreatedByUser?.FullName ?? "-");
                AddTableRow(table, "Date:", defect.ReportDate.ToString("yyyy-MM-dd"));
                AddTableRow(table, "Location:", defect.Location?.DisplayName ?? "-");
                AddTableRow(table, "Working Depth:", defect.WorkingWaterDepthMetres?.ToString("F0") + " metres" ?? "-");
            });
            
            // Equipment Details Section
            col.Item().PaddingTop(10).Text("EQUIPMENT DETAILS").Bold().FontSize(11);
            col.Item().Border(1).BorderColor(QuestColors.Grey.Medium).Padding(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });
                
                AddTableRow(table, "Equipment:", defect.Equipment?.Name ?? "-");
                AddTableRow(table, "Asset Number:", defect.Equipment?.AssetNumber ?? "-");
                AddTableRow(table, "Category:", defect.EquipmentCategory?.Name ?? "-");
                AddTableRow(table, "Equipment Origin:", defect.EquipmentOrigin ?? "-");
                AddTableRow(table, "Manufacturer:", defect.Manufacturer ?? "-");
                AddTableRow(table, "Model:", defect.Model ?? "-");
                AddTableRow(table, "Serial Number:", defect.SerialNumber ?? "-");
            });
            
            // Symptoms / Action Taken Section
            col.Item().PaddingTop(10).Text("SYMPTOMS / ACTION TAKEN").Bold().FontSize(11);
            col.Item().Border(1).BorderColor(QuestColors.Grey.Medium).Padding(10).Column(c =>
            {
                c.Item().Text("Category of Fault:").Bold().FontSize(9);
                c.Item().Text(defect.FaultCategory.ToString()).FontSize(10);
                
                c.Item().PaddingTop(8).Text("Detailed Symptoms:").Bold().FontSize(9);
                c.Item().Text(defect.DetailedSymptoms ?? "-").FontSize(10);
                
                c.Item().PaddingTop(8).Text("Action Taken:").Bold().FontSize(9);
                c.Item().Text(defect.ActionTaken ?? "-").FontSize(10);
                
                c.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(innerCol =>
                    {
                        innerCol.Item().Text("Parts Available On Board:").Bold().FontSize(9);
                        innerCol.Item().Text(defect.PartsAvailableOnBoard ? "Yes" : "No").FontSize(10);
                    });
                    row.RelativeItem().Column(innerCol =>
                    {
                        innerCol.Item().Text("Replacement Required:").Bold().FontSize(9);
                        innerCol.Item().Text(defect.ReplacementRequired ? "Yes" : "No").FontSize(10);
                    });
                    row.RelativeItem().Column(innerCol =>
                    {
                        innerCol.Item().Text("Urgency:").Bold().FontSize(9);
                        innerCol.Item().Text(defect.ReplacementUrgency.ToString()).FontSize(10);
                    });
                });
                
                if (!string.IsNullOrEmpty(defect.FurtherComments))
                {
                    c.Item().PaddingTop(8).Text("Further Comments:").Bold().FontSize(9);
                    c.Item().Text(defect.FurtherComments).FontSize(10);
                }
                
                if (defect.RepairDurationMinutes > 0 || defect.DowntimeDurationMinutes > 0)
                {
                    c.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(innerCol =>
                        {
                            innerCol.Item().Text("Repair Duration:").Bold().FontSize(9);
                            innerCol.Item().Text($"{defect.RepairDurationMinutes} minutes").FontSize(10);
                        });
                        row.RelativeItem().Column(innerCol =>
                        {
                            innerCol.Item().Text("Downtime Duration:").Bold().FontSize(9);
                            innerCol.Item().Text($"{defect.DowntimeDurationMinutes} minutes").FontSize(10);
                        });
                    });
                }
            });
            
            // Parts Failed/Required Section
            if (defect.Parts?.Any() == true)
            {
                col.Item().PaddingTop(10).Text("PARTS FAILED / REQUIRED").Bold().FontSize(11);
                col.Item().Border(1).BorderColor(QuestColors.Grey.Medium).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);   // #
                        columns.RelativeColumn(2);    // Description
                        columns.RelativeColumn(1);    // SAP Number
                        columns.RelativeColumn(1);    // Model
                        columns.RelativeColumn(1);    // Serial
                        columns.ConstantColumn(50);   // Failed
                        columns.ConstantColumn(50);   // Required
                    });
                    
                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(5).Text("#").Bold().FontSize(8);
                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(5).Text("Description").Bold().FontSize(8);
                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(5).Text("SAP Number").Bold().FontSize(8);
                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(5).Text("Model").Bold().FontSize(8);
                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(5).Text("Serial").Bold().FontSize(8);
                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(5).Text("Failed").Bold().FontSize(8);
                        header.Cell().Background(QuestColors.Grey.Lighten2).Padding(5).Text("Req'd").Bold().FontSize(8);
                    });
                    
                    int idx = 1;
                    foreach (var part in defect.Parts)
                    {
                        table.Cell().Padding(5).Text(idx.ToString()).FontSize(8);
                        table.Cell().Padding(5).Text(part.Description ?? "-").FontSize(8);
                        table.Cell().Padding(5).Text(part.SapNumber ?? "-").FontSize(8);
                        table.Cell().Padding(5).Text(part.ModelNumber ?? "-").FontSize(8);
                        table.Cell().Padding(5).Text(part.SerialNumber ?? "-").FontSize(8);
                        table.Cell().Padding(5).Text(part.QuantityFailed.ToString()).FontSize(8);
                        table.Cell().Padding(5).Text(part.QuantityRequired.ToString()).FontSize(8);
                        idx++;
                    }
                });
            }
            
            // Resolution Section (if resolved)
            if (defect.Status == DefectReportStatus.Resolved || defect.Status == DefectReportStatus.Closed)
            {
                col.Item().PaddingTop(10).Text("RESOLUTION").Bold().FontSize(11);
                col.Item().Border(1).BorderColor(QuestColors.Green.Lighten2).Background(QuestColors.Green.Lighten4).Padding(10).Column(c =>
                {
                    c.Item().Text($"Resolved By: {defect.ResolvedByUser?.FullName ?? "-"}").FontSize(10);
                    c.Item().Text($"Resolved At: {defect.ResolvedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-"}").FontSize(10);
                    c.Item().PaddingTop(5).Text("Resolution Notes:").Bold().FontSize(9);
                    c.Item().Text(defect.ResolutionNotes ?? "-").FontSize(10);
                });
            }
        });
    }
    
    private void AddTableRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(3).Text(label).Bold().FontSize(9);
        table.Cell().Padding(3).Text(value).FontSize(9);
    }
}
