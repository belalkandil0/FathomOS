using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class RejectionReasonDialog : MetroWindow
{
    public string RejectionReason { get; private set; } = string.Empty;
    
    public RejectionReasonDialog()
    {
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
    }
    
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ReasonBox.Text))
        {
            MessageBox.Show("Please enter a reason for rejection.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            ReasonBox.Focus();
            return;
        }
        
        RejectionReason = ReasonBox.Text.Trim();
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
