// LicensingSystem.Shared/Services/LicenseVerifier.cs
// ECDSA P-256 license verification for FathomOS client application
// This file should be included in client applications (has public key only)

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LicensingSystem.Shared.Models;

namespace LicensingSystem.Shared.Services;

/// <summary>
/// Verifies offline licenses using ECDSA P-256 digital signature verification.
///
/// SECURITY: This class only uses the public key for verification.
/// Safe to distribute with client applications.
///
/// Usage:
/// 1. Create verifier with embedded public key
/// 2. Call VerifySignature() to check cryptographic validity
/// 3. Call Validate() for full validation (signature + expiry + binding)
/// </summary>
/// <example>
/// // Unit Test Example - Basic Verification:
/// var verifier = new LicenseVerifier(publicKeyPem);
/// bool isValid = verifier.VerifySignature(license);
/// Assert.True(isValid);
///
/// // Unit Test Example - Full Validation:
/// var result = verifier.Validate(license, machineFingerprints);
/// Assert.True(result.IsValid);
/// Assert.Contains("SurveyListing", result.LicensedModules);
/// </example>
public class LicenseVerifier
{
    private readonly string _publicKeyPem;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Default FathomOS public key for license verification.
    /// Generated December 2024 - Update when rotating keys.
    /// </summary>
    public const string DefaultPublicKey = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEqfPevrauRvxoUEd3aCRgwUrare3n
Z/6HvjBOpRV0JuJDmGFyV/cNVZuf5wg2PvogWwN/mSZFB27S2PycJcoEew==
-----END PUBLIC KEY-----";

    /// <summary>
    /// Creates a new LicenseVerifier with the specified public key.
    /// </summary>
    /// <param name="publicKeyPem">ECDSA P-256 public key in PEM format. If null, uses default key.</param>
    public LicenseVerifier(string? publicKeyPem = null)
    {
        _publicKeyPem = string.IsNullOrEmpty(publicKeyPem) ? DefaultPublicKey : publicKeyPem;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Validate the public key on construction
        ValidatePublicKey();
    }

    /// <summary>
    /// Verifies that the license signature is cryptographically valid.
    /// This only checks the signature - does not validate expiry or hardware.
    /// </summary>
    /// <param name="license">The license to verify</param>
    /// <returns>True if the signature is valid, false otherwise</returns>
    /// <example>
    /// var verifier = new LicenseVerifier(publicKey);
    /// if (verifier.VerifySignature(license))
    /// {
    ///     Console.WriteLine("License signature is valid");
    /// }
    /// </example>
    public bool VerifySignature(OfflineLicense license)
    {
        if (license == null)
            return false;

        if (string.IsNullOrEmpty(license.Signature))
            return false;

        try
        {
            // Get the license without signature for verification
            var licenseToVerify = license.CloneWithoutSignature();

            // Serialize to canonical JSON (must match signer exactly)
            var licenseJson = JsonSerializer.Serialize(licenseToVerify, _jsonOptions);
            var dataBytes = Encoding.UTF8.GetBytes(licenseJson);
            var signatureBytes = Convert.FromBase64String(license.Signature);

            // Verify with ECDSA P-256
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_publicKeyPem);

            return ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Signature verification error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Performs full license validation including signature, expiry, and optional hardware binding.
    /// </summary>
    /// <param name="license">The license to validate</param>
    /// <param name="currentFingerprints">Optional list of current machine fingerprints for hardware validation</param>
    /// <param name="productName">Optional product name to verify (defaults to "FathomOS")</param>
    /// <returns>Complete validation result with status and details</returns>
    /// <example>
    /// var verifier = new LicenseVerifier(publicKey);
    /// var fingerprints = MachineFingerprint.Generate(); // or your existing fingerprint collection
    /// var result = verifier.Validate(license, fingerprints);
    ///
    /// if (result.IsValid)
    /// {
    ///     Console.WriteLine($"Licensed modules: {string.Join(", ", result.LicensedModules)}");
    ///     Console.WriteLine($"Days until expiry: {result.DaysUntilExpiry}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Validation failed: {result.Error}");
    /// }
    /// </example>
    public OfflineLicenseValidationResult Validate(
        OfflineLicense license,
        List<string>? currentFingerprints = null,
        string productName = "FathomOS")
    {
        // Step 1: Basic null check
        if (license == null)
        {
            return OfflineLicenseValidationResult.Failure(
                "License is null",
                OfflineLicenseStatus.NotFound);
        }

        // Step 2: Version check
        if (!IsSupportedVersion(license.Version))
        {
            return OfflineLicenseValidationResult.Failure(
                $"License version '{license.Version}' is not supported. Expected: {OfflineLicenseConstants.CurrentVersion}",
                OfflineLicenseStatus.VersionNotSupported);
        }

        // Step 3: Verify cryptographic signature
        if (!VerifySignature(license))
        {
            return OfflineLicenseValidationResult.Failure(
                "License signature is invalid. The license may have been tampered with.",
                OfflineLicenseStatus.InvalidSignature);
        }

        // Step 4: Check product name
        if (!string.Equals(license.Product.Name, productName, StringComparison.OrdinalIgnoreCase))
        {
            return OfflineLicenseValidationResult.Failure(
                $"License is for product '{license.Product.Name}', not '{productName}'",
                OfflineLicenseStatus.ProductMismatch);
        }

        // Step 5: Check hardware binding (if fingerprints provided and license has binding)
        if (currentFingerprints != null &&
            currentFingerprints.Count > 0 &&
            license.Binding.HardwareFingerprints.Count > 0)
        {
            var matchCount = CountFingerprintMatches(
                license.Binding.HardwareFingerprints,
                currentFingerprints);

            if (matchCount < license.Binding.MatchThreshold)
            {
                return OfflineLicenseValidationResult.Failure(
                    $"Hardware fingerprint mismatch. Matched {matchCount}/{license.Binding.MatchThreshold} required components. " +
                    "This license is registered to different hardware.",
                    OfflineLicenseStatus.HardwareMismatch);
            }
        }

        // Step 6: Check expiration
        var now = DateTime.UtcNow;
        var daysUntilExpiry = license.DaysUntilExpiry;
        var isExpired = license.IsExpired;

        if (isExpired)
        {
            // Check grace period
            var gracePeriodEnd = license.Terms.ExpiresAt.AddDays(license.Terms.GracePeriodDays);
            var graceDaysRemaining = (int)(gracePeriodEnd - now).TotalDays;

            if (graceDaysRemaining > 0)
            {
                // In grace period - license is still valid but show warning
                return new OfflineLicenseValidationResult
                {
                    IsValid = true, // Still valid during grace period
                    Status = OfflineLicenseStatus.GracePeriod,
                    Error = string.Empty,
                    IsExpired = true,
                    IsInGracePeriod = true,
                    DaysUntilExpiry = daysUntilExpiry,
                    GraceDaysRemaining = graceDaysRemaining,
                    LicensedModules = license.Modules,
                    EnabledFeatures = license.Features,
                    License = license
                };
            }
            else
            {
                // Grace period also expired
                return OfflineLicenseValidationResult.Failure(
                    "License has expired and grace period has ended. Please renew your license.",
                    OfflineLicenseStatus.Expired);
            }
        }

        // All checks passed - license is valid
        return OfflineLicenseValidationResult.Success(license);
    }

    /// <summary>
    /// Validates a license and checks if a specific module is licensed.
    /// </summary>
    /// <param name="license">The license to check</param>
    /// <param name="moduleId">The module ID to check (e.g., "SurveyListing")</param>
    /// <param name="currentFingerprints">Optional fingerprints for hardware validation</param>
    /// <returns>True if license is valid AND the module is licensed</returns>
    public bool HasModule(OfflineLicense license, string moduleId, List<string>? currentFingerprints = null)
    {
        var result = Validate(license, currentFingerprints);
        if (!result.IsValid)
            return false;

        return result.LicensedModules.Contains(moduleId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates a license and checks if a specific feature is enabled.
    /// </summary>
    /// <param name="license">The license to check</param>
    /// <param name="featureName">The feature name to check (e.g., "Export:PDF")</param>
    /// <param name="currentFingerprints">Optional fingerprints for hardware validation</param>
    /// <returns>True if license is valid AND the feature is enabled</returns>
    public bool HasFeature(OfflineLicense license, string featureName, List<string>? currentFingerprints = null)
    {
        var result = Validate(license, currentFingerprints);
        if (!result.IsValid)
            return false;

        return result.EnabledFeatures.Contains(featureName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the license edition after validation.
    /// </summary>
    /// <param name="license">The license to check</param>
    /// <param name="currentFingerprints">Optional fingerprints for hardware validation</param>
    /// <returns>Edition string if valid, null otherwise</returns>
    public string? GetEdition(OfflineLicense license, List<string>? currentFingerprints = null)
    {
        var result = Validate(license, currentFingerprints);
        if (!result.IsValid)
            return null;

        return result.License?.Product.Edition;
    }

    /// <summary>
    /// Counts how many fingerprints match between stored and current.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    private static int CountFingerprintMatches(List<string> storedFingerprints, List<string> currentFingerprints)
    {
        int matchCount = 0;

        foreach (var stored in storedFingerprints)
        {
            foreach (var current in currentFingerprints)
            {
                if (ConstantTimeCompare(stored, current))
                {
                    matchCount++;
                    break; // Found match for this stored fingerprint
                }
            }
        }

        return matchCount;
    }

    /// <summary>
    /// Performs constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool ConstantTimeCompare(string a, string b)
    {
        if (a == null || b == null)
            return a == b;

        var aUpper = a.ToUpperInvariant();
        var bUpper = b.ToUpperInvariant();

        if (aUpper.Length != bUpper.Length)
            return false;

        var aBytes = Encoding.UTF8.GetBytes(aUpper);
        var bBytes = Encoding.UTF8.GetBytes(bUpper);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    /// <summary>
    /// Checks if the license version is supported.
    /// </summary>
    private static bool IsSupportedVersion(string version)
    {
        // Currently only support version 1.0
        // Add future versions here as they are released
        return version == "1.0" || version == OfflineLicenseConstants.CurrentVersion;
    }

    /// <summary>
    /// Validates that the public key is properly formatted.
    /// </summary>
    private void ValidatePublicKey()
    {
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_publicKeyPem);

            // Verify it's P-256
            var parameters = ecdsa.ExportParameters(false);
            var curveName = parameters.Curve.Oid?.FriendlyName;
            var curveValue = parameters.Curve.Oid?.Value;

            if (curveName != "nistP256" && curveValue != "1.2.840.10045.3.1.7")
            {
                throw new CryptographicException("Public key must use ECDSA P-256 curve");
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid public key: {ex.Message}", nameof(_publicKeyPem), ex);
        }
    }
}

/// <summary>
/// Factory for creating LicenseVerifier instances with different key sources.
/// </summary>
public static class LicenseVerifierFactory
{
    /// <summary>
    /// Creates a verifier using the default embedded public key.
    /// </summary>
    public static LicenseVerifier CreateDefault()
    {
        return new LicenseVerifier();
    }

    /// <summary>
    /// Creates a verifier with a custom public key.
    /// </summary>
    /// <param name="publicKeyPem">The public key in PEM format</param>
    public static LicenseVerifier Create(string publicKeyPem)
    {
        return new LicenseVerifier(publicKeyPem);
    }

    /// <summary>
    /// Creates a verifier by loading the public key from a file.
    /// </summary>
    /// <param name="publicKeyPath">Path to the public key PEM file</param>
    public static LicenseVerifier CreateFromFile(string publicKeyPath)
    {
        if (!File.Exists(publicKeyPath))
            throw new FileNotFoundException("Public key file not found", publicKeyPath);

        var publicKeyPem = File.ReadAllText(publicKeyPath);
        return new LicenseVerifier(publicKeyPem);
    }

    /// <summary>
    /// Creates a verifier from an embedded resource.
    /// </summary>
    /// <param name="resourceName">Name of the embedded resource containing the public key</param>
    /// <param name="assembly">The assembly containing the resource (defaults to calling assembly)</param>
    public static LicenseVerifier CreateFromResource(
        string resourceName,
        System.Reflection.Assembly? assembly = null)
    {
        assembly ??= System.Reflection.Assembly.GetCallingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly");

        using var reader = new StreamReader(stream);
        var publicKeyPem = reader.ReadToEnd();
        return new LicenseVerifier(publicKeyPem);
    }
}

/// <summary>
/// Extension methods for working with licenses and validation results.
/// </summary>
public static class LicenseVerifierExtensions
{
    /// <summary>
    /// Quick check if a license is valid (signature + not expired).
    /// Does not check hardware binding.
    /// </summary>
    public static bool IsQuickValid(this LicenseVerifier verifier, OfflineLicense license)
    {
        if (!verifier.VerifySignature(license))
            return false;

        return !license.IsExpired ||
               DateTime.UtcNow < license.Terms.ExpiresAt.AddDays(license.Terms.GracePeriodDays);
    }

    /// <summary>
    /// Gets a user-friendly status message.
    /// </summary>
    public static string GetStatusMessage(this OfflineLicenseValidationResult result)
    {
        return result.Status switch
        {
            OfflineLicenseStatus.Valid =>
                $"License valid. {result.DaysUntilExpiry} days remaining.",
            OfflineLicenseStatus.GracePeriod =>
                $"License EXPIRED! {result.GraceDaysRemaining} grace days remaining. Please renew.",
            OfflineLicenseStatus.Expired =>
                "License has expired. Please renew your subscription.",
            OfflineLicenseStatus.InvalidSignature =>
                "License is invalid or has been tampered with.",
            OfflineLicenseStatus.HardwareMismatch =>
                "License is registered to different hardware.",
            OfflineLicenseStatus.ProductMismatch =>
                "License is for a different product.",
            OfflineLicenseStatus.NotFound =>
                "No license found. Please activate your copy.",
            OfflineLicenseStatus.Corrupted =>
                "License file is corrupted. Please obtain a new license.",
            OfflineLicenseStatus.Revoked =>
                "License has been revoked. Please contact support.",
            OfflineLicenseStatus.VersionNotSupported =>
                "License format is not supported. Please obtain an updated license.",
            _ => result.Error
        };
    }
}
