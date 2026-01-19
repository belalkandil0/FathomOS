using System.IO;
using System.Windows;
using FathomOS.Core.Certificates;
using FathomOS.Core.Interfaces;
using FathomOS.Core.Services;
using FathomOS.Modules.ProjectManagement.Views;

namespace FathomOS.Modules.ProjectManagement;

/// <summary>
/// Project Management Module - Manage survey projects, milestones,
/// deliverables, and client coordination.
///
/// Certificate System Integration:
/// - Certificate Code: PJM
/// - Certificate Title: Project Management Certificate
/// </summary>
public class ProjectManagementModule : IModule
{
    private MainWindow? _mainWindow;
    private readonly IAuthenticationService? _authService;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;
    private readonly IExportService? _exportService;
    private ProjectManagementSettings? _settings;

    // Certificate configuration (matches ModuleInfo.json)
    public const string CertificateCode = "PJM";
    public const string CertificateTitle = "Project Management Certificate";

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
        IAuthenticationService authService,
        ICertificationService certService,
        IEventAggregator eventAggregator,
        IThemeService themeService,
        IErrorReporter errorReporter,
        IExportService? exportService = null)
    {
        _authService = authService;
        _certService = certService;
        _eventAggregator = eventAggregator;
        _themeService = themeService;
        _errorReporter = errorReporter;
        _exportService = exportService;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the certification service.
    /// </summary>
    public ICertificationService? CertificationService => _certService;

    /// <summary>
    /// Gets the export service from Core.
    /// </summary>
    public IExportService? ExportService => _exportService;

    /// <summary>
    /// Gets the module settings.
    /// </summary>
    public ProjectManagementSettings Settings => _settings ??= ProjectManagementSettings.Load();

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
        // Load module settings
        _settings = ProjectManagementSettings.Load();

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
            // Save settings
            if (_settings != null)
            {
                ProjectManagementSettings.Save(_settings);
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

    #region Certificate Support

    /// <summary>
    /// Generate a certificate for project management operations.
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
    /// Gets certificate processing data for a project milestone completion.
    /// </summary>
    public Dictionary<string, string> GetCertificateProcessingData(
        string projectName,
        string clientName,
        string milestoneName,
        int deliverablesCount,
        DateTime completionDate)
    {
        return new Dictionary<string, string>
        {
            [ModuleCertificateHelper.DataKeys.ProjectName] = projectName,
            [ModuleCertificateHelper.DataKeys.ClientName] = clientName,
            ["Milestone"] = milestoneName,
            ["Deliverables Count"] = deliverablesCount.ToString(),
            ["Completion Date"] = completionDate.ToString("dd MMM yyyy"),
            [ModuleCertificateHelper.DataKeys.ProcessingDate] = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm UTC"),
            [ModuleCertificateHelper.DataKeys.SoftwareVersion] = $"Project Management v{Version}"
        };
    }

    /// <summary>
    /// Gets recommended signatory titles for project management certificates.
    /// </summary>
    public static IEnumerable<string> GetSignatoryTitles()
    {
        return new[]
        {
            "Project Manager",
            "Survey Manager",
            "Operations Manager",
            "Client Representative",
            "Technical Manager",
            "QA/QC Manager"
        };
    }

    #endregion
}
