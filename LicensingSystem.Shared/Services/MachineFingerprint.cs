// LicensingSystem.Shared/Services/MachineFingerprint.cs
// Hardware fingerprint generation and verification for license binding
// Cross-platform compatible with primary support for Windows

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LicensingSystem.Shared.Services;

/// <summary>
/// Generates unique machine fingerprints from hardware identifiers for license binding.
/// Uses multiple hardware components with fuzzy matching to allow minor hardware changes.
///
/// Components collected:
/// - CPU ID (very stable)
/// - Motherboard Serial (very stable)
/// - BIOS Serial (very stable)
/// - System Drive Volume Serial (changes if drive replaced)
/// - Windows Machine GUID (stable until Windows reinstall)
///
/// Fuzzy Matching:
/// Default threshold is 3/5 - allows 2 components to change (e.g., RAM upgrade, GPU swap)
/// </summary>
/// <example>
/// // Unit Test Example - Generate Fingerprints:
/// var fingerprints = MachineFingerprint.Generate();
/// Assert.True(fingerprints.Count >= 3);
/// Assert.All(fingerprints, fp => Assert.Equal(32, fp.Length)); // SHA256 hex, 32 chars
///
/// // Unit Test Example - Verify Match:
/// var stored = MachineFingerprint.Generate();
/// var current = MachineFingerprint.Generate();
/// bool matches = MachineFingerprint.Verify(stored);
/// Assert.True(matches); // Same machine
/// </example>
public static class MachineFingerprint
{
    /// <summary>
    /// Number of fingerprint components collected
    /// </summary>
    public const int TotalComponents = 7;

    /// <summary>
    /// Default minimum matches required (3 of 5-7)
    /// </summary>
    public const int DefaultMatchThreshold = 3;

    /// <summary>
    /// Generates a list of hardware fingerprint hashes for the current machine.
    /// Each fingerprint is a 32-character uppercase hex string (SHA-256 hash).
    /// </summary>
    /// <returns>List of fingerprint hashes (typically 5-7 components)</returns>
    public static List<string> Generate()
    {
        var fingerprints = new List<string>();

        // Only supported on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // For non-Windows, use fallback identifiers
            fingerprints.AddRange(GenerateFallbackFingerprints());
            return fingerprints;
        }

        // CPU ID - Very stable
        TryAddFingerprint(fingerprints, "CPU", GetCpuId);

        // Motherboard Serial - Very stable
        TryAddFingerprint(fingerprints, "MB", GetMotherboardSerial);

        // BIOS Serial - Very stable
        TryAddFingerprint(fingerprints, "BIOS", GetBiosSerial);

        // System Drive Volume Serial - Changes if drive replaced
        TryAddFingerprint(fingerprints, "DRIVE", GetSystemDriveSerial);

        // Windows Machine GUID - Stable until Windows reinstall
        TryAddFingerprint(fingerprints, "WGUID", GetWindowsMachineGuid);

        // Windows Product ID - Tied to Windows installation
        TryAddFingerprint(fingerprints, "WPID", GetWindowsProductId);

        // Display Adapter ID - Can change with GPU swap
        TryAddFingerprint(fingerprints, "GPU", GetDisplayAdapterId);

        // Ensure we have at least some fingerprints
        if (fingerprints.Count == 0)
        {
            fingerprints.AddRange(GenerateFallbackFingerprints());
        }

        return fingerprints;
    }

    /// <summary>
    /// Generates a single combined fingerprint hash (for display purposes).
    /// Uses the most stable components: CPU + Motherboard + BIOS.
    /// </summary>
    /// <returns>Combined fingerprint hash (32 chars)</returns>
    public static string GeneratePrimary()
    {
        var components = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            TryCollect(() => GetCpuId(), components);
            TryCollect(() => GetMotherboardSerial(), components);
            TryCollect(() => GetBiosSerial(), components);
        }

        if (components.Count == 0)
        {
            // Fallback to machine name + user (less stable but available)
            components.Add(Environment.MachineName);
            components.Add(Environment.UserName);
        }

        var combined = string.Join("|", components);
        return ComputeHash($"PRIMARY:{combined}");
    }

    /// <summary>
    /// Generates a display-friendly hardware ID in format XXXX-XXXX-XXXX-XXXX.
    /// This is for user display only, not for license binding.
    /// </summary>
    /// <returns>Formatted hardware ID</returns>
    public static string GenerateDisplayId()
    {
        var primary = GeneratePrimary();
        if (primary.Length >= 16)
        {
            return $"{primary[..4]}-{primary[4..8]}-{primary[8..12]}-{primary[12..16]}".ToUpperInvariant();
        }
        return primary.ToUpperInvariant();
    }

    /// <summary>
    /// Verifies if the current machine matches a stored fingerprint set.
    /// Uses fuzzy matching with configurable threshold.
    /// </summary>
    /// <param name="storedFingerprints">Fingerprints stored in the license</param>
    /// <param name="threshold">Minimum matches required (default: 3)</param>
    /// <returns>True if enough fingerprints match</returns>
    public static bool Verify(List<string> storedFingerprints, int threshold = DefaultMatchThreshold)
    {
        if (storedFingerprints == null || storedFingerprints.Count == 0)
            return true; // No binding = always valid

        var currentFingerprints = Generate();
        var matchCount = CountMatches(storedFingerprints, currentFingerprints);

        return matchCount >= threshold;
    }

    /// <summary>
    /// Verifies if the current machine matches a single stored fingerprint.
    /// </summary>
    /// <param name="storedFingerprint">Single fingerprint to verify</param>
    /// <returns>True if the fingerprint is found in current machine</returns>
    public static bool Verify(string storedFingerprint)
    {
        if (string.IsNullOrEmpty(storedFingerprint))
            return true;

        var currentFingerprints = Generate();
        return currentFingerprints.Any(fp =>
            ConstantTimeCompare(fp, storedFingerprint));
    }

    /// <summary>
    /// Counts how many fingerprints match between stored and current.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    /// <param name="storedFingerprints">Fingerprints from license</param>
    /// <param name="currentFingerprints">Current machine fingerprints</param>
    /// <returns>Number of matching fingerprints</returns>
    public static int CountMatches(List<string> storedFingerprints, List<string> currentFingerprints)
    {
        if (storedFingerprints == null || currentFingerprints == null)
            return 0;

        int matchCount = 0;

        foreach (var stored in storedFingerprints)
        {
            foreach (var current in currentFingerprints)
            {
                if (ConstantTimeCompare(stored, current))
                {
                    matchCount++;
                    break; // Found match for this stored fingerprint
                }
            }
        }

        return matchCount;
    }

    /// <summary>
    /// Gets diagnostic information about the current machine's fingerprints.
    /// Useful for troubleshooting license binding issues.
    /// </summary>
    /// <returns>Diagnostic information string</returns>
    public static string GetDiagnostics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== MACHINE FINGERPRINT DIAGNOSTICS ===");
        sb.AppendLine($"Platform: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"Machine Name: {Environment.MachineName}");
        sb.AppendLine($"Display ID: {GenerateDisplayId()}");
        sb.AppendLine();
        sb.AppendLine("Components:");

        var fingerprints = Generate();
        for (int i = 0; i < fingerprints.Count; i++)
        {
            sb.AppendLine($"  [{i}] {fingerprints[i]}");
        }

        sb.AppendLine();
        sb.AppendLine($"Total Components: {fingerprints.Count}");
        sb.AppendLine($"Match Threshold: {DefaultMatchThreshold}");
        sb.AppendLine("=================================");

        return sb.ToString();
    }

    /// <summary>
    /// Compares stored fingerprints with current and provides detailed analysis.
    /// Useful for diagnosing "hardware mismatch" errors.
    /// </summary>
    /// <param name="storedFingerprints">Fingerprints stored in the license</param>
    /// <param name="threshold">Match threshold</param>
    /// <returns>Detailed comparison report</returns>
    public static string DiagnoseMatch(List<string> storedFingerprints, int threshold = DefaultMatchThreshold)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== FINGERPRINT MATCH ANALYSIS ===");

        var currentFingerprints = Generate();

        sb.AppendLine($"Stored Fingerprints: {storedFingerprints?.Count ?? 0}");
        sb.AppendLine($"Current Fingerprints: {currentFingerprints.Count}");
        sb.AppendLine($"Match Threshold: {threshold}");
        sb.AppendLine();

        int matchCount = 0;
        if (storedFingerprints != null)
        {
            sb.AppendLine("Stored -> Match Status:");
            foreach (var stored in storedFingerprints)
            {
                var found = currentFingerprints.Any(c => ConstantTimeCompare(stored, c));
                sb.AppendLine($"  {stored}: {(found ? "MATCH" : "NO MATCH")}");
                if (found) matchCount++;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"RESULT: {matchCount} matches out of {storedFingerprints?.Count ?? 0} stored");
        sb.AppendLine($"VERDICT: {(matchCount >= threshold ? "WOULD PASS" : "WOULD FAIL")}");
        sb.AppendLine("===================================");

        return sb.ToString();
    }

    #region Windows-specific Hardware Collection

    private static void TryAddFingerprint(List<string> fingerprints, string prefix, Func<string> getter)
    {
        try
        {
            var value = getter();
            if (!string.IsNullOrEmpty(value) && !IsPlaceholder(value))
            {
                fingerprints.Add(ComputeHash($"{prefix}:{value}"));
            }
        }
        catch
        {
            // Component unavailable, skip silently
        }
    }

    private static void TryCollect(Func<string> getter, List<string> values)
    {
        try
        {
            var value = getter();
            if (!string.IsNullOrEmpty(value) && !IsPlaceholder(value))
            {
                values.Add(value);
            }
        }
        catch { }
    }

    private static bool IsPlaceholder(string value)
    {
        // Common placeholder values from manufacturers
        var placeholders = new[]
        {
            "To be filled by O.E.M.",
            "Default string",
            "Not Specified",
            "None",
            "N/A",
            "0"
        };

        return placeholders.Any(p =>
            value.Equals(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetCpuId()
    {
        return GetWmiProperty("Win32_Processor", "ProcessorId");
    }

    private static string GetMotherboardSerial()
    {
        return GetWmiProperty("Win32_BaseBoard", "SerialNumber");
    }

    private static string GetBiosSerial()
    {
        return GetWmiProperty("Win32_BIOS", "SerialNumber");
    }

    private static string GetSystemDriveSerial()
    {
        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
        if (string.IsNullOrEmpty(systemDrive))
            return string.Empty;

        var driveLetter = systemDrive.TrimEnd('\\', ':');

        return GetWmiPropertyWithQuery(
            "Win32_LogicalDisk",
            "VolumeSerialNumber",
            $"DeviceID='{driveLetter}:'");
    }

    private static string GetWindowsMachineGuid()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid")?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetWindowsProductId()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return key?.GetValue("ProductId")?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetDisplayAdapterId()
    {
        return GetWmiProperty("Win32_VideoController", "PNPDeviceID");
    }

    private static string GetWmiProperty(string wmiClass, string propertyName)
    {
        // Use System.Management for WMI queries (Windows only)
        try
        {
            var query = $"SELECT {propertyName} FROM {wmiClass}";
            using var searcher = new System.Management.ManagementObjectSearcher(query);
            foreach (var obj in searcher.Get())
            {
                var value = obj[propertyName]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }
        catch
        {
            // WMI not available
        }
        return string.Empty;
    }

    private static string GetWmiPropertyWithQuery(string wmiClass, string propertyName, string whereClause)
    {
        try
        {
            var query = $"SELECT {propertyName} FROM {wmiClass} WHERE {whereClause}";
            using var searcher = new System.Management.ManagementObjectSearcher(query);
            foreach (var obj in searcher.Get())
            {
                var value = obj[propertyName]?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        catch
        {
            // WMI not available
        }
        return string.Empty;
    }

    #endregion

    #region Fallback Fingerprints

    private static List<string> GenerateFallbackFingerprints()
    {
        var fingerprints = new List<string>();

        // Machine name-based fallback (less stable but always available)
        if (!string.IsNullOrEmpty(Environment.MachineName))
        {
            fingerprints.Add(ComputeHash($"MACHINE:{Environment.MachineName}"));
        }

        // User profile path (stable for user)
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            fingerprints.Add(ComputeHash($"USERPATH:{userProfile}"));
        }

        // OS description
        fingerprints.Add(ComputeHash($"OS:{RuntimeInformation.OSDescription}"));

        return fingerprints;
    }

    #endregion

    #region Cryptographic Helpers

    /// <summary>
    /// Computes SHA-256 hash and returns first 32 chars (128 bits).
    /// </summary>
    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..32]; // First 32 chars (128 bits)
    }

    /// <summary>
    /// Performs constant-time string comparison to prevent timing attacks.
    /// </summary>
    private static bool ConstantTimeCompare(string a, string b)
    {
        if (a == null || b == null)
            return a == b;

        var aUpper = a.ToUpperInvariant();
        var bUpper = b.ToUpperInvariant();

        if (aUpper.Length != bUpper.Length)
            return false;

        var aBytes = Encoding.UTF8.GetBytes(aUpper);
        var bBytes = Encoding.UTF8.GetBytes(bUpper);

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    #endregion
}

/// <summary>
/// Detailed fingerprint entry with component information (for diagnostics).
/// </summary>
public class FingerprintComponent
{
    /// <summary>
    /// Component name (e.g., "CPU", "Motherboard")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Raw value before hashing (for diagnostics, not stored in license)
    /// </summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>
    /// Hashed fingerprint value (stored in license)
    /// </summary>
    public string HashedValue { get; set; } = string.Empty;

    /// <summary>
    /// Whether this component is considered stable
    /// </summary>
    public bool IsStable { get; set; }

    /// <summary>
    /// Whether this component was successfully collected
    /// </summary>
    public bool IsAvailable { get; set; }
}

/// <summary>
/// Extension methods for fingerprint operations.
/// </summary>
public static class MachineFingerprintExtensions
{
    /// <summary>
    /// Validates that a license's hardware binding matches the current machine.
    /// </summary>
    public static bool ValidateHardwareBinding(
        this LicensingSystem.Shared.Models.OfflineLicense license)
    {
        if (license?.Binding?.HardwareFingerprints == null ||
            license.Binding.HardwareFingerprints.Count == 0)
        {
            return true; // No binding = always valid
        }

        return MachineFingerprint.Verify(
            license.Binding.HardwareFingerprints,
            license.Binding.MatchThreshold);
    }

    /// <summary>
    /// Gets the match count for a license against current hardware.
    /// </summary>
    public static int GetHardwareMatchCount(
        this LicensingSystem.Shared.Models.OfflineLicense license)
    {
        if (license?.Binding?.HardwareFingerprints == null ||
            license.Binding.HardwareFingerprints.Count == 0)
        {
            return 0;
        }

        var currentFingerprints = MachineFingerprint.Generate();
        return MachineFingerprint.CountMatches(
            license.Binding.HardwareFingerprints,
            currentFingerprints);
    }
}
