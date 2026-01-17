using System;
using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.RovGyroCalibration.Views;

namespace FathomOS.Modules.RovGyroCalibration;

public class RovGyroCalibrationModule : IModule
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
    public RovGyroCalibrationModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public RovGyroCalibrationModule(
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

    public string ModuleId => "RovGyroCalibration";
    public string DisplayName => "ROV Gyro Calibration";
    public string Description => "Calibrate ROV gyro systems against vessel reference with geometric corrections";
    public Version Version => new(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.RovGyroCalibration;component/Assets/icon.png";
    public string Category => "Calibrations";
    public int DisplayOrder => 20;

    public void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"{DisplayName} v{Version} initialized");

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

    public void Launch(Window? owner = null)
    {
        try
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
            }
            _mainWindow.Show();
            _mainWindow.Activate();
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
        }
        catch (Exception ex)
        {
            var fullError = $"Failed to launch {DisplayName}:\n\n{ex.Message}";
            if (ex.InnerException != null)
                fullError += $"\n\nInner: {ex.InnerException.Message}";
            fullError += $"\n\nStack: {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}";
            
            System.Windows.MessageBox.Show(fullError,
                "Module Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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
        }
        catch { }
    }

    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".npd" or ".csv";
    }

    public void OpenFile(string filePath)
    {
        Launch();
    }
}
