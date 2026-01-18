namespace FathomOS.Core.Interfaces;

/// <summary>
/// File service interface providing async file operations with proper error handling,
/// path validation, and temporary file management.
/// </summary>
public interface IFileService
{
    #region Read Operations

    /// <summary>
    /// Reads all text from a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File contents as string.</returns>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all text from a file synchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File contents as string.</returns>
    string ReadAllText(string path);

    /// <summary>
    /// Reads all bytes from a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File contents as byte array.</returns>
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all bytes from a file synchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File contents as byte array.</returns>
    byte[] ReadAllBytes(string path);

    /// <summary>
    /// Reads all lines from a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of lines.</returns>
    Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a file for reading as a stream.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>Read stream.</returns>
    Stream OpenRead(string path);

    /// <summary>
    /// Reads lines from a file lazily (for large files).
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>Enumerable of lines.</returns>
    IEnumerable<string> ReadLines(string path);

    #endregion

    #region Write Operations

    /// <summary>
    /// Writes text to a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="content">Text content to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes text to a file synchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="content">Text content to write.</param>
    void WriteAllText(string path, string content);

    /// <summary>
    /// Writes bytes to a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="data">Byte array to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes bytes to a file synchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="data">Byte array to write.</param>
    void WriteAllBytes(string path, byte[] data);

    /// <summary>
    /// Writes lines to a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="lines">Lines to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAllLinesAsync(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends text to a file asynchronously.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="content">Text to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AppendAllTextAsync(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a file for writing as a stream.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="append">Whether to append to existing file.</param>
    /// <returns>Write stream.</returns>
    Stream OpenWrite(string path, bool append = false);

    #endregion

    #region File Operations

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>True if file exists.</returns>
    bool Exists(string path);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="path">File path.</param>
    void Delete(string path);

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>True if file was deleted.</returns>
    bool DeleteIfExists(string path);

    /// <summary>
    /// Copies a file to a new location.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="destPath">Destination file path.</param>
    /// <param name="overwrite">Whether to overwrite existing file.</param>
    void Copy(string sourcePath, string destPath, bool overwrite = false);

    /// <summary>
    /// Copies a file asynchronously.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="destPath">Destination file path.</param>
    /// <param name="overwrite">Whether to overwrite existing file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CopyAsync(string sourcePath, string destPath, bool overwrite = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file to a new location.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="destPath">Destination file path.</param>
    /// <param name="overwrite">Whether to overwrite existing file.</param>
    void Move(string sourcePath, string destPath, bool overwrite = false);

    /// <summary>
    /// Gets file information.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File information.</returns>
    FileDetails GetFileInfo(string path);

    #endregion

    #region Directory Operations

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="path">Directory path.</param>
    void EnsureDirectoryExists(string path);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <returns>True if directory exists.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Creates a directory.
    /// </summary>
    /// <param name="path">Directory path.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Deletes a directory and all its contents.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <param name="recursive">Whether to delete contents recursively.</param>
    void DeleteDirectory(string path, bool recursive = true);

    /// <summary>
    /// Gets all files in a directory.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <param name="searchPattern">Search pattern (e.g., "*.txt").</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>Array of file paths.</returns>
    string[] GetFiles(string path, string searchPattern = "*", bool recursive = false);

    /// <summary>
    /// Gets all directories in a directory.
    /// </summary>
    /// <param name="path">Directory path.</param>
    /// <param name="searchPattern">Search pattern.</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>Array of directory paths.</returns>
    string[] GetDirectories(string path, string searchPattern = "*", bool recursive = false);

    #endregion

    #region Temporary Files

    /// <summary>
    /// Gets a unique temporary file path with the specified extension.
    /// </summary>
    /// <param name="extension">File extension (with or without leading dot).</param>
    /// <returns>Temporary file path.</returns>
    string GetTempFilePath(string extension = ".tmp");

    /// <summary>
    /// Gets the temporary directory path for FathomOS.
    /// </summary>
    /// <returns>Temp directory path.</returns>
    string GetTempDirectory();

    /// <summary>
    /// Creates a temporary file and returns a disposable handle.
    /// The file is deleted when the handle is disposed.
    /// </summary>
    /// <param name="extension">File extension.</param>
    /// <returns>Disposable temporary file handle.</returns>
    ITempFile CreateTempFile(string extension = ".tmp");

    /// <summary>
    /// Cleans up old temporary files.
    /// </summary>
    /// <param name="maxAge">Maximum age of files to keep.</param>
    void CleanupTempFiles(TimeSpan maxAge);

    #endregion

    #region Path Utilities

    /// <summary>
    /// Combines path segments.
    /// </summary>
    /// <param name="paths">Path segments.</param>
    /// <returns>Combined path.</returns>
    string CombinePaths(params string[] paths);

    /// <summary>
    /// Gets the file name from a path.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File name with extension.</returns>
    string GetFileName(string path);

    /// <summary>
    /// Gets the file name without extension.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File name without extension.</returns>
    string GetFileNameWithoutExtension(string path);

    /// <summary>
    /// Gets the file extension.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>File extension (with dot).</returns>
    string GetExtension(string path);

    /// <summary>
    /// Gets the directory name from a path.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <returns>Directory path.</returns>
    string? GetDirectoryName(string path);

    /// <summary>
    /// Changes the extension of a file path.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="newExtension">New extension.</param>
    /// <returns>Path with new extension.</returns>
    string ChangeExtension(string path, string newExtension);

    /// <summary>
    /// Gets the full path from a relative path.
    /// </summary>
    /// <param name="path">Relative or absolute path.</param>
    /// <returns>Full absolute path.</returns>
    string GetFullPath(string path);

    /// <summary>
    /// Validates a file path for invalid characters.
    /// </summary>
    /// <param name="path">Path to validate.</param>
    /// <returns>True if path is valid.</returns>
    bool IsValidPath(string path);

    /// <summary>
    /// Sanitizes a file name by removing invalid characters.
    /// </summary>
    /// <param name="fileName">File name to sanitize.</param>
    /// <returns>Sanitized file name.</returns>
    string SanitizeFileName(string fileName);

    #endregion
}

/// <summary>
/// Interface for a disposable temporary file.
/// </summary>
public interface ITempFile : IDisposable
{
    /// <summary>
    /// Gets the path to the temporary file.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets a stream for writing to the file.
    /// </summary>
    Stream GetWriteStream();

    /// <summary>
    /// Gets a stream for reading from the file.
    /// </summary>
    Stream GetReadStream();

    /// <summary>
    /// Keeps the file (prevents deletion on dispose).
    /// </summary>
    void Keep();
}

/// <summary>
/// File information details.
/// </summary>
public class FileDetails
{
    /// <summary>
    /// Full path to the file.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// File name with extension.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File extension.
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Directory containing the file.
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Creation time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification time.
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Last access time.
    /// </summary>
    public DateTime AccessedAt { get; set; }

    /// <summary>
    /// Whether the file is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether the file exists.
    /// </summary>
    public bool Exists { get; set; }
}
