namespace FathomOS.Application.Common.Interfaces;

/// <summary>
/// Provides date and time functionality with support for testing.
/// Use this interface instead of DateTime.Now/UtcNow for testability.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current local date and time.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the current UTC date (time component set to midnight).
    /// </summary>
    DateOnly UtcToday { get; }

    /// <summary>
    /// Gets the current local date (time component set to midnight).
    /// </summary>
    DateOnly Today { get; }

    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    TimeOnly UtcTimeOfDay { get; }
}

/// <summary>
/// Default implementation of <see cref="IDateTimeProvider"/> using system time.
/// </summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime Now => DateTime.Now;

    /// <inheritdoc />
    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <inheritdoc />
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <inheritdoc />
    public TimeOnly UtcTimeOfDay => TimeOnly.FromDateTime(DateTime.UtcNow);
}
