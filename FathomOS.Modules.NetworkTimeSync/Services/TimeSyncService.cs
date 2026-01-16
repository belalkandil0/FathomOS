namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FathomOS.Modules.NetworkTimeSync.Enums;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Service for communicating with TimeSyncAgent on remote computers.
/// </summary>
public class TimeSyncService
{
    private readonly string _secret;
    private readonly int _timeoutMs;

    // JSON options for case-insensitive deserialization
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TimeSyncService(string secret, int timeoutMs = 5000)
    {
        _secret = secret;
        _timeoutMs = timeoutMs;
    }

    /// <summary>
    /// Ping an agent to check if it's alive.
    /// </summary>
    public async Task<(bool Success, string? Error)> PingAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = AgentRequest.CreatePing(_secret);
            var response = await SendRequestAsync(ipAddress, port, request, cancellationToken);
            return (response?.IsSuccess == true, response?.Error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Get current time from a remote computer.
    /// </summary>
    public async Task<(TimeInfo? TimeInfo, string? Error)> GetTimeAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = AgentRequest.CreateGetTime(_secret);
            var response = await SendRequestAsync(ipAddress, port, request, cancellationToken);

            if (response?.IsSuccess != true)
                return (null, response?.Error ?? "Unknown error");

            if (string.IsNullOrEmpty(response.Payload))
                return (null, "No time data in response");

            var timeInfo = JsonSerializer.Deserialize<TimeInfo>(response.Payload, JsonOptions);
            return (timeInfo, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Set time on a remote computer.
    /// </summary>
    public async Task<(bool Success, string? Error)> SetTimeAsync(string ipAddress, int port, DateTime utcTime, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = AgentRequest.CreateSetTime(_secret, utcTime);
            var response = await SendRequestAsync(ipAddress, port, request, cancellationToken);
            return (response?.IsSuccess == true, response?.Error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Get computer information from remote agent.
    /// </summary>
    public async Task<(ComputerInfo? Info, string? Error)> GetInfoAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = AgentRequest.CreateGetInfo(_secret);
            var response = await SendRequestAsync(ipAddress, port, request, cancellationToken);

            if (response?.IsSuccess != true)
                return (null, response?.Error ?? "Unknown error");

            if (string.IsNullOrEmpty(response.Payload))
                return (null, "No info data in response");

            var info = JsonSerializer.Deserialize<ComputerInfo>(response.Payload, JsonOptions);
            return (info, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// Trigger NTP sync on a remote computer.
    /// </summary>
    public async Task<(bool Success, string? Error)> SyncNtpAsync(string ipAddress, int port, string ntpServer, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = AgentRequest.CreateSyncNtp(_secret, ntpServer);
            var response = await SendRequestAsync(ipAddress, port, request, cancellationToken);
            return (response?.IsSuccess == true, response?.Error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Check time drift between reference and remote computer.
    /// </summary>
    public async Task<(double? DriftSeconds, string? Error)> CheckTimeDriftAsync(string ipAddress, int port, DateTime referenceUtcTime, CancellationToken cancellationToken = default)
    {
        var (timeInfo, error) = await GetTimeAsync(ipAddress, port, cancellationToken);
        
        if (timeInfo == null)
            return (null, error);

        var drift = (timeInfo.UtcTime - referenceUtcTime).TotalSeconds;
        return (drift, null);
    }

    /// <summary>
    /// Send request to agent and get response.
    /// </summary>
    private async Task<AgentResponse?> SendRequestAsync(string ipAddress, int port, AgentRequest request, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        
        // Connect with timeout
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectCts.CancelAfter(_timeoutMs);
        
        try
        {
            await client.ConnectAsync(ipAddress, port, connectCts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Connection timeout to {ipAddress}:{port}");
        }

        using var stream = client.GetStream();
        stream.ReadTimeout = _timeoutMs;
        stream.WriteTimeout = _timeoutMs;

        // Send request
        var requestJson = JsonSerializer.Serialize(request);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson + "\n");
        await stream.WriteAsync(requestBytes, cancellationToken);

        // Read response
        var buffer = new byte[8192];
        var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
        
        if (bytesRead == 0)
            throw new Exception("No response from agent");

        var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        return JsonSerializer.Deserialize<AgentResponse>(responseJson, JsonOptions);
    }
}

/// <summary>
/// Result of a sync operation for multiple computers.
/// </summary>
public class SyncResult
{
    public int TotalComputers { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<(string IpAddress, string? Error)> Failures { get; set; } = new();
    public TimeSpan Duration { get; set; }
}
