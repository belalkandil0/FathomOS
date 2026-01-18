// LicensingSystem.Shared/Services/LicenseSigner.cs
// ECDSA P-256 license signing for License Generator UI
// This file should be used ONLY in the License Generator application (has private key)

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LicensingSystem.Shared.Models;

namespace LicensingSystem.Shared.Services;

/// <summary>
/// Signs offline licenses using ECDSA P-256 digital signatures.
///
/// SECURITY: This class requires access to the private key and should ONLY be used
/// in the License Generator UI application. NEVER distribute the private key with
/// client applications.
///
/// Usage:
/// 1. Generate a key pair once: LicenseSigner.GenerateKeyPair()
/// 2. Store private key securely (DPAPI, HSM, or secure config)
/// 3. Embed public key in client applications
/// 4. Use Sign() or CreateSignedLicense() to sign licenses
/// </summary>
/// <example>
/// // Unit Test Example - Generate Key Pair:
/// var (privateKey, publicKey) = LicenseSigner.GenerateKeyPair();
/// Assert.StartsWith("-----BEGIN EC PRIVATE KEY-----", privateKey);
/// Assert.StartsWith("-----BEGIN PUBLIC KEY-----", publicKey);
///
/// // Unit Test Example - Sign License:
/// var signer = new LicenseSigner();
/// var license = new OfflineLicense { Id = "LIC-2026-0001" };
/// var signature = signer.Sign(license, privateKey);
/// Assert.False(string.IsNullOrEmpty(signature));
/// Assert.True(signature.Length > 50); // Base64 signature
/// </example>
public class LicenseSigner
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new LicenseSigner instance
    /// </summary>
    public LicenseSigner()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Generates a new ECDSA P-256 key pair for license signing.
    /// Call this ONCE during initial setup and store the keys securely.
    ///
    /// SECURITY NOTES:
    /// - Private key must be stored securely (use DPAPI, HSM, or encrypted config)
    /// - Public key should be embedded in client applications
    /// - NEVER distribute the private key
    /// </summary>
    /// <returns>Tuple of (privateKeyPem, publicKeyPem)</returns>
    /// <example>
    /// var (privateKey, publicKey) = LicenseSigner.GenerateKeyPair();
    /// // Store privateKey in secure config (DPAPI-protected)
    /// // Embed publicKey in LicenseVerifier for client apps
    /// </example>
    public static (string privateKey, string publicKey) GenerateKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Export private key in PEM format
        var privateKeyBytes = ecdsa.ExportECPrivateKey();
        var privateKeyPem = ExportPrivateKeyToPem(privateKeyBytes);

        // Export public key in PEM format
        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        var publicKeyPem = ExportPublicKeyToPem(publicKeyBytes);

        return (privateKeyPem, publicKeyPem);
    }

    /// <summary>
    /// Generates a key pair and returns a formatted string for display/storage
    /// </summary>
    /// <returns>Formatted key pair info with timestamp</returns>
    public static string GenerateKeyPairFormatted()
    {
        var (privateKey, publicKey) = GenerateKeyPair();
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        return $"""
            ========================================
            FATHOM OS LICENSE SIGNING KEY PAIR
            Generated: {timestamp}
            ========================================

            === PRIVATE KEY (KEEP SECRET - License Generator Only) ===
            {privateKey}

            === PUBLIC KEY (Embed in Client Applications) ===
            {publicKey}

            ========================================
            IMPORTANT:
            - Store private key in DPAPI-protected storage
            - Embed public key in LicenseVerifier
            - NEVER distribute private key
            ========================================
            """;
    }

    /// <summary>
    /// Signs an offline license using ECDSA P-256.
    /// The signature is computed over the canonical JSON representation
    /// of all license fields EXCEPT the Signature field.
    /// </summary>
    /// <param name="license">The license to sign (Signature field will be ignored)</param>
    /// <param name="privateKeyPem">The ECDSA private key in PEM format</param>
    /// <returns>Base64-encoded signature string</returns>
    /// <exception cref="ArgumentNullException">If license or privateKey is null</exception>
    /// <exception cref="CryptographicException">If signing fails</exception>
    public string Sign(OfflineLicense license, string privateKeyPem)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));
        if (string.IsNullOrEmpty(privateKeyPem))
            throw new ArgumentNullException(nameof(privateKeyPem));

        // Clone license without signature for canonical representation
        var licenseToSign = license.CloneWithoutSignature();

        // Serialize to canonical JSON (no indentation, sorted properties)
        var licenseJson = JsonSerializer.Serialize(licenseToSign, _jsonOptions);
        var dataBytes = Encoding.UTF8.GetBytes(licenseJson);

        // Sign with ECDSA P-256
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        var signatureBytes = ecdsa.SignData(dataBytes, HashAlgorithmName.SHA256);
        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// Creates a complete signed offline license from a creation request.
    /// Generates license ID, support code, and signs the license.
    /// </summary>
    /// <param name="request">License creation request with all details</param>
    /// <param name="privateKeyPem">The ECDSA private key in PEM format</param>
    /// <param name="publicKeyId">Optional public key ID for key rotation (e.g., "KEY-20260117")</param>
    /// <returns>A complete, signed OfflineLicense ready for distribution</returns>
    public OfflineLicense CreateSignedLicense(
        OfflineLicenseCreationRequest request,
        string privateKeyPem,
        string? publicKeyId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrEmpty(privateKeyPem))
            throw new ArgumentNullException(nameof(privateKeyPem));

        // Generate license ID
        var licenseId = GenerateLicenseId();

        // Generate support code if licensee code provided
        string? supportCode = null;
        if (!string.IsNullOrEmpty(request.LicenseeCode))
        {
            supportCode = GenerateSupportCode(request.LicenseeCode);
        }

        // Build the license object
        var license = new OfflineLicense
        {
            Id = licenseId,
            Version = OfflineLicenseConstants.CurrentVersion,
            Client = request.Client,
            Product = request.Product,
            Terms = request.Terms,
            Binding = new LicenseBinding
            {
                HardwareFingerprints = request.HardwareFingerprints,
                MatchThreshold = request.MatchThreshold > 0
                    ? request.MatchThreshold
                    : OfflineLicenseConstants.DefaultMatchThreshold
            },
            Modules = request.Modules ?? new List<string>(),
            Features = request.Features ?? new List<string>(),
            Metadata = request.Metadata ?? new Dictionary<string, string>(),
            PublicKeyId = publicKeyId ?? $"KEY-{DateTime.UtcNow:yyyyMMdd}",
            Brand = request.Brand,
            LicenseeCode = request.LicenseeCode,
            SupportCode = supportCode
        };

        // Set issued date if not already set
        if (license.Terms.IssuedAt == default)
        {
            license.Terms.IssuedAt = DateTime.UtcNow;
        }

        // Sign the license
        license.Signature = Sign(license, privateKeyPem);

        return license;
    }

    /// <summary>
    /// Creates a signed license from an existing license object.
    /// Useful for re-signing after modifications.
    /// </summary>
    /// <param name="license">The license to sign</param>
    /// <param name="privateKeyPem">The ECDSA private key in PEM format</param>
    /// <returns>The same license object with updated signature</returns>
    public OfflineLicense SignExistingLicense(OfflineLicense license, string privateKeyPem)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));

        license.Signature = Sign(license, privateKeyPem);
        return license;
    }

    /// <summary>
    /// Verifies that a private key is valid and can be used for signing.
    /// </summary>
    /// <param name="privateKeyPem">The private key to verify</param>
    /// <returns>True if the key is valid for ECDSA P-256</returns>
    public static bool VerifyPrivateKey(string privateKeyPem)
    {
        if (string.IsNullOrEmpty(privateKeyPem))
            return false;

        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(privateKeyPem);

            // Verify it's P-256
            var parameters = ecdsa.ExportParameters(false);
            return parameters.Curve.Oid?.FriendlyName == "nistP256" ||
                   parameters.Curve.Oid?.Value == "1.2.840.10045.3.1.7";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the public key from a private key.
    /// </summary>
    /// <param name="privateKeyPem">The private key in PEM format</param>
    /// <returns>The corresponding public key in PEM format</returns>
    public static string ExtractPublicKey(string privateKeyPem)
    {
        if (string.IsNullOrEmpty(privateKeyPem))
            throw new ArgumentNullException(nameof(privateKeyPem));

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);

        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        return ExportPublicKeyToPem(publicKeyBytes);
    }

    /// <summary>
    /// Generates a unique license ID in format LIC-YYYY-NNNN
    /// </summary>
    private static string GenerateLicenseId()
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random();
        var sequence = random.Next(1, 9999);
        return $"LIC-{year}-{sequence:D4}";
    }

    /// <summary>
    /// Generates a unique license ID with custom prefix
    /// </summary>
    /// <param name="prefix">Custom prefix (e.g., "ENT" for Enterprise)</param>
    public static string GenerateLicenseId(string prefix)
    {
        var year = DateTime.UtcNow.Year;
        var random = new Random();
        var sequence = random.Next(1, 9999);
        return $"{prefix}-{year}-{sequence:D4}";
    }

    /// <summary>
    /// Generates a support code in format SUP-XX-XXXXX
    /// </summary>
    /// <param name="licenseeCode">2-character licensee code</param>
    private static string GenerateSupportCode(string licenseeCode)
    {
        var random = new Random();
        var chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed confusing chars
        var randomPart = new string(Enumerable.Range(0, 5)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"SUP-{licenseeCode.ToUpperInvariant()}-{randomPart}";
    }

    /// <summary>
    /// Exports an EC private key to PEM format
    /// </summary>
    private static string ExportPrivateKeyToPem(byte[] privateKeyBytes)
    {
        var base64 = Convert.ToBase64String(privateKeyBytes);
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN EC PRIVATE KEY-----");

        // Split into 64-character lines
        for (int i = 0; i < base64.Length; i += 64)
        {
            var length = Math.Min(64, base64.Length - i);
            sb.AppendLine(base64.Substring(i, length));
        }

        sb.Append("-----END EC PRIVATE KEY-----");
        return sb.ToString();
    }

    /// <summary>
    /// Exports a public key to PEM format
    /// </summary>
    private static string ExportPublicKeyToPem(byte[] publicKeyBytes)
    {
        var base64 = Convert.ToBase64String(publicKeyBytes);
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN PUBLIC KEY-----");

        // Split into 64-character lines
        for (int i = 0; i < base64.Length; i += 64)
        {
            var length = Math.Min(64, base64.Length - i);
            sb.AppendLine(base64.Substring(i, length));
        }

        sb.Append("-----END PUBLIC KEY-----");
        return sb.ToString();
    }
}

/// <summary>
/// Secure storage helper for private keys using Windows DPAPI.
/// Use this to safely store the signing private key on the License Generator machine.
/// </summary>
public static class PrivateKeyStorage
{
    /// <summary>
    /// Protects a private key using Windows DPAPI.
    /// The key can only be decrypted by the same user on the same machine.
    /// </summary>
    /// <param name="privateKeyPem">The private key to protect</param>
    /// <returns>Base64-encoded protected data</returns>
    public static string ProtectPrivateKey(string privateKeyPem)
    {
        if (string.IsNullOrEmpty(privateKeyPem))
            throw new ArgumentNullException(nameof(privateKeyPem));

        var keyBytes = Encoding.UTF8.GetBytes(privateKeyPem);
        var entropy = GetEntropy();

        var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(
            keyBytes,
            entropy,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(protectedBytes);
    }

    /// <summary>
    /// Unprotects a private key that was protected with ProtectPrivateKey.
    /// </summary>
    /// <param name="protectedKey">The Base64-encoded protected key</param>
    /// <returns>The original private key PEM string</returns>
    public static string UnprotectPrivateKey(string protectedKey)
    {
        if (string.IsNullOrEmpty(protectedKey))
            throw new ArgumentNullException(nameof(protectedKey));

        var protectedBytes = Convert.FromBase64String(protectedKey);
        var entropy = GetEntropy();

        var keyBytes = System.Security.Cryptography.ProtectedData.Unprotect(
            protectedBytes,
            entropy,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(keyBytes);
    }

    /// <summary>
    /// Saves a protected private key to a file.
    /// </summary>
    /// <param name="privateKeyPem">The private key to save</param>
    /// <param name="filePath">Path to save the protected key</param>
    public static void SaveProtectedKey(string privateKeyPem, string filePath)
    {
        var protectedKey = ProtectPrivateKey(privateKeyPem);
        File.WriteAllText(filePath, protectedKey);
    }

    /// <summary>
    /// Loads and unprotects a private key from a file.
    /// </summary>
    /// <param name="filePath">Path to the protected key file</param>
    /// <returns>The unprotected private key PEM string</returns>
    public static string LoadProtectedKey(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Protected key file not found", filePath);

        var protectedKey = File.ReadAllText(filePath);
        return UnprotectPrivateKey(protectedKey);
    }

    /// <summary>
    /// Gets entropy bytes for DPAPI operations.
    /// This adds an extra layer of security to the DPAPI protection.
    /// </summary>
    private static byte[] GetEntropy()
    {
        // Application-specific entropy
        // This should be kept constant for your application
        return new byte[]
        {
            0x46, 0x41, 0x54, 0x48, 0x4F, 0x4D, 0x4F, 0x53,  // "FATHOMOS"
            0x4C, 0x49, 0x43, 0x45, 0x4E, 0x53, 0x45, 0x21   // "LICENSE!"
        };
    }
}
