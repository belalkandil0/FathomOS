using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class ShippingDetailsDialog : MetroWindow
{
    public string? ShippingMethod { get; private set; }
    public string? CarrierName { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }
    
    public ShippingDetailsDialog()
    {
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        // Set default expected date to 7 days from now
        ExpectedDatePicker.SelectedDate = DateTime.Today.AddDays(7);
    }
    
    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        // Get values
        ShippingMethod = (ShippingMethodCombo.SelectedItem as ComboBoxItem)?.Content?.ToString();
        CarrierName = string.IsNullOrWhiteSpace(CarrierNameBox.Text) ? null : CarrierNameBox.Text.Trim();
        TrackingNumber = string.IsNullOrWhiteSpace(TrackingNumberBox.Text) ? null : TrackingNumberBox.Text.Trim();
        ExpectedDeliveryDate = ExpectedDatePicker.SelectedDate;
        
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
