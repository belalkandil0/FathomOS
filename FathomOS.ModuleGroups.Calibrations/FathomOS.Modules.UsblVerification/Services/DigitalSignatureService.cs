using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for digitally signing verification certificates
/// </summary>
public class DigitalSignatureService
{
    private readonly string _keyStorePath;
    private const string KeyFileName = "signing_key.pem";
    private const string PublicKeyFileName = "signing_key.pub";
    
    public DigitalSignatureService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _keyStorePath = Path.Combine(appData, "FathomOS", "UsblVerification", "Keys");
        Directory.CreateDirectory(_keyStorePath);
    }
    
    #region Key Management
    
    /// <summary>
    /// Generate a new RSA key pair for signing
    /// </summary>
    public bool GenerateKeyPair(string? password = null)
    {
        try
        {
            using var rsa = RSA.Create(2048);
            
            // Export private key (optionally encrypted)
            byte[] privateKeyBytes;
            if (!string.IsNullOrEmpty(password))
            {
                privateKeyBytes = rsa.ExportEncryptedPkcs8PrivateKey(
                    Encoding.UTF8.GetBytes(password),
                    new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100000));
            }
            else
            {
                privateKeyBytes = rsa.ExportPkcs8PrivateKey();
            }
            
            // Export public key
            var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
            
            // Save keys
            var privateKeyPem = ConvertToPem(privateKeyBytes, password != null ? "ENCRYPTED PRIVATE KEY" : "PRIVATE KEY");
            var publicKeyPem = ConvertToPem(publicKeyBytes, "PUBLIC KEY");
            
            File.WriteAllText(Path.Combine(_keyStorePath, KeyFileName), privateKeyPem);
            File.WriteAllText(Path.Combine(_keyStorePath, PublicKeyFileName), publicKeyPem);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Check if signing keys exist
    /// </summary>
    public bool HasSigningKey()
    {
        return File.Exists(Path.Combine(_keyStorePath, KeyFileName));
    }
    
    /// <summary>
    /// Load private key from storage
    /// </summary>
    private RSA? LoadPrivateKey(string? password = null)
    {
        var keyPath = Path.Combine(_keyStorePath, KeyFileName);
        if (!File.Exists(keyPath)) return null;
        
        try
        {
            var pemContent = File.ReadAllText(keyPath);
            var keyBytes = ConvertFromPem(pemContent);
            
            var rsa = RSA.Create();
            
            if (pemContent.Contains("ENCRYPTED"))
            {
                if (string.IsNullOrEmpty(password))
                    return null;
                rsa.ImportEncryptedPkcs8PrivateKey(Encoding.UTF8.GetBytes(password), keyBytes, out _);
            }
            else
            {
                rsa.ImportPkcs8PrivateKey(keyBytes, out _);
            }
            
            return rsa;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Load public key from storage
    /// </summary>
    private RSA? LoadPublicKey()
    {
        var keyPath = Path.Combine(_keyStorePath, PublicKeyFileName);
        if (!File.Exists(keyPath)) return null;
        
        try
        {
            var pemContent = File.ReadAllText(keyPath);
            var keyBytes = ConvertFromPem(pemContent);
            
            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);
            return rsa;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region Signing
    
    /// <summary>
    /// Sign certificate data and return signature
    /// </summary>
    public SignatureResult SignCertificate(CertificateSigningData data, string? keyPassword = null)
    {
        var result = new SignatureResult();
        
        using var rsa = LoadPrivateKey(keyPassword);
        if (rsa == null)
        {
            result.Success = false;
            result.Error = "Failed to load signing key. Generate key pair first.";
            return result;
        }
        
        try
        {
            // Create canonical JSON representation
            var canonicalData = CreateCanonicalData(data);
            var dataBytes = Encoding.UTF8.GetBytes(canonicalData);
            
            // Calculate hash
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(dataBytes);
            
            // Sign the hash
            var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            result.Success = true;
            result.Signature = signature;
            result.SignatureBase64 = Convert.ToBase64String(signature);
            result.DataHash = Convert.ToBase64String(hash);
            result.SignedAt = DateTime.UtcNow;
            result.Algorithm = "RSA-SHA256";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        
        return result;
    }
    
    /// <summary>
    /// Verify a certificate signature
    /// </summary>
    public VerificationResult VerifySignature(CertificateSigningData data, string signatureBase64)
    {
        var result = new VerificationResult();
        
        using var rsa = LoadPublicKey();
        if (rsa == null)
        {
            result.IsValid = false;
            result.Error = "Failed to load public key.";
            return result;
        }
        
        try
        {
            var signature = Convert.FromBase64String(signatureBase64);
            var canonicalData = CreateCanonicalData(data);
            var dataBytes = Encoding.UTF8.GetBytes(canonicalData);
            
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(dataBytes);
            
            result.IsValid = rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            result.VerifiedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Error = ex.Message;
        }
        
        return result;
    }
    
    /// <summary>
    /// Sign a PDF file by appending signature metadata
    /// </summary>
    public SignatureResult SignPdfFile(string pdfPath, CertificateSigningData data, string? keyPassword = null)
    {
        // First sign the data
        var signResult = SignCertificate(data, keyPassword);
        if (!signResult.Success)
            return signResult;
        
        // Create signature metadata file alongside PDF
        var metadataPath = Path.ChangeExtension(pdfPath, ".sig");
        var metadata = new PdfSignatureMetadata
        {
            PdfFileName = Path.GetFileName(pdfPath),
            CertificateData = data,
            Signature = signResult.SignatureBase64,
            DataHash = signResult.DataHash,
            SignedAt = signResult.SignedAt,
            Algorithm = signResult.Algorithm
        };
        
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metadataPath, json);
        
        signResult.MetadataPath = metadataPath;
        return signResult;
    }
    
    #endregion
    
    #region Helper Methods
    
    private string CreateCanonicalData(CertificateSigningData data)
    {
        // Create a deterministic JSON representation
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
        
        // Sort properties and create canonical form
        var canonical = new
        {
            certificateNumber = data.CertificateNumber,
            projectName = data.ProjectName,
            vesselName = data.VesselName,
            clientName = data.ClientName,
            verificationDate = data.VerificationDate.ToString("O"),
            transponderName = data.TransponderName,
            overallPassed = data.OverallPassed,
            meanEasting = Math.Round(data.MeanEasting, 6),
            meanNorthing = Math.Round(data.MeanNorthing, 6),
            meanDepth = Math.Round(data.MeanDepth, 3),
            toleranceMeters = Math.Round(data.ToleranceMeters, 3),
            qualityScore = Math.Round(data.QualityScore, 2)
        };
        
        return JsonSerializer.Serialize(canonical, options);
    }
    
    private string ConvertToPem(byte[] data, string label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-----BEGIN {label}-----");
        sb.AppendLine(Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks));
        sb.AppendLine($"-----END {label}-----");
        return sb.ToString();
    }
    
    private byte[] ConvertFromPem(string pem)
    {
        var lines = pem.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !l.StartsWith("-----"))
            .ToArray();
        var base64 = string.Join("", lines);
        return Convert.FromBase64String(base64);
    }
    
    /// <summary>
    /// Export public key for sharing
    /// </summary>
    public string? ExportPublicKey()
    {
        var keyPath = Path.Combine(_keyStorePath, PublicKeyFileName);
        return File.Exists(keyPath) ? File.ReadAllText(keyPath) : null;
    }
    
    /// <summary>
    /// Get key info
    /// </summary>
    public KeyInfo GetKeyInfo()
    {
        var info = new KeyInfo();
        var privatePath = Path.Combine(_keyStorePath, KeyFileName);
        var publicPath = Path.Combine(_keyStorePath, PublicKeyFileName);
        
        info.HasPrivateKey = File.Exists(privatePath);
        info.HasPublicKey = File.Exists(publicPath);
        
        if (info.HasPrivateKey)
        {
            var fileInfo = new FileInfo(privatePath);
            info.KeyCreatedAt = fileInfo.CreationTime;
            info.IsEncrypted = File.ReadAllText(privatePath).Contains("ENCRYPTED");
        }
        
        return info;
    }
    
    #endregion
}

#region Signature Models

/// <summary>
/// Data to be signed for a certificate
/// </summary>
public class CertificateSigningData
{
    public string CertificateNumber { get; set; } = "";
    public string? ProjectName { get; set; }
    public string? VesselName { get; set; }
    public string? ClientName { get; set; }
    public DateTime VerificationDate { get; set; }
    public string? TransponderName { get; set; }
    public bool OverallPassed { get; set; }
    public double MeanEasting { get; set; }
    public double MeanNorthing { get; set; }
    public double MeanDepth { get; set; }
    public double ToleranceMeters { get; set; }
    public double QualityScore { get; set; }
}

/// <summary>
/// Result of signing operation
/// </summary>
public class SignatureResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public byte[]? Signature { get; set; }
    public string? SignatureBase64 { get; set; }
    public string? DataHash { get; set; }
    public DateTime SignedAt { get; set; }
    public string? Algorithm { get; set; }
    public string? MetadataPath { get; set; }
}

/// <summary>
/// Result of verification
/// </summary>
public class VerificationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public DateTime VerifiedAt { get; set; }
}

/// <summary>
/// Metadata stored alongside signed PDF
/// </summary>
public class PdfSignatureMetadata
{
    public string PdfFileName { get; set; } = "";
    public CertificateSigningData? CertificateData { get; set; }
    public string? Signature { get; set; }
    public string? DataHash { get; set; }
    public DateTime SignedAt { get; set; }
    public string? Algorithm { get; set; }
}

/// <summary>
/// Information about stored keys
/// </summary>
public class KeyInfo
{
    public bool HasPrivateKey { get; set; }
    public bool HasPublicKey { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime? KeyCreatedAt { get; set; }
}

#endregion
