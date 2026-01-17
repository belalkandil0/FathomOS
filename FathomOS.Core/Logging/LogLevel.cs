namespace FathomOS.Core.Logging;

/// <summary>
/// Defines the severity levels for log messages.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Detailed diagnostic information for debugging purposes.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// General informational messages about application flow.
    /// </summary>
    Info = 1,

    /// <summary>
    /// Potentially harmful situations that should be reviewed.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error events that might still allow the application to continue.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Severe errors that may cause application termination.
    /// </summary>
    Critical = 4
}
