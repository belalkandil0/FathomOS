using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class DuplicateEquipmentDialog : MetroWindow
{
    private readonly Equipment _sourceEquipment;
    
    public int CopyCount => (int)(CopyCountBox.Value ?? 1);
    public bool CopySerialNumber => CopySerialCheck.IsChecked ?? false;
    public bool CopyPurchaseInfo => CopyPurchaseCheck.IsChecked ?? false;
    public bool CopyPhotos => CopyPhotosCheck.IsChecked ?? false;
    public bool CopyDocuments => CopyDocsCheck.IsChecked ?? false;
    
    public DuplicateEquipmentDialog(Equipment sourceEquipment)
    {
        _sourceEquipment = sourceEquipment;
        
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        SourceName.Text = sourceEquipment.Name;
        SourceAsset.Text = sourceEquipment.AssetNumber;
    }
    
    private void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
