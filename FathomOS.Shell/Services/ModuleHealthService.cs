using System.Collections.Concurrent;
using FathomOS.Core.Interfaces;

namespace FathomOS.Shell.Services;

/// <summary>
/// Tracks module health status including errors, uptime, and recovery.
/// Thread-safe implementation for monitoring module health across the application.
/// Owned by: SHELL-AGENT
/// </summary>
public class ModuleHealthService : IModuleHealthService
{
    /// <summary>
    /// Number of errors before a module is marked as Degraded.
    /// </summary>
    private const int DegradedErrorThreshold = 1;

    /// <summary>
    /// Number of errors before a module is marked as Unhealthy.
    /// </summary>
    private const int UnhealthyErrorThreshold = 3;

    private readonly ConcurrentDictionary<string, ModuleHealthState> _moduleStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly IModuleManager? _moduleManager;
    private readonly object _eventLock = new();

    /// <inheritdoc />
    public event EventHandler<ModuleHealthInfo>? HealthStatusChanged;

    /// <summary>
    /// Creates a new instance of the ModuleHealthService.
    /// </summary>
    /// <param name="moduleManager">Optional module manager for accessing module metadata</param>
    public ModuleHealthService(IModuleManager? moduleManager = null)
    {
        _moduleManager = moduleManager;
    }

    /// <inheritdoc />
    public Task<ModuleHealthInfo> GetModuleHealthAsync(string moduleId)
    {
        var healthInfo = GetOrCreateHealthInfo(moduleId);
        return Task.FromResult(healthInfo);
    }

    /// <inheritdoc />
    public Task<IEnumerable<ModuleHealthInfo>> GetAllModuleHealthAsync()
    {
        var healthInfos = new List<ModuleHealthInfo>();

        // Get health for all tracked modules
        foreach (var moduleId in _moduleStates.Keys)
        {
            healthInfos.Add(GetOrCreateHealthInfo(moduleId));
        }

        // If we have a module manager, also include discovered modules that haven't been tracked yet
        if (_moduleManager != null)
        {
            var discoveredModuleIds = GetDiscoveredModuleIds();
            foreach (var moduleId in discoveredModuleIds)
            {
                if (!_moduleStates.ContainsKey(moduleId))
                {
                    healthInfos.Add(GetOrCreateHealthInfo(moduleId));
                }
            }
        }

        return Task.FromResult<IEnumerable<ModuleHealthInfo>>(healthInfos);
    }

    /// <inheritdoc />
    public void ReportError(string moduleId, Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        if (string.IsNullOrWhiteSpace(moduleId))
            return;

        var state = GetOrCreateState(moduleId);
        var previousStatus = state.Status;

        lock (state.Lock)
        {
            state.ErrorCount++;
            state.LastError = ex.Message;
            state.LastChecked = DateTime.UtcNow;

            // Update status based on error count
            if (state.ErrorCount >= UnhealthyErrorThreshold)
            {
                state.Status = ModuleHealthStatus.Unhealthy;
            }
            else if (state.ErrorCount >= DegradedErrorThreshold)
            {
                state.Status = ModuleHealthStatus.Degraded;
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"ModuleHealthService: Error reported for {moduleId} (count: {state.ErrorCount}): {ex.Message}");

        // Raise event if status changed
        if (previousStatus != state.Status)
        {
            RaiseHealthStatusChanged(moduleId);
        }
    }

    /// <inheritdoc />
    public void ReportRecovery(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return;

        var state = GetOrCreateState(moduleId);
        var previousStatus = state.Status;

        lock (state.Lock)
        {
            state.ErrorCount = 0;
            state.LastError = null;
            state.LastChecked = DateTime.UtcNow;

            // Only set to Healthy if the module is loaded
            if (state.LoadedAt.HasValue)
            {
                state.Status = ModuleHealthStatus.Healthy;
            }
        }

        System.Diagnostics.Debug.WriteLine($"ModuleHealthService: Recovery reported for {moduleId}");

        // Raise event if status changed
        if (previousStatus != state.Status)
        {
            RaiseHealthStatusChanged(moduleId);
        }
    }

    /// <inheritdoc />
    public void RecordModuleLoaded(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return;

        var state = GetOrCreateState(moduleId);
        var previousStatus = state.Status;

        lock (state.Lock)
        {
            state.LoadedAt = DateTime.UtcNow;
            state.LastChecked = DateTime.UtcNow;

            // Set to Healthy if no prior errors
            if (state.ErrorCount == 0)
            {
                state.Status = ModuleHealthStatus.Healthy;
            }
        }

        System.Diagnostics.Debug.WriteLine($"ModuleHealthService: Module loaded - {moduleId}");

        // Raise event if status changed
        if (previousStatus != state.Status)
        {
            RaiseHealthStatusChanged(moduleId);
        }
    }

    /// <inheritdoc />
    public void RecordModuleUnloaded(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return;

        var state = GetOrCreateState(moduleId);
        var previousStatus = state.Status;

        lock (state.Lock)
        {
            state.LoadedAt = null;
            state.LastChecked = DateTime.UtcNow;
            state.Status = ModuleHealthStatus.NotLoaded;
            state.ErrorCount = 0;
            state.LastError = null;
        }

        System.Diagnostics.Debug.WriteLine($"ModuleHealthService: Module unloaded - {moduleId}");

        // Raise event if status changed
        if (previousStatus != state.Status)
        {
            RaiseHealthStatusChanged(moduleId);
        }
    }

    /// <summary>
    /// Gets or creates a health state for a module.
    /// </summary>
    private ModuleHealthState GetOrCreateState(string moduleId)
    {
        return _moduleStates.GetOrAdd(moduleId, id => new ModuleHealthState
        {
            ModuleId = id,
            Status = ModuleHealthStatus.NotLoaded,
            LastChecked = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets or creates a health info object for a module.
    /// </summary>
    private ModuleHealthInfo GetOrCreateHealthInfo(string moduleId)
    {
        var state = GetOrCreateState(moduleId);

        lock (state.Lock)
        {
            return new ModuleHealthInfo
            {
                ModuleId = state.ModuleId,
                Status = state.Status,
                LastChecked = state.LastChecked,
                LastError = state.LastError,
                ErrorCount = state.ErrorCount,
                LoadedAt = state.LoadedAt,
                Uptime = state.LoadedAt.HasValue
                    ? DateTime.UtcNow - state.LoadedAt.Value
                    : TimeSpan.Zero,
                MemoryUsageBytes = 0 // Memory tracking can be enhanced later
            };
        }
    }

    /// <summary>
    /// Gets the list of discovered module IDs from the module manager.
    /// </summary>
    private IEnumerable<string> GetDiscoveredModuleIds()
    {
        if (_moduleManager == null)
            return Enumerable.Empty<string>();

        // Access the Modules property which returns IModuleMetadata
        return _moduleManager.Modules.Select(m => m.ModuleId);
    }

    /// <summary>
    /// Raises the HealthStatusChanged event.
    /// </summary>
    private void RaiseHealthStatusChanged(string moduleId)
    {
        var healthInfo = GetOrCreateHealthInfo(moduleId);

        lock (_eventLock)
        {
            try
            {
                HealthStatusChanged?.Invoke(this, healthInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"ModuleHealthService: Error raising HealthStatusChanged event: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Internal state tracking for a module.
    /// </summary>
    private class ModuleHealthState
    {
        public string ModuleId { get; init; } = string.Empty;
        public ModuleHealthStatus Status { get; set; } = ModuleHealthStatus.Unknown;
        public DateTime LastChecked { get; set; }
        public string? LastError { get; set; }
        public int ErrorCount { get; set; }
        public DateTime? LoadedAt { get; set; }
        public object Lock { get; } = new();
    }
}
