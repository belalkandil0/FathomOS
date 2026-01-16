using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.ViewModels;
using FathomOS.Modules.EquipmentInventory.Views.Dialogs;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class CertificationView : System.Windows.Controls.UserControl
{
    private readonly CertificationViewModel _viewModel;
    
    public CertificationView(LocalDatabaseService dbService)
    {
        InitializeComponent();
        _viewModel = new CertificationViewModel(dbService);
        DataContext = _viewModel;
    }
}

public class CertificationViewModel : ViewModels.ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    
    public CertificationViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        LoadData();
    }
    
    private int _expiredCount;
    public int ExpiredCount { get => _expiredCount; set => SetProperty(ref _expiredCount, value); }
    
    private int _expiringSoonCount;
    public int ExpiringSoonCount { get => _expiringSoonCount; set => SetProperty(ref _expiringSoonCount, value); }
    
    private int _calibrationDueCount;
    public int CalibrationDueCount { get => _calibrationDueCount; set => SetProperty(ref _calibrationDueCount, value); }
    
    private int _validCount;
    public int ValidCount { get => _validCount; set => SetProperty(ref _validCount, value); }
    
    public ObservableCollection<Certification> Certifications { get; } = new();
    
    public ICommand AddCertificationCommand => new RelayCommand(_ => AddCertification());
    public ICommand RefreshCommand => new RelayCommand(_ => LoadData());
    
    private async void LoadData()
    {
        try
        {
            var certs = await _dbService.GetAllCertificationsAsync();
            var equipment = await _dbService.GetAllEquipmentAsync();
            var now = DateTime.UtcNow;
            
            ExpiredCount = certs.Count(c => c.ExpiryDate < now);
            ExpiringSoonCount = certs.Count(c => c.ExpiryDate >= now && c.ExpiryDate <= now.AddDays(30));
            ValidCount = certs.Count(c => c.ExpiryDate > now.AddDays(30));
            
            // Count equipment needing calibration
            CalibrationDueCount = equipment.Count(e => 
                e.RequiresCalibration && 
                e.NextCalibrationDate.HasValue && 
                e.NextCalibrationDate.Value <= now.AddDays(7));
            
            Certifications.Clear();
            foreach (var cert in certs.OrderBy(c => c.ExpiryDate))
            {
                cert.Equipment = equipment.FirstOrDefault(e => e.EquipmentId == cert.EquipmentId);
                Certifications.Add(cert);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CertificationView load error: {ex.Message}");
        }
    }
    
    private void AddCertification()
    {
        try
        {
            var dialog = new CertificationDialog(_dbService);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true && dialog.SavedCertification != null)
            {
                LoadData();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening certification dialog: {ex.Message}");
        }
    }
}
