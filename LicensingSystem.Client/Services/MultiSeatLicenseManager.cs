// LicensingSystem.Client/Services/MultiSeatLicenseManager.cs
// Manages multi-seat (concurrent user) license enforcement
// Tracks active sessions, enforces seat limits, and handles session timeout

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LicensingSystem.Client.Services;

/// <summary>
/// Interface for multi-seat license management
/// </summary>
public interface IMultiSeatLicenseManager
{
    /// <summary>
    /// Acquires a seat for the current session
    /// </summary>
    Task<SeatAcquisitionResult> AcquireSeatAsync();

    /// <summary>
    /// Releases the current seat
    /// </summary>
    Task<bool> ReleaseSeatAsync();

    /// <summary>
    /// Sends a heartbeat to maintain the seat
    /// </summary>
    Task<SeatHeartbeatResult> SendHeartbeatAsync();

    /// <summary>
    /// Gets the current seat status
    /// </summary>
    Task<SeatStatus> GetSeatStatusAsync();

    /// <summary>
    /// Gets all active sessions for this license
    /// </summary>
    Task<List<ActiveSession>> GetActiveSessionsAsync();

    /// <summary>
    /// Forces release of a specific session (admin function)
    /// </summary>
    Task<bool> ForceReleaseSessionAsync(string sessionId);

    /// <summary>
    /// Whether a seat is currently acquired
    /// </summary>
    bool HasSeat { get; }
}

/// <summary>
/// Manages multi-seat (concurrent user) license enforcement.
/// Tracks active sessions, enforces seat limits, and handles session timeouts.
/// </summary>
public class MultiSeatLicenseManager : IMultiSeatLicenseManager, IDisposable
{
    private readonly string _licenseId;
    private readonly string _serverUrl;
    private readonly int _maxSeats;
    private readonly HttpClient _httpClient;
    private readonly System.Timers.Timer _heartbeatTimer;

    private string? _currentSessionId;
    private DateTime _seatAcquiredAt;
    private bool _disposed;

    /// <summary>
    /// Session timeout in minutes (server-side default is 5 minutes)
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Heartbeat interval in seconds (should be less than timeout)
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Event fired when seat is lost (timeout or revoked)
    /// </summary>
    public event EventHandler<SeatLostEventArgs>? SeatLost;

    /// <summary>
    /// Event fired when seat limit is reached
    /// </summary>
    public event EventHandler<SeatLimitReachedEventArgs>? SeatLimitReached;

    /// <summary>
    /// Creates a new MultiSeatLicenseManager instance
    /// </summary>
    /// <param name="licenseId">License ID</param>
    /// <param name="serverUrl">Server URL for seat management</param>
    /// <param name="maxSeats">Maximum seats allowed by license</param>
    public MultiSeatLicenseManager(string licenseId, string serverUrl, int maxSeats = 1)
    {
        _licenseId = licenseId;
        _serverUrl = serverUrl.TrimEnd('/');
        _maxSeats = maxSeats;

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _heartbeatTimer = new System.Timers.Timer(HeartbeatIntervalSeconds * 1000);
        _heartbeatTimer.Elapsed += async (s, e) => await SendHeartbeatInternalAsync();
        _heartbeatTimer.AutoReset = true;
    }

    /// <summary>
    /// Whether a seat is currently acquired
    /// </summary>
    public bool HasSeat => !string.IsNullOrEmpty(_currentSessionId);

    /// <summary>
    /// Current session ID
    /// </summary>
    public string? CurrentSessionId => _currentSessionId;

    /// <summary>
    /// Acquires a seat for the current session
    /// </summary>
    public async Task<SeatAcquisitionResult> AcquireSeatAsync()
    {
        if (HasSeat)
        {
            return new SeatAcquisitionResult
            {
                Success = true,
                SessionId = _currentSessionId!,
                Message = "Seat already acquired."
            };
        }

        try
        {
            var request = new AcquireSeatRequest
            {
                LicenseId = _licenseId,
                HardwareFingerprint = GetHardwareFingerprint(),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                TimeoutMinutes = SessionTimeoutMinutes
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/seats/acquire",
                request);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AcquireSeatResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null)
            {
                return new SeatAcquisitionResult
                {
                    Success = false,
                    Message = "Invalid server response."
                };
            }

            if (result.Success)
            {
                _currentSessionId = result.SessionId;
                _seatAcquiredAt = DateTime.UtcNow;

                // Start heartbeat timer
                _heartbeatTimer.Start();

                return new SeatAcquisitionResult
                {
                    Success = true,
                    SessionId = result.SessionId!,
                    SeatsUsed = result.SeatsUsed,
                    SeatsAvailable = result.SeatsAvailable,
                    Message = "Seat acquired successfully."
                };
            }
            else
            {
                // Seat limit reached
                SeatLimitReached?.Invoke(this, new SeatLimitReachedEventArgs
                {
                    MaxSeats = _maxSeats,
                    SeatsUsed = result.SeatsUsed,
                    ActiveSessions = result.ActiveSessions
                });

                return new SeatAcquisitionResult
                {
                    Success = false,
                    SeatsUsed = result.SeatsUsed,
                    SeatsAvailable = result.SeatsAvailable,
                    Message = result.Message ?? $"Seat limit reached ({result.SeatsUsed}/{_maxSeats} seats in use).",
                    ActiveSessions = result.ActiveSessions
                };
            }
        }
        catch (HttpRequestException ex)
        {
            return new SeatAcquisitionResult
            {
                Success = false,
                Message = $"Network error: {ex.Message}. Will retry when online."
            };
        }
        catch (Exception ex)
        {
            return new SeatAcquisitionResult
            {
                Success = false,
                Message = $"Error acquiring seat: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Releases the current seat
    /// </summary>
    public async Task<bool> ReleaseSeatAsync()
    {
        if (!HasSeat)
            return true;

        _heartbeatTimer.Stop();

        try
        {
            var request = new ReleaseSeatRequest
            {
                LicenseId = _licenseId,
                SessionId = _currentSessionId!
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/seats/release",
                request);

            _currentSessionId = null;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            // Even if release fails, clear local state
            _currentSessionId = null;
            return false;
        }
    }

    /// <summary>
    /// Sends a heartbeat to maintain the seat
    /// </summary>
    public async Task<SeatHeartbeatResult> SendHeartbeatAsync()
    {
        if (!HasSeat)
        {
            return new SeatHeartbeatResult
            {
                Success = false,
                Message = "No seat acquired."
            };
        }

        return await SendHeartbeatInternalAsync();
    }

    /// <summary>
    /// Gets the current seat status
    /// </summary>
    public async Task<SeatStatus> GetSeatStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_serverUrl}/api/license/seats/status?licenseId={_licenseId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<SeatStatus>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (status != null)
                {
                    status.HasLocalSeat = HasSeat;
                    status.LocalSessionId = _currentSessionId;
                    return status;
                }
            }
        }
        catch { }

        return new SeatStatus
        {
            MaxSeats = _maxSeats,
            HasLocalSeat = HasSeat,
            LocalSessionId = _currentSessionId,
            Error = "Failed to get seat status from server."
        };
    }

    /// <summary>
    /// Gets all active sessions for this license
    /// </summary>
    public async Task<List<ActiveSession>> GetActiveSessionsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_serverUrl}/api/license/seats/sessions?licenseId={_licenseId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ActiveSession>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ActiveSession>();
            }
        }
        catch { }

        return new List<ActiveSession>();
    }

    /// <summary>
    /// Forces release of a specific session (admin function)
    /// </summary>
    public async Task<bool> ForceReleaseSessionAsync(string sessionId)
    {
        try
        {
            var request = new ReleaseSeatRequest
            {
                LicenseId = _licenseId,
                SessionId = sessionId
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/seats/force-release",
                request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<SeatHeartbeatResult> SendHeartbeatInternalAsync()
    {
        if (!HasSeat)
        {
            return new SeatHeartbeatResult
            {
                Success = false,
                Message = "No seat acquired."
            };
        }

        try
        {
            var request = new SeatHeartbeatRequest
            {
                LicenseId = _licenseId,
                SessionId = _currentSessionId!
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_serverUrl}/api/license/seats/heartbeat",
                request);

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SeatHeartbeatResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || !result.Success)
            {
                // Seat was lost
                HandleSeatLost(result?.Reason ?? "Session expired or revoked");

                return new SeatHeartbeatResult
                {
                    Success = false,
                    Message = result?.Message ?? "Heartbeat failed - seat lost."
                };
            }

            return new SeatHeartbeatResult
            {
                Success = true,
                SeatsUsed = result.SeatsUsed,
                TimeUntilExpiry = result.TimeUntilExpiry
            };
        }
        catch (HttpRequestException)
        {
            // Network error - don't lose seat immediately, let server timeout handle it
            return new SeatHeartbeatResult
            {
                Success = false,
                Message = "Network error - heartbeat will retry."
            };
        }
        catch (Exception ex)
        {
            return new SeatHeartbeatResult
            {
                Success = false,
                Message = $"Heartbeat error: {ex.Message}"
            };
        }
    }

    private void HandleSeatLost(string reason)
    {
        _heartbeatTimer.Stop();
        var sessionId = _currentSessionId;
        _currentSessionId = null;

        SeatLost?.Invoke(this, new SeatLostEventArgs
        {
            SessionId = sessionId ?? "",
            Reason = reason,
            LostAt = DateTime.UtcNow
        });
    }

    private string GetHardwareFingerprint()
    {
        // Use machine name and user as simple fingerprint
        // In production, use full MachineFingerprint class
        return $"{Environment.MachineName}:{Environment.UserName}".GetHashCode().ToString("X8");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Dispose();

            // Try to release seat on dispose
            _ = ReleaseSeatAsync();

            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

#region Request/Response DTOs

internal class AcquireSeatRequest
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("hardwareFingerprint")]
    public string HardwareFingerprint { get; set; } = string.Empty;

    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("timeoutMinutes")]
    public int TimeoutMinutes { get; set; }
}

internal class AcquireSeatResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("seatsUsed")]
    public int SeatsUsed { get; set; }

    [JsonPropertyName("seatsAvailable")]
    public int SeatsAvailable { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("activeSessions")]
    public List<ActiveSession>? ActiveSessions { get; set; }
}

internal class ReleaseSeatRequest
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
}

internal class SeatHeartbeatRequest
{
    [JsonPropertyName("licenseId")]
    public string LicenseId { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;
}

internal class SeatHeartbeatResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("seatsUsed")]
    public int SeatsUsed { get; set; }

    [JsonPropertyName("timeUntilExpiry")]
    public TimeSpan TimeUntilExpiry { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

#endregion

#region Public DTOs

/// <summary>
/// Result of a seat acquisition attempt
/// </summary>
public class SeatAcquisitionResult
{
    public bool Success { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public int SeatsUsed { get; set; }
    public int SeatsAvailable { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ActiveSession>? ActiveSessions { get; set; }
}

/// <summary>
/// Result of a heartbeat request
/// </summary>
public class SeatHeartbeatResult
{
    public bool Success { get; set; }
    public int SeatsUsed { get; set; }
    public TimeSpan TimeUntilExpiry { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Current seat status
/// </summary>
public class SeatStatus
{
    [JsonPropertyName("maxSeats")]
    public int MaxSeats { get; set; }

    [JsonPropertyName("seatsUsed")]
    public int SeatsUsed { get; set; }

    [JsonPropertyName("seatsAvailable")]
    public int SeatsAvailable => MaxSeats - SeatsUsed;

    [JsonPropertyName("activeSessions")]
    public List<ActiveSession> ActiveSessions { get; set; } = new();

    public bool HasLocalSeat { get; set; }
    public string? LocalSessionId { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Active session information
/// </summary>
public class ActiveSession
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("machineName")]
    public string MachineName { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("lastHeartbeat")]
    public DateTime LastHeartbeat { get; set; }

    [JsonPropertyName("isCurrentSession")]
    public bool IsCurrentSession { get; set; }

    /// <summary>
    /// Session duration
    /// </summary>
    public TimeSpan Duration => DateTime.UtcNow - StartedAt;

    /// <summary>
    /// Time since last heartbeat
    /// </summary>
    public TimeSpan TimeSinceHeartbeat => DateTime.UtcNow - LastHeartbeat;
}

/// <summary>
/// Event args when seat is lost
/// </summary>
public class SeatLostEventArgs : EventArgs
{
    public string SessionId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime LostAt { get; set; }
}

/// <summary>
/// Event args when seat limit is reached
/// </summary>
public class SeatLimitReachedEventArgs : EventArgs
{
    public int MaxSeats { get; set; }
    public int SeatsUsed { get; set; }
    public List<ActiveSession>? ActiveSessions { get; set; }

    /// <summary>
    /// User-friendly message
    /// </summary>
    public string Message => $"All {MaxSeats} license seats are currently in use. " +
        $"Please wait for another user to log out or contact your administrator.";
}

#endregion
