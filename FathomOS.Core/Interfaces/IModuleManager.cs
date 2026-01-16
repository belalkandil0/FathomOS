using System.Windows;

namespace FathomOS.Core.Interfaces;

/// <summary>
/// Contract for module lifecycle management.
/// Handles module discovery, lazy loading, and shutdown.
/// Owned by: SHELL-AGENT (implementation in Shell)
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// Get root modules (not in groups) sorted by display order.
    /// Returns metadata only - modules are not loaded until launched.
    /// </summary>
    IReadOnlyList<IModuleMetadata> Modules { get; }

    /// <summary>
    /// Get all groups sorted by display order.
    /// </summary>
    IReadOnlyList<IModuleGroupMetadata> Groups { get; }

    /// <summary>
    /// Discover all available modules (metadata only, no DLL loading).
    /// </summary>
    void DiscoverModules();

    /// <summary>
    /// Launch a module by ID. This triggers lazy loading if not already loaded.
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    /// <param name="owner">Optional parent window</param>
    void LaunchModule(string moduleId, Window? owner = null);

    /// <summary>
    /// Check if a specific module is licensed.
    /// </summary>
    /// <param name="moduleId">The module identifier</param>
    /// <returns>True if the module is licensed for use</returns>
    bool IsModuleLicensed(string moduleId);

    /// <summary>
    /// Open a file with an appropriate module.
    /// </summary>
    /// <param name="filePath">Path to the file to open</param>
    /// <param name="owner">Optional parent window</param>
    /// <returns>True if a module handled the file</returns>
    bool OpenFileWithModule(string filePath, Window? owner = null);

    /// <summary>
    /// Shutdown all loaded modules.
    /// </summary>
    void ShutdownAll();

    /// <summary>
    /// Get count of loaded modules (for diagnostics).
    /// </summary>
    int LoadedModuleCount { get; }

    /// <summary>
    /// Get count of discovered modules (for diagnostics).
    /// </summary>
    int DiscoveredModuleCount { get; }
}
