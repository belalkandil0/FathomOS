// FathomOS.Shell/Security/AntiDebug.cs
// Anti-debugging protection to prevent reverse engineering

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FathomOS.Shell.Security;

/// <summary>
/// Anti-debugging protection mechanisms
/// Detects and prevents debugging attempts
/// </summary>
public static class AntiDebug
{
    // Windows API imports for advanced detection
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

    // ProcessInformationClass for debug port
    private const int ProcessDebugPort = 7;
    private const int ProcessDebugObjectHandle = 30;
    private const int ProcessDebugFlags = 31;

    /// <summary>
    /// Performs all anti-debug checks
    /// Returns true if debugging is detected
    /// </summary>
    public static bool IsDebuggingDetected()
    {
        try
        {
            // Check 1: Managed debugger check
            if (Debugger.IsAttached)
                return true;

            // Check 2: IsDebuggerPresent API
            if (IsDebuggerPresent())
                return true;

            // Check 3: Remote debugger check
            if (CheckForRemoteDebugger())
                return true;

            // Check 4: Debug port check (NtQueryInformationProcess)
            if (CheckDebugPort())
                return true;

            // Check 5: Timing check (debuggers slow down execution)
            if (TimingCheck())
                return true;

            // Check 6: Check for common debugging tools
            if (CheckForDebuggerProcesses())
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
    /// Check for remote debugger using Windows API
    /// </summary>
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

    /// <summary>
    /// Check debug port via NtQueryInformationProcess
    /// </summary>
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

            // If debug port is non-zero, debugger is attached
            return debugPort != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Timing check - debuggers significantly slow down execution
    /// </summary>
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

    /// <summary>
    /// Check for common debugging/reverse engineering tools
    /// </summary>
    private static bool CheckForDebuggerProcesses()
    {
        try
        {
            string[] debuggerProcesses = new[]
            {
                "dnspy",
                "dnspy-x86",
                "dnspy.console",
                "ilspy",
                "dotpeek",
                "dotpeek64",
                "x64dbg",
                "x32dbg",
                "ollydbg",
                "windbg",
                "ida",
                "ida64",
                "idaq",
                "idaq64",
                "immunitydebugger",
                "cheatengine",
                "cheatengine-x86_64",
                "httpdebugger",
                "fiddler",
                "wireshark",
                "processhacker",
                "procmon",
                "procmon64",
                "procexp",
                "procexp64",
                "de4dot",
                "de4dot-x64",
                "reflexil"
            };

            var runningProcesses = Process.GetProcesses()
                .Select(p => p.ProcessName.ToLowerInvariant())
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

    /// <summary>
    /// Check for virtual machine (often used for analysis)
    /// Returns true if running in VM - you may want to just warn, not block
    /// </summary>
    public static bool IsRunningInVM()
    {
        try
        {
            // Check for common VM indicators
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_ComputerSystem");
            
            foreach (var item in searcher.Get())
            {
                string manufacturer = item["Manufacturer"]?.ToString()?.ToLowerInvariant() ?? "";
                string model = item["Model"]?.ToString()?.ToLowerInvariant() ?? "";

                if (manufacturer.Contains("vmware") ||
                    manufacturer.Contains("virtual") ||
                    manufacturer.Contains("xen") ||
                    model.Contains("vmware") ||
                    model.Contains("virtual") ||
                    model.Contains("virtualbox"))
                {
                    return true;
                }
            }

            // Check for Hyper-V
            using var baseBoard = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_BaseBoard");
            
            foreach (var item in baseBoard.Get())
            {
                string product = item["Product"]?.ToString()?.ToLowerInvariant() ?? "";
                if (product.Contains("virtual"))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Start continuous anti-debug monitoring on background thread
    /// </summary>
    public static void StartContinuousMonitoring(Action onDebuggerDetected)
    {
        var monitorThread = new Thread(() =>
        {
            while (true)
            {
                Thread.Sleep(5000); // Check every 5 seconds

                if (IsDebuggingDetected())
                {
                    onDebuggerDetected?.Invoke();
                    break;
                }
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Lowest
        };

        monitorThread.Start();
    }
}
