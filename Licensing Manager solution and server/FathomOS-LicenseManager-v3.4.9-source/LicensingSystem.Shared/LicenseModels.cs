// LicensingSystem.Shared/LicenseModels.cs
// Shared models between client, server, and license generator
// Updated for Fathom OS with White-Label Branding and Certificate Support

using System.Text.Json.Serialization;

namespace LicensingSystem.Shared;

/// <summary>
/// The license file structure - signed by server, verified by client
/// </summary>
public class LicenseFile
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 3; // Updated for Fathom OS

    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("product")]
    public string Product { get; set; } = LicenseConstants.ProductName;

    [JsonPropertyName("edition")]
    public string Edition { get; set; } = "Professional";

    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("issuedAt")]
    public DateTime IssuedAt { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("subscriptionType")]
    public SubscriptionType SubscriptionType { get; set; }

    [JsonPropertyName("licenseType")]
    public LicenseType LicenseType { get; set; } = LicenseType.Online;

    [JsonPropertyName("hardwareFingerprints")]
    public List<string> HardwareFingerprints { get; set; } = new();

    [JsonPropertyName("fingerprintMatchThreshold")]
    public int FingerprintMatchThreshold { get; set; } = 3;

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    // === WHITE-LABEL BRANDING FIELDS (New for Fathom OS) ===

    /// <summary>
    /// Company/brand name for white-labeling.
    /// Displayed as "Fathom OS — {Brand} Edition"
    /// Example: "Subsea7 Survey Division"
    /// </summary>
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    /// <summary>
    /// Unique 2-character licensee code (A-Z, 0-9).
    /// Used in Certificate IDs: FOS-{LicenseeCode}-2412-00001-X3B7
    /// Example: "S7" for Subsea7
    /// </summary>
    [JsonPropertyName("licenseeCode")]
    public string? LicenseeCode { get; set; }

    /// <summary>
    /// Support verification code in format SUP-XX-XXXXX.
    /// Customer provides this when contacting support.
    /// NOT displayed on certificates.
    /// Example: "SUP-S7-X8K2M"
    /// </summary>
    [JsonPropertyName("supportCode")]
    public string? SupportCode { get; set; }

    /// <summary>
    /// Base64-encoded company logo PNG (≤20KB).
    /// Format: "data:image/png;base64,iVBORw0KGgo..."
    /// Used on certificates and in UI.
    /// </summary>
    [JsonPropertyName("brandLogo")]
    public string? BrandLogo { get; set; }

    /// <summary>
    /// Optional HTTPS URL for high-resolution logo.
    /// Example: "https://server.com/logos/subsea7.png"
    /// </summary>
    [JsonPropertyName("brandLogoUrl")]
    public string? BrandLogoUrl { get; set; }

    // === HELPER PROPERTIES ===

    /// <summary>
    /// Gets the display name for this edition.
    /// Returns "Fathom OS" if no brand, or "Fathom OS — {Brand} Edition" if branded.
    /// </summary>
    [JsonIgnore]
    public string DisplayEdition => string.IsNullOrEmpty(Brand)
        ? LicenseConstants.ProductDisplayName
        : $"{LicenseConstants.ProductDisplayName} — {Brand} Edition";

    /// <summary>
    /// Gets enabled modules from Features list.
    /// Extracts module IDs from "Module:XXX" features.
    /// </summary>
    [JsonIgnore]
    public List<string> Modules => Features
        .Where(f => f.StartsWith("Module:", StringComparison.OrdinalIgnoreCase))
        .Select(f => f.Substring(7))
        .ToList();
}

/// <summary>
/// Complete license with signature
/// </summary>
public class SignedLicense
{
    [JsonPropertyName("license")]
    public LicenseFile License { get; set; } = new();

    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;
}

public enum SubscriptionType
{
    Monthly,
    Yearly,
    Lifetime
}

/// <summary>
/// Type of license - determines validation behavior
/// </summary>
public enum LicenseType
{
    /// <summary>
    /// Online license - requires periodic server validation
    /// </summary>
    Online,
    
    /// <summary>
    /// Offline license - file-based, tracked by server when online
    /// </summary>
    Offline
}

public enum LicenseStatus
{
    Valid,
    Expired,
    InvalidSignature,
    HardwareMismatch,
    Revoked,
    NotFound,
    Corrupted,
    GracePeriod,
    Error,
    NotActivated
}

/// <summary>
/// Result of license validation
/// </summary>
public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public LicenseStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public LicenseFile? License { get; set; }
    public int DaysRemaining { get; set; }
    public int GraceDaysRemaining { get; set; }
    public List<string> EnabledFeatures { get; set; } = new();
    public DateTime ServerTime { get; set; } = DateTime.MinValue;
    
    // Convenience properties that delegate to License
    public string? LicenseId => License?.LicenseId;
    public string? Edition => License?.Edition;
    public string? CustomerName => License?.CustomerName;
    public string? CustomerEmail => License?.CustomerEmail;
    public DateTime? ExpiresAt => License?.ExpiresAt;
    public string? Brand => License?.Brand;
    public string? SupportCode => License?.SupportCode;
    public List<string>? Features => License?.Features;
    public List<string>? Modules => License?.Modules;
}

/// <summary>
/// Request to activate a license (sent from client to server)
/// </summary>
public class ActivationRequest
{
    [JsonPropertyName("licenseKey")]
    public string LicenseKey { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("hardwareFingerprints")]
    public List<string> HardwareFingerprints { get; set; } = new();

    [JsonPropertyName("machineName")]
    public string? MachineName { get; set; }

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = string.Empty;

    [JsonPropertyName("osVersion")]
    public string OsVersion { get; set; } = string.Empty;
}

/// <summary>
/// Response from server after activation attempt
/// </summary>
public class ActivationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("signedLicense")]
    public SignedLicense? SignedLicense { get; set; }

    [JsonPropertyName("serverTime")]
    public DateTime ServerTime { get; set; }
}

/// <summary>
/// Request to validate/heartbeat an existing license
/// </summary>
public class ValidationRequest
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("hardwareFingerprints")]
    public List<string> HardwareFingerprints { get; set; } = new();
}

/// <summary>
/// Server validation response
/// </summary>
public class ValidationResponse
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("status")]
    public LicenseStatus Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("serverTime")]
    public DateTime ServerTime { get; set; }

    [JsonPropertyName("updatedLicense")]
    public SignedLicense? UpdatedLicense { get; set; }
    
    // Enhanced fields for revocation details
    [JsonPropertyName("revokedAt")]
    public DateTime? RevokedAt { get; set; }
    
    [JsonPropertyName("revokeReason")]
    public string? RevokeReason { get; set; }
}

// ============================================================================
// CERTIFICATE MODELS (New for Fathom OS)
// ============================================================================

/// <summary>
/// Processing certificate issued by Fathom OS modules.
/// Synced to server for verification.
/// </summary>
public class ProcessingCertificate
{
    /// <summary>
    /// Unique certificate ID in format: FOS-{LicenseeCode}-{YYMM}-{Sequence}-{Check}
    /// Example: "FOS-S7-2412-00001-X3B7"
    /// </summary>
    [JsonPropertyName("certificateId")]
    public string CertificateId { get; set; } = string.Empty;

    /// <summary>
    /// License ID that created this certificate
    /// </summary>
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>
    /// 2-letter licensee code from license
    /// </summary>
    [JsonPropertyName("licenseeCode")]
    public string LicenseeCode { get; set; } = string.Empty;

    /// <summary>
    /// Module that issued the certificate (e.g., "SurveyListing")
    /// </summary>
    [JsonPropertyName("moduleId")]
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// 3-letter module certificate code (e.g., "SLG" for Survey Listing Generator)
    /// </summary>
    [JsonPropertyName("moduleCertificateCode")]
    public string ModuleCertificateCode { get; set; } = string.Empty;

    /// <summary>
    /// Version of the module that created the certificate
    /// </summary>
    [JsonPropertyName("moduleVersion")]
    public string ModuleVersion { get; set; } = string.Empty;

    /// <summary>
    /// When the certificate was issued
    /// </summary>
    [JsonPropertyName("issuedAt")]
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Project name for the processing work
    /// </summary>
    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Project location/area
    /// </summary>
    [JsonPropertyName("projectLocation")]
    public string? ProjectLocation { get; set; }

    /// <summary>
    /// Vessel name (if applicable)
    /// </summary>
    [JsonPropertyName("vessel")]
    public string? Vessel { get; set; }

    /// <summary>
    /// Client name
    /// </summary>
    [JsonPropertyName("client")]
    public string? Client { get; set; }

    /// <summary>
    /// Name of person who signed/approved the certificate
    /// </summary>
    [JsonPropertyName("signatoryName")]
    public string SignatoryName { get; set; } = string.Empty;

    /// <summary>
    /// Title of signatory
    /// </summary>
    [JsonPropertyName("signatoryTitle")]
    public string? SignatoryTitle { get; set; }

    /// <summary>
    /// Company name (from license Brand)
    /// </summary>
    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Module-specific processing data (flexible key-value pairs).
    /// Each module defines their own content.
    /// Example for Survey Listing: {"Total Survey Points": "15,234", "KP Range": "0.000 km — 45.678 km"}
    /// </summary>
    [JsonPropertyName("processingData")]
    public Dictionary<string, string> ProcessingData { get; set; } = new();

    /// <summary>
    /// List of input files processed
    /// </summary>
    [JsonPropertyName("inputFiles")]
    public List<string> InputFiles { get; set; } = new();

    /// <summary>
    /// List of output files generated
    /// </summary>
    [JsonPropertyName("outputFiles")]
    public List<string> OutputFiles { get; set; } = new();

    /// <summary>
    /// ECDSA signature of the certificate data
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm used for signature (e.g., "SHA256withECDSA")
    /// </summary>
    [JsonPropertyName("signatureAlgorithm")]
    public string SignatureAlgorithm { get; set; } = "SHA256withECDSA";
}

/// <summary>
/// Request to sync certificates from client to server
/// </summary>
public class CertificateSyncRequest
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("certificates")]
    public List<ProcessingCertificate> Certificates { get; set; } = new();
}

/// <summary>
/// Response from certificate sync
/// </summary>
public class CertificateSyncResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("syncedCount")]
    public int SyncedCount { get; set; }

    [JsonPropertyName("failedIds")]
    public List<string> FailedIds { get; set; } = new();
}

/// <summary>
/// Public certificate verification result
/// </summary>
public class CertificateVerificationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("certificateId")]
    public string CertificateId { get; set; } = string.Empty;

    [JsonPropertyName("issuedAt")]
    public DateTime? IssuedAt { get; set; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("moduleId")]
    public string? ModuleId { get; set; }

    [JsonPropertyName("moduleName")]
    public string? ModuleName { get; set; }

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("isSignatureVerified")]
    public bool IsSignatureVerified { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Request to register a new module
/// </summary>
public class ModuleRegistrationRequest
{
    [JsonPropertyName("moduleId")]
    public string ModuleId { get; set; } = string.Empty;

    [JsonPropertyName("certificateCode")]
    public string CertificateCode { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Response from module registration
/// </summary>
public class ModuleRegistrationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("moduleId")]
    public string? ModuleId { get; set; }

    [JsonPropertyName("certificateCode")]
    public string? CertificateCode { get; set; }
}

/// <summary>
/// Constants used across the system
/// </summary>
public static class LicenseConstants
{
    // === PRODUCT NAMING (Updated for Fathom OS) ===
    public const string ProductName = "FathomOS";
    public const string ProductDisplayName = "Fathom OS";
    public const string CertificateIdPrefix = "FOS";
    
    // === CERTIFICATE PUBLIC KEY (for verification) ===
    /// <summary>
    /// ECDSA P-256 public key for verifying certificate signatures.
    /// This key is safe to distribute with client software.
    /// </summary>
    public const string CertificatePublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEFDbwi1nxZlWrSm5SqD8NFu6UprcV
jtY70iOte7R8umJSER7OmqYt/6xAkL2Y+7GXk1X79SRRlfF4S2GIZZpNvQ==
-----END PUBLIC KEY-----";
    
    // === LICENSE SETTINGS ===
    public const int GracePeriodDays = 14;
    public const int OfflineMaxDays = 30;
    public const int FingerprintMatchThreshold = 3;
    public const int TotalFingerprintComponents = 5;
    
    // === WHITE-LABEL SETTINGS ===
    public const int LicenseeCodeLength = 2;
    public const int SupportCodeRandomLength = 5;
    public const int MaxLogoSizeBytes = 20 * 1024; // 20KB
    
    // === CERTIFICATE SETTINGS ===
    public const int CertificateRetentionYears = 5;
    
    /// <summary>
    /// Generates a Support Code in format SUP-XX-XXXXX
    /// </summary>
    public static string GenerateSupportCode(string licenseeCode)
    {
        var random = new Random();
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed confusing chars (0, O, I, 1)
        var randomPart = new string(Enumerable.Range(0, SupportCodeRandomLength)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"SUP-{licenseeCode}-{randomPart}";
    }
    
    /// <summary>
    /// Validates a Licensee Code (2 characters: uppercase letters A-Z or numbers 0-9)
    /// </summary>
    public static bool IsValidLicenseeCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != LicenseeCodeLength)
            return false;
        return code.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'));
    }
    
    /// <summary>
    /// Generates a Certificate ID in format FOS-XX-YYMM-NNNNN-CCCC
    /// </summary>
    public static string GenerateCertificateId(string licenseeCode, int sequenceNumber, DateTime issueDate)
    {
        var datePart = issueDate.ToString("yyMM");
        var seqPart = sequenceNumber.ToString("D5");
        var checkPart = GenerateCheckCode($"{licenseeCode}{datePart}{seqPart}");
        return $"{CertificateIdPrefix}-{licenseeCode}-{datePart}-{seqPart}-{checkPart}";
    }
    
    /// <summary>
    /// Generates a 4-character check code for certificate ID
    /// </summary>
    private static string GenerateCheckCode(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(hash.Take(4).Select(b => chars[b % chars.Length]).ToArray());
    }
}

/// <summary>
/// Result of checking a licensee code availability
/// </summary>
public class LicenseeCodeCheckResult
{
    public bool IsValid { get; set; } = true;
    public bool IsAvailable { get; set; }
    public string? Code { get; set; }
    public string? UsedBy { get; set; }
    public string Message { get; set; } = string.Empty;
}
