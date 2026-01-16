using System;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Core;

namespace FathomOS.Modules.SurveyListing.Views;

public partial class AboutWindow : MetroWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        
        // Set version from centralized AppInfo
        TxtVersion.Text = AppInfo.VersionString;
        
        // Set build date from AppInfo
        TxtBuildDate.Text = AppInfo.BuildDate.ToString("MMMM yyyy");
    }
    
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
