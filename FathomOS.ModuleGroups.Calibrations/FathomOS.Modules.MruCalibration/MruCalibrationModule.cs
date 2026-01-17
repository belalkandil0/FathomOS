using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.MruCalibration.Views;

namespace FathomOS.Modules.MruCalibration;

/// <summary>
/// MRU Calibration Module - Inter-System Comparison for Pitch and Roll sensors
/// Implements the Fathom OS IModule interface for integration with the main application
/// </summary>
public class MruCalibrationModule : IModule
{
    private MainWindow? _mainWindow;
    private readonly IAuthenticationService? _authService;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;

    #region Constructors

    /// <summary>
    /// Default constructor for module discovery
    /// </summary>
    public MruCalibrationModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public MruCalibrationModule(
        IAuthenticationService authService,
        ICertificationService certService,
        IEventAggregator eventAggregator,
        IThemeService themeService,
        IErrorReporter errorReporter)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _certService = certService;
        _eventAggregator = eventAggregator;
        _themeService = themeService;
        _errorReporter = errorReporter;
    }

    #endregion
    
    #region IModule Properties
    
    /// <summary>
    /// Unique module identifier - MUST match DLL name
    /// </summary>
    public string ModuleId => "MruCalibration";
    
    /// <summary>
    /// Display name shown on dashboard tile
    /// </summary>
    public string DisplayName => "MRU Calibration";
    
    /// <summary>
    /// Description shown in tooltip
    /// </summary>
    public string Description => 
        "MRU Calibration and Verification by Inter-System Comparison. " +
        "Supports Pitch and Roll sensor calibration with 3-sigma outlier rejection, " +
        "statistical analysis, and professional PDF reporting.";
    
    /// <summary>
    /// Module version
    /// </summary>
    public Version Version => new Version(3, 0, 0);
    
    /// <summary>
    /// Path to module icon resource
    /// </summary>
    public string IconResource => "/FathomOS.Modules.MruCalibration;component/Assets/icon.png";
    
    /// <summary>
    /// Category for grouping on dashboard
    /// </summary>
    public string Category => "Calibrations";
    
    /// <summary>
    /// Sort order on dashboard (lower = earlier)
    /// </summary>
    public int DisplayOrder => 10;
    
    #endregion
    
    #region IModule Methods
    
    /// <summary>
    /// Called when module is loaded by the Shell
    /// </summary>
    public void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"[{ModuleId}] v{Version} initialized - The Fathom 7 Process");

        // Subscribe to theme changes if available
        if (_themeService != null)
        {
            _themeService.ThemeChanged += OnThemeChanged;
        }
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        // Theme is applied automatically by Shell
        System.Diagnostics.Debug.WriteLine($"{ModuleId}: Theme changed to {theme}");
    }
    
    /// <summary>
    /// Called when user clicks the module tile on dashboard
    /// </summary>
    public void Launch(Window? owner = null)
    {
        try
        {
            // Reuse existing window if available
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
            }
            
            _mainWindow.Show();
            _mainWindow.Activate();
            
            // Restore from minimized state
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to launch {DisplayName}:\n\n{ex.Message}",
                "Module Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Launch error: {ex}");
        }
    }
    
    /// <summary>
    /// Called when application is closing
    /// </summary>
    public void Shutdown()
    {
        try
        {
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }
            _mainWindow?.Close();
            _mainWindow = null;
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Shutdown complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Shutdown error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Check if this module can handle a specific file type
    /// </summary>
    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".npd" or ".csv" or ".mru";
    }
    
    /// <summary>
    /// Open a file directly (e.g., from file association or drag-drop)
    /// </summary>
    public void OpenFile(string filePath)
    {
        Launch();
        
        // Tell the window to load the file
        if (_mainWindow?.DataContext is ViewModels.MainViewModel vm)
        {
            vm.LoadFileCommand.Execute(filePath);
        }
    }
    
    #endregion
}
