// LicensingSystem.Shared/Models/LicenseFeatures.cs
// Enhanced license features model for FathomOS licensing system
// Supports feature flags, module access, and tier-based licensing

using System.Text.Json.Serialization;

namespace LicensingSystem.Shared.Models;

/// <summary>
/// Represents all enabled features and modules for a license.
/// Provides structured access to feature flags, module permissions, and tier information.
/// </summary>
public class LicenseFeatures
{
    /// <summary>
    /// License tier (e.g., "Basic", "Professional", "Enterprise")
    /// </summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "Basic";

    /// <summary>
    /// List of licensed module IDs
    /// </summary>
    [JsonPropertyName("modules")]
    public List<string> Modules { get; set; } = new();

    /// <summary>
    /// List of enabled feature flags
    /// </summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Maximum number of concurrent seats allowed
    /// </summary>
    [JsonPropertyName("maxSeats")]
    public int MaxSeats { get; set; } = 1;

    /// <summary>
    /// Whether offline mode is allowed
    /// </summary>
    [JsonPropertyName("allowOffline")]
    public bool AllowOffline { get; set; } = true;

    /// <summary>
    /// Maximum offline days before requiring server validation
    /// </summary>
    [JsonPropertyName("maxOfflineDays")]
    public int MaxOfflineDays { get; set; } = 30;

    /// <summary>
    /// Whether license transfer is allowed
    /// </summary>
    [JsonPropertyName("allowTransfer")]
    public bool AllowTransfer { get; set; } = false;

    /// <summary>
    /// Maximum number of transfers allowed (0 = unlimited)
    /// </summary>
    [JsonPropertyName("maxTransfers")]
    public int MaxTransfers { get; set; } = 0;

    /// <summary>
    /// Custom feature limits (e.g., "MaxProjects": "10")
    /// </summary>
    [JsonPropertyName("limits")]
    public Dictionary<string, string> Limits { get; set; } = new();

    /// <summary>
    /// Checks if a specific module is licensed
    /// </summary>
    public bool HasModule(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId))
            return false;
        return Modules.Contains(moduleId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a specific feature is enabled
    /// </summary>
    public bool HasFeature(string featureName)
    {
        if (string.IsNullOrEmpty(featureName))
            return false;
        return Features.Contains(featureName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a limit value by key
    /// </summary>
    public int GetLimit(string key, int defaultValue = 0)
    {
        if (Limits.TryGetValue(key, out var value) && int.TryParse(value, out var limit))
            return limit;
        return defaultValue;
    }

    /// <summary>
    /// Gets the tier level for comparison
    /// </summary>
    public int GetTierLevel()
    {
        return Tier?.ToLowerInvariant() switch
        {
            "basic" or "starter" => 1,
            "professional" or "pro" => 2,
            "enterprise" or "business" => 3,
            "ultimate" => 4,
            _ => 0
        };
    }

    /// <summary>
    /// Checks if current tier is at least the specified level
    /// </summary>
    public bool HasMinimumTier(string minimumTier)
    {
        int currentLevel = GetTierLevel();
        int requiredLevel = minimumTier?.ToLowerInvariant() switch
        {
            "basic" or "starter" => 1,
            "professional" or "pro" => 2,
            "enterprise" or "business" => 3,
            "ultimate" => 4,
            _ => 0
        };
        return currentLevel >= requiredLevel;
    }

    /// <summary>
    /// Creates a LicenseFeatures instance from a list of feature strings
    /// </summary>
    public static LicenseFeatures FromFeatureList(List<string> featureList)
    {
        var result = new LicenseFeatures();

        foreach (var feature in featureList)
        {
            if (feature.StartsWith("Module:", StringComparison.OrdinalIgnoreCase))
            {
                result.Modules.Add(feature.Substring(7));
            }
            else if (feature.StartsWith("Tier:", StringComparison.OrdinalIgnoreCase))
            {
                result.Tier = feature.Substring(5);
            }
            else if (feature.StartsWith("Seats:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(feature.Substring(6), out var seats))
                    result.MaxSeats = seats;
            }
            else if (feature.StartsWith("Limit:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = feature.Substring(6).Split('=', 2);
                if (parts.Length == 2)
                    result.Limits[parts[0]] = parts[1];
            }
            else if (feature.Equals("AllowTransfer", StringComparison.OrdinalIgnoreCase))
            {
                result.AllowTransfer = true;
            }
            else if (feature.Equals("AllowOffline", StringComparison.OrdinalIgnoreCase))
            {
                result.AllowOffline = true;
            }
            else
            {
                result.Features.Add(feature);
            }
        }

        return result;
    }
}

/// <summary>
/// Enhanced license validation result with detailed status information
/// </summary>
public class EnhancedLicenseValidationResult
{
    /// <summary>
    /// Whether the license is valid for use
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// License status code
    /// </summary>
    public LicenseValidationStatus Status { get; set; } = LicenseValidationStatus.Unknown;

    /// <summary>
    /// Human-readable status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error code for programmatic handling
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// User-friendly error message for display
    /// </summary>
    public string? UserMessage { get; set; }

    /// <summary>
    /// Days remaining until license expiration
    /// </summary>
    public int DaysRemaining { get; set; }

    /// <summary>
    /// Days remaining in grace period (if applicable)
    /// </summary>
    public int GraceDaysRemaining { get; set; }

    /// <summary>
    /// Whether currently in grace period
    /// </summary>
    public bool IsInGracePeriod { get; set; }

    /// <summary>
    /// Enabled features for this license
    /// </summary>
    public LicenseFeatures Features { get; set; } = new();

    /// <summary>
    /// License ID
    /// </summary>
    public string? LicenseId { get; set; }

    /// <summary>
    /// License edition
    /// </summary>
    public string? Edition { get; set; }

    /// <summary>
    /// Customer name
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Expiration date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this was validated online or offline
    /// </summary>
    public bool WasOnlineValidation { get; set; }

    /// <summary>
    /// Time of validation
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Server time (if online validation)
    /// </summary>
    public DateTime? ServerTime { get; set; }

    /// <summary>
    /// Creates a success result
    /// </summary>
    public static EnhancedLicenseValidationResult Success(
        string licenseId,
        LicenseFeatures features,
        int daysRemaining,
        string? edition = null,
        string? customerName = null)
    {
        return new EnhancedLicenseValidationResult
        {
            IsValid = true,
            Status = LicenseValidationStatus.Valid,
            Message = "License is valid.",
            LicenseId = licenseId,
            Features = features,
            DaysRemaining = daysRemaining,
            Edition = edition,
            CustomerName = customerName
        };
    }

    /// <summary>
    /// Creates a grace period result
    /// </summary>
    public static EnhancedLicenseValidationResult GracePeriod(
        string licenseId,
        LicenseFeatures features,
        int graceDaysRemaining,
        string? edition = null)
    {
        return new EnhancedLicenseValidationResult
        {
            IsValid = true,
            Status = LicenseValidationStatus.GracePeriod,
            Message = $"License expired! {graceDaysRemaining} grace days remaining.",
            LicenseId = licenseId,
            Features = features,
            DaysRemaining = 0,
            GraceDaysRemaining = graceDaysRemaining,
            IsInGracePeriod = true,
            Edition = edition,
            ErrorCode = "LICENSE_GRACE_PERIOD",
            UserMessage = $"Your license has expired. You have {graceDaysRemaining} days remaining in the grace period. Please renew your license to continue using FathomOS."
        };
    }

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static EnhancedLicenseValidationResult Failure(
        LicenseValidationStatus status,
        string message,
        string errorCode,
        string userMessage)
    {
        return new EnhancedLicenseValidationResult
        {
            IsValid = false,
            Status = status,
            Message = message,
            ErrorCode = errorCode,
            UserMessage = userMessage
        };
    }
}

/// <summary>
/// Detailed license validation status codes
/// </summary>
public enum LicenseValidationStatus
{
    /// <summary>Unknown status</summary>
    Unknown = 0,

    /// <summary>License is valid and active</summary>
    Valid = 1,

    /// <summary>License expired but in grace period</summary>
    GracePeriod = 2,

    /// <summary>License has expired (grace period ended)</summary>
    Expired = 3,

    /// <summary>License signature is invalid</summary>
    InvalidSignature = 4,

    /// <summary>Hardware fingerprint mismatch</summary>
    HardwareMismatch = 5,

    /// <summary>License has been revoked</summary>
    Revoked = 6,

    /// <summary>No license found</summary>
    NotFound = 7,

    /// <summary>License file is corrupted</summary>
    Corrupted = 8,

    /// <summary>Network error during validation</summary>
    NetworkError = 9,

    /// <summary>Server validation failed</summary>
    ServerError = 10,

    /// <summary>License version not supported</summary>
    VersionNotSupported = 11,

    /// <summary>Product name mismatch</summary>
    ProductMismatch = 12,

    /// <summary>Clock tampering detected</summary>
    ClockTampering = 13,

    /// <summary>Offline period exceeded</summary>
    OfflinePeriodExceeded = 14,

    /// <summary>Seat limit exceeded</summary>
    SeatLimitExceeded = 15,

    /// <summary>License pending activation</summary>
    PendingActivation = 16
}

/// <summary>
/// Usage data for a license
/// </summary>
public class LicenseUsageData
{
    /// <summary>
    /// License ID
    /// </summary>
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>
    /// Module usage counts
    /// </summary>
    [JsonPropertyName("moduleUsage")]
    public Dictionary<string, int> ModuleUsage { get; set; } = new();

    /// <summary>
    /// Feature usage counts
    /// </summary>
    [JsonPropertyName("featureUsage")]
    public Dictionary<string, int> FeatureUsage { get; set; } = new();

    /// <summary>
    /// Daily active usage
    /// </summary>
    [JsonPropertyName("dailyUsage")]
    public Dictionary<string, UsageDayRecord> DailyUsage { get; set; } = new();

    /// <summary>
    /// Last sync timestamp
    /// </summary>
    [JsonPropertyName("lastSyncAt")]
    public DateTime LastSyncAt { get; set; }

    /// <summary>
    /// Total session count
    /// </summary>
    [JsonPropertyName("totalSessions")]
    public int TotalSessions { get; set; }

    /// <summary>
    /// Total usage time in minutes
    /// </summary>
    [JsonPropertyName("totalUsageMinutes")]
    public long TotalUsageMinutes { get; set; }
}

/// <summary>
/// Usage record for a single day
/// </summary>
public class UsageDayRecord
{
    /// <summary>
    /// Date of usage
    /// </summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Number of sessions
    /// </summary>
    [JsonPropertyName("sessions")]
    public int Sessions { get; set; }

    /// <summary>
    /// Usage time in minutes
    /// </summary>
    [JsonPropertyName("usageMinutes")]
    public int UsageMinutes { get; set; }

    /// <summary>
    /// Modules used
    /// </summary>
    [JsonPropertyName("modulesUsed")]
    public List<string> ModulesUsed { get; set; } = new();

    /// <summary>
    /// Features used
    /// </summary>
    [JsonPropertyName("featuresUsed")]
    public List<string> FeaturesUsed { get; set; } = new();
}

/// <summary>
/// License transfer token for moving licenses between machines
/// </summary>
public class LicenseTransferToken
{
    /// <summary>
    /// Unique transfer token
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// License ID being transferred
    /// </summary>
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>
    /// Source hardware fingerprint
    /// </summary>
    [JsonPropertyName("sourceFingerprint")]
    public string SourceFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// When the token was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the token expires
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been used
    /// </summary>
    [JsonPropertyName("isUsed")]
    public bool IsUsed { get; set; }

    /// <summary>
    /// When the token was used
    /// </summary>
    [JsonPropertyName("usedAt")]
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Target hardware fingerprint (set when used)
    /// </summary>
    [JsonPropertyName("targetFingerprint")]
    public string? TargetFingerprint { get; set; }

    /// <summary>
    /// Transfer number (for tracking limits)
    /// </summary>
    [JsonPropertyName("transferNumber")]
    public int TransferNumber { get; set; }

    /// <summary>
    /// Check if token is still valid
    /// </summary>
    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
}

/// <summary>
/// Multi-seat session information
/// </summary>
public class LicenseSeatSession
{
    /// <summary>
    /// Unique session ID
    /// </summary>
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// License ID
    /// </summary>
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>
    /// Hardware fingerprint of the machine
    /// </summary>
    [JsonPropertyName("hardwareFingerprint")]
    public string HardwareFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Machine name
    /// </summary>
    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    /// <summary>
    /// Session start time
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Last heartbeat time
    /// </summary>
    [JsonPropertyName("lastHeartbeat")]
    public DateTime LastHeartbeat { get; set; }

    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    [JsonPropertyName("timeoutMinutes")]
    public int TimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Whether session is still active
    /// </summary>
    public bool IsActive => DateTime.UtcNow - LastHeartbeat < TimeSpan.FromMinutes(TimeoutMinutes);
}

/// <summary>
/// Grace period warning information
/// </summary>
public class GracePeriodWarning
{
    /// <summary>
    /// Days remaining in grace period
    /// </summary>
    public int DaysRemaining { get; set; }

    /// <summary>
    /// Warning severity level
    /// </summary>
    public GraceWarningSeverity Severity { get; set; }

    /// <summary>
    /// User-friendly warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Recommended action
    /// </summary>
    public string RecommendedAction { get; set; } = string.Empty;

    /// <summary>
    /// Whether to show a modal dialog
    /// </summary>
    public bool ShowModal { get; set; }

    /// <summary>
    /// Creates appropriate warning based on days remaining
    /// </summary>
    public static GracePeriodWarning Create(int daysRemaining)
    {
        return daysRemaining switch
        {
            <= 0 => new GracePeriodWarning
            {
                DaysRemaining = 0,
                Severity = GraceWarningSeverity.Expired,
                Message = "Your license grace period has expired. FathomOS will now operate in limited mode.",
                RecommendedAction = "Please renew your license immediately to restore full functionality.",
                ShowModal = true
            },
            1 => new GracePeriodWarning
            {
                DaysRemaining = 1,
                Severity = GraceWarningSeverity.Critical,
                Message = "URGENT: Your license grace period expires TOMORROW!",
                RecommendedAction = "Renew your license now to avoid service interruption.",
                ShowModal = true
            },
            <= 3 => new GracePeriodWarning
            {
                DaysRemaining = daysRemaining,
                Severity = GraceWarningSeverity.Critical,
                Message = $"Your license grace period expires in {daysRemaining} days.",
                RecommendedAction = "Please renew your license as soon as possible.",
                ShowModal = true
            },
            <= 7 => new GracePeriodWarning
            {
                DaysRemaining = daysRemaining,
                Severity = GraceWarningSeverity.Warning,
                Message = $"Your license has expired. You have {daysRemaining} days remaining in the grace period.",
                RecommendedAction = "Please renew your license to continue using FathomOS.",
                ShowModal = true
            },
            _ => new GracePeriodWarning
            {
                DaysRemaining = daysRemaining,
                Severity = GraceWarningSeverity.Info,
                Message = $"Your license has expired. Grace period: {daysRemaining} days remaining.",
                RecommendedAction = "Consider renewing your license.",
                ShowModal = false
            }
        };
    }
}

/// <summary>
/// Grace period warning severity levels
/// </summary>
public enum GraceWarningSeverity
{
    /// <summary>Informational - more than 7 days remaining</summary>
    Info,

    /// <summary>Warning - 7 days or less remaining</summary>
    Warning,

    /// <summary>Critical - 3 days or less remaining</summary>
    Critical,

    /// <summary>Expired - grace period has ended</summary>
    Expired
}
