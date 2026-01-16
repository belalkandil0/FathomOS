using System.Data;
using System.IO;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.TreeInclination.Models;
using FathomOS.Modules.TreeInclination.Services;

namespace FathomOS.Modules.TreeInclination.Views;

public partial class FileImportDialog : MetroWindow
{
    private readonly NpdDepthParser _parser = new();
    private readonly string _filePath;
    private readonly DepthInputUnit _depthUnit;
    private readonly double _waterDensity;
    private readonly double _atmPressure;
    
    private ColumnDetectionResult? _detection;
    private DataPreviewResult? _preview;
    
    public NpdFileInfo? Result { get; private set; }
    public string CornerName => CornerNameCombo.Text;
    public bool IsClosurePoint => IsClosureCheck.IsChecked == true;

    public FileImportDialog(string filePath, DepthInputUnit depthUnit, double waterDensity, double atmPressure)
    {
        // Load theme before InitializeComponent
        var themeUri = new Uri("/FathomOS.Modules.TreeInclination;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        _filePath = filePath;
        _depthUnit = depthUnit;
        _waterDensity = waterDensity;
        _atmPressure = atmPressure;
        
        FileNameText.Text = Path.GetFileName(filePath);
        
        LoadFileData();
    }

    private void LoadFileData()
    {
        try
        {
            // Auto-detect columns
            _detection = _parser.AutoDetectColumns(_filePath);
            
            if (_detection.Error != null)
            {
                System.Windows.MessageBox.Show($"Error reading file: {_detection.Error}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            // Populate dropdowns with "(None)" option
            var columnsWithNone = new List<string> { "(None)" };
            columnsWithNone.AddRange(_detection.AllColumns);
            
            DepthColumnCombo.ItemsSource = _detection.AllColumns;
            TimeColumnCombo.ItemsSource = columnsWithNone;
            EastingColumnCombo.ItemsSource = columnsWithNone;
            NorthingColumnCombo.ItemsSource = columnsWithNone;
            HeightColumnCombo.ItemsSource = columnsWithNone;
            HeadingColumnCombo.ItemsSource = columnsWithNone;
            
            // Set auto-detected values
            if (!string.IsNullOrEmpty(_detection.DetectedDepthColumn))
                DepthColumnCombo.SelectedItem = _detection.DetectedDepthColumn;
            
            TimeColumnCombo.SelectedItem = !string.IsNullOrEmpty(_detection.DetectedTimeColumn) 
                ? _detection.DetectedTimeColumn : "(None)";
            EastingColumnCombo.SelectedItem = !string.IsNullOrEmpty(_detection.DetectedEastingColumn) 
                ? _detection.DetectedEastingColumn : "(None)";
            NorthingColumnCombo.SelectedItem = !string.IsNullOrEmpty(_detection.DetectedNorthingColumn) 
                ? _detection.DetectedNorthingColumn : "(None)";
            HeightColumnCombo.SelectedItem = !string.IsNullOrEmpty(_detection.DetectedHeightColumn) 
                ? _detection.DetectedHeightColumn : "(None)";
            HeadingColumnCombo.SelectedItem = !string.IsNullOrEmpty(_detection.DetectedHeadingColumn) 
                ? _detection.DetectedHeadingColumn : "(None)";
            
            NaviPacFormatCheck.IsChecked = _detection.HasDateTimeSplit;
            
            // Load preview
            _preview = _parser.GetPreview(_filePath, 20);
            if (_preview.IsValid)
            {
                FileInfoText.Text = $"{_preview.TotalRows:N0} rows, {_preview.Headers.Count} columns";
                LoadPreviewGrid();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadPreviewGrid()
    {
        if (_preview == null || !_preview.IsValid) return;
        
        var dataTable = new DataTable();
        
        // Add columns - handle duplicates by appending index
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in _preview.Headers)
        {
            string columnName = header;
            int suffix = 1;
            
            // If name already exists, append a number
            while (usedNames.Contains(columnName))
            {
                columnName = $"{header}_{suffix++}";
            }
            
            usedNames.Add(columnName);
            dataTable.Columns.Add(columnName);
        }
        
        // Add rows
        foreach (var row in _preview.Rows)
        {
            var dataRow = dataTable.NewRow();
            for (int i = 0; i < Math.Min(row.Count, dataTable.Columns.Count); i++)
            {
                dataRow[i] = row[i];
            }
            dataTable.Rows.Add(dataRow);
        }
        
        PreviewGrid.ItemsSource = dataTable.DefaultView;
    }

    private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
    {
        LoadFileData();
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(DepthColumnCombo.SelectedItem?.ToString()))
        {
            System.Windows.MessageBox.Show("Please select a Depth column.", "Validation", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var selection = CreateColumnSelection();
            var parseResult = _parser.Parse(_filePath, selection, _depthUnit, _waterDensity, _atmPressure);
            
            if (parseResult.IsValid)
            {
                StatsPanel.Visibility = Visibility.Visible;
                AvgText.Text = parseResult.RawDepthAverage.ToString("F4");
                StdDevText.Text = parseResult.RawDepthStdDev.ToString("F4");
                MinMaxText.Text = $"{parseResult.RawDepthMin:F3} / {parseResult.RawDepthMax:F3}";
                RecordsText.Text = $"{parseResult.RecordCount:N0}";
                
                // Show heading if available
                if (parseResult.HasHeadingData && parseResult.AverageHeading.HasValue)
                {
                    HeadingText.Text = $"{parseResult.AverageHeading:F1}°";
                }
                else
                {
                    HeadingText.Text = "-";
                }
                
                // Update file info with position and heading
                var infoText = $"{parseResult.RecordCount:N0} rows";
                if (parseResult.HasPositionData)
                {
                    infoText += $" | Pos: E={parseResult.AverageEasting:F1}, N={parseResult.AverageNorthing:F1}";
                }
                if (parseResult.HasHeadingData)
                {
                    infoText += $" | Hdg: {parseResult.AverageHeading:F1}°";
                }
                FileInfoText.Text = infoText;
            }
            else
            {
                System.Windows.MessageBox.Show($"Parse failed: {parseResult.ValidationMessage}", "Parse Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(DepthColumnCombo.SelectedItem?.ToString()))
        {
            System.Windows.MessageBox.Show("Please select a Depth column.", "Validation", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(CornerNameCombo.Text))
        {
            System.Windows.MessageBox.Show("Please specify a Corner name.", "Validation", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var selection = CreateColumnSelection();
            Result = _parser.Parse(_filePath, selection, _depthUnit, _waterDensity, _atmPressure);
            
            if (Result.IsValid)
            {
                Result.CornerName = CornerNameCombo.Text;
                Result.IsClosurePoint = IsClosureCheck.IsChecked == true;
                DialogResult = true;
                Close();
            }
            else
            {
                System.Windows.MessageBox.Show($"Import failed: {Result.ValidationMessage}", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private ColumnSelection CreateColumnSelection()
    {
        return new ColumnSelection
        {
            DepthColumn = DepthColumnCombo.SelectedItem?.ToString(),
            TimeColumn = GetSelectedColumn(TimeColumnCombo),
            EastingColumn = GetSelectedColumn(EastingColumnCombo),
            NorthingColumn = GetSelectedColumn(NorthingColumnCombo),
            HeightColumn = GetSelectedColumn(HeightColumnCombo),
            HeadingColumn = GetSelectedColumn(HeadingColumnCombo),
            HasDateTimeSplit = NaviPacFormatCheck.IsChecked == true
        };
    }

    private string? GetSelectedColumn(System.Windows.Controls.ComboBox combo)
    {
        var selected = combo.SelectedItem?.ToString();
        return selected == "(None)" ? null : selected;
    }
}
