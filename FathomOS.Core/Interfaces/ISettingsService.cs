namespace FathomOS.Core.Interfaces;

/// <summary>
/// Contract for application and module settings management.
/// Provides type-safe setting access with defaults.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Get a setting value, returning default if not found
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>The setting value or default</returns>
    T Get<T>(string key, T defaultValue);

    /// <summary>
    /// Set a setting value
    /// </summary>
    /// <typeparam name="T">The type of the setting value</typeparam>
    /// <param name="key">The setting key</param>
    /// <param name="value">The value to store</param>
    void Set<T>(string key, T value);

    /// <summary>
    /// Check if a setting exists
    /// </summary>
    /// <param name="key">The setting key</param>
    /// <returns>True if the setting exists</returns>
    bool Exists(string key);

    /// <summary>
    /// Remove a setting
    /// </summary>
    /// <param name="key">The setting key</param>
    void Remove(string key);

    /// <summary>
    /// Save all settings to storage
    /// </summary>
    void Save();

    /// <summary>
    /// Reload settings from storage
    /// </summary>
    void Reload();
}
