namespace FathomOS.Core.Interfaces;

/// <summary>
/// Metadata for a discovered module (loaded from ModuleInfo.json)
/// </summary>
public class ModuleMetadata
{
    public string ModuleId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string IconResource { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string DllPath { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public bool IsLoaded { get; set; }
}

/// <summary>
/// Contract for module lifecycle management.
/// Handles module discovery, lazy loading, and shutdown.
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// Discover all available modules (metadata only, no DLL loading)
    /// </summary>
    void DiscoverModules();

    /// <summary>
    /// Get metadata for all discovered modules
    /// </summary>
    IReadOnlyList<ModuleMetadata> GetModuleMetadata();

    /// <summary>
    /// Get metadata for a specific module
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    /// <returns>Module metadata or null if not found</returns>
    ModuleMetadata? GetModuleMetadata(string moduleId);

    /// <summary>
    /// Load a module (if not already loaded) and return the instance
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    /// <returns>The loaded module instance</returns>
    IModule LoadModule(string moduleId);

    /// <summary>
    /// Launch a module (load if needed, then show window)
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    void LaunchModule(string moduleId);

    /// <summary>
    /// Open a file with an appropriate module
    /// </summary>
    /// <param name="filePath">Path to the file to open</param>
    /// <returns>True if a module handled the file</returns>
    bool OpenFileWithModule(string filePath);

    /// <summary>
    /// Shutdown all loaded modules
    /// </summary>
    void ShutdownAll();

    /// <summary>
    /// Check if a module is currently loaded
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    /// <returns>True if the module is loaded</returns>
    bool IsModuleLoaded(string moduleId);

    /// <summary>
    /// Event fired when a module is loaded
    /// </summary>
    event EventHandler<ModuleMetadata>? ModuleLoaded;

    /// <summary>
    /// Event fired when a module is launched
    /// </summary>
    event EventHandler<ModuleMetadata>? ModuleLaunched;
}
