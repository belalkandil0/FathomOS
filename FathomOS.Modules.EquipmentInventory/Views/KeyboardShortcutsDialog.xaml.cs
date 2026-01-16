using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class KeyboardShortcutsDialog : MetroWindow
{
    public KeyboardShortcutsDialog()
    {
        var themeUri = new Uri("/FathomOS.Modules.EquipmentInventory;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        var shortcutService = new KeyboardShortcutService();
        var categories = shortcutService.GetShortcutsByCategory();
        
        ShortcutCategories = categories.Select(c => new ShortcutCategoryViewModel
        {
            Category = c.Key,
            Shortcuts = c.Value
        }).ToList();
        
        DataContext = this;
    }
    
    public List<ShortcutCategoryViewModel> ShortcutCategories { get; }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

public class ShortcutCategoryViewModel
{
    public string Category { get; set; } = string.Empty;
    public List<ShortcutAction> Shortcuts { get; set; } = new();
}
