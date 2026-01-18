using FathomOS.Core.Services;

namespace FathomOS.Modules.ProjectManagement;

/// <summary>
/// Project Management module-specific settings.
/// </summary>
public class ProjectManagementSettings : ModuleSettingsBase
{
    /// <summary>
    /// Module identifier for settings storage.
    /// </summary>
    public const string MODULE_ID = "ProjectManagement";

    #region Project Settings

    /// <summary>
    /// Default project folder path.
    /// </summary>
    public string? DefaultProjectFolder { get; set; }

    /// <summary>
    /// Whether to auto-save projects.
    /// </summary>
    public bool AutoSaveEnabled { get; set; } = true;

    /// <summary>
    /// Auto-save interval in minutes.
    /// </summary>
    public int AutoSaveIntervalMinutes { get; set; } = 5;

    #endregion

    #region Notification Settings

    /// <summary>
    /// Whether to show milestone deadline notifications.
    /// </summary>
    public bool NotifyMilestoneDeadlines { get; set; } = true;

    /// <summary>
    /// Days before deadline to show warning.
    /// </summary>
    public int DeadlineWarningDays { get; set; } = 7;

    /// <summary>
    /// Whether to show deliverable status notifications.
    /// </summary>
    public bool NotifyDeliverableStatus { get; set; } = true;

    #endregion

    #region Display Settings

    /// <summary>
    /// Default view mode (List, Kanban, Gantt, etc.).
    /// </summary>
    public string DefaultViewMode { get; set; } = "List";

    /// <summary>
    /// Whether to show completed milestones by default.
    /// </summary>
    public bool ShowCompletedMilestones { get; set; } = true;

    /// <summary>
    /// Whether to show archived projects.
    /// </summary>
    public bool ShowArchivedProjects { get; set; } = false;

    #endregion

    #region Certificate Settings

    /// <summary>
    /// Whether to prompt for certificate on milestone completion.
    /// </summary>
    public bool PromptForCertificateOnCompletion { get; set; } = true;

    /// <summary>
    /// Default company name for certificates.
    /// </summary>
    public string? DefaultCompanyName { get; set; }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Loads the ProjectManagement settings.
    /// </summary>
    public static ProjectManagementSettings Load()
    {
        return ModuleSettings.Load<ProjectManagementSettings>(MODULE_ID);
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    public static void Save(ProjectManagementSettings settings)
    {
        ModuleSettings.Save(MODULE_ID, settings);
    }

    #endregion
}
