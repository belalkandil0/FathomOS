using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.ProjectManagement.Views;

namespace FathomOS.Modules.ProjectManagement;

/// <summary>
/// Project Management Module - Manage survey projects, milestones,
/// deliverables, and client coordination.
/// </summary>
public class ProjectManagementModule : IModule
{
    private MainWindow? _mainWindow;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;

    #region Constructors

    /// <summary>
    /// Default constructor for module discovery
    /// </summary>
    public ProjectManagementModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public ProjectManagementModule(
        ICertificationService certService,
        IEventAggregator eventAggregator,
        IThemeService themeService,
        IErrorReporter errorReporter)
    {
        _certService = certService;
        _eventAggregator = eventAggregator;
        _themeService = themeService;
        _errorReporter = errorReporter;
    }

    #endregion

    #region IModule Properties

    public string ModuleId => "ProjectManagement";
    public string DisplayName => "Project Management";
    public string Description => "Manage survey projects, milestones, deliverables, and client coordination";
    public Version Version => new Version(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.ProjectManagement;component/Assets/icon.png";
    public string Category => "Operations";
    public int DisplayOrder => 51;

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
            _errorReporter?.Report(ModuleId, $"Failed to launch {DisplayName}", ex);
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
        return ext is ".sproj" or ".csv" or ".xlsx";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        // TODO: Load project data from file
    }

    #endregion

    #region Private Methods

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        // Theme is applied automatically by Shell
        System.Diagnostics.Debug.WriteLine($"{ModuleId}: Theme changed to {theme}");
    }

    #endregion
}
