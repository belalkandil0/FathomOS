// LicensingSystem.Client/Security/PinnedHttpClientHandler.cs
// SECURITY FIX: Certificate pinning for HTTPS connections to license server (VULN-009 / MISSING-002)
// Prevents MITM attacks by validating server certificate thumbprints
// Updated: January 2026 - Made thumbprints configurable for Render.com deployments

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace LicensingSystem.Client.Security;

/// <summary>
/// SECURITY FIX: HttpClientHandler with certificate pinning to prevent MITM attacks.
/// Validates that the server certificate matches one of the pinned thumbprints.
///
/// For Render.com deployments:
/// Render uses Let's Encrypt certificates that auto-rotate every 90 days.
/// Configure thumbprints via CertificatePinningConfig or use issuer validation.
/// </summary>
public class PinnedHttpClientHandler : HttpClientHandler
{
    // SECURITY FIX: Server certificate thumbprints for certificate pinning
    // These can be configured at runtime via ConfigureThumbprints() or loaded from settings
    //
    // For Render.com deployments with Let's Encrypt:
    // - Use issuer validation (more stable) OR
    // - Update thumbprints when certificates rotate OR
    // - Use the ConfigureFromSettings() method to load from appsettings.json
    //
    // To get the thumbprint of a certificate:
    // PowerShell: (Invoke-WebRequest https://your-server.onrender.com -UseBasicParsing).BaseResponse.GetResponseHeader("X-Certificate-Thumbprint")
    // Or: openssl s_client -connect your-server.onrender.com:443 | openssl x509 -fingerprint -sha1
    private static List<string> _validThumbprints = new()
    {
        // Let's Encrypt Root CA thumbprints (ISRG Root X1 and X2) - these are stable
        "CABD2A79A1076A31F21D253635CB039D4329A5E8", // ISRG Root X1
        "96BCEC06264976F37460779ACF28C5A7CFE8A3C0", // ISRG Root X2 (backup)
    };

    // Trusted certificate issuers (alternative to thumbprint pinning)
    private static readonly List<string> TrustedIssuers = new()
    {
        "CN=R3, O=Let's Encrypt, C=US",
        "CN=E1, O=Let's Encrypt, C=US",
        "CN=R10, O=Let's Encrypt, C=US",
        "CN=R11, O=Let's Encrypt, C=US",
        "CN=ISRG Root X1, O=Internet Security Research Group, C=US",
        "CN=ISRG Root X2, O=Internet Security Research Group, C=US"
    };

    /// <summary>
    /// SECURITY FIX: Whether to enforce strict certificate pinning (should be true in production)
    /// </summary>
    public static bool EnforceStrictPinning { get; set; } = true;

    /// <summary>
    /// SECURITY FIX: Whether to use issuer-based validation (recommended for Let's Encrypt)
    /// When true, validates that the certificate was issued by Let's Encrypt
    /// When false, uses thumbprint-based validation (requires updates when certs rotate)
    /// </summary>
    public static bool UseIssuerValidation { get; set; } = true;

    /// <summary>
    /// SECURITY FIX: Creates a new PinnedHttpClientHandler with certificate validation callback
    /// </summary>
    public PinnedHttpClientHandler()
    {
        // SECURITY FIX: Set custom certificate validation callback for certificate pinning
        ServerCertificateCustomValidationCallback = ValidateCertificate;
    }

    /// <summary>
    /// Configures certificate thumbprints at runtime.
    /// Call this at application startup with thumbprints from your configuration.
    /// </summary>
    /// <param name="thumbprints">Array of valid certificate thumbprints (SHA1, 40 hex chars)</param>
    public static void ConfigureThumbprints(params string[] thumbprints)
    {
        if (thumbprints == null || thumbprints.Length == 0)
            return;

        foreach (var thumbprint in thumbprints)
        {
            var normalized = thumbprint.Replace(" ", "").Replace(":", "").ToUpperInvariant();
            if (normalized.Length == 40 && !_validThumbprints.Contains(normalized))
            {
                _validThumbprints.Add(normalized);
                LogSecurityEvent($"Added certificate thumbprint: {normalized}");
            }
        }
    }

    /// <summary>
    /// Configures trusted certificate issuers at runtime.
    /// </summary>
    /// <param name="issuers">Array of issuer distinguished names</param>
    public static void ConfigureTrustedIssuers(params string[] issuers)
    {
        if (issuers == null || issuers.Length == 0)
            return;

        foreach (var issuer in issuers)
        {
            if (!string.IsNullOrWhiteSpace(issuer) && !TrustedIssuers.Contains(issuer))
            {
                TrustedIssuers.Add(issuer);
                LogSecurityEvent($"Added trusted issuer: {issuer}");
            }
        }
    }

    /// <summary>
    /// SECURITY FIX: Validates the server certificate against pinned thumbprints.
    /// This prevents MITM attacks by ensuring we only trust known certificates.
    /// </summary>
    /// <param name="message">The HTTP request message</param>
    /// <param name="cert">The server's X.509 certificate</param>
    /// <param name="chain">The certificate chain</param>
    /// <param name="errors">SSL policy errors detected during standard validation</param>
    /// <returns>True if the certificate is trusted, false otherwise</returns>
    private static bool ValidateCertificate(
        HttpRequestMessage message,
        X509Certificate2? cert,
        X509Chain? chain,
        SslPolicyErrors errors)
    {
        // SECURITY FIX: Reject if no certificate is presented
        if (cert == null)
        {
            LogSecurityEvent("Certificate validation failed: No certificate presented by server");
            return false;
        }

#if DEBUG
        // SECURITY FIX: In DEBUG mode, allow any valid certificate to facilitate development
        // This should never be the case in production builds
        if (errors == SslPolicyErrors.None)
        {
            LogSecurityEvent($"DEBUG: Accepting valid certificate with thumbprint: {cert.Thumbprint}");
            return true;
        }

        // SECURITY FIX: Even in DEBUG, log and reject certificates with SSL errors
        // unless they are self-signed certs for local development
        if (errors == SslPolicyErrors.RemoteCertificateChainErrors &&
            message.RequestUri?.Host == "localhost")
        {
            LogSecurityEvent("DEBUG: Accepting self-signed certificate for localhost development");
            return true;
        }

        LogSecurityEvent($"DEBUG: Rejecting certificate with errors: {errors}");
        return false;
#else
        // SECURITY FIX: In RELEASE mode, enforce strict certificate pinning

        // First, check if there are any standard SSL errors
        if (errors != SslPolicyErrors.None && EnforceStrictPinning)
        {
            LogSecurityEvent($"Certificate validation failed: SSL policy errors: {errors}");
            return false;
        }

        // SECURITY FIX: Option 1 - Issuer-based validation (recommended for Let's Encrypt)
        if (UseIssuerValidation && chain != null)
        {
            foreach (var chainElement in chain.ChainElements)
            {
                var issuer = chainElement.Certificate.Issuer;
                if (TrustedIssuers.Any(trusted =>
                    issuer.Contains(trusted, StringComparison.OrdinalIgnoreCase) ||
                    trusted.Contains(issuer, StringComparison.OrdinalIgnoreCase)))
                {
                    LogSecurityEvent($"Certificate validated successfully: Issuer '{issuer}' is trusted");
                    return true;
                }
            }
        }

        // SECURITY FIX: Option 2 - Validate against pinned thumbprints
        var thumbprint = cert.Thumbprint;
        foreach (var validThumbprint in _validThumbprints)
        {
            // SECURITY FIX: Skip placeholder values
            if (validThumbprint.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(thumbprint, validThumbprint, StringComparison.OrdinalIgnoreCase))
            {
                LogSecurityEvent($"Certificate validated successfully: Thumbprint matches pinned certificate");
                return true;
            }
        }

        // SECURITY FIX: Check certificate chain for root CA thumbprints
        if (chain != null)
        {
            foreach (var chainElement in chain.ChainElements)
            {
                var chainThumbprint = chainElement.Certificate.Thumbprint;
                if (_validThumbprints.Any(vt =>
                    string.Equals(chainThumbprint, vt, StringComparison.OrdinalIgnoreCase)))
                {
                    LogSecurityEvent($"Certificate validated successfully: Chain contains trusted root CA");
                    return true;
                }
            }
        }

        // SECURITY FIX: If we're using issuer validation and Let's Encrypt issuers are configured,
        // this is a valid fallback for auto-rotating certificates
        if (UseIssuerValidation && TrustedIssuers.Count > 0)
        {
            LogSecurityEvent($"Certificate validation failed: Issuer not in trusted list. " +
                           $"Certificate issuer: {cert.Issuer}");
            return false;
        }

        // SECURITY FIX: Certificate doesn't match any validation criteria
        LogSecurityEvent($"Certificate validation failed: Thumbprint {thumbprint} does not match any pinned certificate. " +
                        "Possible MITM attack detected!");
        return false;
#endif
    }

    /// <summary>
    /// SECURITY FIX: Logs security-related events for auditing and debugging
    /// </summary>
    private static void LogSecurityEvent(string message)
    {
        // In a production environment, this should integrate with your logging infrastructure
        // For now, we use debug output which can be captured during development
        System.Diagnostics.Debug.WriteLine($"[SECURITY] {DateTime.UtcNow:O} - {message}");

        // Also write to trace for production debugging if enabled
        System.Diagnostics.Trace.WriteLine($"[SECURITY] {DateTime.UtcNow:O} - {message}");
    }

    /// <summary>
    /// SECURITY FIX: Updates the pinned certificate thumbprints at runtime.
    /// This can be used for emergency certificate rotation without requiring a new build.
    /// Should only be called from trusted configuration sources.
    /// </summary>
    /// <param name="newThumbprints">Array of new valid thumbprints</param>
    /// <exception cref="ArgumentException">Thrown if thumbprints are null or empty</exception>
    public static void UpdatePinnedThumbprints(string[] newThumbprints)
    {
        if (newThumbprints == null || newThumbprints.Length == 0)
        {
            throw new ArgumentException("At least one certificate thumbprint must be provided",
                nameof(newThumbprints));
        }

        var validatedThumbprints = new List<string>();

        // SECURITY FIX: Validate thumbprint format (SHA-1 thumbprints are 40 hex characters)
        foreach (var thumbprint in newThumbprints)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                throw new ArgumentException("Thumbprint cannot be null or empty");
            }

            var normalized = thumbprint.Replace(" ", "").Replace(":", "").ToUpperInvariant();

            if (normalized.Length != 40 && !normalized.Contains("PLACEHOLDER"))
            {
                throw new ArgumentException($"Invalid thumbprint format: {thumbprint}. " +
                    "Thumbprints must be 40 hexadecimal characters.");
            }

            validatedThumbprints.Add(normalized);
        }

        // Thread-safe update
        _validThumbprints = validatedThumbprints;
        LogSecurityEvent($"Certificate thumbprints updated: {string.Join(", ", validatedThumbprints)}");
    }

    /// <summary>
    /// Gets the current list of valid thumbprints (for debugging/diagnostics)
    /// </summary>
    public static IReadOnlyList<string> GetCurrentThumbprints() => _validThumbprints.AsReadOnly();

    /// <summary>
    /// Gets the current list of trusted issuers (for debugging/diagnostics)
    /// </summary>
    public static IReadOnlyList<string> GetTrustedIssuers() => TrustedIssuers.AsReadOnly();
}

/// <summary>
/// Configuration class for certificate pinning settings.
/// Can be bound to appsettings.json "CertificatePinning" section.
/// </summary>
public class CertificatePinningConfig
{
    /// <summary>
    /// Enable or disable certificate pinning
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Use issuer-based validation (recommended for Let's Encrypt/Render)
    /// </summary>
    public bool UseIssuerValidation { get; set; } = true;

    /// <summary>
    /// List of trusted certificate thumbprints (SHA1, 40 hex chars)
    /// </summary>
    public List<string> Thumbprints { get; set; } = new();

    /// <summary>
    /// List of trusted certificate issuers (Distinguished Names)
    /// </summary>
    public List<string> TrustedIssuers { get; set; } = new();

    /// <summary>
    /// Applies this configuration to the PinnedHttpClientHandler
    /// </summary>
    public void Apply()
    {
        PinnedHttpClientHandler.EnforceStrictPinning = Enabled;
        PinnedHttpClientHandler.UseIssuerValidation = UseIssuerValidation;

        if (Thumbprints?.Count > 0)
        {
            PinnedHttpClientHandler.ConfigureThumbprints(Thumbprints.ToArray());
        }

        if (TrustedIssuers?.Count > 0)
        {
            PinnedHttpClientHandler.ConfigureTrustedIssuers(TrustedIssuers.ToArray());
        }
    }
}
