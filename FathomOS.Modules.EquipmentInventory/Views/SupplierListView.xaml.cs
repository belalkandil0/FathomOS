using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.ViewModels;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class SupplierListView : System.Windows.Controls.UserControl
{
    private readonly SupplierListViewModel _viewModel;
    
    public SupplierListView(LocalDatabaseService dbService)
    {
        InitializeComponent();
        _viewModel = new SupplierListViewModel(dbService);
        DataContext = _viewModel;
    }
}

public class SupplierListViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    
    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }
    
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterSuppliers();
        }
    }
    
    public SupplierListViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        
        AddSupplierCommand = new RelayCommand(_ => AddSupplier());
        EditSupplierCommand = new RelayCommand(_ => EditSupplier(), _ => SelectedSupplier != null);
        DeleteSupplierCommand = new RelayCommand(_ => DeleteSupplier(), _ => SelectedSupplier != null);
        RefreshCommand = new RelayCommand(_ => LoadSuppliers());
        
        LoadSuppliers();
    }
    
    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<Supplier> FilteredSuppliers { get; } = new();
    
    public ICommand AddSupplierCommand { get; }
    public ICommand EditSupplierCommand { get; }
    public ICommand DeleteSupplierCommand { get; }
    public ICommand RefreshCommand { get; }
    
    private async void LoadSuppliers()
    {
        try
        {
            var suppliers = await _dbService.GetAllSuppliersAsync(true);
            Suppliers.Clear();
            FilteredSuppliers.Clear();
            foreach (var s in suppliers.OrderBy(s => s.Name))
            {
                Suppliers.Add(s);
                FilteredSuppliers.Add(s);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void FilterSuppliers()
    {
        FilteredSuppliers.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText) 
            ? Suppliers 
            : Suppliers.Where(s => 
                s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (s.Code?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.ContactPerson?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
                
        foreach (var s in filtered)
            FilteredSuppliers.Add(s);
    }
    
    private async void AddSupplier()
    {
        try
        {
            var dialog = new SupplierEditorDialog(_dbService);
            dialog.Owner = Application.Current.MainWindow;
            
            var settings = Services.ModuleSettings.Load();
            Services.ThemeService.Instance.ApplyTheme(dialog, settings.UseDarkTheme);
            
            if (dialog.ShowDialog() == true)
            {
                var supplier = dialog.GetSupplier();
                if (supplier != null)
                {
                    await _dbService.AddSupplierAsync(supplier);
                    Suppliers.Add(supplier);
                    FilteredSuppliers.Add(supplier);
                    SelectedSupplier = supplier;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding supplier: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void EditSupplier()
    {
        if (SelectedSupplier == null) return;
        
        try
        {
            var dialog = new SupplierEditorDialog(_dbService, SelectedSupplier);
            dialog.Owner = Application.Current.MainWindow;
            
            var settings = Services.ModuleSettings.Load();
            Services.ThemeService.Instance.ApplyTheme(dialog, settings.UseDarkTheme);
            
            if (dialog.ShowDialog() == true)
            {
                var updated = dialog.GetSupplier();
                if (updated != null)
                {
                    await _dbService.UpdateSupplierAsync(updated);
                    LoadSuppliers();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error editing supplier: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void DeleteSupplier()
    {
        if (SelectedSupplier == null) return;
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SelectedSupplier.Name}'?\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
            
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _dbService.DeleteSupplierByIdAsync(SelectedSupplier.SupplierId);
                Suppliers.Remove(SelectedSupplier);
                FilteredSuppliers.Remove(SelectedSupplier);
                SelectedSupplier = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting supplier: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
