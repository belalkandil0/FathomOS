namespace FathomOS.Modules.TreeInclination.Views;

using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

public partial class HelpDialog : MetroWindow
{
    public HelpDialog()
    {
        // Load theme before InitializeComponent
        var themeUri = new Uri("/FathomOS.Modules.TreeInclination;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

        InitializeComponent();
        
        // Select first item
        NavigationList.SelectedIndex = 0;
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavigationList.SelectedItem is ListBoxItem item && item.Tag is string sectionName)
        {
            // Find the section by name and scroll to it
            var section = FindName(sectionName) as FrameworkElement;
            section?.BringIntoView();
        }
    }
}
