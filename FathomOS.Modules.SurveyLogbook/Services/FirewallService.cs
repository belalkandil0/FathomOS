// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/FirewallService.cs
// Purpose: Windows Firewall rule management for NaviPac UDP/TCP connections
// ============================================================================

using System.Diagnostics;
using System.Security.Principal;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Service for managing Windows Firewall rules.
/// 
/// IMPORTANT NOTES:
/// - TCP (outbound): Usually does NOT require firewall rules as Windows allows outbound by default
/// - UDP (inbound): REQUIRES firewall rule to allow incoming datagrams on the listening port
/// 
/// This service uses netsh commands which require elevated (Administrator) permissions.
/// </summary>
public static class FirewallService
{
    private const string RULE_NAME_PREFIX = "FathomOS_SurveyLogbook_";
    
    /// <summary>
    /// Gets whether the current process is running with administrator privileges.
    /// </summary>
    public static bool IsRunningAsAdministrator
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
    
    /// <summary>
    /// Checks if a firewall rule exists for the specified port.
    /// </summary>
    /// <param name="port">Port number.</param>
    /// <param name="protocol">Protocol (TCP or UDP).</param>
    /// <returns>True if rule exists.</returns>
    public static async Task<bool> RuleExistsAsync(int port, NaviPacProtocol protocol)
    {
        try
        {
            var ruleName = GetRuleName(port, protocol);
            var result = await RunNetshCommandAsync($"advfirewall firewall show rule name=\"{ruleName}\"");
            return result.ExitCode == 0 && result.Output.Contains(ruleName);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FirewallService: Error checking rule existence: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets information about whether firewall rules are needed for the current configuration.
    /// </summary>
    /// <param name="protocol">Protocol being used.</param>
    /// <param name="port">Port number.</param>
    /// <returns>Firewall status information.</returns>
    public static FirewallStatus GetFirewallStatus(NaviPacProtocol protocol, int port)
    {
        var status = new FirewallStatus
        {
            Protocol = protocol,
            Port = port
        };
        
        if (protocol == NaviPacProtocol.TCP)
        {
            // TCP outbound connections usually don't need firewall rules
            status.RequiresRule = false;
            status.Message = "TCP connections are outbound and typically don't require firewall rules.";
            status.Severity = FirewallSeverity.Info;
        }
        else // UDP
        {
            // UDP listening requires inbound firewall rule
            status.RequiresRule = true;
            status.Message = $"UDP listener on port {port} requires an inbound firewall rule. " +
                            "Without this rule, NaviPac data may be blocked by Windows Firewall.";
            status.Severity = FirewallSeverity.Warning;
        }
        
        return status;
    }
    
    /// <summary>
    /// Creates a firewall rule for the specified port.
    /// Requires administrator privileges.
    /// </summary>
    /// <param name="port">Port number.</param>
    /// <param name="protocol">Protocol (TCP or UDP).</param>
    /// <param name="description">Rule description.</param>
    /// <returns>Result of the operation.</returns>
    public static async Task<FirewallResult> CreateRuleAsync(int port, NaviPacProtocol protocol, string? description = null)
    {
        if (!IsRunningAsAdministrator)
        {
            return new FirewallResult
            {
                Success = false,
                Message = "Administrator privileges required to create firewall rules. " +
                         "Please run the application as Administrator or create the rule manually.",
                RequiresElevation = true
            };
        }
        
        try
        {
            var ruleName = GetRuleName(port, protocol);
            var protocolStr = protocol.ToString().ToLower();
            var desc = description ?? $"Allow {protocol} traffic for Fathom OS Survey Logbook on port {port}";
            
            // Check if rule already exists
            if (await RuleExistsAsync(port, protocol))
            {
                return new FirewallResult
                {
                    Success = true,
                    Message = $"Firewall rule '{ruleName}' already exists.",
                    RuleAlreadyExists = true
                };
            }
            
            // Create inbound rule
            var command = $"advfirewall firewall add rule " +
                         $"name=\"{ruleName}\" " +
                         $"dir=in " +
                         $"action=allow " +
                         $"protocol={protocolStr} " +
                         $"localport={port} " +
                         $"description=\"{desc}\"";
            
            var result = await RunNetshCommandAsync(command);
            
            if (result.ExitCode == 0)
            {
                return new FirewallResult
                {
                    Success = true,
                    Message = $"Firewall rule '{ruleName}' created successfully."
                };
            }
            else
            {
                return new FirewallResult
                {
                    Success = false,
                    Message = $"Failed to create firewall rule: {result.Error}",
                    ErrorDetails = result.Output
                };
            }
        }
        catch (Exception ex)
        {
            return new FirewallResult
            {
                Success = false,
                Message = $"Error creating firewall rule: {ex.Message}",
                Exception = ex
            };
        }
    }
    
    /// <summary>
    /// Removes a firewall rule for the specified port.
    /// Requires administrator privileges.
    /// </summary>
    /// <param name="port">Port number.</param>
    /// <param name="protocol">Protocol (TCP or UDP).</param>
    /// <returns>Result of the operation.</returns>
    public static async Task<FirewallResult> RemoveRuleAsync(int port, NaviPacProtocol protocol)
    {
        if (!IsRunningAsAdministrator)
        {
            return new FirewallResult
            {
                Success = false,
                Message = "Administrator privileges required to remove firewall rules.",
                RequiresElevation = true
            };
        }
        
        try
        {
            var ruleName = GetRuleName(port, protocol);
            
            // Check if rule exists
            if (!await RuleExistsAsync(port, protocol))
            {
                return new FirewallResult
                {
                    Success = true,
                    Message = $"Firewall rule '{ruleName}' does not exist.",
                    RuleAlreadyExists = false
                };
            }
            
            var command = $"advfirewall firewall delete rule name=\"{ruleName}\"";
            var result = await RunNetshCommandAsync(command);
            
            if (result.ExitCode == 0)
            {
                return new FirewallResult
                {
                    Success = true,
                    Message = $"Firewall rule '{ruleName}' removed successfully."
                };
            }
            else
            {
                return new FirewallResult
                {
                    Success = false,
                    Message = $"Failed to remove firewall rule: {result.Error}",
                    ErrorDetails = result.Output
                };
            }
        }
        catch (Exception ex)
        {
            return new FirewallResult
            {
                Success = false,
                Message = $"Error removing firewall rule: {ex.Message}",
                Exception = ex
            };
        }
    }
    
    /// <summary>
    /// Opens a UAC-elevated prompt to create a firewall rule.
    /// </summary>
    /// <param name="port">Port number.</param>
    /// <param name="protocol">Protocol (TCP or UDP).</param>
    /// <returns>True if process was started successfully.</returns>
    public static bool RequestElevatedRuleCreation(int port, NaviPacProtocol protocol)
    {
        try
        {
            var ruleName = GetRuleName(port, protocol);
            var protocolStr = protocol.ToString().ToLower();
            var desc = $"Allow {protocol} traffic for Fathom OS Survey Logbook on port {port}";
            
            // Create a batch command to run elevated
            var netshCommand = $"netsh advfirewall firewall add rule " +
                              $"name=\"{ruleName}\" " +
                              $"dir=in " +
                              $"action=allow " +
                              $"protocol={protocolStr} " +
                              $"localport={port} " +
                              $"description=\"{desc}\"";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {netshCommand} & pause",
                Verb = "runas", // Request elevation
                UseShellExecute = true
            };
            
            Process.Start(startInfo);
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            Debug.WriteLine("FirewallService: User cancelled UAC elevation request");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FirewallService: Error requesting elevation: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets the command to manually create a firewall rule (for display to user).
    /// </summary>
    /// <param name="port">Port number.</param>
    /// <param name="protocol">Protocol (TCP or UDP).</param>
    /// <returns>The netsh command string.</returns>
    public static string GetManualRuleCommand(int port, NaviPacProtocol protocol)
    {
        var ruleName = GetRuleName(port, protocol);
        var protocolStr = protocol.ToString().ToLower();
        
        return $"netsh advfirewall firewall add rule " +
               $"name=\"{ruleName}\" " +
               $"dir=in " +
               $"action=allow " +
               $"protocol={protocolStr} " +
               $"localport={port}";
    }
    
    /// <summary>
    /// Checks all Survey Logbook firewall rules.
    /// </summary>
    /// <returns>List of existing rules.</returns>
    public static async Task<List<FirewallRuleInfo>> GetExistingRulesAsync()
    {
        var rules = new List<FirewallRuleInfo>();
        
        try
        {
            var result = await RunNetshCommandAsync($"advfirewall firewall show rule name=all");
            
            if (result.ExitCode != 0)
                return rules;
            
            var lines = result.Output.Split('\n');
            FirewallRuleInfo? currentRule = null;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("Rule Name:", StringComparison.OrdinalIgnoreCase))
                {
                    var ruleName = trimmedLine.Substring("Rule Name:".Length).Trim();
                    
                    if (ruleName.StartsWith(RULE_NAME_PREFIX))
                    {
                        currentRule = new FirewallRuleInfo { Name = ruleName };
                        rules.Add(currentRule);
                    }
                    else
                    {
                        currentRule = null;
                    }
                }
                else if (currentRule != null)
                {
                    if (trimmedLine.StartsWith("LocalPort:", StringComparison.OrdinalIgnoreCase))
                    {
                        var portStr = trimmedLine.Substring("LocalPort:".Length).Trim();
                        if (int.TryParse(portStr, out var port))
                            currentRule.Port = port;
                    }
                    else if (trimmedLine.StartsWith("Protocol:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentRule.Protocol = trimmedLine.Substring("Protocol:".Length).Trim();
                    }
                    else if (trimmedLine.StartsWith("Enabled:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentRule.Enabled = trimmedLine.Contains("Yes", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FirewallService: Error getting existing rules: {ex.Message}");
        }
        
        return rules;
    }
    
    #region Private Methods
    
    private static string GetRuleName(int port, NaviPacProtocol protocol)
    {
        return $"{RULE_NAME_PREFIX}{protocol}_{port}";
    }
    
    private static async Task<NetshResult> RunNetshCommandAsync(string arguments)
    {
        var result = new NetshResult();
        
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            
            result.Output = await process.StandardOutput.ReadToEndAsync();
            result.Error = await process.StandardError.ReadToEndAsync();
            
            await process.WaitForExitAsync();
            result.ExitCode = process.ExitCode;
        }
        catch (Exception ex)
        {
            result.ExitCode = -1;
            result.Error = ex.Message;
        }
        
        return result;
    }
    
    #endregion
    
    #region Helper Classes
    
    private class NetshResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
    
    #endregion
}

#region Public Classes

/// <summary>
/// Result of a firewall operation.
/// </summary>
public class FirewallResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool RequiresElevation { get; set; }
    public bool RuleAlreadyExists { get; set; }
    public string? ErrorDetails { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Firewall status information.
/// </summary>
public class FirewallStatus
{
    public NaviPacProtocol Protocol { get; set; }
    public int Port { get; set; }
    public bool RequiresRule { get; set; }
    public string Message { get; set; } = string.Empty;
    public FirewallSeverity Severity { get; set; }
}

/// <summary>
/// Severity level for firewall status.
/// </summary>
public enum FirewallSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Information about an existing firewall rule.
/// </summary>
public class FirewallRuleInfo
{
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

#endregion
