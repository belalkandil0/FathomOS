using System;
using System.Windows;
using MahApps.Metro.Controls;

namespace FathomOS.Modules.VesselGyroCalibration.Views;

public partial class HelpWindow : MetroWindow
{
    public HelpWindow()
    {
        // Load theme
        var themeUri = new Uri("/FathomOS.Modules.VesselGyroCalibration;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Close();
    }
}
