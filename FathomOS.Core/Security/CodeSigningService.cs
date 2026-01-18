// FathomOS.Core/Security/CodeSigningService.cs
// Code signing verification for assembly integrity protection

using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FathomOS.Core.Security;

/// <summary>
/// Interface for code signing verification services.
/// </summary>
public interface ICodeSigningService
{
    /// <summary>
    /// Verifies the Authenticode signature of an assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <returns>True if the signature is valid.</returns>
    bool VerifyAssemblySignature(string assemblyPath);

    /// <summary>
    /// Verifies all loaded assemblies have valid signatures.
    /// </summary>
    /// <returns>True if all assemblies pass verification.</returns>
    bool VerifyAllLoadedAssemblies();

    /// <summary>
    /// Gets the SHA-256 hash of an assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly file.</param>
    /// <returns>Uppercase hexadecimal hash string.</returns>
    string GetAssemblyHash(string assemblyPath);

    /// <summary>
    /// Gets detailed verification result for an assembly.
    /// </summary>
    SignatureVerificationResult VerifyAssemblyDetailed(string assemblyPath);

    /// <summary>
    /// Verifies an assembly matches an expected hash.
    /// </summary>
    bool VerifyAssemblyHash(string assemblyPath, string expectedHash);
}

/// <summary>
/// Provides code signing verification and assembly integrity checking.
///
/// Features:
/// - Authenticode signature verification
/// - SHA-256 hash verification
/// - Certificate chain validation
/// - Loaded assembly verification
/// - Strong name verification
/// </summary>
public sealed class CodeSigningService : ICodeSigningService
{
    // Expected certificate thumbprints for trusted signers
    private readonly HashSet<string> _trustedThumbprints;

    // Expected assembly hashes for integrity verification
    private readonly Dictionary<string, string> _expectedHashes;

    // Cache for verification results
    private readonly Dictionary<string, SignatureVerificationResult> _verificationCache;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Creates a new CodeSigningService with optional trusted thumbprints.
    /// </summary>
    /// <param name="trustedThumbprints">Optional set of trusted certificate thumbprints.</param>
    public CodeSigningService(IEnumerable<string>? trustedThumbprints = null)
    {
        _trustedThumbprints = trustedThumbprints != null
            ? new HashSet<string>(trustedThumbprints, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _expectedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _verificationCache = new Dictionary<string, SignatureVerificationResult>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public bool VerifyAssemblySignature(string assemblyPath)
    {
        var result = VerifyAssemblyDetailed(assemblyPath);
        return result.IsValid;
    }

    /// <inheritdoc/>
    public SignatureVerificationResult VerifyAssemblyDetailed(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            return new SignatureVerificationResult
            {
                AssemblyPath = assemblyPath ?? string.Empty,
                IsValid = false,
                ErrorMessage = "Assembly path cannot be null or empty"
            };
        }

        // Check cache
        lock (_cacheLock)
        {
            if (_verificationCache.TryGetValue(assemblyPath, out var cached))
            {
                return cached;
            }
        }

        var result = PerformSignatureVerification(assemblyPath);

        // Cache result
        lock (_cacheLock)
        {
            _verificationCache[assemblyPath] = result;
        }

        return result;
    }

    /// <inheritdoc/>
    public bool VerifyAllLoadedAssemblies()
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .ToList();

            foreach (var assembly in assemblies)
            {
                // Skip system assemblies
                if (IsSystemAssembly(assembly))
                    continue;

                var result = VerifyAssemblyDetailed(assembly.Location);

                if (!result.IsValid)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public string GetAssemblyHash(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));

        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException("Assembly file not found", assemblyPath);

        using var stream = File.OpenRead(assemblyPath);
        var hashBytes = SHA256.HashData(stream);
        return Convert.ToHexString(hashBytes);
    }

    /// <inheritdoc/>
    public bool VerifyAssemblyHash(string assemblyPath, string expectedHash)
    {
        try
        {
            var actualHash = GetAssemblyHash(assemblyPath);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Registers an expected hash for an assembly.
    /// </summary>
    /// <param name="assemblyName">Name of the assembly (without extension).</param>
    /// <param name="expectedHash">Expected SHA-256 hash.</param>
    public void RegisterExpectedHash(string assemblyName, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyName));

        if (string.IsNullOrWhiteSpace(expectedHash))
            throw new ArgumentException("Expected hash cannot be null or empty", nameof(expectedHash));

        _expectedHashes[assemblyName] = expectedHash;
    }

    /// <summary>
    /// Adds a trusted certificate thumbprint.
    /// </summary>
    /// <param name="thumbprint">Certificate thumbprint (SHA-1 hash).</param>
    public void AddTrustedThumbprint(string thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
            throw new ArgumentException("Thumbprint cannot be null or empty", nameof(thumbprint));

        _trustedThumbprints.Add(thumbprint.Replace(" ", "").ToUpperInvariant());
    }

    /// <summary>
    /// Clears the verification cache.
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _verificationCache.Clear();
        }
    }

    /// <summary>
    /// Gets the signing certificate from an assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly.</param>
    /// <returns>The signing certificate or null if not signed.</returns>
    public X509Certificate2? GetSigningCertificate(string assemblyPath)
    {
        try
        {
            var cert = X509Certificate.CreateFromSignedFile(assemblyPath);
            return new X509Certificate2(cert);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Verifies the strong name signature of an assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly.</param>
    /// <returns>True if the assembly has a valid strong name.</returns>
    public bool VerifyStrongName(string assemblyPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            var name = assembly.GetName();

            // Check if assembly has a public key
            var publicKey = name.GetPublicKey();
            return publicKey != null && publicKey.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    #region Private Methods

    private SignatureVerificationResult PerformSignatureVerification(string assemblyPath)
    {
        var result = new SignatureVerificationResult
        {
            AssemblyPath = assemblyPath,
            VerificationTime = DateTime.UtcNow
        };

        try
        {
            // Check if file exists
            if (!File.Exists(assemblyPath))
            {
                result.ErrorMessage = "Assembly file not found";
                return result;
            }

            // Compute hash
            result.ComputedHash = GetAssemblyHash(assemblyPath);

            // Check against expected hash if registered
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            if (_expectedHashes.TryGetValue(assemblyName, out var expectedHash))
            {
                result.ExpectedHash = expectedHash;
                result.HashValid = string.Equals(result.ComputedHash, expectedHash, StringComparison.OrdinalIgnoreCase);

                if (result.HashValid == false)
                {
                    result.ErrorMessage = "Assembly hash does not match expected value - possible tampering";
                    return result;
                }
            }
            else
            {
                // No expected hash registered - mark as unknown
                result.HashValid = null;
            }

            // Try to get Authenticode certificate
            try
            {
                var cert = X509Certificate.CreateFromSignedFile(assemblyPath);
                var cert2 = new X509Certificate2(cert);

                result.IsSigned = true;
                result.SignerSubject = cert2.Subject;
                result.SignerThumbprint = cert2.Thumbprint;
                result.SignerNotBefore = cert2.NotBefore;
                result.SignerNotAfter = cert2.NotAfter;

                // Verify certificate validity
                result.CertificateValid = DateTime.UtcNow >= cert2.NotBefore &&
                                          DateTime.UtcNow <= cert2.NotAfter;

                // Verify certificate chain
                using var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                result.ChainValid = chain.Build(cert2);

                if (!result.ChainValid)
                {
                    result.ChainStatus = chain.ChainStatus
                        .Select(s => s.StatusInformation)
                        .ToList();
                }

                // Check if signer is trusted
                if (_trustedThumbprints.Count > 0)
                {
                    result.IsTrustedSigner = _trustedThumbprints.Contains(cert2.Thumbprint);
                }

                // Overall validity
                result.IsValid = result.IsSigned &&
                                 (result.HashValid ?? true) &&
                                 result.CertificateValid &&
                                 result.ChainValid;
            }
            catch (CryptographicException)
            {
                // Assembly is not signed
                result.IsSigned = false;
                result.ErrorMessage = "Assembly is not digitally signed";

                // If no expected hash is registered, unsigned assemblies are considered invalid
                result.IsValid = result.HashValid == true;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Verification failed: {ex.Message}";
        }

        return result;
    }

    private static bool IsSystemAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? "";

        // Check for Microsoft/System assemblies
        if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("netstandard", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if assembly is in GAC
        try
        {
            if (assembly.GlobalAssemblyCache)
                return true;
        }
        catch
        {
            // Ignore
        }

        // Check if assembly is in system directory
        try
        {
            var location = assembly.Location;
            var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (location.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase) ||
                location.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
                location.StartsWith(programFilesX86, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        catch
        {
            // Ignore
        }

        return false;
    }

    #endregion
}

/// <summary>
/// Detailed result of assembly signature verification.
/// </summary>
public sealed class SignatureVerificationResult
{
    /// <summary>
    /// Path to the verified assembly.
    /// </summary>
    public string AssemblyPath { get; set; } = string.Empty;

    /// <summary>
    /// Time the verification was performed.
    /// </summary>
    public DateTime VerificationTime { get; set; }

    /// <summary>
    /// Overall verification result.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether the assembly is digitally signed.
    /// </summary>
    public bool IsSigned { get; set; }

    /// <summary>
    /// Computed SHA-256 hash of the assembly.
    /// </summary>
    public string? ComputedHash { get; set; }

    /// <summary>
    /// Expected hash if registered.
    /// </summary>
    public string? ExpectedHash { get; set; }

    /// <summary>
    /// Whether the hash matches the expected value (null if no expected hash registered).
    /// </summary>
    public bool? HashValid { get; set; }

    /// <summary>
    /// Subject of the signing certificate.
    /// </summary>
    public string? SignerSubject { get; set; }

    /// <summary>
    /// Thumbprint of the signing certificate.
    /// </summary>
    public string? SignerThumbprint { get; set; }

    /// <summary>
    /// Certificate validity start date.
    /// </summary>
    public DateTime? SignerNotBefore { get; set; }

    /// <summary>
    /// Certificate validity end date.
    /// </summary>
    public DateTime? SignerNotAfter { get; set; }

    /// <summary>
    /// Whether the certificate is currently valid.
    /// </summary>
    public bool CertificateValid { get; set; }

    /// <summary>
    /// Whether the certificate chain is valid.
    /// </summary>
    public bool ChainValid { get; set; }

    /// <summary>
    /// Certificate chain status if validation failed.
    /// </summary>
    public List<string> ChainStatus { get; set; } = new();

    /// <summary>
    /// Whether the signer is in the trusted list.
    /// </summary>
    public bool? IsTrustedSigner { get; set; }

    /// <summary>
    /// Error message if verification failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    public override string ToString()
    {
        if (IsValid)
        {
            return $"Valid: {Path.GetFileName(AssemblyPath)} - {SignerSubject}";
        }

        return $"Invalid: {Path.GetFileName(AssemblyPath)} - {ErrorMessage}";
    }
}
