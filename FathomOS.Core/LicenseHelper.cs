namespace FathomOS.Core;

/// <summary>
/// Static helper for license checking across modules.
/// The Shell sets these delegates at startup, and modules use them to check features.
/// 
/// NEW IN v2.0: Module-based licensing
/// - Licenses are now per-module (Module:SurveyListing, Module:TideAnalysis, etc.)
/// - Tiers bundle modules (Basic, Professional, Enterprise)
/// - All features within a licensed module are available (no more PRO-specific features)
/// </summary>
public static class LicenseHelper
{
    /// <summary>
    /// Delegate that checks if the current module is licensed.
    /// In module-based licensing, if the module is licensed, ALL its features are available.
    /// </summary>
    public static Func<string, bool> IsModuleLicensed { get; set; } = (_) =>
        throw new InvalidOperationException("License system not initialized");
    
    /// <summary>
    /// Delegate that checks if a specific feature is enabled (for backwards compatibility)
    /// </summary>
    public static Func<string, bool> IsFeatureEnabled { get; set; } = (_) => false;
    
    /// <summary>
    /// Delegate that gets the current tier name
    /// </summary>
    public static Func<string?> GetCurrentTier { get; set; } = () => null;
    
    /// <summary>
    /// Delegate that shows a feature locked message
    /// </summary>
    public static Action<string> ShowFeatureLockedMessage { get; set; } = (featureName) =>
    {
        System.Windows.MessageBox.Show(
            $"{featureName} requires an upgraded license.\n\n" +
            "Contact support to upgrade your license.",
            "Feature Not Available",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    };
    
    /// <summary>
    /// Check if user has access to a feature within their licensed module.
    /// In the new module-based system, if the module is licensed, ALL features are available.
    /// This method is kept for backwards compatibility but now just returns true for licensed modules.
    /// </summary>
    /// <param name="featureName">Name of the feature being accessed (for logging/display)</param>
    /// <returns>True if access granted (always true when module is licensed)</returns>
    [Obsolete("Use IsModuleLicensed directly. In module-based licensing, all module features are available.")]
    public static bool RequirePro(string featureName)
    {
        // In the new module-based system, if the user has access to the module,
        // they have access to ALL features within that module.
        // The module-level check is done in ModuleManager.LaunchModule()
        // So by the time this is called, the module is already verified as licensed.
        return true;
    }
    
    /// <summary>
    /// Check if a specific module is licensed
    /// </summary>
    /// <param name="moduleId">Module identifier (e.g., "SurveyListing", "TideAnalysis")</param>
    /// <returns>True if module is licensed</returns>
    public static bool CheckModuleLicense(string moduleId)
    {
        return IsModuleLicensed(moduleId);
    }
    
    /// <summary>
    /// Check if a specific feature is enabled (for backwards compatibility)
    /// </summary>
    /// <param name="featureName">Feature identifier</param>
    /// <returns>True if enabled</returns>
    public static bool HasFeature(string featureName)
    {
        return IsFeatureEnabled(featureName);
    }
}
