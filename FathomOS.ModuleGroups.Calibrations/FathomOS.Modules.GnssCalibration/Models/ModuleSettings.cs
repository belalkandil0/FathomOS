using System.IO;
using System.Text.Json;

namespace FathomOS.Modules.GnssCalibration.Models;

/// <summary>
/// Persistent module settings saved between sessions.
/// </summary>
public class ModuleSettings
{
    // Last used paths
    public string LastNpdFolder { get; set; } = "";
    public string LastPosFolder { get; set; } = "";
    public string LastExportFolder { get; set; } = "";
    public string LastProjectFolder { get; set; } = "";
    
    // UI preferences
    public string SelectedTheme { get; set; } = "Professional";
    public bool DarkMode { get; set; } = true;
    public double WindowWidth { get; set; } = 1400;
    public double WindowHeight { get; set; } = 900;
    public bool IsMaximized { get; set; } = false;
    
    // Processing defaults
    public double DefaultSigmaThreshold { get; set; } = 3.0;
    public double DefaultToleranceValue { get; set; } = 0.15;
    public bool DefaultToleranceEnabled { get; set; } = true;
    public int DefaultUnitIndex { get; set; } = 0; // Meters
    
    // Recent files (up to 10)
    public List<string> RecentNpdFiles { get; set; } = new();
    public List<string> RecentProjectFiles { get; set; } = new();
    
    // Chart preferences
    public bool ShowChartTooltips { get; set; } = true;
    public bool HighlightSelectedPoints { get; set; } = true;
    
    // Maximum recent files to track
    private const int MaxRecentFiles = 10;
    
    /// <summary>
    /// Gets the settings file path in user's AppData folder.
    /// </summary>
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "GnssCalibration", "settings.json");
    
    /// <summary>
    /// Loads settings from disk or returns default settings.
    /// </summary>
    public static ModuleSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ModuleSettings>(json);
                return settings ?? new ModuleSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModuleSettings] Load error: {ex.Message}");
        }
        return new ModuleSettings();
    }
    
    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true 
            };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModuleSettings] Save error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Adds a file to the recent NPD files list.
    /// </summary>
    public void AddRecentNpdFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        // Remove if already exists
        RecentNpdFiles.Remove(filePath);
        
        // Add to front
        RecentNpdFiles.Insert(0, filePath);
        
        // Trim to max
        while (RecentNpdFiles.Count > MaxRecentFiles)
        {
            RecentNpdFiles.RemoveAt(RecentNpdFiles.Count - 1);
        }
    }
    
    /// <summary>
    /// Adds a file to the recent project files list.
    /// </summary>
    public void AddRecentProjectFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        RecentProjectFiles.Remove(filePath);
        RecentProjectFiles.Insert(0, filePath);
        
        while (RecentProjectFiles.Count > MaxRecentFiles)
        {
            RecentProjectFiles.RemoveAt(RecentProjectFiles.Count - 1);
        }
    }
    
    /// <summary>
    /// Updates folder from a file path.
    /// </summary>
    public void UpdateFolderFromFile(string filePath, FolderType folderType)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        var folder = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(folder)) return;
        
        switch (folderType)
        {
            case FolderType.Npd:
                LastNpdFolder = folder;
                break;
            case FolderType.Pos:
                LastPosFolder = folder;
                break;
            case FolderType.Export:
                LastExportFolder = folder;
                break;
            case FolderType.Project:
                LastProjectFolder = folder;
                break;
        }
    }
}

/// <summary>
/// Type of folder for settings persistence.
/// </summary>
public enum FolderType
{
    Npd,
    Pos,
    Export,
    Project
}
