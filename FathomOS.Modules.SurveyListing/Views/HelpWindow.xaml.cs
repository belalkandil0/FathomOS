using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace FathomOS.Modules.SurveyListing.Views;

public partial class HelpWindow : MetroWindow
{
    private bool _isInitialized = false;
    
    public HelpWindow()
    {
        InitializeComponent();
        _isInitialized = true;
    }
    
    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        // Don't process during initialization
        if (!_isInitialized) return;
        if (sender is not System.Windows.Controls.RadioButton rb) return;
        
        // Hide all sections (with null checks)
        if (SectionOverview != null) SectionOverview.Visibility = Visibility.Collapsed;
        if (SectionWizard != null) SectionWizard.Visibility = Visibility.Collapsed;
        if (SectionEditor != null) SectionEditor.Visibility = Visibility.Collapsed;
        if (SectionShortcuts != null) SectionShortcuts.Visibility = Visibility.Collapsed;
        if (SectionExport != null) SectionExport.Visibility = Visibility.Collapsed;
        if (SectionTroubleshooting != null) SectionTroubleshooting.Visibility = Visibility.Collapsed;
        
        // Show selected section
        switch (rb.Name)
        {
            case "NavOverview":
                if (SectionOverview != null) SectionOverview.Visibility = Visibility.Visible;
                break;
            case "NavWizard":
                if (SectionWizard != null) SectionWizard.Visibility = Visibility.Visible;
                break;
            case "NavEditor":
                if (SectionEditor != null) SectionEditor.Visibility = Visibility.Visible;
                break;
            case "NavShortcuts":
                if (SectionShortcuts != null) SectionShortcuts.Visibility = Visibility.Visible;
                break;
            case "NavExport":
                if (SectionExport != null) SectionExport.Visibility = Visibility.Visible;
                break;
            case "NavTroubleshooting":
                if (SectionTroubleshooting != null) SectionTroubleshooting.Visibility = Visibility.Visible;
                break;
        }
    }
}
