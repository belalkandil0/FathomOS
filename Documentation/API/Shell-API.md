# FathomOS Shell API Reference

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Service Interfaces](#service-interfaces)
3. [Authentication Service](#authentication-service)
4. [Theme Service](#theme-service)
5. [Event Aggregator](#event-aggregator)
6. [Certification Service](#certification-service)
7. [Error Reporter](#error-reporter)
8. [License Manager](#license-manager)

---

## Overview

FathomOS.Shell is the main application host that provides core services to all modules. Modules access these services through dependency injection, receiving interface implementations in their constructors.

### Service Architecture

```
FathomOS.Shell
├── Services/
│   ├── AuthenticationService : IAuthenticationService
│   ├── ThemeService : IThemeService
│   ├── EventAggregator : IEventAggregator
│   ├── CertificationService : ICertificationService
│   └── ErrorReporter : IErrorReporter
└── LicenseManager
```

---

## Service Interfaces

All service interfaces are defined in `FathomOS.Core.Interfaces` to avoid circular dependencies.

### Module Constructor Pattern

Modules receive services through constructor injection:

```csharp
public class MyModule : IModule
{
    private readonly IAuthenticationService? _authService;
    private readonly ICertificationService? _certService;
    private readonly IEventAggregator? _eventAggregator;
    private readonly IThemeService? _themeService;
    private readonly IErrorReporter? _errorReporter;

    // Parameterless constructor for discovery
    public MyModule() { }

    // Full constructor for dependency injection
    public MyModule(
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
}
```

---

## Authentication Service

Provides centralized user authentication across all modules.

### Interface

```csharp
public interface IAuthenticationService
{
    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current authenticated user, or null if not authenticated.
    /// </summary>
    IUser? CurrentUser { get; }

    /// <summary>
    /// Shows the login dialog and authenticates the user.
    /// </summary>
    /// <param name="owner">Parent window for the dialog</param>
    /// <returns>True if login successful, false if cancelled or failed</returns>
    Task<bool> ShowLoginDialogAsync(Window? owner = null);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    void Logout();

    /// <summary>
    /// Checks if the current user has a specific permission.
    /// </summary>
    /// <param name="permission">Permission identifier (e.g., "personnel.edit")</param>
    /// <returns>True if user has the permission</returns>
    bool HasPermission(string permission);

    /// <summary>
    /// Checks if the current user has any of the specified roles.
    /// </summary>
    /// <param name="roles">Role names to check</param>
    /// <returns>True if user has at least one of the roles</returns>
    bool HasRole(params string[] roles);

    /// <summary>
    /// Raised when authentication state changes.
    /// </summary>
    event EventHandler<IUser?> AuthenticationChanged;
}
```

### IUser Interface

```csharp
public interface IUser
{
    Guid UserId { get; }
    string Username { get; }
    string DisplayName { get; }
    string Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
}
```

### Usage Examples

```csharp
// Check authentication before sensitive operations
if (!_authService?.IsAuthenticated ?? false)
{
    var loginResult = await _authService!.ShowLoginDialogAsync(this);
    if (!loginResult)
    {
        // User cancelled or login failed
        return;
    }
}

// Get current user info
var user = _authService.CurrentUser;
Console.WriteLine($"Logged in as: {user?.DisplayName}");

// Check permissions
if (_authService.HasPermission("personnel.delete"))
{
    // Allow delete operation
}

// Check roles
if (_authService.HasRole("Admin", "Manager"))
{
    // Show admin features
}

// React to authentication changes
_authService.AuthenticationChanged += (sender, user) =>
{
    if (user != null)
    {
        Console.WriteLine($"User logged in: {user.DisplayName}");
    }
    else
    {
        Console.WriteLine("User logged out");
        // Close sensitive windows
    }
};
```

---

## Theme Service

Manages application themes across all modules.

### Interface

```csharp
public interface IThemeService
{
    /// <summary>
    /// Gets or sets the current theme.
    /// Setting this property applies the theme and raises ThemeChanged.
    /// </summary>
    AppTheme CurrentTheme { get; set; }

    /// <summary>
    /// Applies a theme to the application.
    /// </summary>
    void ApplyTheme(AppTheme theme);

    /// <summary>
    /// Applies a theme to a specific window.
    /// </summary>
    void ApplyThemeToWindow(Window window, AppTheme theme);

    /// <summary>
    /// Cycles to the next theme in sequence.
    /// </summary>
    void CycleTheme();

    /// <summary>
    /// Gets the display name for a theme.
    /// </summary>
    string GetThemeDisplayName(AppTheme theme);

    /// <summary>
    /// Gets all available themes.
    /// </summary>
    static AppTheme[] GetAllThemes();

    /// <summary>
    /// Raised when the theme changes.
    /// </summary>
    event EventHandler<AppTheme> ThemeChanged;
}
```

### AppTheme Enumeration

```csharp
public enum AppTheme
{
    Light,
    Dark,
    Modern,
    Gradient
}
```

### Usage Examples

```csharp
// Subscribe to theme changes
public void Initialize()
{
    if (_themeService != null)
    {
        _themeService.ThemeChanged += OnThemeChanged;
    }
}

private void OnThemeChanged(object? sender, AppTheme theme)
{
    // Theme is applied automatically by Shell
    // Perform any module-specific theme adjustments
    UpdateChartColors(theme);
}

// Clean up subscription
public void Shutdown()
{
    if (_themeService != null)
    {
        _themeService.ThemeChanged -= OnThemeChanged;
    }
}

// Change theme programmatically
private void SetDarkTheme()
{
    if (_themeService != null)
    {
        _themeService.CurrentTheme = AppTheme.Dark;
    }
}

// Get available themes for UI
var themes = ThemeService.GetAllThemes();
foreach (var theme in themes)
{
    Console.WriteLine(_themeService.GetThemeDisplayName(theme));
}
```

---

## Event Aggregator

Provides cross-module messaging using a publish/subscribe pattern.

### Interface

```csharp
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="eventData">Event data to publish</param>
    void Publish<TEvent>(TEvent eventData);

    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">Event type to subscribe to</typeparam>
    /// <param name="handler">Handler to call when event is published</param>
    /// <returns>Subscription token for unsubscribing</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Subscribes to events on the UI thread.
    /// </summary>
    IDisposable SubscribeOnUIThread<TEvent>(Action<TEvent> handler);
}
```

### Defining Events

```csharp
// Define custom event classes
public class ProjectOpenedEvent
{
    public string ProjectPath { get; init; }
    public string ModuleId { get; init; }
}

public class DataExportedEvent
{
    public string ExportPath { get; init; }
    public string Format { get; init; }
    public int RecordCount { get; init; }
}

public class CertificateCreatedEvent
{
    public string CertificateId { get; init; }
    public string ModuleId { get; init; }
}
```

### Usage Examples

```csharp
// Publishing events
_eventAggregator?.Publish(new ProjectOpenedEvent
{
    ProjectPath = filePath,
    ModuleId = ModuleId
});

// Subscribing to events
private IDisposable? _projectSubscription;

public void Initialize()
{
    _projectSubscription = _eventAggregator?.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
}

private void OnProjectOpened(ProjectOpenedEvent evt)
{
    Console.WriteLine($"Project opened: {evt.ProjectPath} by {evt.ModuleId}");
}

// Unsubscribe
public void Shutdown()
{
    _projectSubscription?.Dispose();
}

// Subscribe on UI thread for UI updates
_eventAggregator?.SubscribeOnUIThread<DataExportedEvent>(evt =>
{
    StatusText = $"Exported {evt.RecordCount} records to {evt.Format}";
});
```

---

## Certification Service

Creates and manages processing certificates.

### Interface

```csharp
public interface ICertificationService
{
    /// <summary>
    /// Creates a new processing certificate with UI dialog.
    /// </summary>
    Task<ProcessingCertificate?> CreateCertificateAsync(
        string moduleId,
        string moduleCertificateCode,
        string moduleVersion,
        string projectName,
        Dictionary<string, string>? processingData = null,
        List<string>? inputFiles = null,
        List<string>? outputFiles = null,
        string? projectLocation = null,
        string? vessel = null,
        string? client = null,
        Window? owner = null);

    /// <summary>
    /// Shows the certificate viewer for a specific certificate.
    /// </summary>
    void ViewCertificate(ProcessingCertificate certificate, Window? owner = null);

    /// <summary>
    /// Opens the certificate list/manager window.
    /// </summary>
    void ShowCertificateManager(Window? owner = null);

    /// <summary>
    /// Gets all local certificates for a module.
    /// </summary>
    IEnumerable<ProcessingCertificate> GetCertificates(string moduleId);
}
```

### Usage Examples

```csharp
// Create a certificate
var processingData = new Dictionary<string, string>
{
    { "Total Points", "15,432" },
    { "Survey Length", "45.7 km" },
    { "Processing Method", "Cubic Spline" }
};

var inputFiles = new List<string> { "survey.npd", "tide.tide" };
var outputFiles = new List<string> { "listing.xlsx", "track.dxf" };

var certificate = await _certService?.CreateCertificateAsync(
    moduleId: "SurveyListing",
    moduleCertificateCode: "SL",
    moduleVersion: "1.0.45",
    projectName: "Pipeline Survey",
    processingData: processingData,
    inputFiles: inputFiles,
    outputFiles: outputFiles,
    projectLocation: "North Sea",
    vessel: "MV Survey One",
    client: "Example Oil Co",
    owner: this);

if (certificate != null)
{
    Console.WriteLine($"Certificate created: {certificate.CertificateId}");
}

// View an existing certificate
_certService?.ViewCertificate(existingCertificate, this);

// Open certificate manager
_certService?.ShowCertificateManager(this);
```

---

## Error Reporter

Centralized error reporting and logging.

### Interface

```csharp
public interface IErrorReporter
{
    /// <summary>
    /// Reports an error from a module.
    /// </summary>
    /// <param name="moduleId">Source module identifier</param>
    /// <param name="message">Error message</param>
    /// <param name="exception">Exception details</param>
    void Report(string moduleId, string message, Exception exception);

    /// <summary>
    /// Reports a warning from a module.
    /// </summary>
    void ReportWarning(string moduleId, string message);

    /// <summary>
    /// Reports an informational message.
    /// </summary>
    void ReportInfo(string moduleId, string message);

    /// <summary>
    /// Gets recent error logs.
    /// </summary>
    IEnumerable<ErrorLogEntry> GetRecentErrors(int count = 100);
}

public class ErrorLogEntry
{
    public DateTime Timestamp { get; set; }
    public string ModuleId { get; set; }
    public string Message { get; set; }
    public string? ExceptionDetails { get; set; }
    public ErrorSeverity Severity { get; set; }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error
}
```

### Usage Examples

```csharp
try
{
    // Risky operation
    ProcessData();
}
catch (Exception ex)
{
    _errorReporter?.Report(ModuleId, "Failed to process data", ex);

    // Also show user-friendly message
    MessageBox.Show($"Error: {ex.Message}", "Processing Error",
        MessageBoxButton.OK, MessageBoxImage.Error);
}

// Report warnings
if (dataPoints.Count < 100)
{
    _errorReporter?.ReportWarning(ModuleId, "Low data point count may affect accuracy");
}

// Report info
_errorReporter?.ReportInfo(ModuleId, $"Processed {dataPoints.Count} points successfully");
```

---

## License Manager

Manages application licensing and branding.

### Key Methods

```csharp
public class LicenseManager
{
    // Activation
    Task<ActivationResult> ActivateAsync(string licenseKey);
    Task<ActivationResult> ActivateOfflineAsync(string activationResponse);
    string GetHardwareFingerprint();

    // License Info
    bool IsActivated { get; }
    bool IsTrialMode { get; }
    DateTime? ExpirationDate { get; }
    LicenseInfo? GetLicenseInfo();

    // Branding
    BrandingInfo? GetBrandingInfo();
    Task<(string? url, string? base64, string? error)> GetBrandLogoAsync();

    // Certificates
    Task<ProcessingCertificate> CreateCertificateAsync(...);
    List<CertificateEntry> GetLocalCertificates(string moduleId);
    CertificateEntry? GetLocalCertificate(string certificateId);
    Task SyncCertificatesAsync();
}
```

### Usage Examples

```csharp
// Check license status
if (!licenseManager.IsActivated)
{
    if (licenseManager.IsTrialMode)
    {
        Console.WriteLine("Running in trial mode");
    }
    else
    {
        Console.WriteLine("Please activate your license");
    }
}

// Get branding info
var branding = licenseManager.GetBrandingInfo();
if (branding != null)
{
    WindowTitle = $"{branding.Brand} - {DisplayName}";
}

// Get brand logo for reports
var (logoUrl, logoBase64, error) = await licenseManager.GetBrandLogoAsync();
if (logoBase64 != null)
{
    // Use logo in PDF reports
}
```

---

## Related Documentation

- [Module API](Module-API.md) - IModule interface
- [Core API](Core-API.md) - Core library reference
- [Services Documentation](../Services/) - Detailed service guides

---

*Copyright 2026 Fathom OS. All rights reserved.*
