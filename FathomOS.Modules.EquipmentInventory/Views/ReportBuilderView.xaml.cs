using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.ViewModels;

namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class ReportBuilderView : MetroWindow
{
    public ReportBuilderView()
    {
        var themeUri = new Uri("/FathomOS.Modules.EquipmentInventory;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        DataContext = new ReportBuilderViewModel();
    }
}
