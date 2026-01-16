namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Windows.Threading;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Service for managing sync history, scheduling, and alerts.
/// </summary>
public class SyncHistoryService : IDisposable
{
    private readonly string _historyFilePath;
    private readonly string _driftFilePath;
    private readonly List<SyncHistoryEntry> _history = new();
    private readonly List<DriftMeasurement> _driftHistory = new();
    private readonly object _lock = new();
    private readonly DispatcherTimer? _scheduleTimer;
    private SyncSchedule _schedule = new();
    
    private const int MaxHistoryEntries = 1000;
    private const int MaxDriftMeasurements = 10000;

    public event EventHandler<SyncSchedule>? ScheduledSyncDue;
    public event EventHandler<(string ComputerIp, double Drift, AlertLevel Level)>? AlertTriggered;

    public SyncHistoryService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "NetworkTimeSync");
        Directory.CreateDirectory(appDataPath);
        
        _historyFilePath = Path.Combine(appDataPath, "sync_history.json");
        _driftFilePath = Path.Combine(appDataPath, "drift_history.json");
        
        LoadHistory();
        LoadDriftHistory();
        
        // Schedule timer
        _scheduleTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30)
        };
        _scheduleTimer.Tick += CheckSchedule;
    }

    #region Sync History

    public void AddEntry(SyncHistoryEntry entry)
    {
        lock (_lock)
        {
            _history.Insert(0, entry);
            
            // Trim if too many entries
            while (_history.Count > MaxHistoryEntries)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
        
        SaveHistoryAsync();
    }

    public IReadOnlyList<SyncHistoryEntry> GetHistory(int count = 100)
    {
        lock (_lock)
        {
            return _history.Take(count).ToList();
        }
    }

    public IReadOnlyList<SyncHistoryEntry> GetHistoryForComputer(string ip, int count = 50)
    {
        lock (_lock)
        {
            return _history
                .Where(h => h.ComputerIp == ip)
                .Take(count)
                .ToList();
        }
    }

    public (int Total, int Successful, int Failed) GetTodayStats()
    {
        lock (_lock)
        {
            var today = _history.Where(h => h.Timestamp.Date == DateTime.Today).ToList();
            return (today.Count, today.Count(h => h.Success), today.Count(h => !h.Success));
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
        }
        SaveHistoryAsync();
    }

    #endregion

    #region Drift Tracking

    public void RecordDrift(string computerIp, double driftSeconds)
    {
        lock (_lock)
        {
            _driftHistory.Add(new DriftMeasurement
            {
                Timestamp = DateTime.Now,
                ComputerIp = computerIp,
                DriftSeconds = driftSeconds
            });
            
            // Trim old measurements (keep last 24 hours)
            var cutoff = DateTime.Now.AddHours(-24);
            _driftHistory.RemoveAll(d => d.Timestamp < cutoff);
            
            // Also enforce max count
            while (_driftHistory.Count > MaxDriftMeasurements)
            {
                _driftHistory.RemoveAt(0);
            }
        }
        
        SaveDriftHistoryAsync();
    }

    public IReadOnlyList<DriftMeasurement> GetDriftHistory(string? computerIp = null, TimeSpan? duration = null)
    {
        lock (_lock)
        {
            var query = _driftHistory.AsEnumerable();
            
            if (!string.IsNullOrEmpty(computerIp))
            {
                query = query.Where(d => d.ComputerIp == computerIp);
            }
            
            if (duration.HasValue)
            {
                var cutoff = DateTime.Now - duration.Value;
                query = query.Where(d => d.Timestamp >= cutoff);
            }
            
            return query.OrderBy(d => d.Timestamp).ToList();
        }
    }

    /// <summary>
    /// Predict when drift will exceed threshold based on historical trend.
    /// </summary>
    public DateTime? PredictThresholdExceedance(string computerIp, double thresholdSeconds)
    {
        var history = GetDriftHistory(computerIp, TimeSpan.FromHours(1));
        if (history.Count < 10) return null;
        
        // Simple linear regression to predict drift trend
        var n = history.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;
        var startTime = history.First().Timestamp;
        
        foreach (var point in history)
        {
            var x = (point.Timestamp - startTime).TotalMinutes;
            var y = Math.Abs(point.DriftSeconds);
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }
        
        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;
        
        if (slope <= 0) return null; // Drift is stable or decreasing
        
        var currentDrift = Math.Abs(history.Last().DriftSeconds);
        if (currentDrift >= thresholdSeconds) return DateTime.Now; // Already exceeded
        
        var minutesToThreshold = (thresholdSeconds - intercept) / slope;
        var lastMeasurement = history.Last().Timestamp;
        var currentMinutes = (lastMeasurement - startTime).TotalMinutes;
        var remainingMinutes = minutesToThreshold - currentMinutes;
        
        if (remainingMinutes <= 0) return DateTime.Now;
        if (remainingMinutes > 60 * 24) return null; // More than 24 hours away
        
        return DateTime.Now.AddMinutes(remainingMinutes);
    }

    #endregion

    #region Scheduling

    public SyncSchedule GetSchedule() => _schedule;

    public void SetSchedule(SyncSchedule schedule)
    {
        _schedule = schedule;
        UpdateNextRunTime();
        
        if (schedule.Enabled)
        {
            _scheduleTimer?.Start();
        }
        else
        {
            _scheduleTimer?.Stop();
        }
    }

    private void UpdateNextRunTime()
    {
        if (!_schedule.Enabled)
        {
            _schedule.NextRun = null;
            return;
        }

        var now = DateTime.Now;

        switch (_schedule.Type)
        {
            case ScheduleType.Interval:
                _schedule.NextRun = _schedule.LastRun?.AddMinutes(_schedule.IntervalMinutes) ?? now;
                if (_schedule.NextRun < now)
                    _schedule.NextRun = now.AddMinutes(_schedule.IntervalMinutes);
                break;

            case ScheduleType.Daily:
                var todayTime = now.Date.Add(_schedule.DailyTime.ToTimeSpan());
                _schedule.NextRun = todayTime > now ? todayTime : todayTime.AddDays(1);
                break;

            case ScheduleType.FixedTimes:
                var nextTime = _schedule.ScheduledTimes
                    .Select(t => now.Date.Add(t.ToTimeSpan()))
                    .Where(t => t > now)
                    .OrderBy(t => t)
                    .FirstOrDefault();
                
                if (nextTime == default)
                {
                    // All times passed today, get first time tomorrow
                    nextTime = now.Date.AddDays(1).Add(_schedule.ScheduledTimes.Min().ToTimeSpan());
                }
                _schedule.NextRun = nextTime;
                break;
        }
    }

    private void CheckSchedule(object? sender, EventArgs e)
    {
        if (!_schedule.Enabled || !_schedule.NextRun.HasValue) return;

        if (DateTime.Now >= _schedule.NextRun.Value)
        {
            _schedule.LastRun = DateTime.Now;
            UpdateNextRunTime();
            ScheduledSyncDue?.Invoke(this, _schedule);
        }
    }

    #endregion

    #region Alerts

    public void CheckAlert(string computerIp, double driftSeconds, AlertThreshold threshold)
    {
        var absDrift = Math.Abs(driftSeconds);
        AlertLevel level;
        
        if (absDrift >= threshold.CriticalSeconds)
            level = AlertLevel.Critical;
        else if (absDrift >= threshold.WarningSeconds)
            level = AlertLevel.Warning;
        else
            return; // No alert needed

        AlertTriggered?.Invoke(this, (computerIp, driftSeconds, level));
        
        if (threshold.EnableSoundAlert)
        {
            PlayAlertSound(level);
        }
    }

    public void PlayAlertSound(AlertLevel level)
    {
        try
        {
            // Use system sounds
            if (level == AlertLevel.Critical)
            {
                SystemSounds.Exclamation.Play();
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }
        }
        catch
        {
            // Ignore sound errors
        }
    }

    #endregion

    #region Persistence

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var json = File.ReadAllText(_historyFilePath);
                var entries = JsonSerializer.Deserialize<List<SyncHistoryEntry>>(json);
                if (entries != null)
                {
                    lock (_lock)
                    {
                        _history.Clear();
                        _history.AddRange(entries);
                    }
                }
            }
        }
        catch
        {
            // Ignore load errors
        }
    }

    private async void SaveHistoryAsync()
    {
        try
        {
            List<SyncHistoryEntry> copy;
            lock (_lock)
            {
                copy = _history.ToList();
            }
            
            var json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_historyFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    private void LoadDriftHistory()
    {
        try
        {
            if (File.Exists(_driftFilePath))
            {
                var json = File.ReadAllText(_driftFilePath);
                var entries = JsonSerializer.Deserialize<List<DriftMeasurement>>(json);
                if (entries != null)
                {
                    lock (_lock)
                    {
                        _driftHistory.Clear();
                        _driftHistory.AddRange(entries);
                    }
                }
            }
        }
        catch
        {
            // Ignore load errors
        }
    }

    private async void SaveDriftHistoryAsync()
    {
        try
        {
            List<DriftMeasurement> copy;
            lock (_lock)
            {
                copy = _driftHistory.ToList();
            }
            
            var json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_driftFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    #endregion

    public void Dispose()
    {
        _scheduleTimer?.Stop();
    }
}

public enum AlertLevel
{
    Warning,
    Critical
}
