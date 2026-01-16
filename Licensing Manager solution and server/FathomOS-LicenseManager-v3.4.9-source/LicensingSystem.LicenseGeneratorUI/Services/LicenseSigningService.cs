using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicensingSystem.Shared;

namespace LicenseGeneratorUI.Services;

/// <summary>
/// Service for signing and verifying licenses
/// </summary>
public class LicenseSigningService
{
    /// <summary>
    /// Signs a license with the provided private key
    /// </summary>
    public SignedLicense SignLicense(LicenseFile license, ECDsa privateKey)
    {
        var json = JsonSerializer.Serialize(license, new JsonSerializerOptions { WriteIndented = false });
        var licenseBytes = Encoding.UTF8.GetBytes(json);
        var signatureBytes = privateKey.SignData(licenseBytes, HashAlgorithmName.SHA256);

        return new SignedLicense
        {
            License = license,
            Signature = Convert.ToBase64String(signatureBytes)
        };
    }

    /// <summary>
    /// Verifies a license signature with the provided public key
    /// </summary>
    public bool VerifyLicense(SignedLicense signedLicense, ECDsa publicKey)
    {
        try
        {
            var json = JsonSerializer.Serialize(signedLicense.License, new JsonSerializerOptions { WriteIndented = false });
            var licenseBytes = Encoding.UTF8.GetBytes(json);
            var signatureBytes = Convert.FromBase64String(signedLicense.Signature);

            return publicKey.VerifyData(licenseBytes, signatureBytes, HashAlgorithmName.SHA256);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a license ID
    /// </summary>
    public static string GenerateLicenseId()
    {
        return $"LIC-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    }
}
