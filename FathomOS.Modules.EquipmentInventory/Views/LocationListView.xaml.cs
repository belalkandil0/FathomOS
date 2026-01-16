using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.ViewModels;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class LocationListView : System.Windows.Controls.UserControl
{
    private readonly LocationListViewModel _viewModel;
    
    public LocationListView(LocalDatabaseService dbService)
    {
        InitializeComponent();
        _viewModel = new LocationListViewModel(dbService);
        DataContext = _viewModel;
    }
}

public class LocationListViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    
    private Location? _selectedLocation;
    public Location? SelectedLocation
    {
        get => _selectedLocation;
        set => SetProperty(ref _selectedLocation, value);
    }
    
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterLocations();
        }
    }
    
    public LocationListViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        
        AddLocationCommand = new RelayCommand(_ => AddLocation());
        EditLocationCommand = new RelayCommand(_ => EditLocation(), _ => SelectedLocation != null);
        DeleteLocationCommand = new RelayCommand(_ => DeleteLocation(), _ => SelectedLocation != null);
        RefreshCommand = new RelayCommand(_ => LoadLocations());
        
        LoadLocations();
    }
    
    public ObservableCollection<Location> Locations { get; } = new();
    public ObservableCollection<Location> FilteredLocations { get; } = new();
    
    public ICommand AddLocationCommand { get; }
    public ICommand EditLocationCommand { get; }
    public ICommand DeleteLocationCommand { get; }
    public ICommand RefreshCommand { get; }
    
    private async void LoadLocations()
    {
        try
        {
            var locations = await _dbService.GetAllLocationsAsync();
            Locations.Clear();
            FilteredLocations.Clear();
            foreach (var loc in locations.OrderBy(l => l.Name))
            {
                Locations.Add(loc);
                FilteredLocations.Add(loc);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading locations: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void FilterLocations()
    {
        FilteredLocations.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText) 
            ? Locations 
            : Locations.Where(l => 
                l.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                l.Code.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                
        foreach (var loc in filtered)
            FilteredLocations.Add(loc);
    }
    
    private async void AddLocation()
    {
        try
        {
            var dialog = new LocationEditorDialog(_dbService);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var location = dialog.GetLocation();
                if (location != null)
                {
                    await _dbService.AddLocationAsync(location);
                    
                    // Save user assignments
                    var userAssignments = dialog.GetUserAssignments();
                    if (userAssignments.Any())
                    {
                        await _dbService.UpdateLocationUsersAsync(location.LocationId, userAssignments);
                    }
                    
                    Locations.Add(location);
                    FilteredLocations.Add(location);
                    SelectedLocation = location;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding location: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void EditLocation()
    {
        if (SelectedLocation == null) return;
        
        try
        {
            var dialog = new LocationEditorDialog(_dbService, SelectedLocation);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var updated = dialog.GetLocation();
                if (updated != null)
                {
                    await _dbService.UpdateLocationAsync(updated);
                    
                    // Save user assignments
                    var userAssignments = dialog.GetUserAssignments();
                    await _dbService.UpdateLocationUsersAsync(updated.LocationId, userAssignments);
                    
                    LoadLocations();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error editing location: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void DeleteLocation()
    {
        if (SelectedLocation == null) return;
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SelectedLocation.Name}'?\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
            
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _dbService.DeleteLocationByIdAsync(SelectedLocation.LocationId);
                Locations.Remove(SelectedLocation);
                FilteredLocations.Remove(SelectedLocation);
                SelectedLocation = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting location: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
