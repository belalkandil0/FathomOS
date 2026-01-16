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
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FathomOSLicenseManager");
        
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        
        _settings = LoadSettings();
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
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
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
}
