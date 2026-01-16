// LicensingSystem.Server/Configuration/LicensingConfiguration.cs
// Helper for secure key configuration

namespace LicensingSystem.Server.Configuration;

public static class LicensingConfiguration
{
    /// <summary>
    /// Gets the license signing private key from configuration, with environment variable override for production
    /// </summary>
    public static string GetPrivateKey(IConfiguration config)
    {
        // First check environment variable (recommended for production)
        var envKey = Environment.GetEnvironmentVariable("LICENSING_PRIVATE_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            // Environment variables may have literal \n, convert to actual newlines
            return envKey.Replace("\\n", "\n");
        }

        // Fall back to config file (for development)
        var configKey = config["Licensing:PrivateKeyPem"];
        if (!string.IsNullOrEmpty(configKey))
        {
            return configKey.Replace("\\n", "\n");
        }

        throw new InvalidOperationException(
            "Private key not configured. Set LICENSING_PRIVATE_KEY environment variable " +
            "or configure Licensing:PrivateKeyPem in appsettings.json");
    }

    /// <summary>
    /// Gets the certificate signing private key from configuration
    /// Used for signing processing certificates (separate from license signing)
    /// </summary>
    public static string GetCertificatePrivateKey(IConfiguration config)
    {
        // First check environment variable (recommended for production)
        var envKey = Environment.GetEnvironmentVariable("CERTIFICATE_PRIVATE_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            return envKey.Replace("\\n", "\n");
        }

        // Fall back to config file (for development)
        var configKey = config["Licensing:CertificatePrivateKeyPem"];
        if (!string.IsNullOrEmpty(configKey))
        {
            return configKey.Replace("\\n", "\n");
        }

        throw new InvalidOperationException(
            "Certificate private key not configured. Set CERTIFICATE_PRIVATE_KEY environment variable " +
            "or configure Licensing:CertificatePrivateKeyPem in appsettings.json");
    }

    /// <summary>
    /// Gets the certificate verification public key from configuration
    /// Used for verifying processing certificate signatures
    /// </summary>
    public static string GetCertificatePublicKey(IConfiguration config)
    {
        // First check environment variable
        var envKey = Environment.GetEnvironmentVariable("CERTIFICATE_PUBLIC_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            return envKey.Replace("\\n", "\n");
        }

        // Fall back to config file
        var configKey = config["Licensing:CertificatePublicKeyPem"];
        if (!string.IsNullOrEmpty(configKey))
        {
            return configKey.Replace("\\n", "\n");
        }

        throw new InvalidOperationException(
            "Certificate public key not configured. Set CERTIFICATE_PUBLIC_KEY environment variable " +
            "or configure Licensing:CertificatePublicKeyPem in appsettings.json");
    }
}
