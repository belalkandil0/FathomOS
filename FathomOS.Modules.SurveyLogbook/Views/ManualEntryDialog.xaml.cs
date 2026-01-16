using System;
using System.Collections.Generic;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Dialog for adding manual log entries.
/// Provides UI for entering event details, timestamps, and optional position data.
/// </summary>
public partial class ManualEntryDialog : MetroWindow
{
    #region Properties

    /// <summary>
    /// Gets or sets the list of available entry types.
    /// </summary>
    public List<EntryTypeItem> EntryTypes { get; set; }

    /// <summary>
    /// Gets or sets the selected entry type.
    /// </summary>
    public EntryTypeItem SelectedEntryType { get; set; }

    /// <summary>
    /// Gets or sets the entry date.
    /// </summary>
    public DateTime EntryDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the entry time.
    /// </summary>
    public DateTime? EntryTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the object/source name.
    /// </summary>
    public string ObjectName { get; set; } = "";

    /// <summary>
    /// Gets or sets the description/comment.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Gets or sets whether to include position data.
    /// </summary>
    public bool IncludePosition { get; set; }

    /// <summary>
    /// Gets or sets the Easting coordinate.
    /// </summary>
    public double? Easting { get; set; }

    /// <summary>
    /// Gets or sets the Northing coordinate.
    /// </summary>
    public double? Northing { get; set; }

    /// <summary>
    /// Gets or sets the KP value.
    /// </summary>
    public double? Kp { get; set; }

    /// <summary>
    /// Gets or sets the depth value.
    /// </summary>
    public double? Depth { get; set; }

    /// <summary>
    /// Gets the created log entry after dialog closes with OK result.
    /// </summary>
    public SurveyLogEntry? CreatedEntry { get; private set; }

    #endregion

    #region Constructor

    public ManualEntryDialog()
    {
        // Load theme before InitializeComponent
        var themeUri = new Uri("/FathomOS.Modules.SurveyLogbook;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

        // Initialize entry types
        EntryTypes = new List<EntryTypeItem>
        {
            new EntryTypeItem { Type = LogEntryType.ManualEntry, DisplayName = "Manual Entry" },
            new EntryTypeItem { Type = LogEntryType.Comment, DisplayName = "Comment" },
            new EntryTypeItem { Type = LogEntryType.SurveyLineStart, DisplayName = "Survey Line Start" },
            new EntryTypeItem { Type = LogEntryType.SurveyLineEnd, DisplayName = "Survey Line End" },
            new EntryTypeItem { Type = LogEntryType.Warning, DisplayName = "Warning" },
            new EntryTypeItem { Type = LogEntryType.Error, DisplayName = "Error" },
            new EntryTypeItem { Type = LogEntryType.EquipmentSetup, DisplayName = "Equipment Setup" },
            new EntryTypeItem { Type = LogEntryType.EquipmentFailure, DisplayName = "Equipment Failure" },
            new EntryTypeItem { Type = LogEntryType.WeatherCondition, DisplayName = "Weather Condition" },
            new EntryTypeItem { Type = LogEntryType.VesselMovement, DisplayName = "Vessel Movement" },
            new EntryTypeItem { Type = LogEntryType.PersonnelChange, DisplayName = "Personnel Change" },
            new EntryTypeItem { Type = LogEntryType.SafetyIncident, DisplayName = "Safety Incident" },
            new EntryTypeItem { Type = LogEntryType.OperationStart, DisplayName = "Operation Start" },
            new EntryTypeItem { Type = LogEntryType.OperationEnd, DisplayName = "Operation End" },
        };

        SelectedEntryType = EntryTypes[0];

        InitializeComponent();
        DataContext = this;

        // Focus on description field
        Loaded += (s, e) => { };
    }

    #endregion

    #region Event Handlers

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(Description))
        {
            System.Windows.MessageBox.Show("Please enter a description for the entry.",
                "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Create the entry
        var timestamp = EntryDate.Date;
        if (EntryTime.HasValue)
        {
            timestamp = timestamp.Add(EntryTime.Value.TimeOfDay);
        }

        CreatedEntry = new SurveyLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp,
            EntryType = SelectedEntryType.Type,
            ObjectName = ObjectName,
            Description = Description,
            Source = "Manual"
            // Note: IsManualEntry is a computed property based on EntryType
        };

        // Add position data if included
        if (IncludePosition)
        {
            CreatedEntry.Easting = Easting;
            CreatedEntry.Northing = Northing;
            CreatedEntry.Kp = Kp;
            CreatedEntry.Depth = Depth;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    #endregion
}

/// <summary>
/// Helper class for entry type selection in ComboBox.
/// </summary>
public class EntryTypeItem
{
    public LogEntryType Type { get; set; }
    public string DisplayName { get; set; } = "";
}
