// LicensingSystem.Server/Services/QrCodeService.cs
// QR Code generation for offline licenses and verification

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicensingSystem.Server.Services;

public interface IQrCodeService
{
    string GenerateVerificationQrData(string licenseId, string licenseKey, DateTime expiresAt);
    string GenerateVerificationUrl(string licenseId, string verificationToken);
    (string Token, DateTime ExpiresAt) CreateVerificationToken(string licenseId);
    bool ValidateVerificationToken(string licenseId, string token);
}

public class QrCodeService : IQrCodeService
{
    private readonly IConfiguration _config;
    private readonly ILogger<QrCodeService> _logger;
    
    // In production, use a secure key from configuration
    private static readonly byte[] SecretKey = Encoding.UTF8.GetBytes("FathomOS-License-QR-Secret-Key-2024!");

    public QrCodeService(IConfiguration config, ILogger<QrCodeService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Generate QR code data for offline license verification
    /// </summary>
    public string GenerateVerificationQrData(string licenseId, string licenseKey, DateTime expiresAt)
    {
        // Create verification payload
        var payload = new QrVerificationPayload
        {
            LicenseId = licenseId,
            KeyFragment = licenseKey.Length > 8 ? licenseKey[..8] : licenseKey, // Only first 8 chars
            ExpiresAt = expiresAt,
            GeneratedAt = DateTime.UtcNow
        };

        // Sign the payload
        var json = JsonSerializer.Serialize(payload);
        var signature = ComputeSignature(json);
        
        payload.Signature = signature;
        
        // Return JSON for QR code
        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Generate verification URL for QR code
    /// </summary>
    public string GenerateVerificationUrl(string licenseId, string verificationToken)
    {
        var baseUrl = _config["BaseUrl"] ?? "https://license.fathomos.com";
        return $"{baseUrl}/verify?id={Uri.EscapeDataString(licenseId)}&token={Uri.EscapeDataString(verificationToken)}";
    }

    /// <summary>
    /// Create a time-limited verification token
    /// </summary>
    public (string Token, DateTime ExpiresAt) CreateVerificationToken(string licenseId)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        
        // Create token data
        var tokenData = $"{licenseId}:{expiresAt:O}";
        var signature = ComputeSignature(tokenData);
        
        // Combine into token
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{tokenData}:{signature}"))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return (token, expiresAt);
    }

    /// <summary>
    /// Validate a verification token
    /// </summary>
    public bool ValidateVerificationToken(string licenseId, string token)
    {
        try
        {
            // Add padding back
            var paddedToken = token.Replace("-", "+").Replace("_", "/");
            switch (paddedToken.Length % 4)
            {
                case 2: paddedToken += "=="; break;
                case 3: paddedToken += "="; break;
            }

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(paddedToken));
            var parts = decoded.Split(':');
            
            if (parts.Length != 3) return false;

            var tokenLicenseId = parts[0];
            var expiresAtStr = parts[1];
            var signature = parts[2];

            // Verify license ID matches
            if (tokenLicenseId != licenseId) return false;

            // Verify not expired
            if (!DateTime.TryParse(expiresAtStr, out var expiresAt)) return false;
            if (expiresAt < DateTime.UtcNow) return false;

            // Verify signature
            var expectedSignature = ComputeSignature($"{tokenLicenseId}:{expiresAtStr}");
            return signature == expectedSignature;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }

    private static string ComputeSignature(string data)
    {
        using var hmac = new HMACSHA256(SecretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash)[..16]; // First 16 chars for brevity
    }
}

public class QrVerificationPayload
{
    public string LicenseId { get; set; } = string.Empty;
    public string KeyFragment { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? Signature { get; set; }
}
