using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.ViewModels;

public class ReportBuilderViewModel : ViewModelBase
{
    private readonly ReportBuilderService _reportService;
    private readonly LocalDatabaseService _dbService;
    
    private string _reportTitle = "Equipment Report";
    private string _statusMessage = "Ready";
    private int _previewRowCount;
    private bool _isPortrait = true;
    private ReportConfiguration? _selectedPreset;
    private EquipmentCategory? _selectedCategory;
    private Location? _selectedLocation;
    private string? _selectedStatus;
    private bool _filterCertExpiring;
    private bool _filterCalOverdue;
    
    public ReportBuilderViewModel()
    {
        _dbService = new LocalDatabaseService();
        _reportService = new ReportBuilderService(_dbService);
        
        SelectedColumns = new ObservableCollection<ReportColumn>();
        PreviewData = new DataTable();
        Categories = new ObservableCollection<EquipmentCategory>();
        Locations = new ObservableCollection<Location>();
        Statuses = new ObservableCollection<string> { "", "Available", "InUse", "InTransit", "UnderRepair", "Retired" };
        
        LoadFieldCategories();
        LoadPresetTemplates();
        _ = LoadFiltersAsync();
        
        AddFieldCommand = new RelayCommand(AddField);
        RemoveColumnCommand = new RelayCommand(RemoveColumn);
        RefreshPreviewCommand = new RelayCommand(async _ => await RefreshPreviewAsync());
        ExportExcelCommand = new RelayCommand(async _ => await ExportExcelAsync());
        ExportPdfCommand = new RelayCommand(async _ => await ExportPdfAsync());
        SaveTemplateCommand = new RelayCommand(SaveTemplate);
    }
    
    #region Properties
    
    public string ReportTitle
    {
        get => _reportTitle;
        set => SetProperty(ref _reportTitle, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public int PreviewRowCount
    {
        get => _previewRowCount;
        set => SetProperty(ref _previewRowCount, value);
    }
    
    public bool IsPortrait
    {
        get => _isPortrait;
        set => SetProperty(ref _isPortrait, value);
    }
    
    public bool IsLandscape
    {
        get => !_isPortrait;
        set => SetProperty(ref _isPortrait, !value);
    }
    
    public ReportConfiguration? SelectedPreset
    {
        get => _selectedPreset;
        set { if (SetProperty(ref _selectedPreset, value) && value != null) LoadPreset(value); }
    }
    
    public EquipmentCategory? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }
    
    public Location? SelectedLocation
    {
        get => _selectedLocation;
        set => SetProperty(ref _selectedLocation, value);
    }
    
    public string? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }
    
    public bool FilterCertExpiring
    {
        get => _filterCertExpiring;
        set => SetProperty(ref _filterCertExpiring, value);
    }
    
    public bool FilterCalOverdue
    {
        get => _filterCalOverdue;
        set => SetProperty(ref _filterCalOverdue, value);
    }
    
    public ObservableCollection<FieldCategory> FieldsByCategory { get; } = new();
    public ObservableCollection<ReportColumn> SelectedColumns { get; }
    public ObservableCollection<ReportConfiguration> PresetTemplates { get; } = new();
    public ObservableCollection<EquipmentCategory> Categories { get; }
    public ObservableCollection<Location> Locations { get; }
    public ObservableCollection<string> Statuses { get; }
    public DataTable PreviewData { get; private set; }
    
    #endregion
    
    #region Commands
    
    public ICommand AddFieldCommand { get; }
    public ICommand RemoveColumnCommand { get; }
    public ICommand RefreshPreviewCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    
    #endregion
    
    #region Methods
    
    private void LoadFieldCategories()
    {
        var fields = _reportService.GetAvailableFields();
        var grouped = fields.GroupBy(f => f.Category);
        
        foreach (var group in grouped)
        {
            FieldsByCategory.Add(new FieldCategory
            {
                Category = group.Key,
                Fields = group.ToList()
            });
        }
    }
    
    private void LoadPresetTemplates()
    {
        foreach (var preset in _reportService.GetPresetTemplates())
        {
            PresetTemplates.Add(preset);
        }
    }
    
    private async Task LoadFiltersAsync()
    {
        var categories = await _dbService.GetCategoriesAsync();
        Categories.Clear();
        Categories.Add(new EquipmentCategory { Name = "All Categories" });
        foreach (var cat in categories)
        {
            Categories.Add(cat);
        }
        
        var locations = await _dbService.GetLocationsAsync();
        Locations.Clear();
        Locations.Add(new Location { Name = "All Locations" });
        foreach (var loc in locations)
        {
            Locations.Add(loc);
        }
    }
    
    private void LoadPreset(ReportConfiguration preset)
    {
        ReportTitle = preset.Title;
        SelectedColumns.Clear();
        foreach (var col in preset.Columns)
        {
            SelectedColumns.Add(col);
        }
    }
    
    private void AddField(object? parameter)
    {
        if (parameter is ReportField field)
        {
            if (SelectedColumns.Any(c => c.FieldId == field.Id))
                return;
            
            SelectedColumns.Add(new ReportColumn
            {
                FieldId = field.Id,
                DisplayName = field.Name,
                DataType = field.DataType
            });
        }
    }
    
    private void RemoveColumn(object? parameter)
    {
        if (parameter is ReportColumn column)
        {
            SelectedColumns.Remove(column);
        }
    }
    
    private ReportConfiguration BuildConfiguration()
    {
        var config = new ReportConfiguration
        {
            Title = ReportTitle,
            Columns = SelectedColumns.ToList(),
            Orientation = IsPortrait ? ReportOrientation.Portrait : ReportOrientation.Landscape,
            Filters = new ReportFilters()
        };
        
        if (SelectedCategory?.CategoryId != null && SelectedCategory.CategoryId != Guid.Empty)
            config.Filters.CategoryId = SelectedCategory.CategoryId;
        
        if (SelectedLocation?.LocationId != null && SelectedLocation.LocationId != Guid.Empty)
            config.Filters.LocationId = SelectedLocation.LocationId;
        
        if (!string.IsNullOrEmpty(SelectedStatus) && Enum.TryParse<EquipmentStatus>(SelectedStatus, out var status))
            config.Filters.Status = status;
        
        if (FilterCertExpiring)
            config.Filters.CertificationExpiringWithinDays = 30;
        
        return config;
    }
    
    private async Task RefreshPreviewAsync()
    {
        if (SelectedColumns.Count == 0)
        {
            StatusMessage = "Please select at least one column";
            return;
        }
        
        try
        {
            StatusMessage = "Loading preview...";
            var config = BuildConfiguration();
            var reportData = await _reportService.GenerateReportAsync(config);
            
            // Convert to DataTable for preview
            var dt = new DataTable();
            foreach (var col in reportData.Columns)
            {
                dt.Columns.Add(col.DisplayName);
            }
            
            foreach (var row in reportData.Rows.Take(100))
            {
                var dr = dt.NewRow();
                for (int i = 0; i < reportData.Columns.Count; i++)
                {
                    var value = row.GetValueOrDefault(reportData.Columns[i].FieldId);
                    dr[i] = value?.ToString() ?? "";
                }
                dt.Rows.Add(dr);
            }
            
            PreviewData = dt;
            OnPropertyChanged(nameof(PreviewData));
            PreviewRowCount = reportData.TotalRows;
            StatusMessage = $"Preview loaded - {reportData.TotalRows} total rows";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
    
    private async Task ExportExcelAsync()
    {
        if (SelectedColumns.Count == 0)
        {
            MessageBox.Show("Please select at least one column", "No Columns", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"{ReportTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            StatusMessage = "Exporting to Excel...";
            var config = BuildConfiguration();
            await _reportService.ExportToExcelAsync(config, dialog.FileName);
            StatusMessage = "Excel export complete";
            MessageBox.Show($"Report exported to:\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task ExportPdfAsync()
    {
        if (SelectedColumns.Count == 0)
        {
            MessageBox.Show("Please select at least one column", "No Columns", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new SaveFileDialog
        {
            Title = "Export to PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"{ReportTitle.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            StatusMessage = "Exporting to PDF...";
            var config = BuildConfiguration();
            await _reportService.ExportToPdfAsync(config, dialog.FileName);
            StatusMessage = "PDF export complete";
            MessageBox.Show($"Report exported to:\n{dialog.FileName}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void SaveTemplate(object? parameter)
    {
        if (!SelectedColumns.Any())
        {
            MessageBox.Show("Please add at least one column to the report.", "No Columns", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Use SaveFileDialog to get template name and save location
        var saveDialog = new SaveFileDialog
        {
            Title = "Save Report Template",
            Filter = "JSON Template (*.json)|*.json",
            FileName = string.IsNullOrEmpty(ReportTitle) ? "CustomTemplate" : ReportTitle.Replace(" ", "_"),
            InitialDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FathomOS", "EquipmentInventory", "ReportTemplates")
        };
        
        // Ensure directory exists
        if (!string.IsNullOrEmpty(saveDialog.InitialDirectory))
            Directory.CreateDirectory(saveDialog.InitialDirectory);
        
        if (saveDialog.ShowDialog() != true)
            return;
        
        var templateName = Path.GetFileNameWithoutExtension(saveDialog.FileName);
        
        try
        {
            var config = new ReportConfiguration
            {
                TemplateName = templateName,
                Title = ReportTitle,
                Orientation = IsPortrait ? ReportOrientation.Portrait : ReportOrientation.Landscape,
                Filters = new ReportFilters
                {
                    CategoryId = SelectedCategory?.CategoryId,
                    LocationId = SelectedLocation?.LocationId,
                    Status = string.IsNullOrEmpty(SelectedStatus) ? null : Enum.TryParse<EquipmentStatus>(SelectedStatus, out var status) ? status : null,
                    CertificationExpiringWithinDays = FilterCertExpiring ? 30 : null,
                    RequiresCalibration = FilterCalOverdue ? true : null
                },
                Columns = SelectedColumns.Select(c => new ReportColumn
                {
                    FieldId = c.FieldId,
                    DisplayName = c.DisplayName,
                    Width = c.Width
                }).ToList()
            };
            
            await _reportService.SaveTemplateAsync(config, templateName);
            
            StatusMessage = $"Template '{templateName}' saved successfully";
            MessageBox.Show($"Template '{templateName}' has been saved.", "Template Saved",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving template: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
}

public class FieldCategory
{
    public string Category { get; set; } = string.Empty;
    public List<ReportField> Fields { get; set; } = new();
}
