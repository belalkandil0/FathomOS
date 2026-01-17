using System.Windows;
using FathomOS.Shell.Services;
using FathomOS.Shell.Views;
using FathomOS.Shell.Security;
using FathomOS.Core.Certificates;
using FathomOS.Core.Interfaces;
using LicensingSystem.Client;
using LicensingSystem.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace FathomOS.Shell;

/// <summary>
/// Fathom OS main application entry point with licensing support
/// </summary>
public partial class App : Application
{
    private ModuleManager? _moduleManager;

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
    
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
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
            // LICENSE VALIDATION - Initialize licensing and check status
            // ============================================================================
            System.Diagnostics.Debug.WriteLine("DEBUG: Initializing license...");
            var result = await Licensing.InitializeAsync();
            System.Diagnostics.Debug.WriteLine($"DEBUG: Initial license check result:");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   IsValid: {result.IsValid}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Status: {result.Status}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Message: {result.Message}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   LicenseId: {result.License?.LicenseId ?? "null"}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Product: {result.License?.Product ?? "null"}");
            
            // Handle revoked license - MUST clear local data
            if (result.Status == LicenseStatus.Revoked)
            {
                // Clear stored license to prevent reuse
                LicenseManager.Deactivate();
                
                MessageBox.Show(
                    "Your license has been revoked.\n\n" +
                    "Please contact support for assistance.",
                    "License Revoked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
            
            // Handle expired license (past grace period)
            if (result.Status == LicenseStatus.Expired)
            {
                MessageBox.Show(
                    "Your license has expired and the grace period has ended.\n\n" +
                    "Please renew your subscription to continue using Fathom OS.",
                    "License Expired",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                // Show activation window for renewal
                var renewWindow = new ActivationWindow();
                if (renewWindow.ShowDialog() != true || !renewWindow.ActivationSuccessful)
                {
                    Shutdown();
                    return;
                }
                
                // Re-check after renewal attempt
                result = await Licensing.InitializeAsync();
                if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
                {
                    Shutdown();
                    return;
                }
            }
            
            // If no valid license found, show activation window
            if (!result.IsValid && result.Status != LicenseStatus.GracePeriod)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: Showing activation window...");
                var activationWindow = new ActivationWindow();
                var dialogResult = activationWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"DEBUG: Activation window closed. DialogResult={dialogResult}, ActivationSuccessful={activationWindow.ActivationSuccessful}");
                
                // User cancelled or closed window without activating
                if (dialogResult != true && !activationWindow.ActivationSuccessful)
                {
                    System.Diagnostics.Debug.WriteLine("DEBUG: User cancelled activation - shutting down");
                    Shutdown();
                    return;
                }
                
                // If activation window reports success, trust it and continue
                // The license was already validated and stored by the LicenseManager
                if (activationWindow.ActivationSuccessful)
                {
                    // Refresh the license status in the wrapper (starts timers)
                    result = Licensing.RefreshLicenseStatus();
                    
                    // Log success with details
                    System.Diagnostics.Debug.WriteLine($"DEBUG: License activated successfully!");
                    System.Diagnostics.Debug.WriteLine($"DEBUG:   LicenseId: {result.License?.LicenseId}");
                    System.Diagnostics.Debug.WriteLine($"DEBUG:   IsValid: {result.IsValid}");
                    System.Diagnostics.Debug.WriteLine($"DEBUG:   Status: {result.Status}");
                }
                else
                {
                    // Should not reach here, but just in case
                    System.Diagnostics.Debug.WriteLine("DEBUG: ActivationSuccessful=false - showing error");
                    MessageBox.Show(
                        "License activation could not be completed.\n\n" +
                        "Please try again or contact support.",
                        "Activation Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }
            
            // Show warning if in grace period
            if (result.Status == LicenseStatus.GracePeriod)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: License in grace period - showing warning");
                MessageBox.Show(
                    $"Your Fathom OS license has expired!\n\n" +
                    $"You have {result.GraceDaysRemaining} days remaining before the software stops working.\n\n" +
                    $"Please renew your subscription immediately.",
                    "License Expiring - Action Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            
            // ============================================================================
            // SYNC WITH SERVER - Notify server about this license (fire and forget)
            // This ensures offline licenses get registered on the server
            // NOTE: We do NOT call ForceServerCheckAsync here to avoid race conditions
            // The Dashboard's periodic check will handle server validation later
            // ============================================================================
            _ = Task.Run(async () =>
            {
                try
                {
                    // Small delay to ensure UI is fully loaded first
                    await Task.Delay(2000);
                    
                    // Sync offline license to server if pending
                    var synced = await LicenseManager.SyncOfflineLicenseAsync();
                    
                    if (synced)
                    {
                        System.Diagnostics.Debug.WriteLine("Offline license synced with server successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No offline sync needed or sync endpoint not available");
                    }
                    
                    // DO NOT call ForceServerCheckAsync here!
                    // It can cause race conditions where newly synced licenses appear as NotFound
                    // The Dashboard's periodic check (every 5 min) will handle validation later
                }
                catch (Exception ex)
                {
                    // Don't block startup - just log the error
                    System.Diagnostics.Debug.WriteLine($"Server sync failed (will retry later): {ex.Message}");
                }
            });
            
            // ============================================================================
            // FINAL LICENSE CHECK - Ensure we have a valid license before proceeding
            // This is a safety net to prevent any edge cases from bypassing license check
            // ============================================================================
            var finalCheck = LicenseManager.GetStatusInfo();
            System.Diagnostics.Debug.WriteLine($"DEBUG: Final license check:");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   IsLicensed: {finalCheck.IsLicensed}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Status: {finalCheck.Status}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Edition: {finalCheck.Edition}");
            
            if (!finalCheck.IsLicensed && finalCheck.Status != LicenseStatus.GracePeriod)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: FINAL CHECK FAILED - showing error and shutting down");
                MessageBox.Show(
                    "A valid license is required to use Fathom OS.\n\n" +
                    "Please restart the application and activate your license.",
                    "License Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
            
            System.Diagnostics.Debug.WriteLine("DEBUG: Final check PASSED - proceeding to login");

            // ============================================================================
            // USER AUTHENTICATION - Show login dialog after license validation
            // ============================================================================
            System.Diagnostics.Debug.WriteLine("DEBUG: Showing login dialog...");
            var authService = Services.GetRequiredService<IAuthenticationService>();

            // Show login dialog and wait for result
            var loginResult = await authService.ShowLoginDialogAsync();

            if (!loginResult)
            {
                System.Diagnostics.Debug.WriteLine("DEBUG: Login cancelled or failed - shutting down");
                MessageBox.Show(
                    "Login is required to use Fathom OS.\n\n" +
                    "The application will now close.",
                    "Login Required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"DEBUG: Login successful for user: {authService.CurrentUser?.Username}");

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
            MessageBox.Show(
                $"Error starting Fathom OS:\n\n{ex.Message}",
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

        // Theme Service - unified theme management (depends on EventAggregator)
        services.AddSingleton<IThemeService>(sp =>
            new ThemeService(sp.GetRequiredService<IEventAggregator>()));

        // Settings Service - persistent JSON-based settings
        services.AddSingleton<ISettingsService, SettingsService>();

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
        // AUTHENTICATION SERVICE
        // Centralized authentication for all FathomOS modules
        // ============================================================================
        services.AddSingleton<IAuthenticationService>(sp =>
            new AuthenticationService(
                sp.GetRequiredService<IEventAggregator>(),
                sp.GetRequiredService<ISettingsService>()));

        // ============================================================================
        // MODULE MANAGEMENT
        // ModuleManager receives IServiceProvider to support DI in module instantiation
        // ============================================================================
        services.AddSingleton<IModuleManager>(sp =>
            new ModuleManager(sp));
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
