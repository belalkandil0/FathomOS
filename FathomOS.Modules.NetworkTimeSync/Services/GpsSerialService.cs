namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Service for receiving GPS time from a serial port using NMEA sentences (GGA/RMC).
/// Supports Varipos and other GPS devices outputting standard NMEA-0183 format.
/// </summary>
public class GpsSerialService : IDisposable
{
    private SerialPort? _serialPort;
    private readonly StringBuilder _buffer = new();
    private readonly object _lock = new();
    
    private DateTime? _lastGpsTime;
    private DateTime? _lastGpsDate;
    private DateTime _lastUpdateTime;
    private bool _isConnected;
    private string _lastError = string.Empty;
    private int _validSentenceCount;
    private int _satelliteCount;
    private GpsFixQuality _fixQuality = GpsFixQuality.NoFix;

    /// <summary>
    /// Event raised when GPS time is updated.
    /// </summary>
    public event EventHandler<GpsTimeEventArgs>? TimeUpdated;

    /// <summary>
    /// Event raised when connection status changes.
    /// </summary>
    public event EventHandler<bool>? ConnectionChanged;

    /// <summary>
    /// Whether the GPS is currently connected and receiving data.
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Last received GPS UTC time.
    /// </summary>
    public DateTime? LastGpsTime
    {
        get
        {
            lock (_lock)
            {
                return _lastGpsTime.HasValue && _lastGpsDate.HasValue
                    ? _lastGpsDate.Value.Date + _lastGpsTime.Value.TimeOfDay
                    : _lastGpsTime;
            }
        }
    }

    /// <summary>
    /// Number of satellites in view.
    /// </summary>
    public int SatelliteCount => _satelliteCount;

    /// <summary>
    /// Current GPS fix quality.
    /// </summary>
    public GpsFixQuality FixQuality => _fixQuality;

    /// <summary>
    /// Last error message.
    /// </summary>
    public string LastError => _lastError;

    /// <summary>
    /// Get available serial port names.
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames();
    }

    /// <summary>
    /// Connect to a serial port and start receiving NMEA data.
    /// </summary>
    public bool Connect(GpsSerialConfiguration config)
    {
        try
        {
            Disconnect();

            _serialPort = new SerialPort
            {
                PortName = config.PortName,
                BaudRate = config.BaudRate,
                DataBits = config.DataBits,
                Parity = config.Parity,
                StopBits = config.StopBits,
                Handshake = Handshake.None,
                ReadTimeout = 2000,
                WriteTimeout = 2000,
                Encoding = Encoding.ASCII
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.ErrorReceived += SerialPort_ErrorReceived;
            _serialPort.Open();

            _isConnected = true;
            _lastError = string.Empty;
            _validSentenceCount = 0;
            
            ConnectionChanged?.Invoke(this, true);
            
            System.Diagnostics.Debug.WriteLine($"[GPS] Connected to {config.PortName} at {config.BaudRate} baud");
            return true;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _isConnected = false;
            System.Diagnostics.Debug.WriteLine($"[GPS] Connection error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Disconnect from the serial port.
    /// </summary>
    public void Disconnect()
    {
        if (_serialPort != null)
        {
            try
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.ErrorReceived -= SerialPort_ErrorReceived;
                
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
                
                _serialPort.Dispose();
            }
            catch { }
            
            _serialPort = null;
        }

        _isConnected = false;
        _buffer.Clear();
        ConnectionChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Get current GPS time information for sync operations.
    /// </summary>
    public GpsTimeInfo? GetCurrentTime()
    {
        lock (_lock)
        {
            if (!_lastGpsTime.HasValue)
                return null;

            // Check if data is stale (more than 5 seconds old)
            if ((DateTime.Now - _lastUpdateTime).TotalSeconds > 5)
                return null;

            var utcTime = _lastGpsDate.HasValue
                ? _lastGpsDate.Value.Date + _lastGpsTime.Value.TimeOfDay
                : DateTime.UtcNow.Date + _lastGpsTime.Value.TimeOfDay;

            return new GpsTimeInfo
            {
                UtcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc),
                FixQuality = _fixQuality,
                SatelliteCount = _satelliteCount,
                IsValid = _fixQuality != GpsFixQuality.NoFix
            };
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return;

            var data = _serialPort.ReadExisting();
            _buffer.Append(data);

            // Process complete sentences
            ProcessBuffer();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Read error: {ex.Message}");
        }
    }

    private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        _lastError = $"Serial error: {e.EventType}";
        System.Diagnostics.Debug.WriteLine($"[GPS] {_lastError}");
    }

    private void ProcessBuffer()
    {
        var content = _buffer.ToString();
        var lines = content.Split('\n');

        // Keep incomplete last line in buffer
        _buffer.Clear();
        if (!content.EndsWith("\n") && lines.Length > 0)
        {
            _buffer.Append(lines[^1]);
            lines = lines[..^1];
        }

        foreach (var line in lines)
        {
            var trimmed = line.Trim('\r', '\n', ' ');
            if (trimmed.StartsWith("$"))
            {
                ProcessNmeaSentence(trimmed);
            }
        }
    }

    private void ProcessNmeaSentence(string sentence)
    {
        try
        {
            // Verify checksum
            if (!VerifyChecksum(sentence))
                return;

            // Remove checksum for parsing
            var checksumIndex = sentence.LastIndexOf('*');
            if (checksumIndex > 0)
                sentence = sentence[..checksumIndex];

            var parts = sentence.Split(',');
            if (parts.Length < 2)
                return;

            var sentenceType = parts[0];

            // Handle different NMEA sentence types
            switch (sentenceType)
            {
                case "$GPGGA":
                case "$GNGGA":
                    ParseGGA(parts);
                    break;

                case "$GPRMC":
                case "$GNRMC":
                    ParseRMC(parts);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPS] Parse error: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse GGA sentence for time, fix quality, and satellite count.
    /// Format: $GPGGA,hhmmss.ss,llll.ll,a,yyyyy.yy,a,x,xx,x.x,x.x,M,x.x,M,x.x,xxxx*hh
    /// </summary>
    private void ParseGGA(string[] parts)
    {
        if (parts.Length < 10)
            return;

        // Parse time (field 1): HHMMSS.ss
        var time = ParseNmeaTime(parts[1]);
        if (!time.HasValue)
            return;

        // Parse fix quality (field 6)
        if (int.TryParse(parts[6], out var quality))
        {
            _fixQuality = (GpsFixQuality)Math.Min(quality, 5);
        }

        // Parse satellite count (field 7)
        if (int.TryParse(parts[7], out var sats))
        {
            _satelliteCount = sats;
        }

        lock (_lock)
        {
            _lastGpsTime = time.Value;
            _lastUpdateTime = DateTime.Now;
            _validSentenceCount++;
        }

        // Raise event
        TimeUpdated?.Invoke(this, new GpsTimeEventArgs
        {
            UtcTime = time.Value,
            FixQuality = _fixQuality,
            SatelliteCount = _satelliteCount,
            SentenceType = "GGA"
        });

        System.Diagnostics.Debug.WriteLine($"[GPS] GGA: {time.Value:HH:mm:ss.fff} Fix:{_fixQuality} Sats:{_satelliteCount}");
    }

    /// <summary>
    /// Parse RMC sentence for time and date.
    /// Format: $GPRMC,hhmmss.ss,A,llll.ll,a,yyyyy.yy,a,x.x,x.x,ddmmyy,x.x,a*hh
    /// </summary>
    private void ParseRMC(string[] parts)
    {
        if (parts.Length < 10)
            return;

        // Parse time (field 1): HHMMSS.ss
        var time = ParseNmeaTime(parts[1]);
        
        // Parse date (field 9): DDMMYY
        var date = ParseNmeaDate(parts[9]);

        // Parse status (field 2): A=Active, V=Void
        var isValid = parts[2] == "A";

        if (time.HasValue)
        {
            lock (_lock)
            {
                _lastGpsTime = time.Value;
                if (date.HasValue)
                {
                    _lastGpsDate = date.Value;
                }
                _lastUpdateTime = DateTime.Now;
                _validSentenceCount++;
            }

            System.Diagnostics.Debug.WriteLine($"[GPS] RMC: {date?.ToString("yyyy-MM-dd") ?? "no date"} {time.Value:HH:mm:ss.fff} Valid:{isValid}");
        }
    }

    /// <summary>
    /// Parse NMEA time format: HHMMSS.ss
    /// </summary>
    private static DateTime? ParseNmeaTime(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr) || timeStr.Length < 6)
            return null;

        try
        {
            var hours = int.Parse(timeStr[..2]);
            var minutes = int.Parse(timeStr.Substring(2, 2));
            var seconds = double.Parse(timeStr[4..], CultureInfo.InvariantCulture);

            var totalSeconds = (int)seconds;
            var milliseconds = (int)((seconds - totalSeconds) * 1000);

            return new DateTime(1, 1, 1, hours, minutes, totalSeconds, milliseconds, DateTimeKind.Utc);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse NMEA date format: DDMMYY
    /// </summary>
    private static DateTime? ParseNmeaDate(string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr) || dateStr.Length < 6)
            return null;

        try
        {
            var day = int.Parse(dateStr[..2]);
            var month = int.Parse(dateStr.Substring(2, 2));
            var year = int.Parse(dateStr.Substring(4, 2));

            // Handle Y2K: assume 20xx for years 00-79, 19xx for 80-99
            year += year < 80 ? 2000 : 1900;

            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Verify NMEA checksum.
    /// </summary>
    private static bool VerifyChecksum(string sentence)
    {
        var asteriskIndex = sentence.LastIndexOf('*');
        if (asteriskIndex < 0 || asteriskIndex + 3 > sentence.Length)
            return true; // No checksum present, accept anyway

        var data = sentence.Substring(1, asteriskIndex - 1); // Skip $ and *
        var checksumStr = sentence.Substring(asteriskIndex + 1, 2);

        if (!int.TryParse(checksumStr, NumberStyles.HexNumber, null, out var expectedChecksum))
            return false;

        var calculatedChecksum = 0;
        foreach (var c in data)
        {
            calculatedChecksum ^= c;
        }

        return calculatedChecksum == expectedChecksum;
    }

    public void Dispose()
    {
        Disconnect();
    }
}

/// <summary>
/// GPS serial port configuration.
/// </summary>
public class GpsSerialConfiguration
{
    public string PortName { get; set; } = "COM1";
    public int BaudRate { get; set; } = 4800;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
}

/// <summary>
/// GPS time information.
/// </summary>
public class GpsTimeInfo
{
    public DateTime UtcTime { get; set; }
    public GpsFixQuality FixQuality { get; set; }
    public int SatelliteCount { get; set; }
    public bool IsValid { get; set; }
}

/// <summary>
/// GPS fix quality from GGA sentence.
/// </summary>
public enum GpsFixQuality
{
    NoFix = 0,
    GpsFix = 1,
    DgpsFix = 2,
    PpsFix = 3,
    RtkFixed = 4,
    RtkFloat = 5
}

/// <summary>
/// Event arguments for GPS time updates.
/// </summary>
public class GpsTimeEventArgs : EventArgs
{
    public DateTime UtcTime { get; set; }
    public GpsFixQuality FixQuality { get; set; }
    public int SatelliteCount { get; set; }
    public string SentenceType { get; set; } = string.Empty;
}
