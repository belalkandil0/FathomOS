using System;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Dialog for adding manual position fixes.
/// </summary>
public partial class PositionFixDialog : MetroWindow
{
    /// <summary>
    /// Gets the created position fix after dialog closes with OK result.
    /// </summary>
    public PositionFix? CreatedFix { get; private set; }

    public PositionFixDialog()
    {
        // Load theme before InitializeComponent
        var themeUri = new Uri("/FathomOS.Modules.SurveyLogbook;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

        InitializeComponent();

        // Set default values
        DatePicker.SelectedDate = DateTime.Now;
        TimePicker.SelectedDateTime = DateTime.Now;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        // Get fix type
        var fixType = FixTypeCombo.SelectedIndex switch
        {
            0 => PositionFixType.SetEastingNorthing,
            1 => PositionFixType.Waypoint,
            2 => PositionFixType.Calibration,
            3 => PositionFixType.Manual,
            _ => PositionFixType.Manual
        };

        // Get timestamp
        var date = DatePicker.SelectedDate ?? DateTime.Now;
        var time = TimePicker.SelectedDateTime?.TimeOfDay ?? DateTime.Now.TimeOfDay;
        var timestamp = date.Date.Add(time);

        // Create the fix
        CreatedFix = new PositionFix
        {
            Id = Guid.NewGuid(),
            Date = date.Date,
            Time = time,
            PositionFixType = fixType,
            ObjectName = ObjectNameText.Text,
            ComputedEasting = ComputedEasting.Value ?? 0,
            ComputedNorthing = ComputedNorthing.Value ?? 0,
            RequiredEasting = RequiredEasting.Value ?? 0,
            RequiredNorthing = RequiredNorthing.Value ?? 0,
            Description = DescriptionText.Text
        };

        // Calculate errors
        CreatedFix.ErrorEasting = CreatedFix.ComputedEasting - CreatedFix.RequiredEasting;
        CreatedFix.ErrorNorthing = CreatedFix.ComputedNorthing - CreatedFix.RequiredNorthing;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
