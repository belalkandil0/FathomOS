using System.Text;

namespace FathomOS.Core.IO;

/// <summary>
/// Helper class for async file operations using true async I/O with FileOptions.Asynchronous.
/// Provides consistent async wrappers for common file operations with proper cancellation support.
/// </summary>
public static class AsyncFileHelper
{
    /// <summary>
    /// Default buffer size for file operations (81920 bytes = 80 KB).
    /// </summary>
    private const int DefaultBufferSize = 81920;

    /// <summary>
    /// Reads all text from a file asynchronously using true async I/O.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous read operation. The value contains the contents of the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified in <paramref name="path"/> was not found.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task<string> ReadAllTextAsync(string path, CancellationToken ct = default)
    {
        ValidatePath(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The file '{path}' was not found.", path);
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: DefaultBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes text to a file asynchronously using true async I/O.
    /// Creates the file if it doesn't exist, or overwrites it if it does.
    /// </summary>
    /// <param name="path">The path to the file to write.</param>
    /// <param name="content">The content to write to the file.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task WriteAllTextAsync(string path, string content, CancellationToken ct = default)
    {
        ValidatePath(path);
        content ??= string.Empty;

        await EnsureDirectoryExistsAsync(Path.GetDirectoryName(path)!).ConfigureAwait(false);

        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: DefaultBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(content.AsMemory(), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads all bytes from a file asynchronously using true async I/O.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous read operation. The value contains the byte array of the file contents.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified in <paramref name="path"/> was not found.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        ValidatePath(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The file '{path}' was not found.", path);
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: DefaultBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var fileLength = stream.Length;
        if (fileLength > int.MaxValue)
        {
            throw new IOException($"The file '{path}' is too large to read into a byte array.");
        }

        var bytes = new byte[fileLength];
        var totalBytesRead = 0;

        while (totalBytesRead < fileLength)
        {
            ct.ThrowIfCancellationRequested();

            var bytesRead = await stream.ReadAsync(
                bytes.AsMemory(totalBytesRead, (int)(fileLength - totalBytesRead)),
                ct).ConfigureAwait(false);

            if (bytesRead == 0)
            {
                // Unexpected end of file
                break;
            }

            totalBytesRead += bytesRead;
        }

        // If we didn't read the expected number of bytes, resize the array
        if (totalBytesRead < fileLength)
        {
            Array.Resize(ref bytes, totalBytesRead);
        }

        return bytes;
    }

    /// <summary>
    /// Writes bytes to a file asynchronously using true async I/O.
    /// Creates the file if it doesn't exist, or overwrites it if it does.
    /// </summary>
    /// <param name="path">The path to the file to write.</param>
    /// <param name="bytes">The byte array to write to the file.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="bytes"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken ct = default)
    {
        ValidatePath(path);
        ArgumentNullException.ThrowIfNull(bytes);

        await EnsureDirectoryExistsAsync(Path.GetDirectoryName(path)!).ConfigureAwait(false);

        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: DefaultBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await stream.WriteAsync(bytes.AsMemory(), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads all lines from a file asynchronously using true async I/O.
    /// </summary>
    /// <param name="path">The path to the file to read.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous read operation. The value contains the string array of all lines in the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified in <paramref name="path"/> was not found.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task<string[]> ReadAllLinesAsync(string path, CancellationToken ct = default)
    {
        ValidatePath(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The file '{path}' was not found.", path);
        }

        var lines = new List<string>();

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: DefaultBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is not null)
            {
                lines.Add(line);
            }
        }

        return lines.ToArray();
    }

    /// <summary>
    /// Copies a file asynchronously using true async I/O.
    /// </summary>
    /// <param name="source">The path to the source file to copy.</param>
    /// <param name="destination">The path to the destination file.</param>
    /// <param name="overwrite">True to allow overwriting an existing file; otherwise, false.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file specified in <paramref name="source"/> was not found.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs or when the destination file exists and <paramref name="overwrite"/> is false.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public static async Task CopyAsync(string source, string destination, bool overwrite = false, CancellationToken ct = default)
    {
        ValidatePath(source, nameof(source));
        ValidatePath(destination, nameof(destination));

        if (!File.Exists(source))
        {
            throw new FileNotFoundException($"The source file '{source}' was not found.", source);
        }

        if (!overwrite && File.Exists(destination))
        {
            throw new IOException($"The destination file '{destination}' already exists.");
        }

        await EnsureDirectoryExistsAsync(Path.GetDirectoryName(destination)!).ConfigureAwait(false);

        await using var sourceStream = new FileStream(
            source,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: DefaultBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var destinationStream = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: DefaultBufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await sourceStream.CopyToAsync(destinationStream, DefaultBufferSize, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if a file exists. This is a synchronous operation wrapped in a task for API consistency.
    /// </summary>
    /// <param name="path">The path to the file to check.</param>
    /// <returns>A task that represents the operation. The value is true if the file exists; otherwise, false.</returns>
    /// <remarks>
    /// File existence checks are inherently synchronous operations. This method provides an async wrapper
    /// for API consistency when working with other async file operations.
    /// </remarks>
    public static Task<bool> ExistsAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(File.Exists(path));
    }

    /// <summary>
    /// Creates a directory if it doesn't exist. This is a synchronous operation wrapped in a task for API consistency.
    /// </summary>
    /// <param name="path">The path to the directory to create.</param>
    /// <returns>A task that represents the operation.</returns>
    /// <remarks>
    /// Directory creation is an inherently synchronous operation. This method provides an async wrapper
    /// for API consistency when working with other async file operations.
    /// Note: This method accepts a directory path, not a file path. If you have a file path,
    /// use Path.GetDirectoryName() to extract the directory portion.
    /// </remarks>
    public static Task EnsureDirectoryExistsAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates that a path is not null, empty, or whitespace.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <param name="paramName">The name of the parameter for exception messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or whitespace.</exception>
    private static void ValidatePath(string path, string paramName = "path")
    {
        ArgumentNullException.ThrowIfNull(path, paramName);

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be empty or whitespace.", paramName);
        }
    }
}
