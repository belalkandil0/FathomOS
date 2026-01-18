using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FathomOS.Core.Services;

/// <summary>
/// Provides standardized, per-module settings persistence with type-safe access.
/// Each module stores its settings in a separate JSON file under %AppData%/FathomOS/{ModuleId}/settings.json
/// </summary>
/// <remarks>
/// Usage in modules:
/// <code>
/// // Load settings
/// var settings = ModuleSettings.Load&lt;MyModuleSettings&gt;("MyModuleId");
///
/// // Modify and save
/// settings.SomeSetting = newValue;
/// ModuleSettings.Save("MyModuleId", settings);
///
/// // Or use the instance method
/// settings.Save();
/// </code>
/// </remarks>
public static class ModuleSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null, // Keep PascalCase for compatibility
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Gets the settings directory path for a module.
    /// </summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <returns>Full path to the module's settings directory.</returns>
    public static string GetSettingsDirectory(string moduleId)
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS",
            moduleId);
    }

    /// <summary>
    /// Gets the settings file path for a module.
    /// </summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <returns>Full path to the module's settings.json file.</returns>
    public static string GetSettingsPath(string moduleId)
    {
        return Path.Combine(GetSettingsDirectory(moduleId), "settings.json");
    }

    /// <summary>
    /// Loads settings for a module, returning a new instance with defaults if not found.
    /// </summary>
    /// <typeparam name="T">The settings type (must have a parameterless constructor).</typeparam>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <returns>The loaded settings or a new instance with defaults.</returns>
    public static T Load<T>(string moduleId) where T : ModuleSettingsBase, new()
    {
        try
        {
            var path = GetSettingsPath(moduleId);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<T>(json, SerializerOptions);
                if (settings != null)
                {
                    settings.ModuleId = moduleId;
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModuleSettings] Failed to load settings for {moduleId}: {ex.Message}");
        }

        // Return new instance with defaults
        var defaultSettings = new T { ModuleId = moduleId };
        return defaultSettings;
    }

    /// <summary>
    /// Saves settings for a module to disk.
    /// </summary>
    /// <typeparam name="T">The settings type.</typeparam>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <param name="settings">The settings to save.</param>
    public static void Save<T>(string moduleId, T settings) where T : ModuleSettingsBase
    {
        try
        {
            var directory = GetSettingsDirectory(moduleId);
            Directory.CreateDirectory(directory);

            var path = GetSettingsPath(moduleId);
            var json = JsonSerializer.Serialize(settings, SerializerOptions);
            File.WriteAllText(path, json);

            System.Diagnostics.Debug.WriteLine($"[ModuleSettings] Saved settings for {moduleId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModuleSettings] Failed to save settings for {moduleId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if settings exist for a module.
    /// </summary>
    /// <param name="moduleId">The unique module identifier.</param>
    /// <returns>True if settings file exists.</returns>
    public static bool Exists(string moduleId)
    {
        return File.Exists(GetSettingsPath(moduleId));
    }

    /// <summary>
    /// Deletes settings for a module.
    /// </summary>
    /// <param name="moduleId">The unique module identifier.</param>
    public static void Delete(string moduleId)
    {
        try
        {
            var path = GetSettingsPath(moduleId);
            if (File.Exists(path))
            {
                File.Delete(path);
                System.Diagnostics.Debug.WriteLine($"[ModuleSettings] Deleted settings for {moduleId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModuleSettings] Failed to delete settings for {moduleId}: {ex.Message}");
        }
    }
}

/// <summary>
/// Base class for module settings. All module-specific settings classes should inherit from this.
/// </summary>
/// <remarks>
/// Provides common functionality:
/// - Automatic module ID tracking
/// - Instance-level Save() method
/// - Window state persistence helpers
/// </remarks>
public abstract class ModuleSettingsBase
{
    /// <summary>
    /// The module ID this settings instance belongs to.
    /// Set automatically when loading/saving.
    /// </summary>
    [JsonIgnore]
    public string ModuleId { get; internal set; } = string.Empty;

    #region Common Settings (inherited by all modules)

    /// <summary>
    /// Window width in pixels.
    /// </summary>
    public double WindowWidth { get; set; } = 1200;

    /// <summary>
    /// Window height in pixels.
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// Whether window was maximized.
    /// </summary>
    public bool IsMaximized { get; set; } = false;

    /// <summary>
    /// Window left position (optional).
    /// </summary>
    public double? WindowLeft { get; set; }

    /// <summary>
    /// Window top position (optional).
    /// </summary>
    public double? WindowTop { get; set; }

    /// <summary>
    /// Last used directory for file operations.
    /// </summary>
    public string? LastDirectory { get; set; }

    /// <summary>
    /// Last export path.
    /// </summary>
    public string? LastExportPath { get; set; }

    /// <summary>
    /// Recent files list.
    /// </summary>
    public List<string> RecentFiles { get; set; } = new();

    /// <summary>
    /// Maximum number of recent files to track.
    /// </summary>
    public int MaxRecentFiles { get; set; } = 10;

    #endregion

    /// <summary>
    /// Saves these settings to disk.
    /// </summary>
    public void Save()
    {
        if (string.IsNullOrEmpty(ModuleId))
        {
            throw new InvalidOperationException("ModuleId must be set before saving. Use ModuleSettings.Save(moduleId, settings) instead.");
        }

        // Use reflection to call the generic Save method with the correct type
        var type = GetType();
        var saveMethod = typeof(ModuleSettings).GetMethod(nameof(ModuleSettings.Save))!.MakeGenericMethod(type);
        saveMethod.Invoke(null, new object[] { ModuleId, this });
    }

    /// <summary>
    /// Updates window state from current window properties.
    /// </summary>
    /// <param name="width">Current window width.</param>
    /// <param name="height">Current window height.</param>
    /// <param name="isMaximized">Whether window is maximized.</param>
    /// <param name="left">Window left position (optional).</param>
    /// <param name="top">Window top position (optional).</param>
    public void UpdateWindowState(double width, double height, bool isMaximized, double? left = null, double? top = null)
    {
        if (!isMaximized)
        {
            WindowWidth = width;
            WindowHeight = height;
            WindowLeft = left;
            WindowTop = top;
        }
        IsMaximized = isMaximized;
    }

    /// <summary>
    /// Adds a file to the recent files list.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    public void AddRecentFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        // Remove if already exists (will be re-added at top)
        RecentFiles.Remove(filePath);

        // Add at beginning
        RecentFiles.Insert(0, filePath);

        // Trim to max
        while (RecentFiles.Count > MaxRecentFiles)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }
    }

    /// <summary>
    /// Removes a file from the recent files list.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    public void RemoveRecentFile(string filePath)
    {
        RecentFiles.Remove(filePath);
    }

    /// <summary>
    /// Clears all recent files.
    /// </summary>
    public void ClearRecentFiles()
    {
        RecentFiles.Clear();
    }
}
