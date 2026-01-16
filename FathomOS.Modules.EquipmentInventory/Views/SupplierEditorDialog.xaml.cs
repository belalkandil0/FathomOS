using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class SupplierEditorDialog : MetroWindow
{
    private readonly SupplierEditorViewModel _viewModel;
    
    public SupplierEditorDialog(LocalDatabaseService dbService, Supplier? existingSupplier = null)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new SupplierEditorViewModel(dbService, existingSupplier);
        DataContext = _viewModel;
        
        Loaded += (s, e) => NameTextBox.Focus();
    }
    
    public Supplier? GetSupplier() => _viewModel.GetSupplier();
    
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (await _viewModel.ValidateAsync())
        {
            DialogResult = true;
            Close();
        }
    }
}

public class SupplierEditorViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private readonly Supplier? _existingSupplier;
    
    private string _name = "";
    private string _code = "";
    private string _contactPerson = "";
    private string _email = "";
    private string _phone = "";
    private string _address = "";
    private bool _isActive = true;
    private string _errorMessage = "";
    
    public SupplierEditorViewModel(LocalDatabaseService dbService, Supplier? existingSupplier = null)
    {
        _dbService = dbService;
        _existingSupplier = existingSupplier;
        
        if (existingSupplier != null)
        {
            _name = existingSupplier.Name;
            _code = existingSupplier.Code ?? "";
            _contactPerson = existingSupplier.ContactPerson ?? "";
            _email = existingSupplier.Email ?? "";
            _phone = existingSupplier.Phone ?? "";
            _address = existingSupplier.Address ?? "";
            _isActive = existingSupplier.IsActive;
        }
    }
    
    public string WindowTitle => _existingSupplier == null ? "New Supplier" : "Edit Supplier";
    public bool IsNew => _existingSupplier == null;
    
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanSave)); }
    }
    
    public string Code
    {
        get => _code;
        set { _code = value; OnPropertyChanged(); }
    }
    
    public string ContactPerson
    {
        get => _contactPerson;
        set { _contactPerson = value; OnPropertyChanged(); }
    }
    
    public string Email
    {
        get => _email;
        set { _email = value; OnPropertyChanged(); }
    }
    
    public string Phone
    {
        get => _phone;
        set { _phone = value; OnPropertyChanged(); }
    }
    
    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(); }
    }
    
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }
    
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool CanSave => !string.IsNullOrWhiteSpace(Name);
    
    public async Task<bool> ValidateAsync()
    {
        ErrorMessage = "";
        
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Supplier name is required.";
            return false;
        }
        
        // Check for duplicate code if specified
        if (!string.IsNullOrWhiteSpace(Code))
        {
            var suppliers = await _dbService.GetAllSuppliersAsync(true);
            var duplicate = suppliers.FirstOrDefault(s => 
                s.Code?.Equals(Code, StringComparison.OrdinalIgnoreCase) == true && 
                s.SupplierId != (_existingSupplier?.SupplierId ?? Guid.Empty));
                
            if (duplicate != null)
            {
                ErrorMessage = $"A supplier with code '{Code}' already exists.";
                return false;
            }
        }
        
        return true;
    }
    
    public Supplier? GetSupplier()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return null;
            
        var supplier = _existingSupplier ?? new Supplier();
        supplier.Name = Name.Trim();
        supplier.Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim().ToUpperInvariant();
        supplier.ContactPerson = string.IsNullOrWhiteSpace(ContactPerson) ? null : ContactPerson.Trim();
        supplier.Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
        supplier.Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim();
        supplier.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
        supplier.IsActive = IsActive;
        
        if (_existingSupplier == null)
        {
            supplier.SupplierId = Guid.NewGuid();
        }
        
        return supplier;
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
