using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using FathomOS.Core.Parsers;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FathomOS.Modules.SurveyListing.Views;

public partial class DxfToListingWindow : MetroWindow
{
    private List<DxfPoint> _extractedPoints = new();
    private string _dxfPath = string.Empty;
    
    public DxfToListingWindow()
    {
        InitializeComponent();
    }
    
    private void BrowseDxf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
            Title = "Select DXF File"
        };
        
        if (dialog.ShowDialog() == true)
        {
            _dxfPath = dialog.FileName;
            TxtDxfPath.Text = _dxfPath;
            TxtStatus.Text = "DXF file selected. Click Preview to extract points.";
        }
    }
    
    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_dxfPath))
        {
            MessageBox.Show("Please select a DXF file first.", "No File", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        try
        {
            TxtStatus.Text = "Extracting points from DXF...";
            ExtractPoints();
            
            // Show preview (first 100 points)
            var previewData = _extractedPoints.Take(100).ToList();
            PreviewGrid.ItemsSource = previewData;
            
            TxtPointCount.Text = $"{_extractedPoints.Count:N0} points extracted";
            TxtStatus.Text = "Preview complete. Click Export to save the listing.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading DXF file:\n\n{ex.Message}\n\nNote: Only DXF files are supported. If you have a DWG file, please convert it to DXF first using AutoCAD, BricsCAD, or a free online converter.",
                "DXF Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Error reading DXF file.";
        }
    }
    
    private void ExtractPoints()
    {
        _extractedPoints.Clear();
        
        var parser = new DxfLayoutParser();
        var layout = parser.Parse(_dxfPath);
        
        // Get layer filter
        var layerFilter = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(TxtLayerFilter.Text))
        {
            foreach (var layer in TxtLayerFilter.Text.Split(','))
            {
                var trimmed = layer.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    layerFilter.Add(trimmed);
            }
        }
        
        bool hasFilter = layerFilter.Count > 0;
        
        // Extract points from different entity types
        if (ChkPoints.IsChecked == true)
        {
            foreach (var pt in layout.Points)
            {
                if (hasFilter && !layerFilter.Contains(pt.Layer)) continue;
                _extractedPoints.Add(new DxfPoint(pt.X, pt.Y, pt.Z, pt.Layer, "Point"));
            }
        }
        
        if (ChkLines.IsChecked == true)
        {
            foreach (var line in layout.Lines)
            {
                if (hasFilter && !layerFilter.Contains(line.Layer)) continue;
                _extractedPoints.Add(new DxfPoint(line.StartX, line.StartY, line.StartZ, line.Layer, "Line Start"));
                _extractedPoints.Add(new DxfPoint(line.EndX, line.EndY, line.EndZ, line.Layer, "Line End"));
            }
        }
        
        if (ChkPolylines.IsChecked == true)
        {
            foreach (var poly in layout.Polylines)
            {
                if (hasFilter && !layerFilter.Contains(poly.Layer)) continue;
                int vertexNum = 0;
                foreach (var v in poly.Vertices)
                {
                    _extractedPoints.Add(new DxfPoint(v.X, v.Y, v.Z, poly.Layer, $"Polyline V{vertexNum++}"));
                }
            }
        }
        
        if (ChkCircles.IsChecked == true)
        {
            foreach (var circle in layout.Circles)
            {
                if (hasFilter && !layerFilter.Contains(circle.Layer)) continue;
                _extractedPoints.Add(new DxfPoint(circle.CenterX, circle.CenterY, circle.CenterZ, circle.Layer, "Circle Center"));
            }
        }
        
        if (ChkArcs.IsChecked == true)
        {
            foreach (var arc in layout.Arcs)
            {
                if (hasFilter && !layerFilter.Contains(arc.Layer)) continue;
                _extractedPoints.Add(new DxfPoint(arc.CenterX, arc.CenterY, arc.CenterZ, arc.Layer, "Arc Center"));
            }
        }
        
        if (ChkText.IsChecked == true)
        {
            foreach (var text in layout.Texts)
            {
                if (hasFilter && !layerFilter.Contains(text.Layer)) continue;
                _extractedPoints.Add(new DxfPoint(text.X, text.Y, text.Z, text.Layer, $"Text: {text.Content}"));
            }
        }
        
        // Remove duplicates if requested
        if (ChkRemoveDuplicates.IsChecked == true)
        {
            _extractedPoints = _extractedPoints
                .GroupBy(p => $"{p.X:F6},{p.Y:F6},{p.Z:F6}")
                .Select(g => g.First())
                .ToList();
        }
    }
    
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_extractedPoints.Count == 0)
        {
            MessageBox.Show("No points to export. Please preview first.", "No Data", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        string filter;
        string defaultExt;
        
        switch (CboOutputFormat.SelectedIndex)
        {
            case 0: // CSV
                filter = "CSV Files (*.csv)|*.csv";
                defaultExt = ".csv";
                break;
            case 1: // Tab
                filter = "Text Files (*.txt)|*.txt";
                defaultExt = ".txt";
                break;
            case 2: // Space
                filter = "Text Files (*.txt)|*.txt";
                defaultExt = ".txt";
                break;
            case 3: // Excel
                filter = "Excel Files (*.xlsx)|*.xlsx";
                defaultExt = ".xlsx";
                break;
            default:
                filter = "All Files (*.*)|*.*";
                defaultExt = ".txt";
                break;
        }
        
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            DefaultExt = defaultExt,
            FileName = Path.GetFileNameWithoutExtension(_dxfPath) + "_SurveyListing"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                ExportToFile(dialog.FileName);
                MessageBox.Show($"Survey listing exported successfully!\n\n{_extractedPoints.Count:N0} points saved to:\n{dialog.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting file:\n\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void ExportToFile(string filePath)
    {
        string delimiter = CboOutputFormat.SelectedIndex switch
        {
            0 => ",",
            1 => "\t",
            2 => " ",
            _ => ","
        };
        
        bool swapXY = CboCoordOrder.SelectedIndex == 1;
        bool includeLayer = ChkIncludeLayer.IsChecked == true;
        bool includeHeader = ChkIncludeHeader.IsChecked == true;
        
        using var writer = new StreamWriter(filePath);
        
        // Header
        if (includeHeader)
        {
            var headers = new List<string>();
            if (swapXY)
            {
                headers.Add("Y (Northing)");
                headers.Add("X (Easting)");
            }
            else
            {
                headers.Add("X (Easting)");
                headers.Add("Y (Northing)");
            }
            headers.Add("Z (Elevation)");
            if (includeLayer)
            {
                headers.Add("Layer");
                headers.Add("Type");
            }
            writer.WriteLine(string.Join(delimiter, headers));
        }
        
        // Data
        foreach (var pt in _extractedPoints)
        {
            var values = new List<string>();
            if (swapXY)
            {
                values.Add(pt.Y.ToString("F6"));
                values.Add(pt.X.ToString("F6"));
            }
            else
            {
                values.Add(pt.X.ToString("F6"));
                values.Add(pt.Y.ToString("F6"));
            }
            values.Add(pt.Z.ToString("F6"));
            if (includeLayer)
            {
                values.Add(pt.Layer);
                values.Add(pt.EntityType);
            }
            writer.WriteLine(string.Join(delimiter, values));
        }
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

/// <summary>
/// Simple class to hold extracted DXF point data
/// </summary>
public class DxfPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string Layer { get; set; }
    public string EntityType { get; set; }
    
    public DxfPoint(double x, double y, double z, string layer, string entityType)
    {
        X = x;
        Y = y;
        Z = z;
        Layer = layer;
        EntityType = entityType;
    }
}
