using FathomOS.Core.Interfaces;

namespace FathomOS.Core.Messaging;

/// <summary>
/// Published when a module completes its work
/// </summary>
public record ModuleWorkCompletedEvent(string ModuleId, string? CertificateId = null);

/// <summary>
/// Published when the application theme changes
/// </summary>
public record ThemeChangedEvent(AppTheme Theme);

/// <summary>
/// Published when an error occurs in any module
/// </summary>
public record ErrorOccurredEvent(string ModuleId, string Message, Exception? Exception = null);

/// <summary>
/// Published when the license status changes
/// </summary>
public record LicenseStatusChangedEvent(bool IsLicensed, string Status, string? Edition = null);

/// <summary>
/// Published when a module is loaded
/// </summary>
public record ModuleLoadedEvent(string ModuleId, string DisplayName);

/// <summary>
/// Published when a module window is launched
/// </summary>
public record ModuleLaunchedEvent(string ModuleId, string DisplayName);

/// <summary>
/// Published when settings change
/// </summary>
public record SettingsChangedEvent(string Key, object? OldValue, object? NewValue);

/// <summary>
/// Published when a certificate is created
/// </summary>
public record CertificateCreatedEvent(string CertificateId, string ModuleId, DateTime CreatedAt);

/// <summary>
/// Published when certificates are synced to server
/// </summary>
public record CertificatesSyncedEvent(int SyncedCount, int FailedCount);

/// <summary>
/// Published when user authentication state changes (login/logout).
/// Modules should subscribe to this to update their UI based on auth state.
/// </summary>
public record UserAuthenticationChangedEvent(IUser? User, bool IsLoggedIn);

/// <summary>
/// Published when a logout is requested (e.g., from session timeout or user action).
/// The authentication service listens for this to perform the actual logout.
/// </summary>
public record UserLogoutRequestedEvent();
