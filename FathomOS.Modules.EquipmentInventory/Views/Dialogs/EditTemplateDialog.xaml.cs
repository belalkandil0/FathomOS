using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class EditTemplateDialog : MetroWindow
{
    public string TemplateName => TemplateNameBox.Text?.Trim() ?? "";
    public string? TemplateDescription => string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();
    
    public EditTemplateDialog(EquipmentTemplate template)
    {
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        TemplateNameBox.Text = template.TemplateName;
        DescriptionBox.Text = template.Description;
    }
    
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TemplateNameBox.Text))
        {
            MessageBox.Show("Please enter a template name.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            TemplateNameBox.Focus();
            return;
        }
        
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
