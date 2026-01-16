using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Views;

public partial class BatchColumnMappingDialog : MetroWindow
{
    private readonly List<string> _filePaths;
    private readonly ObservableCollection<FileMappingInfo> _fileMappings = new();
    private FileMappingInfo? _currentFile;
    private bool _isUpdating;

    public Dictionary<string, UsblColumnMapping> ResultMappings { get; private set; } = new();
    public bool DialogConfirmed { get; private set; }

    public BatchColumnMappingDialog(List<string> filePaths)
    {
        // Load theme
        var themeUri = new Uri("/FathomOS.Modules.UsblVerification;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        _filePaths = filePaths;
        FileListBox.ItemsSource = _fileMappings;
        
        Loaded += BatchColumnMappingDialog_Loaded;
    }

    private void BatchColumnMappingDialog_Loaded(object sender, RoutedEventArgs e)
    {
        LoadAllFiles();
    }

    private void LoadAllFiles()
    {
        _fileMappings.Clear();
        
        foreach (var filePath in _filePaths)
        {
            var info = new FileMappingInfo(filePath);
            info.LoadFile();
            info.AutoDetectColumns();
            _fileMappings.Add(info);
        }

        FileCountText.Text = $"{_fileMappings.Count} files selected";
        
        if (_fileMappings.Count > 0)
        {
            FileListBox.SelectedIndex = 0;
        }
        
        ValidateAllFiles();
    }

    private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FileListBox.SelectedItem is FileMappingInfo info)
        {
            SaveCurrentMapping();
            _currentFile = info;
            LoadFileMapping(info);
        }
    }

    private void LoadFileMapping(FileMappingInfo info)
    {
        _isUpdating = true;
        
        CurrentFileText.Text = info.FileName;
        
        // Set delimiter based on detected value
        int delimiterIndex = info.Mapping.Delimiter switch
        {
            '\t' => 0,  // Tab
            ',' => 1,   // Comma
            ';' => 2,   // Semicolon
            _ => 1      // Default to comma
        };
        DelimiterCombo.SelectedIndex = delimiterIndex;
        
        DateTimeSplitCheck.IsChecked = info.Mapping.HasDateTimeSplit;
        
        // Populate dropdowns
        PopulateColumnDropdowns(info.Headers);
        
        // Set selected columns
        SetComboSelection(DateColumnCombo, info.Mapping.DateColumn);
        SetComboSelection(VesselEastingCombo, info.Mapping.VesselEastingColumn);
        SetComboSelection(VesselNorthingCombo, info.Mapping.VesselNorthingColumn);
        SetComboSelection(VesselGyroCombo, info.Mapping.VesselGyroColumn);
        SetComboSelection(TransponderEastingCombo, info.Mapping.TransponderEastingColumn);
        SetComboSelection(TransponderNorthingCombo, info.Mapping.TransponderNorthingColumn);
        SetComboSelection(TransponderDepthCombo, info.Mapping.TransponderDepthColumn);
        
        // Update preview
        UpdatePreview(info);
        
        _isUpdating = false;
    }

    private void PopulateColumnDropdowns(string[] headers)
    {
        var combos = new[] 
        { 
            DateColumnCombo, VesselEastingCombo, VesselNorthingCombo, VesselGyroCombo,
            TransponderEastingCombo, TransponderNorthingCombo, TransponderDepthCombo 
        };

        foreach (var combo in combos)
        {
            combo.Items.Clear();
            combo.Items.Add(new ComboBoxItem { Content = "(Not Set)", Tag = -1 });
            
            for (int i = 0; i < headers.Length; i++)
            {
                combo.Items.Add(new ComboBoxItem 
                { 
                    Content = $"[{i}] {headers[i]}", 
                    Tag = i 
                });
            }
            combo.SelectedIndex = 0;
        }
    }

    private void SetComboSelection(ComboBox combo, int columnIndex)
    {
        if (columnIndex < 0)
        {
            combo.SelectedIndex = 0;
        }
        else if (columnIndex + 1 < combo.Items.Count)
        {
            combo.SelectedIndex = columnIndex + 1;
        }
    }

    private int GetComboSelection(ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item && item.Tag is int idx)
            return idx;
        return -1;
    }

    private void SaveCurrentMapping()
    {
        if (_currentFile == null || _isUpdating) return;

        _currentFile.Mapping.Delimiter = GetSelectedDelimiter();
        _currentFile.Mapping.HasDateTimeSplit = DateTimeSplitCheck.IsChecked == true;
        _currentFile.Mapping.DateColumn = GetComboSelection(DateColumnCombo);
        _currentFile.Mapping.TimeColumn = GetComboSelection(DateColumnCombo) + 1;
        _currentFile.Mapping.VesselEastingColumn = GetComboSelection(VesselEastingCombo);
        _currentFile.Mapping.VesselNorthingColumn = GetComboSelection(VesselNorthingCombo);
        _currentFile.Mapping.VesselGyroColumn = GetComboSelection(VesselGyroCombo);
        _currentFile.Mapping.TransponderEastingColumn = GetComboSelection(TransponderEastingCombo);
        _currentFile.Mapping.TransponderNorthingColumn = GetComboSelection(TransponderNorthingCombo);
        _currentFile.Mapping.TransponderDepthColumn = GetComboSelection(TransponderDepthCombo);
        
        _currentFile.Validate();
        ValidateAllFiles();
    }

    private char GetSelectedDelimiter()
    {
        // Use index-based selection for reliability
        return DelimiterCombo.SelectedIndex switch
        {
            0 => '\t',  // Tab
            1 => ',',   // Comma
            2 => ';',   // Semicolon
            _ => ','    // Default to comma
        };
    }

    private void UpdatePreview(FileMappingInfo info)
    {
        try
        {
            var dt = new DataTable();
            
            foreach (var header in info.Headers)
            {
                dt.Columns.Add(header);
            }
            
            foreach (var row in info.DataRows.Take(10))
            {
                var dataRow = dt.NewRow();
                for (int i = 0; i < Math.Min(row.Length, info.Headers.Length); i++)
                {
                    dataRow[i] = row[i];
                }
                dt.Rows.Add(dataRow);
            }

            PreviewDataGrid.ItemsSource = dt.DefaultView;
        }
        catch { }
    }

    private void ValidateAllFiles()
    {
        int validCount = _fileMappings.Count(f => f.IsValid);
        int totalCount = _fileMappings.Count;
        
        if (validCount == totalCount)
        {
            ValidationIcon.Kind = PackIconMaterialKind.CheckCircle;
            ValidationIcon.Foreground = FindResource("SuccessBrush") as Brush ?? Brushes.Green;
            ValidationText.Text = $"All {totalCount} files configured correctly";
            ApplyButton.IsEnabled = true;
        }
        else
        {
            ValidationIcon.Kind = PackIconMaterialKind.AlertCircle;
            ValidationIcon.Foreground = FindResource("WarningBrush") as Brush ?? Brushes.Orange;
            ValidationText.Text = $"{validCount} of {totalCount} files configured correctly";
            ApplyButton.IsEnabled = validCount > 0;
        }
        
        // Refresh the list to update icons
        FileListBox.Items.Refresh();
    }

    private void BuildResultMappings()
    {
        ResultMappings.Clear();
        foreach (var info in _fileMappings.Where(f => f.IsValid))
        {
            ResultMappings[info.FilePath] = info.Mapping;
        }
    }

    #region Event Handlers

    private void AutoDetectCurrentButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFile != null)
        {
            _currentFile.AutoDetectColumns();
            LoadFileMapping(_currentFile);
            ValidateAllFiles();
        }
    }

    private void AutoDetectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var info in _fileMappings)
        {
            info.AutoDetectColumns();
        }
        
        if (_currentFile != null)
        {
            LoadFileMapping(_currentFile);
        }
        
        ValidateAllFiles();
    }

    private void ApplyToAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_fileMappings.Count < 2) return;
        
        SaveCurrentMapping();
        
        var firstMapping = _fileMappings[0].Mapping;
        
        for (int i = 1; i < _fileMappings.Count; i++)
        {
            _fileMappings[i].Mapping = new UsblColumnMapping
            {
                Delimiter = firstMapping.Delimiter,
                HeaderRows = firstMapping.HeaderRows,
                HasDateTimeSplit = firstMapping.HasDateTimeSplit,
                DateFormat = firstMapping.DateFormat,
                TimeFormat = firstMapping.TimeFormat,
                DateColumn = firstMapping.DateColumn,
                TimeColumn = firstMapping.TimeColumn,
                VesselEastingColumn = firstMapping.VesselEastingColumn,
                VesselNorthingColumn = firstMapping.VesselNorthingColumn,
                VesselGyroColumn = firstMapping.VesselGyroColumn,
                TransponderEastingColumn = firstMapping.TransponderEastingColumn,
                TransponderNorthingColumn = firstMapping.TransponderNorthingColumn,
                TransponderDepthColumn = firstMapping.TransponderDepthColumn
            };
            _fileMappings[i].Validate();
        }
        
        ValidateAllFiles();
        MessageBox.Show($"Applied mapping from '{_fileMappings[0].FileName}' to all {_fileMappings.Count - 1} other files.", 
                        "Apply to All", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogConfirmed = false;
        DialogResult = false;
        Close();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentMapping();
        BuildResultMappings();
        DialogConfirmed = true;
        DialogResult = true;
        Close();
    }

    #endregion
}

/// <summary>
/// Helper class to track file mapping info
/// </summary>
public class FileMappingInfo : INotifyPropertyChanged
{
    public string FilePath { get; }
    public string FileName => Path.GetFileName(FilePath);
    public string[] Headers { get; private set; } = Array.Empty<string>();
    public List<string[]> DataRows { get; private set; } = new();
    public UsblColumnMapping Mapping { get; set; } = new();
    public int ColumnCount => Headers.Length;
    
    private bool _isValid;
    public bool IsValid
    {
        get => _isValid;
        set { _isValid = value; OnPropertyChanged(nameof(IsValid)); OnPropertyChanged(nameof(ValidationIcon)); OnPropertyChanged(nameof(ValidationColor)); }
    }
    
    public string ValidationIcon => IsValid ? "CheckCircle" : "AlertCircle";
    public Brush ValidationColor => IsValid ? Brushes.LimeGreen : Brushes.Orange;

    public FileMappingInfo(string filePath)
    {
        FilePath = filePath;
    }

    public void LoadFile()
    {
        try
        {
            var lines = File.ReadAllLines(FilePath);
            if (lines.Length == 0) return;

            // Auto-detect delimiter from first line
            Mapping.Delimiter = DetectDelimiter(lines[0]);
            
            Headers = SplitLine(lines[0], Mapping.Delimiter);
            DataRows = lines.Skip(1).Take(20).Select(l => SplitLine(l, Mapping.Delimiter)).ToList();
        }
        catch { }
    }
    
    /// <summary>
    /// Auto-detect the delimiter from file content
    /// </summary>
    private char DetectDelimiter(string headerLine)
    {
        // Count occurrences of common delimiters
        int commaCount = headerLine.Count(c => c == ',');
        int tabCount = headerLine.Count(c => c == '\t');
        int semiCount = headerLine.Count(c => c == ';');
        
        // Find delimiter with most occurrences (minimum 2 to be valid)
        if (commaCount >= tabCount && commaCount >= semiCount && commaCount >= 2)
            return ',';
        if (tabCount >= commaCount && tabCount >= semiCount && tabCount >= 2)
            return '\t';
        if (semiCount >= commaCount && semiCount >= tabCount && semiCount >= 2)
            return ';';
        
        // Default to comma for NPD files
        return ',';
    }

    public void AutoDetectColumns()
    {
        Mapping.HasDateTimeSplit = true;
        
        for (int i = 0; i < Headers.Length; i++)
        {
            var header = Headers[i].ToLowerInvariant().Trim();
            
            // Time/Date
            if (header.Contains("time") || header.Contains("date"))
            {
                Mapping.DateColumn = i;
                Mapping.TimeColumn = i + 1;
            }
            
            // Gyro/Heading (must not be transponder heading)
            if (header.Contains("gyro") || (header.Contains("heading") && !header.Contains("tp") && !header.Contains("transponder")))
                Mapping.VesselGyroColumn = i;
            
            // Vessel positions - MUST contain vessel/vsl/ship AND east/north, NOT height/depth
            else if ((header.Contains("vessel") || header.Contains("vsl") || header.Contains("ship")) && 
                     header.Contains("east") && !header.Contains("height") && !header.Contains("depth"))
                Mapping.VesselEastingColumn = i;
            else if ((header.Contains("vessel") || header.Contains("vsl") || header.Contains("ship")) && 
                     header.Contains("north") && !header.Contains("height") && !header.Contains("depth"))
                Mapping.VesselNorthingColumn = i;
            
            // Transponder positions
            else if ((header.Contains("tp") || header.Contains("transponder") || header.Contains("usbl") || header.Contains("beacon")) && header.Contains("east"))
                Mapping.TransponderEastingColumn = i;
            else if ((header.Contains("tp") || header.Contains("transponder") || header.Contains("usbl") || header.Contains("beacon")) && header.Contains("north"))
                Mapping.TransponderNorthingColumn = i;
            else if ((header.Contains("tp") || header.Contains("transponder") || header.Contains("usbl") || header.Contains("beacon")) && 
                     (header.Contains("depth") || header.Contains("height") || header.Contains("z")))
                Mapping.TransponderDepthColumn = i;
            
            // Depth/Height as standalone (for transponder depth)
            else if ((header.Contains("depth") || header.Contains("height")) && 
                     !header.Contains("vessel") && !header.Contains("vsl") && !header.Contains("ship") &&
                     Mapping.TransponderDepthColumn < 0)
                Mapping.TransponderDepthColumn = i;
        }
        
        // Fallback for generic columns if vessel-specific not found
        // CRITICAL: Exclude height/depth/z from northing detection
        if (Mapping.VesselEastingColumn < 0)
        {
            for (int i = 0; i < Headers.Length; i++)
            {
                var header = Headers[i].ToLowerInvariant();
                // Must contain east but NOT be transponder or height/depth
                if (header.Contains("east") && 
                    !header.Contains("tp") && !header.Contains("transponder") && !header.Contains("usbl") && !header.Contains("beacon") &&
                    !header.Contains("height") && !header.Contains("depth") &&
                    Mapping.TransponderEastingColumn != i)
                {
                    Mapping.VesselEastingColumn = i;
                    break;
                }
            }
        }
        
        if (Mapping.VesselNorthingColumn < 0)
        {
            for (int i = 0; i < Headers.Length; i++)
            {
                var header = Headers[i].ToLowerInvariant();
                // Must contain north but NOT be transponder or height/depth/z
                if (header.Contains("north") && 
                    !header.Contains("tp") && !header.Contains("transponder") && !header.Contains("usbl") && !header.Contains("beacon") &&
                    !header.Contains("height") && !header.Contains("depth") && !header.Contains("z") &&
                    Mapping.TransponderNorthingColumn != i)
                {
                    Mapping.VesselNorthingColumn = i;
                    break;
                }
            }
        }
        
        Validate();
    }

    public void Validate()
    {
        IsValid = Mapping.DateColumn >= 0 &&
                  Mapping.VesselEastingColumn >= 0 &&
                  Mapping.VesselNorthingColumn >= 0 &&
                  Mapping.VesselGyroColumn >= 0 &&
                  Mapping.TransponderEastingColumn >= 0 &&
                  Mapping.TransponderNorthingColumn >= 0 &&
                  Mapping.TransponderDepthColumn >= 0;
    }

    private string[] SplitLine(string line, char delimiter)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else current.Append(c);
        }
        result.Add(current.ToString().Trim());
        return result.ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
