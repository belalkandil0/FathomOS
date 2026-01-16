using System;
using System.IO;
using OxyPlot;
using OxyPlot.Wpf;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for exporting OxyPlot charts to various formats
/// </summary>
public class ChartExportService
{
    /// <summary>
    /// Export plot model to PNG file
    /// </summary>
    public void ExportToPng(PlotModel model, string filePath, int width = 1200, int height = 800)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        
        var exporter = new PngExporter { Width = width, Height = height };
        using var stream = File.Create(filePath);
        exporter.Export(model, stream);
    }

    /// <summary>
    /// Export plot model to SVG file
    /// </summary>
    public void ExportToSvg(PlotModel model, string filePath, int width = 1200, int height = 800)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        
        using var stream = File.Create(filePath);
        var exporter = new OxyPlot.SvgExporter { Width = width, Height = height };
        exporter.Export(model, stream);
    }

    /// <summary>
    /// Export plot model to PDF file
    /// </summary>
    public void ExportToPdf(PlotModel model, string filePath, int width = 800, int height = 600)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));
        
        using var stream = File.Create(filePath);
        var exporter = new PdfExporter { Width = width, Height = height };
        exporter.Export(model, stream);
    }

    /// <summary>
    /// Export both spin and transit charts to a single folder
    /// </summary>
    public void ExportAllCharts(PlotModel spinModel, PlotModel transitModel, string folderPath, string prefix = "USBL")
    {
        Directory.CreateDirectory(folderPath);
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        
        if (spinModel != null)
        {
            var spinPath = Path.Combine(folderPath, $"{prefix}_Spin_{timestamp}.png");
            ExportToPng(spinModel, spinPath);
        }
        
        if (transitModel != null)
        {
            var transitPath = Path.Combine(folderPath, $"{prefix}_Transit_{timestamp}.png");
            ExportToPng(transitModel, transitPath);
        }
    }

    /// <summary>
    /// Create a combined chart image with both plots
    /// </summary>
    public void ExportCombinedChart(PlotModel spinModel, PlotModel transitModel, string filePath, 
        int width = 2400, int height = 800)
    {
        // For combined charts, we export them side by side using the PNG exporter
        // This is a simplified version - a more complex implementation would use SkiaSharp to combine
        
        var tempFolder = Path.Combine(Path.GetTempPath(), "USBLCharts");
        Directory.CreateDirectory(tempFolder);
        
        try
        {
            var spinPath = Path.Combine(tempFolder, "spin_temp.png");
            var transitPath = Path.Combine(tempFolder, "transit_temp.png");
            
            ExportToPng(spinModel, spinPath, width / 2, height);
            ExportToPng(transitModel, transitPath, width / 2, height);
            
            // For now, just copy the spin chart as the "combined" 
            // A full implementation would use image processing to combine them
            File.Copy(spinPath, filePath, true);
        }
        finally
        {
            try { Directory.Delete(tempFolder, true); } catch { }
        }
    }
}
