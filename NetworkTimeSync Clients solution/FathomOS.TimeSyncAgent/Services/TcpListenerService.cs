using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FathomOS.TimeSyncAgent.Models;

namespace FathomOS.TimeSyncAgent.Services;

/// <summary>
/// TCP listener service that handles incoming time sync commands.
/// Includes rate limiting for protection against brute-force and DoS attacks.
/// </summary>
public class TcpListenerService : BackgroundService
{
    private readonly AgentConfiguration _config;
    private readonly TimeService _timeService;
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<TcpListenerService> _logger;
    private TcpListener? _listener;

    // Connection tracking
    private static DateTime _serviceStartTime = DateTime.Now;
    private static int _totalConnections;
    private static DateTime? _lastConnectionTime;
    private static readonly object _statsLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public TcpListenerService(
        IOptions<AgentConfiguration> config,
        TimeService timeService,
        RateLimiter rateLimiter,
        ILogger<TcpListenerService> logger)
    {
        _config = config.Value;
        _timeService = timeService;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, _config.Port);
        _serviceStartTime = DateTime.Now;
        
        try
        {
            _listener.Start();
            _logger.LogInformation("Time Sync Agent listening on port {Port}", _config.Port);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                    var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

                    // Check rate limit before processing
                    var (rateLimitResult, retryAfter) = _rateLimiter.CheckRateLimit(clientEndpoint);

                    if (rateLimitResult != RateLimiter.RateLimitResult.Allowed &&
                        rateLimitResult != RateLimiter.RateLimitResult.Disabled)
                    {
                        // Rate limit exceeded - reject connection with appropriate response
                        await RejectConnectionAsync(client, rateLimitResult, retryAfter);
                        continue;
                    }

                    // Track connection
                    lock (_statsLock)
                    {
                        _totalConnections++;
                        _lastConnectionTime = DateTime.Now;
                    }

                    _ = HandleClientAsync(client, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting client connection");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting TCP listener");
        }
        finally
        {
            _listener?.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogDebug("Client connected: {Endpoint}", clientEndpoint);

        try
        {
            client.ReceiveTimeout = _config.ConnectionTimeoutMs;
            client.SendTimeout = _config.ConnectionTimeoutMs;

            // Use UTF-8 WITHOUT BOM - BOM causes JSON parsing errors on client side
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, utf8NoBom);
            using var writer = new StreamWriter(stream, utf8NoBom) { AutoFlush = true };

            // Read request
            var requestJson = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(requestJson))
            {
                _logger.LogWarning("Empty request from {Endpoint}", clientEndpoint);
                return;
            }

            _logger.LogDebug("Request from {Endpoint}: {Request}", clientEndpoint, requestJson);

            // Parse request
            AgentRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<AgentRequest>(requestJson, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid request JSON from {Endpoint}", clientEndpoint);
                await SendErrorAsync(writer, "Invalid request format");
                return;
            }

            if (request == null)
            {
                await SendErrorAsync(writer, "Null request");
                return;
            }

            // Validate authentication
            if (!ValidateAuth(request))
            {
                _logger.LogWarning("Authentication failed from {Endpoint}", clientEndpoint);

                // Record failed attempt for rate limiting
                _rateLimiter.RecordFailedAttempt(clientEndpoint);

                await SendErrorAsync(writer, "Authentication failed", "AuthFailed");
                return;
            }

            // Record successful authentication
            _rateLimiter.RecordSuccessfulAuth(clientEndpoint);

            // Process command
            var response = ProcessCommand(request);
            
            // Send response
            var responseJson = JsonSerializer.Serialize(response, JsonOptions);
            await writer.WriteLineAsync(responseJson);
            
            _logger.LogDebug("Response to {Endpoint}: {Response}", clientEndpoint, responseJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client {Endpoint}", clientEndpoint);
        }
        finally
        {
            client.Close();
            _logger.LogDebug("Client disconnected: {Endpoint}", clientEndpoint);
        }
    }

    private bool ValidateAuth(AgentRequest request)
    {
        // Check timestamp is within 5 minutes
        var requestTime = new DateTime(request.Timestamp, DateTimeKind.Utc);
        var timeDiff = Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes);
        if (timeDiff > 5)
        {
            _logger.LogWarning("Request timestamp too old: {TimeDiff} minutes", timeDiff);
            return false;
        }

        // Validate auth hash
        var expectedAuth = GenerateAuth(_config.SharedSecret, request.Timestamp);
        return request.Auth == expectedAuth;
    }

    private static string GenerateAuth(string secret, long timestamp)
    {
        var data = $"{secret}:{timestamp}";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }

    private AgentResponse ProcessCommand(AgentRequest request)
    {
        _logger.LogInformation("Processing command: {Command}", request.Command);

        return request.Command.ToUpper() switch
        {
            "PING" => HandlePing(),
            "GETTIME" => HandleGetTime(),
            "SETTIME" => HandleSetTime(request.Payload),
            "GETINFO" => HandleGetInfo(),
            "SYNCNTP" => HandleSyncNtp(request.Payload),
            _ => CreateErrorResponse($"Unknown command: {request.Command}", "InvalidCommand")
        };
    }

    private AgentResponse HandlePing()
    {
        return new AgentResponse
        {
            Status = "Success",
            Timestamp = DateTime.UtcNow.Ticks,
            Payload = "Pong"
        };
    }

    private AgentResponse HandleGetTime()
    {
        var timeInfo = _timeService.GetTimeInfo();
        return new AgentResponse
        {
            Status = "Success",
            Timestamp = DateTime.UtcNow.Ticks,
            Payload = JsonSerializer.Serialize(timeInfo, JsonOptions)
        };
    }

    private AgentResponse HandleSetTime(string? payload)
    {
        if (!_config.AllowTimeSet)
        {
            return CreateErrorResponse("Time setting is disabled on this agent");
        }

        if (string.IsNullOrEmpty(payload) || !long.TryParse(payload, out var ticks))
        {
            return CreateErrorResponse("Invalid time payload");
        }

        var utcTime = new DateTime(ticks, DateTimeKind.Utc);
        var (success, error) = _timeService.SetSystemTime(utcTime);

        if (success)
        {
            _logger.LogInformation("System time set to {Time}", utcTime);
            return new AgentResponse
            {
                Status = "Success",
                Timestamp = DateTime.UtcNow.Ticks
            };
        }
        else
        {
            _logger.LogError("Failed to set system time: {Error}", error);
            return CreateErrorResponse(error ?? "Unknown error");
        }
    }

    private AgentResponse HandleGetInfo()
    {
        var info = _timeService.GetComputerInfo();

        // Add connection statistics
        int totalConns;
        DateTime? lastConn;
        lock (_statsLock)
        {
            totalConns = _totalConnections;
            lastConn = _lastConnectionTime;
        }

        // Get rate limiter statistics
        var rateLimiterStats = _rateLimiter.GetStats();

        // Create extended response with connection stats and rate limiter info
        var extendedInfo = new
        {
            info.Hostname,
            info.OsVersion,
            info.TimeZone,
            info.UtcOffset,
            AgentVersion = "1.0.3",
            StartTime = _serviceStartTime.ToString("o"),
            TotalConnections = totalConns,
            LastConnectionTime = lastConn?.ToString("o"),
            Port = _config.Port,
            RateLimiting = new
            {
                rateLimiterStats.IsEnabled,
                rateLimiterStats.TotalTrackedIps,
                rateLimiterStats.TotalBlockedIps,
                rateLimiterStats.TotalFailedAttemptIps,
                rateLimiterStats.RecentTotalRequests
            }
        };

        return new AgentResponse
        {
            Status = "Success",
            Timestamp = DateTime.UtcNow.Ticks,
            Payload = JsonSerializer.Serialize(extendedInfo, JsonOptions)
        };
    }

    private AgentResponse HandleSyncNtp(string? payload)
    {
        if (!_config.AllowNtpSync)
        {
            return CreateErrorResponse("NTP sync is disabled on this agent");
        }

        var ntpServer = string.IsNullOrEmpty(payload) ? "time.windows.com" : payload;
        
        _logger.LogInformation("Syncing time from NTP server: {Server}", ntpServer);
        
        // Try direct NTP first, fall back to w32tm
        var (success, error) = _timeService.SyncFromNtp(ntpServer);
        
        if (!success)
        {
            _logger.LogWarning("Direct NTP sync failed, trying w32tm: {Error}", error);
            (success, error) = _timeService.SyncFromNtpViaW32tm(ntpServer);
        }

        if (success)
        {
            _logger.LogInformation("NTP sync completed successfully");
            return new AgentResponse
            {
                Status = "Success",
                Timestamp = DateTime.UtcNow.Ticks
            };
        }
        else
        {
            _logger.LogError("NTP sync failed: {Error}", error);
            return CreateErrorResponse(error ?? "NTP sync failed");
        }
    }

    private static AgentResponse CreateErrorResponse(string message, string status = "Failed")
    {
        return new AgentResponse
        {
            Status = status,
            Error = message,
            Timestamp = DateTime.UtcNow.Ticks
        };
    }

    private static async Task SendErrorAsync(StreamWriter writer, string message, string status = "Failed")
    {
        var response = CreateErrorResponse(message, status);
        var json = JsonSerializer.Serialize(response, JsonOptions);
        await writer.WriteLineAsync(json);
    }

    /// <summary>
    /// Rejects a connection due to rate limiting.
    /// </summary>
    private async Task RejectConnectionAsync(TcpClient client, RateLimiter.RateLimitResult result, int? retryAfter)
    {
        var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";

        try
        {
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, utf8NoBom) { AutoFlush = true };

            var (message, status) = result switch
            {
                RateLimiter.RateLimitResult.IpRateLimitExceeded =>
                    ("Too many requests from your IP address. Please try again later.", "RateLimited"),
                RateLimiter.RateLimitResult.TotalRateLimitExceeded =>
                    ("Server is busy. Please try again later.", "RateLimited"),
                RateLimiter.RateLimitResult.BlockedByBackoff =>
                    ($"Too many failed attempts. Blocked for {retryAfter} seconds.", "Blocked"),
                _ =>
                    ("Request rejected.", "Rejected")
            };

            var response = new AgentResponse
            {
                Status = status,
                Error = message,
                Timestamp = DateTime.UtcNow.Ticks,
                Payload = retryAfter?.ToString()
            };

            var json = JsonSerializer.Serialize(response, JsonOptions);
            await writer.WriteLineAsync(json);

            _logger.LogDebug("Rejected connection from {Endpoint}: {Result}", clientEndpoint, result);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error sending rejection response to {Endpoint}", clientEndpoint);
        }
        finally
        {
            client.Close();
        }
    }
}

/// <summary>
/// Request message from the Fathom OS module.
/// </summary>
public class AgentRequest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("auth")]
    public string Auth { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

/// <summary>
/// Response message to the Fathom OS module.
/// </summary>
public class AgentResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}
