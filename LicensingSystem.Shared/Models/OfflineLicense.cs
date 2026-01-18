// LicensingSystem.Shared/Models/OfflineLicense.cs
// Complete offline license model with ECDSA signature support
// For FathomOS offline license validation system

using System.Text.Json.Serialization;

namespace LicensingSystem.Shared.Models;

/// <summary>
/// Complete offline license file structure.
/// Contains all license data and is signed using ECDSA P-256.
///
/// Usage:
/// - Created by License Generator UI using LicenseSigner
/// - Validated by FathomOS app using LicenseVerifier
/// - Stored as JSON (.lic file) or compact license key string
/// </summary>
/// <example>
/// // Unit Test Example:
/// var license = new OfflineLicense
/// {
///     Id = "LIC-2026-0001",
///     Client = new LicenseClient { Name = "Acme Corp", Email = "admin@acme.com" },
///     Product = new LicenseProduct { Name = "FathomOS", Edition = "Professional" },
///     Terms = new LicenseTerms { IssuedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddYears(1) },
///     Modules = new List&lt;string&gt; { "SurveyListing", "TideAnalysis" }
/// };
/// Assert.Equal("LIC-2026-0001", license.Id);
/// </example>
public class OfflineLicense
{
    /// <summary>
    /// Unique license identifier in format LIC-YYYY-NNNN
    /// Example: "LIC-2026-0001"
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// License format version for future compatibility.
    /// Current version: "1.0"
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Client/customer information
    /// </summary>
    [JsonPropertyName("client")]
    public LicenseClient Client { get; set; } = new();

    /// <summary>
    /// Product information being licensed
    /// </summary>
    [JsonPropertyName("product")]
    public LicenseProduct Product { get; set; } = new();

    /// <summary>
    /// License terms including dates and type
    /// </summary>
    [JsonPropertyName("terms")]
    public LicenseTerms Terms { get; set; } = new();

    /// <summary>
    /// Hardware binding information for machine locking
    /// </summary>
    [JsonPropertyName("binding")]
    public LicenseBinding Binding { get; set; } = new();

    /// <summary>
    /// List of licensed module IDs.
    /// Example: ["SurveyListing", "TideAnalysis", "UsblVerification"]
    /// </summary>
    [JsonPropertyName("modules")]
    public List<string> Modules { get; set; } = new();

    /// <summary>
    /// List of enabled feature flags.
    /// Example: ["Export:PDF", "Export:DXF", "AdvancedCharts"]
    /// </summary>
    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Custom metadata key-value pairs.
    /// Use for organization-specific data.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Base64-encoded ECDSA P-256 signature of the license data.
    /// Computed over the canonical JSON representation of all fields except Signature.
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Public key identifier for key rotation support.
    /// Format: "KEY-YYYYMMDD" (e.g., "KEY-20260117")
    /// </summary>
    [JsonPropertyName("publicKeyId")]
    public string PublicKeyId { get; set; } = string.Empty;

    // === WHITE-LABEL BRANDING FIELDS ===

    /// <summary>
    /// Company/brand name for white-labeling.
    /// Displayed as "Fathom OS - {Brand} Edition"
    /// Example: "Subsea7 Survey Division"
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// Unique 2-character licensee code (A-Z, 0-9).
    /// Used in Certificate IDs: FOS-{LicenseeCode}-YYMM-NNNNN-CCCC
    /// Example: "S7" for Subsea7
    /// </summary>
    [JsonPropertyName("licenseeCode")]
    public string? LicenseeCode { get; set; }

    /// <summary>
    /// Support verification code in format SUP-XX-XXXXX.
    /// Customer provides this when contacting support.
    /// Example: "SUP-S7-X8K2M"
    /// </summary>
    [JsonPropertyName("supportCode")]
    public string? SupportCode { get; set; }

    /// <summary>
    /// Base64-encoded company logo PNG (max 20KB).
    /// Format: "data:image/png;base64,iVBORw0KGgo..."
    /// </summary>
    [JsonPropertyName("brandLogo")]
    public string? BrandLogo { get; set; }

    // === HELPER PROPERTIES ===

    /// <summary>
    /// Gets the display edition formatted as:
    /// - "Fathom OS" if no brand
    /// - "Fathom OS - {Brand} Edition" if branded
    /// </summary>
    [JsonIgnore]
    public string DisplayEdition => string.IsNullOrEmpty(Brand)
        ? Product.Name
        : $"{Product.Name} - {Brand} Edition";

    /// <summary>
    /// Checks if this is a lifetime license (no expiration).
    /// </summary>
    [JsonIgnore]
    public bool IsLifetime => Terms.SubscriptionType == OfflineSubscriptionType.Lifetime;

    /// <summary>
    /// Checks if the license has expired (based on current UTC time).
    /// Returns false for lifetime licenses.
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => !IsLifetime && Terms.ExpiresAt < DateTime.UtcNow;

    /// <summary>
    /// Gets the number of days until expiration.
    /// Returns int.MaxValue for lifetime licenses.
    /// Returns negative value if expired.
    /// </summary>
    [JsonIgnore]
    public int DaysUntilExpiry => IsLifetime
        ? int.MaxValue
        : (int)(Terms.ExpiresAt - DateTime.UtcNow).TotalDays;

    /// <summary>
    /// Creates a clone of this license without the signature.
    /// Used for signature computation.
    /// </summary>
    public OfflineLicense CloneWithoutSignature()
    {
        return new OfflineLicense
        {
            Id = Id,
            Version = Version,
            Client = Client,
            Product = Product,
            Terms = Terms,
            Binding = Binding,
            Modules = new List<string>(Modules),
            Features = new List<string>(Features),
            Metadata = new Dictionary<string, string>(Metadata),
            Signature = string.Empty, // Clear signature
            PublicKeyId = PublicKeyId,
            Brand = Brand,
            LicenseeCode = LicenseeCode,
            SupportCode = SupportCode,
            BrandLogo = BrandLogo
        };
    }
}

/// <summary>
/// Client/customer information for the license
/// </summary>
public class LicenseClient
{
    /// <summary>
    /// Customer or company name.
    /// Example: "Acme Offshore Survey Ltd"
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Primary contact email address.
    /// Used for license delivery and support.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Company registration or VAT number (optional).
    /// </summary>
    [JsonPropertyName("companyId")]
    public string? CompanyId { get; set; }

    /// <summary>
    /// Physical address (optional).
    /// </summary>
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    /// <summary>
    /// Contact phone number (optional).
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Internal customer reference number (optional).
    /// </summary>
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }
}

/// <summary>
/// Product information for the license
/// </summary>
public class LicenseProduct
{
    /// <summary>
    /// Product name. Must match the product being licensed.
    /// Example: "FathomOS"
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "FathomOS";

    /// <summary>
    /// Product edition/tier.
    /// Example: "Professional", "Enterprise", "Basic"
    /// </summary>
    [JsonPropertyName("edition")]
    public string Edition { get; set; } = "Professional";

    /// <summary>
    /// Major version this license is valid for.
    /// Example: "1.x" means valid for 1.0, 1.1, 1.9 but not 2.0
    /// Use "*" for all versions.
    /// </summary>
    [JsonPropertyName("majorVersion")]
    public string MajorVersion { get; set; } = "*";

    /// <summary>
    /// Number of seats/activations allowed.
    /// Default 1 for single-machine license.
    /// </summary>
    [JsonPropertyName("seats")]
    public int Seats { get; set; } = 1;
}

/// <summary>
/// License terms including dates and subscription type
/// </summary>
public class LicenseTerms
{
    /// <summary>
    /// When the license was issued (UTC).
    /// </summary>
    [JsonPropertyName("issuedAt")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the license expires (UTC).
    /// Ignored for Lifetime licenses.
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddYears(1);

    /// <summary>
    /// When the license was first activated (UTC).
    /// Set by the client upon first validation.
    /// </summary>
    [JsonPropertyName("activatedAt")]
    public DateTime? ActivatedAt { get; set; }

    /// <summary>
    /// Type of subscription/license.
    /// </summary>
    [JsonPropertyName("subscriptionType")]
    public OfflineSubscriptionType SubscriptionType { get; set; } = OfflineSubscriptionType.Yearly;

    /// <summary>
    /// Number of grace period days after expiration.
    /// During grace period, software works but shows warnings.
    /// Default: 14 days
    /// </summary>
    [JsonPropertyName("gracePeriodDays")]
    public int GracePeriodDays { get; set; } = 14;

    /// <summary>
    /// Maximum days the license can operate offline without server validation.
    /// Only applicable for hybrid licenses. Default: 30 days
    /// </summary>
    [JsonPropertyName("offlineMaxDays")]
    public int OfflineMaxDays { get; set; } = 30;

    /// <summary>
    /// Whether this license allows offline operation.
    /// </summary>
    [JsonPropertyName("allowOffline")]
    public bool AllowOffline { get; set; } = true;
}

/// <summary>
/// Hardware binding information for machine-locking
/// </summary>
public class LicenseBinding
{
    /// <summary>
    /// List of hardware fingerprint hashes (SHA-256, 32 chars each).
    /// Each fingerprint represents a hardware component.
    /// Example: CPU, Motherboard, BIOS, Drive, MachineGuid
    /// </summary>
    [JsonPropertyName("hardwareFingerprints")]
    public List<string> HardwareFingerprints { get; set; } = new();

    /// <summary>
    /// Minimum number of fingerprints that must match.
    /// Allows tolerance for minor hardware changes (RAM, GPU).
    /// Default: 3 of 5 must match
    /// </summary>
    [JsonPropertyName("matchThreshold")]
    public int MatchThreshold { get; set; } = 3;

    /// <summary>
    /// Windows Machine GUID (optional, for display).
    /// </summary>
    [JsonPropertyName("machineGuid")]
    public string? MachineGuid { get; set; }

    /// <summary>
    /// Machine name at time of license generation (for reference).
    /// </summary>
    [JsonPropertyName("machineName")]
    public string? MachineName { get; set; }

    /// <summary>
    /// Whether to enforce strict hardware binding.
    /// If false, allows some hardware flexibility.
    /// </summary>
    [JsonPropertyName("strictBinding")]
    public bool StrictBinding { get; set; } = false;
}

/// <summary>
/// Subscription type for offline licenses
/// </summary>
public enum OfflineSubscriptionType
{
    /// <summary>
    /// Trial license (typically 14-30 days)
    /// </summary>
    Trial,

    /// <summary>
    /// Monthly subscription
    /// </summary>
    Monthly,

    /// <summary>
    /// Yearly subscription
    /// </summary>
    Yearly,

    /// <summary>
    /// Perpetual/lifetime license (never expires)
    /// </summary>
    Lifetime,

    /// <summary>
    /// Project-based license (specific date range)
    /// </summary>
    Project
}

/// <summary>
/// Request object for creating a new offline license
/// </summary>
public class OfflineLicenseCreationRequest
{
    /// <summary>
    /// Client information
    /// </summary>
    public LicenseClient Client { get; set; } = new();

    /// <summary>
    /// Product configuration
    /// </summary>
    public LicenseProduct Product { get; set; } = new();

    /// <summary>
    /// License terms
    /// </summary>
    public LicenseTerms Terms { get; set; } = new();

    /// <summary>
    /// Hardware fingerprints for binding
    /// </summary>
    public List<string> HardwareFingerprints { get; set; } = new();

    /// <summary>
    /// Fingerprint match threshold
    /// </summary>
    public int MatchThreshold { get; set; } = 3;

    /// <summary>
    /// List of module IDs to license
    /// </summary>
    public List<string> Modules { get; set; } = new();

    /// <summary>
    /// List of features to enable
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Optional brand name for white-labeling
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Optional licensee code (2 chars)
    /// </summary>
    public string? LicenseeCode { get; set; }

    /// <summary>
    /// Optional custom metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Result of offline license validation
/// </summary>
public class OfflineLicenseValidationResult
{
    /// <summary>
    /// Whether the license is valid and usable
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Detailed error message if not valid
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Validation status code
    /// </summary>
    public OfflineLicenseStatus Status { get; set; } = OfflineLicenseStatus.Unknown;

    /// <summary>
    /// Whether the license has expired
    /// </summary>
    public bool IsExpired { get; set; }

    /// <summary>
    /// Whether currently in grace period (expired but still usable)
    /// </summary>
    public bool IsInGracePeriod { get; set; }

    /// <summary>
    /// Days remaining until expiration (negative if expired)
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// Days remaining in grace period (0 if not in grace period)
    /// </summary>
    public int GraceDaysRemaining { get; set; }

    /// <summary>
    /// List of licensed modules (empty if invalid)
    /// </summary>
    public List<string> LicensedModules { get; set; } = new();

    /// <summary>
    /// List of enabled features (empty if invalid)
    /// </summary>
    public List<string> EnabledFeatures { get; set; } = new();

    /// <summary>
    /// The validated license (null if invalid signature)
    /// </summary>
    public OfflineLicense? License { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static OfflineLicenseValidationResult Success(OfflineLicense license)
    {
        var daysUntilExpiry = license.DaysUntilExpiry;
        var isExpired = license.IsExpired;
        var graceDaysRemaining = isExpired
            ? Math.Max(0, license.Terms.GracePeriodDays + daysUntilExpiry)
            : 0;
        var isInGracePeriod = isExpired && graceDaysRemaining > 0;

        return new OfflineLicenseValidationResult
        {
            IsValid = !isExpired || isInGracePeriod,
            Status = isExpired
                ? (isInGracePeriod ? OfflineLicenseStatus.GracePeriod : OfflineLicenseStatus.Expired)
                : OfflineLicenseStatus.Valid,
            IsExpired = isExpired,
            IsInGracePeriod = isInGracePeriod,
            DaysUntilExpiry = daysUntilExpiry,
            GraceDaysRemaining = graceDaysRemaining,
            LicensedModules = license.Modules,
            EnabledFeatures = license.Features,
            License = license
        };
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static OfflineLicenseValidationResult Failure(string error, OfflineLicenseStatus status)
    {
        return new OfflineLicenseValidationResult
        {
            IsValid = false,
            Error = error,
            Status = status
        };
    }
}

/// <summary>
/// Status codes for offline license validation
/// </summary>
public enum OfflineLicenseStatus
{
    /// <summary>
    /// Unknown status
    /// </summary>
    Unknown,

    /// <summary>
    /// License is valid and active
    /// </summary>
    Valid,

    /// <summary>
    /// License has expired
    /// </summary>
    Expired,

    /// <summary>
    /// License expired but in grace period
    /// </summary>
    GracePeriod,

    /// <summary>
    /// Digital signature is invalid (tampered or corrupted)
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// Hardware fingerprint does not match
    /// </summary>
    HardwareMismatch,

    /// <summary>
    /// Product name does not match
    /// </summary>
    ProductMismatch,

    /// <summary>
    /// License file not found
    /// </summary>
    NotFound,

    /// <summary>
    /// License file is corrupted or malformed
    /// </summary>
    Corrupted,

    /// <summary>
    /// License has been revoked
    /// </summary>
    Revoked,

    /// <summary>
    /// License version not supported
    /// </summary>
    VersionNotSupported
}

/// <summary>
/// Constants for offline license system
/// </summary>
public static class OfflineLicenseConstants
{
    /// <summary>
    /// Current license format version
    /// </summary>
    public const string CurrentVersion = "1.0";

    /// <summary>
    /// Default grace period in days
    /// </summary>
    public const int DefaultGracePeriodDays = 14;

    /// <summary>
    /// Default fingerprint match threshold
    /// </summary>
    public const int DefaultMatchThreshold = 3;

    /// <summary>
    /// Total fingerprint components collected
    /// </summary>
    public const int TotalFingerprintComponents = 5;

    /// <summary>
    /// File extension for license files
    /// </summary>
    public const string LicenseFileExtension = ".lic";

    /// <summary>
    /// License key prefix
    /// </summary>
    public const string LicenseKeyPrefix = "FATHOM";

    /// <summary>
    /// Maximum logo size in bytes (20KB)
    /// </summary>
    public const int MaxLogoSizeBytes = 20 * 1024;

    /// <summary>
    /// Licensee code length (2 characters)
    /// </summary>
    public const int LicenseeCodeLength = 2;
}
