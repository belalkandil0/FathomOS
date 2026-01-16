using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.Data;

namespace FathomOS.Modules.EquipmentInventory.ViewModels;

public class MaintenanceScheduleViewModel : ViewModelBase
{
    private readonly MaintenanceSchedulingService _maintenanceService;
    private MaintenanceSummary? _summary;
    private bool _filterAll = true;
    private bool _filterCertifications;
    private bool _filterCalibrations;
    private bool _filterServices;
    private bool _filterInspections;
    private bool _showOverdueOnly;
    
    public MaintenanceScheduleViewModel()
    {
        var dbService = new LocalDatabaseService();
        _maintenanceService = new MaintenanceSchedulingService(dbService);
        
        MaintenanceItems = new ObservableCollection<MaintenanceItem>();
        
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        CompleteMaintenanceCommand = new RelayCommand(CompleteMaintenance);
        ViewEquipmentCommand = new RelayCommand(ViewEquipment);
        
        _ = LoadDataAsync();
    }
    
    #region Properties
    
    public MaintenanceSummary? Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }
    
    public ObservableCollection<MaintenanceItem> MaintenanceItems { get; }
    
    public bool FilterAll
    {
        get => _filterAll;
        set { if (SetProperty(ref _filterAll, value) && value) _ = ApplyFiltersAsync(); }
    }
    
    public bool FilterCertifications
    {
        get => _filterCertifications;
        set { if (SetProperty(ref _filterCertifications, value) && value) _ = ApplyFiltersAsync(); }
    }
    
    public bool FilterCalibrations
    {
        get => _filterCalibrations;
        set { if (SetProperty(ref _filterCalibrations, value) && value) _ = ApplyFiltersAsync(); }
    }
    
    public bool FilterServices
    {
        get => _filterServices;
        set { if (SetProperty(ref _filterServices, value) && value) _ = ApplyFiltersAsync(); }
    }
    
    public bool FilterInspections
    {
        get => _filterInspections;
        set { if (SetProperty(ref _filterInspections, value) && value) _ = ApplyFiltersAsync(); }
    }
    
    public bool ShowOverdueOnly
    {
        get => _showOverdueOnly;
        set { if (SetProperty(ref _showOverdueOnly, value)) _ = ApplyFiltersAsync(); }
    }
    
    #endregion
    
    #region Commands
    
    public ICommand RefreshCommand { get; }
    public ICommand CompleteMaintenanceCommand { get; }
    public ICommand ViewEquipmentCommand { get; }
    
    #endregion
    
    #region Methods
    
    private List<MaintenanceItem> _allItems = new();
    
    private async Task LoadDataAsync()
    {
        Summary = await _maintenanceService.GetMaintenanceSummaryAsync();
        _allItems = await _maintenanceService.GetUpcomingMaintenanceAsync(90);
        await ApplyFiltersAsync();
    }
    
    private Task ApplyFiltersAsync()
    {
        var filtered = _allItems.AsEnumerable();
        
        if (FilterCertifications)
            filtered = filtered.Where(i => i.MaintenanceType == MaintenanceType.Certification);
        else if (FilterCalibrations)
            filtered = filtered.Where(i => i.MaintenanceType == MaintenanceType.Calibration);
        else if (FilterServices)
            filtered = filtered.Where(i => i.MaintenanceType == MaintenanceType.Service);
        else if (FilterInspections)
            filtered = filtered.Where(i => i.MaintenanceType == MaintenanceType.Inspection);
        
        if (ShowOverdueOnly)
            filtered = filtered.Where(i => i.IsOverdue);
        
        MaintenanceItems.Clear();
        foreach (var item in filtered)
        {
            MaintenanceItems.Add(item);
        }
        
        return Task.CompletedTask;
    }
    
    private void CompleteMaintenance(object? parameter)
    {
        if (parameter is MaintenanceItem item)
        {
            // Open completion dialog based on type
            System.Diagnostics.Debug.WriteLine($"Complete {item.MaintenanceType} for {item.AssetNumber}");
        }
    }
    
    private void ViewEquipment(object? parameter)
    {
        if (parameter is MaintenanceItem item)
        {
            System.Diagnostics.Debug.WriteLine($"View equipment: {item.EquipmentId}");
        }
    }
    
    #endregion
}
