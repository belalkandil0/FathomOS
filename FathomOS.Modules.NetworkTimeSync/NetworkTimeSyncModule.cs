namespace FathomOS.Modules.NetworkTimeSync;

using System.IO;
using System.Windows;
using FathomOS.Core.Certificates;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.NetworkTimeSync.Views;

/// <summary>
/// Module implementation for Fathom OS Shell integration.
/// Implements IModule interface to enable discovery and launching.
///
/// Certificate System Integration:
/// - Certificate Code: NTS
/// - Certificate Title: Network Time Synchronization Certificate
/// </summary>
public class NetworkTimeSyncModule : IModule
{
    private DashboardWindow? _mainWindow;
    private readonly IAuthenticationService? _authService;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;

    // Certificate configuration (matches ModuleInfo.json)
    public const string CertificateCode = "NT";
    public const string CertificateTitle = "Network Time Synchronization Verification Certificate";

    #region Constructors

    /// <summary>
    /// Default constructor for module discovery
    /// </summary>
    public NetworkTimeSyncModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public NetworkTimeSyncModule(
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

    #region Public Properties

    /// <summary>
    /// Gets the certification service.
    /// </summary>
    public ICertificationService? CertificationService => _certService;

    #endregion
    
    // ═══════════════════════════════════════════════════════════
    // MODULE METADATA
    // ═══════════════════════════════════════════════════════════
    
    public string ModuleId => "NetworkTimeSync";
    
    public string DisplayName => "Network Time Sync";
    
    public string Description => "Discover, monitor, and synchronize time across network computers";
    
    public Version Version => new Version(2, 0, 0);
    
    public string IconResource => "/FathomOS.Modules.NetworkTimeSync;component/Assets/icon.png";
    
    public string Category => "Utilities";
    
    public int DisplayOrder => 30;
    
    // ═══════════════════════════════════════════════════════════
    // LIFECYCLE METHODS
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Called when module is first loaded by the Shell.
    /// Initialize resources, register services, load settings.
    /// </summary>
    public void Initialize()
    {
        // Load any saved settings
        // Register any services with DI container if using one
        // Pre-load any resources needed

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
    /// Launch the module's main window
    /// </summary>
    /// <param name="owner">Parent window for modal dialogs</param>
    public void Launch(Window? owner = null)
    {
        // If window exists and is loaded, just activate it
        if (_mainWindow != null && _mainWindow.IsLoaded)
        {
            _mainWindow.Activate();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            return;
        }
        
        // Create new window
        _mainWindow = new DashboardWindow();
        
        if (owner != null)
        {
            _mainWindow.Owner = owner;
        }
        
        _mainWindow.Show();
    }
    
    /// <summary>
    /// Called when application is closing - cleanup resources
    /// </summary>
    public void Shutdown()
    {
        // Unsubscribe from theme changes
        if (_themeService != null)
        {
            _themeService.ThemeChanged -= OnThemeChanged;
        }

        // Save any unsaved data
        // Close window if open
        _mainWindow?.Close();
        _mainWindow = null;
    }
    
    // ═══════════════════════════════════════════════════════════
    // FILE HANDLING
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Check if module can handle a specific file type
    /// </summary>
    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;
        
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Module can handle NTS files (time sync configuration)
        return ext == ".nts" || ext == ".timesync";
    }
    
    /// <summary>
    /// Open a file directly in this module (for drag-drop or file association)
    /// </summary>
    public void OpenFile(string filePath)
    {
        // Launch the module
        Launch();

        // Load the configuration file
        _mainWindow?.LoadConfiguration(filePath);
    }

    #region Certificate Support

    /// <summary>
    /// Generate a certificate for network time synchronization.
    /// </summary>
    /// <param name="projectName">Project name.</param>
    /// <param name="processingData">Processing data for certificate.</param>
    /// <param name="owner">Parent window for dialog.</param>
    /// <returns>Certificate ID if created, null if cancelled.</returns>
    public async Task<string?> GenerateCertificateAsync(
        string projectName,
        Dictionary<string, string> processingData,
        Window? owner = null)
    {
        var request = ModuleCertificateHelper.CreateRequest(
            ModuleId,
            CertificateCode,
            Version.ToString(),
            projectName,
            processingData);

        return await ModuleCertificateHelper.GenerateCertificateAsync(_certService, request, owner);
    }

    /// <summary>
    /// Gets certificate processing data for time sync verification.
    /// </summary>
    public Dictionary<string, string> GetCertificateProcessingData(
        string projectName,
        string referenceServer,
        int computersSynced,
        double maxOffset,
        double averageOffset,
        DateTime syncTime)
    {
        return new Dictionary<string, string>
        {
            [ModuleCertificateHelper.DataKeys.ProjectName] = projectName,
            ["Reference Server"] = referenceServer,
            ["Computers Synchronized"] = computersSynced.ToString(),
            ["Maximum Offset"] = $"{maxOffset:F3} ms",
            ["Average Offset"] = $"{averageOffset:F3} ms",
            ["Sync Time"] = syncTime.ToString("dd MMM yyyy HH:mm:ss UTC"),
            [ModuleCertificateHelper.DataKeys.ProcessingDate] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm UTC"),
            [ModuleCertificateHelper.DataKeys.SoftwareVersion] = $"Network Time Sync v{Version}"
        };
    }

    /// <summary>
    /// Gets recommended signatory titles for time sync certificates.
    /// </summary>
    public static IEnumerable<string> GetSignatoryTitles()
    {
        return new[]
        {
            "Survey Engineer",
            "IT Administrator",
            "Online Surveyor",
            "System Administrator",
            "Technical Manager"
        };
    }

    #endregion
}
