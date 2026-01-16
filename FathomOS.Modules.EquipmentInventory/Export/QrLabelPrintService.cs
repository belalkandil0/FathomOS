using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Export;

/// <summary>
/// Service for generating printable QR code label sheets
/// </summary>
public class QrLabelPrintService
{
    private readonly QRCodeService _qrService;
    
    public QrLabelPrintService()
    {
        _qrService = new QRCodeService();
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    /// <summary>
    /// Generate a PDF sheet of QR code labels
    /// </summary>
    public void GenerateLabelSheet(string filePath, IEnumerable<Equipment> equipment, LabelOptions options)
    {
        var items = equipment.ToList();
        
        Document.Create(container =>
        {
            container.Page(page =>
            {
                // Set page size based on label format
                page.Size(options.PageSize);
                page.Margin(options.PageMargin);
                
                page.Content().Column(column =>
                {
                    // Calculate items per row
                    var labelsPerRow = options.LabelsPerRow;
                    var rows = (items.Count + labelsPerRow - 1) / labelsPerRow;
                    
                    for (int rowIndex = 0; rowIndex < rows; rowIndex++)
                    {
                        var rowItems = items.Skip(rowIndex * labelsPerRow).Take(labelsPerRow).ToList();
                        
                        column.Item().Row(row =>
                        {
                            foreach (var eq in rowItems)
                            {
                                row.RelativeItem().Element(c => ComposeLabel(c, eq, options));
                            }
                            
                            // Fill remaining columns with empty space
                            for (int i = rowItems.Count; i < labelsPerRow; i++)
                            {
                                row.RelativeItem();
                            }
                        });
                        
                        // Add spacing between rows (except last)
                        if (rowIndex < rows - 1)
                            column.Item().Height(options.LabelSpacing);
                    }
                });
            });
        }).GeneratePdf(filePath);
    }
    
    private void ComposeLabel(IContainer container, Equipment equipment, LabelOptions options)
    {
        container
            .Width(options.LabelWidth)
            .Height(options.LabelHeight)
            .Border(options.ShowBorder ? 0.5f : 0)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(options.LabelPadding)
            .Column(col =>
            {
                // QR Code
                var qrContent = QRCodeService.GenerateEquipmentQrCode(equipment.AssetNumber);
                var qrBytes = _qrService.GenerateQrCodePng(qrContent, options.QrSize);
                
                col.Item()
                    .AlignCenter()
                    .Width(options.QrSize)
                    .Height(options.QrSize)
                    .Image(qrBytes);
                
                col.Item().Height(3);
                
                // Asset Number
                col.Item()
                    .AlignCenter()
                    .Text(equipment.AssetNumber)
                    .FontSize(options.AssetNumberFontSize)
                    .Bold();
                
                if (options.ShowName)
                {
                    col.Item().Height(2);
                    col.Item()
                        .AlignCenter()
                        .Text(TruncateText(equipment.Name, options.MaxNameLength))
                        .FontSize(options.NameFontSize);
                }
                
                if (options.ShowSerialNumber && !string.IsNullOrEmpty(equipment.SerialNumber))
                {
                    col.Item().Height(1);
                    col.Item()
                        .AlignCenter()
                        .Text($"S/N: {equipment.SerialNumber}")
                        .FontSize(options.SerialNumberFontSize)
                        .FontColor(Colors.Grey.Darken1);
                }
                
                if (options.ShowCategory && equipment.Category != null)
                {
                    col.Item().Height(1);
                    col.Item()
                        .AlignCenter()
                        .Text(equipment.Category.Name)
                        .FontSize(7)
                        .FontColor(Colors.Grey.Darken2);
                }
                
                if (options.ShowCompanyName)
                {
                    col.Item().Height(2);
                    col.Item()
                        .AlignCenter()
                        .Text(options.CompanyName)
                        .FontSize(6)
                        .FontColor(Colors.Grey.Medium);
                }
            });
    }
    
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text[..(maxLength - 3)] + "...";
    }
    
    /// <summary>
    /// Generate labels for multiple pages worth of equipment
    /// </summary>
    public void GenerateMultiPageLabels(string filePath, IEnumerable<Equipment> equipment, LabelOptions options)
    {
        var items = equipment.ToList();
        var labelsPerPage = options.LabelsPerRow * options.LabelsPerColumn;
        
        Document.Create(container =>
        {
            var pageCount = (items.Count + labelsPerPage - 1) / labelsPerPage;
            
            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var pageItems = items.Skip(pageIndex * labelsPerPage).Take(labelsPerPage).ToList();
                
                container.Page(page =>
                {
                    page.Size(options.PageSize);
                    page.Margin(options.PageMargin);
                    
                    page.Content().Column(column =>
                    {
                        for (int rowIndex = 0; rowIndex < options.LabelsPerColumn; rowIndex++)
                        {
                            var rowItems = pageItems.Skip(rowIndex * options.LabelsPerRow).Take(options.LabelsPerRow).ToList();
                            
                            if (!rowItems.Any()) break;
                            
                            column.Item().Row(row =>
                            {
                                foreach (var eq in rowItems)
                                {
                                    row.RelativeItem().Element(c => ComposeLabel(c, eq, options));
                                }
                                
                                for (int i = rowItems.Count; i < options.LabelsPerRow; i++)
                                {
                                    row.RelativeItem();
                                }
                            });
                            
                            if (rowIndex < options.LabelsPerColumn - 1)
                                column.Item().Height(options.LabelSpacing);
                        }
                    });
                });
            }
        }).GeneratePdf(filePath);
    }
}

public class LabelOptions
{
    // Page settings
    public PageSize PageSize { get; set; } = PageSizes.A4;
    public float PageMargin { get; set; } = 20;
    
    // Label layout
    public int LabelsPerRow { get; set; } = 3;
    public int LabelsPerColumn { get; set; } = 7;
    public float LabelWidth { get; set; } = 60;  // mm
    public float LabelHeight { get; set; } = 35; // mm
    public float LabelPadding { get; set; } = 3;
    public float LabelSpacing { get; set; } = 5;
    
    // QR Code
    public int QrSize { get; set; } = 20; // pixels per module
    
    // Display options
    public bool ShowBorder { get; set; } = true;
    public bool ShowName { get; set; } = true;
    public bool ShowSerialNumber { get; set; } = true;
    public bool ShowCategory { get; set; } = false;
    public bool ShowCompanyName { get; set; } = true;
    public string CompanyName { get; set; } = "S7 Solutions";
    
    // Text
    public int MaxNameLength { get; set; } = 25;
    public float AssetNumberFontSize { get; set; } = 8;
    public float NameFontSize { get; set; } = 7;
    public float SerialNumberFontSize { get; set; } = 6;
    
    // Presets
    public static LabelOptions SmallLabel => new()
    {
        LabelsPerRow = 4,
        LabelsPerColumn = 10,
        LabelWidth = 45,
        LabelHeight = 25,
        QrSize = 15,
        ShowCategory = false,
        ShowSerialNumber = false,
        AssetNumberFontSize = 7,
        NameFontSize = 6
    };
    
    public static LabelOptions StandardLabel => new()
    {
        LabelsPerRow = 3,
        LabelsPerColumn = 7,
        LabelWidth = 60,
        LabelHeight = 35,
        QrSize = 20
    };
    
    public static LabelOptions LargeLabel => new()
    {
        LabelsPerRow = 2,
        LabelsPerColumn = 4,
        LabelWidth = 90,
        LabelHeight = 60,
        QrSize = 30,
        ShowCategory = true,
        AssetNumberFontSize = 10,
        NameFontSize = 9
    };
}
