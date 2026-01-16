using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class MaintenanceRecordDialog : MetroWindow
{
    public MaintenanceRecordDialog(LocalDatabaseService dbService, MaintenanceRecord? record = null, Equipment? equipment = null)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        var viewModel = new MaintenanceRecordViewModel(dbService, record, equipment);
        viewModel.RequestClose += (s, result) =>
        {
            DialogResult = result;
            Close();
        };
        DataContext = viewModel;
    }
    
    public MaintenanceRecord? SavedRecord => (DataContext as MaintenanceRecordViewModel)?.SavedRecord;
}

public class MaintenanceRecordViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly MaintenanceRecord _record;
    private readonly bool _isNew;
    
    public MaintenanceRecordViewModel(LocalDatabaseService dbService, MaintenanceRecord? record = null, Equipment? equipment = null)
    {
        _dbService = dbService;
        _isNew = record == null;
        _record = record ?? new MaintenanceRecord
        {
            MaintenanceId = Guid.NewGuid(),
            PerformedDate = DateTime.Today,
            MaintenanceType = MaintenanceType.Preventive
        };
        
        if (equipment != null)
        {
            _record.EquipmentId = equipment.EquipmentId;
            SelectedEquipment = equipment;
        }
        
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false));
        
        LoadData();
    }
    
    public event EventHandler<bool>? RequestClose;
    
    public MaintenanceRecord? SavedRecord { get; private set; }
    
    public string DialogTitle => _isNew ? "Log Maintenance Record" : "Edit Maintenance Record";
    
    public ObservableCollection<Equipment> EquipmentList { get; } = new();
    
    public List<MaintenanceType> MaintenanceTypes { get; } = Enum.GetValues<MaintenanceType>().ToList();
    
    private Equipment? _selectedEquipment;
    public Equipment? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            if (SetProperty(ref _selectedEquipment, value))
            {
                if (value != null)
                    _record.EquipmentId = value.EquipmentId;
                OnPropertyChanged(nameof(HasSelectedEquipment));
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }
    
    public bool HasSelectedEquipment => SelectedEquipment != null;
    
    public MaintenanceType MaintenanceType
    {
        get => _record.MaintenanceType;
        set { _record.MaintenanceType = value; OnPropertyChanged(); }
    }
    
    public string? Description
    {
        get => _record.Description;
        set { _record.Description = value; OnPropertyChanged(); }
    }
    
    public DateTime PerformedDate
    {
        get => _record.PerformedDate;
        set { _record.PerformedDate = value; OnPropertyChanged(); }
    }
    
    public DateTime? NextDueDate
    {
        get => _record.NextDueDate;
        set { _record.NextDueDate = value; OnPropertyChanged(); }
    }
    
    public string? PerformedBy
    {
        get => _record.PerformedBy;
        set { _record.PerformedBy = value; OnPropertyChanged(); }
    }
    
    public decimal? Cost
    {
        get => _record.Cost;
        set { _record.Cost = value; OnPropertyChanged(); }
    }
    
    public string? WorkOrderNumber
    {
        get => _record.WorkOrderNumber;
        set { _record.WorkOrderNumber = value; OnPropertyChanged(); }
    }
    
    public string? Notes
    {
        get => _record.Notes;
        set { _record.Notes = value; OnPropertyChanged(); }
    }
    
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool CanSave => SelectedEquipment != null;
    
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    
    private async void LoadData()
    {
        try
        {
            var equipment = await _dbService.GetAllEquipmentAsync();
            EquipmentList.Clear();
            foreach (var eq in equipment.Where(e => e.IsActive).OrderBy(e => e.Name))
            {
                EquipmentList.Add(eq);
            }
            
            // If editing, select the equipment
            if (!_isNew && _record.EquipmentId != Guid.Empty)
            {
                SelectedEquipment = EquipmentList.FirstOrDefault(e => e.EquipmentId == _record.EquipmentId);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
        }
    }
    
    private async void Save()
    {
        try
        {
            StatusMessage = "Saving...";
            
            await _dbService.SaveMaintenanceRecordAsync(_record);
            
            // Update equipment next service date if provided
            if (SelectedEquipment != null && NextDueDate.HasValue)
            {
                SelectedEquipment.NextServiceDate = NextDueDate;
                SelectedEquipment.LastServiceDate = PerformedDate;
                await _dbService.SaveEquipmentAsync(SelectedEquipment);
            }
            
            SavedRecord = _record;
            RequestClose?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }
}
