// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/TlsWrapper.cs
// Purpose: TLS encryption support for NaviPac TCP connections (VULN-005)
// Version: 1.0 - Initial implementation
// ============================================================================
//
// SECURITY NOTES:
// - Provides optional TLS encryption for TCP connections
// - Supports self-signed certificate generation for development/testing
// - Allows configuration of TLS protocol versions and validation
// - Backward compatible - TLS can be disabled for legacy systems
//
// ============================================================================

using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// TLS configuration options for NaviPac connections.
/// </summary>
public class TlsConfiguration
{
    /// <summary>
    /// Enable TLS encryption for TCP connections.
    /// Default: false for backward compatibility.
    /// </summary>
    public bool EnableTls { get; set; } = false;

    /// <summary>
    /// Path to the server certificate file (.pfx or .p12).
    /// Required when running as TLS server (TCP listener mode).
    /// </summary>
    public string? ServerCertificatePath { get; set; }

    /// <summary>
    /// Password for the server certificate file.
    /// </summary>
    public string? ServerCertificatePassword { get; set; }

    /// <summary>
    /// Enable client certificate validation.
    /// When true, clients must present a valid certificate.
    /// </summary>
    public bool RequireClientCertificate { get; set; } = false;

    /// <summary>
    /// Allow self-signed certificates.
    /// WARNING: Only enable for development/testing or controlled environments.
    /// </summary>
    public bool AllowSelfSignedCertificates { get; set; } = false;

    /// <summary>
    /// Skip certificate chain validation.
    /// WARNING: Reduces security. Only use when necessary.
    /// </summary>
    public bool SkipCertificateChainValidation { get; set; } = false;

    /// <summary>
    /// Target host name for certificate validation (client mode).
    /// </summary>
    public string? TargetHostName { get; set; }

    /// <summary>
    /// Minimum TLS protocol version to accept.
    /// Default: TLS 1.2 for security.
    /// </summary>
    public SslProtocols MinimumProtocolVersion { get; set; } = SslProtocols.Tls12;

    /// <summary>
    /// Maximum TLS protocol version to use.
    /// Default: TLS 1.3 when available.
    /// </summary>
    public SslProtocols MaximumProtocolVersion { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;

    /// <summary>
    /// Timeout for TLS handshake in milliseconds.
    /// </summary>
    public int HandshakeTimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Validates the configuration and returns any error messages.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (EnableTls)
        {
            if (!string.IsNullOrEmpty(ServerCertificatePath) && !File.Exists(ServerCertificatePath))
            {
                errors.Add($"Server certificate file not found: {ServerCertificatePath}");
            }

            if (MinimumProtocolVersion < SslProtocols.Tls12)
            {
                errors.Add("TLS versions below 1.2 are not recommended due to security vulnerabilities.");
            }
        }

        return errors;
    }
}

/// <summary>
/// Provides TLS wrapper functionality for TCP connections.
/// Used to secure NaviPac communications when configured.
/// </summary>
public class TlsWrapper : IDisposable
{
    private readonly TlsConfiguration _config;
    private X509Certificate2? _serverCertificate;
    private bool _disposed;

    /// <summary>
    /// Event raised when TLS errors occur.
    /// </summary>
    public event EventHandler<TlsErrorEventArgs>? TlsError;

    /// <summary>
    /// Event raised when TLS connection is established.
    /// </summary>
    public event EventHandler<TlsConnectedEventArgs>? TlsConnected;

    /// <summary>
    /// Initializes a new TLS wrapper with the specified configuration.
    /// </summary>
    public TlsWrapper(TlsConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Gets whether TLS is enabled.
    /// </summary>
    public bool IsEnabled => _config.EnableTls;

    /// <summary>
    /// Loads the server certificate from file.
    /// </summary>
    public void LoadServerCertificate()
    {
        if (string.IsNullOrEmpty(_config.ServerCertificatePath))
            return;

        try
        {
            _serverCertificate = new X509Certificate2(
                _config.ServerCertificatePath,
                _config.ServerCertificatePassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            Debug.WriteLine($"TlsWrapper: Loaded server certificate: {_serverCertificate.Subject}");
        }
        catch (Exception ex)
        {
            OnTlsError($"Failed to load server certificate: {ex.Message}", ex);
            throw;
        }
    }

    /// <summary>
    /// Wraps a network stream with TLS encryption (server mode).
    /// Used when accepting incoming connections from NaviPac.
    /// </summary>
    /// <param name="tcpClient">The connected TCP client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authenticated SSL stream, or the original network stream if TLS is disabled.</returns>
    public async Task<Stream> WrapServerStreamAsync(TcpClient tcpClient, CancellationToken cancellationToken = default)
    {
        if (!_config.EnableTls)
        {
            return tcpClient.GetStream();
        }

        if (_serverCertificate == null)
        {
            throw new InvalidOperationException("Server certificate not loaded. Call LoadServerCertificate() first.");
        }

        var networkStream = tcpClient.GetStream();
        var sslStream = new SslStream(
            networkStream,
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: ValidateClientCertificate);

        try
        {
            var authOptions = new SslServerAuthenticationOptions
            {
                ServerCertificate = _serverCertificate,
                ClientCertificateRequired = _config.RequireClientCertificate,
                EnabledSslProtocols = _config.MaximumProtocolVersion,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            };

            using var timeoutCts = new CancellationTokenSource(_config.HandshakeTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await sslStream.AuthenticateAsServerAsync(authOptions, linkedCts.Token);

            LogTlsConnectionInfo(sslStream, tcpClient);

            OnTlsConnected(new TlsConnectedEventArgs
            {
                Protocol = sslStream.SslProtocol,
                CipherAlgorithm = sslStream.CipherAlgorithm,
                IsAuthenticated = sslStream.IsAuthenticated,
                IsEncrypted = sslStream.IsEncrypted,
                RemoteEndPoint = tcpClient.Client.RemoteEndPoint?.ToString()
            });

            return sslStream;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            sslStream.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            sslStream.Dispose();
            OnTlsError($"TLS handshake failed: {ex.Message}", ex);
            throw new TlsHandshakeException("TLS handshake failed", ex);
        }
    }

    /// <summary>
    /// Wraps a network stream with TLS encryption (client mode).
    /// Used when connecting to a TLS-enabled server.
    /// </summary>
    /// <param name="tcpClient">The connected TCP client.</param>
    /// <param name="targetHost">The target host name for certificate validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authenticated SSL stream.</returns>
    public async Task<Stream> WrapClientStreamAsync(TcpClient tcpClient, string targetHost, CancellationToken cancellationToken = default)
    {
        if (!_config.EnableTls)
        {
            return tcpClient.GetStream();
        }

        var networkStream = tcpClient.GetStream();
        var sslStream = new SslStream(
            networkStream,
            leaveInnerStreamOpen: false,
            userCertificateValidationCallback: ValidateServerCertificate);

        try
        {
            var authOptions = new SslClientAuthenticationOptions
            {
                TargetHost = targetHost,
                EnabledSslProtocols = _config.MaximumProtocolVersion,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            };

            using var timeoutCts = new CancellationTokenSource(_config.HandshakeTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await sslStream.AuthenticateAsClientAsync(authOptions, linkedCts.Token);

            LogTlsConnectionInfo(sslStream, tcpClient);

            OnTlsConnected(new TlsConnectedEventArgs
            {
                Protocol = sslStream.SslProtocol,
                CipherAlgorithm = sslStream.CipherAlgorithm,
                IsAuthenticated = sslStream.IsAuthenticated,
                IsEncrypted = sslStream.IsEncrypted,
                RemoteEndPoint = tcpClient.Client.RemoteEndPoint?.ToString()
            });

            return sslStream;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            sslStream.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            sslStream.Dispose();
            OnTlsError($"TLS handshake failed: {ex.Message}", ex);
            throw new TlsHandshakeException("TLS handshake failed", ex);
        }
    }

    /// <summary>
    /// Validates the server certificate during client authentication.
    /// </summary>
    private bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        // Allow self-signed certificates if configured
        if (_config.AllowSelfSignedCertificates &&
            (sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
        {
            if (chain?.ChainStatus.All(s =>
                s.Status == X509ChainStatusFlags.UntrustedRoot ||
                s.Status == X509ChainStatusFlags.PartialChain) == true)
            {
                Debug.WriteLine("TlsWrapper: Accepting self-signed certificate");
                return true;
            }
        }

        // Skip chain validation if configured
        if (_config.SkipCertificateChainValidation &&
            (sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
        {
            Debug.WriteLine("TlsWrapper: Skipping certificate chain validation");
            return true;
        }

        Debug.WriteLine($"TlsWrapper: Certificate validation failed: {sslPolicyErrors}");
        OnTlsError($"Certificate validation failed: {sslPolicyErrors}", null);
        return false;
    }

    /// <summary>
    /// Validates the client certificate during server authentication.
    /// </summary>
    private bool ValidateClientCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (!_config.RequireClientCertificate)
            return true;

        if (sslPolicyErrors == SslPolicyErrors.None)
            return true;

        // Allow self-signed client certificates if configured
        if (_config.AllowSelfSignedCertificates &&
            (sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
        {
            if (chain?.ChainStatus.All(s =>
                s.Status == X509ChainStatusFlags.UntrustedRoot ||
                s.Status == X509ChainStatusFlags.PartialChain) == true)
            {
                Debug.WriteLine("TlsWrapper: Accepting self-signed client certificate");
                return true;
            }
        }

        Debug.WriteLine($"TlsWrapper: Client certificate validation failed: {sslPolicyErrors}");
        OnTlsError($"Client certificate validation failed: {sslPolicyErrors}", null);
        return false;
    }

    private void LogTlsConnectionInfo(SslStream sslStream, TcpClient client)
    {
        Debug.WriteLine($"TlsWrapper: TLS connection established");
        Debug.WriteLine($"  Remote: {client.Client.RemoteEndPoint}");
        Debug.WriteLine($"  Protocol: {sslStream.SslProtocol}");
        Debug.WriteLine($"  Cipher: {sslStream.CipherAlgorithm} ({sslStream.CipherStrength} bits)");
        Debug.WriteLine($"  Hash: {sslStream.HashAlgorithm} ({sslStream.HashStrength} bits)");
        Debug.WriteLine($"  Key Exchange: {sslStream.KeyExchangeAlgorithm} ({sslStream.KeyExchangeStrength} bits)");
        Debug.WriteLine($"  Authenticated: {sslStream.IsAuthenticated}");
        Debug.WriteLine($"  Encrypted: {sslStream.IsEncrypted}");
        Debug.WriteLine($"  Signed: {sslStream.IsSigned}");
    }

    private void OnTlsError(string message, Exception? exception)
    {
        TlsError?.Invoke(this, new TlsErrorEventArgs
        {
            Message = message,
            Exception = exception,
            Timestamp = DateTime.Now
        });
    }

    private void OnTlsConnected(TlsConnectedEventArgs args)
    {
        TlsConnected?.Invoke(this, args);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _serverCertificate?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Utility class for generating self-signed certificates.
/// For development and testing purposes.
/// </summary>
public static class CertificateUtility
{
    /// <summary>
    /// Generates a self-signed certificate for development/testing.
    /// </summary>
    /// <param name="subjectName">Certificate subject name (e.g., "CN=FathomOS NaviPac Server").</param>
    /// <param name="validDays">Number of days the certificate is valid.</param>
    /// <param name="password">Password for the certificate file.</param>
    /// <param name="outputPath">Path to save the .pfx certificate file.</param>
    /// <returns>The generated certificate.</returns>
    public static X509Certificate2 GenerateSelfSignedCertificate(
        string subjectName,
        int validDays = 365,
        string? password = null,
        string? outputPath = null)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            subjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add key usage extensions
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        // Add enhanced key usage (server and client authentication)
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
                    new Oid("1.3.6.1.5.5.7.3.2")  // Client Authentication
                },
                critical: false));

        // Add subject alternative name for localhost
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = notBefore.AddDays(validDays);

        var certificate = request.CreateSelfSigned(notBefore, notAfter);

        // Export to PFX format with private key
        if (!string.IsNullOrEmpty(outputPath))
        {
            var pfxBytes = certificate.Export(X509ContentType.Pfx, password);

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, pfxBytes);
            Debug.WriteLine($"CertificateUtility: Certificate saved to {outputPath}");
        }

        // Return a certificate with exportable private key
        return new X509Certificate2(
            certificate.Export(X509ContentType.Pfx, password),
            password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    /// <summary>
    /// Gets the default certificate storage path for FathomOS.
    /// </summary>
    public static string GetDefaultCertificatePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "SurveyLogbook", "Certificates", "navipac_server.pfx");
    }

    /// <summary>
    /// Checks if a certificate file exists and is valid.
    /// </summary>
    /// <param name="certificatePath">Path to the certificate file.</param>
    /// <param name="password">Certificate password.</param>
    /// <returns>True if the certificate is valid and not expired.</returns>
    public static bool IsCertificateValid(string certificatePath, string? password = null)
    {
        try
        {
            if (!File.Exists(certificatePath))
                return false;

            using var cert = new X509Certificate2(certificatePath, password);
            return cert.NotAfter > DateTime.Now && cert.NotBefore <= DateTime.Now;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets certificate information as a formatted string.
    /// </summary>
    public static string GetCertificateInfo(string certificatePath, string? password = null)
    {
        try
        {
            using var cert = new X509Certificate2(certificatePath, password);
            return $"Subject: {cert.Subject}\n" +
                   $"Issuer: {cert.Issuer}\n" +
                   $"Serial: {cert.SerialNumber}\n" +
                   $"Valid From: {cert.NotBefore:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Valid To: {cert.NotAfter:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Thumbprint: {cert.Thumbprint}";
        }
        catch (Exception ex)
        {
            return $"Error reading certificate: {ex.Message}";
        }
    }
}

/// <summary>
/// Exception thrown when TLS handshake fails.
/// </summary>
public class TlsHandshakeException : Exception
{
    public TlsHandshakeException(string message) : base(message) { }
    public TlsHandshakeException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Event args for TLS errors.
/// </summary>
public class TlsErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event args for successful TLS connection.
/// </summary>
public class TlsConnectedEventArgs : EventArgs
{
    public SslProtocols Protocol { get; set; }
    public CipherAlgorithmType CipherAlgorithm { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsEncrypted { get; set; }
    public string? RemoteEndPoint { get; set; }
}
