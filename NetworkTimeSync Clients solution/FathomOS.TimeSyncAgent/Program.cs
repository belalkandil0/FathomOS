using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FathomOS.TimeSyncAgent.Models;
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
        builder.Services.Configure<RateLimitSettings>(
            builder.Configuration.GetSection("RateLimitSettings"));

        // Register services
        builder.Services.AddSingleton<TimeService>();
        builder.Services.AddSingleton<RateLimiter>();
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
  IMPORTANT: You MUST configure a unique SharedSecret before use.
  Generate a secure secret using: openssl rand -base64 32

Default Port: 7700
Default Secret: (none - must be configured)
");
    }

    private static void TestAgent()
    {
        Console.WriteLine("Testing agent configuration...");

        var config = LoadConfiguration();
        Console.WriteLine($"  Port: {config.Port}");
        Console.WriteLine($"  Secret: {(string.IsNullOrEmpty(config.SharedSecret) ? "(not set)" : "****")}");

        var rateLimitSettings = LoadRateLimitSettings();
        Console.WriteLine($"  Rate Limiting: {(rateLimitSettings.Enabled ? "Enabled" : "Disabled")}");
        if (rateLimitSettings.Enabled)
        {
            Console.WriteLine($"    Max requests/min/IP: {rateLimitSettings.MaxRequestsPerMinutePerIp}");
            Console.WriteLine($"    Max total requests/min: {rateLimitSettings.MaxTotalRequestsPerMinute}");
            Console.WriteLine($"    Failed attempts before backoff: {rateLimitSettings.FailedAttemptsBeforeBackoff}");
        }

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
        var rateLimitSettings = LoadRateLimitSettings();
        var timeService = new TimeService();
        var rateLimiter = new RateLimiter(
            Microsoft.Extensions.Options.Options.Create(rateLimitSettings),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RateLimiter>.Instance);
        var listener = new TcpListenerService(
            Microsoft.Extensions.Options.Options.Create(config),
            timeService,
            rateLimiter,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<TcpListenerService>.Instance);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        listener.StartAsync(cts.Token).Wait();

        Console.WriteLine($"Listening on port {config.Port}...");
        Console.WriteLine($"Rate limiting: {(rateLimitSettings.Enabled ? "Enabled" : "Disabled")}");

        while (!cts.Token.IsCancellationRequested)
        {
            Thread.Sleep(1000);
        }

        listener.StopAsync(CancellationToken.None).Wait();
        rateLimiter.Dispose();
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

    private static RateLimitSettings LoadRateLimitSettings()
    {
        var settings = new RateLimitSettings();

        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("RateLimitSettings", out var rateLimitSection))
                {
                    if (rateLimitSection.TryGetProperty("Enabled", out var enabled))
                        settings.Enabled = enabled.GetBoolean();
                    if (rateLimitSection.TryGetProperty("MaxRequestsPerMinutePerIp", out var maxPerIp))
                        settings.MaxRequestsPerMinutePerIp = maxPerIp.GetInt32();
                    if (rateLimitSection.TryGetProperty("MaxTotalRequestsPerMinute", out var maxTotal))
                        settings.MaxTotalRequestsPerMinute = maxTotal.GetInt32();
                    if (rateLimitSection.TryGetProperty("FailedAttemptsBeforeBackoff", out var failedAttempts))
                        settings.FailedAttemptsBeforeBackoff = failedAttempts.GetInt32();
                    if (rateLimitSection.TryGetProperty("InitialBackoffSeconds", out var initialBackoff))
                        settings.InitialBackoffSeconds = initialBackoff.GetInt32();
                    if (rateLimitSection.TryGetProperty("MaxBackoffSeconds", out var maxBackoff))
                        settings.MaxBackoffSeconds = maxBackoff.GetInt32();
                    if (rateLimitSection.TryGetProperty("BackoffMultiplier", out var multiplier))
                        settings.BackoffMultiplier = multiplier.GetDouble();
                    if (rateLimitSection.TryGetProperty("TrackingWindowMinutes", out var window))
                        settings.TrackingWindowMinutes = window.GetInt32();
                    if (rateLimitSection.TryGetProperty("FailedAttemptResetMinutes", out var resetMinutes))
                        settings.FailedAttemptResetMinutes = resetMinutes.GetInt32();
                    if (rateLimitSection.TryGetProperty("WhitelistedIps", out var whitelist))
                        settings.WhitelistedIps = whitelist.GetString() ?? settings.WhitelistedIps;
                    if (rateLimitSection.TryGetProperty("LogViolations", out var logViolations))
                        settings.LogViolations = logViolations.GetBoolean();
                    if (rateLimitSection.TryGetProperty("CleanupIntervalMinutes", out var cleanup))
                        settings.CleanupIntervalMinutes = cleanup.GetInt32();
                }
            }
            catch { }
        }

        return settings;
    }
}

/// <summary>
/// Agent configuration settings.
/// </summary>
public class AgentConfiguration
{
    public int Port { get; set; } = 7700;

    /// <summary>
    /// Shared secret for authentication.
    /// SECURITY FIX (VULN-001): Default is empty - must be configured per installation.
    /// </summary>
    public string SharedSecret { get; set; } = string.Empty;

    public int ConnectionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Whether to allow remote time setting commands.
    /// SECURITY FIX (MISSING-005 / Task 4.4): Default is FALSE for secure-by-default.
    /// Must be explicitly enabled by administrator after verifying security requirements.
    /// </summary>
    public bool AllowTimeSet { get; set; } = false;

    /// <summary>
    /// Whether to allow remote NTP sync commands.
    /// SECURITY FIX (MISSING-005 / Task 4.4): Default is FALSE for secure-by-default.
    /// Must be explicitly enabled by administrator after verifying security requirements.
    /// </summary>
    public bool AllowNtpSync { get; set; } = false;

    /// <summary>
    /// Validates that the configuration is secure.
    /// </summary>
    public bool IsSecretConfigured()
    {
        return !string.IsNullOrEmpty(SharedSecret) &&
               SharedSecret.Length >= 16 &&
               SharedSecret != "FathomOSTimeSync2024"; // Reject known weak default
    }
}
