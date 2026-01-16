// LicensingSystem.Client/HardwareFingerprint.cs
// Generates hardware fingerprints with fuzzy matching support

using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace LicensingSystem.Client;

/// <summary>
/// Generates hardware fingerprints for license binding.
/// Uses multiple components with fuzzy matching to allow minor hardware changes.
/// </summary>
public class HardwareFingerprint
{
    private readonly Dictionary<string, string> _components = new();

    /// <summary>
    /// Collects all hardware identifiers
    /// </summary>
    public HardwareFingerprint()
    {
        CollectAllFingerprints();
    }

    /// <summary>
    /// Gets all fingerprint hashes for license binding
    /// </summary>
    public List<string> GetFingerprints()
    {
        return _components.Values.ToList();
    }

    /// <summary>
    /// Gets the primary (most stable) fingerprint
    /// </summary>
    public string GetPrimaryFingerprint()
    {
        // Combine the most stable components
        var stableComponents = new[]
        {
            _components.GetValueOrDefault("CPU", ""),
            _components.GetValueOrDefault("Motherboard", ""),
            _components.GetValueOrDefault("BIOS", "")
        };

        var combined = string.Join("|", stableComponents.Where(c => !string.IsNullOrEmpty(c)));
        
        // BUG FIX: If no stable components, use any available component
        if (string.IsNullOrEmpty(combined))
        {
            combined = string.Join("|", _components.Values.Take(3));
        }
        
        // BUG FIX: If still empty, generate a fallback based on machine name
        if (string.IsNullOrEmpty(combined))
        {
            combined = $"FALLBACK:{Environment.MachineName}:{Environment.UserName}";
        }
        
        return ComputeHash(combined);
    }

    /// <summary>
    /// Gets a display-friendly HWID for the user
    /// </summary>
    public string GetDisplayHwid()
    {
        var primary = GetPrimaryFingerprint();
        // Format as: XXXX-XXXX-XXXX-XXXX for user display
        if (primary.Length >= 16)
        {
            return $"{primary[..4]}-{primary[4..8]}-{primary[8..12]}-{primary[12..16]}".ToUpperInvariant();
        }
        return primary.ToUpperInvariant();
    }

    /// <summary>
    /// Checks how many fingerprints match the stored ones
    /// </summary>
    public static int CountMatches(List<string> storedFingerprints, List<string> currentFingerprints)
    {
        return storedFingerprints.Count(stored => 
            currentFingerprints.Any(current => 
                string.Equals(stored, current, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Validates if current hardware matches stored fingerprints with threshold
    /// </summary>
    public static bool ValidateWithThreshold(
        List<string> storedFingerprints, 
        List<string> currentFingerprints, 
        int threshold)
    {
        var matches = CountMatches(storedFingerprints, currentFingerprints);
        return matches >= threshold;
    }

    private void CollectAllFingerprints()
    {
        // CPU ID - Very stable
        try
        {
            var cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
            if (!string.IsNullOrEmpty(cpuId))
                _components["CPU"] = ComputeHash($"CPU:{cpuId}");
        }
        catch { /* Component unavailable */ }

        // Motherboard Serial - Very stable
        try
        {
            var mbSerial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
            if (!string.IsNullOrEmpty(mbSerial) && mbSerial != "To be filled by O.E.M.")
                _components["Motherboard"] = ComputeHash($"MB:{mbSerial}");
        }
        catch { /* Component unavailable */ }

        // BIOS Serial - Very stable
        try
        {
            var biosSerial = GetWmiProperty("Win32_BIOS", "SerialNumber");
            if (!string.IsNullOrEmpty(biosSerial) && biosSerial != "To be filled by O.E.M.")
                _components["BIOS"] = ComputeHash($"BIOS:{biosSerial}");
        }
        catch { /* Component unavailable */ }

        // System Drive Volume Serial - Changes if drive replaced
        try
        {
            var driveSerial = GetSystemDriveSerial();
            if (!string.IsNullOrEmpty(driveSerial))
                _components["SystemDrive"] = ComputeHash($"DRIVE:{driveSerial}");
        }
        catch { /* Component unavailable */ }

        // Windows Machine GUID - Stable until Windows reinstall
        try
        {
            var machineGuid = GetWindowsMachineGuid();
            if (!string.IsNullOrEmpty(machineGuid))
                _components["MachineGuid"] = ComputeHash($"WGUID:{machineGuid}");
        }
        catch { /* Component unavailable */ }

        // Windows Product ID - Tied to Windows installation
        try
        {
            var productId = GetWindowsProductId();
            if (!string.IsNullOrEmpty(productId))
                _components["WindowsProduct"] = ComputeHash($"WPID:{productId}");
        }
        catch { /* Component unavailable */ }

        // Display Adapter ID - Can change
        try
        {
            var gpuId = GetWmiProperty("Win32_VideoController", "PNPDeviceID");
            if (!string.IsNullOrEmpty(gpuId))
                _components["GPU"] = ComputeHash($"GPU:{gpuId}");
        }
        catch { /* Component unavailable */ }
    }

    private static string GetWmiProperty(string wmiClass, string propertyName)
    {
        using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {wmiClass}");
        foreach (var obj in searcher.Get())
        {
            var value = obj[propertyName]?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return string.Empty;
    }

    private static string GetSystemDriveSerial()
    {
        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory);
        if (string.IsNullOrEmpty(systemDrive)) return string.Empty;

        var driveLetter = systemDrive.TrimEnd('\\', ':');
        
        using var searcher = new ManagementObjectSearcher(
            $"SELECT VolumeSerialNumber FROM Win32_LogicalDisk WHERE DeviceID='{driveLetter}:'");
        
        foreach (var obj in searcher.Get())
        {
            var serial = obj["VolumeSerialNumber"]?.ToString();
            if (!string.IsNullOrEmpty(serial))
                return serial;
        }
        return string.Empty;
    }

    private static string GetWindowsMachineGuid()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
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
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            return key?.GetValue("ProductId")?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..32]; // First 32 chars (128 bits)
    }
}

/// <summary>
/// Extended fingerprint with component names for debugging
/// </summary>
public class DetailedFingerprint
{
    public string ComponentName { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
    public string HashedValue { get; set; } = string.Empty;
    public bool IsStable { get; set; }
}
