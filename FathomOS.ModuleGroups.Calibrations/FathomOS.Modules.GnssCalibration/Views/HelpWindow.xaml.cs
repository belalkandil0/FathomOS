using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace FathomOS.Modules.GnssCalibration.Views;

/// <summary>
/// Help window with navigation panel.
/// </summary>
public partial class HelpWindow : MetroWindow
{
    public HelpWindow()
    {
        InitializeComponent();
        
        // Set initial selection AFTER InitializeComponent completes
        Loaded += HelpWindow_Loaded;
    }
    
    private void HelpWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Safety check - ensure all panels exist
        if (OverviewPanel == null || WorkflowPanel == null || 
            ChartsPanel == null || StatisticsPanel == null || ShortcutsPanel == null)
            return;
        
        // Now it's safe to set selection - all controls exist
        if (NavList?.Items.Count > 0)
        {
            NavList.SelectedIndex = 0;
        }
        
        // Show overview panel by default
        OverviewPanel.Visibility = Visibility.Visible;
        WorkflowPanel.Visibility = Visibility.Collapsed;
        ChartsPanel.Visibility = Visibility.Collapsed;
        StatisticsPanel.Visibility = Visibility.Collapsed;
        ShortcutsPanel.Visibility = Visibility.Collapsed;
    }
    
    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Safety check - don't process if controls aren't loaded yet
        if (OverviewPanel == null || WorkflowPanel == null || 
            ChartsPanel == null || StatisticsPanel == null || ShortcutsPanel == null)
            return;
        
        if (NavList.SelectedItem is ListBoxItem item && item.Tag is string tag)
        {
            // Hide all content panels
            OverviewPanel.Visibility = Visibility.Collapsed;
            WorkflowPanel.Visibility = Visibility.Collapsed;
            ChartsPanel.Visibility = Visibility.Collapsed;
            StatisticsPanel.Visibility = Visibility.Collapsed;
            ShortcutsPanel.Visibility = Visibility.Collapsed;
            
            // Show selected content
            switch (tag)
            {
                case "overview":
                    OverviewPanel.Visibility = Visibility.Visible;
                    break;
                case "workflow":
                    WorkflowPanel.Visibility = Visibility.Visible;
                    break;
                case "charts":
                    ChartsPanel.Visibility = Visibility.Visible;
                    break;
                case "statistics":
                    StatisticsPanel.Visibility = Visibility.Visible;
                    break;
                case "shortcuts":
                    ShortcutsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
