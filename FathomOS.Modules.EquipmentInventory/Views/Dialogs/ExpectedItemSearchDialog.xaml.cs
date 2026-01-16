using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class ExpectedItemSearchDialog : MetroWindow
{
    private readonly ExpectedItemSearchViewModel _viewModel;
    
    public ExpectedItemSearchDialog(List<VerificationItemViewModel> expectedItems)
    {
        InitializeComponent();
        
        // Apply theme
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new ExpectedItemSearchViewModel(expectedItems);
        DataContext = _viewModel;
        
        Loaded += (s, e) => SearchTextBox.Focus();
    }
    
    public VerificationItemViewModel? SelectedItem => _viewModel.SelectedItem;
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void VerifyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedItem != null)
        {
            DialogResult = true;
            Close();
        }
    }
    
    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedItem != null)
        {
            DialogResult = true;
            Close();
        }
    }
}

public class ExpectedItemSearchViewModel : INotifyPropertyChanged
{
    private readonly List<VerificationItemViewModel> _allItems;
    private string _searchText = string.Empty;
    private VerificationItemViewModel? _selectedItem;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ExpectedItemSearchViewModel(List<VerificationItemViewModel> expectedItems)
    {
        _allItems = expectedItems;
        FilteredItems = new ObservableCollection<VerificationItemViewModel>(expectedItems);
    }
    
    public ObservableCollection<VerificationItemViewModel> FilteredItems { get; }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilterItems();
            }
        }
    }
    
    public VerificationItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
        }
    }
    
    public bool HasSelection => SelectedItem != null;
    public bool ShowEmptyState => FilteredItems.Count == 0;
    public int FilteredCount => FilteredItems.Count;
    
    private void FilterItems()
    {
        FilteredItems.Clear();
        
        var searchLower = SearchText.ToLowerInvariant();
        
        foreach (var item in _allItems)
        {
            if (string.IsNullOrWhiteSpace(SearchText) ||
                (item.Name?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (item.AssetNumber?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (item.UniqueId?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (item.SerialNumber?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                (item.Description?.ToLowerInvariant().Contains(searchLower) ?? false))
            {
                FilteredItems.Add(item);
            }
        }
        
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(ShowEmptyState));
    }
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
