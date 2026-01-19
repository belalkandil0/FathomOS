using System.IO;
using System.Windows;
using FathomOS.Core;
using FathomOS.Core.Certificates;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.SurveyListing.Settings;
using FathomOS.Modules.SurveyListing.Views;
using MessageBox = System.Windows.MessageBox;

namespace FathomOS.Modules.SurveyListing;

/// <summary>
/// Survey Listing Generator module for Fathom OS.
/// Generates survey listings from NPD files with route alignment, tide corrections,
/// and multiple export formats.
///
/// Certificate System Integration:
/// - Certificate Code: SLG
/// - Certificate Title: Survey Listing Processing Certificate
/// </summary>
public class SurveyListingModule : IModule
{
    private MainWindow? _mainWindow;
    private readonly IAuthenticationService? _authService;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;
    private readonly ISmoothingService? _smoothingService;
    private readonly IExportService? _exportService;
    private SurveyListingSettings? _settings;

    // Certificate configuration
    public const string CertificateCode = "SLG";
    public const string CertificateTitle = "Survey Listing Processing Certificate";
    public const string CertificateStatement = "This is to certify that the survey listing data documented herein has been processed and verified using FathomOS Survey Listing Generator in accordance with industry standards for hydrographic data processing.";

    #region Constructors

    /// <summary>
    /// Default constructor for module discovery
    /// </summary>
    public SurveyListingModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public SurveyListingModule(
        IAuthenticationService authService,
        ICertificationService certService,
        IEventAggregator eventAggregator,
        IThemeService themeService,
        IErrorReporter errorReporter,
        ISmoothingService? smoothingService = null,
        IExportService? exportService = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _certService = certService;
        _eventAggregator = eventAggregator;
        _themeService = themeService;
        _errorReporter = errorReporter;
        _smoothingService = smoothingService;
        _exportService = exportService;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the certification service for external access (e.g., from ViewModels).
    /// </summary>
    public ICertificationService? CertificationService => _certService;

    /// <summary>
    /// Gets the smoothing service from Core.
    /// </summary>
    public ISmoothingService? SmoothingService => _smoothingService;

    /// <summary>
    /// Gets the export service from Core.
    /// </summary>
    public IExportService? ExportService => _exportService;

    /// <summary>
    /// Gets the module settings.
    /// </summary>
    public SurveyListingSettings Settings => _settings ??= SurveyListingSettings.Load();

    #endregion
    
    #region IModule Properties
    
    public string ModuleId => "SurveyListing";
    
    public string DisplayName => "Survey Listing Generator";
    
    public string Description => AppInfo.Description;
    
    public Version Version => AppInfo.Version;
    
    public string IconResource => "/FathomOS.Modules.SurveyListing;component/Assets/icon.png";
    
    public string Category => "Data Processing";
    
    public int DisplayOrder => 10;
    
    #endregion
    
    #region IModule Methods
    
    public void Initialize()
    {
        // Load module settings
        _settings = SurveyListingSettings.Load();

        System.Diagnostics.Debug.WriteLine($"Survey Listing Module {AppInfo.VersionDisplay} initialized");

        // Subscribe to theme changes if available
        if (_themeService != null)
        {
            _themeService.ThemeChanged += OnThemeChanged;
            // Sync settings with Shell theme
            _settings.UseDarkTheme = _themeService.IsDarkTheme;
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
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch Survey Listing module: {ex.Message}",
                "Module Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    public void Shutdown()
    {
        try
        {
            // Save settings
            if (_settings != null)
            {
                SurveyListingSettings.Save(_settings);
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
            System.Diagnostics.Debug.WriteLine($"Error during Survey Listing shutdown: {ex.Message}");
        }
    }

    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        return extension switch
        {
            ".npd" => true,   // Survey data files
            ".rlx" => true,   // Route alignment files
            ".s7p" => true,   // Fathom OS project files
            ".tide" => true,  // Tide correction files
            _ => false
        };
    }
    
    public void OpenFile(string filePath)
    {
        Launch();
        
        if (_mainWindow != null && File.Exists(filePath))
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // Load file based on type
            switch (extension)
            {
                case ".s7p":
                    _mainWindow.LoadProject(filePath);
                    break;
                case ".npd":
                    _mainWindow.LoadSurveyFile(filePath);
                    break;
                case ".rlx":
                    _mainWindow.LoadRouteFile(filePath);
                    break;
                case ".tide":
                    _mainWindow.LoadTideFile(filePath);
                    break;
            }
        }
    }

    #endregion

    #region Certificate Support

    /// <summary>
    /// Generate a certificate for survey listing processing results.
    /// </summary>
    /// <param name="projectName">Project name.</param>
    /// <param name="processingData">Processing result data for certificate.</param>
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
    /// Get certificate processing data from survey listing results.
    /// </summary>
    /// <param name="projectName">Project name.</param>
    /// <param name="routeFile">Route file path.</param>
    /// <param name="surveyFiles">Survey data files.</param>
    /// <param name="outputFile">Output file path.</param>
    /// <param name="pointCount">Number of points processed.</param>
    /// <param name="startKp">Start KP value.</param>
    /// <param name="endKp">End KP value.</param>
    /// <param name="tideApplied">Whether tide correction was applied.</param>
    /// <param name="smoothingApplied">Whether smoothing was applied.</param>
    /// <returns>Dictionary of certificate processing data.</returns>
    public Dictionary<string, string> GetCertificateProcessingData(
        string projectName,
        string? routeFile,
        IEnumerable<string>? surveyFiles,
        string? outputFile,
        int pointCount,
        double startKp,
        double endKp,
        bool tideApplied,
        bool smoothingApplied)
    {
        var data = new Dictionary<string, string>
        {
            [ModuleCertificateHelper.DataKeys.ProjectName] = projectName,
            [ModuleCertificateHelper.DataKeys.ProcessingDate] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm UTC"),
            [ModuleCertificateHelper.DataKeys.SoftwareVersion] = $"Survey Listing Generator v{Version}",
            [ModuleCertificateHelper.DataKeys.PointsProcessed] = pointCount.ToString("N0"),
            [ModuleCertificateHelper.DataKeys.StartKp] = startKp.ToString("F3"),
            [ModuleCertificateHelper.DataKeys.EndKp] = endKp.ToString("F3"),
            [ModuleCertificateHelper.DataKeys.RouteLength] = $"{(endKp - startKp):F3} km",
            [ModuleCertificateHelper.DataKeys.TideCorrection] = tideApplied ? "Applied" : "Not Applied",
            [ModuleCertificateHelper.DataKeys.SmoothingApplied] = smoothingApplied ? "Applied" : "Not Applied"
        };

        if (!string.IsNullOrEmpty(routeFile))
        {
            data["Route File"] = Path.GetFileName(routeFile);
        }

        if (surveyFiles != null)
        {
            var fileNames = surveyFiles.Select(Path.GetFileName).ToList();
            data[ModuleCertificateHelper.DataKeys.InputFiles] = string.Join(", ", fileNames);
        }

        if (!string.IsNullOrEmpty(outputFile))
        {
            data[ModuleCertificateHelper.DataKeys.OutputFiles] = Path.GetFileName(outputFile);
        }

        return data;
    }

    /// <summary>
    /// Gets recommended signatory titles for survey listing certificates.
    /// </summary>
    public static IEnumerable<string> GetSignatoryTitles()
    {
        return ModuleCertificateHelper.StandardSignatoryTitles;
    }

    #endregion
}
