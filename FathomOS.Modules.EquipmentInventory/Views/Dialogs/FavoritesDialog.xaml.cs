using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class FavoritesDialog : MetroWindow
{
    private readonly LocalDatabaseService _dbService;
    private readonly FavoritesService _favoritesService;
    
    public Equipment? SelectedEquipment { get; private set; }
    
    public FavoritesDialog(LocalDatabaseService dbService, FavoritesService favoritesService)
    {
        _dbService = dbService;
        _favoritesService = favoritesService;
        
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        Loaded += async (s, e) => await LoadDataAsync();
    }
    
    private async Task LoadDataAsync()
    {
        try
        {
            var quickAccess = await _favoritesService.GetQuickAccessAsync();
            
            // Update favorites
            FavoritesList.ItemsSource = quickAccess.Favorites;
            FavoritesCount.Text = $" ({quickAccess.Favorites.Count})";
            FavoritesEmpty.Visibility = quickAccess.Favorites.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            
            // Update recent
            RecentList.ItemsSource = quickAccess.Recent;
            RecentCount.Text = $" ({quickAccess.Recent.Count})";
            RecentEmpty.Visibility = quickAccess.Recent.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
    }
    
    private void FavoritesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RecentList.SelectedItem = null;
        UpdateSelectButton();
    }
    
    private void RecentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FavoritesList.SelectedItem = null;
        UpdateSelectButton();
    }
    
    private void UpdateSelectButton()
    {
        SelectButton.IsEnabled = FavoritesList.SelectedItem != null || RecentList.SelectedItem != null;
    }
    
    private void List_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox list && list.SelectedItem is Equipment equipment)
        {
            SelectedEquipment = equipment;
            DialogResult = true;
            Close();
        }
    }
    
    private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Equipment equipment)
        {
            _favoritesService.RemoveFromFavorites(equipment.EquipmentId);
            _ = LoadDataAsync();
        }
    }
    
    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        var activeTab = MainTabs.SelectedIndex;
        
        if (activeTab == 0) // Favorites
        {
            var result = MessageBox.Show("Clear all favorites?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _favoritesService.ClearFavorites();
                _ = LoadDataAsync();
            }
        }
        else // Recent
        {
            var result = MessageBox.Show("Clear recent history?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _favoritesService.ClearRecent();
                _ = LoadDataAsync();
            }
        }
    }
    
    private void Select_Click(object sender, RoutedEventArgs e)
    {
        SelectedEquipment = (FavoritesList.SelectedItem ?? RecentList.SelectedItem) as Equipment;
        
        if (SelectedEquipment != null)
        {
            DialogResult = true;
            Close();
        }
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
