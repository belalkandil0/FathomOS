// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Views/FieldConfigurationWindow.xaml.cs
// Purpose: Code-behind for NaviPac field configuration window
// Version: 9.0.0
// ============================================================================

using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.SurveyLogbook.ViewModels;

namespace FathomOS.Modules.SurveyLogbook.Views;

/// <summary>
/// Interaction logic for FieldConfigurationWindow.xaml.
/// Allows users to configure NaviPac UDO field mappings.
/// </summary>
public partial class FieldConfigurationWindow : MetroWindow
{
    private readonly FieldConfigurationViewModel _viewModel;
    
    /// <summary>
    /// Initializes the FieldConfigurationWindow with a ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel to bind to.</param>
    public FieldConfigurationWindow(FieldConfigurationViewModel viewModel)
    {
        // Load theme BEFORE InitializeComponent
        try
        {
            var themeUri = new Uri("/FathomOS.Modules.SurveyLogbook;component/Themes/DarkTheme.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
        }
        
        InitializeComponent();
        
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        
        // Subscribe to close request
        _viewModel.RequestClose += OnRequestClose;
    }
    
    /// <summary>
    /// Handles close request from ViewModel.
    /// </summary>
    private void OnRequestClose(bool saved)
    {
        DialogResult = saved;
        Close();
    }
    
    /// <summary>
    /// Cleanup when window closes.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _viewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
