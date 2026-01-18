namespace FathomOS.Core.Interfaces;

/// <summary>
/// HTTP service interface with built-in resilience patterns including
/// retry with exponential backoff, circuit breaker, and timeout handling.
/// </summary>
public interface IHttpService
{
    /// <summary>
    /// Performs a GET request and deserializes the response.
    /// </summary>
    /// <typeparam name="T">Type to deserialize response to.</typeparam>
    /// <param name="url">Request URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the deserialized response or error.</returns>
    Task<Result<T>> GetAsync<T>(string url, CancellationToken ct = default);

    /// <summary>
    /// Performs a GET request and returns the response as string.
    /// </summary>
    /// <param name="url">Request URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the response string or error.</returns>
    Task<Result<string>> GetStringAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Performs a GET request and returns the response as bytes.
    /// </summary>
    /// <param name="url">Request URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the response bytes or error.</returns>
    Task<Result<byte[]>> GetBytesAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Performs a POST request with a JSON body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize response to.</typeparam>
    /// <param name="url">Request URL.</param>
    /// <param name="data">Data to serialize as JSON body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the deserialized response or error.</returns>
    Task<Result<T>> PostAsync<T>(string url, object data, CancellationToken ct = default);

    /// <summary>
    /// Performs a POST request without expecting a response body.
    /// </summary>
    /// <param name="url">Request URL.</param>
    /// <param name="data">Data to serialize as JSON body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<Result> PostAsync(string url, object data, CancellationToken ct = default);

    /// <summary>
    /// Performs a PUT request with a JSON body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize response to.</typeparam>
    /// <param name="url">Request URL.</param>
    /// <param name="data">Data to serialize as JSON body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the deserialized response or error.</returns>
    Task<Result<T>> PutAsync<T>(string url, object data, CancellationToken ct = default);

    /// <summary>
    /// Performs a PUT request without expecting a response body.
    /// </summary>
    /// <param name="url">Request URL.</param>
    /// <param name="data">Data to serialize as JSON body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<Result> PutAsync(string url, object data, CancellationToken ct = default);

    /// <summary>
    /// Performs a PATCH request with a JSON body.
    /// </summary>
    /// <typeparam name="T">Type to deserialize response to.</typeparam>
    /// <param name="url">Request URL.</param>
    /// <param name="data">Data to serialize as JSON body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the deserialized response or error.</returns>
    Task<Result<T>> PatchAsync<T>(string url, object data, CancellationToken ct = default);

    /// <summary>
    /// Performs a DELETE request.
    /// </summary>
    /// <param name="url">Request URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<Result> DeleteAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Performs a DELETE request with a typed response.
    /// </summary>
    /// <typeparam name="T">Type to deserialize response to.</typeparam>
    /// <param name="url">Request URL.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the deserialized response or error.</returns>
    Task<Result<T>> DeleteAsync<T>(string url, CancellationToken ct = default);

    /// <summary>
    /// Downloads a file to a local path.
    /// </summary>
    /// <param name="url">URL to download from.</param>
    /// <param name="localPath">Local file path to save to.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or error.</returns>
    Task<Result> DownloadFileAsync(string url, string localPath, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Uploads a file.
    /// </summary>
    /// <typeparam name="T">Type to deserialize response to.</typeparam>
    /// <param name="url">Upload URL.</param>
    /// <param name="filePath">Path to file to upload.</param>
    /// <param name="fieldName">Form field name for the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the deserialized response or error.</returns>
    Task<Result<T>> UploadFileAsync<T>(string url, string filePath, string fieldName = "file", CancellationToken ct = default);

    /// <summary>
    /// Sets default headers that will be sent with every request.
    /// </summary>
    /// <param name="headers">Headers to set.</param>
    void SetDefaultHeaders(IDictionary<string, string> headers);

    /// <summary>
    /// Sets the authorization header.
    /// </summary>
    /// <param name="scheme">Authorization scheme (e.g., "Bearer").</param>
    /// <param name="token">Authorization token.</param>
    void SetAuthorization(string scheme, string token);

    /// <summary>
    /// Clears the authorization header.
    /// </summary>
    void ClearAuthorization();

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    CircuitBreakerState CircuitState { get; }
}

/// <summary>
/// Configuration options for the HTTP service.
/// </summary>
public class HttpServiceOptions
{
    /// <summary>
    /// Base URL for all requests.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Request timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable exponential backoff for retries.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Number of consecutive failures before opening circuit breaker.
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Duration to keep circuit breaker open.
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable request/response logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// User agent string.
    /// </summary>
    public string UserAgent { get; set; } = "FathomOS/1.0";

    /// <summary>
    /// HTTP status codes that should trigger retry.
    /// </summary>
    public HashSet<int> RetryStatusCodes { get; set; } = new()
    {
        408, // Request Timeout
        429, // Too Many Requests
        500, // Internal Server Error
        502, // Bad Gateway
        503, // Service Unavailable
        504  // Gateway Timeout
    };
}

/// <summary>
/// Result type for operations that can succeed or fail.
/// </summary>
public class Result
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? Error { get; protected set; }

    /// <summary>
    /// Exception if one was caught.
    /// </summary>
    public Exception? Exception { get; protected set; }

    /// <summary>
    /// HTTP status code if applicable.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">Error message.</param>
    /// <param name="exception">Optional exception.</param>
    public static Result Failure(string error, Exception? exception = null) =>
        new() { IsSuccess = false, Error = error, Exception = exception };

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    /// <param name="exception">Exception that caused the failure.</param>
    public static Result Failure(Exception exception) =>
        new() { IsSuccess = false, Error = exception.Message, Exception = exception };
}

/// <summary>
/// Result type with a value for operations that return data.
/// </summary>
/// <typeparam name="T">Type of the value.</typeparam>
public class Result<T> : Result
{
    private T? _value;

    /// <summary>
    /// The value if operation succeeded.
    /// </summary>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException($"Cannot access Value on failed result. Error: {Error}");
            return _value!;
        }
        private set => _value = value;
    }

    /// <summary>
    /// Gets the value or a default if operation failed.
    /// </summary>
    /// <param name="defaultValue">Default value to return on failure.</param>
    /// <returns>The value or default.</returns>
    public T? GetValueOrDefault(T? defaultValue = default) =>
        IsSuccess ? _value : defaultValue;

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    public static Result<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">Error message.</param>
    /// <param name="exception">Optional exception.</param>
    public new static Result<T> Failure(string error, Exception? exception = null) =>
        new() { IsSuccess = false, Error = error, Exception = exception };

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    /// <param name="exception">Exception that caused the failure.</param>
    public new static Result<T> Failure(Exception exception) =>
        new() { IsSuccess = false, Error = exception.Message, Exception = exception };

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Download progress information.
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// Total bytes to download (may be 0 if unknown).
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Bytes downloaded so far.
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double Percentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;

    /// <summary>
    /// Download speed in bytes per second.
    /// </summary>
    public double BytesPerSecond { get; set; }

    /// <summary>
    /// Estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }
}

/// <summary>
/// Circuit breaker state.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed (normal operation).
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open (requests blocked).
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open (testing if service recovered).
    /// </summary>
    HalfOpen
}
