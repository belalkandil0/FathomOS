namespace FathomOS.Modules.TreeInclination;

using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.TreeInclination.Views;

/// <summary>
/// Tree Inclination Calculator Module for Fathom OS
/// Calculates structure inclination from DigiQuartz sensor depth measurements
/// </summary>
public class TreeInclinationModule : IModule
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
    public TreeInclinationModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public TreeInclinationModule(
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

    public string ModuleId => "TreeInclination";
    public string DisplayName => "Tree Inclination";
    public string Description => "Calculate inclination of subsea structures using DigiQuartz depth sensors";
    public Version Version => new Version(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.TreeInclination;component/Assets/icon.png";
    public string Category => "Quality Control";
    public int DisplayOrder => 25;

    #endregion

    #region IModule Methods

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
                if (owner != null)
                    _mainWindow.Owner = owner;
            }
            
            _mainWindow.Show();
            _mainWindow.Activate();
            
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch {DisplayName}: {ex.Message}",
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}");
        }
    }

    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".fosi" or ".npd";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        
        if (_mainWindow?.DataContext is ViewModels.MainViewModel vm)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (ext == ".fosi")
            {
                vm.LoadProjectCommand.Execute(filePath);
            }
            else if (ext == ".npd")
            {
                vm.LoadFilesCommand.Execute(new[] { filePath });
            }
        }
    }

    #endregion
}
