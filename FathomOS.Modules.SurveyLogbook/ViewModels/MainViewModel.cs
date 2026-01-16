// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: ViewModels/MainViewModel.cs
// Purpose: Main ViewModel for the Survey Electronic Logbook window
// ============================================================================

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Services;
using FathomOS.Modules.SurveyLogbook.Views;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// Main ViewModel for the Survey Electronic Logbook module.
/// Manages four tabs (Survey Log, DVR Recordings, Position Fixes, and DPR) 
/// and global functionality including connection management and export.
/// </summary>
public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ConnectionSettings _settings;
    private readonly LogEntryService _logService;
    private readonly DispatcherTimer _clockTimer;
    private ApplicationSettings _appSettings;
    
    private int _selectedTabIndex;
    private string _statusMessage = "Ready";
    private string _busyMessage = "Loading...";
    private bool _isBusy;
    private bool _isConnected;
    private string _connectionStatus = "Disconnected";
    private DateTime _currentTime = DateTime.Now;
    
    private SurveyLogViewModel? _surveyLogViewModel;
    private DvrRecordingsViewModel? _dvrRecordingsViewModel;
    private PositionFixesViewModel? _positionFixesViewModel;
    private DprViewModel? _dprViewModel;
    private DataMonitorWindow? _dataMonitorWindow;
    
    public MainViewModel()
    {
        _settings = ConnectionSettings.Load();
        _appSettings = ApplicationSettings.Load();
        _logService = new LogEntryService(_settings);
        
        _logService.StatusChanged += OnServiceStatusChanged;
        
        // Initialize sub-ViewModels
        _surveyLogViewModel = new SurveyLogViewModel(_logService, _settings);
        _dvrRecordingsViewModel = new DvrRecordingsViewModel();
        _positionFixesViewModel = new PositionFixesViewModel();
        _dprViewModel = new DprViewModel(_logService, _settings);
        
        // Apply settings to child ViewModels
        ApplySettingsToViewModels();
        
        // Initialize commands
        StartCommand = new AsyncRelayCommand(StartAsync, _ => !IsConnected && !IsBusy);
        StopCommand = new RelayCommand(_ => StopInternal(), _ => IsConnected && !IsBusy);
        NewCommand = new RelayCommand(_ => NewLogFile());
        SaveCommand = new AsyncRelayCommand(SaveAsync, _ => !IsBusy);
        LoadCommand = new AsyncRelayCommand(LoadAsync, _ => !IsBusy);
        ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, _ => !IsBusy);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, _ => !IsBusy);
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        OpenDataMonitorCommand = new RelayCommand(_ => OpenDataMonitor());
        OpenFieldConfigurationCommand = new RelayCommand(_ => OpenFieldConfiguration());
        RefreshCommand = new RelayCommand(_ => Refresh());
        
        // Initialize clock timer
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => CurrentTime = DateTime.Now;
        _clockTimer.Start();
    }
    
    #region Properties
    
    /// <summary>
    /// ViewModel for the Survey Events tab.
    /// </summary>
    public SurveyLogViewModel SurveyLogViewModel => _surveyLogViewModel!;
    
    /// <summary>
    /// ViewModel for the DVR Recordings tab.
    /// </summary>
    public DvrRecordingsViewModel DvrRecordingsViewModel => _dvrRecordingsViewModel!;
    
    /// <summary>
    /// ViewModel for the Position Fixes tab.
    /// </summary>
    public PositionFixesViewModel PositionFixesViewModel => _positionFixesViewModel!;
    
    /// <summary>
    /// ViewModel for the DPR / Shift Handover tab.
    /// </summary>
    public DprViewModel DprViewModel => _dprViewModel!;
    
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }
    
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (SetProperty(ref _isConnected, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }
    
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }
    
    public DateTime CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }
    
    public string ProjectName => _settings.ProjectInfo?.ProjectName ?? "Survey Electronic Logbook";
    public string VesselName => _settings.ProjectInfo?.Vessel ?? "";
    
    public string ProjectInfo => !string.IsNullOrEmpty(VesselName) 
        ? $"{ProjectName} | {VesselName}" 
        : ProjectName;
    
    public bool HasProjectInfo => !string.IsNullOrEmpty(_settings.ProjectInfo?.ProjectName);
    
    public int TotalEntryCount => _surveyLogViewModel?.TotalEntries ?? 0;
    
    #endregion
    
    #region Commands
    
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenDataMonitorCommand { get; }
    public ICommand OpenFieldConfigurationCommand { get; }
    public ICommand RefreshCommand { get; }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Starts all monitoring services.
    /// </summary>
    public async Task StartAsync()
    {
        await StartAsync(null);
    }
    
    /// <summary>
    /// Stops all monitoring services.
    /// </summary>
    public void Stop()
    {
        StopInternal();
    }
    
    /// <summary>
    /// Saves the current log to a file.
    /// </summary>
    public async Task SaveAsync()
    {
        await SaveAsync(null);
    }
    
    #endregion
    
    #region Command Implementations
    
    private async Task StartAsync(object? _)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Starting services...";
            
            await _logService.StartAsync();
            
            IsConnected = true;
            ConnectionStatus = "Connected";
            StatusMessage = "Services started";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to start services: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void StopInternal()
    {
        try
        {
            _logService.Stop();
            IsConnected = false;
            ConnectionStatus = "Disconnected";
            StatusMessage = "Services stopped";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
    
    private async Task SaveAsync(object? _)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save Survey Log",
            Filter = "Survey Log Files (*.slog)|*.slog|Compressed Survey Log (*.slogz)|*.slogz|All Files (*.*)|*.*",
            DefaultExt = ".slog",
            FileName = $"SurveyLog_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Saving...";
            
            var compress = dialog.FileName.EndsWith(".slogz", StringComparison.OrdinalIgnoreCase);
            
            await Task.Run(() => _logService.SaveToFile(dialog.FileName, compress, Environment.UserName));
            
            StatusMessage = $"Saved: {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to save file: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task LoadAsync(object? _)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Open Survey Log",
            Filter = "Survey Log Files (*.slog;*.slogz)|*.slog;*.slogz|All Files (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Loading...";
            
            var success = await Task.Run(() => _logService.LoadFromFile(dialog.FileName));
            
            if (success)
            {
                StatusMessage = $"Loaded: {System.IO.Path.GetFileName(dialog.FileName)}";
                _surveyLogViewModel?.RefreshEntries();
                _dprViewModel?.RefreshReports();
            }
            else
            {
                StatusMessage = "Failed to load file";
                System.Windows.MessageBox.Show("Failed to load file. Check if the file format is valid.", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to load file: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportExcelAsync(object? _)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            FileName = $"SurveyLog_Export_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Exporting to Excel...";
            
            await Task.Run(() =>
            {
                var exporter = new Export.ExcelExporter();
                var logFile = _logService.CreateExportFile(Environment.UserName);
                exporter.Export(dialog.FileName, logFile);
            });
            
            StatusMessage = $"Exported: {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to export: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportPdfAsync(object? _)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = $"SurveyLog_Report_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Exporting to PDF...";
            
            await Task.Run(() =>
            {
                var generator = new Export.PdfReportGenerator();
                var logFile = _logService.CreateExportFile(Environment.UserName);
                generator.Generate(dialog.FileName, logFile);
            });
            
            StatusMessage = $"Exported: {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to export: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void NewLogFile()
    {
        var result = System.Windows.MessageBox.Show(
            "Create a new log file? This will clear all current entries.\n\nDo you want to save the current log first?",
            "New Log File",
            System.Windows.MessageBoxButton.YesNoCancel,
            System.Windows.MessageBoxImage.Question);
        
        if (result == System.Windows.MessageBoxResult.Cancel) return;
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            // Save current log first
            _ = SaveAsync(null);
        }
        
        // Clear all entries
        _logService.ClearAll();
        _surveyLogViewModel?.RefreshEntries();
        _dvrRecordingsViewModel?.ClearRecordings();
        _positionFixesViewModel?.ClearFixes();
        _dprViewModel?.RefreshReports();
        
        StatusMessage = "New log file created";
    }
    
    private void OpenSettings()
    {
        try
        {
            var settingsViewModel = new SettingsViewModel(_appSettings.Clone());
            var settingsWindow = new SettingsWindow
            {
                DataContext = settingsViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            
            settingsViewModel.RequestClose += saved =>
            {
                if (saved == true && settingsViewModel.Settings != null)
                {
                    _appSettings = settingsViewModel.Settings;
                    _appSettings.Save();
                    ApplySettingsToViewModels();
                    StatusMessage = "Settings saved";
                }
                settingsWindow.DialogResult = saved;
                settingsWindow.Close();
            };
            
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening settings: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to open settings: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Opens the NaviPac Data Monitor window for real-time data debugging.
    /// </summary>
    private void OpenDataMonitor()
    {
        try
        {
            // Check if window is already open
            if (_dataMonitorWindow != null && _dataMonitorWindow.IsLoaded)
            {
                _dataMonitorWindow.Activate();
                if (_dataMonitorWindow.WindowState == WindowState.Minimized)
                {
                    _dataMonitorWindow.WindowState = WindowState.Normal;
                }
                return;
            }
            
            // Get NaviPacClient from LogService
            var naviPacClient = _logService.NaviPacClient;
            
            // Create view model with NaviPacClient reference
            var viewModel = new DataMonitorViewModel(naviPacClient);
            
            // Create and show window
            _dataMonitorWindow = new DataMonitorWindow(viewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            
            // Clear reference when window closes
            _dataMonitorWindow.Closed += (s, e) => _dataMonitorWindow = null;
            
            _dataMonitorWindow.Show();
            StatusMessage = "Data Monitor opened";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening Data Monitor: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to open Data Monitor: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Opens the Field Configuration window for NaviPac UDO field mapping.
    /// </summary>
    private void OpenFieldConfiguration()
    {
        try
        {
            var viewModel = new FieldConfigurationViewModel(_appSettings);
            var window = new Views.FieldConfigurationWindow(viewModel);
            
            // Show as dialog
            var result = window.ShowDialog();
            
            if (result == true)
            {
                // Settings were saved - reload and apply
                _appSettings = ApplicationSettings.Load();
                ApplySettingsToViewModels();
                StatusMessage = "Field configuration saved";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening Field Configuration: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to open Field Configuration: {ex.Message}", "Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private void Refresh()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Refreshing...";
            
            _surveyLogViewModel?.RefreshEntries();
            _dvrRecordingsViewModel?.RefreshRecordings();
            _positionFixesViewModel?.RefreshFixes();
            _dprViewModel?.RefreshReports();
            
            OnPropertyChanged(nameof(TotalEntryCount));
            StatusMessage = $"Refreshed - {TotalEntryCount} entries";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void ApplySettingsToViewModels()
    {
        // Apply DVR folder path
        if (!string.IsNullOrEmpty(_appSettings.DvrFolderPath) && _dvrRecordingsViewModel != null)
        {
            _dvrRecordingsViewModel.DvrFolderPath = _appSettings.DvrFolderPath;
        }
        
        // Apply project defaults to connection settings if not already set
        if (_settings.ProjectInfo != null)
        {
            if (string.IsNullOrEmpty(_settings.ProjectInfo.Client) && !string.IsNullOrEmpty(_appSettings.DefaultClient))
                _settings.ProjectInfo.Client = _appSettings.DefaultClient;
            if (string.IsNullOrEmpty(_settings.ProjectInfo.Vessel) && !string.IsNullOrEmpty(_appSettings.DefaultVessel))
                _settings.ProjectInfo.Vessel = _appSettings.DefaultVessel;
            if (string.IsNullOrEmpty(_settings.ProjectInfo.ProjectName) && !string.IsNullOrEmpty(_appSettings.DefaultProject))
                _settings.ProjectInfo.ProjectName = _appSettings.DefaultProject;
        }
        
        // Apply field configuration to LogEntryService for dynamic columns
        _logService.FieldConfiguration = _appSettings.NaviPacFields;
        
        // Pass field configuration to SurveyLogViewModel
        if (_surveyLogViewModel != null)
        {
            _surveyLogViewModel.FieldConfiguration = _appSettings.NaviPacFields;
        }
        
        // Update UI bindings
        OnPropertyChanged(nameof(ProjectName));
        OnPropertyChanged(nameof(VesselName));
        OnPropertyChanged(nameof(ProjectInfo));
        OnPropertyChanged(nameof(HasProjectInfo));
    }
    
    #endregion
    
    #region Event Handlers
    
    private void OnServiceStatusChanged(object? sender, ServiceStatusEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            StatusMessage = e.Status;
        });
    }
    
    #endregion
    
    #region Public Methods for External File Loading
    
    /// <summary>
    /// Loads a survey log file from the specified path.
    /// Called by the module when opening associated file types.
    /// </summary>
    public async Task LoadFileAsync(string filePath)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"Loading {System.IO.Path.GetFileName(filePath)}...";
            
            var success = await Task.Run(() => _logService.LoadFromFile(filePath));
            
            if (success)
            {
                StatusMessage = $"Loaded: {System.IO.Path.GetFileName(filePath)}";
                _surveyLogViewModel?.RefreshEntries();
                _dprViewModel?.RefreshReports();
            }
            else
            {
                StatusMessage = "Failed to load file";
                System.Windows.MessageBox.Show("Failed to load file. Check if the file format is valid.", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            System.Windows.MessageBox.Show($"Failed to load file: {ex.Message}", "Error", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Imports a NaviPac calibration/position fix file (.npc).
    /// </summary>
    public void ImportNpcFile(string filePath)
    {
        try
        {
            StatusMessage = $"Importing {System.IO.Path.GetFileName(filePath)}...";
            
            var parser = new Parsers.NpcFileParser();
            var positionFix = parser.Parse(filePath);
            
            if (positionFix != null)
            {
                // Create log entry from position fix
                var entry = new SurveyLogEntry
                {
                    Timestamp = positionFix.Timestamp,
                    EntryType = positionFix.FixType switch
                    {
                        PositionFixType.Calibration => LogEntryType.CalibrationFix,
                        PositionFixType.Verification => LogEntryType.VerificationFix,
                        PositionFixType.SetEastingNorthing => LogEntryType.SetEastingNorthing,
                        _ => LogEntryType.PositionFix
                    },
                    Description = positionFix.Description ?? $"Position fix from {System.IO.Path.GetFileName(filePath)}",
                    Easting = positionFix.ComputedEasting,
                    Northing = positionFix.ComputedNorthing,
                    Source = positionFix.ObjectMonitored ?? "Imported"
                };
                
                _logService.AddEntry(entry);
                _logService.AddPositionFix(positionFix);
                
                _surveyLogViewModel?.RefreshEntries();
                StatusMessage = $"Imported position fix: {positionFix.ObjectMonitored}";
            }
            else
            {
                StatusMessage = "No data found in file";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"NPC import error: {ex}");
        }
    }
    
    /// <summary>
    /// Imports a waypoint file (.wp2) for manual analysis.
    /// </summary>
    public void ImportWaypointFile(string filePath)
    {
        try
        {
            StatusMessage = $"Importing {System.IO.Path.GetFileName(filePath)}...";
            
            var parser = new Parsers.WaypointFileParser();
            var waypoints = parser.Parse(filePath);
            
            if (waypoints.Any())
            {
                // Add entry for each waypoint
                foreach (var wp in waypoints)
                {
                    var entry = new SurveyLogEntry
                    {
                        Timestamp = DateTime.Now,
                        EntryType = LogEntryType.WaypointAdded,
                        Description = $"Waypoint: {wp.Name}",
                        Easting = wp.Easting,
                        Northing = wp.Northing,
                        Source = "Manual Import"
                    };
                    
                    _logService.AddEntry(entry);
                }
                
                _surveyLogViewModel?.RefreshEntries();
                StatusMessage = $"Imported {waypoints.Count} waypoints";
            }
            else
            {
                StatusMessage = "No waypoints found in file";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Waypoint import error: {ex}");
        }
    }
    
    #endregion
    
    public void Dispose()
    {
        _clockTimer?.Stop();
        _logService.Dispose();
        
        // Close DataMonitorWindow if open
        _dataMonitorWindow?.Close();
        _dataMonitorWindow = null;
        
        // Dispose child ViewModels if they implement IDisposable
        (_surveyLogViewModel as IDisposable)?.Dispose();
        (_dvrRecordingsViewModel as IDisposable)?.Dispose();
        (_positionFixesViewModel as IDisposable)?.Dispose();
        (_dprViewModel as IDisposable)?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
