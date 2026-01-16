using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

/// <summary>
/// ViewModel for adding unregistered items that are not in the equipment system.
/// Used during both outward manifest creation and inward verification.
/// </summary>
public class AddUnregisteredItemViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly Guid? _locationId;
    
    private bool _isEquipment = true;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private decimal _quantity = 1;
    private string _unitOfMeasure = "Each";
    private string _serialNumber = string.Empty;
    private string _manufacturer = string.Empty;
    private string _model = string.Empty;
    private string _partNumber = string.Empty;
    private EquipmentCategory? _selectedCategory;
    private EquipmentType? _selectedType;
    private string _statusMessage = string.Empty;
    private bool? _dialogResult;
    private int _photoCount;
    private List<string> _photoUrls = new();
    
    public AddUnregisteredItemViewModel(LocalDatabaseService dbService, Guid? locationId = null)
    {
        _dbService = dbService;
        _locationId = locationId;
        
        Categories = new ObservableCollection<EquipmentCategory>();
        Types = new ObservableCollection<EquipmentType>();
        
        InitializeCommands();
        _ = LoadLookupsAsync();
    }
    
    #region Properties
    
    public bool IsEquipment
    {
        get => _isEquipment;
        set
        {
            if (SetProperty(ref _isEquipment, value))
            {
                OnPropertyChanged(nameof(IsConsumable));
            }
        }
    }
    
    public bool IsConsumable
    {
        get => !_isEquipment;
        set => IsEquipment = !value;
    }
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
    
    public decimal Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, Math.Max(1, value));
    }
    
    public string UnitOfMeasure
    {
        get => _unitOfMeasure;
        set => SetProperty(ref _unitOfMeasure, value);
    }
    
    public List<string> UnitOptions => new()
    {
        "Each", "Set", "Box", "Kit", "Pair", "Meter", "Foot", "Roll", "Liter", "Gallon", "Kg", "Lb"
    };
    
    public string SerialNumber
    {
        get => _serialNumber;
        set => SetProperty(ref _serialNumber, value);
    }
    
    public string Manufacturer
    {
        get => _manufacturer;
        set => SetProperty(ref _manufacturer, value);
    }
    
    public string Model
    {
        get => _model;
        set => SetProperty(ref _model, value);
    }
    
    public string PartNumber
    {
        get => _partNumber;
        set => SetProperty(ref _partNumber, value);
    }
    
    public ObservableCollection<EquipmentCategory> Categories { get; }
    
    public ObservableCollection<EquipmentType> Types { get; }
    
    public EquipmentCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _ = LoadTypesForCategoryAsync(value?.CategoryId);
            }
        }
    }
    
    public EquipmentType? SelectedType
    {
        get => _selectedType;
        set => SetProperty(ref _selectedType, value);
    }
    
    public int PhotoCount
    {
        get => _photoCount;
        set => SetProperty(ref _photoCount, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }
    
    /// <summary>
    /// The created item (available after successful save)
    /// </summary>
    public UnregisteredItem? CreatedItem { get; private set; }
    
    #endregion
    
    #region Commands
    
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand AddPhotoCommand { get; private set; } = null!;
    
    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), 
            _ => !string.IsNullOrWhiteSpace(Name));
        CancelCommand = new RelayCommand(_ => Cancel());
        AddPhotoCommand = new RelayCommand(_ => AddPhoto());
    }
    
    #endregion
    
    #region Methods
    
    private async Task LoadLookupsAsync()
    {
        try
        {
            var categories = await _dbService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in categories.OrderBy(c => c.Name))
            {
                Categories.Add(cat);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading categories: {ex.Message}";
        }
    }
    
    private async Task LoadTypesForCategoryAsync(Guid? categoryId)
    {
        Types.Clear();
        _selectedType = null;
        OnPropertyChanged(nameof(SelectedType));
        
        if (!categoryId.HasValue) return;
        
        try
        {
            var types = await _dbService.GetTypesAsync(categoryId.Value);
            foreach (var type in types.OrderBy(t => t.Name))
            {
                Types.Add(type);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading types: {ex.Message}";
        }
    }
    
    private void AddPhoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Photo",
            Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All Files (*.*)|*.*",
            Multiselect = true
        };
        
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                // In a real implementation, upload to server or store locally
                _photoUrls.Add(file);
            }
            PhotoCount = _photoUrls.Count;
        }
    }
    
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Name is required";
            return;
        }
        
        try
        {
            // Create unregistered item
            var item = new UnregisteredItem
            {
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? null : SerialNumber.Trim(),
                Manufacturer = string.IsNullOrWhiteSpace(Manufacturer) ? null : Manufacturer.Trim(),
                Model = string.IsNullOrWhiteSpace(Model) ? null : Model.Trim(),
                PartNumber = string.IsNullOrWhiteSpace(PartNumber) ? null : PartNumber.Trim(),
                Quantity = Quantity,
                UnitOfMeasure = UnitOfMeasure,
                SuggestedCategoryId = SelectedCategory?.CategoryId,
                SuggestedTypeId = SelectedType?.TypeId,
                IsConsumable = IsConsumable,
                CurrentLocationId = _locationId,
                Status = UnregisteredItemStatus.PendingReview,
                PhotoUrls = _photoUrls.Count > 0 ? string.Join(",", _photoUrls) : null
            };
            
            await _dbService.AddUnregisteredItemAsync(item);
            
            CreatedItem = item;
            StatusMessage = "Item added successfully";
            DialogResult = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
            MessageBox.Show($"Failed to save item:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Cancel()
    {
        DialogResult = false;
    }
    
    #endregion
}
