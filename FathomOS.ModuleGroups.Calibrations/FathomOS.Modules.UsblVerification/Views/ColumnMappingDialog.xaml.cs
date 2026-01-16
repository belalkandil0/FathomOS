using System;
using System.Collections.Generic;
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

public partial class ColumnMappingDialog : MetroWindow
{
    private readonly string _filePath;
    private string[] _headers = Array.Empty<string>();
    private List<string[]> _dataRows = new();
    
    public UsblColumnMapping ResultMapping { get; private set; } = new();
    public bool DialogConfirmed { get; private set; }

    public ColumnMappingDialog(string filePath)
    {
        // Load theme
        var themeUri = new Uri("/FathomOS.Modules.UsblVerification;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        _filePath = filePath;
        FileNameText.Text = Path.GetFileName(filePath);
        
        Loaded += ColumnMappingDialog_Loaded;
    }

    private void ColumnMappingDialog_Loaded(object sender, RoutedEventArgs e)
    {
        LoadFileAndDetectColumns();
    }

    private void LoadFileAndDetectColumns()
    {
        try
        {
            var lines = File.ReadAllLines(_filePath);
            
            if (lines.Length == 0)
            {
                ShowValidationError("File is empty");
                return;
            }

            // Auto-detect delimiter from file content
            var detectedDelimiter = DetectDelimiter(lines[0]);
            SetDelimiterComboBox(detectedDelimiter);
            
            var delimiter = detectedDelimiter;

            // Parse headers
            _headers = SplitLine(lines[0], delimiter);
            
            // Parse data rows (skip header, take up to 20)
            int headerRows = (int)HeaderRowsUpDown.Value;
            _dataRows = lines.Skip(headerRows)
                             .Take(20)
                             .Select(l => SplitLine(l, delimiter))
                             .ToList();

            // Populate column dropdowns
            PopulateColumnDropdowns();
            
            // Auto-detect columns
            AutoDetectColumns();
            
            // Show preview
            UpdatePreview();
            
            // Validate
            ValidateMapping();
        }
        catch (Exception ex)
        {
            ShowValidationError($"Error loading file: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Auto-detect the delimiter from file content
    /// </summary>
    private char DetectDelimiter(string headerLine)
    {
        // Count occurrences of common delimiters
        var delimiters = new Dictionary<char, int>
        {
            { ',', headerLine.Count(c => c == ',') },
            { '\t', headerLine.Count(c => c == '\t') },
            { ';', headerLine.Count(c => c == ';') },
            { ' ', 0 } // Don't auto-detect space - too common in text
        };
        
        // Find delimiter with most occurrences (minimum 2 to be valid)
        var best = delimiters.OrderByDescending(kvp => kvp.Value).First();
        
        if (best.Value >= 2)
        {
            return best.Key;
        }
        
        // Default to comma for NPD files
        return ',';
    }
    
    /// <summary>
    /// Set the delimiter combo box to match detected delimiter
    /// </summary>
    private void SetDelimiterComboBox(char delimiter)
    {
        int index = delimiter switch
        {
            '\t' => 0,  // Tab
            ',' => 1,   // Comma
            ';' => 2,   // Semicolon
            ' ' => 3,   // Space
            _ => 1      // Default to comma
        };
        
        DelimiterCombo.SelectedIndex = index;
    }

    private void PopulateColumnDropdowns()
    {
        var columnItems = new List<ComboBoxItem>
        {
            new ComboBoxItem { Content = "(Not Set)", Tag = -1 }
        };

        for (int i = 0; i < _headers.Length; i++)
        {
            columnItems.Add(new ComboBoxItem 
            { 
                Content = $"[{i}] {_headers[i]}", 
                Tag = i 
            });
        }

        var combos = new[] 
        { 
            DateColumnCombo, VesselEastingCombo, VesselNorthingCombo, VesselGyroCombo,
            TransponderEastingCombo, TransponderNorthingCombo, TransponderDepthCombo 
        };

        foreach (var combo in combos)
        {
            combo.Items.Clear();
            foreach (var item in columnItems)
            {
                combo.Items.Add(new ComboBoxItem 
                { 
                    Content = item.Content, 
                    Tag = item.Tag 
                });
            }
            combo.SelectedIndex = 0;
        }
    }

    private void AutoDetectColumns()
    {
        // Enhanced auto-detection with multiple patterns
        // Order matters - more specific patterns first
        var patterns = new Dictionary<ComboBox, string[]>
        {
            { DateColumnCombo, new[] { "time", "date", "datetime", "timestamp" } },
            { VesselGyroCombo, new[] { "gyro", "heading", "hdg", "cog", "course", "vessel.*head", "ship.*head" } },
            // Vessel positions - exclude height/depth/z patterns
            { VesselEastingCombo, new[] { "vessel.*east", "vsl.*east", "ship.*east", "surface.*east" } },
            { VesselNorthingCombo, new[] { "vessel.*north", "vsl.*north", "ship.*north", "surface.*north" } },
            // Transponder positions
            { TransponderEastingCombo, new[] { "tp.*east", "transponder.*east", "usbl.*east", "beacon.*east", "acoustic.*east", "subsea.*east", "target.*east" } },
            { TransponderNorthingCombo, new[] { "tp.*north", "transponder.*north", "usbl.*north", "beacon.*north", "acoustic.*north", "subsea.*north", "target.*north" } },
            { TransponderDepthCombo, new[] { "tp.*depth", "tp.*height", "tp.*z", "transponder.*depth", "transponder.*height", "usbl.*depth", "usbl.*height", "beacon.*depth", "beacon.*height", "acoustic.*depth", "subsea.*depth", "target.*depth", "depth", "height" } }
        };

        foreach (var kvp in patterns)
        {
            int bestMatch = FindBestColumnMatch(kvp.Value, kvp.Key);
            if (bestMatch >= 0)
            {
                kvp.Key.SelectedIndex = bestMatch + 1; // +1 for "(Not Set)" item
            }
        }

        // Special handling: If we didn't find vessel-specific columns, try generic ones
        // But EXCLUDE height/depth/z columns for northing
        if (GetSelectedIndex(VesselEastingCombo) < 0)
        {
            int eastIdx = FindGenericColumnByPattern(new[] { "east", "easting" }, new[] { "tp", "transponder", "usbl", "beacon" });
            if (eastIdx >= 0 && GetSelectedIndex(TransponderEastingCombo) != eastIdx)
            {
                VesselEastingCombo.SelectedIndex = eastIdx + 1;
            }
        }

        if (GetSelectedIndex(VesselNorthingCombo) < 0)
        {
            // CRITICAL: Exclude height, depth, z columns from northing detection
            int northIdx = FindGenericColumnByPattern(new[] { "north", "northing" }, new[] { "tp", "transponder", "usbl", "beacon", "height", "depth", "z" });
            if (northIdx >= 0 && GetSelectedIndex(TransponderNorthingCombo) != northIdx)
            {
                VesselNorthingCombo.SelectedIndex = northIdx + 1;
            }
        }

        // Check for NaviPac date/time split by looking at data
        DetectNaviPacFormat();
    }
    
    /// <summary>
    /// Find best column match with exclusion patterns
    /// </summary>
    private int FindBestColumnMatch(string[] patterns, ComboBox targetCombo)
    {
        // Determine exclusion patterns based on target
        string[] exclusions = Array.Empty<string>();
        if (targetCombo == VesselNorthingCombo || targetCombo == VesselEastingCombo)
        {
            exclusions = new[] { "height", "depth", "z", "tp", "transponder", "usbl", "beacon" };
        }
        
        for (int i = 0; i < _headers.Length; i++)
        {
            string header = _headers[i].ToLowerInvariant().Trim();
            
            // Skip if matches any exclusion
            if (exclusions.Any(ex => header.Contains(ex)))
                continue;
            
            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith("^"))
                {
                    var regex = new System.Text.RegularExpressions.Regex(pattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (regex.IsMatch(header))
                        return i;
                }
                else if (header.Contains(pattern) || 
                         System.Text.RegularExpressions.Regex.IsMatch(header, pattern.Replace(".*", ".*"), 
                             System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return i;
                }
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Find column by pattern with exclusions
    /// </summary>
    private int FindGenericColumnByPattern(string[] includePatterns, string[] excludePatterns)
    {
        for (int i = 0; i < _headers.Length; i++)
        {
            string header = _headers[i].ToLowerInvariant().Trim();
            
            // Skip if matches any exclusion
            if (excludePatterns.Any(ex => header.Contains(ex)))
                continue;
            
            // Check if matches any include pattern
            if (includePatterns.Any(p => header.Contains(p)))
                return i;
        }
        return -1;
    }

    private void DetectNaviPacFormat()
    {
        if (_dataRows.Count == 0) return;

        // Check if first data row has more columns than headers (NaviPac sign)
        var firstDataRow = _dataRows[0];
        
        if (firstDataRow.Length > _headers.Length)
        {
            DateTimeSplitCheck.IsChecked = true;
            return;
        }

        // Check if first column looks like a date and second like a time
        if (firstDataRow.Length >= 2)
        {
            bool firstIsDate = DateTime.TryParseExact(firstDataRow[0], 
                new[] { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
            
            bool secondIsTime = TimeSpan.TryParseExact(firstDataRow[1], 
                new[] { "HH:mm:ss", "hh:mm:ss", "HH:mm:ss.fff" }, 
                CultureInfo.InvariantCulture, out _);

            if (firstIsDate && secondIsTime)
            {
                DateTimeSplitCheck.IsChecked = true;
            }
        }
    }

    private void UpdatePreview()
    {
        try
        {
            var dt = new DataTable();
            
            // Add columns
            for (int i = 0; i < _headers.Length; i++)
            {
                dt.Columns.Add(_headers[i]);
            }
            
            // Add rows
            foreach (var row in _dataRows)
            {
                var dataRow = dt.NewRow();
                for (int i = 0; i < Math.Min(row.Length, _headers.Length); i++)
                {
                    dataRow[i] = row[i];
                }
                dt.Rows.Add(dataRow);
            }

            PreviewDataGrid.ItemsSource = dt.DefaultView;
            PreviewStatusText.Text = $"Showing {_dataRows.Count} rows, {_headers.Length} columns";
        }
        catch (Exception ex)
        {
            PreviewStatusText.Text = $"Preview error: {ex.Message}";
        }
    }

    private void ValidateMapping()
    {
        var issues = new List<string>();

        if (GetSelectedIndex(DateColumnCombo) < 0)
            issues.Add("Date/Time column not set");
        if (GetSelectedIndex(VesselEastingCombo) < 0)
            issues.Add("Vessel Easting not set");
        if (GetSelectedIndex(VesselNorthingCombo) < 0)
            issues.Add("Vessel Northing not set");
        if (GetSelectedIndex(VesselGyroCombo) < 0)
            issues.Add("Vessel Gyro/Heading not set");
        if (GetSelectedIndex(TransponderEastingCombo) < 0)
            issues.Add("Transponder Easting not set");
        if (GetSelectedIndex(TransponderNorthingCombo) < 0)
            issues.Add("Transponder Northing not set");
        if (GetSelectedIndex(TransponderDepthCombo) < 0)
            issues.Add("Transponder Depth not set");

        if (issues.Count > 0)
        {
            ShowValidationWarning(string.Join(", ", issues));
            ApplyButton.IsEnabled = false;
        }
        else
        {
            ShowValidationSuccess("All required columns mapped correctly");
            ApplyButton.IsEnabled = true;
        }
    }

    private int GetSelectedIndex(ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item && item.Tag is int idx)
            return idx;
        return -1;
    }

    private void ShowValidationSuccess(string message)
    {
        ValidationIcon.Kind = PackIconMaterialKind.CheckCircle;
        ValidationIcon.Foreground = FindResource("SuccessBrush") as Brush ?? Brushes.Green;
        ValidationText.Text = message;
    }

    private void ShowValidationWarning(string message)
    {
        ValidationIcon.Kind = PackIconMaterialKind.AlertCircle;
        ValidationIcon.Foreground = FindResource("WarningBrush") as Brush ?? Brushes.Orange;
        ValidationText.Text = message;
    }

    private void ShowValidationError(string message)
    {
        ValidationIcon.Kind = PackIconMaterialKind.CloseCircle;
        ValidationIcon.Foreground = FindResource("ErrorBrush") as Brush ?? Brushes.Red;
        ValidationText.Text = message;
    }

    private char GetSelectedDelimiter()
    {
        // Use index-based selection for reliability
        return DelimiterCombo.SelectedIndex switch
        {
            0 => '\t',  // Tab
            1 => ',',   // Comma
            2 => ';',   // Semicolon
            3 => ' ',   // Space
            _ => ','    // Default to comma
        };
    }

    private string[] SplitLine(string line, char delimiter)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());

        return result.ToArray();
    }

    private void BuildResultMapping()
    {
        ResultMapping = new UsblColumnMapping
        {
            Delimiter = GetSelectedDelimiter(),
            HeaderRows = (int)HeaderRowsUpDown.Value,
            HasDateTimeSplit = DateTimeSplitCheck.IsChecked == true,
            DateColumn = GetSelectedIndex(DateColumnCombo),
            TimeColumn = GetSelectedIndex(DateColumnCombo) + 1, // NaviPac: next column
            VesselEastingColumn = GetSelectedIndex(VesselEastingCombo),
            VesselNorthingColumn = GetSelectedIndex(VesselNorthingCombo),
            VesselGyroColumn = GetSelectedIndex(VesselGyroCombo),
            TransponderEastingColumn = GetSelectedIndex(TransponderEastingCombo),
            TransponderNorthingColumn = GetSelectedIndex(TransponderNorthingCombo),
            TransponderDepthColumn = GetSelectedIndex(TransponderDepthCombo)
        };
    }

    #region Event Handlers

    private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
    {
        AutoDetectColumns();
        ValidateMapping();
    }

    private void DelimiterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            LoadFileAndDetectColumns();
        }
    }

    private void HeaderRowsUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
    {
        if (IsLoaded)
        {
            LoadFileAndDetectColumns();
        }
    }

    private void DateTimeSplitCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            ValidateMapping();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogConfirmed = false;
        DialogResult = false;
        Close();
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        BuildResultMapping();
        DialogConfirmed = true;
        DialogResult = true;
        Close();
    }

    #endregion
}
