namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FathomOS.Modules.NetworkTimeSync.Enums;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Service for continuous monitoring of computer time sync status.
/// </summary>
public class StatusMonitorService : IDisposable
{
    private readonly TimeSyncService _syncService;
    private readonly ObservableCollection<NetworkComputer> _computers;
    private readonly SyncConfiguration _config;
    private readonly Func<DateTime>? _referenceTimeProvider;
    
    private CancellationTokenSource? _monitorCts;
    private Task? _monitorTask;
    private bool _isRunning;
    private bool _disposed;

    public StatusMonitorService(
        ObservableCollection<NetworkComputer> computers,
        SyncConfiguration config,
        TimeSyncService syncService,
        Func<DateTime>? referenceTimeProvider = null)
    {
        _computers = computers;
        _config = config;
        _syncService = syncService;
        _referenceTimeProvider = referenceTimeProvider;
    }

    /// <summary>
    /// Whether the monitor is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Event raised when a computer's status changes.
    /// </summary>
    public event EventHandler<NetworkComputer>? StatusChanged;

    /// <summary>
    /// Event raised when auto-correction is triggered.
    /// </summary>
    public event EventHandler<(NetworkComputer Computer, bool Success, string? Error)>? AutoCorrectionTriggered;

    /// <summary>
    /// Event raised when a monitoring cycle completes.
    /// </summary>
    public event EventHandler<(int Checked, int Synced, int OutOfSync, int Unreachable)>? CycleCompleted;

    /// <summary>
    /// Start continuous monitoring.
    /// </summary>
    public void Start()
    {
        if (_isRunning) return;

        _monitorCts = new CancellationTokenSource();
        _isRunning = true;
        _monitorTask = MonitorLoopAsync(_monitorCts.Token);
    }

    /// <summary>
    /// Stop continuous monitoring.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _monitorCts?.Cancel();
        _isRunning = false;
    }

    /// <summary>
    /// Check status of all computers once.
    /// </summary>
    public async Task CheckAllAsync(CancellationToken cancellationToken = default)
    {
        var referenceTime = await GetReferenceTimeAsync();
        var tasks = _computers.Select(c => CheckComputerAsync(c, referenceTime, cancellationToken));
        await Task.WhenAll(tasks);

        var stats = GetStatusStats();
        CycleCompleted?.Invoke(this, stats);
    }

    /// <summary>
    /// Check status of a single computer.
    /// </summary>
    public async Task CheckComputerAsync(NetworkComputer computer, DateTime? referenceTime = null, CancellationToken cancellationToken = default)
    {
        // Skip computers without agent installed
        if (!computer.AgentInstalled || computer.Status == SyncStatus.AgentNotInstalled)
        {
            return;
        }

        var prevStatus = computer.Status;
        
        try
        {
            // Update UI to show checking
            await UpdateOnUiThread(() => computer.Status = SyncStatus.Checking);

            referenceTime ??= await GetReferenceTimeAsync();

            // Get time from remote computer
            var (timeInfo, error) = await _syncService.GetTimeAsync(
                computer.IpAddress, 
                computer.Port, 
                cancellationToken);

            if (timeInfo == null)
            {
                await UpdateOnUiThread(() =>
                {
                    computer.Status = SyncStatus.Unreachable;
                    computer.LastChecked = DateTime.Now;
                    computer.CurrentTime = null;
                    computer.TimeDriftSeconds = 0;
                });
            }
            else
            {
                var drift = (timeInfo.UtcTime - referenceTime.Value).TotalSeconds;
                var isSynced = Math.Abs(drift) <= _config.ToleranceSeconds;

                await UpdateOnUiThread(() =>
                {
                    computer.Status = isSynced ? SyncStatus.Synced : SyncStatus.OutOfSync;
                    computer.CurrentTime = timeInfo.LocalTime;
                    computer.TimeDriftSeconds = drift;
                    computer.LastChecked = DateTime.Now;
                    computer.AgentInstalled = true;
                });

                // Auto-correct if in continuous mode and drift exceeds threshold
                if (_config.SyncMode == SyncMode.Continuous && 
                    !isSynced && 
                    Math.Abs(drift) >= _config.AutoCorrectThresholdSeconds)
                {
                    await AutoCorrectAsync(computer, referenceTime.Value, cancellationToken);
                }
            }
        }
        catch (Exception)
        {
            await UpdateOnUiThread(() =>
            {
                computer.Status = SyncStatus.Error;
                computer.LastChecked = DateTime.Now;
            });
        }

        if (computer.Status != prevStatus)
        {
            StatusChanged?.Invoke(this, computer);
        }
    }

    /// <summary>
    /// Force sync a single computer.
    /// </summary>
    public async Task<(bool Success, string? Error)> SyncComputerAsync(NetworkComputer computer, CancellationToken cancellationToken = default)
    {
        // Cannot sync computers without agent installed
        if (!computer.AgentInstalled || computer.Status == SyncStatus.AgentNotInstalled)
        {
            return (false, "Agent is not installed on this computer");
        }

        var prevStatus = computer.Status;

        try
        {
            await UpdateOnUiThread(() => computer.Status = SyncStatus.Syncing);

            var referenceTime = await GetReferenceTimeAsync();

            bool success;
            string? error;

            // Sync based on time source
            if (_config.TimeSource == TimeSourceType.InternetNtp)
            {
                (success, error) = await _syncService.SyncNtpAsync(
                    computer.IpAddress,
                    computer.Port,
                    _config.PrimaryNtpServer,
                    cancellationToken);
            }
            else if (_config.TimeSource == TimeSourceType.LocalNtpServer)
            {
                (success, error) = await _syncService.SyncNtpAsync(
                    computer.IpAddress,
                    computer.Port,
                    _config.LocalNtpServer,
                    cancellationToken);
            }
            else
            {
                // HostComputer mode - set time directly
                (success, error) = await _syncService.SetTimeAsync(
                    computer.IpAddress,
                    computer.Port,
                    referenceTime,
                    cancellationToken);
            }

            if (success)
            {
                await UpdateOnUiThread(() =>
                {
                    computer.Status = SyncStatus.Synced;
                    computer.LastSynced = DateTime.Now;
                    computer.TimeDriftSeconds = 0;
                });

                // Verify sync
                await Task.Delay(500, cancellationToken);
                await CheckComputerAsync(computer, referenceTime, cancellationToken);
            }
            else
            {
                await UpdateOnUiThread(() => computer.Status = SyncStatus.Error);
            }

            return (success, error);
        }
        catch (Exception ex)
        {
            await UpdateOnUiThread(() => computer.Status = SyncStatus.Error);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Force sync all computers.
    /// </summary>
    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.Now;
        var result = new SyncResult { TotalComputers = _computers.Count };

        foreach (var computer in _computers.ToList())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var (success, error) = await SyncComputerAsync(computer, cancellationToken);
            
            if (success)
                result.SuccessCount++;
            else
            {
                result.FailedCount++;
                result.Failures.Add((computer.IpAddress, error));
            }
        }

        result.Duration = DateTime.Now - startTime;
        return result;
    }

    /// <summary>
    /// Get reference time based on configuration.
    /// </summary>
    private Task<DateTime> GetReferenceTimeAsync()
    {
        // If external provider is set (e.g., GPS), use it
        if (_referenceTimeProvider != null)
        {
            return Task.FromResult(_referenceTimeProvider());
        }
        
        // Default to local UTC time
        return Task.FromResult(DateTime.UtcNow);
    }

    /// <summary>
    /// Main monitoring loop.
    /// </summary>
    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllAsync(cancellationToken);
                await Task.Delay(_config.CheckIntervalSeconds * 1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Monitor] Error in loop: {ex.Message}");
                await Task.Delay(5000, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Auto-correct a computer's time.
    /// </summary>
    private async Task AutoCorrectAsync(NetworkComputer computer, DateTime referenceTime, CancellationToken cancellationToken)
    {
        var (success, error) = await SyncComputerAsync(computer, cancellationToken);
        AutoCorrectionTriggered?.Invoke(this, (computer, success, error));
    }

    /// <summary>
    /// Get current status statistics.
    /// </summary>
    public (int Checked, int Synced, int OutOfSync, int Unreachable) GetStatusStats()
    {
        var synced = _computers.Count(c => c.Status == SyncStatus.Synced);
        var outOfSync = _computers.Count(c => c.Status == SyncStatus.OutOfSync);
        var unreachable = _computers.Count(c => c.Status == SyncStatus.Unreachable || c.Status == SyncStatus.Error);
        return (_computers.Count, synced, outOfSync, unreachable);
    }

    /// <summary>
    /// Helper to update properties on UI thread.
    /// </summary>
    private async Task UpdateOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher != null)
        {
            await Application.Current.Dispatcher.InvokeAsync(action);
        }
        else
        {
            action();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _monitorCts?.Dispose();
    }
}
