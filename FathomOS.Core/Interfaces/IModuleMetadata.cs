namespace FathomOS.Core.Interfaces;

/// <summary>
/// Metadata for a module that can be loaded without loading the module's DLL.
/// Used for lazy loading - only metadata is loaded during discovery, DLL is loaded on demand.
/// </summary>
public interface IModuleMetadata
{
    /// <summary>
    /// Unique identifier for the module (e.g., "SurveyListing", "GnssCalibration").
    /// </summary>
    string ModuleId { get; }

    /// <summary>
    /// Display name shown on dashboard (e.g., "Survey Listing Generator").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Short description of the module's purpose.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Module version string (e.g., "1.0.0").
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Category for grouping on dashboard (e.g., "Data Processing", "Calibration").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Display order on dashboard (lower numbers appear first).
    /// </summary>
    int DisplayOrder { get; }

    /// <summary>
    /// File types this module can handle (e.g., [".npd", ".rlx"]).
    /// </summary>
    string[] SupportedFileTypes { get; }

    /// <summary>
    /// Path to the module's DLL file.
    /// </summary>
    string DllPath { get; }

    /// <summary>
    /// Path to the module's icon file.
    /// </summary>
    string IconPath { get; }

    /// <summary>
    /// Whether the module's DLL has been loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Optional group ID if this module belongs to a group.
    /// </summary>
    string? GroupId { get; }
}

/// <summary>
/// Metadata for a module group (collection of related modules).
/// </summary>
public interface IModuleGroupMetadata
{
    /// <summary>
    /// Unique identifier for the group.
    /// </summary>
    string GroupId { get; }

    /// <summary>
    /// Display name shown on dashboard.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of the group.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Display order on dashboard.
    /// </summary>
    int DisplayOrder { get; }

    /// <summary>
    /// Path to the group's icon file.
    /// </summary>
    string IconPath { get; }

    /// <summary>
    /// Module IDs contained in this group.
    /// </summary>
    IReadOnlyList<string> ModuleIds { get; }
}
