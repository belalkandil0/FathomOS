namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Service for generating reports in various formats.
/// </summary>
public class ReportService
{
    /// <summary>
    /// Export sync history to CSV.
    /// </summary>
    public void ExportHistoryToCsv(string filePath, IEnumerable<SyncHistoryEntry> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Computer IP,Computer Name,Status,Drift Before (s),Drift After (s),Time Source,Duration (ms),Error");
        
        foreach (var entry in history)
        {
            sb.AppendLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                         $"{entry.ComputerIp}," +
                         $"\"{entry.ComputerName}\"," +
                         $"{entry.StatusDisplay}," +
                         $"{entry.DriftBeforeSeconds:F3}," +
                         $"{(entry.Success ? entry.DriftAfterSeconds.ToString("F3") : "")}," +
                         $"{entry.TimeSource}," +
                         $"{entry.Duration.TotalMilliseconds:F0}," +
                         $"\"{entry.ErrorMessage ?? ""}\"");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// Export current status to CSV.
    /// </summary>
    public void ExportStatusToCsv(string filePath, IEnumerable<NetworkComputer> computers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("IP Address,Hostname,Status,Current Time,Time Offset (s),Last Checked,Last Synced,Agent Installed,Agent Version");
        
        foreach (var computer in computers)
        {
            sb.AppendLine($"{computer.IpAddress}," +
                         $"\"{computer.Hostname}\"," +
                         $"{computer.StatusText}," +
                         $"{computer.CurrentTimeDisplay}," +
                         $"{computer.TimeDriftSeconds:F3}," +
                         $"{computer.LastChecked?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}," +
                         $"{computer.LastSynced?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never"}," +
                         $"{computer.AgentInstalled}," +
                         $"{computer.AgentVersion}");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// Export drift history to CSV.
    /// </summary>
    public void ExportDriftHistoryToCsv(string filePath, IEnumerable<DriftMeasurement> driftHistory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Computer IP,Drift (s)");
        
        foreach (var measurement in driftHistory)
        {
            sb.AppendLine($"{measurement.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                         $"{measurement.ComputerIp}," +
                         $"{measurement.DriftSeconds:F3}");
        }
        
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// Generate a simple HTML report.
    /// </summary>
    public void ExportStatusToHtml(string filePath, IEnumerable<NetworkComputer> computers, 
        IEnumerable<SyncHistoryEntry> recentHistory)
    {
        var computersList = computers.ToList();
        var historyList = recentHistory.Take(50).ToList();
        
        var syncedCount = computersList.Count(c => c.Status == Enums.SyncStatus.Synced);
        var outOfSyncCount = computersList.Count(c => c.Status == Enums.SyncStatus.OutOfSync);
        var unreachableCount = computersList.Count(c => c.Status == Enums.SyncStatus.Unreachable);
        
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <title>Fathom OS Time Sync Report</title>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background: #1e1e1e; color: #fff; }}
        h1 {{ color: #60CDFF; }}
        h2 {{ color: #60CDFF; border-bottom: 1px solid #3f3f46; padding-bottom: 10px; }}
        table {{ border-collapse: collapse; width: 100%; margin-bottom: 30px; }}
        th, td {{ border: 1px solid #3f3f46; padding: 10px; text-align: left; }}
        th {{ background: #2d2d30; color: #60CDFF; }}
        tr:nth-child(even) {{ background: #252526; }}
        .status-synced {{ color: #6CCB5F; }}
        .status-outofsync {{ color: #FF6B6B; }}
        .status-unreachable {{ color: #A0A0A0; }}
        .summary {{ display: flex; gap: 20px; margin-bottom: 30px; }}
        .summary-card {{ background: #2d2d30; padding: 20px; border-radius: 8px; min-width: 150px; }}
        .summary-value {{ font-size: 36px; font-weight: bold; }}
        .summary-label {{ color: #A0A0A0; }}
        .synced {{ color: #6CCB5F; }}
        .outofsync {{ color: #FF6B6B; }}
        .unreachable {{ color: #A0A0A0; }}
    </style>
</head>
<body>
    <h1>Fathom OS Time Sync Report</h1>
    <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    
    <div class='summary'>
        <div class='summary-card'>
            <div class='summary-value'>{computersList.Count}</div>
            <div class='summary-label'>Total Computers</div>
        </div>
        <div class='summary-card'>
            <div class='summary-value synced'>{syncedCount}</div>
            <div class='summary-label'>Synced</div>
        </div>
        <div class='summary-card'>
            <div class='summary-value outofsync'>{outOfSyncCount}</div>
            <div class='summary-label'>Out of Sync</div>
        </div>
        <div class='summary-card'>
            <div class='summary-value unreachable'>{unreachableCount}</div>
            <div class='summary-label'>Unreachable</div>
        </div>
    </div>
    
    <h2>Computer Status</h2>
    <table>
        <tr>
            <th>IP Address</th>
            <th>Hostname</th>
            <th>Status</th>
            <th>System Time</th>
            <th>Offset</th>
            <th>Last Synced</th>
        </tr>
        {string.Join("\n", computersList.Select(c => $@"
        <tr>
            <td>{c.IpAddress}</td>
            <td>{c.Hostname}</td>
            <td class='status-{c.StatusText.ToLower().Replace(" ", "")}'>{c.StatusText}</td>
            <td>{c.CurrentTimeDisplay}</td>
            <td>{c.TimeDriftDisplay}</td>
            <td>{c.LastSyncedDisplay}</td>
        </tr>"))}
    </table>
    
    <h2>Recent Sync History</h2>
    <table>
        <tr>
            <th>Time</th>
            <th>Computer</th>
            <th>Status</th>
            <th>Drift Before</th>
            <th>Drift After</th>
            <th>Duration</th>
        </tr>
        {string.Join("\n", historyList.Select(h => $@"
        <tr>
            <td>{h.Timestamp:HH:mm:ss}</td>
            <td>{h.ComputerName} ({h.ComputerIp})</td>
            <td class='{(h.Success ? "status-synced" : "status-outofsync")}'>{h.StatusDisplay}</td>
            <td>{h.DriftBeforeDisplay}</td>
            <td>{h.DriftAfterDisplay}</td>
            <td>{h.DurationDisplay}</td>
        </tr>"))}
    </table>
</body>
</html>";
        
        File.WriteAllText(filePath, html);
    }

    /// <summary>
    /// Generate a text-based status report.
    /// </summary>
    public string GenerateTextReport(IEnumerable<NetworkComputer> computers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("              S7 FATHOM TIME SYNC STATUS REPORT                 ");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        
        var list = computers.ToList();
        var synced = list.Count(c => c.Status == Enums.SyncStatus.Synced);
        var outOfSync = list.Count(c => c.Status == Enums.SyncStatus.OutOfSync);
        var unreachable = list.Count(c => c.Status == Enums.SyncStatus.Unreachable);
        
        sb.AppendLine($"Total Computers: {list.Count}");
        sb.AppendLine($"  ✓ Synced:      {synced}");
        sb.AppendLine($"  ✗ Out of Sync: {outOfSync}");
        sb.AppendLine($"  ? Unreachable: {unreachable}");
        sb.AppendLine();
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine("COMPUTER DETAILS");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        
        foreach (var computer in list.OrderBy(c => c.IpAddress))
        {
            var statusIcon = computer.Status switch
            {
                Enums.SyncStatus.Synced => "✓",
                Enums.SyncStatus.OutOfSync => "✗",
                _ => "?"
            };
            
            sb.AppendLine($"{statusIcon} {computer.IpAddress,-15} {computer.Hostname,-20} {computer.StatusText,-12} {computer.TimeDriftDisplay}");
        }
        
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        
        return sb.ToString();
    }
}
