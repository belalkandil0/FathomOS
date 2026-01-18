using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LicenseGeneratorUI.Services;

/// <summary>
/// Service for secure storage and management of ECDSA signing keys.
/// Private keys are encrypted using DPAPI (Windows Data Protection API).
/// </summary>
public class KeyStorageService
{
    private readonly string _keyStoragePath;
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public KeyStorageService()
    {
        var localAppData = GetLocalAppDataPath();

        _keyStoragePath = Path.Combine(
            localAppData,
            "FathomOSLicenseManager",
            "keys");

        Directory.CreateDirectory(_keyStoragePath);

        _privateKeyPath = Path.Combine(_keyStoragePath, "private.key.dpapi");
        _publicKeyPath = Path.Combine(_keyStoragePath, "public.pem");
    }

    /// <summary>
    /// Get the local app data path with robust fallback logic
    /// </summary>
    private static string GetLocalAppDataPath()
    {
        // Try LocalApplicationData first
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localAppData))
        {
            System.Diagnostics.Debug.WriteLine($"KeyStorageService: Using LocalApplicationData: {localAppData}");
            return localAppData;
        }

        // Fallback to UserProfile
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            var fallbackPath = Path.Combine(userProfile, ".FathomOS");
            System.Diagnostics.Debug.WriteLine($"KeyStorageService: Using UserProfile fallback: {fallbackPath}");
            return fallbackPath;
        }

        // Ultimate fallback: use application directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = Directory.GetCurrentDirectory();
        }
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = "."; // Current directory as last resort
        }

        var appDataPath = Path.Combine(baseDir, "AppData");
        System.Diagnostics.Debug.WriteLine($"KeyStorageService: Using application directory fallback: {appDataPath}");
        return appDataPath;
    }

    /// <summary>
    /// Check if private key exists
    /// </summary>
    public bool HasPrivateKey()
    {
        return File.Exists(_privateKeyPath);
    }

    /// <summary>
    /// Check if public key exists
    /// </summary>
    public bool HasPublicKey()
    {
        return File.Exists(_publicKeyPath);
    }

    /// <summary>
    /// Generate a new ECDSA P-256 key pair
    /// </summary>
    /// <returns>Tuple of (privateKeyPem, publicKeyPem)</returns>
    public (string privateKey, string publicKey) GenerateKeyPair()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Export private key in EC format
        var privateKeyBytes = ecdsa.ExportECPrivateKey();
        var privateKeyPem = "-----BEGIN EC PRIVATE KEY-----\n" +
                           Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                           "\n-----END EC PRIVATE KEY-----";

        // Export public key in SubjectPublicKeyInfo format
        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        var publicKeyPem = "-----BEGIN PUBLIC KEY-----\n" +
                          Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                          "\n-----END PUBLIC KEY-----";

        return (privateKeyPem, publicKeyPem);
    }

    /// <summary>
    /// Store the private key encrypted with DPAPI
    /// </summary>
    public void StorePrivateKey(string privateKeyPem)
    {
        if (string.IsNullOrEmpty(privateKeyPem))
            throw new ArgumentNullException(nameof(privateKeyPem));

        // Encrypt using DPAPI (CurrentUser scope - only this user can decrypt)
        var plaintextBytes = Encoding.UTF8.GetBytes(privateKeyPem);
        var encryptedBytes = ProtectedData.Protect(
            plaintextBytes,
            GetEntropy(),
            DataProtectionScope.CurrentUser);

        File.WriteAllBytes(_privateKeyPath, encryptedBytes);
    }

    /// <summary>
    /// Store the public key (unencrypted - safe to share)
    /// </summary>
    public void StorePublicKey(string publicKeyPem)
    {
        if (string.IsNullOrEmpty(publicKeyPem))
            throw new ArgumentNullException(nameof(publicKeyPem));

        File.WriteAllText(_publicKeyPath, publicKeyPem);
    }

    /// <summary>
    /// Load the private key (decrypted from DPAPI storage)
    /// </summary>
    public string LoadPrivateKey()
    {
        if (!HasPrivateKey())
            throw new FileNotFoundException("Private key not found. Please set up keys first.");

        var encryptedBytes = File.ReadAllBytes(_privateKeyPath);
        var plaintextBytes = ProtectedData.Unprotect(
            encryptedBytes,
            GetEntropy(),
            DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    /// <summary>
    /// Load the private key as an ECDsa object
    /// </summary>
    public ECDsa LoadPrivateKeyAsECDsa()
    {
        var privateKeyPem = LoadPrivateKey();
        return ParsePrivateKey(privateKeyPem);
    }

    /// <summary>
    /// Load the public key
    /// </summary>
    public string LoadPublicKey()
    {
        if (!HasPublicKey())
            throw new FileNotFoundException("Public key not found. Please set up keys first.");

        return File.ReadAllText(_publicKeyPath);
    }

    /// <summary>
    /// Load the public key as an ECDsa object
    /// </summary>
    public ECDsa LoadPublicKeyAsECDsa()
    {
        var publicKeyPem = LoadPublicKey();
        return ParsePublicKey(publicKeyPem);
    }

    /// <summary>
    /// Export the private key to a file (for backup)
    /// </summary>
    public void ExportPrivateKey(string path)
    {
        var privateKey = LoadPrivateKey();
        File.WriteAllText(path, privateKey);
    }

    /// <summary>
    /// Export the public key to a file
    /// </summary>
    public void ExportPublicKey(string path)
    {
        var publicKey = LoadPublicKey();
        File.WriteAllText(path, publicKey);
    }

    /// <summary>
    /// Import a private key from a file
    /// </summary>
    public void ImportPrivateKey(string path)
    {
        var privateKeyPem = File.ReadAllText(path);

        // Validate the key by parsing it
        var ecdsa = ParsePrivateKey(privateKeyPem);

        // Store it encrypted
        StorePrivateKey(privateKeyPem);

        // Derive and store the public key
        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        var publicKeyPem = "-----BEGIN PUBLIC KEY-----\n" +
                          Convert.ToBase64String(publicKeyBytes, Base64FormattingOptions.InsertLineBreaks) +
                          "\n-----END PUBLIC KEY-----";
        StorePublicKey(publicKeyPem);
    }

    /// <summary>
    /// Get the key storage path for display purposes
    /// </summary>
    public string GetStoragePath()
    {
        return _keyStoragePath;
    }

    /// <summary>
    /// Delete all stored keys (use with caution!)
    /// </summary>
    public void DeleteAllKeys()
    {
        if (File.Exists(_privateKeyPath))
            File.Delete(_privateKeyPath);

        if (File.Exists(_publicKeyPath))
            File.Delete(_publicKeyPath);
    }

    /// <summary>
    /// Get a fingerprint/ID of the current public key
    /// </summary>
    public string? GetPublicKeyId()
    {
        if (!HasPublicKey())
            return null;

        var publicKey = LoadPublicKey();
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(publicKey));
        return Convert.ToBase64String(hash.Take(8).ToArray());
    }

    // Additional entropy for DPAPI protection (application-specific)
    private static byte[] GetEntropy()
    {
        // This adds application-specific entropy to DPAPI protection
        // The same entropy must be used for encryption and decryption
        return Encoding.UTF8.GetBytes("FathomOS-LicenseManager-v3-Key-Entropy");
    }

    private static ECDsa ParsePrivateKey(string pem)
    {
        var ecdsa = ECDsa.Create();

        // Remove PEM headers and decode
        var base64 = pem
            .Replace("-----BEGIN EC PRIVATE KEY-----", "")
            .Replace("-----END EC PRIVATE KEY-----", "")
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        var keyBytes = Convert.FromBase64String(base64);

        // Try ECPrivateKey format first
        try
        {
            ecdsa.ImportECPrivateKey(keyBytes, out _);
            return ecdsa;
        }
        catch
        {
            // Try PKCS8 format
            ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);
            return ecdsa;
        }
    }

    private static ECDsa ParsePublicKey(string pem)
    {
        var ecdsa = ECDsa.Create();

        // Remove PEM headers and decode
        var base64 = pem
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        var keyBytes = Convert.FromBase64String(base64);
        ecdsa.ImportSubjectPublicKeyInfo(keyBytes, out _);

        return ecdsa;
    }
}
