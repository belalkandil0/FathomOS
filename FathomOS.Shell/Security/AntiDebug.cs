// FathomOS.Shell/Security/AntiDebug.cs
// Comprehensive anti-debugging and environment detection protection

using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace FathomOS.Shell.Security;

/// <summary>
/// Comprehensive anti-debugging protection mechanisms.
///
/// Features:
/// - Managed and native debugger detection
/// - Virtual machine detection (VMware, VirtualBox, Hyper-V, etc.)
/// - Sandbox environment detection
/// - Remote desktop detection
/// - Process integrity validation
/// - Timing-based detection
/// - Common analysis tool detection
/// - Continuous background monitoring
/// </summary>
public static class AntiDebug
{
    #region Windows API Imports

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool isDebuggerPresent);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref IntPtr processInformation,
        int processInformationLength,
        ref int returnLength);

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    [DllImport("ntdll.dll")]
    private static extern int NtSetInformationThread(
        IntPtr threadHandle,
        int threadInformationClass,
        IntPtr threadInformation,
        int threadInformationLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO
    {
        public ushort processorArchitecture;
        public ushort reserved;
        public uint pageSize;
        public IntPtr minimumApplicationAddress;
        public IntPtr maximumApplicationAddress;
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }

    // NtQueryInformationProcess classes
    private const int ProcessDebugPort = 7;
    private const int ProcessDebugObjectHandle = 30;
    private const int ProcessDebugFlags = 31;

    // NtSetInformationThread
    private const int ThreadHideFromDebugger = 0x11;

    #endregion

    #region Detection State

    private static volatile bool _monitoringActive;
    private static CancellationTokenSource? _monitoringCts;
    private static readonly object _monitoringLock = new();

    /// <summary>
    /// Event raised when debugging is detected during monitoring.
    /// </summary>
    public static event Action<DebugDetectionResult>? OnDebuggerDetected;

    #endregion

    #region Primary Detection Methods

    /// <summary>
    /// Performs all anti-debug checks.
    /// </summary>
    /// <returns>True if debugging is detected.</returns>
    public static bool IsDebuggingDetected()
    {
        try
        {
            // Check 1: Managed debugger
            if (Debugger.IsAttached)
                return true;

            // Check 2: IsDebuggerPresent API
            if (IsDebuggerPresent())
                return true;

            // Check 3: Remote debugger
            if (CheckForRemoteDebugger())
                return true;

            // Check 4: Debug port
            if (CheckDebugPort())
                return true;

            // Check 5: Debug object handle
            if (CheckDebugObjectHandle())
                return true;

            // Check 6: Debug flags
            if (CheckDebugFlags())
                return true;

            // Check 7: Timing check
            if (TimingCheck())
                return true;

            // Check 8: Common debugger processes
            if (CheckForDebuggerProcesses())
                return true;

            // Check 9: Parent process check
            if (CheckParentProcess())
                return true;

            // Check 10: Anti-anti-debug hooks
            if (CheckForHooks())
                return true;

            return false;
        }
        catch
        {
            // If any check fails, assume not debugging (fail open for usability)
            return false;
        }
    }

    /// <summary>
    /// Performs comprehensive detection and returns detailed results.
    /// </summary>
    /// <returns>Detailed detection result.</returns>
    public static DebugDetectionResult PerformComprehensiveCheck()
    {
        var result = new DebugDetectionResult
        {
            CheckTime = DateTime.UtcNow
        };

        // Debugger checks
        result.ManagedDebuggerAttached = Debugger.IsAttached;
        result.NativeDebuggerPresent = SafeCheck(() => IsDebuggerPresent());
        result.RemoteDebuggerPresent = SafeCheck(CheckForRemoteDebugger);
        result.DebugPortDetected = SafeCheck(CheckDebugPort);
        result.DebugObjectDetected = SafeCheck(CheckDebugObjectHandle);
        result.DebugFlagsAnomalous = SafeCheck(CheckDebugFlags);
        result.TimingAnomalyDetected = SafeCheck(TimingCheck);
        result.DebuggerProcessesFound = SafeCheck(CheckForDebuggerProcesses);
        result.SuspiciousParentProcess = SafeCheck(CheckParentProcess);
        result.HooksDetected = SafeCheck(CheckForHooks);

        // Environment checks
        result.VirtualMachineDetected = SafeCheck(CheckForVirtualMachine);
        result.SandboxDetected = SafeCheck(CheckForSandbox);
        result.RemoteDesktopActive = SafeCheck(CheckForRemoteDesktop);

        // Calculate overall detection
        result.IsDebuggingDetected = result.ManagedDebuggerAttached ||
                                      result.NativeDebuggerPresent ||
                                      result.RemoteDebuggerPresent ||
                                      result.DebugPortDetected ||
                                      result.DebugObjectDetected ||
                                      result.DebugFlagsAnomalous ||
                                      result.TimingAnomalyDetected ||
                                      result.DebuggerProcessesFound ||
                                      result.SuspiciousParentProcess ||
                                      result.HooksDetected;

        return result;
    }

    #endregion

    #region Virtual Machine Detection

    /// <summary>
    /// Checks if running in a virtual machine.
    /// </summary>
    /// <returns>True if VM is detected.</returns>
    public static bool CheckForVirtualMachine()
    {
        try
        {
            // Check 1: WMI computer system
            if (CheckWmiComputerSystem())
                return true;

            // Check 2: WMI base board
            if (CheckWmiBaseBoard())
                return true;

            // Check 3: WMI BIOS
            if (CheckWmiBios())
                return true;

            // Check 4: Registry indicators
            if (CheckVmRegistryKeys())
                return true;

            // Check 5: MAC address prefix
            if (CheckVmMacAddresses())
                return true;

            // Check 6: Known VM processes
            if (CheckVmProcesses())
                return true;

            // Check 7: Known VM drivers
            if (CheckVmDrivers())
                return true;

            // Check 8: Processor count anomaly (single CPU often indicates VM)
            if (CheckProcessorAnomaly())
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckWmiComputerSystem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");

            foreach (var item in searcher.Get())
            {
                var manufacturer = item["Manufacturer"]?.ToString()?.ToLowerInvariant() ?? "";
                var model = item["Model"]?.ToString()?.ToLowerInvariant() ?? "";

                var vmIndicators = new[] { "vmware", "virtual", "xen", "qemu", "kvm", "parallels", "bochs" };

                foreach (var indicator in vmIndicators)
                {
                    if (manufacturer.Contains(indicator) || model.Contains(indicator))
                        return true;
                }

                // Microsoft Hyper-V
                if (manufacturer.Contains("microsoft") && model.Contains("virtual"))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckWmiBaseBoard()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");

            foreach (var item in searcher.Get())
            {
                var manufacturer = item["Manufacturer"]?.ToString()?.ToLowerInvariant() ?? "";
                var product = item["Product"]?.ToString()?.ToLowerInvariant() ?? "";

                var vmIndicators = new[] { "virtual", "vmware", "oracle", "xen" };

                foreach (var indicator in vmIndicators)
                {
                    if (manufacturer.Contains(indicator) || product.Contains(indicator))
                        return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckWmiBios()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");

            foreach (var item in searcher.Get())
            {
                var version = item["SMBIOSBIOSVersion"]?.ToString()?.ToLowerInvariant() ?? "";
                var serialNumber = item["SerialNumber"]?.ToString()?.ToLowerInvariant() ?? "";

                var vmIndicators = new[] { "vmware", "virtual", "xen", "qemu", "vbox", "parallels" };

                foreach (var indicator in vmIndicators)
                {
                    if (version.Contains(indicator) || serialNumber.Contains(indicator))
                        return true;
                }

                // VirtualBox serial number pattern
                if (serialNumber == "0")
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckVmRegistryKeys()
    {
        try
        {
            var vmKeys = new[]
            {
                @"SOFTWARE\VMware, Inc.\VMware Tools",
                @"SOFTWARE\Oracle\VirtualBox Guest Additions",
                @"SYSTEM\CurrentControlSet\Services\VBoxGuest",
                @"SYSTEM\CurrentControlSet\Services\vmci",
                @"SYSTEM\CurrentControlSet\Services\vmhgfs",
                @"SOFTWARE\Microsoft\Virtual Machine\Guest\Parameters",
            };

            foreach (var keyPath in vmKeys)
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                if (key != null)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckVmMacAddresses()
    {
        try
        {
            // Known VM MAC address prefixes
            var vmMacPrefixes = new[]
            {
                "00:05:69", // VMware
                "00:0C:29", // VMware
                "00:1C:14", // VMware
                "00:50:56", // VMware
                "08:00:27", // VirtualBox
                "00:1C:42", // Parallels
                "00:16:3E", // Xen
                "00:15:5D", // Hyper-V
            };

            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;

                var mac = ni.GetPhysicalAddress().ToString();
                if (mac.Length >= 6)
                {
                    var macPrefix = $"{mac[0..2]}:{mac[2..4]}:{mac[4..6]}";
                    if (vmMacPrefixes.Contains(macPrefix))
                        return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckVmProcesses()
    {
        try
        {
            var vmProcesses = new[]
            {
                "vmtoolsd", "vmwaretray", "vmwareuser",     // VMware
                "vboxservice", "vboxtray",                   // VirtualBox
                "prl_tools", "prl_cc",                       // Parallels
                "xenservice",                                 // Xen
                "vmcompute", "vmms"                           // Hyper-V
            };

            var runningProcesses = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLowerInvariant())
                .ToHashSet();

            foreach (var vmProcess in vmProcesses)
            {
                if (runningProcesses.Contains(vmProcess))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckVmDrivers()
    {
        try
        {
            var vmDrivers = new[]
            {
                @"\Device\vmci",
                @"\Device\VBoxMouse",
                @"\Device\VBoxGuest",
                @"\\.\HGFS",
                @"\\.\vmci"
            };

            foreach (var driver in vmDrivers)
            {
                try
                {
                    using var fs = File.Open(driver, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    return true;
                }
                catch
                {
                    // Driver not accessible - expected if not in VM
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckProcessorAnomaly()
    {
        try
        {
            var sysInfo = new SYSTEM_INFO();
            GetSystemInfo(ref sysInfo);

            // Single processor is suspicious (most VMs default to 1 core)
            // But this is a weak indicator, only use as supporting evidence
            return sysInfo.numberOfProcessors == 1;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Sandbox Detection

    /// <summary>
    /// Checks if running in a sandbox environment.
    /// </summary>
    /// <returns>True if sandbox is detected.</returns>
    public static bool CheckForSandbox()
    {
        try
        {
            // Check 1: Known sandbox processes
            if (CheckSandboxProcesses())
                return true;

            // Check 2: Sandbox-specific registry keys
            if (CheckSandboxRegistryKeys())
                return true;

            // Check 3: Sandbox-specific files/directories
            if (CheckSandboxFiles())
                return true;

            // Check 4: User profile anomalies
            if (CheckUserProfileAnomaly())
                return true;

            // Check 5: Recent file count (sandboxes often have few recent files)
            if (CheckRecentFileCount())
                return true;

            // Check 6: Computer name patterns
            if (CheckComputerNamePattern())
                return true;

            // Check 7: Uptime check (sandboxes often just booted)
            if (CheckUptimeAnomaly())
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckSandboxProcesses()
    {
        try
        {
            var sandboxProcesses = new[]
            {
                "sandboxie", "sbiectrl", "sbiesvc",     // Sandboxie
                "cuckoo",                                 // Cuckoo Sandbox
                "joe",                                    // Joe Sandbox
                "anubis",                                 // Anubis
                "threatanalyzer",                         // ThreatAnalyzer
                "avmb",                                   // Avira
            };

            var runningProcesses = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLowerInvariant())
                .ToHashSet();

            foreach (var sandbox in sandboxProcesses)
            {
                if (runningProcesses.Contains(sandbox))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckSandboxRegistryKeys()
    {
        try
        {
            var sandboxKeys = new[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Sandboxie",
                @"SYSTEM\CurrentControlSet\Services\SbieDrv",
                @"SOFTWARE\Cuckoo",
            };

            foreach (var keyPath in sandboxKeys)
            {
                using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                if (key != null)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckSandboxFiles()
    {
        try
        {
            var sandboxPaths = new[]
            {
                @"C:\agent\agent.pyw",                        // Cuckoo
                @"C:\sandbox",                                 // Generic sandbox
                @"C:\cuckoo",                                  // Cuckoo
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Sandboxie"),
            };

            foreach (var path in sandboxPaths)
            {
                if (Directory.Exists(path) || File.Exists(path))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckUserProfileAnomaly()
    {
        try
        {
            var userName = Environment.UserName.ToLowerInvariant();

            // Common sandbox/analysis usernames
            var suspiciousNames = new[]
            {
                "user", "admin", "test", "malware", "virus",
                "sample", "sandbox", "analysis", "cuckoo",
                "john", "john doe"
            };

            foreach (var name in suspiciousNames)
            {
                if (userName == name || userName.Contains(name))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckRecentFileCount()
    {
        try
        {
            var recentFolder = Environment.GetFolderPath(Environment.SpecialFolder.Recent);

            if (!Directory.Exists(recentFolder))
                return true; // No recent folder is suspicious

            var recentFiles = Directory.GetFiles(recentFolder, "*", SearchOption.TopDirectoryOnly);

            // Very few recent files suggests sandbox
            return recentFiles.Length < 3;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckComputerNamePattern()
    {
        try
        {
            var computerName = Environment.MachineName.ToLowerInvariant();

            // Check for suspicious patterns
            var suspiciousPatterns = new[]
            {
                "sandbox", "malware", "virus", "test",
                "cuckoo", "joe", "analysis", "sample"
            };

            foreach (var pattern in suspiciousPatterns)
            {
                if (computerName.Contains(pattern))
                    return true;
            }

            // Check for random-looking names (common in automated sandboxes)
            if (computerName.Length >= 10 && computerName.All(c => char.IsLetterOrDigit(c)))
            {
                var letterCount = computerName.Count(char.IsLetter);
                var digitCount = computerName.Count(char.IsDigit);

                // Roughly equal letters and digits suggests random generation
                if (Math.Abs(letterCount - digitCount) <= 2)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckUptimeAnomaly()
    {
        try
        {
            var tickCount = GetTickCount();
            var uptimeMinutes = tickCount / 60000;

            // Less than 5 minutes uptime is suspicious
            return uptimeMinutes < 5;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Remote Desktop Detection

    /// <summary>
    /// Checks if the session is running via Remote Desktop.
    /// </summary>
    /// <returns>True if remote desktop is detected.</returns>
    public static bool CheckForRemoteDesktop()
    {
        try
        {
            // Check 1: System Metrics
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return true;

            // Check 2: Session ID (0 = console, >0 often means remote)
            var sessionId = Process.GetCurrentProcess().SessionId;
            if (sessionId == 0)
                return false; // Console session

            // Check 3: RDP specific environment variable
            var sessionName = Environment.GetEnvironmentVariable("SESSIONNAME") ?? "";
            if (sessionName.StartsWith("RDP", StringComparison.OrdinalIgnoreCase))
                return true;

            // Check 4: Check for RDP client processes
            var rdpProcesses = new[] { "mstsc", "rdpclip" };
            var runningProcesses = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLowerInvariant())
                .ToHashSet();

            foreach (var rdp in rdpProcesses)
            {
                if (runningProcesses.Contains(rdp))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Process Integrity Validation

    /// <summary>
    /// Validates the integrity of the current process.
    /// </summary>
    /// <returns>True if process integrity appears compromised.</returns>
    public static bool ValidateProcessIntegrity()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();

            // Check 1: Process has expected modules
            if (!ValidateLoadedModules(currentProcess))
                return false;

            // Check 2: Main module integrity
            if (!ValidateMainModule(currentProcess))
                return false;

            // Check 3: Memory region integrity (basic check)
            if (!ValidateMemoryRegions(currentProcess))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool ValidateLoadedModules(Process process)
    {
        try
        {
            var modules = process.Modules;
            var suspiciousModules = new[]
            {
                "dbghelp.dll",     // Debugging support (can be legitimate)
                "ntdll_hook.dll",  // Hook library
                "detoursxx.dll",   // Detours hooking
                "easyhook32.dll",
                "easyhook64.dll"
            };

            foreach (ProcessModule module in modules)
            {
                var moduleName = module.ModuleName.ToLowerInvariant();

                foreach (var suspicious in suspiciousModules)
                {
                    if (moduleName.Contains(suspicious))
                        return false;
                }
            }

            return true;
        }
        catch
        {
            return true; // Can't enumerate, assume OK
        }
    }

    private static bool ValidateMainModule(Process process)
    {
        try
        {
            var mainModule = process.MainModule;
            if (mainModule == null)
                return false;

            // Verify the main module path is in expected location
            var expectedPath = AppDomain.CurrentDomain.BaseDirectory;
            var actualPath = Path.GetDirectoryName(mainModule.FileName) ?? "";

            if (!actualPath.StartsWith(expectedPath, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch
        {
            return true; // Can't access, assume OK
        }
    }

    private static bool ValidateMemoryRegions(Process process)
    {
        // Basic memory validation - could be expanded
        try
        {
            var handle = process.Handle;
            // Additional memory validation could be implemented here
            return handle != IntPtr.Zero;
        }
        catch
        {
            return true;
        }
    }

    #endregion

    #region Continuous Monitoring

    /// <summary>
    /// Starts continuous anti-debug monitoring on a background thread.
    /// </summary>
    /// <param name="onDetected">Action to execute when debugging is detected.</param>
    /// <param name="checkIntervalMs">Interval between checks in milliseconds.</param>
    public static void StartContinuousMonitoring(Action onDetected, int checkIntervalMs = 5000)
    {
        lock (_monitoringLock)
        {
            if (_monitoringActive)
                return;

            _monitoringActive = true;
            _monitoringCts = new CancellationTokenSource();

            var token = _monitoringCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(checkIntervalMs, token);

                        if (IsDebuggingDetected())
                        {
                            var result = PerformComprehensiveCheck();
                            OnDebuggerDetected?.Invoke(result);
                            onDetected?.Invoke();
                            break;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        // Continue monitoring despite errors
                    }
                }
            }, token);
        }
    }

    /// <summary>
    /// Stops continuous monitoring.
    /// </summary>
    public static void StopContinuousMonitoring()
    {
        lock (_monitoringLock)
        {
            if (!_monitoringActive)
                return;

            _monitoringCts?.Cancel();
            _monitoringCts?.Dispose();
            _monitoringCts = null;
            _monitoringActive = false;
        }
    }

    #endregion

    #region Private Detection Methods

    private static bool CheckForRemoteDebugger()
    {
        try
        {
            bool isDebuggerPresent = false;
            CheckRemoteDebuggerPresent(GetCurrentProcess(), ref isDebuggerPresent);
            return isDebuggerPresent;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckDebugPort()
    {
        try
        {
            IntPtr debugPort = IntPtr.Zero;
            int returnLength = 0;

            int status = NtQueryInformationProcess(
                GetCurrentProcess(),
                ProcessDebugPort,
                ref debugPort,
                IntPtr.Size,
                ref returnLength);

            return debugPort != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckDebugObjectHandle()
    {
        try
        {
            IntPtr debugObject = IntPtr.Zero;
            int returnLength = 0;

            int status = NtQueryInformationProcess(
                GetCurrentProcess(),
                ProcessDebugObjectHandle,
                ref debugObject,
                IntPtr.Size,
                ref returnLength);

            // STATUS_PORT_NOT_SET (0xC0000353) means no debugger
            return status == 0 && debugObject != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckDebugFlags()
    {
        try
        {
            IntPtr debugFlags = IntPtr.Zero;
            int returnLength = 0;

            int status = NtQueryInformationProcess(
                GetCurrentProcess(),
                ProcessDebugFlags,
                ref debugFlags,
                IntPtr.Size,
                ref returnLength);

            // If NoDebugInherit flag is 0, we're being debugged
            return status == 0 && debugFlags == IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private static bool TimingCheck()
    {
        try
        {
            var sw = Stopwatch.StartNew();

            // Simple operations that should be very fast
            int sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                sum += i;
            }

            sw.Stop();

            // If this simple loop takes more than 50ms, something is wrong
            // Normal execution: < 1ms, Debugger stepping: > 100ms
            return sw.ElapsedMilliseconds > 50;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckForDebuggerProcesses()
    {
        try
        {
            var debuggerProcesses = new[]
            {
                // .NET debuggers/decompilers
                "dnspy", "dnspy-x86", "dnspy.console",
                "ilspy", "dotpeek", "dotpeek64",
                "jetbrains.dotpeek", "reflexil",
                "de4dot", "de4dot-x64",

                // Native debuggers
                "x64dbg", "x32dbg", "ollydbg",
                "windbg", "windbgx", "cdb",
                "ida", "ida64", "idaq", "idaq64",
                "immunitydebugger", "radare2",

                // Memory editors
                "cheatengine", "cheatengine-x86_64",
                "artmoney", "tsearch",

                // Network analyzers
                "wireshark", "fiddler", "charles",
                "httpdebugger", "burp",

                // Process analyzers
                "processhacker", "procmon", "procmon64",
                "procexp", "procexp64", "apimonitor",
                "pestudio", "regmon", "filemon",

                // Deobfuscators
                "confuserex", "megadumper", "dumper",

                // API monitors
                "apimonitor", "rohitab",
            };

            var runningProcesses = Process.GetProcesses()
                .Select(p =>
                {
                    try { return p.ProcessName.ToLowerInvariant(); }
                    catch { return ""; }
                })
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            foreach (var debugger in debuggerProcesses)
            {
                if (runningProcesses.Contains(debugger))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckParentProcess()
    {
        try
        {
            using var process = Process.GetCurrentProcess();

            // Get parent process ID
            int parentId = 0;
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}");

                foreach (var item in searcher.Get())
                {
                    parentId = Convert.ToInt32(item["ParentProcessId"]);
                    break;
                }
            }
            catch
            {
                return false;
            }

            if (parentId == 0)
                return false;

            // Get parent process name
            try
            {
                using var parent = Process.GetProcessById(parentId);
                var parentName = parent.ProcessName.ToLowerInvariant();

                // Suspicious parent processes
                var suspiciousParents = new[]
                {
                    "cmd", "powershell", "pwsh",
                    "python", "python3", "pythonw",
                    "cscript", "wscript",
                    "bash", "sh",
                    "dnspy", "x64dbg", "ollydbg"
                };

                foreach (var suspicious in suspiciousParents)
                {
                    if (parentName.Contains(suspicious))
                        return true;
                }
            }
            catch
            {
                // Parent process may have exited
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckForHooks()
    {
        try
        {
            // Check if common anti-debug APIs are hooked
            // by verifying their entry points

            var ntdll = GetModuleHandle("ntdll.dll");
            var kernel32 = GetModuleHandle("kernel32.dll");

            if (ntdll == IntPtr.Zero || kernel32 == IntPtr.Zero)
                return false;

            // Additional hook detection could be implemented here
            // by checking the first bytes of API functions for JMP instructions

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool SafeCheck(Func<bool> check)
    {
        try
        {
            return check();
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Detailed result of a debugging detection check.
/// </summary>
public class DebugDetectionResult
{
    /// <summary>
    /// Time the check was performed.
    /// </summary>
    public DateTime CheckTime { get; set; }

    /// <summary>
    /// Overall result - true if any debugging indicator was detected.
    /// </summary>
    public bool IsDebuggingDetected { get; set; }

    // Debugger checks
    public bool ManagedDebuggerAttached { get; set; }
    public bool NativeDebuggerPresent { get; set; }
    public bool RemoteDebuggerPresent { get; set; }
    public bool DebugPortDetected { get; set; }
    public bool DebugObjectDetected { get; set; }
    public bool DebugFlagsAnomalous { get; set; }
    public bool TimingAnomalyDetected { get; set; }
    public bool DebuggerProcessesFound { get; set; }
    public bool SuspiciousParentProcess { get; set; }
    public bool HooksDetected { get; set; }

    // Environment checks
    public bool VirtualMachineDetected { get; set; }
    public bool SandboxDetected { get; set; }
    public bool RemoteDesktopActive { get; set; }

    /// <summary>
    /// Gets a summary of all detected threats.
    /// </summary>
    public IEnumerable<string> GetDetectedThreats()
    {
        if (ManagedDebuggerAttached) yield return "Managed debugger attached";
        if (NativeDebuggerPresent) yield return "Native debugger present";
        if (RemoteDebuggerPresent) yield return "Remote debugger present";
        if (DebugPortDetected) yield return "Debug port detected";
        if (DebugObjectDetected) yield return "Debug object handle detected";
        if (DebugFlagsAnomalous) yield return "Debug flags anomalous";
        if (TimingAnomalyDetected) yield return "Timing anomaly detected";
        if (DebuggerProcessesFound) yield return "Debugger processes found";
        if (SuspiciousParentProcess) yield return "Suspicious parent process";
        if (HooksDetected) yield return "API hooks detected";
        if (VirtualMachineDetected) yield return "Virtual machine detected";
        if (SandboxDetected) yield return "Sandbox detected";
        if (RemoteDesktopActive) yield return "Remote desktop active";
    }

    public override string ToString()
    {
        var threats = GetDetectedThreats().ToList();
        return threats.Count == 0
            ? "No threats detected"
            : $"Detected: {string.Join(", ", threats)}";
    }
}
