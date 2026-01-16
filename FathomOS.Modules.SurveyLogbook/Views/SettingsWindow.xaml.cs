using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyLogbook.ViewModels;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Settings window for configuring module connections and paths.
/// Provides UI for NaviPac TCP connection, file monitoring, and auto-save settings.
/// </summary>
public partial class SettingsWindow : MetroWindow
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    /// Initializes the settings window with theme and view model.
    /// </summary>
    public SettingsWindow()
    {
        // Load theme before InitializeComponent
        var themeUri = new System.Uri("/FathomOS.Modules.SurveyLogbook;component/Themes/DarkTheme.xaml", System.UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

        InitializeComponent();

        _viewModel = new SettingsViewModel();
        _viewModel.RequestClose += OnRequestClose;
        DataContext = _viewModel;
    }

    /// <summary>
    /// Initializes settings window with existing settings.
    /// </summary>
    /// <param name="settings">Current application settings to edit.</param>
    public SettingsWindow(Models.ApplicationSettings settings) : this()
    {
        _viewModel.LoadSettings(settings);
    }

    /// <summary>
    /// Gets the edited settings after dialog closes with OK result.
    /// </summary>
    public Models.ApplicationSettings? EditedSettings => _viewModel.GetSettings();

    /// <summary>
    /// Gets whether settings were saved (dialog result was OK).
    /// </summary>
    public bool SettingsSaved => DialogResult == true;

    private void OnRequestClose(bool? dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        _viewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
