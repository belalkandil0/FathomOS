using System.Windows;
using FathomOS.Shell.Services;
using FathomOS.Shell.Views;
using FathomOS.Shell.Security;
using FathomOS.Core.Certificates;
using FathomOS.Core.Data;
using FathomOS.Core.Interfaces;
using LicensingSystem.Client;
using LicensingSystem.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace FathomOS.Shell;

/// <summary>
/// Fathom OS main application entry point with licensing support.
/// Implements first-launch flow: License Activation -> Account Creation -> Login
/// </summary>
public partial class App : Application
{
    private ModuleManager? _moduleManager;
    private static LocalUser? _currentLocalUser;

    /// <summary>
    /// Dependency Injection Service Provider
    /// Provides access to all registered services throughout the application.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Fathom OS License Integration - provides session management, heartbeat, and events
    /// </summary>
    public static FathomOSLicenseIntegration Licensing { get; private set; } = null!;

    /// <summary>
    /// The currently logged in local user (for local authentication flow).
    /// </summary>
    public static LocalUser? CurrentLocalUser
    {
        get => _currentLocalUser;
        set => _currentLocalUser = value;
    }
    
    /// <summary>
    /// License Manager - provides license checking capabilities (backward compatibility)
    /// </summary>
    public static LicenseManager LicenseManager => Licensing?.InternalManager!;
    
    /// <summary>
    /// Check if the current license has Professional tier or higher.
    /// In the new module-based system, this checks for Professional/Enterprise tier.
    /// </summary>
    public static bool IsProfessionalOrHigher
    {
        get
        {
            if (Licensing == null || !Licensing.IsLicensed) return false;
            var status = LicenseManager.GetStatusInfo();
            return status.HasMinimumTier("Professional");
        }
    }
    
    /// <summary>
    /// Get the current license tier (Basic, Professional, Enterprise, Custom)
    /// </summary>
    public static string? CurrentTier
    {
        get
        {
            if (LicenseManager == null) return null;
            return LicenseManager.GetLicenseTier();
        }
    }
    
    /// <summary>
    /// Check if the software is currently licensed (any edition)
    /// </summary>
    public static bool IsLicensed => Licensing?.IsLicensed ?? false;
    
    /// <summary>
    /// Get license status display text
    /// </summary>
    public static string LicenseStatusText
    {
        get
        {
            if (Licensing == null) return "Not Initialized";
            return Licensing.GetLicenseDisplayInfo().StatusText;
        }
    }
    
    /// <summary>
    /// Get license edition name
    /// </summary>
    public static string LicenseEdition
    {
        get
        {
            if (Licensing == null) return "UNLICENSED";
            var edition = Licensing.GetLicenseDisplayInfo().Edition;
            return string.IsNullOrEmpty(edition) ? "UNLICENSED" : edition;
        }
    }
    
    /// <summary>
    /// Get display edition (e.g., "Fathom OS" or "Fathom OS — Brand Edition")
    /// </summary>
    public static string DisplayEdition => Licensing?.GetLicenseDisplayInfo().DisplayEdition ?? "Fathom OS";

    /// <summary>
    /// Get the licensed client/customer name
    /// </summary>
    public static string ClientName
    {
        get
        {
            if (Licensing == null) return "";
            var customerName = Licensing.GetLicenseDisplayInfo().CustomerName;
            return string.IsNullOrEmpty(customerName) ? "" : customerName;
        }
    }
    
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        // ============================================================================
        // GLOBAL EXCEPTION HANDLERS - Catch unhandled exceptions
        // ============================================================================
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var message = ex?.ToString() ?? "Unknown error";
            System.Diagnostics.Debug.WriteLine($"FATAL: AppDomain UnhandledException: {message}");
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FathomOS", "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AppDomain UnhandledException:\n{message}");
            }
            catch { /* Ignore logging errors */ }

            MessageBox.Show(
                $"A fatal error occurred:\n\n{ex?.Message ?? "Unknown error"}\n\nThe application will now close.\n\nDetails logged to: %LOCALAPPDATA%\\FathomOS\\crash.log",
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            var ex = args.Exception;
            System.Diagnostics.Debug.WriteLine($"ERROR: Dispatcher UnhandledException: {ex}");
            try
            {
                var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FathomOS");
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(logDir, "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Dispatcher UnhandledException:\n{ex}\n\n");
            }
            catch { /* Ignore logging errors */ }

            MessageBox.Show(
                $"An error occurred:\n\n{ex.Message}\n\nPlease report this issue.\n\nDetails logged to: %LOCALAPPDATA%\\FathomOS\\crash.log",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            var ex = args.Exception;
            System.Diagnostics.Debug.WriteLine($"ERROR: TaskScheduler UnobservedTaskException: {ex}");
            try
            {
                var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FathomOS");
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(logDir, "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] TaskScheduler UnobservedTaskException:\n{ex}\n\n");
            }
            catch { /* Ignore logging errors */ }
            args.SetObserved();
        };

        try
        {
            // ============================================================================
            // ANTI-DEBUG PROTECTION - Check for debuggers/reverse engineering tools
            // ============================================================================
            #if !DEBUG
            if (AntiDebug.IsDebuggingDetected())
            {
                MessageBox.Show(
                    "Security violation detected.\n\n" +
                    "This application cannot run in a debugging environment.",
                    "Security Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Start continuous monitoring for debugger attachment
            AntiDebug.StartContinuousMonitoring(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "Debugging detected. Application will now close.",
                        "Security Violation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                });
            });
            #endif
            
            // ============================================================================
            // DEPENDENCY INJECTION SETUP
            // ============================================================================
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();

            // Initialize module manager from DI container
            _moduleManager = Services.GetRequiredService<IModuleManager>() as ModuleManager;
            _moduleManager?.DiscoverModules();
            
            // ============================================================================
            // INITIALIZE LICENSE INTEGRATION (v3.4.7 simplified wrapper)
            // ============================================================================
            Licensing = new FathomOSLicenseIntegration(
                "https://s7fathom-license-server.onrender.com",
                LicenseConstants.ProductName  // "FathomOS" - must match license server
            );
            
            // Wire up license expiration warning event
            Licensing.LicenseExpiringSoon += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var message = e.Severity switch
                    {
                        ExpirationSeverity.Expired => "Your license has EXPIRED! Please renew immediately.",
                        ExpirationSeverity.Critical => $"Your license expires in {e.DaysRemaining} days! Please renew soon.",
                        _ => $"Your license will expire in {e.DaysRemaining} days."
                    };
                    
                    var icon = e.Severity == ExpirationSeverity.Expired ? MessageBoxImage.Error : MessageBoxImage.Warning;
                    MessageBox.Show(message, "License Expiration Warning", MessageBoxButton.OK, icon);
                });
            };
            
            // Wire up license status changed event (for revocation detection during runtime)
            Licensing.LicenseStatusChanged += (s, e) =>
            {
                if (!e.IsValid && e.Status == LicenseStatus.Revoked)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            "Your license has been revoked.\n\nThe application will now close.",
                            "License Revoked",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown();
                    });
                }
            };
            
            // Configure license helper for cross-module license checking
            // NEW: Module-based licensing - if module is licensed, ALL features are available
            FathomOS.Core.LicenseHelper.IsModuleLicensed = (moduleId) => Licensing.HasModule(moduleId);
            FathomOS.Core.LicenseHelper.IsFeatureEnabled = (feature) => Licensing.HasFeature(feature);
            FathomOS.Core.LicenseHelper.GetCurrentTier = () => LicenseManager.GetLicenseTier();
            FathomOS.Core.LicenseHelper.ShowFeatureLockedMessage = (featureName) =>
            {
                var tier = CurrentTier ?? "No tier";
                MessageBox.Show(
                    $"{featureName} requires an upgraded license.\n\n" +
                    $"Your current tier: {tier}\n\n" +
                    $"Contact support to upgrade your license.",
                    "Feature Not Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            };
            
            // ============================================================================
            // CONFIGURE CERTIFICATE HELPER DELEGATES
            // These connect the Core library to Shell UI components
            // ============================================================================
            CertificateHelper.ShowSignatoryDialog = (companyName, owner) =>
            {
                var dialog = new SignatoryDialog();
                if (!string.IsNullOrEmpty(companyName))
                {
                    dialog.SetCompanyName(companyName);
                }
                if (owner != null)
                {
                    dialog.Owner = owner;
                }
                
                if (dialog.ShowDialog() != true)
                {
                    return new SignatoryInfo { Cancelled = true };
                }
                
                return new SignatoryInfo
                {
                    SignatoryName = dialog.SignatoryName,
                    SignatoryTitle = dialog.SignatoryTitle,
                    CompanyName = dialog.CompanyName,
                    Cancelled = false
                };
            };
            
            CertificateHelper.ShowCertificateViewer = (certificate, isSynced, brandLogo, owner) =>
            {
                var viewer = new CertificateViewerWindow(certificate, isSynced, brandLogo);
                if (owner != null)
                {
                    viewer.Owner = owner;
                }
                viewer.ShowDialog();
            };
            
            CertificateHelper.ShowCertificateManager = (licenseManager, brandLogo, owner) =>
            {
                var window = new CertificateListWindow(licenseManager, brandLogo);
                if (owner != null)
                {
                    window.Owner = owner;
                }
                window.ShowDialog();
            };
            
            // ============================================================================
            // FIRST-LAUNCH FLOW - License Activation, Account Creation, Login
            // Uses AppStartupService to determine the correct startup sequence
            // ============================================================================
            var userService = Services.GetRequiredService<ILocalUserService>();
            var startupService = new AppStartupService(Licensing, userService);

            var startupFlow = await startupService.DetermineStartupFlowAsync();
            System.Diagnostics.Debug.WriteLine($"DEBUG: Startup flow result: {startupFlow.Result}");

            // Process the startup flow
            switch (startupFlow.Result)
            {
                case StartupResult.LicenseRevoked:
                    LicenseManager.Deactivate();
                    MessageBox.Show(
                        "Your license has been revoked.\n\nPlease contact support for assistance.",
                        "License Revoked",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;

                case StartupResult.LicenseExpired:
                    MessageBox.Show(
                        "Your license has expired and the grace period has ended.\n\n" +
                        "Please renew your subscription to continue using Fathom OS.",
                        "License Expired",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    // Show license activation window for renewal
                    var renewWindow = new LicenseActivationWindow();
                    if (renewWindow.ShowDialog() != true || !renewWindow.IsActivated)
                    {
                        Shutdown();
                        return;
                    }
                    // Refresh startup state after renewal
                    startupFlow = startupService.RefreshStartupState();
                    if (!startupFlow.CanProceed)
                    {
                        Shutdown();
                        return;
                    }
                    break;

                case StartupResult.LicenseInvalid:
                    MessageBox.Show(
                        $"License validation failed:\n\n{startupFlow.ErrorMessage}\n\nPlease activate a valid license.",
                        "Invalid License",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    // Show license activation window
                    var reactivateWindow = new LicenseActivationWindow();
                    if (reactivateWindow.ShowDialog() != true || !reactivateWindow.IsActivated)
                    {
                        Shutdown();
                        return;
                    }
                    startupFlow = startupService.RefreshStartupState();
                    if (!startupFlow.CanProceed)
                    {
                        Shutdown();
                        return;
                    }
                    break;

                case StartupResult.NeedsLicenseActivation:
                    System.Diagnostics.Debug.WriteLine("DEBUG: First launch - showing license activation window...");
                    var activationWindow = new LicenseActivationWindow();
                    var activationResult = activationWindow.ShowDialog();

                    if (activationResult != true || !activationWindow.IsActivated)
                    {
                        System.Diagnostics.Debug.WriteLine("DEBUG: User cancelled license activation - shutting down");
                        Shutdown();
                        return;
                    }

                    // Refresh license status and startup flow
                    Licensing.RefreshLicenseStatus();
                    startupFlow = startupService.RefreshStartupState();
                    System.Diagnostics.Debug.WriteLine("DEBUG: License activated successfully!");

                    // Fall through to account creation
                    goto case StartupResult.NeedsAccountCreation;

                case StartupResult.NeedsAccountCreation:
                    System.Diagnostics.Debug.WriteLine("DEBUG: No local users - showing admin setup window...");
                    var adminSetupWindow = new AdminSetupWindow(userService);
                    var createResult = adminSetupWindow.ShowDialog();

                    if (createResult != true || adminSetupWindow.CreatedUser == null)
                    {
                        System.Diagnostics.Debug.WriteLine("DEBUG: User cancelled admin setup - shutting down");
                        Shutdown();
                        return;
                    }

                    // Auto-login with newly created admin account
                    CurrentLocalUser = adminSetupWindow.CreatedUser;
                    System.Diagnostics.Debug.WriteLine($"DEBUG: Admin account created and logged in: {CurrentLocalUser.Username}");
                    break;

                case StartupResult.LicenseInGracePeriod:
                    System.Diagnostics.Debug.WriteLine($"DEBUG: License in grace period - {startupFlow.GraceDaysRemaining} days remaining");
                    MessageBox.Show(
                        $"Your Fathom OS license has expired!\n\n" +
                        $"You have {startupFlow.GraceDaysRemaining} days remaining before the software stops working.\n\n" +
                        $"Please renew your subscription immediately.",
                        "License Expiring - Action Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    // Fall through to login
                    goto case StartupResult.ReadyForLogin;

                case StartupResult.ReadyForLogin:
                    System.Diagnostics.Debug.WriteLine("DEBUG: Showing local login window...");

                    var settingsService = Services.GetService(typeof(ISettingsService)) as ISettingsService;
                    var loginWindow = new LocalLoginWindow(userService, settingsService);
                    var loginResult = loginWindow.ShowDialog();

                    if (loginResult != true || loginWindow.AuthenticatedUser == null)
                    {
                        System.Diagnostics.Debug.WriteLine("DEBUG: Login cancelled or failed - shutting down");
                        Shutdown();
                        return;
                    }

                    CurrentLocalUser = loginWindow.AuthenticatedUser;
                    System.Diagnostics.Debug.WriteLine($"DEBUG: Local login successful for: {CurrentLocalUser.Username}");
                    break;
            }

            // ============================================================================
            // SYNC WITH SERVER - Notify server about this license (fire and forget)
            // ============================================================================
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000);
                    var synced = await LicenseManager.SyncOfflineLicenseAsync();
                    System.Diagnostics.Debug.WriteLine(synced
                        ? "Offline license synced with server successfully"
                        : "No offline sync needed or sync endpoint not available");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Server sync failed (will retry later): {ex.Message}");
                }
            });

            // ============================================================================
            // FINAL LICENSE CHECK
            // ============================================================================
            var finalCheck = LicenseManager.GetStatusInfo();
            System.Diagnostics.Debug.WriteLine($"DEBUG: Final license check - IsLicensed: {finalCheck.IsLicensed}, Status: {finalCheck.Status}");

            if (!finalCheck.IsLicensed && finalCheck.Status != LicenseStatus.GracePeriod)
            {
                MessageBox.Show(
                    "A valid license is required to use Fathom OS.\n\n" +
                    "Please restart the application and activate your license.",
                    "License Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            System.Diagnostics.Debug.WriteLine("DEBUG: Final check PASSED - proceeding to dashboard");

            // Ensure we have a logged-in user (either local or from auth service)
            if (CurrentLocalUser == null)
            {
                // Fallback to existing authentication service if local user not set
                var authService = Services.GetRequiredService<IAuthenticationService>();
                if (!authService.IsAuthenticated)
                {
                    var authResult = await authService.ShowLoginDialogAsync();
                    if (!authResult)
                    {
                        MessageBox.Show(
                            "Login is required to use Fathom OS.\n\nThe application will now close.",
                            "Login Required",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        Shutdown();
                        return;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"DEBUG: Auth service login: {authService.CurrentUser?.Username}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG: Local user login: {CurrentLocalUser.Username}");
            }

            // Handle command-line arguments (file open)
            if (e.Args.Length > 0)
            {
                var filePath = e.Args[0];
                if (System.IO.File.Exists(filePath))
                {
                    _moduleManager.OpenFileWithModule(filePath);
                }
            }

            // Show main dashboard window
            System.Diagnostics.Debug.WriteLine("DEBUG: Creating DashboardWindow...");
            var mainWindow = new DashboardWindow();
            System.Diagnostics.Debug.WriteLine("DEBUG: Setting MainWindow...");
            MainWindow = mainWindow;
            System.Diagnostics.Debug.WriteLine("DEBUG: Calling mainWindow.Show()...");
            mainWindow.Show();
            System.Diagnostics.Debug.WriteLine("DEBUG: Calling mainWindow.Activate()...");
            mainWindow.Activate();
            System.Diagnostics.Debug.WriteLine("DEBUG: mainWindow.Show() completed - startup finished!");
            
            // CRITICAL FIX: Change ShutdownMode AFTER showing dashboard
            // This prevents the app from exiting when the activation window closes
            // but allows normal shutdown when the dashboard closes
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            System.Diagnostics.Debug.WriteLine("DEBUG: ShutdownMode set to OnMainWindowClose");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: EXCEPTION in startup: {ex}");
            try
            {
                var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FathomOS");
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(logDir, "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Startup Exception:\n{ex}\n\n");
            }
            catch { /* Ignore logging errors */ }

            MessageBox.Show(
                $"Error starting Fathom OS:\n\n{ex.Message}\n\nDetails logged to: %LOCALAPPDATA%\\FathomOS\\crash.log",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
    
    /// <summary>
    /// Configure all application services for dependency injection.
    /// This is the central point for registering all services used by the Shell and modules.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    private void ConfigureServices(IServiceCollection services)
    {
        // ============================================================================
        // CORE SERVICES - Shell owns these implementations
        // ============================================================================

        // Event Aggregator - thread-safe pub/sub messaging
        services.AddSingleton<IEventAggregator, EventAggregator>();

        // Settings Service - persistent JSON-based settings (needed by ThemeService)
        services.AddSingleton<ISettingsService, SettingsService>();

        // Theme Service - unified theme management with System theme support
        services.AddSingleton<IThemeService>(sp =>
            new ThemeService(
                sp.GetRequiredService<IEventAggregator>(),
                sp.GetRequiredService<ISettingsService>()));

        // Notification Service - toast notifications for user feedback
        services.AddSingleton<INotificationService, NotificationService>();

        // Error Reporter - centralized error logging (depends on EventAggregator)
        services.AddSingleton<IErrorReporter>(sp =>
            new ErrorReporter(sp.GetRequiredService<IEventAggregator>()));

        // ============================================================================
        // CERTIFICATION SERVICE
        // CertificationService requires Func<LicenseManager> to resolve App.LicenseManager
        // This maintains backward compatibility with existing delegate patterns
        // ============================================================================
        services.AddSingleton<ICertificationService>(sp =>
            new CertificationService(
                () => App.LicenseManager,
                sp.GetRequiredService<IEventAggregator>()));

        // ============================================================================
        // CERTIFICATE SYNC ENGINE - Offline-first certificate storage with sync
        // ============================================================================

        // SQLite Connection Factory - manages database connections
        services.AddSingleton<SqliteConnectionFactory>(sp =>
            SqliteConnectionFactory.CreateDefault("certificates.db"));

        // Certificate Repository - local SQLite storage for certificates
        services.AddSingleton<ICertificateRepository>(sp =>
            new SqliteCertificateRepository(sp.GetRequiredService<SqliteConnectionFactory>()));

        // Certificate PDF Service - QuestPDF-based PDF generation for certificates
        services.AddSingleton<ICertificatePdfService, CertificatePdfService>();

        // Sync API Client - bridges to LicenseManager for server communication
        services.AddSingleton<ISyncApiClient>(sp =>
            new LicenseServerSyncApiClient(() => App.LicenseManager));

        // Certificate Sync Engine - handles offline-first sync with exponential backoff
        services.AddSingleton<ICertificateSyncEngine>(sp =>
            new CertificateSyncEngine(
                sp.GetRequiredService<ICertificateRepository>(),
                sp.GetRequiredService<ISyncApiClient>()));

        // ============================================================================
        // AUTHENTICATION SERVICE
        // Centralized authentication for all FathomOS modules
        // ============================================================================
        services.AddSingleton<IAuthenticationService>(sp =>
            new AuthenticationService(
                sp.GetRequiredService<IEventAggregator>(),
                sp.GetRequiredService<ISettingsService>()));

        // ============================================================================
        // LOCAL USER SERVICE
        // SQLite-based local user management for offline authentication
        // ============================================================================
        services.AddSingleton<ILocalUserService, LocalUserService>();

        // ============================================================================
        // CONSOLIDATED CORE SERVICES
        // Unified services for common functionality across all modules
        // ============================================================================

        // Smoothing Service - signal processing and data smoothing algorithms
        services.AddSingleton<ISmoothingService, FathomOS.Core.Services.UnifiedSmoothingService>();

        // Unit Conversion Service - length, temperature, pressure, velocity, angle conversions
        services.AddSingleton<IUnitConversionService, FathomOS.Core.Services.UnitConversionService>();

        // Excel Export Service - generic Excel export for any data type
        services.AddSingleton<IExcelExportService, FathomOS.Core.Services.ExcelExportService>();

        // Unified Export Service - provides consistent export across Excel, PDF, DXF, CSV, JSON formats
        services.AddSingleton<IExportService, FathomOS.Core.Services.ExportService>();

        // Backup Service - database and configuration backup management
        services.AddSingleton<IBackupService, FathomOS.Core.Services.BackupService>();

        // Encryption Service - AES-256 encryption, key management, file encryption
        services.AddSingleton<IEncryptionService, FathomOS.Core.Services.EncryptionService>();

        // ============================================================================
        // MODULE MANAGEMENT
        // ModuleManager receives IServiceProvider to support DI in module instantiation
        // ============================================================================
        services.AddSingleton<IModuleManager>(sp =>
            new ModuleManager(sp));

        // ============================================================================
        // SHELL SERVICES - Keyboard shortcuts, command palette, session management
        // These services are created in DashboardWindow for now to avoid circular deps
        // ============================================================================

        // Keyboard Shortcut Service - global shortcut registration
        services.AddSingleton<IKeyboardShortcutService>(sp =>
            new KeyboardShortcutService(
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<IEventAggregator>()));

        // Recent Projects Service - tracks recently opened files
        services.AddSingleton<IRecentProjectsService>(sp =>
            new RecentProjectsService(
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<IEventAggregator>()));

        // Background Task Service - task queue with progress
        services.AddSingleton<IBackgroundTaskService>(sp =>
            new BackgroundTaskService(
                sp.GetRequiredService<IEventAggregator>()));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("DEBUG: OnExit called!");
        System.Diagnostics.Debug.WriteLine($"DEBUG: Stack trace:\n{Environment.StackTrace}");

        // Properly shutdown licensing (ends session on server)
        try
        {
            Licensing?.ShutdownAsync().GetAwaiter().GetResult();
        }
        catch { /* Ignore shutdown errors */ }

        // Dispose licensing wrapper
        Licensing?.Dispose();

        // Shutdown all modules
        _moduleManager?.ShutdownAll();

        // Dispose DI container if it implements IDisposable
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
    
    /// <summary>
    /// Get the module manager instance
    /// </summary>
    public ModuleManager ModuleManager => _moduleManager ?? throw new InvalidOperationException("App not initialized");
    
    /// <summary>
    /// Get the current app instance
    /// </summary>
    public static new App Current => (App)Application.Current;
    
    /// <summary>
    /// Apply a theme by name
    /// </summary>
    public void ApplyTheme(string themeName)
    {
        var themeUri = themeName.ToLower() switch
        {
            "light" => new Uri("Themes/LightTheme.xaml", UriKind.Relative),
            "dark" => new Uri("Themes/DarkTheme.xaml", UriKind.Relative),
            _ => new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
        };
        
        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
    
    /// <summary>
    /// Check if a specific module is licensed
    /// </summary>
    public static bool IsModuleLicensed(string moduleId)
    {
        if (LicenseManager == null) return false;
        return LicenseManager.IsModuleLicensed(moduleId);
    }
    
    /// <summary>
    /// Show a message if a module is not licensed
    /// </summary>
    public static bool CheckModuleLicense(string moduleId, string displayName)
    {
        if (IsModuleLicensed(moduleId)) return true;
        
        var tier = CurrentTier ?? "No tier";
        MessageBox.Show(
            $"The '{displayName}' module is not included in your license.\n\n" +
            $"Your current tier: {tier}\n\n" +
            $"Please upgrade your license to access this module.",
            "Module Not Licensed",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        
        return false;
    }
}
