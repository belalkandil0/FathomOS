namespace FathomOS.Core;

/// <summary>
/// Centralized application information and version management.
/// Update version here and it will be reflected throughout the application.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// Application name
    /// </summary>
    public const string Name = "Fathom OS";
    
    /// <summary>
    /// Full application name with module
    /// </summary>
    public const string FullName = "Fathom OS Survey Listing";
    
    /// <summary>
    /// Major version number
    /// </summary>
    public const int VersionMajor = 1;
    
    /// <summary>
    /// Minor version number
    /// </summary>
    public const int VersionMinor = 0;
    
    /// <summary>
    /// Build/Patch version number
    /// </summary>
    public const int VersionBuild = 45;
    
    /// <summary>
    /// Version string (e.g., "1.0.45")
    /// </summary>
    public const string VersionString = "1.0.45";
    
    /// <summary>
    /// Full version string with prefix (e.g., "v1.0.45")
    /// </summary>
    public const string VersionDisplay = "v1.0.45";
    
    /// <summary>
    /// Version object for comparisons
    /// </summary>
    public static Version Version => new Version(VersionMajor, VersionMinor, VersionBuild);
    
    /// <summary>
    /// Company/Author name
    /// </summary>
    public const string Company = "Fathom OS";
    
    /// <summary>
    /// Copyright notice
    /// </summary>
    public static string Copyright => $"© {DateTime.Now.Year} {Company}";
    
    /// <summary>
    /// Application description
    /// </summary>
    public const string Description = "Survey Listing Generator - Process NPD survey data with route alignment, " +
                                       "tide corrections, smoothing, and multiple export formats.";
    
    /// <summary>
    /// Generator string for export files
    /// </summary>
    public static string GeneratorString => $"{FullName} {VersionDisplay}";
    
    /// <summary>
    /// Build date (compile time)
    /// </summary>
    public static DateTime BuildDate => new DateTime(2026, 1, 12); // Update on each release
    
    /// <summary>
    /// Get version info for display
    /// </summary>
    public static string GetVersionInfo()
    {
        return $"{Name} {VersionDisplay}\n{Copyright}\n{Description}";
    }
}
