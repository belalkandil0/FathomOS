using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Import;
using FathomOS.Modules.EquipmentInventory.Models;


namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

public class ImportViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly ExcelImportService _importService;
    
    private string _filePath = string.Empty;
    private bool _updateExisting;
    private int _headerRowNumber = 1;
    private EquipmentCategory? _defaultCategory;
    private Location? _defaultLocation;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool? _dialogResult;
    private ImportResult? _result;
    
    public ImportViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _importService = new ExcelImportService(dbService);
        
        Categories = new ObservableCollection<EquipmentCategory>();
        Locations = new ObservableCollection<Location>();
        Messages = new ObservableCollection<string>();
        
        BrowseCommand = new RelayCommand(_ => BrowseFile());
        ImportCommand = new AsyncRelayCommand(async _ => await ImportAsync(), _ => !string.IsNullOrEmpty(FilePath) && !IsBusy);
        DownloadTemplateCommand = new RelayCommand(_ => DownloadTemplate());
        CloseCommand = new RelayCommand(_ => Close());
        
        _ = LoadLookupsAsync();
    }
    
    #region Properties
    
    public ObservableCollection<EquipmentCategory> Categories { get; }
    public ObservableCollection<Location> Locations { get; }
    public ObservableCollection<string> Messages { get; }
    
    public string FilePath
    {
        get => _filePath;
        set { SetProperty(ref _filePath, value); OnPropertyChanged(nameof(HasFile)); }
    }
    
    public bool HasFile => !string.IsNullOrEmpty(FilePath);
    
    public bool UpdateExisting
    {
        get => _updateExisting;
        set => SetProperty(ref _updateExisting, value);
    }
    
    public int HeaderRowNumber
    {
        get => _headerRowNumber;
        set => SetProperty(ref _headerRowNumber, Math.Max(1, value));
    }
    
    public EquipmentCategory? DefaultCategory
    {
        get => _defaultCategory;
        set => SetProperty(ref _defaultCategory, value);
    }
    
    public Location? DefaultLocation
    {
        get => _defaultLocation;
        set => SetProperty(ref _defaultLocation, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }
    
    public ImportResult? Result => _result;
    
    public bool HasCompleted => _result != null;
    
    #endregion
    
    #region Commands
    
    public ICommand BrowseCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand DownloadTemplateCommand { get; }
    public ICommand CloseCommand { get; }
    
    #endregion
    
    #region Methods
    
    private async Task LoadLookupsAsync()
    {
        var categories = await _dbService.GetCategoriesAsync();
        Categories.Clear();
        Categories.Add(new EquipmentCategory { Name = "(None - use file data)" });
        foreach (var c in categories) Categories.Add(c);
        
        var locations = await _dbService.GetLocationsAsync();
        Locations.Clear();
        Locations.Add(new Location { Name = "(None - use file data)" });
        foreach (var l in locations) Locations.Add(l);
    }
    
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Import File",
            Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
            Messages.Clear();
            StatusMessage = $"Selected: {Path.GetFileName(FilePath)}";
        }
    }
    
    private async Task ImportAsync()
    {
        if (string.IsNullOrEmpty(FilePath)) return;
        
        IsBusy = true;
        Messages.Clear();
        StatusMessage = "Importing...";
        
        try
        {
            var options = new ImportOptions
            {
                HeaderRowNumber = HeaderRowNumber,
                UpdateExisting = UpdateExisting,
                DefaultCategoryId = DefaultCategory?.CategoryId != Guid.Empty ? DefaultCategory?.CategoryId : null,
                DefaultLocationId = DefaultLocation?.LocationId != Guid.Empty ? DefaultLocation?.LocationId : null
            };
            
            _result = await _importService.ImportEquipmentAsync(FilePath, options);
            
            // Show results
            Messages.Add($"✓ Imported: {_result.Imported}");
            if (_result.Updated > 0)
                Messages.Add($"✓ Updated: {_result.Updated}");
            if (_result.Skipped > 0)
                Messages.Add($"○ Skipped: {_result.Skipped}");
            
            foreach (var warning in _result.Warnings.Take(10))
                Messages.Add($"⚠ {warning}");
            
            foreach (var error in _result.Errors.Take(10))
                Messages.Add($"✗ {error}");
            
            if (_result.Warnings.Count > 10)
                Messages.Add($"... and {_result.Warnings.Count - 10} more warnings");
            
            if (_result.Errors.Count > 10)
                Messages.Add($"... and {_result.Errors.Count - 10} more errors");
            
            StatusMessage = _result.Summary;
            OnPropertyChanged(nameof(HasCompleted));
        }
        catch (Exception ex)
        {
            Messages.Add($"✗ Import failed: {ex.Message}");
            StatusMessage = "Import failed";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void DownloadTemplate()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Import Template",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = "Equipment_Import_Template"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _importService.GenerateTemplate(dialog.FileName);
                StatusMessage = "Template saved";
                
                if (MessageBox.Show("Template saved. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save template: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void Close()
    {
        DialogResult = _result?.Success == true;
    }
    
    #endregion
}
