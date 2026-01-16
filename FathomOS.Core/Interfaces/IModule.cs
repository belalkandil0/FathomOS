namespace FathomOS.Core.Interfaces;

using System.Windows;

/// <summary>
/// Contract that all Fathom OS modules must implement.
/// This interface defines how the Shell discovers and interacts with modules.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Unique identifier for the module (e.g., "SurveyListing", "Calibration").
    /// Used internally for module management and project files.
    /// </summary>
    string ModuleId { get; }
    
    /// <summary>
    /// Display name shown on dashboard (e.g., "Survey Listing Generator").
    /// This is what users see on the module tile.
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Short description for tooltip/card display.
    /// Should be 1-2 sentences describing the module's purpose.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Module version following semantic versioning.
    /// </summary>
    Version Version { get; }
    
    /// <summary>
    /// Icon resource path for dashboard tile.
    /// Format: "/FathomOS.Modules.ModuleName;component/Assets/icon.png"
    /// </summary>
    string IconResource { get; }
    
    /// <summary>
    /// Category for grouping on dashboard (e.g., "Data Processing", "Quality Control").
    /// Modules in the same category may be visually grouped together.
    /// </summary>
    string Category { get; }
    
    /// <summary>
    /// Display order on dashboard (lower numbers appear first).
    /// Use increments of 10 to allow insertion: 10, 20, 30, etc.
    /// </summary>
    int DisplayOrder { get; }
    
    /// <summary>
    /// Called when module is first loaded by the Shell.
    /// Use for initialization, service registration, settings loading.
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Launch the module's main window.
    /// Called when user clicks the module tile on dashboard.
    /// </summary>
    /// <param name="owner">Optional parent window for modal behavior</param>
    void Launch(Window? owner = null);
    
    /// <summary>
    /// Called when application is closing.
    /// Use for cleanup, saving state, releasing resources.
    /// </summary>
    void Shutdown();
    
    /// <summary>
    /// Check if this module can handle a specific file type.
    /// Used for file associations and drag-drop handling.
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <returns>True if module can open this file type</returns>
    bool CanHandleFile(string filePath);
    
    /// <summary>
    /// Open a file directly in this module.
    /// Called after CanHandleFile returns true.
    /// </summary>
    /// <param name="filePath">Full path to the file to open</param>
    void OpenFile(string filePath);
}
