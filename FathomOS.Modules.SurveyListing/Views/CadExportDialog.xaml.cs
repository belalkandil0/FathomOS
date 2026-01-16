using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyListing.Models;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FathomOS.Modules.SurveyListing.Views;

/// <summary>
/// Layer export item for the CAD export dialog
/// </summary>
public class ExportLayerItem : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public string Name { get; set; } = string.Empty;
    public Color Color { get; set; } = Colors.White;
    public SolidColorBrush ColorBrush => new(Color);
    public int PointCount { get; set; }
    public List<(double X, double Y, double Z)> Points { get; set; } = new();
    public bool IsPolyline { get; set; } = true; // Whether to draw as connected polyline
    
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Dialog for selecting layers and options for CAD script export
/// </summary>
public partial class CadExportDialog : MetroWindow
{
    public ObservableCollection<ExportLayerItem> ExportLayers { get; } = new();
    public bool DialogResultOk { get; private set; }
    public string? ExportedFilePath { get; private set; }

    public CadExportDialog()
    {
        InitializeComponent();
        LayerListControl.ItemsSource = ExportLayers;
    }

    /// <summary>
    /// Add a layer for potential export
    /// </summary>
    public void AddLayer(string name, Color color, List<(double X, double Y, double Z)> points, bool isPolyline = true)
    {
        ExportLayers.Add(new ExportLayerItem
        {
            Name = name,
            Color = color,
            Points = points,
            PointCount = points.Count,
            IsPolyline = isPolyline,
            IsSelected = points.Count > 0
        });
    }

    /// <summary>
    /// Add layer from EditorLayer
    /// </summary>
    public void AddLayerFromEditor(EditorLayer layer, List<EditablePoint> points)
    {
        var pointList = points.Select(p => (p.X, p.Y, p.Z)).ToList();
        AddLayer(layer.Name, layer.Color, pointList, layer.LayerType != EditorLayerType.Digitized);
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var layer in ExportLayers)
            layer.IsSelected = true;
    }

    private void SelectNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var layer in ExportLayers)
            layer.IsSelected = false;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        // Check if any layers selected
        if (!ExportLayers.Any(l => l.IsSelected))
        {
            MessageBox.Show("Please select at least one layer to export.", "No Layers Selected",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Get save file path - offer AutoLISP and CSV
        var dialog = new SaveFileDialog
        {
            Title = "Export CAD Data",
            Filter = "AutoLISP Script (*.lsp)|*.lsp|CSV Data (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".lsp",
            FileName = "survey_export.lsp"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            // Parse options
            bool includePoints = ChkIncludePoints.IsChecked == true;
            bool includePolylines = ChkIncludePolylines.IsChecked == true;
            bool includeLayerSetup = ChkIncludeLayerSetup.IsChecked == true;
            bool useExaggeration = ChkUseExaggeration.IsChecked == true;
            double exaggeration = 1.0;
            
            if (useExaggeration && double.TryParse(TxtExaggeration.Text, out double exag))
                exaggeration = exag;

            string extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
            
            if (extension == ".csv")
            {
                // Generate CSV
                var csv = GenerateCsvExport(exaggeration);
                File.WriteAllText(dialog.FileName, csv, Encoding.UTF8);
            }
            else
            {
                // Generate AutoLISP (works for .lsp and any other extension)
                var lisp = GenerateAutoLisp(includePoints, includePolylines, includeLayerSetup, exaggeration);
                File.WriteAllText(dialog.FileName, lisp, Encoding.ASCII);
            }

            ExportedFilePath = dialog.FileName;
            DialogResultOk = true;
            DialogResult = true;
            Close();
            
            // Show instructions
            if (extension == ".lsp")
            {
                MessageBox.Show(
                    "AutoLISP file exported successfully!\n\n" +
                    "To load in AutoCAD/BricsCAD:\n" +
                    "1. Type APPLOAD and press Enter\n" +
                    "2. Browse to and select the .lsp file\n" +
                    "3. Click 'Load'\n" +
                    "4. The survey data will be drawn automatically\n\n" +
                    "Or drag and drop the .lsp file onto the CAD window.",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting CAD data:\n{ex.Message}", "Export Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResultOk = false;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Generate AutoLISP script that works in AutoCAD and BricsCAD
    /// </summary>
    private string GenerateAutoLisp(bool includePoints, bool includePolylines, bool includeLayerSetup, double exaggeration)
    {
        var sb = new StringBuilder();
        
        // AutoLISP header
        sb.AppendLine("; Fathom OS Survey Listing - AutoLISP Export");
        sb.AppendLine($"; Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("; Compatible with AutoCAD and BricsCAD");
        sb.AppendLine("; Load with APPLOAD command or drag-drop onto CAD window");
        sb.AppendLine();
        sb.AppendLine("(defun C:LOADSURVEY (/ oldecho oldsnap)");
        sb.AppendLine("  (setq oldecho (getvar \"CMDECHO\"))");
        sb.AppendLine("  (setq oldsnap (getvar \"OSMODE\"))");
        sb.AppendLine("  (setvar \"CMDECHO\" 0)");
        sb.AppendLine("  (setvar \"OSMODE\" 0)");
        sb.AppendLine("  (princ \"\\nLoading survey data...\")");
        sb.AppendLine();

        // Process each selected layer
        foreach (var layer in ExportLayers.Where(l => l.IsSelected && l.Points.Count > 0))
        {
            string layerName = SanitizeLayerName(layer.Name);
            string colorIndex = GetAutoCADColorIndex(layer.Color);
            
            // Layer setup
            if (includeLayerSetup)
            {
                sb.AppendLine($"  ; Create layer: {layerName}");
                sb.AppendLine($"  (command \"_.-LAYER\" \"_N\" \"{layerName}\" \"_C\" \"{colorIndex}\" \"{layerName}\" \"_S\" \"{layerName}\" \"\")");
                sb.AppendLine();
            }

            // Draw 3D polyline
            if (includePolylines && layer.IsPolyline && layer.Points.Count > 1)
            {
                sb.AppendLine($"  ; 3D Polyline for {layerName}");
                sb.AppendLine("  (command \"_.3DPOLY\"");
                
                foreach (var pt in layer.Points)
                {
                    double z = pt.Z * exaggeration * -1; // Negative for correct orientation
                    sb.AppendLine(FormattableString.Invariant($"    \"{pt.X:F4},{pt.Y:F4},{z:F4}\""));
                }
                
                sb.AppendLine("    \"\"");
                sb.AppendLine("  )");
                sb.AppendLine();
            }

            // Draw individual points
            if (includePoints)
            {
                sb.AppendLine($"  ; Points for {layerName}");
                sb.AppendLine("  (setvar \"PDMODE\" 0)");
                sb.AppendLine("  (setvar \"PDSIZE\" 0)");
                
                foreach (var pt in layer.Points)
                {
                    double z = pt.Z * exaggeration * -1;
                    sb.AppendLine(FormattableString.Invariant($"  (command \"_.POINT\" \"{pt.X:F4},{pt.Y:F4},{z:F4}\")"));
                }
                sb.AppendLine();
            }
        }

        // Restore settings and zoom
        sb.AppendLine("  ; Restore settings and zoom");
        sb.AppendLine("  (command \"_.ZOOM\" \"_E\")");
        sb.AppendLine("  (command \"_.REGEN\")");
        sb.AppendLine("  (setvar \"CMDECHO\" oldecho)");
        sb.AppendLine("  (setvar \"OSMODE\" oldsnap)");
        sb.AppendLine();
        
        // Count totals for message
        int totalPoints = ExportLayers.Where(l => l.IsSelected).Sum(l => l.Points.Count);
        int layerCount = ExportLayers.Count(l => l.IsSelected && l.Points.Count > 0);
        
        sb.AppendLine($"  (princ \"\\nSurvey data loaded: {totalPoints} points in {layerCount} layer(s)\")");
        sb.AppendLine("  (princ)");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("; Auto-run on load");
        sb.AppendLine("(C:LOADSURVEY)");

        return sb.ToString();
    }
    
    /// <summary>
    /// Generate CSV export with coordinates
    /// </summary>
    private string GenerateCsvExport(double exaggeration)
    {
        var sb = new StringBuilder();
        
        // CSV header
        sb.AppendLine("Layer,Index,X,Y,Z");
        
        foreach (var layer in ExportLayers.Where(l => l.IsSelected && l.Points.Count > 0))
        {
            int index = 1;
            foreach (var pt in layer.Points)
            {
                double z = pt.Z * exaggeration * -1;
                sb.AppendLine(FormattableString.Invariant($"\"{layer.Name}\",{index},{pt.X:F6},{pt.Y:F6},{z:F6}"));
                index++;
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Sanitize layer name for AutoCAD (remove special characters)
    /// </summary>
    private string SanitizeLayerName(string name)
    {
        // Remove characters not allowed in AutoCAD layer names
        var sanitized = name
            .Replace("<", "")
            .Replace(">", "")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace("\"", "")
            .Replace(":", "_")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("|", "_")
            .Replace("=", "_")
            .Replace("`", "")
            .Replace(";", "_");
        
        // Trim to max length
        if (sanitized.Length > 255)
            sanitized = sanitized.Substring(0, 255);
            
        return sanitized;
    }

    /// <summary>
    /// Convert RGB color to AutoCAD Color Index (ACI)
    /// Uses closest match to standard ACI colors 1-7
    /// </summary>
    private string GetAutoCADColorIndex(Color color)
    {
        // Map to closest ACI color (1-7 are the standard colors)
        // 1=Red, 2=Yellow, 3=Green, 4=Cyan, 5=Blue, 6=Magenta, 7=White
        
        int r = color.R;
        int g = color.G;
        int b = color.B;
        
        // Calculate brightness
        int brightness = (r + g + b) / 3;
        
        // White/Black check
        if (brightness > 200 && Math.Abs(r - g) < 30 && Math.Abs(g - b) < 30)
            return "7"; // White
        if (brightness < 50)
            return "7"; // Use white for very dark colors
        
        // Find dominant color
        if (r > g && r > b)
        {
            if (g > b + 50) return "2"; // Yellow (red + green)
            if (b > g + 50) return "6"; // Magenta (red + blue)
            return "1"; // Red
        }
        else if (g > r && g > b)
        {
            if (r > b + 50) return "2"; // Yellow (green + red)
            if (b > r + 50) return "4"; // Cyan (green + blue)
            return "3"; // Green
        }
        else if (b > r && b > g)
        {
            if (r > g + 50) return "6"; // Magenta (blue + red)
            if (g > r + 50) return "4"; // Cyan (blue + green)
            return "5"; // Blue
        }
        else if (Math.Abs(r - g) < 30 && r > b)
        {
            return "2"; // Yellow
        }
        else if (Math.Abs(g - b) < 30 && g > r)
        {
            return "4"; // Cyan
        }
        else if (Math.Abs(r - b) < 30 && r > g)
        {
            return "6"; // Magenta
        }
        
        return "7"; // Default to white
    }
}
