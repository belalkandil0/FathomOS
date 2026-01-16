namespace FathomOS.Modules.NetworkTimeSync.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FathomOS.Modules.NetworkTimeSync.Enums;
using FathomOS.Modules.NetworkTimeSync.Models;

/// <summary>
/// Service for saving and loading module configuration.
/// </summary>
public class ConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Default configuration file path.
    /// </summary>
    public static string DefaultConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FathomOS",
        "Modules",
        "NetworkTimeSync",
        "config.json");

    /// <summary>
    /// Ensure the configuration directory exists.
    /// </summary>
    public static void EnsureConfigDirectory()
    {
        var dir = Path.GetDirectoryName(DefaultConfigPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// Save configuration to file.
    /// </summary>
    public static void SaveConfiguration(ModuleConfiguration config, string? path = null)
    {
        path ??= DefaultConfigPath;
        EnsureConfigDirectory();

        config.LastSaved = DateTime.Now;
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Load configuration from file.
    /// </summary>
    public static ModuleConfiguration LoadConfiguration(string? path = null)
    {
        path ??= DefaultConfigPath;

        if (!File.Exists(path))
        {
            return new ModuleConfiguration();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ModuleConfiguration>(json, JsonOptions) 
                   ?? new ModuleConfiguration();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Config] Error loading config: {ex.Message}");
            return new ModuleConfiguration();
        }
    }

    /// <summary>
    /// Save computers to configuration.
    /// </summary>
    public static void SaveComputers(IEnumerable<NetworkComputer> computers, ModuleConfiguration config)
    {
        config.Computers = computers.Select(c => new SavedComputer
        {
            IpAddress = c.IpAddress,
            Hostname = c.Hostname,
            Port = c.Port,
            DiscoveryMethod = c.DiscoveryMethod.ToString(),
            Notes = c.Notes
        }).ToList();
    }

    /// <summary>
    /// Load computers from configuration.
    /// </summary>
    public static List<NetworkComputer> LoadComputers(ModuleConfiguration config)
    {
        return config.Computers.Select(sc => new NetworkComputer
        {
            IpAddress = sc.IpAddress,
            Hostname = sc.Hostname,
            Port = sc.Port,
            DiscoveryMethod = Enum.TryParse<DiscoveryMethod>(sc.DiscoveryMethod, out var dm) 
                ? dm 
                : DiscoveryMethod.Manual,
            Notes = sc.Notes,
            Status = SyncStatus.Unknown
        }).ToList();
    }

    /// <summary>
    /// Export configuration to a specific file (.nts format).
    /// </summary>
    public static void ExportConfiguration(ModuleConfiguration config, string filePath)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Import configuration from a file.
    /// </summary>
    public static ModuleConfiguration ImportConfiguration(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Configuration file not found", filePath);
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ModuleConfiguration>(json, JsonOptions) 
               ?? throw new InvalidDataException("Invalid configuration file");
    }

    /// <summary>
    /// Get available timezone IDs.
    /// </summary>
    public static List<string> GetAvailableTimeZones()
    {
        var zones = new List<string> { "UTC" };
        zones.AddRange(TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => tz.Id)
            .OrderBy(id => id));
        return zones;
    }

    /// <summary>
    /// Get common NTP servers.
    /// </summary>
    public static List<string> GetCommonNtpServers()
    {
        return new List<string>
        {
            "time.windows.com",
            "pool.ntp.org",
            "time.nist.gov",
            "time.google.com",
            "time.apple.com",
            "ntp.ubuntu.com"
        };
    }
}
