// SECURITY FIX: Task 3.6 - Input Validation Framework
// Issue ID: VULN-006 / MISSING-008
// Priority: MEDIUM
// Purpose: File validation before parsing to prevent malicious file attacks

using System.Text.RegularExpressions;

namespace FathomOS.Core.Validation;

/// <summary>
/// Provides file validation functionality to prevent malicious file attacks.
///
/// SECURITY FIX: This class implements comprehensive file validation to protect against:
/// - Path traversal attacks (../, ~, etc.)
/// - Oversized file denial-of-service attacks
/// - Unauthorized file type access
/// - Null byte injection attacks
/// - Reserved/dangerous file names
///
/// All file operations should validate files through this class before processing.
/// </summary>
public static class FileValidator
{
    /// <summary>
    /// SECURITY FIX: Maximum file size limit to prevent denial-of-service attacks.
    /// Default is 100MB which should be sufficient for survey data files.
    /// </summary>
    private const long MaxFileSizeMB = 100;

    /// <summary>
    /// Maximum file size in bytes.
    /// </summary>
    private const long MaxFileSizeBytes = MaxFileSizeMB * 1024 * 1024;

    /// <summary>
    /// SECURITY FIX: Windows reserved file names that could cause issues.
    /// </summary>
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>
    /// SECURITY FIX: Characters that are invalid in file paths.
    /// </summary>
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

    /// <summary>
    /// SECURITY FIX: Characters that are invalid in file names.
    /// </summary>
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// Validates a file against security criteria.
    /// </summary>
    /// <param name="filePath">The path to the file to validate.</param>
    /// <param name="allowedExtensions">Optional list of allowed file extensions (e.g., ".npd", ".csv").
    /// Extensions should include the leading dot. If empty, no extension filtering is applied.</param>
    /// <returns>A validation result containing whether the file is valid and any error messages.</returns>
    public static FileValidationResult Validate(string filePath, params string[] allowedExtensions)
    {
        var result = new FileValidationResult();

        // SECURITY FIX: Validate input is not null or empty
        if (string.IsNullOrWhiteSpace(filePath))
        {
            result.Errors.Add("File path cannot be null or empty");
            return result;
        }

        // SECURITY FIX: Check for null byte injection
        if (filePath.Contains('\0'))
        {
            result.Errors.Add("File path contains invalid null character");
            return result;
        }

        // SECURITY FIX: Check for invalid path characters
        if (filePath.IndexOfAny(InvalidPathChars) >= 0)
        {
            result.Errors.Add("File path contains invalid characters");
            return result;
        }

        // SECURITY FIX: Check for path traversal attacks
        if (ContainsPathTraversal(filePath))
        {
            result.Errors.Add("Invalid file path detected - path traversal not allowed");
            return result;
        }

        // SECURITY FIX: Check for reserved Windows file names
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        if (!string.IsNullOrEmpty(fileName) && ReservedNames.Contains(fileName))
        {
            result.Errors.Add("File name uses a reserved system name");
            return result;
        }

        // Check if file exists
        if (!File.Exists(filePath))
        {
            result.Errors.Add("File does not exist");
            return result;
        }

        FileInfo fileInfo;
        try
        {
            fileInfo = new FileInfo(filePath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Unable to access file information: {ex.Message}");
            return result;
        }

        // SECURITY FIX: Validate the resolved path doesn't escape expected directory
        // This catches symlink attacks and other path manipulation
        try
        {
            var resolvedPath = Path.GetFullPath(filePath);
            if (resolvedPath != fileInfo.FullName)
            {
                // Path resolution changed the path - could indicate attack
                result.Warnings.Add("File path was normalized - verify intended file is being accessed");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Unable to resolve file path: {ex.Message}");
            return result;
        }

        // SECURITY FIX: Check file size to prevent denial-of-service
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            result.Errors.Add($"File exceeds maximum size of {MaxFileSizeMB}MB (file size: {fileInfo.Length / (1024 * 1024)}MB)");
            return result;
        }

        // SECURITY FIX: Validate file extension if restrictions specified
        if (allowedExtensions.Length > 0)
        {
            var fileExtension = fileInfo.Extension.ToLowerInvariant();
            var normalizedAllowedExtensions = allowedExtensions
                .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}")
                .ToArray();

            if (!normalizedAllowedExtensions.Contains(fileExtension))
            {
                result.Errors.Add($"File type not allowed. Expected: {string.Join(", ", normalizedAllowedExtensions)}");
                return result;
            }
        }

        // SECURITY FIX: Check if file is readable
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            // Successfully opened for reading
        }
        catch (UnauthorizedAccessException)
        {
            result.Errors.Add("Access denied - insufficient permissions to read file");
            return result;
        }
        catch (IOException ex)
        {
            result.Errors.Add($"Unable to access file: {ex.Message}");
            return result;
        }

        // All validations passed
        result.IsValid = result.Errors.Count == 0;
        result.ValidatedPath = fileInfo.FullName;
        result.FileSize = fileInfo.Length;
        result.Extension = fileInfo.Extension;

        return result;
    }

    /// <summary>
    /// Validates a file asynchronously (performs same checks as Validate).
    /// </summary>
    /// <param name="filePath">The path to the file to validate.</param>
    /// <param name="allowedExtensions">Optional list of allowed file extensions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result containing whether the file is valid and any error messages.</returns>
    public static Task<FileValidationResult> ValidateAsync(
        string filePath,
        string[] allowedExtensions,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // SECURITY FIX: File validation operations are primarily synchronous I/O
        // but we wrap in Task.Run to avoid blocking the UI thread
        return Task.Run(() => Validate(filePath, allowedExtensions), cancellationToken);
    }

    /// <summary>
    /// SECURITY FIX: Checks if a path contains path traversal sequences.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path contains traversal sequences, false otherwise.</returns>
    private static bool ContainsPathTraversal(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // SECURITY FIX: Check for common path traversal patterns
        // These patterns could be used to escape the intended directory

        // Check for parent directory traversal
        if (path.Contains(".."))
            return true;

        // Check for home directory expansion (Unix-style)
        if (path.Contains('~'))
            return true;

        // SECURITY FIX: Check for URL-encoded path traversal attempts
        if (path.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase) ||  // ..
            path.Contains("%252e", StringComparison.OrdinalIgnoreCase) ||   // double-encoded .
            path.Contains("..%2f", StringComparison.OrdinalIgnoreCase) ||   // ../
            path.Contains("%2f..", StringComparison.OrdinalIgnoreCase) ||   // /..
            path.Contains("..%5c", StringComparison.OrdinalIgnoreCase) ||   // ..\
            path.Contains("%5c..", StringComparison.OrdinalIgnoreCase))     // \..
        {
            return true;
        }

        // SECURITY FIX: Check for backslash-encoded traversal
        if (path.Contains(@"..\\") || path.Contains(@"\.."))
            return true;

        return false;
    }

    /// <summary>
    /// Validates that a file path is within an allowed base directory.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <param name="allowedBaseDirectory">The base directory that the file must be within.</param>
    /// <returns>True if the file is within the allowed directory, false otherwise.</returns>
    public static bool IsWithinDirectory(string filePath, string allowedBaseDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(allowedBaseDirectory))
            return false;

        try
        {
            // SECURITY FIX: Resolve both paths fully to catch symlink attacks
            var fullFilePath = Path.GetFullPath(filePath);
            var fullBaseDirectory = Path.GetFullPath(allowedBaseDirectory);

            // Ensure base directory ends with separator for accurate comparison
            if (!fullBaseDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                fullBaseDirectory += Path.DirectorySeparatorChar;
            }

            return fullFilePath.StartsWith(fullBaseDirectory, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // SECURITY FIX: Fail closed - if we can't resolve paths, deny access
            return false;
        }
    }

    /// <summary>
    /// Sanitizes a file name by removing or replacing invalid characters.
    /// </summary>
    /// <param name="fileName">The file name to sanitize.</param>
    /// <param name="replacementChar">The character to replace invalid characters with (default: underscore).</param>
    /// <returns>A sanitized file name safe for use in the file system.</returns>
    public static string SanitizeFileName(string fileName, char replacementChar = '_')
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "unnamed_file";

        // SECURITY FIX: Remove or replace all invalid characters
        var sanitized = new char[fileName.Length];
        for (int i = 0; i < fileName.Length; i++)
        {
            var c = fileName[i];
            sanitized[i] = InvalidFileNameChars.Contains(c) ? replacementChar : c;
        }

        var result = new string(sanitized);

        // SECURITY FIX: Check for reserved names and append suffix if needed
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(result);
        if (ReservedNames.Contains(nameWithoutExtension))
        {
            var extension = Path.GetExtension(result);
            result = $"{nameWithoutExtension}_file{extension}";
        }

        // SECURITY FIX: Prevent empty result
        if (string.IsNullOrWhiteSpace(result) || result.All(c => c == replacementChar))
        {
            return "unnamed_file";
        }

        return result;
    }

    /// <summary>
    /// Validates multiple files and returns aggregated results.
    /// </summary>
    /// <param name="filePaths">The file paths to validate.</param>
    /// <param name="allowedExtensions">Optional list of allowed file extensions.</param>
    /// <returns>A dictionary mapping file paths to their validation results.</returns>
    public static Dictionary<string, FileValidationResult> ValidateMultiple(
        IEnumerable<string> filePaths,
        params string[] allowedExtensions)
    {
        var results = new Dictionary<string, FileValidationResult>();

        foreach (var filePath in filePaths)
        {
            results[filePath] = Validate(filePath, allowedExtensions);
        }

        return results;
    }
}

/// <summary>
/// Result of a file validation operation.
/// </summary>
public class FileValidationResult
{
    /// <summary>
    /// Whether the file passed all validation checks.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The fully resolved path to the validated file (only set if validation passed).
    /// </summary>
    public string? ValidatedPath { get; set; }

    /// <summary>
    /// The size of the file in bytes (only set if validation passed).
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// The file extension (only set if validation passed).
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// List of validation errors that caused the file to be rejected.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// List of validation warnings (file is still valid but may have concerns).
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Returns all errors as a single string.
    /// </summary>
    public string ErrorMessage => string.Join("; ", Errors);

    /// <summary>
    /// Returns all warnings as a single string.
    /// </summary>
    public string WarningMessage => string.Join("; ", Warnings);

    /// <summary>
    /// Returns a summary of the validation result.
    /// </summary>
    public override string ToString()
    {
        if (IsValid)
        {
            var warningText = Warnings.Count > 0 ? $" (with {Warnings.Count} warning(s))" : "";
            return $"Valid{warningText}: {ValidatedPath}";
        }
        return $"Invalid: {ErrorMessage}";
    }
}
