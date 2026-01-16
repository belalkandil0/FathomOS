namespace FathomOS.Modules.NetworkTimeSync.Models;

using System;
using System.Text.Json.Serialization;
using FathomOS.Modules.NetworkTimeSync.Enums;

/// <summary>
/// Request message sent to the agent.
/// </summary>
public class AgentRequest
{
    /// <summary>
    /// Protocol version for compatibility.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Command to execute.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Authentication token (SHA256 of secret + timestamp).
    /// </summary>
    [JsonPropertyName("auth")]
    public string Auth { get; set; } = string.Empty;

    /// <summary>
    /// Request timestamp (UTC ticks).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Optional payload data (e.g., time to set).
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Create a Ping request.
    /// </summary>
    public static AgentRequest CreatePing(string secret)
    {
        return CreateRequest(AgentCommand.Ping, secret);
    }

    /// <summary>
    /// Create a GetTime request.
    /// </summary>
    public static AgentRequest CreateGetTime(string secret)
    {
        return CreateRequest(AgentCommand.GetTime, secret);
    }

    /// <summary>
    /// Create a SetTime request.
    /// </summary>
    public static AgentRequest CreateSetTime(string secret, DateTime utcTime)
    {
        var request = CreateRequest(AgentCommand.SetTime, secret);
        request.Payload = utcTime.Ticks.ToString();
        return request;
    }

    /// <summary>
    /// Create a GetInfo request.
    /// </summary>
    public static AgentRequest CreateGetInfo(string secret)
    {
        return CreateRequest(AgentCommand.GetInfo, secret);
    }

    /// <summary>
    /// Create a SyncNtp request.
    /// </summary>
    public static AgentRequest CreateSyncNtp(string secret, string ntpServer)
    {
        var request = CreateRequest(AgentCommand.SyncNtp, secret);
        request.Payload = ntpServer;
        return request;
    }

    private static AgentRequest CreateRequest(AgentCommand command, string secret)
    {
        var timestamp = DateTime.UtcNow.Ticks;
        return new AgentRequest
        {
            Version = "1.0",
            Command = command.ToString(),
            Timestamp = timestamp,
            Auth = GenerateAuth(secret, timestamp)
        };
    }

    /// <summary>
    /// Generate authentication hash.
    /// </summary>
    public static string GenerateAuth(string secret, long timestamp)
    {
        var data = $"{secret}:{timestamp}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Response message from the agent.
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// Protocol version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Response status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Response timestamp (UTC ticks).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// Response payload data.
    /// </summary>
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    /// <summary>
    /// Check if response indicates success.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Status == AgentResponseStatus.Success.ToString();

    /// <summary>
    /// Get the timestamp as DateTime.
    /// </summary>
    [JsonIgnore]
    public DateTime ResponseTime => new DateTime(Timestamp, DateTimeKind.Utc);
}

/// <summary>
/// Computer information returned by GetInfo command.
/// </summary>
public class ComputerInfo
{
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("osVersion")]
    public string OsVersion { get; set; } = string.Empty;

    [JsonPropertyName("agentVersion")]
    public string AgentVersion { get; set; } = string.Empty;

    [JsonPropertyName("currentTimeUtc")]
    public long CurrentTimeUtc { get; set; }

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = string.Empty;

    [JsonPropertyName("uptime")]
    public long UptimeSeconds { get; set; }
}

/// <summary>
/// Time information returned by GetTime command.
/// </summary>
public class TimeInfo
{
    [JsonPropertyName("utcTime")]
    public long UtcTimeTicks { get; set; }

    [JsonPropertyName("localTime")]
    public long LocalTimeTicks { get; set; }

    [JsonPropertyName("timeZoneId")]
    public string TimeZoneId { get; set; } = string.Empty;

    [JsonPropertyName("utcOffset")]
    public double UtcOffsetHours { get; set; }

    /// <summary>
    /// Get UTC time as DateTime.
    /// </summary>
    [JsonIgnore]
    public DateTime UtcTime => new DateTime(UtcTimeTicks, DateTimeKind.Utc);

    /// <summary>
    /// Get local time as DateTime.
    /// </summary>
    [JsonIgnore]
    public DateTime LocalTime => new DateTime(LocalTimeTicks, DateTimeKind.Local);
}
