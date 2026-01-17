namespace FathomOS.Core.Interfaces;

/// <summary>
/// Health status of a module.
/// </summary>
public enum ModuleHealthStatus
{
    /// <summary>
    /// Health status has not been determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// Module is operating normally.
    /// </summary>
    Healthy,

    /// <summary>
    /// Module has experienced errors but is still functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// Module has experienced critical errors and may not be functional.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Module has not been loaded yet.
    /// </summary>
    NotLoaded
}

/// <summary>
/// Health information for a single module.
/// </summary>
public class ModuleHealthInfo
{
    /// <summary>
    /// The unique identifier of the module.
    /// </summary>
    public string ModuleId { get; set; } = string.Empty;

    /// <summary>
    /// Current health status of the module.
    /// </summary>
    public ModuleHealthStatus Status { get; set; } = ModuleHealthStatus.Unknown;

    /// <summary>
    /// When the health was last checked or updated.
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// The most recent error message, if any.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Approximate memory usage by the module in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// How long the module has been loaded and running.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Number of errors reported since the module was loaded.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// When the module was loaded (if loaded).
    /// </summary>
    public DateTime? LoadedAt { get; set; }
}

/// <summary>
/// Contract for module health monitoring.
/// Tracks module status, errors, and provides health information.
/// Owned by: SHELL-AGENT (implementation in Shell)
/// </summary>
public interface IModuleHealthService
{
    /// <summary>
    /// Get health information for a specific module.
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    /// <returns>Health information for the module</returns>
    Task<ModuleHealthInfo> GetModuleHealthAsync(string moduleId);

    /// <summary>
    /// Get health information for all discovered modules.
    /// </summary>
    /// <returns>Collection of health information for all modules</returns>
    Task<IEnumerable<ModuleHealthInfo>> GetAllModuleHealthAsync();

    /// <summary>
    /// Report an error that occurred in a module.
    /// This may change the module's health status.
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    /// <param name="ex">The exception that occurred</param>
    void ReportError(string moduleId, Exception ex);

    /// <summary>
    /// Report that a module has recovered from previous errors.
    /// This will reset the module's health status to Healthy.
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    void ReportRecovery(string moduleId);

    /// <summary>
    /// Record that a module has been loaded.
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    void RecordModuleLoaded(string moduleId);

    /// <summary>
    /// Record that a module has been unloaded.
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    void RecordModuleUnloaded(string moduleId);

    /// <summary>
    /// Event raised when a module's health status changes.
    /// </summary>
    event EventHandler<ModuleHealthInfo>? HealthStatusChanged;
}
