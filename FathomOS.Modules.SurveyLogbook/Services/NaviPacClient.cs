// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/NaviPacClient.cs
// Purpose: Unified TCP/UDP server/listener for real-time NaviPac data acquisition
// Version: 4.0 - Added TLS encryption (VULN-005) and rate limiting (MISSING-007)
// ============================================================================
//
// CRITICAL ARCHITECTURE NOTES:
//
// NaviPac User Defined Output (UDO) configuration:
// - NaviPac SENDS data TO a specified IP:Port
// - For TCP: NaviPac CONNECTS TO our server (we must be TcpListener)
// - For UDP: NaviPac SENDS datagrams (we must listen with UdpClient)
//
// This is the OPPOSITE of typical client-server roles!
// We are the SERVER, NaviPac is the CLIENT.
//
// SECURITY FEATURES (v4.0):
// - Optional TLS encryption for TCP connections (VULN-005)
// - Token bucket rate limiting per IP address (MISSING-007)
// - Connection rate limiting and temporary IP blocking
//
// ============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Protocol type for NaviPac connection.
/// </summary>
public enum NaviPacProtocol
{
    /// <summary>TCP server mode - listens for incoming NaviPac connections.</summary>
    TCP,
    /// <summary>UDP listener - receives NaviPac datagrams.</summary>
    UDP
}

/// <summary>
/// Connection state for NaviPac.
/// </summary>
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Listening,
    Error
}

/// <summary>
/// Message type from NaviPac.
/// </summary>
public enum NaviPacMessageType
{
    Unknown,
    Position,
    Event,
    LoggingStatus,
    Sensor
}

/// <summary>
/// Unified TCP/UDP server for receiving real-time data from NaviPac.
/// 
/// IMPORTANT: This acts as a SERVER, not a client!
/// - TCP Mode: Starts a TcpListener and waits for NaviPac to connect
/// - UDP Mode: Binds to a port and receives datagrams from NaviPac
/// 
/// NaviPac User Defined Output sends data TO us, so we must LISTEN.
/// </summary>
public class NaviPacClient : IDisposable
{
    #region Constants
    
    private const int DEFAULT_BUFFER_SIZE = 8192;
    private const int DEFAULT_CONNECTION_TIMEOUT_MS = 10000;
    private const int MAX_RECONNECT_DELAY_SECONDS = 60;
    private const int DATA_TIMEOUT_SECONDS = 30;
    
    #endregion
    
    #region Fields

    private readonly ConnectionSettings _settings;
    private readonly object _syncLock = new();
    private readonly ConcurrentQueue<string> _messageQueue = new();

    // TCP Server fields
    private TcpListener? _tcpListener;
    private TcpClient? _connectedClient;
    private Stream? _clientStream; // Can be NetworkStream or SslStream

    // UDP fields
    private UdpClient? _udpClient;

    // Security: TLS support (VULN-005)
    private TlsWrapper? _tlsWrapper;

    // Security: Rate limiting (MISSING-007)
    private RateLimiter? _rateLimiter;
    
    // Common fields
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenTask;
    private Task? _receiveTask;
    private Task? _processTask;
    private readonly StringBuilder _lineBuffer = new();
    
    // SECURITY FIX: Use Interlocked for thread-safe connection state
    // Previous implementation had race condition with volatile booleans
    private volatile int _connectionState; // 0 = disconnected, 1 = listening, 2 = connected
    private volatile bool _isDisposed;
    #pragma warning disable CS0414 // Field is assigned but never used (reserved for auto-reconnect logic)
    private volatile bool _intentionalDisconnect;
    #pragma warning restore CS0414
    private DateTime _lastDataReceived;
    
    // Debug log file
    private StreamWriter? _debugLogWriter;
    private readonly string _debugLogPath;
    
    #endregion
    
    #region Events
    
    /// <summary>Raised when position data is received.</summary>
    public event EventHandler<NaviPacDataEventArgs>? DataReceived;
    
    /// <summary>Raised when an event marker is received.</summary>
    public event EventHandler<NaviPacEventArgs>? EventReceived;
    
    /// <summary>Raised when logging status changes.</summary>
    public event EventHandler<LoggingStatusEventArgs>? LoggingStatusChanged;
    
    /// <summary>Raised when connection status changes.</summary>
    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;
    
    /// <summary>Raised when an error occurs.</summary>
    public event EventHandler<NaviPacErrorEventArgs>? ErrorOccurred;
    
    /// <summary>Raised when raw data is received (for debugging).</summary>
    public event EventHandler<RawDataEventArgs>? RawDataReceived;

    /// <summary>Raised when a rate limit violation occurs.</summary>
    public event EventHandler<RateLimitViolationEventArgs>? RateLimitViolation;

    /// <summary>Raised when TLS connection is established.</summary>
    public event EventHandler<TlsConnectedEventArgs>? TlsConnectionEstablished;

    #endregion
    
    #region Constructor
    
    /// <summary>
    /// Initializes a new instance of NaviPacClient.
    /// </summary>
    public NaviPacClient(ConnectionSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _lastDataReceived = DateTime.MinValue;

        // Setup debug log path
        var logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "SurveyLogbook", "Logs");
        Directory.CreateDirectory(logFolder);
        _debugLogPath = Path.Combine(logFolder, $"NaviPac_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        // Initialize security components
        InitializeSecurity();
    }

    /// <summary>
    /// Initializes TLS and rate limiting components based on settings.
    /// </summary>
    private void InitializeSecurity()
    {
        // Initialize TLS wrapper if enabled
        if (_settings.TlsConfiguration != null && _settings.TlsConfiguration.EnableTls)
        {
            _tlsWrapper = new TlsWrapper(_settings.TlsConfiguration);
            _tlsWrapper.TlsError += (s, e) => OnError($"TLS Error: {e.Message}", e.Exception);
            _tlsWrapper.TlsConnected += (s, e) => TlsConnectionEstablished?.Invoke(this, e);

            // Load server certificate if path is specified
            if (!string.IsNullOrEmpty(_settings.TlsConfiguration.ServerCertificatePath))
            {
                try
                {
                    _tlsWrapper.LoadServerCertificate();
                    LogDebug("TLS: Server certificate loaded successfully");
                }
                catch (Exception ex)
                {
                    LogDebug($"TLS: Failed to load server certificate: {ex.Message}");
                }
            }
        }

        // Initialize rate limiter if enabled
        if (_settings.RateLimitConfiguration != null && _settings.RateLimitConfiguration.Enabled)
        {
            _rateLimiter = new RateLimiter(_settings.RateLimitConfiguration);
            _rateLimiter.ViolationOccurred += (s, e) =>
            {
                LogDebug($"Rate Limit: Violation from {e.IpAddress} - {e.Reason}");
                RateLimitViolation?.Invoke(this, e);
            };
            LogDebug("Rate Limiter: Initialized");
        }
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Gets whether the server is currently listening/connected.
    /// SECURITY FIX: Uses thread-safe Interlocked operation to prevent race condition
    /// </summary>
    public bool IsConnected => Interlocked.CompareExchange(ref _connectionState, 0, 0) != 0;

    /// <summary>
    /// Gets whether the server is in listening state (TCP server waiting, UDP bound).
    /// </summary>
    private bool IsListening => Interlocked.CompareExchange(ref _connectionState, 0, 0) == 1;

    /// <summary>
    /// Gets whether a client is actively connected and communicating.
    /// </summary>
    private bool IsActivelyConnected => Interlocked.CompareExchange(ref _connectionState, 0, 0) == 2;
    
    /// <summary>
    /// Gets whether a NaviPac client is currently connected (TCP only).
    /// </summary>
    public bool HasConnectedClient => _connectedClient?.Connected ?? false;
    
    /// <summary>
    /// Gets the time of last received data.
    /// </summary>
    public DateTime LastDataReceived => _lastDataReceived;
    
    /// <summary>
    /// Gets whether data has been received recently.
    /// </summary>
    public bool IsReceivingData =>
        _lastDataReceived > DateTime.MinValue &&
        (DateTime.Now - _lastDataReceived).TotalSeconds < DATA_TIMEOUT_SECONDS;

    /// <summary>
    /// Connection statistics.
    /// </summary>
    public NaviPacStatistics Statistics { get; } = new();

    /// <summary>
    /// Gets whether TLS is enabled for this connection.
    /// </summary>
    public bool IsTlsEnabled => _tlsWrapper?.IsEnabled ?? false;

    /// <summary>
    /// Gets whether rate limiting is enabled.
    /// </summary>
    public bool IsRateLimitingEnabled => _rateLimiter != null;

    /// <summary>
    /// Gets the rate limiter instance for external configuration.
    /// </summary>
    public RateLimiter? RateLimiter => _rateLimiter;

    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Starts the NaviPac server/listener.
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(NaviPacClient));
        
        if (IsConnected)
        {
            Debug.WriteLine("NaviPacClient: Already listening/connected");
            return true;
        }
        
        _intentionalDisconnect = false;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Start debug logging if enabled
        if (_settings.EnableDebugLogging)
        {
            StartDebugLogging();
        }
        
        try
        {
            return _settings.NaviPacProtocol switch
            {
                NaviPacProtocol.TCP => await StartTcpServerAsync(_cancellationTokenSource.Token),
                NaviPacProtocol.UDP => await StartUdpListenerAsync(_cancellationTokenSource.Token),
                _ => throw new InvalidOperationException($"Unknown protocol: {_settings.NaviPacProtocol}")
            };
        }
        catch (OperationCanceledException)
        {
            OnConnectionStatusChanged(ConnectionState.Disconnected, "Operation cancelled");
            return false;
        }
        catch (Exception ex)
        {
            OnError($"Failed to start: {ex.Message}", ex);
            OnConnectionStatusChanged(ConnectionState.Error, $"Failed: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Stops the NaviPac server/listener.
    /// </summary>
    public void Disconnect()
    {
        _intentionalDisconnect = true;
        Interlocked.Exchange(ref _connectionState, 0); // Set to disconnected
        
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException) { }
        
        // Wait briefly for tasks
        try
        {
            var waitTask = Task.WhenAny(
                _listenTask ?? Task.CompletedTask,
                _receiveTask ?? Task.CompletedTask,
                _processTask ?? Task.CompletedTask,
                Task.Delay(2000)
            );
            waitTask.Wait(3000);
        }
        catch { }
        
        CleanupResources();
        StopDebugLogging();
        
        OnConnectionStatusChanged(ConnectionState.Disconnected, "Stopped");
        LogDebug("Disconnected");
    }
    
    /// <summary>
    /// Tests if the port is available.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make truly async
        
        try
        {
            if (_settings.NaviPacProtocol == NaviPacProtocol.TCP)
            {
                // Test if we can bind to the port
                try
                {
                    var testListener = new TcpListener(IPAddress.Any, _settings.NaviPacPort);
                    testListener.Start();
                    testListener.Stop();
                    return (true, $"TCP port {_settings.NaviPacPort} is available for listening");
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    return (false, $"TCP port {_settings.NaviPacPort} is already in use");
                }
            }
            else // UDP
            {
                try
                {
                    using var testClient = new UdpClient(_settings.NaviPacPort);
                    return (true, $"UDP port {_settings.NaviPacPort} is available for listening");
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    return (false, $"UDP port {_settings.NaviPacPort} is already in use");
                }
            }
        }
        catch (Exception ex)
        {
            return (false, $"Test failed: {ex.Message}");
        }
    }
    
    #endregion
    
    #region TCP Server Methods
    
    private async Task<bool> StartTcpServerAsync(CancellationToken cancellationToken)
    {
        OnConnectionStatusChanged(ConnectionState.Connecting, 
            $"Starting TCP server on port {_settings.NaviPacPort}...");
        LogDebug($"Starting TCP server on port {_settings.NaviPacPort}");
        
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, _settings.NaviPacPort);
            _tcpListener.Start();
            Interlocked.Exchange(ref _connectionState, 1); // Set to listening
            
            OnConnectionStatusChanged(ConnectionState.Listening, 
                $"TCP server listening on port {_settings.NaviPacPort} - waiting for NaviPac to connect...");
            LogDebug($"TCP server started, waiting for connections on port {_settings.NaviPacPort}");
            
            // Start accept task
            _listenTask = AcceptTcpConnectionsAsync(cancellationToken);
            _processTask = ProcessMessageQueueAsync(cancellationToken);
            
            return true;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            var msg = $"TCP port {_settings.NaviPacPort} is already in use";
            OnError(msg, ex);
            throw new InvalidOperationException(msg, ex);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied)
        {
            var msg = $"Access denied to TCP port {_settings.NaviPacPort}. May need elevated permissions.";
            OnError(msg, ex);
            throw new InvalidOperationException(msg, ex);
        }
    }
    
    private async Task AcceptTcpConnectionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _tcpListener != null)
            {
                LogDebug("Waiting for TCP client connection...");

                TcpClient client;
                try
                {
                    client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                // Client connected!
                var remoteEp = client.Client.RemoteEndPoint as IPEndPoint;
                LogDebug($"TCP client connected from {remoteEp?.Address}:{remoteEp?.Port}");

                // SECURITY: Rate limiting check (MISSING-007)
                if (_rateLimiter != null && remoteEp != null)
                {
                    var rateLimitResult = _rateLimiter.CheckConnection(remoteEp);
                    if (!rateLimitResult.IsAllowed)
                    {
                        LogDebug($"Rate limit: Connection from {remoteEp.Address} rejected - {rateLimitResult.DenialReason}");
                        Statistics.RecordRejectedConnection();
                        try
                        {
                            client.Close();
                        }
                        catch { }
                        continue; // Wait for next connection
                    }
                }

                // Close previous client if any
                if (_connectedClient != null)
                {
                    LogDebug("Closing previous TCP client connection");
                    try
                    {
                        _clientStream?.Close();
                        _connectedClient.Close();
                    }
                    catch { }
                }

                _connectedClient = client;
                _connectedClient.NoDelay = true;
                _connectedClient.ReceiveBufferSize = DEFAULT_BUFFER_SIZE;

                // SECURITY: TLS handshake if enabled (VULN-005)
                try
                {
                    if (_tlsWrapper != null && _tlsWrapper.IsEnabled)
                    {
                        LogDebug($"TLS: Starting handshake with {remoteEp?.Address}");
                        _clientStream = await _tlsWrapper.WrapServerStreamAsync(_connectedClient, cancellationToken);
                        LogDebug($"TLS: Handshake completed with {remoteEp?.Address}");
                    }
                    else
                    {
                        _clientStream = _connectedClient.GetStream();
                    }
                }
                catch (TlsHandshakeException ex)
                {
                    LogDebug($"TLS: Handshake failed with {remoteEp?.Address}: {ex.Message}");
                    OnError($"TLS handshake failed: {ex.Message}", ex);
                    try
                    {
                        client.Close();
                    }
                    catch { }
                    continue; // Wait for next connection
                }

                Interlocked.Exchange(ref _connectionState, 2); // Set to connected
                _lastDataReceived = DateTime.Now;

                Statistics.RecordConnection();

                var statusMessage = $"NaviPac connected from {remoteEp?.Address}:{remoteEp?.Port}";
                if (_tlsWrapper?.IsEnabled == true)
                {
                    statusMessage += " (TLS secured)";
                }
                OnConnectionStatusChanged(ConnectionState.Connected, statusMessage);

                // Start receiving data from this client
                _receiveTask = ReceiveTcpDataAsync(cancellationToken);

                // Wait for this client to disconnect before accepting another
                try
                {
                    await _receiveTask;
                }
                catch { }

                Interlocked.Exchange(ref _connectionState, 1); // Set back to listening
                OnConnectionStatusChanged(ConnectionState.Listening,
                    "NaviPac disconnected - waiting for reconnection...");
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            OnError($"TCP accept error: {ex.Message}", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _connectionState, 0); // Set to disconnected
        }
    }

    private async Task ReceiveTcpDataAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[DEFAULT_BUFFER_SIZE];

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _connectedClient?.Connected == true &&
                   _clientStream != null)
            {
                int bytesRead;
                try
                {
                    bytesRead = await _clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                }
                catch (IOException ex) when (ex.InnerException is SocketException)
                {
                    LogDebug($"TCP connection closed: {ex.Message}");
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    LogDebug("TCP connection closed by remote");
                    break;
                }

                _lastDataReceived = DateTime.Now;
                Statistics.RecordBytesReceived(bytesRead);

                var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                LogDebug($"TCP received {bytesRead} bytes: {data.Replace("\r", "\\r").Replace("\n", "\\n")}");

                var protocol = _tlsWrapper?.IsEnabled == true ? "TCP/TLS" : "TCP";
                RawDataReceived?.Invoke(this, new RawDataEventArgs
                {
                    Data = data,
                    Timestamp = DateTime.Now,
                    Protocol = protocol
                });

                ProcessRawData(data);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            OnError($"TCP receive error: {ex.Message}", ex);
        }
    }
    
    #endregion
    
    #region UDP Methods
    
    private async Task<bool> StartUdpListenerAsync(CancellationToken cancellationToken)
    {
        OnConnectionStatusChanged(ConnectionState.Connecting, 
            $"Starting UDP listener on port {_settings.NaviPacPort}...");
        LogDebug($"Starting UDP listener on port {_settings.NaviPacPort}");
        
        try
        {
            // Determine bind address
            IPAddress bindAddress = IPAddress.Any;
            if (!string.IsNullOrWhiteSpace(_settings.UdpBindInterface))
            {
                if (IPAddress.TryParse(_settings.UdpBindInterface, out var specifiedIp))
                {
                    bindAddress = specifiedIp;
                    LogDebug($"Binding to specific interface: {bindAddress}");
                }
                else
                {
                    LogDebug($"Invalid bind interface '{_settings.UdpBindInterface}', using all interfaces");
                }
            }
            
            // Create UDP client with specific endpoint
            var localEndPoint = new IPEndPoint(bindAddress, _settings.NaviPacPort);
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(localEndPoint);
            _udpClient.Client.ReceiveBufferSize = DEFAULT_BUFFER_SIZE;
            
            // Enable broadcast if configured
            _udpClient.EnableBroadcast = _settings.EnableUdpBroadcast;
            LogDebug($"UDP broadcast enabled: {_settings.EnableUdpBroadcast}");
            
            // Join multicast group if configured
            if (_settings.EnableMulticast && !string.IsNullOrWhiteSpace(_settings.MulticastGroup))
            {
                if (IPAddress.TryParse(_settings.MulticastGroup, out var multicastAddress))
                {
                    try
                    {
                        _udpClient.JoinMulticastGroup(multicastAddress);
                        _udpClient.Client.SetSocketOption(
                            SocketOptionLevel.IP, 
                            SocketOptionName.MulticastTimeToLive, 
                            _settings.MulticastTtl);
                        LogDebug($"Joined multicast group: {multicastAddress}, TTL: {_settings.MulticastTtl}");
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Failed to join multicast group: {ex.Message}");
                        OnError($"Failed to join multicast group {_settings.MulticastGroup}: {ex.Message}", ex);
                    }
                }
                else
                {
                    LogDebug($"Invalid multicast group address: {_settings.MulticastGroup}");
                }
            }
            
            Interlocked.Exchange(ref _connectionState, 2); // Set to connected (UDP is listening + connected)
            _lastDataReceived = DateTime.Now;

            Statistics.RecordConnection();

            var statusMsg = $"UDP listener started on {bindAddress}:{_settings.NaviPacPort}";
            if (_settings.EnableMulticast && !string.IsNullOrWhiteSpace(_settings.MulticastGroup))
                statusMsg += $" (multicast: {_settings.MulticastGroup})";
            if (_settings.EnableSourceIpFilter)
                statusMsg += $" (filtered: {_settings.AllowedSourceIps})";
            
            OnConnectionStatusChanged(ConnectionState.Connected, statusMsg);
            LogDebug(statusMsg);
            
            _receiveTask = ReceiveUdpDataAsync(cancellationToken);
            _processTask = ProcessMessageQueueAsync(cancellationToken);
            
            return true;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            var msg = $"UDP port {_settings.NaviPacPort} is already in use";
            OnError(msg, ex);
            throw new InvalidOperationException(msg, ex);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AccessDenied)
        {
            var msg = $"Access denied to UDP port {_settings.NaviPacPort}. Check firewall settings.";
            OnError(msg, ex);
            throw new InvalidOperationException(msg, ex);
        }
    }
    
    private async Task ReceiveUdpDataAsync(CancellationToken cancellationToken)
    {
        LogDebug("UDP receive loop started");
        
        // Pre-parse allowed IPs for filtering
        HashSet<string>? allowedIps = null;
        if (_settings.EnableSourceIpFilter && _settings.AllowedSourceIpList.Length > 0)
        {
            allowedIps = new HashSet<string>(_settings.AllowedSourceIpList, StringComparer.OrdinalIgnoreCase);
            LogDebug($"Source IP filtering enabled, allowed: {string.Join(", ", allowedIps)}");
        }
        
        try
        {
            while (!cancellationToken.IsCancellationRequested && _udpClient != null)
            {
                UdpReceiveResult result;
                try
                {
                    result = await _udpClient.ReceiveAsync(cancellationToken);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    LogDebug("UDP socket interrupted");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    LogDebug("UDP client disposed");
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                
                // SECURITY: Rate limiting check (MISSING-007)
                if (_rateLimiter != null)
                {
                    var rateLimitResult = _rateLimiter.CheckConnection(result.RemoteEndPoint);
                    if (!rateLimitResult.IsAllowed)
                    {
                        LogDebug($"Rate limit: UDP packet from {result.RemoteEndPoint.Address} rejected - {rateLimitResult.DenialReason}");
                        Statistics.RecordRejectedConnection();
                        continue;
                    }
                }

                // Source IP filtering
                if (allowedIps != null)
                {
                    var sourceIp = result.RemoteEndPoint.Address.ToString();
                    if (!allowedIps.Contains(sourceIp))
                    {
                        LogDebug($"UDP packet from {sourceIp} rejected (not in allowed list)");
                        Statistics.RecordPacket(null); // Record as filtered
                        continue;
                    }
                }
                
                _lastDataReceived = DateTime.Now;
                Statistics.RecordBytesReceived(result.Buffer.Length);
                Statistics.RecordPacket(result.RemoteEndPoint);
                
                var data = Encoding.ASCII.GetString(result.Buffer);
                LogDebug($"UDP received {result.Buffer.Length} bytes from {result.RemoteEndPoint}: {data.Replace("\r", "\\r").Replace("\n", "\\n")}");
                
                RawDataReceived?.Invoke(this, new RawDataEventArgs 
                { 
                    Data = data, 
                    Timestamp = DateTime.Now,
                    Protocol = "UDP",
                    RemoteEndPoint = result.RemoteEndPoint
                });
                
                ProcessRawData(data);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            OnError($"UDP receive error: {ex.Message}", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _connectionState, 0); // Set to disconnected
            LogDebug("UDP receive loop ended");
        }
    }

    #endregion

    #region Data Processing
    
    private void ProcessRawData(string data)
    {
        // Add to line buffer and extract complete lines
        _lineBuffer.Append(data);
        var bufferContent = _lineBuffer.ToString();
        
        // Split by line endings
        var lines = bufferContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        // Process all complete lines (all except the last one which may be incomplete)
        for (int i = 0; i < lines.Length - 1; i++)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrEmpty(line))
            {
                _messageQueue.Enqueue(line);
                Statistics.RecordMessage();
                LogDebug($"Queued message: {line}");
            }
        }
        
        // Keep incomplete line in buffer
        _lineBuffer.Clear();
        _lineBuffer.Append(lines[^1]);
    }
    
    private async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
    {
        LogDebug("Message processing loop started");
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out var line))
                {
                    try
                    {
                        ParseLine(line);
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Parse error for line '{line}': {ex.Message}");
                        OnError($"Parse error: {ex.Message}", ex);
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        
        LogDebug("Message processing loop ended");
    }
    
    private void ParseLine(string line)
    {
        LogDebug($"Parsing line: {line}");
        
        // Determine separator
        char separator = _settings.NaviPacSeparator;
        
        // Split by the configured separator
        var fields = line.Split(separator)
                         .Select(f => f.Trim())
                         .ToArray();
        
        if (fields.Length == 0)
        {
            LogDebug("Empty line after split");
            return;
        }
        
        LogDebug($"Parsed {fields.Length} fields with separator '{separator}'");
        
        // Create NaviPac data object
        var navData = new NaviPacData
        {
            RawLine = line,
            Timestamp = DateTime.Now,
            FieldCount = fields.Length
        };
        
        // Parse based on field mapping or auto-detect
        if (_settings.FieldMapping.AutoDetect)
        {
            ParseAutoDetect(fields, navData);
        }
        else
        {
            ParseWithMapping(fields, navData);
        }
        
        // Determine message type
        var messageType = DetermineMessageType(navData, line);
        
        // Raise appropriate events
        switch (messageType)
        {
            case NaviPacMessageType.Event:
                LogDebug($"Event detected: {navData.EventNumber}");
                EventReceived?.Invoke(this, new NaviPacEventArgs
                {
                    EventNumber = navData.EventNumber ?? 0,
                    EventText = navData.EventText,
                    Timestamp = navData.Time ?? DateTime.Now,
                    RawData = line,
                    Data = navData
                });
                break;
                
            case NaviPacMessageType.LoggingStatus:
                LogDebug($"Logging status: {navData.LoggingActive}");
                LoggingStatusChanged?.Invoke(this, new LoggingStatusEventArgs
                {
                    IsLogging = navData.LoggingActive,
                    Timestamp = DateTime.Now
                });
                break;
                
            case NaviPacMessageType.Position:
            default:
                LogDebug($"Position data: E={navData.Easting}, N={navData.Northing}");
                break;
        }
        
        // Always raise DataReceived for any message
        DataReceived?.Invoke(this, new NaviPacDataEventArgs
        {
            Data = navData,
            RawData = line,
            MessageType = messageType,
            Timestamp = DateTime.Now
        });
    }
    
    private void ParseAutoDetect(string[] fields, NaviPacData navData)
    {
        LogDebug("Using auto-detect parsing");
        
        // Try to parse fields intelligently
        foreach (var field in fields)
        {
            // Try as event number (integer)
            if (int.TryParse(field, out var eventNum) && eventNum > 0 && eventNum < 100000)
            {
                if (!navData.EventNumber.HasValue)
                {
                    navData.EventNumber = eventNum;
                    continue;
                }
            }
            
            // Try as coordinate (large number with decimals)
            if (double.TryParse(field, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var dblValue))
            {
                // Easting/Northing typically > 100000
                if (Math.Abs(dblValue) > 100000)
                {
                    if (!navData.Easting.HasValue)
                    {
                        navData.Easting = dblValue;
                        continue;
                    }
                    else if (!navData.Northing.HasValue)
                    {
                        navData.Northing = dblValue;
                        continue;
                    }
                }
                // Lat/Long typically -180 to 180
                else if (Math.Abs(dblValue) <= 180)
                {
                    if (!navData.Latitude.HasValue)
                    {
                        navData.Latitude = dblValue;
                        continue;
                    }
                    else if (!navData.Longitude.HasValue)
                    {
                        navData.Longitude = dblValue;
                        continue;
                    }
                }
                // KP typically 0-1000
                else if (dblValue >= -1000 && dblValue <= 1000)
                {
                    if (!navData.KP.HasValue)
                    {
                        navData.KP = dblValue;
                        continue;
                    }
                    else if (!navData.DCC.HasValue)
                    {
                        navData.DCC = dblValue;
                        continue;
                    }
                }
                // Heading/Gyro 0-360
                else if (dblValue >= 0 && dblValue <= 360)
                {
                    if (!navData.Heading.HasValue)
                    {
                        navData.Heading = dblValue;
                        continue;
                    }
                }
                // Small values could be Roll/Pitch/Heave
                else if (Math.Abs(dblValue) <= 90)
                {
                    if (!navData.Roll.HasValue)
                    {
                        navData.Roll = dblValue;
                        continue;
                    }
                    else if (!navData.Pitch.HasValue)
                    {
                        navData.Pitch = dblValue;
                        continue;
                    }
                    else if (!navData.Heave.HasValue)
                    {
                        navData.Heave = dblValue;
                        continue;
                    }
                }
            }
            
            // Try as time
            if (TryParseTime(field, out var time))
            {
                navData.Time = time;
                continue;
            }
            
            // Try as date
            if (TryParseDate(field, out var date))
            {
                navData.Date = date;
                continue;
            }
        }
    }
    
    private void ParseWithMapping(string[] fields, NaviPacData navData)
    {
        LogDebug($"Using field mapping with {_settings.FieldMapping.Fields.Count} configured fields");
        
        var mapping = _settings.FieldMapping;
        
        for (int i = 0; i < Math.Min(fields.Length, mapping.Fields.Count); i++)
        {
            var fieldType = mapping.Fields[i];
            var value = fields[i];
            
            if (string.IsNullOrWhiteSpace(value))
                continue;
            
            switch (fieldType)
            {
                case NaviPacFieldType.Event:
                    if (int.TryParse(value, out var eventNum))
                        navData.EventNumber = eventNum;
                    else
                        navData.EventText = value;
                    break;
                    
                case NaviPacFieldType.Gyro:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var gyro))
                        navData.Heading = gyro;
                    break;
                    
                case NaviPacFieldType.Roll:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var roll))
                        navData.Roll = roll;
                    break;
                    
                case NaviPacFieldType.Pitch:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var pitch))
                        navData.Pitch = pitch;
                    break;
                    
                case NaviPacFieldType.Heave:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var heave))
                        navData.Heave = heave;
                    break;
                    
                case NaviPacFieldType.Easting:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var easting))
                        navData.Easting = easting;
                    break;
                    
                case NaviPacFieldType.Northing:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var northing))
                        navData.Northing = northing;
                    break;
                    
                case NaviPacFieldType.Latitude:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var lat))
                        navData.Latitude = lat;
                    break;
                    
                case NaviPacFieldType.Longitude:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var lon))
                        navData.Longitude = lon;
                    break;
                    
                case NaviPacFieldType.Height:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var height))
                        navData.Height = height;
                    break;
                    
                case NaviPacFieldType.KP:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var kp))
                        navData.KP = kp;
                    break;
                    
                case NaviPacFieldType.DAL:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var dal))
                        navData.DAL = dal;
                    break;
                    
                case NaviPacFieldType.DOL:
                case NaviPacFieldType.DCC:
                    if (double.TryParse(value, System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var dcc))
                        navData.DCC = dcc;
                    break;
                    
                case NaviPacFieldType.DateTime:
                    if (DateTime.TryParse(value, out var dateTime))
                        navData.Time = dateTime;
                    break;
                    
                case NaviPacFieldType.Time:
                    if (TryParseTime(value, out var time))
                        navData.Time = time;
                    break;
                    
                case NaviPacFieldType.Date:
                    if (TryParseDate(value, out var date))
                        navData.Date = date;
                    break;
            }
        }
    }
    
    private NaviPacMessageType DetermineMessageType(NaviPacData data, string line)
    {
        var upperLine = line.ToUpperInvariant();
        
        // Check for event indicators
        if (data.EventNumber.HasValue && data.EventNumber > 0)
            return NaviPacMessageType.Event;
            
        if (upperLine.Contains("EVENT") || upperLine.Contains("EVT") || upperLine.Contains("MARK"))
            return NaviPacMessageType.Event;
        
        // Check for logging status
        if (upperLine.Contains("LOG") && 
            (upperLine.Contains("START") || upperLine.Contains("STOP") || 
             upperLine.Contains("ON") || upperLine.Contains("OFF")))
            return NaviPacMessageType.LoggingStatus;
        
        // Default to position
        return NaviPacMessageType.Position;
    }
    
    private bool TryParseTime(string value, out DateTime time)
    {
        time = DateTime.MinValue;
        
        // Try HH:MM:SS.sss format
        if (TimeSpan.TryParse(value, out var ts))
        {
            time = DateTime.Today.Add(ts);
            return true;
        }
        
        // Try various time formats
        string[] formats = { "HH:mm:ss.fff", "HH:mm:ss", "H:mm:ss", "HH:mm" };
        if (DateTime.TryParseExact(value, formats, 
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out time))
        {
            return true;
        }
        
        return false;
    }
    
    private bool TryParseDate(string value, out DateTime date)
    {
        date = DateTime.MinValue;
        
        string[] formats = { 
            "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", 
            "dd.MM.yyyy", "d/M/yyyy", "yyyy/MM/dd"
        };
        
        return DateTime.TryParseExact(value, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out date);
    }
    
    #endregion
    
    #region Debug Logging
    
    private void StartDebugLogging()
    {
        try
        {
            _debugLogWriter = new StreamWriter(_debugLogPath, true, Encoding.UTF8)
            {
                AutoFlush = true
            };
            LogDebug($"=== Debug logging started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            LogDebug($"Protocol: {_settings.NaviPacProtocol}");
            LogDebug($"Port: {_settings.NaviPacPort}");
            LogDebug($"Separator: '{_settings.NaviPacSeparator}' (ASCII {(int)_settings.NaviPacSeparator})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start debug logging: {ex.Message}");
        }
    }
    
    private void StopDebugLogging()
    {
        try
        {
            LogDebug($"=== Debug logging ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            _debugLogWriter?.Close();
            _debugLogWriter?.Dispose();
            _debugLogWriter = null;
        }
        catch { }
    }
    
    private void LogDebug(string message)
    {
        var logLine = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine($"NaviPac: {logLine}");
        
        try
        {
            _debugLogWriter?.WriteLine(logLine);
        }
        catch { }
    }
    
    #endregion
    
    #region Event Helpers
    
    private void OnConnectionStatusChanged(ConnectionState state, string message)
    {
        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusEventArgs
        {
            State = state,
            Message = message,
            Timestamp = DateTime.Now
        });
    }
    
    private void OnError(string message, Exception? exception = null)
    {
        LogDebug($"ERROR: {message}");
        ErrorOccurred?.Invoke(this, new NaviPacErrorEventArgs
        {
            Message = message,
            Exception = exception,
            Timestamp = DateTime.Now
        });
    }
    
    #endregion
    
    #region Resource Cleanup
    
    private void CleanupResources()
    {
        try
        {
            _clientStream?.Close();
            _clientStream?.Dispose();
            _clientStream = null;
        }
        catch { }
        
        try
        {
            _connectedClient?.Close();
            _connectedClient?.Dispose();
            _connectedClient = null;
        }
        catch { }
        
        try
        {
            _tcpListener?.Stop();
            _tcpListener = null;
        }
        catch { }
        
        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }
        catch { }
        
        try
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        catch { }
    }
    
    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        Disconnect();
        GC.SuppressFinalize(this);
    }
    
    #endregion
}

#region Event Args Classes

/// <summary>
/// Event args for NaviPac data received.
/// </summary>
public class NaviPacDataEventArgs : EventArgs
{
    public NaviPacData? Data { get; set; }
    public string RawData { get; set; } = string.Empty;
    public NaviPacMessageType MessageType { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event args for NaviPac event marker.
/// </summary>
public class NaviPacEventArgs : EventArgs
{
    public int EventNumber { get; set; }
    public string? EventText { get; set; }
    public DateTime Timestamp { get; set; }
    public string RawData { get; set; } = string.Empty;
    public NaviPacData? Data { get; set; }
}

/// <summary>
/// Event args for logging status change.
/// </summary>
public class LoggingStatusEventArgs : EventArgs
{
    public bool IsLogging { get; set; }
    public string System { get; set; } = "NaviPac";
    public string? RunlineName { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event args for connection status change.
/// </summary>
public class ConnectionStatusEventArgs : EventArgs
{
    public ConnectionState State { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event args for errors.
/// </summary>
public class NaviPacErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event args for raw data (debugging).
/// </summary>
public class RawDataEventArgs : EventArgs
{
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public IPEndPoint? RemoteEndPoint { get; set; }
}

#endregion

#region NaviPac Data Model

/// <summary>
/// Parsed NaviPac User Defined Output data.
/// </summary>
public class NaviPacData
{
    // Raw data
    public string RawLine { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int FieldCount { get; set; }
    
    // Time/Date
    public DateTime? Time { get; set; }
    public DateTime? Date { get; set; }
    
    // Event
    public int? EventNumber { get; set; }
    public string? EventText { get; set; }
    public bool LoggingActive { get; set; }
    
    // Position
    public double? Easting { get; set; }
    public double? Northing { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? Height { get; set; }
    
    // Route
    public double? KP { get; set; }
    public double? DAL { get; set; }
    public double? DCC { get; set; }
    
    // Sensors
    public double? Heading { get; set; }
    public double? Roll { get; set; }
    public double? Pitch { get; set; }
    public double? Heave { get; set; }
    
    // DAQ
    public double? DAQ1 { get; set; }
    public double? DAQ2 { get; set; }
    
    // Speed
    public double? SMG { get; set; }
    public double? CMG { get; set; }
    
    // Quality
    public double? Age { get; set; }
    
    /// <summary>
    /// Gets the best available timestamp.
    /// </summary>
    public DateTime GetTimestamp()
    {
        if (Date.HasValue && Time.HasValue)
            return Date.Value.Date.Add(Time.Value.TimeOfDay);
        if (Time.HasValue)
            return DateTime.Today.Add(Time.Value.TimeOfDay);
        if (Date.HasValue)
            return Date.Value;
        return Timestamp;
    }
}

#endregion

#region Statistics

/// <summary>
/// Connection and data statistics.
/// </summary>
public class NaviPacStatistics
{
    private long _bytesReceived;
    private long _messagesReceived;
    private long _packetsReceived;
    private int _connectionCount;
    private int _rejectedConnections;
    private readonly ConcurrentDictionary<string, int> _sourceAddresses = new();

    public long BytesReceived => _bytesReceived;
    public long MessagesReceived => _messagesReceived;
    public long PacketsReceived => _packetsReceived;
    public int ConnectionCount => _connectionCount;
    public int RejectedConnections => _rejectedConnections;
    public IReadOnlyDictionary<string, int> SourceAddresses => _sourceAddresses;

    public void RecordBytesReceived(int bytes) => Interlocked.Add(ref _bytesReceived, bytes);
    public void RecordMessage() => Interlocked.Increment(ref _messagesReceived);
    public void RecordConnection() => Interlocked.Increment(ref _connectionCount);
    public void RecordRejectedConnection() => Interlocked.Increment(ref _rejectedConnections);
    
    public void RecordPacket(IPEndPoint? endpoint)
    {
        Interlocked.Increment(ref _packetsReceived);
        if (endpoint != null)
        {
            var key = endpoint.Address.ToString();
            _sourceAddresses.AddOrUpdate(key, 1, (_, count) => count + 1);
        }
    }
    
    public void Reset()
    {
        _bytesReceived = 0;
        _messagesReceived = 0;
        _packetsReceived = 0;
        _connectionCount = 0;
        _rejectedConnections = 0;
        _sourceAddresses.Clear();
    }
}

#endregion
