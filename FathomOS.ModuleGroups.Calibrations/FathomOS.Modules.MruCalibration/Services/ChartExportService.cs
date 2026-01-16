using OxyPlot;
using OxyPlot.Wpf;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FathomOS.Modules.MruCalibration.Services;

/// <summary>
/// Service for exporting charts to PNG and PDF formats
/// </summary>
public class ChartExportService
{
    #region Constants
    
    private const int DefaultWidth = 1200;
    private const int DefaultHeight = 800;
    private const int DefaultDpi = 150;
    
    #endregion
    
    #region PNG Export
    
    /// <summary>
    /// Export a PlotModel to PNG file
    /// </summary>
    public bool ExportToPng(PlotModel model, string filePath, int width = DefaultWidth, int height = DefaultHeight)
    {
        if (model == null) return false;
        
        try
        {
            // Set background on model
            model.Background = OxyColor.FromRgb(30, 30, 30); // Dark background
            
            var exporter = new PngExporter 
            { 
                Width = width, 
                Height = height
            };
            
            using var stream = File.Create(filePath);
            exporter.Export(model, stream);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PNG export failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Export a PlotModel to PNG with light background
    /// </summary>
    public bool ExportToPngLight(PlotModel model, string filePath, int width = DefaultWidth, int height = DefaultHeight)
    {
        if (model == null) return false;
        
        try
        {
            // Clone the model and set light colors
            var lightModel = CloneModelWithLightTheme(model);
            lightModel.Background = OxyColors.White;
            
            var exporter = new PngExporter 
            { 
                Width = width, 
                Height = height
            };
            
            using var stream = File.Create(filePath);
            exporter.Export(lightModel, stream);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PNG export failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Export multiple charts to PNG files
    /// </summary>
    public int ExportAllChartsToPng(Dictionary<string, PlotModel> charts, string outputFolder, bool useLightTheme = true)
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);
        
        int successCount = 0;
        
        foreach (var kvp in charts)
        {
            if (kvp.Value == null) continue;
            
            string fileName = SanitizeFileName(kvp.Key) + ".png";
            string filePath = Path.Combine(outputFolder, fileName);
            
            bool success = useLightTheme 
                ? ExportToPngLight(kvp.Value, filePath) 
                : ExportToPng(kvp.Value, filePath);
            
            if (success) successCount++;
        }
        
        return successCount;
    }
    
    #endregion
    
    #region PDF Export
    
    /// <summary>
    /// Export a PlotModel to PDF file
    /// </summary>
    public bool ExportToPdf(PlotModel model, string filePath, int width = DefaultWidth, int height = DefaultHeight)
    {
        if (model == null) return false;
        
        try
        {
            // Clone model with light theme for PDF (better printing)
            var pdfModel = CloneModelWithLightTheme(model);
            pdfModel.Background = OxyColors.White;
            
            var exporter = new PdfExporter 
            { 
                Width = width, 
                Height = height
            };
            
            using var stream = File.Create(filePath);
            exporter.Export(pdfModel, stream);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF export failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Export all charts to individual PDF files
    /// </summary>
    public int ExportAllChartsToPdf(Dictionary<string, PlotModel> charts, string outputFolder)
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);
        
        int successCount = 0;
        
        foreach (var kvp in charts)
        {
            if (kvp.Value == null) continue;
            
            string fileName = SanitizeFileName(kvp.Key) + ".pdf";
            string filePath = Path.Combine(outputFolder, fileName);
            
            if (ExportToPdf(kvp.Value, filePath))
                successCount++;
        }
        
        return successCount;
    }
    
    #endregion
    
    #region Chart to Bitmap (for embedding in reports)
    
    /// <summary>
    /// Convert PlotModel to byte array (PNG format) for embedding in reports
    /// </summary>
    public byte[]? GetChartAsPngBytes(PlotModel model, int width = 800, int height = 500)
    {
        if (model == null) return null;
        
        try
        {
            var lightModel = CloneModelWithLightTheme(model);
            lightModel.Background = OxyColors.White;
            
            var exporter = new PngExporter 
            { 
                Width = width, 
                Height = height
            };
            
            using var stream = new MemoryStream();
            exporter.Export(lightModel, stream);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Convert PlotModel to BitmapSource for WPF display
    /// </summary>
    public BitmapSource? GetChartAsBitmap(PlotModel model, int width = 800, int height = 500)
    {
        var bytes = GetChartAsPngBytes(model, width, height);
        if (bytes == null) return null;
        
        try
        {
            using var stream = new MemoryStream(bytes);
            var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return decoder.Frames[0];
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Clone a PlotModel with light theme colors
    /// </summary>
    private PlotModel CloneModelWithLightTheme(PlotModel source)
    {
        // Create a new model with same structure but light colors
        var model = new PlotModel
        {
            Title = source.Title,
            TitleColor = OxyColors.Black,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            TextColor = OxyColors.Black,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White
        };
        
        // Clone axes with light colors
        foreach (var axis in source.Axes)
        {
            var newAxis = CloneAxisWithLightTheme(axis);
            model.Axes.Add(newAxis);
        }
        
        // Clone series (keep data colors)
        foreach (var series in source.Series)
        {
            model.Series.Add(series);
        }
        
        // Clone annotations
        foreach (var annotation in source.Annotations)
        {
            model.Annotations.Add(annotation);
        }
        
        return model;
    }
    
    /// <summary>
    /// Clone an axis with light theme colors
    /// </summary>
    private OxyPlot.Axes.Axis CloneAxisWithLightTheme(OxyPlot.Axes.Axis source)
    {
        OxyPlot.Axes.Axis newAxis;
        
        if (source is OxyPlot.Axes.DateTimeAxis dtAxis)
        {
            newAxis = new OxyPlot.Axes.DateTimeAxis
            {
                Position = dtAxis.Position,
                Title = dtAxis.Title,
                StringFormat = dtAxis.StringFormat,
                Key = dtAxis.Key,
                Minimum = dtAxis.Minimum,
                Maximum = dtAxis.Maximum,
                IsPanEnabled = dtAxis.IsPanEnabled,
                IsZoomEnabled = dtAxis.IsZoomEnabled
            };
        }
        else if (source is OxyPlot.Axes.LinearAxis linAxis)
        {
            newAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = linAxis.Position,
                Title = linAxis.Title,
                StringFormat = linAxis.StringFormat,
                Key = linAxis.Key,
                Minimum = linAxis.Minimum,
                Maximum = linAxis.Maximum,
                IsPanEnabled = linAxis.IsPanEnabled,
                IsZoomEnabled = linAxis.IsZoomEnabled
            };
        }
        else if (source is OxyPlot.Axes.CategoryAxis catAxis)
        {
            var newCatAxis = new OxyPlot.Axes.CategoryAxis
            {
                Position = catAxis.Position,
                Title = catAxis.Title,
                Key = catAxis.Key
            };
            foreach (var label in catAxis.Labels)
                newCatAxis.Labels.Add(label);
            newAxis = newCatAxis;
        }
        else
        {
            // Generic axis clone
            newAxis = new OxyPlot.Axes.LinearAxis
            {
                Position = source.Position,
                Title = source.Title,
                Key = source.Key
            };
        }
        
        // Apply light theme colors
        newAxis.TextColor = OxyColors.Black;
        newAxis.TitleColor = OxyColors.Black;
        newAxis.AxislineColor = OxyColor.FromRgb(100, 100, 100);
        newAxis.MajorGridlineColor = OxyColor.FromRgb(220, 220, 220);
        newAxis.MinorGridlineColor = OxyColor.FromRgb(240, 240, 240);
        newAxis.TicklineColor = OxyColor.FromRgb(100, 100, 100);
        
        return newAxis;
    }
    
    /// <summary>
    /// Sanitize a string to be used as a file name
    /// </summary>
    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Replace(" ", "_");
    }
    
    #endregion
}
