using FathomOS.Core.Interfaces;
using System.Text.Json;

namespace FathomOS.Shell.Services;

/// <summary>
/// Persistent settings service using JSON file storage.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private Dictionary<string, JsonElement> _settings = new();
    private readonly object _lock = new();

    public SettingsService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FathomOS"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");

        Reload();
    }

    /// <inheritdoc />
    public T Get<T>(string key, T defaultValue)
    {
        lock (_lock)
        {
            if (_settings.TryGetValue(key, out var element))
            {
                try
                {
                    return element.Deserialize<T>() ?? defaultValue;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            var json = JsonSerializer.SerializeToElement(value);
            _settings[key] = json;
        }
    }

    /// <inheritdoc />
    public bool Exists(string key)
    {
        lock (_lock)
        {
            return _settings.ContainsKey(key);
        }
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        lock (_lock)
        {
            _settings.Remove(key);
        }
    }

    /// <inheritdoc />
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsService: Failed to save: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public void Reload()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
                }
                else
                {
                    _settings = new();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsService: Failed to load: {ex.Message}");
                _settings = new();
            }
        }
    }
}
