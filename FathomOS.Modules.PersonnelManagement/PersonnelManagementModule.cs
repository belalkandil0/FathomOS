using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.PersonnelManagement.Views;

namespace FathomOS.Modules.PersonnelManagement;

/// <summary>
/// Personnel Management Module - Manage survey crew, certifications,
/// training records, and competency tracking for offshore operations.
/// </summary>
public class PersonnelManagementModule : IModule
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
    public PersonnelManagementModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public PersonnelManagementModule(
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

        // Subscribe to authentication changes
        _authService.AuthenticationChanged += OnAuthenticationChanged;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the currently logged-in user from the centralized authentication service.
    /// </summary>
    public IUser? CurrentUser => _authService?.CurrentUser;

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    public bool IsAuthenticated => _authService?.IsAuthenticated ?? false;

    #endregion

    #region IModule Properties

    public string ModuleId => "PersonnelManagement";
    public string DisplayName => "Personnel Management";
    public string Description => "Manage survey crew, certifications, training records, and competency tracking for offshore operations";
    public Version Version => new Version(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.PersonnelManagement;component/Assets/icon.png";
    public string Category => "Operations";
    public int DisplayOrder => 50;

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

        // Log authentication state
        if (_authService != null)
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                System.Diagnostics.Debug.WriteLine($"{ModuleId}: Authenticated as {user.DisplayName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"{ModuleId}: Not authenticated");
            }
        }
    }

    public void Launch(Window? owner = null)
    {
        try
        {
            // Check authentication - require login for Personnel Management
            if (_authService != null && !_authService.IsAuthenticated)
            {
                // Show login dialog via centralized authentication service
                var loginTask = _authService.ShowLoginDialogAsync(owner);
                loginTask.Wait();

                if (!loginTask.Result)
                {
                    System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Authentication cancelled or failed");
                    return;
                }
            }

            // Log current user for audit purposes
            if (_authService?.CurrentUser != null)
            {
                System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Launched by user: {_authService.CurrentUser.DisplayName}");
            }

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
            // Unsubscribe from authentication changes
            if (_authService != null)
            {
                _authService.AuthenticationChanged -= OnAuthenticationChanged;
            }

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
        return ext is ".csv" or ".xlsx";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        // TODO: Load personnel data from file
    }

    #endregion

    #region Private Methods

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        // Theme is applied automatically by Shell
        System.Diagnostics.Debug.WriteLine($"{ModuleId}: Theme changed to {theme}");
    }

    private void OnAuthenticationChanged(object? sender, IUser? user)
    {
        if (user != null)
        {
            System.Diagnostics.Debug.WriteLine($"{ModuleId}: User authenticated: {user.DisplayName}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"{ModuleId}: User logged out");

            // Close module window when user logs out
            if (_mainWindow != null && _mainWindow.IsLoaded)
            {
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    _mainWindow.Close();
                    _mainWindow = null;
                });
            }
        }
    }

    /// <summary>
    /// Check if the current user has a specific permission.
    /// Uses the centralized authentication service.
    /// </summary>
    /// <param name="permission">The permission to check (e.g., "personnel.edit")</param>
    /// <returns>True if user has the permission</returns>
    public bool HasPermission(string permission)
    {
        return _authService?.HasPermission(permission) ?? false;
    }

    /// <summary>
    /// Check if the current user has a specific role.
    /// Uses the centralized authentication service.
    /// </summary>
    /// <param name="roles">The roles to check</param>
    /// <returns>True if user has any of the specified roles</returns>
    public bool HasRole(params string[] roles)
    {
        return _authService?.HasRole(roles) ?? false;
    }

    /// <summary>
    /// Gets the current user's ID for audit trail purposes.
    /// </summary>
    /// <returns>The current user's ID, or null if not authenticated</returns>
    public Guid? GetCurrentUserId()
    {
        return _authService?.CurrentUser?.UserId;
    }

    #endregion
}
