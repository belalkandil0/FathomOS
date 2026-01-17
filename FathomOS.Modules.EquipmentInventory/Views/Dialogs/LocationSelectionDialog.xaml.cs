using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class LocationSelectionDialog : MetroWindow
{
    private readonly LocalDatabaseService _dbService;
    private List<Location> _allLocations = new();
    
    public Location? SelectedLocation { get; private set; }
    
    public LocationSelectionDialog(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        Loaded += async (s, e) => await LoadLocationsAsync();
    }
    
    private async Task LoadLocationsAsync()
    {
        try
        {
            _allLocations = await _dbService.GetLocationsAsync();
            LocationsList.ItemsSource = _allLocations.OrderBy(l => l.Name).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading locations: {ex.Message}");
        }
    }
    
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var search = SearchBox.Text?.ToLower() ?? "";
        
        if (string.IsNullOrWhiteSpace(search))
        {
            LocationsList.ItemsSource = _allLocations.OrderBy(l => l.Name).ToList();
        }
        else
        {
            LocationsList.ItemsSource = _allLocations
                .Where(l => l.Name.ToLower().Contains(search) ||
                           l.Type.ToString().ToLower().Contains(search))
                .OrderBy(l => l.Name)
                .ToList();
        }
    }
    
    private void LocationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = LocationsList.SelectedItem != null;
    }
    
    private void Select_Click(object sender, RoutedEventArgs e)
    {
        SelectedLocation = LocationsList.SelectedItem as Location;
        DialogResult = true;
        Close();
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
