using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;


namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

public class ManifestWizardViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly ManifestType _manifestType;
    private readonly Manifest _manifest;
    private readonly bool _isEditMode;
    
    private int _currentStep = 1;
    private const int TotalSteps = 4;
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool? _dialogResult;
    private string _searchText = string.Empty;
    private Equipment? _selectedAvailableEquipment;
    private ManifestItem? _selectedManifestItem;
    
    /// <summary>
    /// Constructor for creating a new manifest
    /// </summary>
    public ManifestWizardViewModel(LocalDatabaseService dbService, ManifestType manifestType)
    {
        _dbService = dbService;
        _manifestType = manifestType;
        _manifest = new Manifest { Type = manifestType };
        _isEditMode = false;
        
        Locations = new ObservableCollection<Location>();
        Projects = new ObservableCollection<Project>();
        AvailableEquipment = new ObservableCollection<Equipment>();
        ManifestItems = new ObservableCollection<ManifestItem>();
        
        InitializeCommands();
        _ = LoadLookupsAsync();
    }
    
    /// <summary>
    /// Constructor for editing an existing manifest
    /// </summary>
    public ManifestWizardViewModel(LocalDatabaseService dbService, ManifestType manifestType, Manifest existingManifest)
    {
        _dbService = dbService;
        _manifestType = manifestType;
        _manifest = existingManifest;
        _isEditMode = true;
        
        Locations = new ObservableCollection<Location>();
        Projects = new ObservableCollection<Project>();
        AvailableEquipment = new ObservableCollection<Equipment>();
        ManifestItems = new ObservableCollection<ManifestItem>();
        
        // Pre-populate items from existing manifest
        if (existingManifest.Items != null)
        {
            foreach (var item in existingManifest.Items)
            {
                ManifestItems.Add(item);
            }
        }
        
        InitializeCommands();
        _ = LoadLookupsAsync();
    }
    
    #region Properties
    
    public bool IsEditMode => _isEditMode;
    
    public string WindowTitle => _isEditMode 
        ? $"Edit Manifest - {_manifest.ManifestNumber}"
        : (_manifestType == ManifestType.Outward 
            ? "Create Outward Manifest" 
            : "Create Inward Manifest");
    
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            SetProperty(ref _currentStep, value);
            OnPropertyChanged(nameof(StepTitle));
            OnPropertyChanged(nameof(StepDescription));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(IsStep1));
            OnPropertyChanged(nameof(IsStep2));
            OnPropertyChanged(nameof(IsStep3));
            OnPropertyChanged(nameof(IsStep4));
            OnPropertyChanged(nameof(NextButtonText));
        }
    }
    
    public string StepTitle => CurrentStep switch
    {
        1 => "Step 1: Locations",
        2 => "Step 2: Select Equipment",
        3 => "Step 3: Contact Details",
        4 => _isEditMode ? "Step 4: Review & Save" : "Step 4: Review & Submit",
        _ => "Manifest Wizard"
    };
    
    public string StepDescription => CurrentStep switch
    {
        1 => "Select the source and destination locations",
        2 => "Add equipment items to this manifest",
        3 => "Enter contact information for sender and receiver",
        4 => _isEditMode ? "Review the changes and save" : "Review the manifest and submit",
        _ => ""
    };
    
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    
    public bool CanGoBack => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < TotalSteps && ValidateCurrentStep();
    public string NextButtonText => CurrentStep == TotalSteps ? (_isEditMode ? "Save" : "Submit") : "Next →";
    
    public ObservableCollection<Location> Locations { get; }
    public ObservableCollection<Project> Projects { get; }
    public ObservableCollection<Equipment> AvailableEquipment { get; }
    public ObservableCollection<ManifestItem> ManifestItems { get; }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _ = SearchEquipmentAsync();
        }
    }
    
    public Equipment? SelectedAvailableEquipment
    {
        get => _selectedAvailableEquipment;
        set => SetProperty(ref _selectedAvailableEquipment, value);
    }
    
    public ManifestItem? SelectedManifestItem
    {
        get => _selectedManifestItem;
        set => SetProperty(ref _selectedManifestItem, value);
    }
    
    // Step 1: Locations
    public Location? FromLocation
    {
        get => Locations.FirstOrDefault(l => l.LocationId == _manifest.FromLocationId);
        set
        {
            _manifest.FromLocationId = value?.LocationId;
            _manifest.FromLocation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGoNext));
            _ = LoadEquipmentForLocationAsync();
        }
    }
    
    public Location? ToLocation
    {
        get => Locations.FirstOrDefault(l => l.LocationId == _manifest.ToLocationId);
        set
        {
            _manifest.ToLocationId = value?.LocationId;
            _manifest.ToLocation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanGoNext));
        }
    }
    
    public Project? SelectedProject
    {
        get => Projects.FirstOrDefault(p => p.ProjectId == _manifest.ProjectId);
        set
        {
            _manifest.ProjectId = value?.ProjectId;
            _manifest.Project = value;
            OnPropertyChanged();
        }
    }
    
    public DateTime? ExpectedDeliveryDate
    {
        get => _manifest.ExpectedDeliveryDate;
        set { _manifest.ExpectedDeliveryDate = value; OnPropertyChanged(); }
    }
    
    public string? ShippingMethod
    {
        get => _manifest.ShippingMethod;
        set { _manifest.ShippingMethod = value; OnPropertyChanged(); }
    }
    
    // Step 3: Contacts
    public string? FromContactName
    {
        get => _manifest.FromContactName;
        set { _manifest.FromContactName = value; OnPropertyChanged(); }
    }
    
    public string? FromContactPhone
    {
        get => _manifest.FromContactPhone;
        set { _manifest.FromContactPhone = value; OnPropertyChanged(); }
    }
    
    public string? FromContactEmail
    {
        get => _manifest.FromContactEmail;
        set { _manifest.FromContactEmail = value; OnPropertyChanged(); }
    }
    
    public string? ToContactName
    {
        get => _manifest.ToContactName;
        set { _manifest.ToContactName = value; OnPropertyChanged(); }
    }
    
    public string? ToContactPhone
    {
        get => _manifest.ToContactPhone;
        set { _manifest.ToContactPhone = value; OnPropertyChanged(); }
    }
    
    public string? ToContactEmail
    {
        get => _manifest.ToContactEmail;
        set { _manifest.ToContactEmail = value; OnPropertyChanged(); }
    }
    
    public string? Notes
    {
        get => _manifest.Notes;
        set { _manifest.Notes = value; OnPropertyChanged(); }
    }
    
    // Step 4: Summary
    public int TotalItems => ManifestItems.Count;
    public decimal? TotalWeight => ManifestItems.Sum(i => (i.Weight ?? 0) * i.Quantity);
    public string FromLocationName => FromLocation?.Name ?? "-";
    public string ToLocationName => ToLocation?.Name ?? "-";
    
    #endregion
    
    #region Commands
    
    public ICommand NextCommand { get; private set; } = null!;
    public ICommand BackCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand AddEquipmentCommand { get; private set; } = null!;
    public ICommand RemoveEquipmentCommand { get; private set; } = null!;
    public ICommand AddAllEquipmentCommand { get; private set; } = null!;
    public ICommand AddUnregisteredItemCommand { get; private set; } = null!;
    
    private void InitializeCommands()
    {
        NextCommand = new AsyncRelayCommand(async _ => await NextStepAsync());
        BackCommand = new RelayCommand(_ => PreviousStep(), _ => CanGoBack);
        CancelCommand = new RelayCommand(_ => Cancel());
        AddEquipmentCommand = new RelayCommand(_ => AddEquipment(), _ => SelectedAvailableEquipment != null);
        RemoveEquipmentCommand = new RelayCommand(_ => RemoveEquipment(), _ => SelectedManifestItem != null);
        AddAllEquipmentCommand = new RelayCommand(_ => AddAllEquipment(), _ => AvailableEquipment.Any());
        AddUnregisteredItemCommand = new RelayCommand(_ => AddUnregisteredItem());
    }
    
    #endregion
    
    #region Methods
    
    private async Task LoadLookupsAsync()
    {
        try
        {
            var locations = await _dbService.GetLocationsAsync();
            Locations.Clear();
            foreach (var l in locations) Locations.Add(l);
            
            var projects = await _dbService.GetProjectsAsync();
            Projects.Clear();
            foreach (var p in projects) Projects.Add(p);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load data: {ex.Message}";
        }
    }
    
    private async Task LoadEquipmentForLocationAsync()
    {
        if (FromLocation == null) return;
        
        try
        {
            var equipment = await _dbService.GetEquipmentAsync(
                locationId: FromLocation.LocationId,
                status: EquipmentStatus.Available);
            
            AvailableEquipment.Clear();
            foreach (var e in equipment)
            {
                // Don't show equipment already in manifest
                if (!ManifestItems.Any(mi => mi.EquipmentId == e.EquipmentId))
                    AvailableEquipment.Add(e);
            }
            
            StatusMessage = $"Found {AvailableEquipment.Count} available items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load equipment: {ex.Message}";
        }
    }
    
    private async Task SearchEquipmentAsync()
    {
        if (FromLocation == null) return;
        
        try
        {
            var equipment = await _dbService.GetEquipmentAsync(
                locationId: FromLocation.LocationId,
                status: EquipmentStatus.Available,
                search: SearchText);
            
            AvailableEquipment.Clear();
            foreach (var e in equipment)
            {
                if (!ManifestItems.Any(mi => mi.EquipmentId == e.EquipmentId))
                    AvailableEquipment.Add(e);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
    }
    
    private void AddEquipment()
    {
        if (SelectedAvailableEquipment == null) return;
        
        var item = new ManifestItem
        {
            ManifestId = _manifest.ManifestId,
            EquipmentId = SelectedAvailableEquipment.EquipmentId,
            Equipment = SelectedAvailableEquipment,
            AssetNumber = SelectedAvailableEquipment.AssetNumber,
            Name = SelectedAvailableEquipment.Name,
            Description = SelectedAvailableEquipment.Description,
            SerialNumber = SelectedAvailableEquipment.SerialNumber,
            Quantity = SelectedAvailableEquipment.IsConsumable ? 1 : 1,
            Weight = SelectedAvailableEquipment.WeightKg,
            ConditionAtSend = SelectedAvailableEquipment.Condition.ToString()
        };
        
        ManifestItems.Add(item);
        AvailableEquipment.Remove(SelectedAvailableEquipment);
        SelectedAvailableEquipment = null;
        
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalWeight));
        OnPropertyChanged(nameof(CanGoNext));
    }
    
    private void AddAllEquipment()
    {
        var items = AvailableEquipment.ToList();
        foreach (var eq in items)
        {
            var item = new ManifestItem
            {
                ManifestId = _manifest.ManifestId,
                EquipmentId = eq.EquipmentId,
                Equipment = eq,
                AssetNumber = eq.AssetNumber,
                Name = eq.Name,
                Description = eq.Description,
                SerialNumber = eq.SerialNumber,
                Quantity = 1,
                Weight = eq.WeightKg,
                ConditionAtSend = eq.Condition.ToString()
            };
            ManifestItems.Add(item);
        }
        AvailableEquipment.Clear();
        
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalWeight));
        OnPropertyChanged(nameof(CanGoNext));
    }
    
    private void RemoveEquipment()
    {
        if (SelectedManifestItem?.Equipment == null) return;
        
        AvailableEquipment.Add(SelectedManifestItem.Equipment);
        ManifestItems.Remove(SelectedManifestItem);
        SelectedManifestItem = null;
        
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalWeight));
        OnPropertyChanged(nameof(CanGoNext));
    }
    
    /// <summary>
    /// Add an unregistered item (not in system) to the manifest.
    /// This allows shipping items that cannot be found in the equipment database.
    /// </summary>
    private void AddUnregisteredItem()
    {
        try
        {
            // Open the AddUnregisteredItemDialog
            var dialog = new Views.Dialogs.AddUnregisteredItemDialog(_dbService, FromLocation?.LocationId);
            
            if (dialog.ShowDialog() == true && dialog.CreatedItem != null)
            {
                var unregistered = dialog.CreatedItem;
                
                // Create manifest item from unregistered item
                var item = new ManifestItem
                {
                    ManifestId = _manifest.ManifestId,
                    EquipmentId = null, // No equipment reference
                    UnregisteredItemId = unregistered.UnregisteredItemId,
                    AssetNumber = null,
                    Name = unregistered.Name,
                    Description = unregistered.Description,
                    SerialNumber = unregistered.SerialNumber,
                    Quantity = unregistered.Quantity,
                    IsManuallyAdded = true,
                    ConditionAtSend = "New"
                };
                
                ManifestItems.Add(item);
                
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalWeight));
                OnPropertyChanged(nameof(CanGoNext));
                
                StatusMessage = $"Added: {unregistered.Name}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add item: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private bool ValidateCurrentStep()
    {
        return CurrentStep switch
        {
            1 => FromLocation != null && ToLocation != null && FromLocation.LocationId != ToLocation.LocationId,
            2 => ManifestItems.Any(),
            3 => true,
            4 => true,
            _ => false
        };
    }
    
    private async Task NextStepAsync()
    {
        if (CurrentStep < TotalSteps)
        {
            if (!ValidateCurrentStep())
            {
                StatusMessage = GetValidationMessage();
                return;
            }
            CurrentStep++;
        }
        else
        {
            await SubmitAsync();
        }
    }
    
    private string GetValidationMessage()
    {
        return CurrentStep switch
        {
            1 => "Please select both source and destination locations",
            2 => "Please add at least one item to the manifest",
            _ => "Please complete all required fields"
        };
    }
    
    private void PreviousStep()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }
    
    private async Task SubmitAsync()
    {
        IsBusy = true;
        StatusMessage = _isEditMode ? "Saving changes..." : "Creating manifest...";
        
        try
        {
            // Update manifest items and totals
            _manifest.Items = ManifestItems.ToList();
            _manifest.TotalItems = ManifestItems.Count;
            _manifest.TotalWeight = TotalWeight;
            
            if (_isEditMode)
            {
                // Update existing manifest (don't change status)
                _manifest.UpdatedAt = DateTime.UtcNow;
                await _dbService.UpdateManifestAsync(_manifest);
                StatusMessage = "Manifest updated successfully";
            }
            else
            {
                // Create new manifest as draft
                _manifest.Status = ManifestStatus.Draft;
                await _dbService.SaveManifestAsync(_manifest);
                StatusMessage = "Manifest created successfully";
            }
            
            DialogResult = true;
        }
        catch (Exception ex)
        {
            var action = _isEditMode ? "update" : "create";
            StatusMessage = $"Failed to {action} manifest: {ex.Message}";
            MessageBox.Show($"Failed to {action} manifest:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void Cancel()
    {
        DialogResult = false;
    }
    
    public Manifest? GetManifest()
    {
        return DialogResult == true ? _manifest : null;
    }
    
    #endregion
}
