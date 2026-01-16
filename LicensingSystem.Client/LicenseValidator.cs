// LicensingSystem.Client/LicenseValidator.cs
// Client-side license validation with ECDSA signature verification

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicensingSystem.Shared;

namespace LicensingSystem.Client;

/// <summary>
/// Validates licenses on the client side
/// </summary>
public class LicenseValidator
{
    // YOUR PUBLIC KEY - Generated December 23, 2024
    // This is embedded in your app and used to verify license signatures
    private const string DefaultPublicKeyPem = @"-----BEGIN PUBLIC KEY-----
MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEqfPevrauRvxoUEd3aCRgwUrare3n
Z/6HvjBOpRV0JuJDmGFyV/cNVZuf5wg2PvogWwN/mSZFB27S2PycJcoEew==
-----END PUBLIC KEY-----";

    private readonly HardwareFingerprint _hwFingerprint;
    private readonly LicenseStorage _storage;
    private readonly string _productName;
    private readonly string _publicKeyPem;

    public LicenseValidator(string productName, string? customPublicKey = null)
    {
        _productName = productName;
        _hwFingerprint = new HardwareFingerprint();
        _storage = new LicenseStorage(productName); // BUG FIX: Pass productName
        
        // BUG FIX: Actually use the custom public key if provided
        _publicKeyPem = !string.IsNullOrEmpty(customPublicKey) 
            ? customPublicKey 
            : DefaultPublicKeyPem;
    }

    /// <summary>
    /// Main validation entry point - validates the current stored license
    /// </summary>
    public LicenseValidationResult ValidateCurrentLicense()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG: LicenseValidator.ValidateCurrentLicense() called");
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Product name: {_productName}");
            
            // Try to load stored license
            var signedLicense = _storage.LoadLicense();
            if (signedLicense == null)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG:   No license found in storage");
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.NotFound,
                    Message = "No license found. Please activate your copy."
                };
            }
            
            System.Diagnostics.Debug.WriteLine($"DEBUG:   License loaded from storage:");
            System.Diagnostics.Debug.WriteLine($"DEBUG:     LicenseId: {signedLicense.License.LicenseId}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:     Product: {signedLicense.License.Product}");
            System.Diagnostics.Debug.WriteLine($"DEBUG:     Edition: {signedLicense.License.Edition}");

            // SECURITY: Check if this license was previously revoked
            // This prevents using a stored license after revocation was detected
            if (_storage.IsLicenseRevoked(signedLicense.License.LicenseId))
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG:   License is revoked!");
                // Clear the stored license since it's revoked
                _storage.ClearLicense();
                
                var reason = _storage.GetRevocationReason(signedLicense.License.LicenseId);
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.Revoked,
                    Message = $"This license has been revoked. {reason ?? "Please contact support."}"
                };
            }

            return ValidateLicense(signedLicense);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Exception in ValidateCurrentLicense: {ex.Message}");
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = $"License validation error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Validates a specific license file
    /// </summary>
    public LicenseValidationResult ValidateLicense(SignedLicense signedLicense)
    {
        var license = signedLicense.License;
        System.Diagnostics.Debug.WriteLine($"DEBUG: ValidateLicense() called");

        // Step 1: Verify cryptographic signature
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 1: Verifying signature...");
        if (!VerifySignature(signedLicense))
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 1 FAILED: Invalid signature");
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.InvalidSignature,
                Message = "License signature is invalid. The license may have been tampered with."
            };
        }
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 1 PASSED: Signature valid");

        // Step 2: Check product name
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 2: Checking product name...");
        System.Diagnostics.Debug.WriteLine($"DEBUG:     License.Product: '{license.Product}'");
        System.Diagnostics.Debug.WriteLine($"DEBUG:     Expected (_productName): '{_productName}'");
        if (!string.Equals(license.Product, _productName, StringComparison.OrdinalIgnoreCase))
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 2 FAILED: Product mismatch");
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.InvalidSignature,
                Message = "License is not valid for this product."
            };
        }
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 2 PASSED: Product matches");

        // Step 3: Verify hardware fingerprint with fuzzy matching
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 3: Checking hardware fingerprints...");
        var currentFingerprints = _hwFingerprint.GetFingerprints();
        var matchCount = HardwareFingerprint.CountMatches(
            license.HardwareFingerprints, 
            currentFingerprints);
        System.Diagnostics.Debug.WriteLine($"DEBUG:     Match count: {matchCount}, Threshold: {license.FingerprintMatchThreshold}");

        if (matchCount < license.FingerprintMatchThreshold)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 3 FAILED: Hardware mismatch");
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.HardwareMismatch,
                Message = $"License is registered to different hardware. " +
                          $"Matched {matchCount}/{license.FingerprintMatchThreshold} required components."
            };
        }
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 3 PASSED: Hardware matches");

        // Step 4: Check expiration
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 4: Checking expiration...");
        var now = DateTime.UtcNow;
        var daysRemaining = (int)(license.ExpiresAt - now).TotalDays;
        System.Diagnostics.Debug.WriteLine($"DEBUG:     ExpiresAt: {license.ExpiresAt}, Now: {now}, DaysRemaining: {daysRemaining}");

        if (license.ExpiresAt < now)
        {
            // Check grace period
            var gracePeriodEnd = license.ExpiresAt.AddDays(LicenseConstants.GracePeriodDays);
            var graceDaysRemaining = (int)(gracePeriodEnd - now).TotalDays;
            System.Diagnostics.Debug.WriteLine($"DEBUG:     License expired! Grace period ends: {gracePeriodEnd}, GraceDaysRemaining: {graceDaysRemaining}");

            if (now <= gracePeriodEnd)
            {
                System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 4: In grace period");
                // In grace period
                return new LicenseValidationResult
                {
                    IsValid = true, // Still allow usage
                    Status = LicenseStatus.GracePeriod,
                    Message = $"License expired! You have {graceDaysRemaining} grace days remaining. Please renew.",
                    License = license,
                    DaysRemaining = 0,
                    GraceDaysRemaining = graceDaysRemaining,
                    EnabledFeatures = license.Features
                };
            }

            System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 4 FAILED: License expired");
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Expired,
                Message = "License has expired. Please renew your subscription.",
                License = license,
                DaysRemaining = daysRemaining
            };
        }
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 4 PASSED: License not expired");

        // Step 5: Check offline period
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 5: Checking offline period...");
        var lastOnlineCheck = _storage.GetLastOnlineCheck();
        var daysSinceOnlineCheck = (DateTime.UtcNow - lastOnlineCheck).TotalDays;
        System.Diagnostics.Debug.WriteLine($"DEBUG:     LastOnlineCheck: {lastOnlineCheck}, DaysSinceOnlineCheck: {daysSinceOnlineCheck}, MaxDays: {LicenseConstants.OfflineMaxDays}");

        if (daysSinceOnlineCheck > LicenseConstants.OfflineMaxDays)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 5 FAILED: Offline period exceeded");
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Expired,
                Message = "Offline period exceeded. Please connect to the internet to revalidate your license.",
                License = license
            };
        }
        System.Diagnostics.Debug.WriteLine($"DEBUG:   Step 5 PASSED: Within offline period");

        // All checks passed
        System.Diagnostics.Debug.WriteLine($"DEBUG:   ALL CHECKS PASSED - License is valid!");
        return new LicenseValidationResult
        {
            IsValid = true,
            Status = LicenseStatus.Valid,
            Message = "License is valid.",
            License = license,
            DaysRemaining = daysRemaining,
            EnabledFeatures = license.Features
        };
    }

    /// <summary>
    /// Activates a license from a file
    /// </summary>
    public LicenseValidationResult ActivateFromFile(string licenseFilePath)
    {
        try
        {
            // BUG FIX: Check if file exists first
            if (!File.Exists(licenseFilePath))
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.NotFound,
                    Message = $"License file not found: {licenseFilePath}"
                };
            }
            
            var json = File.ReadAllText(licenseFilePath);
            var signedLicense = JsonSerializer.Deserialize<SignedLicense>(json);

            if (signedLicense == null)
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.Corrupted,
                    Message = "Invalid license file format."
                };
            }

            // SECURITY: Check if this license was previously revoked
            // This prevents re-importing a .lic file after revocation
            if (_storage.IsLicenseRevoked(signedLicense.License.LicenseId))
            {
                var reason = _storage.GetRevocationReason(signedLicense.License.LicenseId);
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Status = LicenseStatus.Revoked,
                    Message = $"This license has been revoked and cannot be reused. {reason ?? ""}"
                };
            }

            // Validate the license
            var result = ValidateLicense(signedLicense);

            if (result.IsValid || result.Status == LicenseStatus.GracePeriod)
            {
                // Store the valid license
                _storage.SaveLicense(signedLicense);
                _storage.UpdateLastOnlineCheck();
            }

            return result;
        }
        catch (JsonException)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = "License file is corrupted or has invalid format."
            };
        }
        catch (Exception ex)
        {
            return new LicenseValidationResult
            {
                IsValid = false,
                Status = LicenseStatus.Corrupted,
                Message = $"Error reading license file: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Gets the current hardware ID for display to user
    /// </summary>
    public string GetHardwareId()
    {
        return _hwFingerprint.GetDisplayHwid();
    }

    /// <summary>
    /// Gets all hardware fingerprints for activation request
    /// </summary>
    public List<string> GetHardwareFingerprints()
    {
        return _hwFingerprint.GetFingerprints();
    }

    /// <summary>
    /// Verifies the ECDSA signature of the license
    /// </summary>
    private bool VerifySignature(SignedLicense signedLicense)
    {
        try
        {
            // Serialize the license (without signature) for verification
            var licenseJson = JsonSerializer.Serialize(signedLicense.License, 
                new JsonSerializerOptions { WriteIndented = false });
            var licenseBytes = Encoding.UTF8.GetBytes(licenseJson);
            var signatureBytes = Convert.FromBase64String(signedLicense.Signature);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(_publicKeyPem); // BUG FIX: Use instance field

            return ecdsa.VerifyData(licenseBytes, signatureBytes, HashAlgorithmName.SHA256);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Removes the current license (for testing or deactivation)
    /// </summary>
    public void RemoveLicense()
    {
        _storage.ClearLicense();
    }
}
