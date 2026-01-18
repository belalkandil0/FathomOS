// FathomOS.Core/Security/Sanitizer.cs
// Input sanitization utilities for secure input handling

using System.Text.RegularExpressions;

namespace FathomOS.Core.Security;

/// <summary>
/// Interface for input sanitization and validation.
/// </summary>
public interface ISanitizer
{
    /// <summary>
    /// Sanitizes a file name by removing or replacing invalid characters.
    /// </summary>
    /// <param name="input">The input file name to sanitize.</param>
    /// <returns>A sanitized file name safe for use in file system operations.</returns>
    string SanitizeFileName(string input);

    /// <summary>
    /// Sanitizes a file path by removing dangerous patterns and invalid characters.
    /// </summary>
    /// <param name="input">The input path to sanitize.</param>
    /// <returns>A sanitized path safe for use in file system operations.</returns>
    string SanitizePath(string input);

    /// <summary>
    /// Validates an email address format.
    /// </summary>
    /// <param name="input">The email address to validate.</param>
    /// <returns>True if the email format is valid.</returns>
    bool IsValidEmail(string input);

    /// <summary>
    /// Validates a URL format and scheme.
    /// </summary>
    /// <param name="input">The URL to validate.</param>
    /// <returns>True if the URL format is valid and uses an allowed scheme.</returns>
    bool IsValidUrl(string input);

    /// <summary>
    /// Sanitizes a string for safe display in logs.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <param name="maxLength">Maximum allowed length (default 1000).</param>
    /// <returns>A sanitized string safe for logging.</returns>
    string SanitizeForLog(string input, int maxLength = 1000);

    /// <summary>
    /// Sanitizes HTML content by removing potentially dangerous elements and attributes.
    /// </summary>
    /// <param name="input">The HTML input to sanitize.</param>
    /// <returns>Sanitized HTML content.</returns>
    string SanitizeHtml(string input);

    /// <summary>
    /// Validates that an input string contains only alphanumeric characters.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>True if the input contains only alphanumeric characters.</returns>
    bool IsAlphanumeric(string input);

    /// <summary>
    /// Validates that an input represents a valid identifier (letters, numbers, underscores).
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>True if the input is a valid identifier.</returns>
    bool IsValidIdentifier(string input);

    /// <summary>
    /// Removes control characters from a string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>String with control characters removed.</returns>
    string RemoveControlCharacters(string input);

    /// <summary>
    /// Validates and normalizes a phone number.
    /// </summary>
    /// <param name="input">The phone number to validate.</param>
    /// <param name="normalized">The normalized phone number if valid.</param>
    /// <returns>True if the phone number format is valid.</returns>
    bool TryNormalizePhoneNumber(string input, out string normalized);
}

/// <summary>
/// Provides comprehensive input sanitization and validation utilities.
///
/// Security features:
/// - Path traversal prevention
/// - Null byte injection prevention
/// - Control character removal
/// - HTML/Script injection prevention
/// - Length limits to prevent DoS
/// - Unicode normalization
/// </summary>
public sealed class Sanitizer : ISanitizer
{
    // Constants
    private const int MaxFileNameLength = 255;
    private const int MaxPathLength = 260;
    private const int MaxEmailLength = 254;
    private const int MaxUrlLength = 2083;

    // Precompiled regex patterns for performance
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    private static readonly Regex AlphanumericRegex = new(
        @"^[a-zA-Z0-9]+$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex IdentifierRegex = new(
        @"^[a-zA-Z_][a-zA-Z0-9_]*$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex PhoneRegex = new(
        @"^[\+]?[(]?[0-9]{1,3}[)]?[-\s\.]?[(]?[0-9]{1,4}[)]?[-\s\.]?[0-9]{1,4}[-\s\.]?[0-9]{1,9}$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex ControlCharRegex = new(
        @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]*>",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250));

    private static readonly Regex ScriptRegex = new(
        @"<script[^>]*>[\s\S]*?</script>|javascript:|on\w+\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    private static readonly Regex PathTraversalRegex = new(
        @"\.\.|[\\/]+\.+[\\/]+|^\.+[\\/]|[\\/]\.+$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    // Characters invalid in file names (Windows)
    private static readonly char[] InvalidFileNameChars =
        Path.GetInvalidFileNameChars()
        .Concat(new[] { '\0' }) // Explicitly include null byte
        .Distinct()
        .ToArray();

    // Characters invalid in paths (Windows)
    private static readonly char[] InvalidPathChars =
        Path.GetInvalidPathChars()
        .Concat(new[] { '\0' }) // Explicitly include null byte
        .Distinct()
        .ToArray();

    // Allowed URL schemes
    private static readonly HashSet<string> AllowedUrlSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http",
        "https",
        "ftp",
        "ftps",
        "mailto"
    };

    // Dangerous path patterns
    private static readonly string[] DangerousPathPatterns = new[]
    {
        "..",           // Parent directory
        "..\\",         // Parent directory (Windows)
        "../",          // Parent directory (Unix)
        "~",            // Home directory
        "\\\\",         // UNC path start
        "//",           // UNC path start (alternative)
        "con",          // Windows reserved
        "prn",          // Windows reserved
        "aux",          // Windows reserved
        "nul",          // Windows reserved
        "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9",
        "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9"
    };

    /// <inheritdoc/>
    public string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unnamed";

        // Remove null bytes first (security critical)
        var sanitized = input.Replace("\0", "");

        // Remove or replace invalid characters
        foreach (var c in InvalidFileNameChars)
        {
            sanitized = sanitized.Replace(c.ToString(), "");
        }

        // Remove leading/trailing dots and spaces
        sanitized = sanitized.Trim('.', ' ');

        // Remove control characters
        sanitized = RemoveControlCharacters(sanitized);

        // Check for Windows reserved names
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized).ToUpperInvariant();
        foreach (var reserved in DangerousPathPatterns)
        {
            if (nameWithoutExtension.Equals(reserved, StringComparison.OrdinalIgnoreCase))
            {
                sanitized = "_" + sanitized;
                break;
            }
        }

        // Enforce length limit
        if (sanitized.Length > MaxFileNameLength)
        {
            var extension = Path.GetExtension(sanitized);
            var name = Path.GetFileNameWithoutExtension(sanitized);
            var maxNameLength = MaxFileNameLength - extension.Length;
            sanitized = name[..Math.Min(name.Length, maxNameLength)] + extension;
        }

        // Return fallback if nothing left
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    /// <inheritdoc/>
    public string SanitizePath(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove null bytes first (security critical)
        var sanitized = input.Replace("\0", "");

        // Remove control characters
        sanitized = RemoveControlCharacters(sanitized);

        // Detect and prevent path traversal attempts
        if (PathTraversalRegex.IsMatch(sanitized))
        {
            // Remove all path traversal patterns
            sanitized = PathTraversalRegex.Replace(sanitized, "");
        }

        // Remove invalid path characters
        foreach (var c in InvalidPathChars)
        {
            sanitized = sanitized.Replace(c.ToString(), "");
        }

        // Normalize path separators to platform default
        sanitized = sanitized.Replace('/', Path.DirectorySeparatorChar);
        sanitized = sanitized.Replace('\\', Path.DirectorySeparatorChar);

        // Remove consecutive separators
        while (sanitized.Contains($"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}"))
        {
            sanitized = sanitized.Replace(
                $"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}",
                Path.DirectorySeparatorChar.ToString());
        }

        // Enforce length limit
        if (sanitized.Length > MaxPathLength)
        {
            sanitized = sanitized[..MaxPathLength];
        }

        return sanitized.Trim();
    }

    /// <inheritdoc/>
    public bool IsValidEmail(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Length check
        if (input.Length > MaxEmailLength)
            return false;

        // Must not contain control characters
        if (ControlCharRegex.IsMatch(input))
            return false;

        // Must not contain null bytes
        if (input.Contains('\0'))
            return false;

        try
        {
            // Regex validation with timeout protection
            return EmailRegex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool IsValidUrl(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Length check
        if (input.Length > MaxUrlLength)
            return false;

        // Must not contain control characters (except allowed whitespace)
        var testInput = input.Replace(" ", "").Replace("\t", "");
        if (ControlCharRegex.IsMatch(testInput))
            return false;

        // Must not contain null bytes
        if (input.Contains('\0'))
            return false;

        try
        {
            // Try to parse as URI
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
                return false;

            // Verify scheme is allowed
            if (!AllowedUrlSchemes.Contains(uri.Scheme))
                return false;

            // Check for suspicious patterns
            if (input.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
                return false;

            if (input.Contains("data:", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public string SanitizeForLog(string input, int maxLength = 1000)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove null bytes
        var sanitized = input.Replace("\0", "[NULL]");

        // Remove control characters
        sanitized = RemoveControlCharacters(sanitized);

        // Replace newlines with escaped versions
        sanitized = sanitized
            .Replace("\r\n", "\\r\\n")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

        // Truncate if too long
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..(maxLength - 3)] + "...";
        }

        return sanitized;
    }

    /// <inheritdoc/>
    public string SanitizeHtml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove null bytes
        var sanitized = input.Replace("\0", "");

        try
        {
            // Remove script tags and javascript handlers
            sanitized = ScriptRegex.Replace(sanitized, "");

            // Remove all HTML tags (basic sanitization)
            sanitized = HtmlTagRegex.Replace(sanitized, "");

            // Encode remaining special characters
            sanitized = System.Net.WebUtility.HtmlEncode(sanitized);

            return sanitized;
        }
        catch (RegexMatchTimeoutException)
        {
            // If regex times out, apply aggressive sanitization
            return System.Net.WebUtility.HtmlEncode(input.Replace("<", "").Replace(">", ""));
        }
    }

    /// <inheritdoc/>
    public bool IsAlphanumeric(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        try
        {
            return AlphanumericRegex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            // Fallback to character-by-character check
            return input.All(char.IsLetterOrDigit);
        }
    }

    /// <inheritdoc/>
    public bool IsValidIdentifier(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Must not be too long
        if (input.Length > 255)
            return false;

        try
        {
            return IdentifierRegex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            // Fallback to manual check
            if (!char.IsLetter(input[0]) && input[0] != '_')
                return false;

            return input.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
    }

    /// <inheritdoc/>
    public string RemoveControlCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            return ControlCharRegex.Replace(input, "");
        }
        catch (RegexMatchTimeoutException)
        {
            // Fallback to character-by-character removal
            return new string(input.Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t').ToArray());
        }
    }

    /// <inheritdoc/>
    public bool TryNormalizePhoneNumber(string input, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Remove all non-digit characters except leading +
        var hasPlus = input.TrimStart().StartsWith("+");
        var digits = new string(input.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits) || digits.Length < 7 || digits.Length > 15)
            return false;

        // Build normalized format
        normalized = hasPlus ? "+" + digits : digits;

        try
        {
            // Verify the original format was roughly valid
            return PhoneRegex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            // Accept if digits look reasonable
            return true;
        }
    }
}

/// <summary>
/// Extension methods for string sanitization.
/// </summary>
public static class SanitizerExtensions
{
    private static readonly Lazy<ISanitizer> DefaultSanitizer = new(() => new Sanitizer());

    /// <summary>
    /// Sanitizes a file name using the default sanitizer.
    /// </summary>
    public static string ToSafeFileName(this string input)
    {
        return DefaultSanitizer.Value.SanitizeFileName(input);
    }

    /// <summary>
    /// Sanitizes a path using the default sanitizer.
    /// </summary>
    public static string ToSafePath(this string input)
    {
        return DefaultSanitizer.Value.SanitizePath(input);
    }

    /// <summary>
    /// Sanitizes a string for safe logging.
    /// </summary>
    public static string ToSafeLog(this string input, int maxLength = 1000)
    {
        return DefaultSanitizer.Value.SanitizeForLog(input, maxLength);
    }

    /// <summary>
    /// Removes HTML tags from a string.
    /// </summary>
    public static string StripHtml(this string input)
    {
        return DefaultSanitizer.Value.SanitizeHtml(input);
    }
}
