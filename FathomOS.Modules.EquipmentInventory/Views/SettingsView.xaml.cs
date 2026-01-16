using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class SettingsView : System.Windows.Controls.UserControl
{
    private readonly SettingsViewViewModel _viewModel;
    
    public SettingsView(LocalDatabaseService dbService, SyncService syncService)
    {
        InitializeComponent();
        _viewModel = new SettingsViewViewModel(dbService, syncService);
        DataContext = _viewModel;
    }
}

public class SettingsViewViewModel : ViewModels.ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly SyncService _syncService;
    private readonly ModuleSettings _settings;
    
    public SettingsViewViewModel(LocalDatabaseService dbService, SyncService syncService)
    {
        _dbService = dbService;
        _syncService = syncService;
        _settings = ModuleSettings.Load();
        
        // Load settings
        CompanyName = _settings.CompanyName;
        AssetNumberPrefix = _settings.AssetNumberPrefix;
        ApiBaseUrl = _settings.ApiBaseUrl;
        AutoSyncEnabled = _settings.AutoSyncEnabled;
        SyncIntervalMinutes = _settings.SyncIntervalMinutes;
        IncludeLogoOnLabels = _settings.IncludeLogoOnLabels;
    }
    
    private string _companyName = "S7 Solutions";
    public string CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }
    
    private string _assetNumberPrefix = "S7-";
    public string AssetNumberPrefix { get => _assetNumberPrefix; set => SetProperty(ref _assetNumberPrefix, value); }
    
    private string _apiBaseUrl = "";
    public string ApiBaseUrl { get => _apiBaseUrl; set => SetProperty(ref _apiBaseUrl, value); }
    
    private bool _autoSyncEnabled = true;
    public bool AutoSyncEnabled { get => _autoSyncEnabled; set => SetProperty(ref _autoSyncEnabled, value); }
    
    private int _syncIntervalMinutes = 5;
    public int SyncIntervalMinutes { get => _syncIntervalMinutes; set => SetProperty(ref _syncIntervalMinutes, value); }
    
    private bool _includeLogoOnLabels = true;
    public bool IncludeLogoOnLabels { get => _includeLogoOnLabels; set => SetProperty(ref _includeLogoOnLabels, value); }
    
    public System.Windows.Input.ICommand SaveSettingsCommand => new ViewModels.RelayCommand(_ => SaveSettings());
    public System.Windows.Input.ICommand BrowseBackupLocationCommand => new ViewModels.RelayCommand(_ => BrowseBackupLocation());
    public System.Windows.Input.ICommand CreateBackupCommand => new ViewModels.RelayCommand(_ => CreateBackup());
    
    private void SaveSettings()
    {
        _settings.CompanyName = CompanyName;
        _settings.AssetNumberPrefix = AssetNumberPrefix;
        _settings.ApiBaseUrl = ApiBaseUrl;
        _settings.AutoSyncEnabled = AutoSyncEnabled;
        _settings.SyncIntervalMinutes = SyncIntervalMinutes;
        _settings.IncludeLogoOnLabels = IncludeLogoOnLabels;
        _settings.Save();
        
        MessageBox.Show("Settings saved successfully.", "Settings", 
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
    
    private void BrowseBackupLocation()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select backup location"
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _settings.BackupPath = dialog.SelectedPath;
        }
    }
    
    private async void CreateBackup()
    {
        try
        {
            var backupService = new BackupService(_dbService);
            await backupService.CreateBackupAsync(true, true, null);
            MessageBox.Show("Backup created successfully.", "Backup", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
