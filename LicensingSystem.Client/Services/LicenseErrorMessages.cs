// LicensingSystem.Client/Services/LicenseErrorMessages.cs
// User-friendly error messages for all license-related errors
// Provides localized, actionable error messages for various license states

using LicensingSystem.Shared;

namespace LicensingSystem.Client.Services;

/// <summary>
/// Provides user-friendly error messages for license-related errors.
/// All messages are designed to be helpful and actionable.
/// </summary>
public static class LicenseErrorMessages
{
    #region License Expired Messages

    /// <summary>
    /// Gets the error message for an expired license
    /// </summary>
    public static LicenseErrorInfo GetExpiredError(DateTime expirationDate, int gracePeriodDays)
    {
        var gracePeriodEnd = expirationDate.AddDays(gracePeriodDays);
        var daysExpired = (int)(DateTime.UtcNow - expirationDate).TotalDays;

        return new LicenseErrorInfo
        {
            ErrorCode = "LICENSE_EXPIRED",
            Title = "License Expired",
            ShortMessage = "Your FathomOS license has expired.",
            DetailedMessage = $"Your license expired on {expirationDate:MMMM d, yyyy}. " +
                $"The grace period ended on {gracePeriodEnd:MMMM d, yyyy}. " +
                "Please renew your license to continue using FathomOS.",
            RecommendedActions = new List<string>
            {
                "Contact your account manager to renew your license",
                "Visit the FathomOS licensing portal to purchase a renewal",
                "If you believe this is an error, contact support with your license ID"
            },
            Severity = ErrorSeverity.Error,
            CanRetry = false,
            ShowContactSupport = true,
            SupportContext = $"License expired {daysExpired} days ago"
        };
    }

    /// <summary>
    /// Gets the error message for a license in grace period
    /// </summary>
    public static LicenseErrorInfo GetGracePeriodWarning(int daysRemaining)
    {
        var severity = daysRemaining switch
        {
            <= 1 => ErrorSeverity.Critical,
            <= 3 => ErrorSeverity.Critical,
            <= 7 => ErrorSeverity.Warning,
            _ => ErrorSeverity.Info
        };

        var title = daysRemaining <= 1
            ? "License Expires Tomorrow!"
            : $"License Expires in {daysRemaining} Days";

        return new LicenseErrorInfo
        {
            ErrorCode = "LICENSE_GRACE_PERIOD",
            Title = title,
            ShortMessage = $"Your license has expired. You have {daysRemaining} days remaining in the grace period.",
            DetailedMessage = $"Your FathomOS license has expired and you are currently in the grace period. " +
                $"You have {daysRemaining} day(s) remaining to renew your license before losing access. " +
                "Please renew immediately to avoid service interruption.",
            RecommendedActions = new List<string>
            {
                "Renew your license immediately to avoid service interruption",
                "Contact your account manager if you need assistance",
                "All features remain available during the grace period"
            },
            Severity = severity,
            CanRetry = false,
            ShowContactSupport = daysRemaining <= 3,
            SupportContext = $"Grace period: {daysRemaining} days remaining"
        };
    }

    #endregion

    #region License Revoked Messages

    /// <summary>
    /// Gets the error message for a revoked license
    /// </summary>
    public static LicenseErrorInfo GetRevokedError(string? reason = null)
    {
        var detailedMessage = "Your FathomOS license has been revoked and can no longer be used. ";
        if (!string.IsNullOrEmpty(reason))
        {
            detailedMessage += $"Reason: {reason}. ";
        }
        detailedMessage += "Please contact support for assistance.";

        return new LicenseErrorInfo
        {
            ErrorCode = "LICENSE_REVOKED",
            Title = "License Revoked",
            ShortMessage = "Your license has been revoked.",
            DetailedMessage = detailedMessage,
            RecommendedActions = new List<string>
            {
                "Contact support to understand why your license was revoked",
                "Check if your organization has a new license available",
                "Provide your license ID when contacting support"
            },
            Severity = ErrorSeverity.Error,
            CanRetry = false,
            ShowContactSupport = true,
            SupportContext = $"License revoked. Reason: {reason ?? "Not specified"}"
        };
    }

    #endregion

    #region Hardware Mismatch Messages

    /// <summary>
    /// Gets the error message for a hardware mismatch
    /// </summary>
    public static LicenseErrorInfo GetHardwareMismatchError(int matchedFingerprints, int requiredFingerprints)
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "HARDWARE_MISMATCH",
            Title = "Hardware Changed",
            ShortMessage = "This license is registered to different hardware.",
            DetailedMessage = $"Your license is bound to specific hardware. " +
                $"Only {matchedFingerprints} of {requiredFingerprints} required hardware fingerprints matched. " +
                "This usually happens when major hardware components have been changed or the license was activated on a different computer.",
            RecommendedActions = new List<string>
            {
                "If you replaced hardware, use the license transfer feature to move your license",
                "If this is a new computer, deactivate the old computer first and reactivate here",
                "Contact support if you need assistance with hardware fingerprint reset"
            },
            Severity = ErrorSeverity.Error,
            CanRetry = false,
            ShowContactSupport = true,
            SupportContext = $"Hardware match: {matchedFingerprints}/{requiredFingerprints}"
        };
    }

    #endregion

    #region Network Error Messages

    /// <summary>
    /// Gets the error message for network errors during validation
    /// </summary>
    public static LicenseErrorInfo GetNetworkError(string? technicalDetails = null)
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "NETWORK_ERROR",
            Title = "Connection Error",
            ShortMessage = "Unable to reach the license server.",
            DetailedMessage = "FathomOS could not connect to the license server. " +
                "This may be due to internet connectivity issues or the server being temporarily unavailable. " +
                "The application will continue to work in offline mode if you have a valid cached license.",
            RecommendedActions = new List<string>
            {
                "Check your internet connection",
                "Try again in a few minutes",
                "If the problem persists, check your firewall settings",
                "Contact support if you continue to experience issues"
            },
            Severity = ErrorSeverity.Warning,
            CanRetry = true,
            ShowContactSupport = false,
            TechnicalDetails = technicalDetails
        };
    }

    /// <summary>
    /// Gets the error message for server errors
    /// </summary>
    public static LicenseErrorInfo GetServerError(int? statusCode = null)
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "SERVER_ERROR",
            Title = "Server Error",
            ShortMessage = "The license server encountered an error.",
            DetailedMessage = "The FathomOS license server is experiencing technical difficulties. " +
                "This is usually temporary. Please try again in a few minutes. " +
                "The application will continue to work in offline mode if you have a valid cached license.",
            RecommendedActions = new List<string>
            {
                "Wait a few minutes and try again",
                "Check the FathomOS status page for any known issues",
                "If the problem persists, contact support"
            },
            Severity = ErrorSeverity.Warning,
            CanRetry = true,
            ShowContactSupport = false,
            TechnicalDetails = statusCode.HasValue ? $"HTTP Status: {statusCode}" : null
        };
    }

    #endregion

    #region Invalid License Messages

    /// <summary>
    /// Gets the error message for an invalid license file
    /// </summary>
    public static LicenseErrorInfo GetInvalidLicenseError(string? reason = null)
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "INVALID_LICENSE",
            Title = "Invalid License",
            ShortMessage = "The license file is invalid or corrupted.",
            DetailedMessage = string.IsNullOrEmpty(reason)
                ? "The license file could not be validated. It may be corrupted, modified, or not a valid FathomOS license."
                : $"The license file is invalid: {reason}",
            RecommendedActions = new List<string>
            {
                "Re-download your license file from the licensing portal",
                "Ensure the license file was not modified or corrupted during download",
                "Contact support if you continue to have issues"
            },
            Severity = ErrorSeverity.Error,
            CanRetry = false,
            ShowContactSupport = true,
            SupportContext = reason ?? "Invalid license signature"
        };
    }

    /// <summary>
    /// Gets the error message for a corrupted license
    /// </summary>
    public static LicenseErrorInfo GetCorruptedLicenseError()
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "LICENSE_CORRUPTED",
            Title = "License Corrupted",
            ShortMessage = "The stored license data is corrupted.",
            DetailedMessage = "The locally stored license data appears to be corrupted. " +
                "This can happen due to disk errors or file system issues. " +
                "You will need to reactivate your license.",
            RecommendedActions = new List<string>
            {
                "Try activating your license again using your license key",
                "Re-import your license file if you have one",
                "Contact support if the problem persists"
            },
            Severity = ErrorSeverity.Error,
            CanRetry = true,
            ShowContactSupport = false
        };
    }

    #endregion

    #region Not Found Messages

    /// <summary>
    /// Gets the error message for no license found
    /// </summary>
    public static LicenseErrorInfo GetNotFoundError()
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "LICENSE_NOT_FOUND",
            Title = "No License Found",
            ShortMessage = "FathomOS is not activated.",
            DetailedMessage = "No valid license was found for FathomOS. " +
                "Please activate your copy using your license key or license file.",
            RecommendedActions = new List<string>
            {
                "Enter your license key to activate",
                "Import a license file if you received one",
                "Contact sales to purchase a license",
                "Visit the FathomOS website for trial options"
            },
            Severity = ErrorSeverity.Info,
            CanRetry = false,
            ShowContactSupport = false
        };
    }

    #endregion

    #region Seat Limit Messages

    /// <summary>
    /// Gets the error message for seat limit exceeded
    /// </summary>
    public static LicenseErrorInfo GetSeatLimitExceededError(int maxSeats, int currentUsers, List<string>? activeUsers = null)
    {
        var userList = activeUsers != null && activeUsers.Count > 0
            ? $"Current users: {string.Join(", ", activeUsers.Take(5))}" +
                (activeUsers.Count > 5 ? $" and {activeUsers.Count - 5} more" : "")
            : "";

        return new LicenseErrorInfo
        {
            ErrorCode = "SEAT_LIMIT_EXCEEDED",
            Title = "Seat Limit Reached",
            ShortMessage = $"All {maxSeats} license seats are in use.",
            DetailedMessage = $"Your license allows {maxSeats} concurrent users, but {currentUsers} users are currently active. " +
                "Please wait for another user to log out or contact your administrator. " +
                (!string.IsNullOrEmpty(userList) ? userList : ""),
            RecommendedActions = new List<string>
            {
                "Wait for another user to finish and log out",
                "Contact your administrator to free up a seat",
                "Request additional seats from your account manager"
            },
            Severity = ErrorSeverity.Warning,
            CanRetry = true,
            ShowContactSupport = false,
            SupportContext = $"Seats: {currentUsers}/{maxSeats}"
        };
    }

    #endregion

    #region Offline Period Messages

    /// <summary>
    /// Gets the error message for offline period exceeded
    /// </summary>
    public static LicenseErrorInfo GetOfflinePeriodExceededError(int maxOfflineDays, int daysSinceOnline)
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "OFFLINE_PERIOD_EXCEEDED",
            Title = "Online Validation Required",
            ShortMessage = "License requires online verification.",
            DetailedMessage = $"Your license allows up to {maxOfflineDays} days of offline use. " +
                $"It has been {daysSinceOnline} days since your last online validation. " +
                "Please connect to the internet to verify your license.",
            RecommendedActions = new List<string>
            {
                "Connect to the internet and restart FathomOS",
                "If you cannot access the internet, contact support for assistance",
                "Consider requesting an extended offline license if you frequently work offline"
            },
            Severity = ErrorSeverity.Warning,
            CanRetry = true,
            ShowContactSupport = false,
            SupportContext = $"Offline for {daysSinceOnline} days (max: {maxOfflineDays})"
        };
    }

    #endregion

    #region Clock Tampering Messages

    /// <summary>
    /// Gets the error message for detected clock tampering
    /// </summary>
    public static LicenseErrorInfo GetClockTamperingError()
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "CLOCK_TAMPERING_DETECTED",
            Title = "System Clock Error",
            ShortMessage = "System clock appears to have been modified.",
            DetailedMessage = "FathomOS detected that your system clock may have been changed or is significantly incorrect. " +
                "License validation requires an accurate system clock to function properly. " +
                "Please correct your system time and try again.",
            RecommendedActions = new List<string>
            {
                "Verify your system date and time are correct",
                "Enable automatic time synchronization in your system settings",
                "If your system time is correct, contact support for assistance"
            },
            Severity = ErrorSeverity.Error,
            CanRetry = true,
            ShowContactSupport = false
        };
    }

    #endregion

    #region Transfer Messages

    /// <summary>
    /// Gets the error message for transfer limit reached
    /// </summary>
    public static LicenseErrorInfo GetTransferLimitReachedError(int totalTransfers, int maxTransfers)
    {
        return new LicenseErrorInfo
        {
            ErrorCode = "TRANSFER_LIMIT_REACHED",
            Title = "Transfer Limit Reached",
            ShortMessage = "Maximum number of transfers has been reached.",
            DetailedMessage = $"Your license allows up to {maxTransfers} transfers, and you have already transferred {totalTransfers} times. " +
                "Contact support to request additional transfers or discuss your options.",
            RecommendedActions = new List<string>
            {
                "Contact support to request additional transfer allowance",
                "Consider upgrading to a license with more transfers",
                "Deactivate unused machines to free up your current license"
            },
            Severity = ErrorSeverity.Warning,
            CanRetry = false,
            ShowContactSupport = true,
            SupportContext = $"Transfers: {totalTransfers}/{maxTransfers}"
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the appropriate error info based on license status
    /// </summary>
    public static LicenseErrorInfo GetErrorForStatus(LicenseStatus status, string? additionalInfo = null)
    {
        return status switch
        {
            LicenseStatus.Expired => GetExpiredError(DateTime.UtcNow.AddDays(-30), 14), // Defaults
            LicenseStatus.GracePeriod => GetGracePeriodWarning(7), // Default
            LicenseStatus.Revoked => GetRevokedError(additionalInfo),
            LicenseStatus.HardwareMismatch => GetHardwareMismatchError(1, 3), // Defaults
            LicenseStatus.InvalidSignature => GetInvalidLicenseError(additionalInfo),
            LicenseStatus.Corrupted => GetCorruptedLicenseError(),
            LicenseStatus.NotFound => GetNotFoundError(),
            LicenseStatus.Error => GetServerError(),
            _ => GetInvalidLicenseError(additionalInfo ?? "Unknown status")
        };
    }

    /// <summary>
    /// Gets a short, user-friendly message for status bar display
    /// </summary>
    public static string GetStatusBarMessage(LicenseStatus status, int? daysRemaining = null)
    {
        return status switch
        {
            LicenseStatus.Valid => daysRemaining.HasValue && daysRemaining > 0
                ? $"Licensed ({daysRemaining} days remaining)"
                : "Licensed",
            LicenseStatus.GracePeriod => daysRemaining.HasValue
                ? $"EXPIRED - {daysRemaining} grace days left"
                : "EXPIRED - In grace period",
            LicenseStatus.Expired => "License Expired",
            LicenseStatus.Revoked => "License Revoked",
            LicenseStatus.HardwareMismatch => "Hardware Changed",
            LicenseStatus.NotFound => "Not Activated",
            LicenseStatus.InvalidSignature => "Invalid License",
            LicenseStatus.Corrupted => "License Error",
            LicenseStatus.Error => "Validation Error",
            _ => "Unknown Status"
        };
    }

    #endregion
}

/// <summary>
/// Detailed error information for display to users
/// </summary>
public class LicenseErrorInfo
{
    /// <summary>
    /// Machine-readable error code
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Short title for the error
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Brief error message (one line)
    /// </summary>
    public string ShortMessage { get; set; } = string.Empty;

    /// <summary>
    /// Full error message with context
    /// </summary>
    public string DetailedMessage { get; set; } = string.Empty;

    /// <summary>
    /// List of recommended actions for the user
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>
    /// Error severity level
    /// </summary>
    public ErrorSeverity Severity { get; set; }

    /// <summary>
    /// Whether the operation can be retried
    /// </summary>
    public bool CanRetry { get; set; }

    /// <summary>
    /// Whether to show "Contact Support" option prominently
    /// </summary>
    public bool ShowContactSupport { get; set; }

    /// <summary>
    /// Context to provide when contacting support
    /// </summary>
    public string? SupportContext { get; set; }

    /// <summary>
    /// Technical details (for advanced users/support)
    /// </summary>
    public string? TechnicalDetails { get; set; }

    /// <summary>
    /// Gets a formatted string for clipboard/email
    /// </summary>
    public string GetSupportTicketInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Error Code: {ErrorCode}");
        sb.AppendLine($"Title: {Title}");
        sb.AppendLine($"Message: {ShortMessage}");
        if (!string.IsNullOrEmpty(SupportContext))
            sb.AppendLine($"Context: {SupportContext}");
        if (!string.IsNullOrEmpty(TechnicalDetails))
            sb.AppendLine($"Technical Details: {TechnicalDetails}");
        sb.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Machine: {Environment.MachineName}");
        sb.AppendLine($"User: {Environment.UserName}");
        return sb.ToString();
    }
}

/// <summary>
/// Error severity levels
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Informational - no action required
    /// </summary>
    Info,

    /// <summary>
    /// Warning - attention needed but not blocking
    /// </summary>
    Warning,

    /// <summary>
    /// Error - blocking issue that needs resolution
    /// </summary>
    Error,

    /// <summary>
    /// Critical - immediate attention required
    /// </summary>
    Critical
}
