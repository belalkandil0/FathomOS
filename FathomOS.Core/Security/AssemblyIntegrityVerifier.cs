// SECURITY FIX: Task 3.4 - Assembly Integrity Verification
// Issue ID: MISSING-004
// Priority: MEDIUM
// Purpose: Runtime verification that assemblies haven't been modified to detect tampering

using System.Reflection;
using System.Security.Cryptography;

namespace FathomOS.Core.Security;

/// <summary>
/// Provides runtime verification of assembly integrity to detect tampering.
///
/// SECURITY FIX: This class implements assembly hash verification to ensure
/// that the application's assemblies haven't been modified since build time.
/// This helps protect against:
/// - Unauthorized code modifications
/// - Malicious DLL injection
/// - Assembly tampering attacks
///
/// Note: Expected hashes should be populated at build time via a build step
/// that generates and embeds the correct hash values.
/// </summary>
public static class AssemblyIntegrityVerifier
{
    /// <summary>
    /// Expected SHA-256 hashes for core assemblies.
    /// SECURITY FIX: These values should be populated at build time by a secure build process.
    /// The placeholder values will cause verification to skip in DEBUG mode.
    /// </summary>
    private static readonly Dictionary<string, string> ExpectedHashes = new()
    {
        // SECURITY FIX: Placeholder hashes - these should be generated and embedded at build time
        // by a post-build step that computes SHA-256 hashes of the release assemblies.
        // Format: ["AssemblyName"] = "SHA256_HASH_IN_UPPERCASE_HEX"
        ["FathomOS.Shell"] = "HASH_PLACEHOLDER",
        ["FathomOS.Core"] = "HASH_PLACEHOLDER",
        ["LicensingSystem.Client"] = "HASH_PLACEHOLDER"
    };

    /// <summary>
    /// Results from the last integrity verification.
    /// </summary>
    public static IntegrityVerificationResult? LastVerificationResult { get; private set; }

    /// <summary>
    /// Verifies the integrity of all registered assemblies.
    /// </summary>
    /// <returns>True if all assemblies pass verification, false if any assembly has been modified.</returns>
    public static bool VerifyIntegrity()
    {
#if DEBUG
        // SECURITY FIX: In DEBUG mode, skip verification to allow development
        // This is intentional - integrity checking is only meaningful for release builds
        LastVerificationResult = new IntegrityVerificationResult
        {
            IsValid = true,
            VerificationSkipped = true,
            SkipReason = "DEBUG build - integrity verification is only performed in RELEASE builds"
        };
        return true;
#else
        // SECURITY FIX: In RELEASE mode, perform full verification
        return PerformVerification();
#endif
    }

    /// <summary>
    /// Verifies the integrity of a specific assembly by name.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to verify.</param>
    /// <returns>True if the assembly passes verification, false otherwise.</returns>
    public static bool VerifyAssembly(string assemblyName)
    {
#if DEBUG
        return true;
#else
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return false;
        }

        if (!ExpectedHashes.TryGetValue(assemblyName, out var expectedHash))
        {
            // Assembly not in the verification list - this could be suspicious
            return false;
        }

        // Skip if placeholder hash
        if (expectedHash == "HASH_PLACEHOLDER")
        {
            return true;
        }

        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (assembly == null)
            {
                return false;
            }

            var actualHash = ComputeAssemblyHash(assembly);
            return string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // SECURITY FIX: Fail closed - if we can't verify, assume compromised
            return false;
        }
#endif
    }

    /// <summary>
    /// Performs the full verification of all registered assemblies.
    /// </summary>
    /// <returns>True if all assemblies pass verification.</returns>
    private static bool PerformVerification()
    {
        var result = new IntegrityVerificationResult();
        var allValid = true;

        foreach (var (assemblyName, expectedHash) in ExpectedHashes)
        {
            var assemblyResult = new AssemblyVerificationResult
            {
                AssemblyName = assemblyName,
                ExpectedHash = expectedHash
            };

            // SECURITY FIX: Skip placeholder hashes (not yet populated by build process)
            if (expectedHash == "HASH_PLACEHOLDER")
            {
                assemblyResult.Status = VerificationStatus.Skipped;
                assemblyResult.Notes = "Placeholder hash - build process should populate actual hash";
                result.AssemblyResults.Add(assemblyResult);
                continue;
            }

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == assemblyName);

                if (assembly == null)
                {
                    assemblyResult.Status = VerificationStatus.NotFound;
                    assemblyResult.Notes = "Assembly not loaded in current AppDomain";
                    result.AssemblyResults.Add(assemblyResult);
                    allValid = false;
                    continue;
                }

                var actualHash = ComputeAssemblyHash(assembly);
                assemblyResult.ActualHash = actualHash;

                if (string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
                {
                    assemblyResult.Status = VerificationStatus.Valid;
                }
                else
                {
                    // SECURITY FIX: Hash mismatch - possible tampering
                    assemblyResult.Status = VerificationStatus.HashMismatch;
                    assemblyResult.Notes = "SECURITY WARNING: Assembly hash does not match expected value";
                    allValid = false;
                }
            }
            catch (Exception ex)
            {
                // SECURITY FIX: Log verification failures securely
                assemblyResult.Status = VerificationStatus.Error;
                assemblyResult.Notes = $"Verification error: {ex.Message}";
                allValid = false;
            }

            result.AssemblyResults.Add(assemblyResult);
        }

        result.IsValid = allValid;
        result.VerificationTime = DateTime.UtcNow;
        LastVerificationResult = result;

        return allValid;
    }

    /// <summary>
    /// Computes the SHA-256 hash of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to hash.</param>
    /// <returns>The SHA-256 hash as an uppercase hexadecimal string.</returns>
    private static string ComputeAssemblyHash(Assembly assembly)
    {
        // SECURITY FIX: Use SHA-256 for strong hash verification
        var location = assembly.Location;

        if (string.IsNullOrEmpty(location) || !File.Exists(location))
        {
            throw new InvalidOperationException($"Cannot locate assembly file: {assembly.GetName().Name}");
        }

        using var stream = File.OpenRead(location);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);

        return Convert.ToHexString(hashBytes);
    }

    /// <summary>
    /// Registers an additional assembly for integrity verification.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <param name="expectedHash">The expected SHA-256 hash.</param>
    public static void RegisterAssembly(string assemblyName, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            throw new ArgumentException("Assembly name cannot be null or empty.", nameof(assemblyName));
        }

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            throw new ArgumentException("Expected hash cannot be null or empty.", nameof(expectedHash));
        }

        // SECURITY FIX: Allow registration but not modification of existing entries
        // This prevents attackers from replacing expected hashes at runtime
        if (!ExpectedHashes.ContainsKey(assemblyName))
        {
            ExpectedHashes[assemblyName] = expectedHash;
        }
    }

    /// <summary>
    /// Gets the list of assemblies registered for verification.
    /// </summary>
    /// <returns>A read-only collection of assembly names.</returns>
    public static IReadOnlyCollection<string> GetRegisteredAssemblies()
    {
        return ExpectedHashes.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Computes the hash of an assembly at the specified path.
    /// This can be used during build to generate expected hash values.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>The SHA-256 hash as an uppercase hexadecimal string.</returns>
    public static string ComputeHashForPath(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("Assembly file not found.", assemblyPath);
        }

        using var stream = File.OpenRead(assemblyPath);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);

        return Convert.ToHexString(hashBytes);
    }
}

/// <summary>
/// Result of an integrity verification operation.
/// </summary>
public class IntegrityVerificationResult
{
    /// <summary>
    /// Whether all assemblies passed verification.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Whether verification was skipped (e.g., in DEBUG mode).
    /// </summary>
    public bool VerificationSkipped { get; set; }

    /// <summary>
    /// Reason verification was skipped, if applicable.
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Time the verification was performed.
    /// </summary>
    public DateTime VerificationTime { get; set; }

    /// <summary>
    /// Individual results for each assembly.
    /// </summary>
    public List<AssemblyVerificationResult> AssemblyResults { get; } = new();
}

/// <summary>
/// Result of verifying a single assembly.
/// </summary>
public class AssemblyVerificationResult
{
    /// <summary>
    /// Name of the assembly.
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;

    /// <summary>
    /// The expected hash value.
    /// </summary>
    public string ExpectedHash { get; set; } = string.Empty;

    /// <summary>
    /// The actual computed hash value.
    /// </summary>
    public string? ActualHash { get; set; }

    /// <summary>
    /// The verification status.
    /// </summary>
    public VerificationStatus Status { get; set; }

    /// <summary>
    /// Additional notes about the verification result.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Status of assembly verification.
/// </summary>
public enum VerificationStatus
{
    /// <summary>
    /// Assembly hash matches expected value.
    /// </summary>
    Valid,

    /// <summary>
    /// Assembly hash does not match expected value - possible tampering.
    /// </summary>
    HashMismatch,

    /// <summary>
    /// Assembly was not found in the current AppDomain.
    /// </summary>
    NotFound,

    /// <summary>
    /// Verification was skipped (e.g., placeholder hash).
    /// </summary>
    Skipped,

    /// <summary>
    /// An error occurred during verification.
    /// </summary>
    Error
}
