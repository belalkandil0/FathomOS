namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FathomOS.Modules.NetworkTimeSync.Enums;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Service for discovering computers on the network.
/// First pings to find reachable computers, then checks if TimeSyncAgent is installed.
/// </summary>
public class NetworkDiscoveryService
{
    private readonly int _port;
    private readonly int _timeoutMs;
    private readonly int _concurrentScans;
    private readonly string _agentSecret;

    public NetworkDiscoveryService(int port = 7700, int timeoutMs = 1000, int concurrentScans = 20, string agentSecret = "")
    {
        _port = port;
        _timeoutMs = timeoutMs;
        _concurrentScans = concurrentScans;
        _agentSecret = agentSecret;
    }

    /// <summary>
    /// Event raised when a computer is discovered.
    /// </summary>
    public event EventHandler<NetworkComputer>? ComputerDiscovered;

    /// <summary>
    /// Event raised to report scan progress.
    /// </summary>
    public event EventHandler<(int Current, int Total, string Status)>? ProgressChanged;

    /// <summary>
    /// Scan an IP range for computers - finds all reachable hosts via ping,
    /// then checks which have the agent installed.
    /// </summary>
    public async Task<List<NetworkComputer>> ScanRangeAsync(
        string startIp, 
        string endIp, 
        CancellationToken cancellationToken = default)
    {
        var discovered = new List<NetworkComputer>();
        var ipAddresses = GenerateIpRange(startIp, endIp);
        var total = ipAddresses.Count;
        var current = 0;

        // Create semaphore to limit concurrent scans
        using var semaphore = new SemaphoreSlim(_concurrentScans);
        var tasks = new List<Task>();

        foreach (var ip in ipAddresses)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await semaphore.WaitAsync(cancellationToken);

            var task = Task.Run(async () =>
            {
                try
                {
                    var computer = await ScanSingleAsync(ip, cancellationToken);
                    if (computer != null)
                    {
                        lock (discovered)
                        {
                            discovered.Add(computer);
                        }
                        ComputerDiscovered?.Invoke(this, computer);
                    }
                }
                finally
                {
                    var count = Interlocked.Increment(ref current);
                    ProgressChanged?.Invoke(this, (count, total, $"Scanning {ip}..."));
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Scan was cancelled
        }

        ProgressChanged?.Invoke(this, (total, total, $"Scan complete. Found {discovered.Count} computer(s)."));
        return discovered;
    }

    /// <summary>
    /// Scan a single IP address - check for agent, then try ping for hostname.
    /// Returns computer info if agent is responding OR if host is pingable.
    /// </summary>
    public async Task<NetworkComputer?> ScanSingleAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Check if agent is installed (port is open) - this is the primary check
            // We do this FIRST because Windows Firewall might block ICMP ping but allow TCP port 7700
            bool agentResponding = await CheckAgentPortAsync(ipAddress, cancellationToken);
            
            // Step 2: If agent not responding, try ping to see if host exists at all
            bool isReachable = agentResponding;
            if (!agentResponding)
            {
                isReachable = await PingAsync(ipAddress, _timeoutMs);
                if (!isReachable)
                    return null; // Host not reachable at all, skip
            }
            
            // Step 3: Get hostname via DNS
            string hostname = ipAddress;
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                if (!string.IsNullOrEmpty(hostEntry.HostName))
                {
                    hostname = hostEntry.HostName;
                }
            }
            catch
            {
                // DNS lookup failed, use IP as hostname
            }

            // Step 4: Create computer object
            var computer = new NetworkComputer
            {
                IpAddress = ipAddress,
                Port = _port,
                Hostname = hostname,
                DiscoveryMethod = DiscoveryMethod.IpRangeScan,
                AgentInstalled = agentResponding,
                Status = agentResponding ? SyncStatus.Unknown : SyncStatus.AgentNotInstalled
            };

            // Step 5: If agent is there and we have secret, get more info
            if (agentResponding && !string.IsNullOrEmpty(_agentSecret))
            {
                try
                {
                    var syncService = new TimeSyncService(_agentSecret, _timeoutMs);
                    var (info, _) = await syncService.GetInfoAsync(ipAddress, _port, cancellationToken);
                    
                    if (info != null)
                    {
                        computer.Hostname = info.Hostname;
                        computer.OsVersion = info.OsVersion;
                        computer.AgentVersion = info.AgentVersion;
                        computer.Status = SyncStatus.Unknown; // Will be updated by status monitor
                    }
                }
                catch
                {
                    // Agent responded to port check but info request failed
                    // Still mark as agent installed
                }
            }

            return computer;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if the agent port is open/responding.
    /// </summary>
    private async Task<bool> CheckAgentPortAsync(string ipAddress, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(_timeoutMs);

            try
            {
                await client.ConnectAsync(ipAddress, _port, connectCts.Token);
                client.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Quick ping check if an IP is reachable.
    /// </summary>
    public async Task<bool> PingAsync(string ipAddress, int timeoutMs = 1000)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, timeoutMs);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generate a list of IP addresses from a range.
    /// </summary>
    private List<string> GenerateIpRange(string startIp, string endIp)
    {
        var result = new List<string>();
        
        if (!IPAddress.TryParse(startIp, out var start) || !IPAddress.TryParse(endIp, out var end))
            return result;

        var startBytes = start.GetAddressBytes();
        var endBytes = end.GetAddressBytes();

        // Convert to uint for easier comparison
        uint startNum = (uint)(startBytes[0] << 24 | startBytes[1] << 16 | startBytes[2] << 8 | startBytes[3]);
        uint endNum = (uint)(endBytes[0] << 24 | endBytes[1] << 16 | endBytes[2] << 8 | endBytes[3]);

        // Limit range to prevent excessive scanning
        if (endNum - startNum > 1024)
        {
            endNum = startNum + 1024;
        }

        for (uint i = startNum; i <= endNum; i++)
        {
            var bytes = new byte[]
            {
                (byte)(i >> 24),
                (byte)(i >> 16),
                (byte)(i >> 8),
                (byte)i
            };
            result.Add(new IPAddress(bytes).ToString());
        }

        return result;
    }

    /// <summary>
    /// Get suggested IP range based on local network adapters.
    /// Returns (startIp, endIp) tuple.
    /// </summary>
    public (string StartIp, string EndIp) SuggestIpRange()
    {
        try
        {
            var networkInfo = GetLocalNetworkInfo();
            if (networkInfo.HasValue)
            {
                return CalculateNetworkRange(networkInfo.Value.IpAddress, networkInfo.Value.SubnetMask);
            }
        }
        catch
        {
            // Fall through to default
        }

        // Default fallback
        return ("192.168.1.1", "192.168.1.254");
    }

    /// <summary>
    /// Get local network interface information.
    /// Returns the most likely LAN adapter's IP and subnet mask.
    /// </summary>
    public (string IpAddress, string SubnetMask)? GetLocalNetworkInfo()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            !ni.Description.ToLower().Contains("virtual") &&
                            !ni.Description.ToLower().Contains("vmware") &&
                            !ni.Description.ToLower().Contains("hyper-v") &&
                            !ni.Description.ToLower().Contains("vethernet") &&
                            !ni.Name.ToLower().Contains("docker") &&
                            !ni.Name.ToLower().Contains("vethernet"))
                .ToList();

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var unicast = ipProps.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                                        !IPAddress.IsLoopback(a.Address));

                if (unicast != null)
                {
                    var ip = unicast.Address.ToString();
                    var mask = unicast.IPv4Mask?.ToString() ?? "255.255.255.0";
                    
                    // Prefer 192.168.x.x, 10.x.x.x, or 172.16-31.x.x ranges
                    if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || 
                        (ip.StartsWith("172.") && IsPrivate172Range(ip)))
                    {
                        return (ip, mask);
                    }
                }
            }

            // If no preferred range found, return first available
            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var unicast = ipProps.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                                        !IPAddress.IsLoopback(a.Address));

                if (unicast != null)
                {
                    return (unicast.Address.ToString(), unicast.IPv4Mask?.ToString() ?? "255.255.255.0");
                }
            }
        }
        catch
        {
            // Ignore network enumeration errors
        }

        return null;
    }

    /// <summary>
    /// Check if a 172.x.x.x address is in the private range (172.16-31.x.x).
    /// </summary>
    private bool IsPrivate172Range(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length >= 2 && int.TryParse(parts[1], out int second))
        {
            return second >= 16 && second <= 31;
        }
        return false;
    }

    /// <summary>
    /// Calculate the network range from IP and subnet mask.
    /// </summary>
    public (string StartIp, string EndIp) CalculateNetworkRange(string ipAddress, string subnetMask)
    {
        try
        {
            if (!IPAddress.TryParse(ipAddress, out var ip) || !IPAddress.TryParse(subnetMask, out var mask))
            {
                return ("192.168.1.1", "192.168.1.254");
            }

            var ipBytes = ip.GetAddressBytes();
            var maskBytes = mask.GetAddressBytes();

            // Calculate network address (IP AND Mask)
            var networkBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }

            // Calculate broadcast address (Network OR ~Mask)
            var broadcastBytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                broadcastBytes[i] = (byte)(networkBytes[i] | ~maskBytes[i]);
            }

            // Start IP = Network + 1, End IP = Broadcast - 1
            var startBytes = (byte[])networkBytes.Clone();
            startBytes[3] = (byte)(startBytes[3] + 1);

            var endBytes = (byte[])broadcastBytes.Clone();
            endBytes[3] = (byte)(endBytes[3] - 1);

            // Limit to max 254 hosts to prevent huge scans
            uint startNum = (uint)(startBytes[0] << 24 | startBytes[1] << 16 | startBytes[2] << 8 | startBytes[3]);
            uint endNum = (uint)(endBytes[0] << 24 | endBytes[1] << 16 | endBytes[2] << 8 | endBytes[3]);

            if (endNum - startNum > 254)
            {
                // Limit to same /24 as the host IP
                startBytes[0] = ipBytes[0];
                startBytes[1] = ipBytes[1];
                startBytes[2] = ipBytes[2];
                startBytes[3] = 1;

                endBytes[0] = ipBytes[0];
                endBytes[1] = ipBytes[1];
                endBytes[2] = ipBytes[2];
                endBytes[3] = 254;
            }

            return (new IPAddress(startBytes).ToString(), new IPAddress(endBytes).ToString());
        }
        catch
        {
            return ("192.168.1.1", "192.168.1.254");
        }
    }

    /// <summary>
    /// Get list of local IP addresses.
    /// </summary>
    public List<string> GetLocalIpAddresses()
    {
        var result = new List<string>();
        
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    result.Add(ip.ToString());
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return result;
    }

    /// <summary>
    /// Get information about all network adapters.
    /// </summary>
    public List<NetworkAdapterInfo> GetNetworkAdapters()
    {
        var adapters = new List<NetworkAdapterInfo>();
        
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                            ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in interfaces)
            {
                var ipProps = ni.GetIPProperties();
                var unicast = ipProps.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                if (unicast != null)
                {
                    adapters.Add(new NetworkAdapterInfo
                    {
                        Name = ni.Name,
                        Description = ni.Description,
                        IpAddress = unicast.Address.ToString(),
                        SubnetMask = unicast.IPv4Mask?.ToString() ?? "255.255.255.0",
                        Type = ni.NetworkInterfaceType.ToString()
                    });
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return adapters;
    }
}

/// <summary>
/// Information about a network adapter.
/// </summary>
public class NetworkAdapterInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string SubnetMask { get; set; } = "";
    public string Type { get; set; } = "";
    
    public override string ToString() => $"{Name} ({IpAddress})";
}
