using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;


namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

public class EquipmentEditorViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly QRCodeService _qrService;
    private readonly LabelPrintService _printService;
    private readonly ModuleSettings _settings;
    private readonly Equipment _equipment;
    private readonly bool _isNew;
    
    private string _statusMessage = string.Empty;
    private bool _isBusy;
    private bool? _dialogResult;
    private string _primaryPhotoPath = string.Empty;
    
    public EquipmentEditorViewModel(LocalDatabaseService dbService, Equipment? equipment = null)
    {
        _dbService = dbService;
        _settings = ModuleSettings.Load();
        _qrService = new QRCodeService(_settings);
        _printService = new LabelPrintService(_settings);
        _isNew = equipment == null;
        _equipment = equipment ?? new Equipment();
        
        Categories = new ObservableCollection<EquipmentCategory>();
        Types = new ObservableCollection<EquipmentType>();
        Locations = new ObservableCollection<Location>();
        Projects = new ObservableCollection<Project>();
        Suppliers = new ObservableCollection<Supplier>();
        
        InitializeCommands();
        _ = LoadLookupsAsync();
        
        // Auto-generate asset number for new equipment
        if (_isNew)
        {
            _ = InitializeNewEquipmentAsync();
        }
    }
    
    /// <summary>
    /// Initialize new equipment with auto-generated asset number
    /// UniqueId is generated when category is selected
    /// </summary>
    private async Task InitializeNewEquipmentAsync()
    {
        try
        {
            AssetNumber = await _dbService.GenerateAssetNumberAsync();
            OnPropertyChanged(nameof(AssetNumber));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to generate asset number: {ex.Message}";
        }
    }
    
    #region Properties
    
    public string WindowTitle => _isNew ? "New Equipment" : $"Edit: {_equipment.AssetNumber}";
    public bool IsNew => _isNew;
    
    public ObservableCollection<EquipmentCategory> Categories { get; }
    public ObservableCollection<EquipmentType> Types { get; }
    public ObservableCollection<Location> Locations { get; }
    public ObservableCollection<Project> Projects { get; }
    public ObservableCollection<Supplier> Suppliers { get; }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set 
        { 
            if (SetProperty(ref _isBusy, value))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    
    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }
    
    public string PrimaryPhotoPath
    {
        get => _primaryPhotoPath;
        set => SetProperty(ref _primaryPhotoPath, value);
    }
    
    // Equipment Properties
    public string AssetNumber
    {
        get => _equipment.AssetNumber;
        set { _equipment.AssetNumber = value; OnPropertyChanged(); }
    }
    
    public string Name
    {
        get => _equipment.Name;
        set 
        { 
            _equipment.Name = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(CanSave)); 
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
    }
    
    public string? Description
    {
        get => _equipment.Description;
        set { _equipment.Description = value; OnPropertyChanged(); }
    }
    
    public string? SapNumber
    {
        get => _equipment.SapNumber;
        set { _equipment.SapNumber = value; OnPropertyChanged(); }
    }
    
    public string? TechNumber
    {
        get => _equipment.TechNumber;
        set { _equipment.TechNumber = value; OnPropertyChanged(); }
    }
    
    public string? SerialNumber
    {
        get => _equipment.SerialNumber;
        set { _equipment.SerialNumber = value; OnPropertyChanged(); }
    }
    
    public string? UniqueId
    {
        get => _equipment.UniqueId;
        set { _equipment.UniqueId = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUniqueId)); }
    }
    
    public bool HasUniqueId => !string.IsNullOrEmpty(_equipment.UniqueId);
    
    // Generated label image (for preview)
    private byte[]? _labelImageBytes;
    public byte[]? LabelImageBytes
    {
        get => _labelImageBytes;
        set => SetProperty(ref _labelImageBytes, value);
    }
    
    // Barcode type selection
    private BarcodeType _selectedBarcodeType = BarcodeType.QRCode;
    public BarcodeType SelectedBarcodeType
    {
        get => _selectedBarcodeType;
        set => SetProperty(ref _selectedBarcodeType, value);
    }
    
    public IEnumerable<BarcodeType> BarcodeTypeValues => Enum.GetValues<BarcodeType>();
    
    public string? Manufacturer
    {
        get => _equipment.Manufacturer;
        set { _equipment.Manufacturer = value; OnPropertyChanged(); }
    }
    
    public string? Model
    {
        get => _equipment.Model;
        set { _equipment.Model = value; OnPropertyChanged(); }
    }
    
    public string? PartNumber
    {
        get => _equipment.PartNumber;
        set { _equipment.PartNumber = value; OnPropertyChanged(); }
    }
    
    public EquipmentCategory? SelectedCategory
    {
        get => Categories.FirstOrDefault(c => c.CategoryId == _equipment.CategoryId);
        set
        {
            _equipment.CategoryId = value?.CategoryId;
            _equipment.Category = value;
            OnPropertyChanged();
            _ = LoadTypesForCategoryAsync(value?.CategoryId);
            
            // Auto-set certification/calibration requirements
            if (value != null)
            {
                RequiresCertification = value.RequiresCertification;
                RequiresCalibration = value.RequiresCalibration;
                
                // AUTO-GENERATE UNIQUE ID for new equipment when category is selected
                if (_isNew && _settings.AutoGenerateUniqueId && string.IsNullOrEmpty(UniqueId))
                {
                    _ = AutoGenerateUniqueIdAsync(value);
                }
            }
        }
    }
    
    /// <summary>
    /// Auto-generate UniqueId based on category (called when category changes for new equipment)
    /// </summary>
    private async Task AutoGenerateUniqueIdAsync(EquipmentCategory category)
    {
        try
        {
            var categoryCode = category.Code ?? "EQP";
            UniqueId = await _dbService.GenerateUniqueIdAsync(_settings.OrganizationCode, categoryCode);
            
            // Update QR code to include unique ID
            if (!string.IsNullOrEmpty(AssetNumber))
            {
                _equipment.QrCode = QRCodeService.GenerateEquipmentQrCodeWithUniqueId(AssetNumber, UniqueId);
            }
            
            StatusMessage = $"Generated ID: {UniqueId}";
            
            // Auto-generate label preview
            GenerateLabel();
        }
        catch (Exception ex)
        {
            StatusMessage = $"ID generation failed: {ex.Message}";
        }
    }
    
    public EquipmentType? SelectedType
    {
        get => Types.FirstOrDefault(t => t.TypeId == _equipment.TypeId);
        set
        {
            _equipment.TypeId = value?.TypeId;
            _equipment.Type = value;
            OnPropertyChanged();
        }
    }
    
    public Location? SelectedLocation
    {
        get => Locations.FirstOrDefault(l => l.LocationId == _equipment.CurrentLocationId);
        set
        {
            _equipment.CurrentLocationId = value?.LocationId;
            _equipment.CurrentLocation = value;
            OnPropertyChanged();
        }
    }
    
    public Project? SelectedProject
    {
        get => Projects.FirstOrDefault(p => p.ProjectId == _equipment.CurrentProjectId);
        set
        {
            _equipment.CurrentProjectId = value?.ProjectId;
            _equipment.CurrentProject = value;
            OnPropertyChanged();
        }
    }
    
    public Supplier? SelectedSupplier
    {
        get => Suppliers.FirstOrDefault(s => s.SupplierId == _equipment.SupplierId);
        set
        {
            _equipment.SupplierId = value?.SupplierId;
            _equipment.Supplier = value;
            OnPropertyChanged();
        }
    }
    
    public EquipmentStatus Status
    {
        get => _equipment.Status;
        set { _equipment.Status = value; OnPropertyChanged(); }
    }
    
    public EquipmentCondition Condition
    {
        get => _equipment.Condition;
        set { _equipment.Condition = value; OnPropertyChanged(); }
    }
    
    public OwnershipType OwnershipType
    {
        get => _equipment.OwnershipType;
        set { _equipment.OwnershipType = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRented)); }
    }
    
    public bool IsRented => OwnershipType == OwnershipType.Rented;
    
    // Physical Properties
    public decimal? WeightKg
    {
        get => _equipment.WeightKg;
        set { _equipment.WeightKg = value; OnPropertyChanged(); }
    }
    
    public decimal? LengthCm
    {
        get => _equipment.LengthCm;
        set { _equipment.LengthCm = value; OnPropertyChanged(); }
    }
    
    public decimal? WidthCm
    {
        get => _equipment.WidthCm;
        set { _equipment.WidthCm = value; OnPropertyChanged(); }
    }
    
    public decimal? HeightCm
    {
        get => _equipment.HeightCm;
        set { _equipment.HeightCm = value; OnPropertyChanged(); }
    }
    
    // Packaging
    public string? PackagingType
    {
        get => _equipment.PackagingType;
        set { _equipment.PackagingType = value; OnPropertyChanged(); }
    }
    
    public decimal? PackagingWeightKg
    {
        get => _equipment.PackagingWeightKg;
        set { _equipment.PackagingWeightKg = value; OnPropertyChanged(); }
    }
    
    public decimal? PackagingLengthCm
    {
        get => _equipment.PackagingLengthCm;
        set { _equipment.PackagingLengthCm = value; OnPropertyChanged(); }
    }
    
    public decimal? PackagingWidthCm
    {
        get => _equipment.PackagingWidthCm;
        set { _equipment.PackagingWidthCm = value; OnPropertyChanged(); }
    }
    
    public decimal? PackagingHeightCm
    {
        get => _equipment.PackagingHeightCm;
        set { _equipment.PackagingHeightCm = value; OnPropertyChanged(); }
    }
    
    public string? PackagingDescription
    {
        get => _equipment.PackagingDescription;
        set { _equipment.PackagingDescription = value; OnPropertyChanged(); }
    }
    
    // Additional Consumable Properties
    public decimal? MaximumStockLevel
    {
        get => _equipment.MaximumStockLevel;
        set { _equipment.MaximumStockLevel = value; OnPropertyChanged(); }
    }
    
    public string? BatchNumber
    {
        get => _equipment.BatchNumber;
        set { _equipment.BatchNumber = value; OnPropertyChanged(); }
    }
    
    public string? LotNumber
    {
        get => _equipment.LotNumber;
        set { _equipment.LotNumber = value; OnPropertyChanged(); }
    }
    
    public DateTime? ExpiryDate
    {
        get => _equipment.ExpiryDate;
        set { _equipment.ExpiryDate = value; OnPropertyChanged(); }
    }
    
    // Service & Maintenance
    public DateTime? LastServiceDate
    {
        get => _equipment.LastServiceDate;
        set { _equipment.LastServiceDate = value; OnPropertyChanged(); }
    }
    
    public DateTime? NextServiceDate
    {
        get => _equipment.NextServiceDate;
        set { _equipment.NextServiceDate = value; OnPropertyChanged(); }
    }
    
    public int? ServiceIntervalDays
    {
        get => _equipment.ServiceIntervalDays;
        set { _equipment.ServiceIntervalDays = value; OnPropertyChanged(); }
    }
    
    public DateTime? LastInspectionDate
    {
        get => _equipment.LastInspectionDate;
        set { _equipment.LastInspectionDate = value; OnPropertyChanged(); }
    }
    
    // Depreciation
    public string? DepreciationMethod
    {
        get => _equipment.DepreciationMethod;
        set { _equipment.DepreciationMethod = value; OnPropertyChanged(); }
    }
    
    public int? UsefulLifeYears
    {
        get => _equipment.UsefulLifeYears;
        set { _equipment.UsefulLifeYears = value; OnPropertyChanged(); }
    }
    
    public decimal? ResidualValue
    {
        get => _equipment.ResidualValue;
        set { _equipment.ResidualValue = value; OnPropertyChanged(); }
    }
    
    public decimal? CurrentValue
    {
        get => _equipment.CurrentValue;
        set { _equipment.CurrentValue = value; OnPropertyChanged(); }
    }
    
    // Certification & Calibration
    public bool RequiresCertification
    {
        get => _equipment.RequiresCertification;
        set { _equipment.RequiresCertification = value; OnPropertyChanged(); }
    }
    
    public string? CertificationNumber
    {
        get => _equipment.CertificationNumber;
        set { _equipment.CertificationNumber = value; OnPropertyChanged(); }
    }
    
    public string? CertificationBody
    {
        get => _equipment.CertificationBody;
        set { _equipment.CertificationBody = value; OnPropertyChanged(); }
    }
    
    public DateTime? CertificationDate
    {
        get => _equipment.CertificationDate;
        set { _equipment.CertificationDate = value; OnPropertyChanged(); }
    }
    
    public DateTime? CertificationExpiryDate
    {
        get => _equipment.CertificationExpiryDate;
        set { _equipment.CertificationExpiryDate = value; OnPropertyChanged(); }
    }
    
    public bool RequiresCalibration
    {
        get => _equipment.RequiresCalibration;
        set { _equipment.RequiresCalibration = value; OnPropertyChanged(); }
    }
    
    public DateTime? LastCalibrationDate
    {
        get => _equipment.LastCalibrationDate;
        set { _equipment.LastCalibrationDate = value; OnPropertyChanged(); }
    }
    
    public DateTime? NextCalibrationDate
    {
        get => _equipment.NextCalibrationDate;
        set { _equipment.NextCalibrationDate = value; OnPropertyChanged(); }
    }
    
    public int? CalibrationIntervalDays
    {
        get => _equipment.CalibrationIntervalDays;
        set { _equipment.CalibrationIntervalDays = value; OnPropertyChanged(); }
    }
    
    // Procurement
    public DateTime? PurchaseDate
    {
        get => _equipment.PurchaseDate;
        set { _equipment.PurchaseDate = value; OnPropertyChanged(); }
    }
    
    public decimal? PurchasePrice
    {
        get => _equipment.PurchasePrice;
        set { _equipment.PurchasePrice = value; OnPropertyChanged(); }
    }
    
    public string? PurchaseCurrency
    {
        get => _equipment.PurchaseCurrency;
        set { _equipment.PurchaseCurrency = value; OnPropertyChanged(); }
    }
    
    public string? PurchaseOrderNumber
    {
        get => _equipment.PurchaseOrderNumber;
        set { _equipment.PurchaseOrderNumber = value; OnPropertyChanged(); }
    }
    
    public DateTime? WarrantyExpiryDate
    {
        get => _equipment.WarrantyExpiryDate;
        set { _equipment.WarrantyExpiryDate = value; OnPropertyChanged(); }
    }
    
    // Rental
    public DateTime? RentalStartDate
    {
        get => _equipment.RentalStartDate;
        set { _equipment.RentalStartDate = value; OnPropertyChanged(); }
    }
    
    public DateTime? RentalEndDate
    {
        get => _equipment.RentalEndDate;
        set { _equipment.RentalEndDate = value; OnPropertyChanged(); }
    }
    
    public decimal? RentalRate
    {
        get => _equipment.RentalRate;
        set { _equipment.RentalRate = value; OnPropertyChanged(); }
    }
    
    // Consumables
    public bool IsConsumable
    {
        get => _equipment.IsConsumable;
        set { _equipment.IsConsumable = value; OnPropertyChanged(); }
    }
    
    public decimal QuantityOnHand
    {
        get => _equipment.QuantityOnHand;
        set { _equipment.QuantityOnHand = value; OnPropertyChanged(); }
    }
    
    public string UnitOfMeasure
    {
        get => _equipment.UnitOfMeasure;
        set { _equipment.UnitOfMeasure = value; OnPropertyChanged(); }
    }
    
    public decimal? MinimumStockLevel
    {
        get => _equipment.MinimumStockLevel;
        set { _equipment.MinimumStockLevel = value; OnPropertyChanged(); }
    }
    
    public decimal? ReorderLevel
    {
        get => _equipment.ReorderLevel;
        set { _equipment.ReorderLevel = value; OnPropertyChanged(); }
    }
    
    // Notes
    public string? Notes
    {
        get => _equipment.Notes;
        set { _equipment.Notes = value; OnPropertyChanged(); }
    }
    
    public string? InternalNotes
    {
        get => _equipment.InternalNotes;
        set { _equipment.InternalNotes = value; OnPropertyChanged(); }
    }
    
    public bool CanSave => !string.IsNullOrWhiteSpace(Name);
    
    // Enum values for ComboBoxes
    public IEnumerable<EquipmentStatus> StatusValues => Enum.GetValues<EquipmentStatus>();
    public IEnumerable<EquipmentCondition> ConditionValues => Enum.GetValues<EquipmentCondition>();
    public IEnumerable<OwnershipType> OwnershipTypeValues => Enum.GetValues<OwnershipType>();
    
    #endregion
    
    #region Commands
    
    public ICommand SaveCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand BrowsePhotoCommand { get; private set; } = null!;
    public ICommand GenerateQrCommand { get; private set; } = null!;
    public ICommand GenerateUniqueIdCommand { get; private set; } = null!;
    public ICommand GenerateLabelCommand { get; private set; } = null!;
    public ICommand ViewLabelCommand { get; private set; } = null!;
    public ICommand SaveLabelCommand { get; private set; } = null!;
    public ICommand PrintLabelCommand { get; private set; } = null!;
    
    private void InitializeCommands()
    {
        SaveCommand = new AsyncRelayCommand(async _ => await SaveAsync(), _ => CanSave && !IsBusy);
        CancelCommand = new RelayCommand(_ => Cancel());
        BrowsePhotoCommand = new RelayCommand(_ => BrowsePhoto());
        GenerateQrCommand = new RelayCommand(_ => GenerateQr(), _ => !string.IsNullOrEmpty(AssetNumber));
        GenerateUniqueIdCommand = new AsyncRelayCommand(async _ => await GenerateUniqueIdAsync(), _ => SelectedCategory != null);
        GenerateLabelCommand = new RelayCommand(_ => GenerateLabel(), _ => !string.IsNullOrEmpty(AssetNumber));
        ViewLabelCommand = new RelayCommand(_ => ViewLabel(), _ => LabelImageBytes != null);
        SaveLabelCommand = new RelayCommand(_ => SaveLabel(), _ => LabelImageBytes != null);
        PrintLabelCommand = new RelayCommand(_ => PrintLabel(), _ => LabelImageBytes != null);
    }
    
    #endregion
    
    #region Methods
    
    private async Task LoadLookupsAsync()
    {
        try
        {
            var categories = await _dbService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);
            
            var locations = await _dbService.GetLocationsAsync();
            Locations.Clear();
            foreach (var l in locations) Locations.Add(l);
            
            var projects = await _dbService.GetProjectsAsync();
            Projects.Clear();
            foreach (var p in projects) Projects.Add(p);
            
            var suppliers = await _dbService.GetSuppliersAsync();
            Suppliers.Clear();
            foreach (var s in suppliers) Suppliers.Add(s);
            
            // Notify for initial selection binding
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(SelectedLocation));
            OnPropertyChanged(nameof(SelectedProject));
            OnPropertyChanged(nameof(SelectedSupplier));
            
            if (_equipment.CategoryId.HasValue)
            {
                await LoadTypesForCategoryAsync(_equipment.CategoryId);
                OnPropertyChanged(nameof(SelectedType));
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load data: {ex.Message}";
        }
    }
    
    private async Task LoadTypesForCategoryAsync(Guid? categoryId)
    {
        Types.Clear();
        if (!categoryId.HasValue) return;
        
        var types = await _dbService.GetTypesAsync(categoryId);
        foreach (var t in types) Types.Add(t);
    }
    
    private async Task SaveAsync()
    {
        if (!CanSave) return;
        
        IsBusy = true;
        StatusMessage = "Saving...";
        
        try
        {
            // Generate asset number if new
            if (_isNew && string.IsNullOrEmpty(_equipment.AssetNumber))
            {
                var categoryCode = SelectedCategory?.Code;
                _equipment.AssetNumber = await _dbService.GenerateAssetNumberAsync(categoryCode);
                _equipment.QrCode = $"foseq:{_equipment.AssetNumber}";
                OnPropertyChanged(nameof(AssetNumber));
            }
            
            await _dbService.SaveEquipmentAsync(_equipment);
            StatusMessage = "Saved successfully";
            DialogResult = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
            MessageBox.Show($"Failed to save equipment:\n\n{ex.Message}", "Save Error", 
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
    
    private void BrowsePhoto()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Photo",
            Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
        };
        
        if (dialog.ShowDialog() == true)
        {
            PrimaryPhotoPath = dialog.FileName;
            _equipment.PrimaryPhotoUrl = dialog.FileName;
        }
    }
    
    private void GenerateQr()
    {
        if (string.IsNullOrEmpty(AssetNumber)) return;
        
        var dialog = new SaveFileDialog
        {
            Title = "Save QR Code",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"QR_{AssetNumber}"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var qrContent = QRCodeService.GenerateEquipmentQrCode(AssetNumber);
                _qrService.SaveQrCodeToFile(qrContent, dialog.FileName, 15);
                StatusMessage = "QR code saved";
            }
            catch (Exception ex)
            {
                StatusMessage = $"QR generation failed: {ex.Message}";
            }
        }
    }
    
    /// <summary>
    /// Generate a unique ID for this equipment based on category
    /// </summary>
    private async Task GenerateUniqueIdAsync()
    {
        try
        {
            if (SelectedCategory == null)
            {
                StatusMessage = "Please select a category first";
                return;
            }
            
            var settings = ModuleSettings.Load();
            var categoryCode = SelectedCategory.Code ?? "EQP";
            
            // Generate unique ID using org code + category code + sequence
            UniqueId = await _dbService.GenerateUniqueIdAsync(settings.OrganizationCode, categoryCode);
            
            // Also update the QR code to include unique ID
            if (!string.IsNullOrEmpty(AssetNumber))
            {
                _equipment.QrCode = QRCodeService.GenerateEquipmentQrCodeWithUniqueId(AssetNumber, UniqueId);
            }
            
            StatusMessage = $"Generated Unique ID: {UniqueId}";
            
            // Auto-generate label preview
            GenerateLabel();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to generate ID: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Generate a label image preview with organization name, barcode/QR code, and unique ID
    /// </summary>
    private void GenerateLabel()
    {
        try
        {
            if (string.IsNullOrEmpty(AssetNumber))
            {
                StatusMessage = "Asset number required to generate label";
                return;
            }
            
            var settings = ModuleSettings.Load();
            var preset = LabelPresets.GetPreset(settings.LabelPreset);
            
            // Use unique ID or generate placeholder
            var displayId = !string.IsNullOrEmpty(UniqueId) ? UniqueId : AssetNumber;
            
            // Generate label based on selected barcode type
            if (SelectedBarcodeType == BarcodeType.QRCode)
            {
                // Generate QR content
                var qrContent = !string.IsNullOrEmpty(UniqueId) 
                    ? QRCodeService.GenerateEquipmentQrCodeWithUniqueId(AssetNumber, UniqueId)
                    : QRCodeService.GenerateEquipmentQrCode(AssetNumber);
                
                // Generate label with QR code
                LabelImageBytes = _qrService.GenerateLabelPng(displayId, qrContent, preset.Width, preset.Height);
            }
            else
            {
                // Use BarcodeService for Code128/Code39
                var barcodeService = new BarcodeService(_settings);
                LabelImageBytes = barcodeService.GenerateCombinedLabel(AssetNumber, UniqueId, SelectedBarcodeType, preset.Width, preset.Height);
            }
            
            StatusMessage = $"Label generated ({SelectedBarcodeType}) - ready to save or print";
            OnPropertyChanged(nameof(LabelImageBytes));
            OnPropertyChanged(nameof(HasLabelPreview));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Label generation failed: {ex.Message}";
        }
    }
    
    public bool HasLabelPreview => LabelImageBytes != null && LabelImageBytes.Length > 0;
    
    /// <summary>
    /// Save label image to file
    /// </summary>
    private void SaveLabel()
    {
        if (LabelImageBytes == null) return;
        
        var displayId = !string.IsNullOrEmpty(UniqueId) ? UniqueId : AssetNumber;
        
        var dialog = new SaveFileDialog
        {
            Title = "Save Equipment Label",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"Label_{displayId}"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                System.IO.File.WriteAllBytes(dialog.FileName, LabelImageBytes);
                StatusMessage = "Label saved successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save label: {ex.Message}";
            }
        }
    }
    
    /// <summary>
    /// View label in a full-size preview window
    /// </summary>
    private void ViewLabel()
    {
        if (LabelImageBytes == null)
        {
            GenerateLabel();
            if (LabelImageBytes == null) return;
        }
        
        try
        {
            var displayId = !string.IsNullOrEmpty(UniqueId) ? UniqueId : AssetNumber;
            
            // Create a simple preview window
            var window = new MahApps.Metro.Controls.MetroWindow
            {
                Title = $"Label Preview - {displayId}",
                Width = 500,
                Height = 400,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = System.Windows.Application.Current.MainWindow,
                Background = System.Windows.Media.Brushes.White,
                ResizeMode = System.Windows.ResizeMode.CanResize
            };
            
            var image = new System.Windows.Controls.Image
            {
                Source = ConvertBytesToImage(LabelImageBytes),
                Stretch = System.Windows.Media.Stretch.Uniform,
                Margin = new System.Windows.Thickness(20)
            };
            
            window.Content = image;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error viewing label: {ex.Message}";
        }
    }
    
    private System.Windows.Media.Imaging.BitmapImage ConvertBytesToImage(byte[] bytes)
    {
        using var stream = new System.IO.MemoryStream(bytes);
        var image = new System.Windows.Media.Imaging.BitmapImage();
        image.BeginInit();
        image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
    
    /// <summary>
    /// Print label using configured printer
    /// </summary>
    private void PrintLabel()
    {
        if (LabelImageBytes == null)
        {
            // Auto-generate if not already done
            GenerateLabel();
            if (LabelImageBytes == null)
            {
                StatusMessage = "Cannot generate label - asset number required";
                return;
            }
        }
        
        try
        {
            // Use the print service with configured printer
            if (_printService.PrintLabelWithDialog(LabelImageBytes))
            {
                StatusMessage = "Label sent to printer";
            }
            else
            {
                StatusMessage = "Print cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Print failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Quick print without dialog (uses configured settings)
    /// </summary>
    private void QuickPrintLabel()
    {
        if (LabelImageBytes == null)
        {
            GenerateLabel();
            if (LabelImageBytes == null) return;
        }
        
        try
        {
            if (_printService.PrintLabel(LabelImageBytes))
            {
                StatusMessage = "Label printed successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Print failed: {ex.Message}";
        }
    }
    
    public Equipment GetEquipment() => _equipment;
    
    #endregion
}
