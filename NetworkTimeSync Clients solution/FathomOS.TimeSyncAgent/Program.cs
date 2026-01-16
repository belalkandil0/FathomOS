using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FathomOS.TimeSyncAgent.Services;

namespace FathomOS.TimeSyncAgent;

/// <summary>
/// Fathom OS Time Sync Agent
/// A lightweight Windows service that listens for time sync commands from Fathom OS.
/// 
/// Installation:
///   sc create FathomOSTimeSyncAgent binPath="C:\path\to\FathomOS.TimeSyncAgent.exe"
///   sc start FathomOSTimeSyncAgent
/// 
/// Or use the provided Install-Agent.ps1 script.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Check for command-line arguments for manual operations
        if (args.Length > 0)
        {
            HandleCommandLine(args);
            return;
        }

        // Run as Windows Service
        var builder = Host.CreateApplicationBuilder(args);
        
        // Add configuration
        builder.Services.Configure<AgentConfiguration>(
            builder.Configuration.GetSection("AgentSettings"));
        
        // Register services
        builder.Services.AddSingleton<TimeService>();
        builder.Services.AddHostedService<TcpListenerService>();
        
        // Configure as Windows Service
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "Fathom OS Time Sync Agent";
        });

        var host = builder.Build();
        host.Run();
    }

    private static void HandleCommandLine(string[] args)
    {
        var command = args[0].ToLower();

        switch (command)
        {
            case "--version":
            case "-v":
                Console.WriteLine("Fathom OS Time Sync Agent v1.0.2");
                break;

            case "--help":
            case "-h":
                PrintHelp();
                break;

            case "--test":
                TestAgent();
                break;

            case "--install":
                InstallService();
                break;

            case "--uninstall":
                UninstallService();
                break;

            case "--console":
                RunConsoleMode();
                break;

            default:
                Console.WriteLine($"Unknown command: {command}");
                PrintHelp();
                break;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
Fathom OS Time Sync Agent
========================

Usage: FathomOS.TimeSyncAgent.exe [options]

Options:
  (no args)      Run as Windows Service
  --console      Run in console mode (for testing)
  --install      Install as Windows Service
  --uninstall    Uninstall Windows Service
  --test         Test agent configuration
  --version, -v  Show version
  --help, -h     Show this help

Configuration:
  Edit appsettings.json to change port and shared secret.

Default Port: 7700
Default Secret: FathomOSTimeSync2024
");
    }

    private static void TestAgent()
    {
        Console.WriteLine("Testing agent configuration...");
        
        var config = LoadConfiguration();
        Console.WriteLine($"  Port: {config.Port}");
        Console.WriteLine($"  Secret: {(string.IsNullOrEmpty(config.SharedSecret) ? "(not set)" : "****")}");
        
        var timeService = new TimeService();
        var timeInfo = timeService.GetTimeInfo();
        Console.WriteLine($"  Current UTC: {timeInfo.UtcTime:O}");
        Console.WriteLine($"  Timezone: {timeInfo.TimeZoneId}");
        
        Console.WriteLine("\nAgent configuration OK.");
    }

    private static void InstallService()
    {
        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        // For single-file publish, use the process path
        if (string.IsNullOrEmpty(exePath) || exePath.EndsWith(".dll"))
        {
            exePath = Environment.ProcessPath ?? "FathomOS.TimeSyncAgent.exe";
        }

        Console.WriteLine($"Installing service from: {exePath}");
        Console.WriteLine("\nRun the following command as Administrator:");
        Console.WriteLine($"  sc create FathomOSTimeSyncAgent binPath=\"{exePath}\" start=auto");
        Console.WriteLine("  sc description FathomOSTimeSyncAgent \"Fathom OS Time Sync Agent - Allows remote time synchronization\"");
        Console.WriteLine("  sc start FathomOSTimeSyncAgent");
        Console.WriteLine("\nOr use the Install-Agent.ps1 script.");
    }

    private static void UninstallService()
    {
        Console.WriteLine("To uninstall the service, run as Administrator:");
        Console.WriteLine("  sc stop FathomOSTimeSyncAgent");
        Console.WriteLine("  sc delete FathomOSTimeSyncAgent");
    }

    private static void RunConsoleMode()
    {
        Console.WriteLine("Running in console mode. Press Ctrl+C to exit.");
        
        var config = LoadConfiguration();
        var timeService = new TimeService();
        var listener = new TcpListenerService(
            Microsoft.Extensions.Options.Options.Create(config),
            timeService,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TcpListenerService>.Instance);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        listener.StartAsync(cts.Token).Wait();
        
        Console.WriteLine($"Listening on port {config.Port}...");
        
        while (!cts.Token.IsCancellationRequested)
        {
            Thread.Sleep(1000);
        }

        listener.StopAsync(CancellationToken.None).Wait();
        Console.WriteLine("Agent stopped.");
    }

    private static AgentConfiguration LoadConfiguration()
    {
        var config = new AgentConfiguration();
        
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("AgentSettings", out var settings))
                {
                    if (settings.TryGetProperty("Port", out var port))
                        config.Port = port.GetInt32();
                    if (settings.TryGetProperty("SharedSecret", out var secret))
                        config.SharedSecret = secret.GetString() ?? config.SharedSecret;
                }
            }
            catch { }
        }
        
        return config;
    }
}

/// <summary>
/// Agent configuration settings.
/// </summary>
public class AgentConfiguration
{
    public int Port { get; set; } = 7700;
    public string SharedSecret { get; set; } = "FathomOSTimeSync2024";
    public int ConnectionTimeoutMs { get; set; } = 30000;
    public bool AllowTimeSet { get; set; } = true;
    public bool AllowNtpSync { get; set; } = true;
}
