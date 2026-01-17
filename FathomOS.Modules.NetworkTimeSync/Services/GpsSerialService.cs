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
    private readonly object _portLock = new();

    private DateTime? _lastGpsTime;
    private DateTime? _lastGpsDate;
    private DateTime _lastUpdateTime;
    private bool _isConnected;
    private bool _isDisposed;
    private string _lastError = string.Empty;
    private int _validSentenceCount;
    private int _satelliteCount;
    private int _timeoutErrorCount;
    private GpsFixQuality _fixQuality = GpsFixQuality.NoFix;

    // Configurable timeout values (in milliseconds)
    private const int DefaultReadTimeoutMs = 3000;
    private const int DefaultWriteTimeoutMs = 2000;
    private const int MaxConsecutiveTimeoutErrors = 5;

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
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(GpsSerialService));
        }

        SerialPort? newPort = null;
        bool success = false;

        try
        {
            Disconnect();

            lock (_portLock)
            {
                newPort = new SerialPort
                {
                    PortName = config.PortName,
                    BaudRate = config.BaudRate,
                    DataBits = config.DataBits,
                    Parity = config.Parity,
                    StopBits = config.StopBits,
                    Handshake = Handshake.None,
                    ReadTimeout = config.ReadTimeoutMs > 0 ? config.ReadTimeoutMs : DefaultReadTimeoutMs,
                    WriteTimeout = config.WriteTimeoutMs > 0 ? config.WriteTimeoutMs : DefaultWriteTimeoutMs,
                    Encoding = Encoding.ASCII
                };

                newPort.DataReceived += SerialPort_DataReceived;
                newPort.ErrorReceived += SerialPort_ErrorReceived;
                newPort.Open();

                _serialPort = newPort;
                _isConnected = true;
                _lastError = string.Empty;
                _validSentenceCount = 0;
                _timeoutErrorCount = 0;
                success = true;
            }

            ConnectionChanged?.Invoke(this, true);

            System.Diagnostics.Debug.WriteLine($"[GPS] Connected to {config.PortName} at {config.BaudRate} baud (ReadTimeout: {newPort.ReadTimeout}ms, WriteTimeout: {newPort.WriteTimeout}ms)");
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _lastError = $"Access denied to {config.PortName}. Port may be in use by another application.";
            _isConnected = false;
            System.Diagnostics.Debug.WriteLine($"[GPS] Access error: {ex.Message}");
            return false;
        }
        catch (System.IO.IOException ex)
        {
            _lastError = $"I/O error on {config.PortName}: {ex.Message}";
            _isConnected = false;
            System.Diagnostics.Debug.WriteLine($"[GPS] I/O error: {ex.Message}");
            return false;
        }
        catch (TimeoutException ex)
        {
            _lastError = $"Timeout connecting to {config.PortName}: {ex.Message}";
            _isConnected = false;
            System.Diagnostics.Debug.WriteLine($"[GPS] Timeout error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _isConnected = false;
            System.Diagnostics.Debug.WriteLine($"[GPS] Connection error: {ex.Message}");
            return false;
        }
        finally
        {
            // If connection failed, ensure the port is properly disposed
            if (!success && newPort != null)
            {
                try
                {
                    newPort.DataReceived -= SerialPort_DataReceived;
                    newPort.ErrorReceived -= SerialPort_ErrorReceived;
                    if (newPort.IsOpen)
                    {
                        newPort.Close();
                    }
                    newPort.Dispose();
                }
                catch (Exception disposeEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[GPS] Error disposing port after failed connection: {disposeEx.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Disconnect from the serial port.
    /// </summary>
    public void Disconnect()
    {
        SerialPort? portToDispose = null;

        lock (_portLock)
        {
            if (_serialPort != null)
            {
                portToDispose = _serialPort;
                _serialPort = null;
            }
            _isConnected = false;
        }

        if (portToDispose != null)
        {
            try
            {
                portToDispose.DataReceived -= SerialPort_DataReceived;
                portToDispose.ErrorReceived -= SerialPort_ErrorReceived;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPS] Error unsubscribing events: {ex.Message}");
            }

            try
            {
                if (portToDispose.IsOpen)
                {
                    // Discard any buffered data before closing
                    try
                    {
                        portToDispose.DiscardInBuffer();
                        portToDispose.DiscardOutBuffer();
                    }
                    catch { }

                    portToDispose.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPS] Error closing port: {ex.Message}");
            }

            try
            {
                portToDispose.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GPS] Error disposing port: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine("[GPS] Port disconnected and disposed");
        }

        lock (_lock)
        {
            _buffer.Clear();
        }

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
        SerialPort? port;

        lock (_portLock)
        {
            port = _serialPort;
            if (port == null || !_isConnected)
                return;
        }

        try
        {
            if (!port.IsOpen)
                return;

            var data = port.ReadExisting();

            // Reset timeout error count on successful read
            Interlocked.Exchange(ref _timeoutErrorCount, 0);

            lock (_lock)
            {
                _buffer.Append(data);
            }

            // Process complete sentences
            ProcessBuffer();
        }
        catch (TimeoutException ex)
        {
            var errorCount = Interlocked.Increment(ref _timeoutErrorCount);
            System.Diagnostics.Debug.WriteLine($"[GPS] Read timeout ({errorCount}/{MaxConsecutiveTimeoutErrors}): {ex.Message}");

            if (errorCount >= MaxConsecutiveTimeoutErrors)
            {
                _lastError = $"Too many consecutive timeouts ({errorCount}). Disconnecting.";
                System.Diagnostics.Debug.WriteLine($"[GPS] {_lastError}");

                // Disconnect on too many consecutive timeout errors (on a separate thread to avoid deadlock)
                Task.Run(() =>
                {
                    try
                    {
                        Disconnect();
                    }
                    catch (Exception disconnectEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GPS] Error during timeout-triggered disconnect: {disconnectEx.Message}");
                    }
                });
            }
        }
        catch (InvalidOperationException ex)
        {
            // Port was closed or disposed
            System.Diagnostics.Debug.WriteLine($"[GPS] Port operation error (likely closed): {ex.Message}");
        }
        catch (System.IO.IOException ex)
        {
            _lastError = $"I/O error reading from GPS: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[GPS] I/O error: {ex.Message}");
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
        string content;
        lock (_lock)
        {
            content = _buffer.ToString();
            _buffer.Clear();
        }

        var lines = content.Split('\n');

        // Keep incomplete last line in buffer
        if (!content.EndsWith("\n") && lines.Length > 0)
        {
            lock (_lock)
            {
                _buffer.Append(lines[^1]);
            }
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

    /// <summary>
    /// Disposes the GPS service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose implementation.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            System.Diagnostics.Debug.WriteLine("[GPS] Disposing GpsSerialService...");
            Disconnect();
        }

        _isDisposed = true;
        System.Diagnostics.Debug.WriteLine("[GPS] GpsSerialService disposed");
    }

    /// <summary>
    /// Finalizer for safety in case Dispose is not called.
    /// </summary>
    ~GpsSerialService()
    {
        Dispose(disposing: false);
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

    /// <summary>
    /// Read timeout in milliseconds. Set to 0 or negative to use default (3000ms).
    /// </summary>
    public int ReadTimeoutMs { get; set; } = 3000;

    /// <summary>
    /// Write timeout in milliseconds. Set to 0 or negative to use default (2000ms).
    /// </summary>
    public int WriteTimeoutMs { get; set; } = 2000;
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
