using LicensingSystem.Client;
using LicensingSystem.Shared;

namespace FathomOS.Shell.Services;

/// <summary>
/// Startup result indicating what action is needed at application launch.
/// </summary>
public enum StartupResult
{
    /// <summary>
    /// No license found - needs activation.
    /// </summary>
    NeedsLicenseActivation,

    /// <summary>
    /// License valid but no local users - needs account creation.
    /// </summary>
    NeedsAccountCreation,

    /// <summary>
    /// License valid and users exist - ready for login.
    /// </summary>
    ReadyForLogin,

    /// <summary>
    /// License has expired and is past grace period.
    /// </summary>
    LicenseExpired,

    /// <summary>
    /// License is in grace period - warn user.
    /// </summary>
    LicenseInGracePeriod,

    /// <summary>
    /// License is invalid (signature, hardware mismatch, etc.)
    /// </summary>
    LicenseInvalid,

    /// <summary>
    /// License has been revoked.
    /// </summary>
    LicenseRevoked
}

/// <summary>
/// Startup flow information returned by AppStartupService.
/// </summary>
public class StartupFlowResult
{
    /// <summary>
    /// The startup result indicating what action is needed.
    /// </summary>
    public StartupResult Result { get; set; }

    /// <summary>
    /// The license validation result (if license was checked).
    /// </summary>
    public LicenseValidationResult? LicenseResult { get; set; }

    /// <summary>
    /// Error message if applicable.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of grace days remaining (if in grace period).
    /// </summary>
    public int GraceDaysRemaining { get; set; }

    /// <summary>
    /// Whether the startup flow allows the application to proceed.
    /// </summary>
    public bool CanProceed => Result == StartupResult.NeedsLicenseActivation ||
                              Result == StartupResult.NeedsAccountCreation ||
                              Result == StartupResult.ReadyForLogin ||
                              Result == StartupResult.LicenseInGracePeriod;
}

/// <summary>
/// Service that manages the application startup flow.
/// Determines whether license activation, account creation, or login is needed.
/// </summary>
public class AppStartupService
{
    private readonly FathomOSLicenseIntegration _licenseIntegration;
    private readonly ILocalUserService _userService;

    /// <summary>
    /// Creates a new AppStartupService.
    /// </summary>
    /// <param name="licenseIntegration">The license integration instance</param>
    /// <param name="userService">The local user service</param>
    public AppStartupService(FathomOSLicenseIntegration licenseIntegration, ILocalUserService userService)
    {
        _licenseIntegration = licenseIntegration ?? throw new ArgumentNullException(nameof(licenseIntegration));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    /// <summary>
    /// Determines the startup flow based on license and user state.
    /// </summary>
    /// <returns>The startup flow result</returns>
    public async Task<StartupFlowResult> DetermineStartupFlowAsync()
    {
        System.Diagnostics.Debug.WriteLine("AppStartupService: Determining startup flow...");

        // Step 1: Check license
        var licenseResult = await _licenseIntegration.InitializeAsync();

        System.Diagnostics.Debug.WriteLine($"AppStartupService: License check result - Status: {licenseResult.Status}, IsValid: {licenseResult.IsValid}");

        // Handle revoked license
        if (licenseResult.Status == LicenseStatus.Revoked)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseRevoked,
                LicenseResult = licenseResult,
                ErrorMessage = "Your license has been revoked. Please contact support."
            };
        }

        // Handle expired license (past grace period)
        if (licenseResult.Status == LicenseStatus.Expired)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseExpired,
                LicenseResult = licenseResult,
                ErrorMessage = "Your license has expired. Please renew to continue."
            };
        }

        // Handle invalid license
        if (!licenseResult.IsValid && licenseResult.Status != LicenseStatus.GracePeriod && licenseResult.Status != LicenseStatus.NotFound)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseInvalid,
                LicenseResult = licenseResult,
                ErrorMessage = licenseResult.Message ?? "License validation failed."
            };
        }

        // Handle no license found
        if (licenseResult.Status == LicenseStatus.NotFound || licenseResult.License == null)
        {
            System.Diagnostics.Debug.WriteLine("AppStartupService: No license found - needs activation");
            return new StartupFlowResult
            {
                Result = StartupResult.NeedsLicenseActivation,
                LicenseResult = licenseResult
            };
        }

        // Step 2: License is valid (or in grace period) - check for users
        var hasUsers = _userService.HasUsers();
        System.Diagnostics.Debug.WriteLine($"AppStartupService: Has users: {hasUsers}");

        if (!hasUsers)
        {
            // License valid but no users - need to create admin account
            System.Diagnostics.Debug.WriteLine("AppStartupService: No users found - needs account creation");
            return new StartupFlowResult
            {
                Result = StartupResult.NeedsAccountCreation,
                LicenseResult = licenseResult
            };
        }

        // Step 3: License valid and users exist
        if (licenseResult.Status == LicenseStatus.GracePeriod)
        {
            System.Diagnostics.Debug.WriteLine($"AppStartupService: License in grace period - {licenseResult.GraceDaysRemaining} days remaining");
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseInGracePeriod,
                LicenseResult = licenseResult,
                GraceDaysRemaining = licenseResult.GraceDaysRemaining
            };
        }

        System.Diagnostics.Debug.WriteLine("AppStartupService: Ready for login");
        return new StartupFlowResult
        {
            Result = StartupResult.ReadyForLogin,
            LicenseResult = licenseResult
        };
    }

    /// <summary>
    /// Synchronous version of DetermineStartupFlow for simpler use cases.
    /// </summary>
    public StartupFlowResult DetermineStartupFlow()
    {
        return DetermineStartupFlowAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Refreshes the startup state after an action (e.g., license activation).
    /// </summary>
    public StartupFlowResult RefreshStartupState()
    {
        System.Diagnostics.Debug.WriteLine("AppStartupService: Refreshing startup state...");

        // Get current license status without re-initializing
        var licenseResult = _licenseIntegration.RefreshLicenseStatus();

        // Handle various license states
        if (licenseResult.Status == LicenseStatus.Revoked)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseRevoked,
                LicenseResult = licenseResult,
                ErrorMessage = "Your license has been revoked."
            };
        }

        if (licenseResult.Status == LicenseStatus.Expired)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseExpired,
                LicenseResult = licenseResult,
                ErrorMessage = "Your license has expired."
            };
        }

        if (!licenseResult.IsValid && licenseResult.Status != LicenseStatus.GracePeriod && licenseResult.Status != LicenseStatus.NotFound)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseInvalid,
                LicenseResult = licenseResult,
                ErrorMessage = licenseResult.Message
            };
        }

        if (licenseResult.Status == LicenseStatus.NotFound || licenseResult.License == null)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.NeedsLicenseActivation,
                LicenseResult = licenseResult
            };
        }

        // Check for users
        var hasUsers = _userService.HasUsers();

        if (!hasUsers)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.NeedsAccountCreation,
                LicenseResult = licenseResult
            };
        }

        if (licenseResult.Status == LicenseStatus.GracePeriod)
        {
            return new StartupFlowResult
            {
                Result = StartupResult.LicenseInGracePeriod,
                LicenseResult = licenseResult,
                GraceDaysRemaining = licenseResult.GraceDaysRemaining
            };
        }

        return new StartupFlowResult
        {
            Result = StartupResult.ReadyForLogin,
            LicenseResult = licenseResult
        };
    }
}
