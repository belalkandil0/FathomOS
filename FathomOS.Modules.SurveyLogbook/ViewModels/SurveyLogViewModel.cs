// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: ViewModels/SurveyLogViewModel.cs
// Purpose: ViewModel for the Survey Log tab (Tab 1)
// ============================================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Services;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// ViewModel for the Survey Log tab. Manages the display and filtering
/// of survey log entries from all sources.
/// </summary>
public class SurveyLogViewModel : ViewModelBase
{
    private readonly LogEntryService _logService;
    private readonly ConnectionSettings _settings;
    private readonly ICollectionView _entriesView;
    
    private SurveyLogEntry? _selectedEntry;
    private string _filterText = string.Empty;
    private string _selectedCategory = "All";
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;
    private bool _showDvrEntries = true;
    private bool _showPositionFixes = true;
    private bool _showNaviPacEvents = true;
    private bool _showManualEntries = true;
    private List<UserFieldDefinition>? _fieldConfiguration;
    
    public SurveyLogViewModel(LogEntryService logService, ConnectionSettings settings)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        // Set up collection view for filtering
        _entriesView = CollectionViewSource.GetDefaultView(_logService.Entries);
        _entriesView.Filter = FilterEntry;
        _entriesView.SortDescriptions.Add(new SortDescription(nameof(SurveyLogEntry.Timestamp), ListSortDirection.Descending));
        
        // Subscribe to entry changes
        _logService.EntryAdded += OnEntryAdded;
        
        // Initialize commands
        AddManualEntryCommand = new RelayCommand(_ => AddManualEntry());
        DeleteEntryCommand = new RelayCommand(_ => DeleteEntry(), _ => SelectedEntry != null);
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
        ExportSelectionCommand = new RelayCommand(_ => ExportSelection(), _ => SelectedEntry != null);
        CopyToClipboardCommand = new RelayCommand(_ => CopyToClipboard(), _ => SelectedEntry != null);
        ViewDetailsCommand = new RelayCommand(_ => ViewDetails(), _ => SelectedEntry != null);
    }
    
    #region Properties
    
    public ICollectionView EntriesView => _entriesView;
    
    public ObservableCollection<SurveyLogEntry> Entries => _logService.Entries;
    
    public SurveyLogEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }
    
    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                _entriesView.Refresh();
        }
    }
    
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                _entriesView.Refresh();
        }
    }
    
    public DateTime? FilterStartDate
    {
        get => _filterStartDate;
        set
        {
            if (SetProperty(ref _filterStartDate, value))
                _entriesView.Refresh();
        }
    }
    
    public DateTime? FilterEndDate
    {
        get => _filterEndDate;
        set
        {
            if (SetProperty(ref _filterEndDate, value))
                _entriesView.Refresh();
        }
    }
    
    public bool ShowDvrEntries
    {
        get => _showDvrEntries;
        set
        {
            if (SetProperty(ref _showDvrEntries, value))
                _entriesView.Refresh();
        }
    }
    
    public bool ShowPositionFixes
    {
        get => _showPositionFixes;
        set
        {
            if (SetProperty(ref _showPositionFixes, value))
                _entriesView.Refresh();
        }
    }
    
    public bool ShowNaviPacEvents
    {
        get => _showNaviPacEvents;
        set
        {
            if (SetProperty(ref _showNaviPacEvents, value))
                _entriesView.Refresh();
        }
    }
    
    public bool ShowManualEntries
    {
        get => _showManualEntries;
        set
        {
            if (SetProperty(ref _showManualEntries, value))
                _entriesView.Refresh();
        }
    }
    
    /// <summary>
    /// Field configuration for dynamic columns.
    /// When changed, triggers column regeneration in the view.
    /// </summary>
    public List<UserFieldDefinition>? FieldConfiguration
    {
        get => _fieldConfiguration;
        set
        {
            if (SetProperty(ref _fieldConfiguration, value))
            {
                FieldConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    
    /// <summary>
    /// Event raised when field configuration changes, allowing views to regenerate columns.
    /// </summary>
    public event EventHandler? FieldConfigurationChanged;
    
    /// <summary>
    /// Gets the complete list of column definitions for the DataGrid.
    /// Combines core columns with user-defined dynamic columns.
    /// </summary>
    public List<DynamicColumnService.ColumnDefinition> GetColumnDefinitions()
    {
        var coreColumns = DynamicColumnService.GetCoreColumns();
        var dynamicColumns = DynamicColumnService.GetDynamicColumns(_fieldConfiguration);
        return coreColumns.Concat(dynamicColumns).ToList();
    }
    
    public string[] Categories => new[]
    {
        "All", "DVR", "Position Fixes", "NaviPac", "Waypoints", 
        "Survey Operations", "Manual", "System"
    };
    
    public int TotalEntries => _logService.Entries.Count;
    public int FilteredEntries => _entriesView.Cast<object>().Count();
    public int DvrCount => _logService.DvrRecordings.Count;
    public int PositionFixCount => _logService.PositionFixes.Count;
    
    #endregion
    
    #region Commands
    
    public ICommand AddManualEntryCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand ExportSelectionCommand { get; }
    public ICommand CopyToClipboardCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    
    #endregion
    
    #region Filter Logic
    
    private bool FilterEntry(object obj)
    {
        if (obj is not SurveyLogEntry entry)
            return false;
        
        // Category filter
        if (!FilterByCategory(entry))
            return false;
        
        // Type toggle filters
        if (!FilterByToggle(entry))
            return false;
        
        // Date range filter
        if (FilterStartDate.HasValue && entry.Timestamp < FilterStartDate.Value)
            return false;
        if (FilterEndDate.HasValue && entry.Timestamp > FilterEndDate.Value.AddDays(1))
            return false;
        
        // Text filter
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var searchText = FilterText.ToLowerInvariant();
            return entry.Description?.ToLowerInvariant().Contains(searchText) == true ||
                   entry.Source?.ToLowerInvariant().Contains(searchText) == true ||
                   entry.Vehicle?.ToLowerInvariant().Contains(searchText) == true ||
                   entry.Comments?.ToLowerInvariant().Contains(searchText) == true;
        }
        
        return true;
    }
    
    private bool FilterByCategory(SurveyLogEntry entry)
    {
        return SelectedCategory switch
        {
            "All" => true,
            "DVR" => entry.Category == "DVR",
            "Position Fixes" => entry.Category == "Position Fix",
            "NaviPac" => entry.Category == "NaviPac",
            "Waypoints" => entry.Category == "Waypoint",
            "Survey Operations" => entry.Category == "Survey" || entry.Category == "Calibration",
            "Manual" => entry.Category == "Manual",
            "System" => entry.Category == "System",
            _ => true
        };
    }
    
    private bool FilterByToggle(SurveyLogEntry entry)
    {
        var category = entry.Category;
        
        if (category == "DVR" && !ShowDvrEntries)
            return false;
        if (category == "Position Fix" && !ShowPositionFixes)
            return false;
        if (category == "NaviPac" && !ShowNaviPacEvents)
            return false;
        if (category == "Manual" && !ShowManualEntries)
            return false;
        
        return true;
    }
    
    #endregion
    
    #region Command Implementations
    
    private void AddManualEntry()
    {
        try
        {
            var dialog = new Views.ManualEntryDialog
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            
            if (dialog.ShowDialog() == true && dialog.CreatedEntry != null)
            {
                _logService.AddEntry(dialog.CreatedEntry);
                RefreshEntries();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding manual entry: {ex.Message}");
            System.Windows.MessageBox.Show($"Error adding entry: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private void DeleteEntry()
    {
        if (SelectedEntry == null) return;
        
        // Note: For now we don't actually delete - entries are log records
        SelectedEntry.IsExcluded = true;
        _entriesView.Refresh();
    }
    
    private void ClearFilter()
    {
        FilterText = string.Empty;
        SelectedCategory = "All";
        FilterStartDate = null;
        FilterEndDate = null;
        ShowDvrEntries = true;
        ShowPositionFixes = true;
        ShowNaviPacEvents = true;
        ShowManualEntries = true;
    }
    
    private void ExportSelection()
    {
        var selectedEntries = _entriesView.Cast<SurveyLogEntry>().ToList();
        if (selectedEntries.Count == 0)
        {
            System.Windows.MessageBox.Show("No entries to export.", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Survey Log Entries",
                Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                FileName = $"SurveyLog_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() != true) return;
            
            // Get column definitions
            var coreColumns = DynamicColumnService.GetCoreColumns();
            var dynamicColumns = DynamicColumnService.GetDynamicColumns(_fieldConfiguration);
            var allColumns = coreColumns.Concat(dynamicColumns).ToList();
            
            if (saveDialog.FilterIndex == 1) // Excel
            {
                ExportToExcel(saveDialog.FileName, selectedEntries, allColumns);
            }
            else // CSV
            {
                ExportToCsv(saveDialog.FileName, selectedEntries, allColumns);
            }
            
            System.Windows.MessageBox.Show($"Exported {selectedEntries.Count} entries to:\n{saveDialog.FileName}",
                "Export Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Exports entries to Excel with dynamic columns.
    /// </summary>
    private void ExportToExcel(string filePath, List<SurveyLogEntry> entries, List<DynamicColumnService.ColumnDefinition> columns)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var ws = workbook.Worksheets.Add("Survey Log");
        
        // Headers
        for (int col = 0; col < columns.Count; col++)
        {
            ws.Cell(1, col + 1).Value = columns[col].Header;
        }
        
        // Style header row
        ws.Row(1).Style.Font.Bold = true;
        ws.Row(1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightBlue;
        
        // Data rows
        for (int row = 0; row < entries.Count; row++)
        {
            var entry = entries[row];
            
            for (int col = 0; col < columns.Count; col++)
            {
                var colDef = columns[col];
                var value = DynamicColumnService.GetColumnValue(entry, colDef);
                
                if (value != null)
                {
                    if (value is DateTime dt)
                    {
                        ws.Cell(row + 2, col + 1).Value = dt;
                        ws.Cell(row + 2, col + 1).Style.NumberFormat.Format = colDef.StringFormat ?? "yyyy-MM-dd HH:mm:ss";
                    }
                    else if (value is double d)
                    {
                        ws.Cell(row + 2, col + 1).Value = d;
                        if (!string.IsNullOrEmpty(colDef.StringFormat))
                        {
                            var decimalPlaces = colDef.StringFormat.Replace("N", "").Replace("F", "");
                            if (int.TryParse(decimalPlaces, out var places))
                            {
                                ws.Cell(row + 2, col + 1).Style.NumberFormat.Format = "0." + new string('0', places);
                            }
                        }
                    }
                    else
                    {
                        ws.Cell(row + 2, col + 1).Value = value.ToString();
                    }
                }
            }
        }
        
        // Auto-fit columns
        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Exports entries to CSV with dynamic columns.
    /// </summary>
    private void ExportToCsv(string filePath, List<SurveyLogEntry> entries, List<DynamicColumnService.ColumnDefinition> columns)
    {
        var lines = new System.Collections.Generic.List<string>();
        
        // Header row
        lines.Add(string.Join(",", columns.Select(c => $"\"{c.Header}\"")));
        
        // Data rows
        foreach (var entry in entries)
        {
            var values = columns.Select(c => 
            {
                var value = DynamicColumnService.GetFormattedColumnValue(entry, c);
                // Escape quotes and wrap in quotes
                return $"\"{value.Replace("\"", "\"\"")}\"";
            });
            
            lines.Add(string.Join(",", values));
        }
        
        System.IO.File.WriteAllLines(filePath, lines);
    }
    
    private void CopyToClipboard()
    {
        if (SelectedEntry == null) return;
        
        var text = $"{SelectedEntry.TimeDisplay}\t{SelectedEntry.EntryTypeDisplay}\t{SelectedEntry.Description}";
        System.Windows.Clipboard.SetText(text);
    }
    
    private void ViewDetails()
    {
        if (SelectedEntry == null) return;
        
        var entry = SelectedEntry;
        var details = $"Entry Details\n" +
                      $"====================\n\n" +
                      $"Timestamp: {entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n" +
                      $"Type: {entry.EntryTypeDisplay}\n" +
                      $"Source: {entry.Source}\n" +
                      $"Object: {entry.ObjectName}\n\n" +
                      $"Description:\n{entry.Description}\n\n";
        
        if (entry.Easting.HasValue || entry.Northing.HasValue)
        {
            details += $"Position:\n" +
                      $"  Easting: {entry.Easting:F3}\n" +
                      $"  Northing: {entry.Northing:F3}\n";
            
            if (entry.Kp.HasValue)
                details += $"  KP: {entry.Kp:F3}\n";
            if (entry.Dcc.HasValue)
                details += $"  DCC: {entry.Dcc:F3}\n";
            if (entry.Depth.HasValue)
                details += $"  Depth: {entry.Depth:F2}\n";
        }
        
        if (!string.IsNullOrEmpty(entry.RawData))
        {
            details += $"\nRaw Data:\n{entry.RawData}\n";
        }
        
        System.Windows.MessageBox.Show(details, "Entry Details", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
    
    #endregion
    
    #region Public Methods
    
    public void RefreshEntries()
    {
        _entriesView.Refresh();
        OnPropertyChanged(nameof(TotalEntries));
        OnPropertyChanged(nameof(FilteredEntries));
        OnPropertyChanged(nameof(DvrCount));
        OnPropertyChanged(nameof(PositionFixCount));
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnEntryAdded(object? sender, LogEntryAddedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            _entriesView.Refresh();
            OnPropertyChanged(nameof(TotalEntries));
            OnPropertyChanged(nameof(FilteredEntries));
            OnPropertyChanged(nameof(DvrCount));
            OnPropertyChanged(nameof(PositionFixCount));
        });
    }
    
    #endregion
}
