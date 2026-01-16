namespace FathomOS.Modules.NetworkTimeSync.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single sync operation record.
/// </summary>
public class SyncHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string ComputerIp { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public double DriftBeforeSeconds { get; set; }
    public double DriftAfterSeconds { get; set; }
    public string TimeSource { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }

    public string DriftBeforeDisplay => $"{(DriftBeforeSeconds >= 0 ? "+" : "")}{DriftBeforeSeconds:F2}s";
    public string DriftAfterDisplay => Success ? $"{(DriftAfterSeconds >= 0 ? "+" : "")}{DriftAfterSeconds:F2}s" : "N/A";
    public string StatusDisplay => Success ? "Success" : "Failed";
    public string DurationDisplay => $"{Duration.TotalMilliseconds:F0}ms";
}

/// <summary>
/// Represents a time drift measurement point.
/// </summary>
public class DriftMeasurement
{
    public DateTime Timestamp { get; set; }
    public string ComputerIp { get; set; } = string.Empty;
    public double DriftSeconds { get; set; }
}

/// <summary>
/// Schedule for automatic sync operations.
/// </summary>
public class SyncSchedule
{
    public bool Enabled { get; set; }
    public ScheduleType Type { get; set; } = ScheduleType.Interval;
    
    // For interval-based scheduling
    public int IntervalMinutes { get; set; } = 60;
    
    // For time-based scheduling
    public List<TimeOnly> ScheduledTimes { get; set; } = new();
    
    // For daily scheduling
    public TimeOnly DailyTime { get; set; } = new TimeOnly(0, 0); // Midnight
    
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
}

public enum ScheduleType
{
    /// <summary>Run at fixed intervals (e.g., every hour).</summary>
    Interval,
    
    /// <summary>Run at specific times each day.</summary>
    FixedTimes,
    
    /// <summary>Run once per day at specified time.</summary>
    Daily
}

/// <summary>
/// Alert threshold configuration for a computer.
/// </summary>
public class AlertThreshold
{
    public string ComputerIp { get; set; } = string.Empty;
    public double WarningSeconds { get; set; } = 0.5;
    public double CriticalSeconds { get; set; } = 1.0;
    public bool EnableSoundAlert { get; set; } = true;
    public bool AutoCorrect { get; set; } = true;
}

/// <summary>
/// Computer group for organization.
/// </summary>
public class ComputerGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#60CDFF";
    public List<string> ComputerIps { get; set; } = new();
    public bool IsExpanded { get; set; } = true;
}
