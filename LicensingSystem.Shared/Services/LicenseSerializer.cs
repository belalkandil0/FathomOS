// LicensingSystem.Shared/Services/LicenseSerializer.cs
// Serialization utilities for offline licenses (JSON and compact license key formats)
// Supports both .lic files and compact license key strings

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LicensingSystem.Shared.Models;

namespace LicensingSystem.Shared.Services;

/// <summary>
/// Serializes and deserializes offline licenses to various formats:
/// - JSON (.lic files) - Human-readable, full data
/// - License key strings - Compact, for manual entry
///
/// License Key Format: FATHOM-{EDITION}-{BASE64-DATA}-{CHECKSUM}
/// Example: FATHOM-PRO-H4sIAAAAAAAACqtWKkktLlGyUlAyNDJW0lFQSs7PLShKLS5RsgIAYLqYJxgAAAA-A7B2
/// </summary>
/// <example>
/// // Unit Test Example - JSON Serialization:
/// var license = new OfflineLicense { Id = "LIC-2026-0001" };
/// var json = LicenseSerializer.ToJson(license);
/// var restored = LicenseSerializer.FromJson(json);
/// Assert.Equal(license.Id, restored.Id);
///
/// // Unit Test Example - License Key:
/// var key = LicenseSerializer.ToLicenseKey(license);
/// Assert.StartsWith("FATHOM-", key);
/// var fromKey = LicenseSerializer.FromLicenseKey(key);
/// Assert.Equal(license.Id, fromKey.Id);
/// </example>
public static class LicenseSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region JSON Serialization

    /// <summary>
    /// Serializes an offline license to pretty-printed JSON.
    /// </summary>
    /// <param name="license">The license to serialize</param>
    /// <returns>JSON string representation</returns>
    public static string ToJson(OfflineLicense license)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));

        return JsonSerializer.Serialize(license, JsonOptions);
    }

    /// <summary>
    /// Serializes an offline license to compact JSON (no indentation).
    /// </summary>
    /// <param name="license">The license to serialize</param>
    /// <returns>Compact JSON string</returns>
    public static string ToCompactJson(OfflineLicense license)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));

        return JsonSerializer.Serialize(license, CompactJsonOptions);
    }

    /// <summary>
    /// Deserializes an offline license from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>Deserialized OfflineLicense</returns>
    /// <exception cref="LicenseSerializationException">If deserialization fails</exception>
    public static OfflineLicense FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentNullException(nameof(json));

        try
        {
            var license = JsonSerializer.Deserialize<OfflineLicense>(json, JsonOptions);
            if (license == null)
                throw new LicenseSerializationException("Deserialization returned null");

            return license;
        }
        catch (JsonException ex)
        {
            throw new LicenseSerializationException($"Invalid JSON format: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tries to deserialize an offline license from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="license">The resulting license if successful</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool TryFromJson(string json, out OfflineLicense? license)
    {
        license = null;
        try
        {
            license = FromJson(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region File Operations

    /// <summary>
    /// Saves an offline license to a JSON file (.lic).
    /// </summary>
    /// <param name="license">The license to save</param>
    /// <param name="path">File path (should end with .lic)</param>
    public static void SaveToFile(OfflineLicense license, string path)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = ToJson(license);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    /// <summary>
    /// Loads an offline license from a JSON file (.lic).
    /// </summary>
    /// <param name="path">File path to load from</param>
    /// <returns>Loaded OfflineLicense</returns>
    /// <exception cref="FileNotFoundException">If file doesn't exist</exception>
    /// <exception cref="LicenseSerializationException">If file is corrupted</exception>
    public static OfflineLicense LoadFromFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("License file not found", path);

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return FromJson(json);
        }
        catch (IOException ex)
        {
            throw new LicenseSerializationException($"Error reading license file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tries to load an offline license from a file.
    /// </summary>
    /// <param name="path">File path to load from</param>
    /// <param name="license">The resulting license if successful</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool TryLoadFromFile(string path, out OfflineLicense? license)
    {
        license = null;
        try
        {
            license = LoadFromFile(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves a license to file asynchronously.
    /// </summary>
    public static async Task SaveToFileAsync(OfflineLicense license, string path, CancellationToken cancellationToken = default)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = ToJson(license);
        await File.WriteAllTextAsync(path, json, Encoding.UTF8, cancellationToken);
    }

    /// <summary>
    /// Loads a license from file asynchronously.
    /// </summary>
    public static async Task<OfflineLicense> LoadFromFileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("License file not found", path);

        try
        {
            var json = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
            return FromJson(json);
        }
        catch (IOException ex)
        {
            throw new LicenseSerializationException($"Error reading license file: {ex.Message}", ex);
        }
    }

    #endregion

    #region License Key Serialization

    /// <summary>
    /// Converts an offline license to a compact license key string.
    /// Format: FATHOM-{EDITION}-{COMPRESSED-BASE64}-{CHECKSUM}
    /// Uses GZip compression + URL-safe Base64 encoding.
    /// </summary>
    /// <param name="license">The license to convert</param>
    /// <returns>Compact license key string</returns>
    /// <example>
    /// var key = LicenseSerializer.ToLicenseKey(license);
    /// // Output: FATHOM-PRO-H4sIAAAAA...base64...-A7B2
    /// </example>
    public static string ToLicenseKey(OfflineLicense license)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));

        // Get edition code (first 3 chars of edition)
        var edition = GetEditionCode(license.Product.Edition);

        // Serialize to compact JSON
        var json = ToCompactJson(license);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        // Compress with GZip
        byte[] compressedBytes;
        using (var outputStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
            {
                gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
            }
            compressedBytes = outputStream.ToArray();
        }

        // Convert to URL-safe Base64
        var base64 = Convert.ToBase64String(compressedBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        // Compute checksum (first 4 chars of SHA256)
        var checksum = ComputeChecksum(base64);

        // Format: FATHOM-{EDITION}-{DATA}-{CHECKSUM}
        return $"{OfflineLicenseConstants.LicenseKeyPrefix}-{edition}-{base64}-{checksum}";
    }

    /// <summary>
    /// Parses a compact license key string back to an OfflineLicense.
    /// </summary>
    /// <param name="key">The license key string</param>
    /// <returns>Parsed OfflineLicense</returns>
    /// <exception cref="LicenseSerializationException">If key format is invalid</exception>
    public static OfflineLicense FromLicenseKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        // Clean up the key
        key = key.Trim().ToUpperInvariant();

        // Parse format: FATHOM-{EDITION}-{DATA}-{CHECKSUM}
        var parts = key.Split('-');
        if (parts.Length < 4)
            throw new LicenseSerializationException("Invalid license key format. Expected: FATHOM-XXX-{data}-{checksum}");

        if (parts[0] != OfflineLicenseConstants.LicenseKeyPrefix)
            throw new LicenseSerializationException($"Invalid license key prefix. Expected: {OfflineLicenseConstants.LicenseKeyPrefix}");

        // Extract data and checksum (handle case where base64 might contain dashes)
        var checksum = parts[^1]; // Last part
        var edition = parts[1];   // Second part

        // Everything between edition and checksum is the data
        var dataStartIndex = key.IndexOf('-', key.IndexOf('-') + 1) + 1;
        var dataEndIndex = key.LastIndexOf('-');

        if (dataStartIndex >= dataEndIndex)
            throw new LicenseSerializationException("Invalid license key format: missing data segment");

        var base64Data = key.Substring(dataStartIndex, dataEndIndex - dataStartIndex);

        // Verify checksum
        var computedChecksum = ComputeChecksum(base64Data.ToUpperInvariant());
        if (checksum != computedChecksum)
            throw new LicenseSerializationException("License key checksum mismatch. Key may be corrupted or tampered.");

        try
        {
            // Convert from URL-safe Base64
            var paddedBase64 = base64Data
                .Replace('-', '+')
                .Replace('_', '/');

            // Add padding if needed
            var padLength = (4 - paddedBase64.Length % 4) % 4;
            paddedBase64 += new string('=', padLength);

            var compressedBytes = Convert.FromBase64String(paddedBase64);

            // Decompress with GZip
            byte[] jsonBytes;
            using (var inputStream = new MemoryStream(compressedBytes))
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                gzipStream.CopyTo(outputStream);
                jsonBytes = outputStream.ToArray();
            }

            var json = Encoding.UTF8.GetString(jsonBytes);
            return FromJson(json);
        }
        catch (Exception ex) when (ex is not LicenseSerializationException)
        {
            throw new LicenseSerializationException($"Failed to decode license key: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tries to parse a license key string.
    /// </summary>
    /// <param name="key">The license key string</param>
    /// <param name="license">The resulting license if successful</param>
    /// <returns>True if successful, false otherwise</returns>
    public static bool TryFromLicenseKey(string key, out OfflineLicense? license)
    {
        license = null;
        try
        {
            license = FromLicenseKey(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a license key format without fully parsing it.
    /// </summary>
    /// <param name="key">The license key to validate</param>
    /// <returns>True if format is valid</returns>
    public static bool ValidateLicenseKeyFormat(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        key = key.Trim().ToUpperInvariant();

        if (!key.StartsWith($"{OfflineLicenseConstants.LicenseKeyPrefix}-"))
            return false;

        var parts = key.Split('-');
        if (parts.Length < 4)
            return false;

        // Check checksum
        var checksum = parts[^1];
        if (checksum.Length != 4)
            return false;

        return true;
    }

    /// <summary>
    /// Formats a license key for display (with line breaks every 64 chars).
    /// </summary>
    /// <param name="key">The license key to format</param>
    /// <param name="lineLength">Characters per line (default 64)</param>
    /// <returns>Formatted license key</returns>
    public static string FormatLicenseKeyForDisplay(string key, int lineLength = 64)
    {
        if (string.IsNullOrEmpty(key))
            return key;

        var sb = new StringBuilder();
        for (int i = 0; i < key.Length; i += lineLength)
        {
            var length = Math.Min(lineLength, key.Length - i);
            if (i > 0) sb.AppendLine();
            sb.Append(key.Substring(i, length));
        }
        return sb.ToString();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a 3-character edition code from the full edition name.
    /// </summary>
    private static string GetEditionCode(string edition)
    {
        if (string.IsNullOrEmpty(edition))
            return "STD"; // Standard

        return edition.ToUpperInvariant() switch
        {
            "TRIAL" => "TRL",
            "BASIC" => "BAS",
            "STANDARD" => "STD",
            "PROFESSIONAL" => "PRO",
            "ENTERPRISE" => "ENT",
            "UNLIMITED" => "UNL",
            "LIFETIME" => "LIF",
            _ => edition.Length >= 3 ? edition[..3].ToUpperInvariant() : edition.ToUpperInvariant()
        };
    }

    /// <summary>
    /// Computes a 4-character checksum for license key validation.
    /// </summary>
    private static string ComputeChecksum(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..4];
    }

    /// <summary>
    /// Detects the format of a license string/file.
    /// </summary>
    /// <param name="content">License content (file contents or key string)</param>
    /// <returns>Detected format</returns>
    public static LicenseFormat DetectFormat(string content)
    {
        if (string.IsNullOrEmpty(content))
            return LicenseFormat.Unknown;

        content = content.Trim();

        // Check for JSON format
        if (content.StartsWith('{') && content.EndsWith('}'))
            return LicenseFormat.Json;

        // Check for license key format
        if (content.StartsWith($"{OfflineLicenseConstants.LicenseKeyPrefix}-", StringComparison.OrdinalIgnoreCase))
            return LicenseFormat.LicenseKey;

        return LicenseFormat.Unknown;
    }

    /// <summary>
    /// Auto-parses a license from either JSON or license key format.
    /// </summary>
    /// <param name="content">License content in any supported format</param>
    /// <returns>Parsed OfflineLicense</returns>
    public static OfflineLicense AutoParse(string content)
    {
        var format = DetectFormat(content);
        return format switch
        {
            LicenseFormat.Json => FromJson(content),
            LicenseFormat.LicenseKey => FromLicenseKey(content),
            _ => throw new LicenseSerializationException("Unable to detect license format")
        };
    }

    /// <summary>
    /// Tries to auto-parse a license from any supported format.
    /// </summary>
    public static bool TryAutoParse(string content, out OfflineLicense? license)
    {
        license = null;
        try
        {
            license = AutoParse(content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Supported license file/string formats
/// </summary>
public enum LicenseFormat
{
    /// <summary>
    /// Unknown or unsupported format
    /// </summary>
    Unknown,

    /// <summary>
    /// JSON format (.lic file)
    /// </summary>
    Json,

    /// <summary>
    /// Compact license key string
    /// </summary>
    LicenseKey
}

/// <summary>
/// Exception thrown when license serialization/deserialization fails
/// </summary>
public class LicenseSerializationException : Exception
{
    public LicenseSerializationException(string message) : base(message) { }
    public LicenseSerializationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Extension methods for license serialization
/// </summary>
public static class LicenseSerializerExtensions
{
    /// <summary>
    /// Saves the license to a file.
    /// </summary>
    public static void SaveTo(this OfflineLicense license, string path)
    {
        LicenseSerializer.SaveToFile(license, path);
    }

    /// <summary>
    /// Converts the license to a JSON string.
    /// </summary>
    public static string ToJson(this OfflineLicense license)
    {
        return LicenseSerializer.ToJson(license);
    }

    /// <summary>
    /// Converts the license to a compact license key.
    /// </summary>
    public static string ToLicenseKey(this OfflineLicense license)
    {
        return LicenseSerializer.ToLicenseKey(license);
    }

    /// <summary>
    /// Gets a formatted display string for the license.
    /// </summary>
    public static string GetDisplayInfo(this OfflineLicense license)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"License ID: {license.Id}");
        sb.AppendLine($"Product: {license.Product.Name} {license.Product.Edition}");
        sb.AppendLine($"Customer: {license.Client.Name} ({license.Client.Email})");
        sb.AppendLine($"Issued: {license.Terms.IssuedAt:yyyy-MM-dd}");

        if (license.IsLifetime)
            sb.AppendLine("Expires: Never (Lifetime)");
        else
            sb.AppendLine($"Expires: {license.Terms.ExpiresAt:yyyy-MM-dd} ({license.DaysUntilExpiry} days)");

        if (license.Modules.Any())
            sb.AppendLine($"Modules: {string.Join(", ", license.Modules)}");

        if (license.Features.Any())
            sb.AppendLine($"Features: {string.Join(", ", license.Features)}");

        return sb.ToString();
    }
}
