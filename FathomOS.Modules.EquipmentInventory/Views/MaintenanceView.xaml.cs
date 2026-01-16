using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.ViewModels;
using FathomOS.Modules.EquipmentInventory.Views.Dialogs;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class MaintenanceView : System.Windows.Controls.UserControl
{
    private readonly MaintenanceViewModel _viewModel;
    
    public MaintenanceView(LocalDatabaseService dbService)
    {
        InitializeComponent();
        _viewModel = new MaintenanceViewModel(dbService);
        DataContext = _viewModel;
    }
}

public class MaintenanceViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    
    public MaintenanceViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        LoadData();
    }
    
    private int _maintenanceDueCount;
    public int MaintenanceDueCount { get => _maintenanceDueCount; set => SetProperty(ref _maintenanceDueCount, value); }
    
    private int _underRepairCount;
    public int UnderRepairCount { get => _underRepairCount; set => SetProperty(ref _underRepairCount, value); }
    
    private int _completedThisMonthCount;
    public int CompletedThisMonthCount { get => _completedThisMonthCount; set => SetProperty(ref _completedThisMonthCount, value); }
    
    public ObservableCollection<MaintenanceRecord> MaintenanceRecords { get; } = new();
    
    public ICommand NewMaintenanceRecordCommand => new RelayCommand(_ => NewMaintenanceRecord());
    public ICommand RefreshCommand => new RelayCommand(_ => LoadData());
    
    private async void LoadData()
    {
        try
        {
            var records = await _dbService.GetAllMaintenanceRecordsAsync();
            var equipment = await _dbService.GetAllEquipmentAsync();
            
            MaintenanceRecords.Clear();
            foreach (var record in records.OrderByDescending(r => r.PerformedDate).Take(50))
            {
                record.Equipment = equipment.FirstOrDefault(e => e.EquipmentId == record.EquipmentId);
                MaintenanceRecords.Add(record);
            }
            
            // Calculate counts
            var now = DateTime.UtcNow;
            UnderRepairCount = equipment.Count(e => e.Status == EquipmentStatus.UnderRepair);
            MaintenanceDueCount = equipment.Count(e => e.NextMaintenanceDate.HasValue && e.NextMaintenanceDate.Value <= now.AddDays(7));
            CompletedThisMonthCount = records.Count(r => r.PerformedDate.Month == now.Month && r.PerformedDate.Year == now.Year);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MaintenanceView load error: {ex.Message}");
        }
    }
    
    private void NewMaintenanceRecord()
    {
        try
        {
            var dialog = new MaintenanceRecordDialog(_dbService);
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true && dialog.SavedRecord != null)
            {
                // Reload data to show new record
                LoadData();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening maintenance dialog: {ex.Message}");
        }
    }
}
