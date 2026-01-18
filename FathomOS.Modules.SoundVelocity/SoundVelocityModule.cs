using System;
using System.IO;
using System.Windows;
using FathomOS.Core.Certificates;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.SoundVelocity.Views;

namespace FathomOS.Modules.SoundVelocity;

/// <summary>
/// Sound Velocity Profile processing module.
///
/// Certificate System Integration:
/// - Certificate Code: SVP
/// - Certificate Title: Sound Velocity Profile Processing Certificate
/// </summary>
public class SoundVelocityModule : IModule
{
    private MainWindow? _mainWindow;
    private readonly IAuthenticationService? _authService;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;
    private readonly ISmoothingService? _smoothingService;
    private readonly IExportService? _exportService;

    // Certificate configuration
    public const string CertificateCode = "SVP";
    public const string CertificateTitle = "Sound Velocity Profile Processing Certificate";

    #region Constructors

    /// <summary>
    /// Default constructor for module discovery
    /// </summary>
    public SoundVelocityModule()
    {
    }

    /// <summary>
    /// DI constructor for full functionality
    /// </summary>
    public SoundVelocityModule(
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
    /// Gets the certification service.
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

    #endregion

    #region IModule Properties

    public string ModuleId => "SoundVelocity";
    public string DisplayName => "Sound Velocity Profile";
    public string Description => "Process CTD cast data, calculate sound velocity using Chen-Millero or Del Grosso formulas, and export to industry formats (USR, VEL, PRO, Excel)";
    public Version Version => new Version(1, 2, 0);
    public string IconResource => "/FathomOS.Modules.SoundVelocity;component/Assets/icon.png";
    public string Category => "Data Processing";
    public int DisplayOrder => 15;

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
        return ext is ".000" or ".001" or ".002" or ".003" or ".svp" or ".ctd" or ".bp3" or ".txt" or ".csv";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        _mainWindow?.LoadFile(filePath);
    }

    #endregion

    #region Certificate Support

    /// <summary>
    /// Generate a certificate for sound velocity profile processing.
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
    /// Gets certificate processing data for SVP processing.
    /// </summary>
    public Dictionary<string, string> GetCertificateProcessingData(
        string projectName,
        string inputFile,
        string outputFile,
        int dataPoints,
        double minDepth,
        double maxDepth,
        double averageVelocity,
        string formula)
    {
        return new Dictionary<string, string>
        {
            [ModuleCertificateHelper.DataKeys.ProjectName] = projectName,
            [ModuleCertificateHelper.DataKeys.InputFiles] = Path.GetFileName(inputFile),
            [ModuleCertificateHelper.DataKeys.OutputFiles] = Path.GetFileName(outputFile),
            ["Data Points"] = dataPoints.ToString(),
            ["Min Depth"] = $"{minDepth:F2} m",
            ["Max Depth"] = $"{maxDepth:F2} m",
            ["Depth Range"] = $"{maxDepth - minDepth:F2} m",
            ["Average Velocity"] = $"{averageVelocity:F2} m/s",
            ["Formula Used"] = formula,
            [ModuleCertificateHelper.DataKeys.ProcessingDate] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm UTC"),
            [ModuleCertificateHelper.DataKeys.SoftwareVersion] = $"Sound Velocity Profile v{Version}"
        };
    }

    /// <summary>
    /// Gets recommended signatory titles for SVP certificates.
    /// </summary>
    public static IEnumerable<string> GetSignatoryTitles()
    {
        return new[]
        {
            "Online Surveyor",
            "Senior Surveyor",
            "Party Chief",
            "Survey Manager",
            "Data Processor",
            "QA/QC Engineer"
        };
    }

    #endregion
}
