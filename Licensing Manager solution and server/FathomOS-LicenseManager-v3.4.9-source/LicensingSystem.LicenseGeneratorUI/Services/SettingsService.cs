using System.IO;
using System.Text.Json;

namespace LicenseGeneratorUI.Services;

/// <summary>
/// Service for managing application settings
/// </summary>
public class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings;

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            GetLocalAppDataPath(),
            "FathomOSLicenseManager");

        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");

        _settings = LoadSettings();
    }

    /// <summary>
    /// Get the local app data path with fallbacks for edge cases
    /// </summary>
    private static string GetLocalAppDataPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (!string.IsNullOrEmpty(localAppData))
            return localAppData;

        // Fallback to UserProfile
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
            return Path.Combine(userProfile, ".FathomOS");

        // Ultimate fallback: use application directory
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData");
    }

    // Direct property accessors
    public string ServerUrl
    {
        get => string.IsNullOrEmpty(_settings.ServerUrl)
            ? "https://fathom-os-license-server.onrender.com"
            : _settings.ServerUrl;
        set => _settings.ServerUrl = value;
    }

    public string ProductName
    {
        get => _settings.ProductName;
        set => _settings.ProductName = value;
    }

    public int GracePeriodDays
    {
        get => _settings.GracePeriodDays;
        set => _settings.GracePeriodDays = value;
    }

    public string KeyStoragePath
    {
        get
        {
            if (string.IsNullOrEmpty(_settings.KeyStoragePath))
            {
                _settings.KeyStoragePath = Path.Combine(
                    GetLocalAppDataPath(),
                    "FathomOSLicenseManager",
                    "keys");
            }
            return _settings.KeyStoragePath;
        }
        set => _settings.KeyStoragePath = value;
    }

    public string? PrivateKey
    {
        get => _settings.PrivateKey;
        set => _settings.PrivateKey = value;
    }

    public string? PublicKey
    {
        get => _settings.PublicKey;
        set => _settings.PublicKey = value;
    }

    public string DefaultEdition
    {
        get => _settings.DefaultEdition;
        set => _settings.DefaultEdition = value;
    }

    public string DefaultDuration
    {
        get => _settings.DefaultDuration;
        set => _settings.DefaultDuration = value;
    }

    public string DefaultBrand
    {
        get => _settings.DefaultBrand;
        set => _settings.DefaultBrand = value;
    }

    // === NEW: Standalone Mode Settings ===

    /// <summary>
    /// API Key for server authentication
    /// </summary>
    public string? ApiKey
    {
        get => _settings.ApiKey;
        set => _settings.ApiKey = value;
    }

    /// <summary>
    /// Whether to automatically sync licenses to server when connected
    /// </summary>
    public bool AutoSyncToServer
    {
        get => _settings.AutoSyncToServer;
        set => _settings.AutoSyncToServer = value;
    }

    /// <summary>
    /// Whether the initial key setup has been completed
    /// </summary>
    public bool HasCompletedSetup
    {
        get => _settings.HasCompletedSetup;
        set => _settings.HasCompletedSetup = value;
    }

    /// <summary>
    /// Whether server configuration has been set up
    /// </summary>
    public bool HasServerConfig
    {
        get => _settings.HasServerConfig;
        set => _settings.HasServerConfig = value;
    }

    /// <summary>
    /// ID/fingerprint of the current public key (for key rotation tracking)
    /// </summary>
    public string? PublicKeyId
    {
        get => _settings.PublicKeyId;
        set => _settings.PublicKeyId = value;
    }

    /// <summary>
    /// Whether to work in offline mode (no server connection)
    /// </summary>
    public bool WorkOffline
    {
        get => _settings.WorkOffline;
        set => _settings.WorkOffline = value;
    }

    /// <summary>
    /// Last time licenses were synced to server
    /// </summary>
    public DateTime? LastSyncTime
    {
        get => _settings.LastSyncTime;
        set => _settings.LastSyncTime = value;
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Silently fail - not critical
        }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Return defaults if loading fails
        }
        return new AppSettings();
    }

    // Legacy methods for backward compatibility
    public string GetProductName() => ProductName;
    public void SetProductName(string value) { ProductName = value; Save(); }
    public int GetGracePeriod() => GracePeriodDays;
    public void SetGracePeriod(int value) { GracePeriodDays = value; Save(); }
    public string GetKeyStoragePath() => KeyStoragePath;
    public void SetKeyStoragePath(string value) { KeyStoragePath = value; Save(); }
    public string GetServerUrl() => ServerUrl;
    public void SetServerUrl(string value) { ServerUrl = value; Save(); }
}

/// <summary>
/// Application settings model
/// </summary>
public class AppSettings
{
    public string ProductName { get; set; } = "FathomOS";
    public int GracePeriodDays { get; set; } = 14;
    public string KeyStoragePath { get; set; } = string.Empty;
    public string LastLicenseDirectory { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = "https://fathom-os-license-server.onrender.com";
    public string? PrivateKey { get; set; }
    public string? PublicKey { get; set; }
    public string DefaultEdition { get; set; } = "Professional";
    public string DefaultDuration { get; set; } = "1 Year";
    public string DefaultBrand { get; set; } = "";

    // === NEW: Standalone Mode Settings ===
    public string? ApiKey { get; set; }
    public bool AutoSyncToServer { get; set; } = false;
    public bool HasCompletedSetup { get; set; } = false;
    public bool HasServerConfig { get; set; } = false;
    public string? PublicKeyId { get; set; }
    public bool WorkOffline { get; set; } = true;
    public DateTime? LastSyncTime { get; set; }
}
