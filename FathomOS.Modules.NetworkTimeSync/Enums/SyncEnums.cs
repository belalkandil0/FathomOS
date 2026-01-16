namespace FathomOS.Modules.NetworkTimeSync.Enums;

/// <summary>
/// Synchronization status of a network computer.
/// </summary>
public enum SyncStatus
{
    /// <summary>Computer status is unknown (not yet checked).</summary>
    Unknown,
    
    /// <summary>Computer is synchronized within acceptable tolerance.</summary>
    Synced,
    
    /// <summary>Computer time is outside acceptable tolerance.</summary>
    OutOfSync,
    
    /// <summary>Computer or agent is not reachable.</summary>
    Unreachable,
    
    /// <summary>Currently checking status.</summary>
    Checking,
    
    /// <summary>Currently syncing time.</summary>
    Syncing,
    
    /// <summary>Sync failed due to an error.</summary>
    Error,
    
    /// <summary>Agent is not installed on this computer.</summary>
    AgentNotInstalled
}

/// <summary>
/// Method used to discover computers on the network.
/// </summary>
public enum DiscoveryMethod
{
    /// <summary>Scan an IP address range.</summary>
    IpRangeScan,
    
    /// <summary>Manually entered computer.</summary>
    Manual
}

/// <summary>
/// Source of reference time for synchronization.
/// </summary>
public enum TimeSourceType
{
    /// <summary>Use Internet NTP servers (time.windows.com, pool.ntp.org).</summary>
    InternetNtp,
    
    /// <summary>Use a local NTP server on the network.</summary>
    LocalNtpServer,
    
    /// <summary>Use the Fathom OS host computer as time reference.</summary>
    HostComputer,
    
    /// <summary>Use GPS time from serial port (NMEA GGA/RMC sentences).</summary>
    GpsSerial
}

/// <summary>
/// Synchronization mode for the module.
/// </summary>
public enum SyncMode
{
    /// <summary>One-time force sync when triggered.</summary>
    OneTime,
    
    /// <summary>Continuous monitoring with auto-correction.</summary>
    Continuous
}

/// <summary>
/// Agent command types for communication protocol.
/// </summary>
public enum AgentCommand
{
    /// <summary>Check if agent is alive.</summary>
    Ping,
    
    /// <summary>Get current system time.</summary>
    GetTime,
    
    /// <summary>Set system time.</summary>
    SetTime,
    
    /// <summary>Get computer information (hostname, OS, etc.).</summary>
    GetInfo,
    
    /// <summary>Sync to NTP server.</summary>
    SyncNtp
}

/// <summary>
/// Result status of agent operations.
/// </summary>
public enum AgentResponseStatus
{
    /// <summary>Operation completed successfully.</summary>
    Success,
    
    /// <summary>Operation failed.</summary>
    Failed,
    
    /// <summary>Authentication failed.</summary>
    AuthFailed,
    
    /// <summary>Invalid command.</summary>
    InvalidCommand,
    
    /// <summary>Timeout waiting for response.</summary>
    Timeout
}
