// FathomOS.Core/Data/RetryPolicy.cs
// Configuration class for database retry behavior with exponential backoff

namespace FathomOS.Core.Data;

/// <summary>
/// Configuration for database operation retry behavior.
/// Provides settings for exponential backoff with optional jitter to prevent thundering herd problems.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Default retry policy with standard settings suitable for most scenarios.
    /// </summary>
    public static RetryPolicy Default => new();

    /// <summary>
    /// Aggressive retry policy with more attempts and longer delays.
    /// Use for critical operations that must succeed.
    /// </summary>
    public static RetryPolicy Aggressive => new()
    {
        MaxRetries = 5,
        InitialDelayMs = 200,
        MaxDelayMs = 10000,
        BackoffMultiplier = 2.5
    };

    /// <summary>
    /// Fast-fail retry policy with minimal retries.
    /// Use for operations where responsiveness is critical.
    /// </summary>
    public static RetryPolicy FastFail => new()
    {
        MaxRetries = 2,
        InitialDelayMs = 50,
        MaxDelayMs = 1000,
        BackoffMultiplier = 1.5
    };

    /// <summary>
    /// Maximum number of retry attempts before giving up.
    /// Default: 3
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay in milliseconds before the first retry.
    /// Default: 100ms
    /// </summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// Maximum delay in milliseconds between retries.
    /// Delays will be capped at this value regardless of exponential growth.
    /// Default: 5000ms (5 seconds)
    /// </summary>
    public int MaxDelayMs { get; set; } = 5000;

    /// <summary>
    /// Multiplier for exponential backoff calculation.
    /// Each retry delay = previous delay * BackoffMultiplier.
    /// Default: 2.0 (doubles each retry)
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Whether to add random jitter to retry delays.
    /// Jitter helps prevent thundering herd when multiple operations retry simultaneously.
    /// When enabled, adds +/- 25% randomization to the calculated delay.
    /// Default: true
    /// </summary>
    public bool AddJitter { get; set; } = true;

    /// <summary>
    /// SQLite error codes that should trigger a retry.
    /// Default includes SQLITE_BUSY and SQLITE_LOCKED.
    /// </summary>
    public HashSet<int> RetryableSqliteErrorCodes { get; set; } = new()
    {
        5,  // SQLITE_BUSY - database is locked by another process
        6   // SQLITE_LOCKED - table is locked (e.g., deadlock)
    };

    /// <summary>
    /// Validates the retry policy settings.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if settings are invalid.</exception>
    public void Validate()
    {
        if (MaxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxRetries), "MaxRetries must be non-negative.");

        if (InitialDelayMs < 0)
            throw new ArgumentOutOfRangeException(nameof(InitialDelayMs), "InitialDelayMs must be non-negative.");

        if (MaxDelayMs < InitialDelayMs)
            throw new ArgumentOutOfRangeException(nameof(MaxDelayMs), "MaxDelayMs must be greater than or equal to InitialDelayMs.");

        if (BackoffMultiplier < 1.0)
            throw new ArgumentOutOfRangeException(nameof(BackoffMultiplier), "BackoffMultiplier must be at least 1.0.");
    }

    /// <summary>
    /// Calculates the delay for a specific retry attempt.
    /// </summary>
    /// <param name="attempt">The retry attempt number (1-based).</param>
    /// <returns>Delay in milliseconds.</returns>
    public int CalculateDelay(int attempt)
    {
        if (attempt <= 0)
            return 0;

        // Calculate exponential delay: initialDelay * (multiplier ^ (attempt - 1))
        var exponentialDelay = InitialDelayMs * Math.Pow(BackoffMultiplier, attempt - 1);

        // Cap at maximum delay
        var cappedDelay = Math.Min(exponentialDelay, MaxDelayMs);

        // Apply jitter if enabled (+/- 25% randomization)
        if (AddJitter)
        {
            var jitterFactor = 0.25;
            var jitterRange = cappedDelay * jitterFactor;
            var jitter = (Random.Shared.NextDouble() * 2 - 1) * jitterRange;
            cappedDelay = Math.Max(1, cappedDelay + jitter); // Ensure at least 1ms delay
        }

        return (int)cappedDelay;
    }

    /// <summary>
    /// Creates a copy of this retry policy with modified settings.
    /// </summary>
    /// <returns>A new RetryPolicy instance with the same settings.</returns>
    public RetryPolicy Clone()
    {
        return new RetryPolicy
        {
            MaxRetries = MaxRetries,
            InitialDelayMs = InitialDelayMs,
            MaxDelayMs = MaxDelayMs,
            BackoffMultiplier = BackoffMultiplier,
            AddJitter = AddJitter,
            RetryableSqliteErrorCodes = new HashSet<int>(RetryableSqliteErrorCodes)
        };
    }

    /// <summary>
    /// Creates a new retry policy with modified maximum retries.
    /// </summary>
    /// <param name="maxRetries">New maximum retry count.</param>
    /// <returns>A new RetryPolicy with updated settings.</returns>
    public RetryPolicy WithMaxRetries(int maxRetries)
    {
        var clone = Clone();
        clone.MaxRetries = maxRetries;
        return clone;
    }

    /// <summary>
    /// Creates a new retry policy with additional retryable error codes.
    /// </summary>
    /// <param name="errorCodes">Additional SQLite error codes to treat as retryable.</param>
    /// <returns>A new RetryPolicy with updated settings.</returns>
    public RetryPolicy WithAdditionalErrorCodes(params int[] errorCodes)
    {
        var clone = Clone();
        foreach (var code in errorCodes)
        {
            clone.RetryableSqliteErrorCodes.Add(code);
        }
        return clone;
    }
}
