using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace FathomOS.TimeSyncAgent.Services;

/// <summary>
/// Service for getting and setting system time.
/// </summary>
public class TimeService
{
    // Windows API for setting system time
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetSystemTime(ref SYSTEMTIME st);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEMTIME
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    /// <summary>
    /// Get current time information.
    /// </summary>
    public TimeInfo GetTimeInfo()
    {
        var utcNow = DateTime.UtcNow;
        var localNow = DateTime.Now;
        var tz = TimeZoneInfo.Local;

        return new TimeInfo
        {
            UtcTimeTicks = utcNow.Ticks,
            LocalTimeTicks = localNow.Ticks,
            TimeZoneId = tz.Id,
            UtcOffsetHours = tz.GetUtcOffset(localNow).TotalHours
        };
    }

    /// <summary>
    /// Set system time (requires admin privileges).
    /// </summary>
    public (bool Success, string? Error) SetSystemTime(DateTime utcTime)
    {
        try
        {
            var st = new SYSTEMTIME
            {
                wYear = (ushort)utcTime.Year,
                wMonth = (ushort)utcTime.Month,
                wDay = (ushort)utcTime.Day,
                wDayOfWeek = (ushort)utcTime.DayOfWeek,
                wHour = (ushort)utcTime.Hour,
                wMinute = (ushort)utcTime.Minute,
                wSecond = (ushort)utcTime.Second,
                wMilliseconds = (ushort)utcTime.Millisecond
            };

            if (SetSystemTime(ref st))
            {
                return (true, null);
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                return (false, $"SetSystemTime failed with error code: {error}");
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Sync time from an NTP server.
    /// </summary>
    public (bool Success, string? Error) SyncFromNtp(string ntpServer)
    {
        try
        {
            var ntpTime = GetNtpTime(ntpServer);
            if (!ntpTime.HasValue)
            {
                return (false, $"Failed to get time from NTP server: {ntpServer}");
            }

            return SetSystemTime(ntpTime.Value);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Get time from an NTP server.
    /// </summary>
    public DateTime? GetNtpTime(string ntpServer, int timeoutMs = 5000)
    {
        try
        {
            // NTP uses port 123
            const int ntpPort = 123;
            
            // NTP message is 48 bytes
            var ntpData = new byte[48];
            
            // Set the Leap Indicator, Version Number and Mode values
            // LI = 0 (no warning), VN = 4 (IPv4 only), Mode = 3 (client)
            ntpData[0] = 0x23; // 0b00100011
            
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = timeoutMs;
            socket.SendTimeout = timeoutMs;
            
            socket.Connect(ntpServer, ntpPort);
            socket.Send(ntpData);
            socket.Receive(ntpData);
            
            // Offset to get to the "Transmit Timestamp" field (48 bytes total, starts at byte 40)
            const byte serverReplyTime = 40;
            
            // Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
            
            // Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);
            
            // Convert from big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);
            
            // Calculate milliseconds
            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            
            // NTP time is from 1900-01-01
            var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var networkDateTime = ntpEpoch.AddMilliseconds((long)milliseconds);
            
            return networkDateTime;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"NTP error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Alternative NTP sync using Windows w32tm command.
    /// </summary>
    public (bool Success, string? Error) SyncFromNtpViaW32tm(string ntpServer)
    {
        try
        {
            // Configure the NTP server
            var configProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "w32tm",
                    Arguments = $"/config /manualpeerlist:\"{ntpServer}\" /syncfromflags:manual /reliable:yes /update",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            configProcess.Start();
            configProcess.WaitForExit(10000);
            
            if (configProcess.ExitCode != 0)
            {
                var error = configProcess.StandardError.ReadToEnd();
                return (false, $"w32tm config failed: {error}");
            }

            // Force resync
            var syncProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "w32tm",
                    Arguments = "/resync /force",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            syncProcess.Start();
            syncProcess.WaitForExit(30000);
            
            if (syncProcess.ExitCode != 0)
            {
                var error = syncProcess.StandardError.ReadToEnd();
                return (false, $"w32tm resync failed: {error}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Get computer information.
    /// </summary>
    public ComputerInfo GetComputerInfo()
    {
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        var utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalHours;
        
        return new ComputerInfo
        {
            Hostname = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            AgentVersion = "1.0.2",
            CurrentTimeUtc = DateTime.UtcNow.Ticks,
            TimeZone = TimeZoneInfo.Local.Id,
            UtcOffset = utcOffset,
            UptimeSeconds = (long)uptime.TotalSeconds
        };
    }

    private static uint SwapEndianness(ulong x)
    {
        return (uint)(((x & 0x000000ff) << 24) +
                      ((x & 0x0000ff00) << 8) +
                      ((x & 0x00ff0000) >> 8) +
                      ((x & 0xff000000) >> 24));
    }
}

/// <summary>
/// Time information structure.
/// </summary>
public class TimeInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("utcTime")]
    public long UtcTimeTicks { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("localTime")]
    public long LocalTimeTicks { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("timeZoneId")]
    public string TimeZoneId { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("utcOffset")]
    public double UtcOffsetHours { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime UtcTime => new DateTime(UtcTimeTicks, DateTimeKind.Utc);
    
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime LocalTime => new DateTime(LocalTimeTicks, DateTimeKind.Local);
}

/// <summary>
/// Computer information structure.
/// </summary>
public class ComputerInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("osVersion")]
    public string OsVersion { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("agentVersion")]
    public string AgentVersion { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("currentTimeUtc")]
    public long CurrentTimeUtc { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("utcOffset")]
    public double UtcOffset { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("uptime")]
    public long UptimeSeconds { get; set; }
}
