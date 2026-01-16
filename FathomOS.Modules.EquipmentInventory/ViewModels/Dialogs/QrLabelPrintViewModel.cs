using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Export;
using FathomOS.Modules.EquipmentInventory.Models;


namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

public class QrLabelPrintViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly QrLabelPrintService _printService;
    
    private string _searchText = string.Empty;
    private Location? _selectedLocation;
    private EquipmentCategory? _selectedCategory;
    private string _selectedPreset = "Standard";
    private bool _showBorders = true;
    private bool _showName = true;
    private bool _showSerialNumber = true;
    private bool _showCategory = false;
    private bool _showCompanyName = true;
    private string _companyName = "S7 Solutions";
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool? _dialogResult;
    private Equipment? _selectedAvailable;
    private Equipment? _selectedToPrint;
    
    public QrLabelPrintViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _printService = new QrLabelPrintService();
        
        Locations = new ObservableCollection<Location>();
        Categories = new ObservableCollection<EquipmentCategory>();
        AvailableEquipment = new ObservableCollection<Equipment>();
        EquipmentToPrint = new ObservableCollection<Equipment>();
        
        InitializeCommands();
        _ = LoadLookupsAsync();
    }
    
    #region Properties
    
    public ObservableCollection<Location> Locations { get; }
    public ObservableCollection<EquipmentCategory> Categories { get; }
    public ObservableCollection<Equipment> AvailableEquipment { get; }
    public ObservableCollection<Equipment> EquipmentToPrint { get; }
    
    public string[] PresetOptions => new[] { "Small", "Standard", "Large" };
    
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) _ = SearchEquipmentAsync(); }
    }
    
    public Location? SelectedLocation
    {
        get => _selectedLocation;
        set { if (SetProperty(ref _selectedLocation, value)) _ = SearchEquipmentAsync(); }
    }
    
    public EquipmentCategory? SelectedCategory
    {
        get => _selectedCategory;
        set { if (SetProperty(ref _selectedCategory, value)) _ = SearchEquipmentAsync(); }
    }
    
    public string SelectedPreset
    {
        get => _selectedPreset;
        set => SetProperty(ref _selectedPreset, value);
    }
    
    public bool ShowBorders
    {
        get => _showBorders;
        set => SetProperty(ref _showBorders, value);
    }
    
    public bool ShowName
    {
        get => _showName;
        set => SetProperty(ref _showName, value);
    }
    
    public bool ShowSerialNumber
    {
        get => _showSerialNumber;
        set => SetProperty(ref _showSerialNumber, value);
    }
    
    public bool ShowCategory
    {
        get => _showCategory;
        set => SetProperty(ref _showCategory, value);
    }
    
    public bool ShowCompanyName
    {
        get => _showCompanyName;
        set => SetProperty(ref _showCompanyName, value);
    }
    
    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }
    
    public Equipment? SelectedAvailable
    {
        get => _selectedAvailable;
        set => SetProperty(ref _selectedAvailable, value);
    }
    
    public Equipment? SelectedToPrint
    {
        get => _selectedToPrint;
        set => SetProperty(ref _selectedToPrint, value);
    }
    
    public int LabelCount => EquipmentToPrint.Count;
    
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
    
    #endregion
    
    #region Commands
    
    public ICommand AddCommand { get; private set; } = null!;
    public ICommand AddAllCommand { get; private set; } = null!;
    public ICommand RemoveCommand { get; private set; } = null!;
    public ICommand ClearCommand { get; private set; } = null!;
    public ICommand PrintCommand { get; private set; } = null!;
    public ICommand PreviewCommand { get; private set; } = null!;
    public ICommand CloseCommand { get; private set; } = null!;
    
    private void InitializeCommands()
    {
        AddCommand = new RelayCommand(_ => Add(), _ => SelectedAvailable != null);
        AddAllCommand = new RelayCommand(_ => AddAll(), _ => AvailableEquipment.Any());
        RemoveCommand = new RelayCommand(_ => Remove(), _ => SelectedToPrint != null);
        ClearCommand = new RelayCommand(_ => Clear(), _ => EquipmentToPrint.Any());
        PrintCommand = new RelayCommand(_ => Print(), _ => EquipmentToPrint.Any() && !IsBusy);
        PreviewCommand = new RelayCommand(_ => Preview(), _ => EquipmentToPrint.Any() && !IsBusy);
        CloseCommand = new RelayCommand(_ => DialogResult = false);
    }
    
    #endregion
    
    #region Methods
    
    private async Task LoadLookupsAsync()
    {
        var locations = await _dbService.GetLocationsAsync();
        Locations.Clear();
        Locations.Add(new Location { Name = "(All Locations)" });
        foreach (var l in locations) Locations.Add(l);
        
        var categories = await _dbService.GetCategoriesAsync();
        Categories.Clear();
        Categories.Add(new EquipmentCategory { Name = "(All Categories)" });
        foreach (var c in categories) Categories.Add(c);
        
        await SearchEquipmentAsync();
    }
    
    private async Task SearchEquipmentAsync()
    {
        try
        {
            var equipment = await _dbService.GetEquipmentAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                locationId: SelectedLocation?.LocationId == Guid.Empty ? null : SelectedLocation?.LocationId,
                categoryId: SelectedCategory?.CategoryId == Guid.Empty ? null : SelectedCategory?.CategoryId
            );
            
            AvailableEquipment.Clear();
            foreach (var e in equipment.Where(eq => !EquipmentToPrint.Any(p => p.EquipmentId == eq.EquipmentId)))
            {
                AvailableEquipment.Add(e);
            }
            
            StatusMessage = $"Found {AvailableEquipment.Count} items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
    }
    
    private void Add()
    {
        if (SelectedAvailable == null) return;
        EquipmentToPrint.Add(SelectedAvailable);
        AvailableEquipment.Remove(SelectedAvailable);
        SelectedAvailable = null;
        OnPropertyChanged(nameof(LabelCount));
    }
    
    private void AddAll()
    {
        foreach (var e in AvailableEquipment.ToList())
        {
            EquipmentToPrint.Add(e);
        }
        AvailableEquipment.Clear();
        OnPropertyChanged(nameof(LabelCount));
    }
    
    private void Remove()
    {
        if (SelectedToPrint == null) return;
        AvailableEquipment.Add(SelectedToPrint);
        EquipmentToPrint.Remove(SelectedToPrint);
        SelectedToPrint = null;
        OnPropertyChanged(nameof(LabelCount));
    }
    
    private void Clear()
    {
        foreach (var e in EquipmentToPrint.ToList())
        {
            AvailableEquipment.Add(e);
        }
        EquipmentToPrint.Clear();
        OnPropertyChanged(nameof(LabelCount));
    }
    
    private LabelOptions GetLabelOptions()
    {
        var options = SelectedPreset switch
        {
            "Small" => LabelOptions.SmallLabel,
            "Large" => LabelOptions.LargeLabel,
            _ => LabelOptions.StandardLabel
        };
        
        options.ShowBorder = ShowBorders;
        options.ShowName = ShowName;
        options.ShowSerialNumber = ShowSerialNumber;
        options.ShowCategory = ShowCategory;
        options.ShowCompanyName = ShowCompanyName;
        options.CompanyName = CompanyName;
        
        return options;
    }
    
    private void Print()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save QR Labels",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"QR_Labels_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Generating labels...";
            
            var options = GetLabelOptions();
            _printService.GenerateMultiPageLabels(dialog.FileName, EquipmentToPrint, options);
            
            StatusMessage = $"Saved {LabelCount} labels";
            
            if (MessageBox.Show("Labels saved. Open PDF?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed: {ex.Message}";
            MessageBox.Show($"Failed to generate labels:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void Preview()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Generating preview...";
            
            var tempPath = Path.Combine(Path.GetTempPath(), $"QR_Labels_Preview_{Guid.NewGuid():N}.pdf");
            var options = GetLabelOptions();
            
            // Just generate first page worth
            var previewItems = EquipmentToPrint.Take(options.LabelsPerRow * options.LabelsPerColumn);
            _printService.GenerateLabelSheet(tempPath, previewItems, options);
            
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });
            StatusMessage = "Preview opened";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    #endregion
}
