using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.Data;


namespace FathomOS.Modules.EquipmentInventory.ViewModels;

public class BackupRestoreViewModel : ViewModelBase
{
    private readonly BackupService _backupService;
    private readonly ModuleSettings _settings;
    
    private bool _isBusy;
    private string _busyMessage = "";
    private bool _includePhotos = true;
    private bool _includeDocuments = true;
    private bool _includeSettings = true;
    private bool _autoBackupEnabled;
    private BackupFrequency _backupFrequency = BackupFrequency.Daily;
    private int _keepBackupCount = 10;
    private BackupInfo? _selectedBackup;
    
    public BackupRestoreViewModel()
    {
        var dbService = new LocalDatabaseService();
        _backupService = new BackupService(dbService);
        _settings = ModuleSettings.Load();
        
        AvailableBackups = new ObservableCollection<BackupInfo>();
        FrequencyOptions = new[] { BackupFrequency.Daily, BackupFrequency.Weekly, BackupFrequency.Monthly };
        
        LoadSettings();
        
        CreateBackupCommand = new RelayCommand(async _ => await CreateBackupAsync());
        RestoreBackupCommand = new RelayCommand(async _ => await RestoreBackupAsync(), _ => SelectedBackup != null);
        DeleteBackupCommand = new RelayCommand(DeleteBackup);
        
        _ = LoadBackupsAsync();
    }
    
    #region Properties
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }
    
    public bool IncludePhotos
    {
        get => _includePhotos;
        set => SetProperty(ref _includePhotos, value);
    }
    
    public bool IncludeDocuments
    {
        get => _includeDocuments;
        set => SetProperty(ref _includeDocuments, value);
    }
    
    public bool IncludeSettings
    {
        get => _includeSettings;
        set => SetProperty(ref _includeSettings, value);
    }
    
    public bool AutoBackupEnabled
    {
        get => _autoBackupEnabled;
        set { if (SetProperty(ref _autoBackupEnabled, value)) SaveSettings(); }
    }
    
    public BackupFrequency BackupFrequency
    {
        get => _backupFrequency;
        set { if (SetProperty(ref _backupFrequency, value)) SaveSettings(); }
    }
    
    public int KeepBackupCount
    {
        get => _keepBackupCount;
        set { if (SetProperty(ref _keepBackupCount, value)) SaveSettings(); }
    }
    
    public BackupInfo? SelectedBackup
    {
        get => _selectedBackup;
        set => SetProperty(ref _selectedBackup, value);
    }
    
    public bool CanRestore => SelectedBackup != null;
    
    public string LastBackupText => _settings.BackupSettings.LastBackup.HasValue
        ? $"Last backup: {_settings.BackupSettings.LastBackup:g}"
        : "No backups created yet";
    
    public ObservableCollection<BackupInfo> AvailableBackups { get; }
    public BackupFrequency[] FrequencyOptions { get; }
    
    #endregion
    
    #region Commands
    
    public ICommand CreateBackupCommand { get; }
    public ICommand RestoreBackupCommand { get; }
    public ICommand DeleteBackupCommand { get; }
    
    #endregion
    
    #region Methods
    
    private void LoadSettings()
    {
        _autoBackupEnabled = _settings.BackupSettings.AutoBackupEnabled;
        _backupFrequency = _settings.BackupSettings.BackupFrequency;
        _keepBackupCount = _settings.BackupSettings.KeepBackupCount;
    }
    
    private void SaveSettings()
    {
        _settings.BackupSettings.AutoBackupEnabled = _autoBackupEnabled;
        _settings.BackupSettings.BackupFrequency = _backupFrequency;
        _settings.BackupSettings.KeepBackupCount = _keepBackupCount;
        _settings.Save();
    }
    
    private async Task LoadBackupsAsync()
    {
        var backups = _backupService.GetAvailableBackups();
        AvailableBackups.Clear();
        foreach (var backup in backups)
        {
            AvailableBackups.Add(backup);
        }
        await Task.CompletedTask; // Keep method async for consistency
    }
    
    private async Task CreateBackupAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Creating backup...";
            
            var result = await _backupService.CreateBackupAsync(IncludePhotos, IncludeDocuments);
            
            if (result.Success)
            {
                MessageBox.Show($"Backup created successfully!\n\nLocation: {result.BackupPath}\nSize: {result.BackupSize / 1024.0 / 1024.0:F2} MB",
                    "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadBackupsAsync();
                OnPropertyChanged(nameof(LastBackupText));
            }
            else
            {
                MessageBox.Show("Backup failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task RestoreBackupAsync()
    {
        if (SelectedBackup == null) return;
        
        var confirm = MessageBox.Show(
            "Are you sure you want to restore this backup?\n\nThis will replace all current data with the backup data. This action cannot be undone.",
            "Confirm Restore",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (confirm != MessageBoxResult.Yes) return;
        
        try
        {
            IsBusy = true;
            BusyMessage = "Restoring backup...";
            
            var result = await _backupService.RestoreFromBackupAsync(SelectedBackup.FilePath);
            
            if (result.Success)
            {
                MessageBox.Show("Backup restored successfully!\n\nPlease restart the application for changes to take effect.",
                    "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Restore failed. Your data has not been changed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void DeleteBackup(object? parameter)
    {
        if (parameter is not BackupInfo backup) return;
        
        var confirm = MessageBox.Show(
            $"Delete backup from {backup.BackupDate:g}?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (confirm != MessageBoxResult.Yes) return;
        
        if (_backupService.DeleteBackup(backup.FilePath))
        {
            AvailableBackups.Remove(backup);
        }
    }
    
    #endregion
}
