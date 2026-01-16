// LicensingSystem.Server/Services/LicenseObfuscationService.cs
// License key obfuscation with encryption, checksums, and machine-specific binding

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicensingSystem.Server.Services;

public interface ILicenseObfuscationService
{
    /// <summary>
    /// Encrypt license data with machine-specific key
    /// </summary>
    string EncryptLicenseData(LicenseEncryptionData data, string? hardwareId = null);
    
    /// <summary>
    /// Decrypt license data
    /// </summary>
    LicenseEncryptionData? DecryptLicenseData(string encryptedData, string? hardwareId = null);
    
    /// <summary>
    /// Generate tamper-proof checksum for license
    /// </summary>
    string GenerateChecksum(string licenseKey, string licenseId, DateTime expiresAt);
    
    /// <summary>
    /// Validate license checksum
    /// </summary>
    bool ValidateChecksum(string licenseKey, string licenseId, DateTime expiresAt, string checksum);
    
    /// <summary>
    /// Obfuscate license key for display (XXXX-****-****-XXXX)
    /// </summary>
    string ObfuscateKeyForDisplay(string licenseKey);
    
    /// <summary>
    /// Generate machine-specific encryption key
    /// </summary>
    byte[] GenerateMachineKey(string hardwareId);
}

public class LicenseObfuscationService : ILicenseObfuscationService
{
    private readonly ILogger<LicenseObfuscationService> _logger;
    
    // Master encryption key (in production, load from secure configuration)
    private static readonly byte[] MasterKey = Encoding.UTF8.GetBytes("FathomOS-Master-License-Key-2024");
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("FathomOS-Salt-v3");

    public LicenseObfuscationService(ILogger<LicenseObfuscationService> logger)
    {
        _logger = logger;
    }

    public string EncryptLicenseData(LicenseEncryptionData data, string? hardwareId = null)
    {
        try
        {
            // Add anti-tampering metadata
            data.GeneratedAt = DateTime.UtcNow;
            data.Version = "3.4.8";
            data.Checksum = GenerateDataChecksum(data);

            var json = JsonSerializer.Serialize(data);
            var plainBytes = Encoding.UTF8.GetBytes(json);

            // Determine encryption key
            var key = string.IsNullOrEmpty(hardwareId) 
                ? MasterKey 
                : GenerateMachineKey(hardwareId);

            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV + encrypted data
            var result = new byte[aes.IV.Length + encrypted.Length];
            aes.IV.CopyTo(result, 0);
            encrypted.CopyTo(result, aes.IV.Length);

            // Add header to indicate encryption version
            var header = "FATHOM3:";
            if (!string.IsNullOrEmpty(hardwareId))
            {
                header = "FATHOM3HW:"; // Hardware-bound
            }

            return header + Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt license data");
            throw;
        }
    }

    public LicenseEncryptionData? DecryptLicenseData(string encryptedData, string? hardwareId = null)
    {
        try
        {
            // Check header
            string base64Data;
            bool isHardwareBound = false;

            if (encryptedData.StartsWith("FATHOM3HW:"))
            {
                base64Data = encryptedData[10..];
                isHardwareBound = true;
            }
            else if (encryptedData.StartsWith("FATHOM3:"))
            {
                base64Data = encryptedData[8..];
            }
            else
            {
                _logger.LogWarning("Invalid license data format");
                return null;
            }

            // Hardware bound but no hardware ID provided
            if (isHardwareBound && string.IsNullOrEmpty(hardwareId))
            {
                _logger.LogWarning("Hardware-bound license requires hardware ID");
                return null;
            }

            var combinedBytes = Convert.FromBase64String(base64Data);

            // Extract IV and encrypted data
            var iv = new byte[16];
            var encrypted = new byte[combinedBytes.Length - 16];
            Array.Copy(combinedBytes, 0, iv, 0, 16);
            Array.Copy(combinedBytes, 16, encrypted, 0, encrypted.Length);

            // Determine decryption key
            var key = isHardwareBound 
                ? GenerateMachineKey(hardwareId!) 
                : MasterKey;

            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
            var json = Encoding.UTF8.GetString(decrypted);

            var data = JsonSerializer.Deserialize<LicenseEncryptionData>(json);
            if (data == null) return null;

            // Verify checksum
            var expectedChecksum = GenerateDataChecksum(data);
            if (data.Checksum != expectedChecksum)
            {
                _logger.LogWarning("License data checksum mismatch - possible tampering");
                return null;
            }

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt license data");
            return null;
        }
    }

    public string GenerateChecksum(string licenseKey, string licenseId, DateTime expiresAt)
    {
        var data = $"{licenseKey}:{licenseId}:{expiresAt:O}:FathomOS";
        
        using var hmac = new HMACSHA256(MasterKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        
        // Return first 8 characters of base64
        return Convert.ToBase64String(hash)[..8];
    }

    public bool ValidateChecksum(string licenseKey, string licenseId, DateTime expiresAt, string checksum)
    {
        var expected = GenerateChecksum(licenseKey, licenseId, expiresAt);
        return expected == checksum;
    }

    public string ObfuscateKeyForDisplay(string licenseKey)
    {
        if (string.IsNullOrEmpty(licenseKey)) return "****-****-****-****";
        
        var parts = licenseKey.Split('-');
        if (parts.Length != 4) return licenseKey;
        
        // Show first and last parts, hide middle
        return $"{parts[0]}-****-****-{parts[3]}";
    }

    public byte[] GenerateMachineKey(string hardwareId)
    {
        // Derive a unique key from hardware ID + master key
        using var pbkdf2 = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(hardwareId),
            Salt,
            100000,
            HashAlgorithmName.SHA256);
        
        return pbkdf2.GetBytes(32);
    }

    private static byte[] DeriveKey(byte[] baseKey)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            baseKey,
            Salt,
            10000,
            HashAlgorithmName.SHA256);
        
        return pbkdf2.GetBytes(32);
    }

    private static string GenerateDataChecksum(LicenseEncryptionData data)
    {
        var checksumData = $"{data.LicenseId}:{data.LicenseKey}:{data.ExpiresAt:O}:{data.Edition}:{data.Version}";
        
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(checksumData));
        
        return Convert.ToBase64String(hash)[..12];
    }
}

/// <summary>
/// License data structure for encryption
/// </summary>
public class LicenseEncryptionData
{
    public string LicenseId { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? Edition { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Features { get; set; }
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    public List<string>? Modules { get; set; }
    
    // Anti-tampering fields
    public string? Version { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public string? Checksum { get; set; }
}
