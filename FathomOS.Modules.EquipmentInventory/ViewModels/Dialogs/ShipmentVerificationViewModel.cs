using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

/// <summary>
/// ViewModel for shipment verification workflow.
/// Handles scanning shipment QR, verifying items, and completing inward manifest.
/// </summary>
public class ShipmentVerificationViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly AuthenticationService _authService;
    private readonly QRCodeService _qrService;
    private readonly ModuleSettings _settings;
    
    private Manifest? _outwardManifest;
    private Manifest? _inwardManifest;
    private string _shipmentQrCode = string.Empty;
    private string _itemScanInput = string.Empty;
    private string _statusMessage = string.Empty;
    private string _lastScanResult = string.Empty;
    private bool _isBusy;
    private bool? _dialogResult;
    
    // Workflow state
    private VerificationWorkflowState _workflowState = VerificationWorkflowState.ScanShipment;
    
    // Pending manifests for current location
    private Guid? _currentUserLocationId;
    
    public ShipmentVerificationViewModel(LocalDatabaseService dbService, AuthenticationService authService)
    {
        _dbService = dbService;
        _authService = authService;
        _settings = ModuleSettings.Load();
        _qrService = new QRCodeService(_settings);
        
        ExpectedItems = new ObservableCollection<VerificationItemViewModel>();
        ExtraItems = new ObservableCollection<VerificationItemViewModel>();
        PendingManifests = new ObservableCollection<Manifest>();
        
        InitializeCommands();
        
        // Load pending manifests on startup
        _ = LoadPendingManifestsAsync();
    }
    
    /// <summary>
    /// Initialize with a specific user location
    /// </summary>
    public ShipmentVerificationViewModel(LocalDatabaseService dbService, AuthenticationService authService, Guid userLocationId)
        : this(dbService, authService)
    {
        _currentUserLocationId = userLocationId;
        _ = LoadPendingManifestsAsync();
    }
    
    /// <summary>
    /// Initialize with a known manifest (e.g., opened from list)
    /// </summary>
    public ShipmentVerificationViewModel(LocalDatabaseService dbService, AuthenticationService authService, Manifest outwardManifest)
        : this(dbService, authService)
    {
        _outwardManifest = outwardManifest;
        _shipmentQrCode = outwardManifest.QrCode ?? outwardManifest.ManifestNumber;
        _ = LoadManifestDataAsync();
    }
    
    #region Properties
    
    public VerificationWorkflowState WorkflowState
    {
        get => _workflowState;
        set
        {
            if (SetProperty(ref _workflowState, value))
            {
                OnPropertyChanged(nameof(IsScanShipmentState));
                OnPropertyChanged(nameof(IsVerifyingState));
                OnPropertyChanged(nameof(IsCompleteState));
                OnPropertyChanged(nameof(ShowShipmentScanner));
                OnPropertyChanged(nameof(ShowItemScanner));
            }
        }
    }
    
    public bool IsScanShipmentState => WorkflowState == VerificationWorkflowState.ScanShipment;
    public bool IsVerifyingState => WorkflowState == VerificationWorkflowState.Verifying;
    public bool IsCompleteState => WorkflowState == VerificationWorkflowState.Complete;
    public bool ShowShipmentScanner => WorkflowState == VerificationWorkflowState.ScanShipment;
    public bool ShowItemScanner => WorkflowState == VerificationWorkflowState.Verifying;
    
    public string ShipmentQrCode
    {
        get => _shipmentQrCode;
        set => SetProperty(ref _shipmentQrCode, value);
    }
    
    public string ItemScanInput
    {
        get => _itemScanInput;
        set => SetProperty(ref _itemScanInput, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public string LastScanResult
    {
        get => _lastScanResult;
        set => SetProperty(ref _lastScanResult, value);
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
    
    // Manifest info
    public Manifest? OutwardManifest => _outwardManifest;
    public Manifest? InwardManifest => _inwardManifest;
    
    public string ManifestNumber => _outwardManifest?.ManifestNumber ?? "Not loaded";
    public string FromLocation => _outwardManifest?.FromLocation?.Name ?? "Unknown";
    public string ToLocation => _outwardManifest?.ToLocation?.Name ?? "Unknown";
    public string ShippedDate => _outwardManifest?.ShippedDate?.ToString("dd MMM yyyy") ?? "-";
    
    // Item collections
    public ObservableCollection<VerificationItemViewModel> ExpectedItems { get; }
    public ObservableCollection<VerificationItemViewModel> ExtraItems { get; }
    
    // Pending inbound manifests for current location
    public ObservableCollection<Manifest> PendingManifests { get; }
    
    private Manifest? _selectedPendingManifest;
    public Manifest? SelectedPendingManifest
    {
        get => _selectedPendingManifest;
        set
        {
            if (SetProperty(ref _selectedPendingManifest, value) && value != null)
            {
                // Auto-load when selected
                ShipmentQrCode = value.ManifestNumber;
            }
        }
    }
    
    public bool HasPendingManifests => PendingManifests.Count > 0;
    public int PendingManifestsCount => PendingManifests.Count;
    
    // Progress tracking
    public int TotalExpectedItems => ExpectedItems.Count;
    public int VerifiedCount => ExpectedItems.Count(i => i.IsVerified);
    public int MissingCount => ExpectedItems.Count(i => i.Status == VerificationStatus.Missing);
    public int DamagedCount => ExpectedItems.Count(i => i.Status == VerificationStatus.Damaged);
    public int ExtraCount => ExtraItems.Count;
    public int PendingCount => ExpectedItems.Count(i => i.Status == VerificationStatus.Pending);
    
    public string ProgressText => $"{VerifiedCount} / {TotalExpectedItems} verified";
    public double ProgressPercentage => TotalExpectedItems > 0 
        ? (double)VerifiedCount / TotalExpectedItems * 100 
        : 0;
    
    public bool HasDiscrepancies => MissingCount > 0 || DamagedCount > 0 || ExtraCount > 0;
    public bool CanComplete => VerifiedCount > 0 || MissingCount > 0;  // At least some action taken
    
    public string DiscrepancySummary
    {
        get
        {
            var parts = new List<string>();
            if (MissingCount > 0) parts.Add($"{MissingCount} missing");
            if (DamagedCount > 0) parts.Add($"{DamagedCount} damaged");
            if (ExtraCount > 0) parts.Add($"{ExtraCount} extra");
            return parts.Count > 0 ? string.Join(", ", parts) : "None";
        }
    }
    
    #endregion
    
    #region Commands
    
    public ICommand LoadShipmentCommand { get; private set; } = null!;
    public ICommand ScanItemCommand { get; private set; } = null!;
    public ICommand MarkMissingCommand { get; private set; } = null!;
    public ICommand MarkDamagedCommand { get; private set; } = null!;
    public ICommand MarkVerifiedCommand { get; private set; } = null!;
    public ICommand AddExtraItemCommand { get; private set; } = null!;
    public ICommand AddManualItemCommand { get; private set; } = null!;
    public ICommand MarkAllMissingCommand { get; private set; } = null!;
    public ICommand CompleteVerificationCommand { get; private set; } = null!;
    public ICommand SaveProgressCommand { get; private set; } = null!;
    public ICommand CancelCommand { get; private set; } = null!;
    public ICommand RefreshPendingManifestsCommand { get; private set; } = null!;
    public ICommand SelectPendingManifestCommand { get; private set; } = null!;
    public ICommand SearchExpectedItemCommand { get; private set; } = null!;
    
    private void InitializeCommands()
    {
        LoadShipmentCommand = new AsyncRelayCommand(async _ => await LoadShipmentAsync(), 
            _ => !IsBusy && !string.IsNullOrWhiteSpace(ShipmentQrCode));
        
        ScanItemCommand = new AsyncRelayCommand(async _ => await ScanItemAsync(), 
            _ => !IsBusy && IsVerifyingState && !string.IsNullOrWhiteSpace(ItemScanInput));
        
        MarkMissingCommand = new RelayCommand(param => MarkItemStatus(param, VerificationStatus.Missing),
            _ => IsVerifyingState);
        
        MarkDamagedCommand = new RelayCommand(param => MarkItemStatus(param, VerificationStatus.Damaged),
            _ => IsVerifyingState);
        
        MarkVerifiedCommand = new RelayCommand(param => MarkItemStatus(param, VerificationStatus.Verified),
            _ => IsVerifyingState);
        
        AddExtraItemCommand = new AsyncRelayCommand(async _ => await AddExtraItemAsync(),
            _ => !IsBusy && IsVerifyingState);
        
        AddManualItemCommand = new AsyncRelayCommand(async _ => await AddManualItemAsync(),
            _ => !IsBusy && IsVerifyingState);
        
        MarkAllMissingCommand = new RelayCommand(_ => MarkAllPendingAsMissing(),
            _ => IsVerifyingState && PendingCount > 0);
        
        CompleteVerificationCommand = new AsyncRelayCommand(async _ => await CompleteVerificationAsync(),
            _ => !IsBusy && IsVerifyingState && CanComplete);
        
        SaveProgressCommand = new AsyncRelayCommand(async _ => await SaveProgressAsync(),
            _ => !IsBusy && IsVerifyingState);
        
        CancelCommand = new RelayCommand(_ => Cancel());
        
        RefreshPendingManifestsCommand = new AsyncRelayCommand(async _ => await LoadPendingManifestsAsync(),
            _ => !IsBusy && IsScanShipmentState);
        
        SelectPendingManifestCommand = new AsyncRelayCommand(async param => 
        {
            if (param is Manifest manifest)
            {
                ShipmentQrCode = manifest.ManifestNumber;
                await LoadShipmentAsync();
            }
        }, _ => !IsBusy);
        
        SearchExpectedItemCommand = new RelayCommand(_ => SearchExpectedItem(),
            _ => IsVerifyingState);
    }
    
    #endregion
    
    #region Pending Manifests
    
    /// <summary>
    /// Load pending inbound manifests for the current user's location
    /// </summary>
    private async Task LoadPendingManifestsAsync()
    {
        try
        {
            PendingManifests.Clear();
            
            // If we have a specific location, filter by it
            if (_currentUserLocationId.HasValue)
            {
                var manifests = await _dbService.GetPendingInboundManifestsAsync(_currentUserLocationId.Value);
                foreach (var m in manifests)
                {
                    PendingManifests.Add(m);
                }
            }
            else
            {
                // Get all pending inbound manifests (for demo/testing)
                var allManifests = await _dbService.GetManifestsAsync(
                    status: null, 
                    type: ManifestType.Outward);
                
                foreach (var m in allManifests.Where(m => 
                    m.Status == ManifestStatus.Submitted || 
                    m.Status == ManifestStatus.InTransit))
                {
                    PendingManifests.Add(m);
                }
            }
            
            OnPropertyChanged(nameof(HasPendingManifests));
            OnPropertyChanged(nameof(PendingManifestsCount));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading pending manifests: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Set the current user's location for filtering manifests
    /// </summary>
    public void SetUserLocation(Guid locationId)
    {
        _currentUserLocationId = locationId;
        _ = LoadPendingManifestsAsync();
    }
    
    /// <summary>
    /// Open search dialog to find and verify an expected item manually
    /// </summary>
    private void SearchExpectedItem()
    {
        try
        {
            // Show dialog with only expected items from current manifest
            var searchDialog = new Views.Dialogs.ExpectedItemSearchDialog(ExpectedItems.ToList());
            
            if (searchDialog.ShowDialog() == true && searchDialog.SelectedItem != null)
            {
                var item = searchDialog.SelectedItem;
                
                // Mark as verified
                item.Status = VerificationStatus.Verified;
                item.VerifiedAt = DateTime.UtcNow;
                
                UpdateProgressProperties();
                LastScanResult = $"✓ Verified: {item.Name}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Search failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    #region Load Shipment
    
    /// <summary>
    /// Load shipment from QR code scan
    /// </summary>
    private async Task LoadShipmentAsync()
    {
        if (string.IsNullOrWhiteSpace(ShipmentQrCode)) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Loading shipment...";
            
            // Parse QR code - format: s7man:MAN-2024-00001 or just the manifest number
            var manifestNumber = ShipmentQrCode;
            if (manifestNumber.StartsWith("s7man:", StringComparison.OrdinalIgnoreCase))
            {
                manifestNumber = manifestNumber.Substring(6);
            }
            
            // Try to find the outward manifest
            _outwardManifest = await _dbService.GetManifestByNumberAsync(manifestNumber);
            
            if (_outwardManifest == null)
            {
                StatusMessage = "Shipment not found. Check the QR code.";
                LastScanResult = "❌ Invalid shipment QR code";
                return;
            }
            
            if (_outwardManifest.Type != ManifestType.Outward)
            {
                StatusMessage = "This is not an outward manifest.";
                LastScanResult = "❌ Wrong manifest type";
                return;
            }
            
            await LoadManifestDataAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            LastScanResult = "❌ Failed to load shipment";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Load manifest data and items
    /// </summary>
    private async Task LoadManifestDataAsync()
    {
        if (_outwardManifest == null) return;
        
        try
        {
            // Load items
            var items = await _dbService.GetManifestItemsAsync(_outwardManifest.ManifestId);
            
            ExpectedItems.Clear();
            foreach (var item in items)
            {
                ExpectedItems.Add(new VerificationItemViewModel(item));
            }
            
            // Check if there's an existing inward manifest (resuming verification)
            if (_outwardManifest.LinkedManifestId.HasValue)
            {
                _inwardManifest = await _dbService.GetManifestAsync(_outwardManifest.LinkedManifestId.Value);
                
                if (_inwardManifest != null)
                {
                    // Restore verification status from inward manifest
                    var inwardItems = await _dbService.GetManifestItemsAsync(_inwardManifest.ManifestId);
                    foreach (var inwardItem in inwardItems)
                    {
                        // Find matching expected item
                        var expectedItem = ExpectedItems.FirstOrDefault(e => 
                            e.EquipmentId == inwardItem.EquipmentId ||
                            e.AssetNumber == inwardItem.AssetNumber);
                        
                        if (expectedItem != null)
                        {
                            expectedItem.Status = inwardItem.VerificationStatus;
                            expectedItem.DamageNotes = inwardItem.DamageNotes;
                        }
                        else if (inwardItem.IsExtraItem)
                        {
                            // This is an extra item
                            ExtraItems.Add(new VerificationItemViewModel(inwardItem));
                        }
                    }
                    
                    StatusMessage = $"Resumed verification - {VerifiedCount}/{TotalExpectedItems} done";
                }
            }
            else
            {
                // Create new inward manifest
                await CreateInwardManifestAsync();
            }
            
            // Update UI
            OnPropertyChanged(nameof(ManifestNumber));
            OnPropertyChanged(nameof(FromLocation));
            OnPropertyChanged(nameof(ToLocation));
            OnPropertyChanged(nameof(ShippedDate));
            UpdateProgressProperties();
            
            // Transition to verification state
            WorkflowState = VerificationWorkflowState.Verifying;
            StatusMessage = "Scan items to verify shipment";
            LastScanResult = "✓ Shipment loaded - ready to verify";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading items: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Create the inward manifest linked to the outward
    /// </summary>
    private async Task CreateInwardManifestAsync()
    {
        if (_outwardManifest == null) return;
        
        _inwardManifest = new Manifest
        {
            ManifestNumber = $"IN-{_outwardManifest.ManifestNumber}",
            Type = ManifestType.Inward,
            FromLocationId = _outwardManifest.FromLocationId,
            ToLocationId = _outwardManifest.ToLocationId,
            ProjectId = _outwardManifest.ProjectId,
            Status = ManifestStatus.Draft,
            LinkedManifestId = _outwardManifest.ManifestId,
            VerificationStatus = ShipmentVerificationStatus.InProgress,
            VerificationStartedAt = DateTime.UtcNow,
            Notes = $"Inward verification for {_outwardManifest.ManifestNumber}"
        };
        
        _inwardManifest.QrCode = _qrService.GenerateManifestQrContent(_inwardManifest.ManifestNumber);
        
        await _dbService.SaveManifestAsync(_inwardManifest);
        
        // Link the outward manifest
        _outwardManifest.LinkedManifestId = _inwardManifest.ManifestId;
        _outwardManifest.VerificationStatus = ShipmentVerificationStatus.InProgress;
        await _dbService.SaveManifestAsync(_outwardManifest);
    }
    
    #endregion
    
    #region Item Scanning
    
    /// <summary>
    /// Process scanned item QR code
    /// </summary>
    private async Task ScanItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemScanInput)) return;
        
        try
        {
            var scanInput = ItemScanInput.Trim();
            ItemScanInput = string.Empty;  // Clear for next scan
            
            // Parse QR code - formats:
            // s7eq:EQ-2024-00001|S7WSS04068
            // s7eq:EQ-2024-00001
            // S7WSS04068 (just unique ID)
            // EQ-2024-00001 (just asset number)
            
            string? assetNumber = null;
            string? uniqueId = null;
            
            if (scanInput.StartsWith("s7eq:", StringComparison.OrdinalIgnoreCase))
            {
                var content = scanInput.Substring(5);
                if (content.Contains('|'))
                {
                    var parts = content.Split('|', 2);
                    assetNumber = parts[0];
                    uniqueId = parts[1];
                }
                else
                {
                    assetNumber = content;
                }
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(scanInput, @"^[A-Z]{2,5}[A-Z]{2,4}\d{5}$"))
            {
                // Plain unique ID format
                uniqueId = scanInput;
            }
            else
            {
                assetNumber = scanInput;
            }
            
            // Find in expected items
            var matchedItem = ExpectedItems.FirstOrDefault(i => 
                (!string.IsNullOrEmpty(assetNumber) && i.AssetNumber == assetNumber) ||
                (!string.IsNullOrEmpty(uniqueId) && i.UniqueId == uniqueId));
            
            if (matchedItem != null)
            {
                if (matchedItem.Status == VerificationStatus.Pending)
                {
                    matchedItem.Status = VerificationStatus.Verified;
                    matchedItem.VerifiedAt = DateTime.UtcNow;
                    LastScanResult = $"✓ {matchedItem.Name ?? matchedItem.AssetNumber} verified";
                    StatusMessage = ProgressText;
                    
                    // Save to inward manifest
                    await SaveItemVerificationAsync(matchedItem);
                }
                else
                {
                    LastScanResult = $"ℹ️ {matchedItem.Name ?? matchedItem.AssetNumber} already {matchedItem.Status}";
                }
            }
            else
            {
                // Item not on manifest - offer to add as extra
                LastScanResult = $"⚠️ Item not on manifest - adding as extra";
                await AddScannedExtraItemAsync(assetNumber, uniqueId);
            }
            
            UpdateProgressProperties();
        }
        catch (Exception ex)
        {
            LastScanResult = $"❌ Scan error: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Add a scanned item that's not on the manifest
    /// </summary>
    private async Task AddScannedExtraItemAsync(string? assetNumber, string? uniqueId)
    {
        try
        {
            // Try to find equipment in database
            Equipment? equipment = null;
            
            if (!string.IsNullOrEmpty(uniqueId))
            {
                equipment = await _dbService.GetEquipmentByUniqueIdAsync(uniqueId);
            }
            
            if (equipment == null && !string.IsNullOrEmpty(assetNumber))
            {
                equipment = await _dbService.GetEquipmentByAssetNumberAsync(assetNumber);
            }
            
            var extraItem = new VerificationItemViewModel
            {
                ItemId = Guid.NewGuid(),
                EquipmentId = equipment?.EquipmentId,
                AssetNumber = equipment?.AssetNumber ?? assetNumber,
                UniqueId = equipment?.UniqueId ?? uniqueId,
                Name = equipment?.Name ?? "Unknown Equipment",
                Status = VerificationStatus.Extra,
                IsExtraItem = true,
                VerifiedAt = DateTime.UtcNow
            };
            
            ExtraItems.Add(extraItem);
            
            // Save to inward manifest
            if (_inwardManifest != null)
            {
                var manifestItem = new ManifestItem
                {
                    ItemId = extraItem.ItemId,
                    ManifestId = _inwardManifest.ManifestId,
                    EquipmentId = extraItem.EquipmentId,
                    AssetNumber = extraItem.AssetNumber,
                    UniqueId = extraItem.UniqueId,
                    Name = extraItem.Name,
                    VerificationStatus = VerificationStatus.Extra,
                    IsExtraItem = true,
                    VerifiedAt = DateTime.UtcNow
                };
                
                await _dbService.SaveManifestItemAsync(manifestItem);
            }
            
            LastScanResult = $"➕ Added extra item: {extraItem.Name}";
            UpdateProgressProperties();
        }
        catch (Exception ex)
        {
            LastScanResult = $"❌ Failed to add extra: {ex.Message}";
        }
    }
    
    #endregion
    
    #region Item Status Changes
    
    /// <summary>
    /// Mark an item with a specific status
    /// </summary>
    private void MarkItemStatus(object? param, VerificationStatus status)
    {
        if (param is not VerificationItemViewModel item) return;
        
        item.Status = status;
        item.VerifiedAt = DateTime.UtcNow;
        
        if (status == VerificationStatus.Damaged)
        {
            // Show damage dialog to capture notes
            ShowDamageDialog(item);
        }
        
        _ = SaveItemVerificationAsync(item);
        UpdateProgressProperties();
        
        LastScanResult = $"{item.Name} marked as {status}";
    }
    
    /// <summary>
    /// Show dialog to capture damage notes
    /// </summary>
    private void ShowDamageDialog(VerificationItemViewModel item)
    {
        // Simple input dialog for damage notes
        var result = Microsoft.VisualBasic.Interaction.InputBox(
            $"Describe the damage to {item.Name}:",
            "Damage Report",
            item.DamageNotes ?? "");
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            item.DamageNotes = result;
        }
    }
    
    /// <summary>
    /// Mark all pending items as missing
    /// </summary>
    private void MarkAllPendingAsMissing()
    {
        foreach (var item in ExpectedItems.Where(i => i.Status == VerificationStatus.Pending))
        {
            item.Status = VerificationStatus.Missing;
            item.VerifiedAt = DateTime.UtcNow;
            _ = SaveItemVerificationAsync(item);
        }
        
        UpdateProgressProperties();
        LastScanResult = $"{MissingCount} items marked as missing";
    }
    
    /// <summary>
    /// Save item verification status to database
    /// </summary>
    private async Task SaveItemVerificationAsync(VerificationItemViewModel item)
    {
        if (_inwardManifest == null) return;
        
        try
        {
            // Find or create manifest item
            var manifestItem = await _dbService.GetManifestItemAsync(_inwardManifest.ManifestId, item.ItemId)
                ?? new ManifestItem
                {
                    ItemId = item.ItemId,
                    ManifestId = _inwardManifest.ManifestId,
                    EquipmentId = item.EquipmentId,
                    AssetNumber = item.AssetNumber,
                    UniqueId = item.UniqueId,
                    Name = item.Name
                };
            
            manifestItem.VerificationStatus = item.Status;
            manifestItem.VerifiedAt = item.VerifiedAt;
            manifestItem.DamageNotes = item.DamageNotes;
            manifestItem.IsExtraItem = item.IsExtraItem;
            manifestItem.ConditionAtReceive = item.Status == VerificationStatus.Damaged ? "Damaged" : "Good";
            
            await _dbService.SaveManifestItemAsync(manifestItem);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save item: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Add Manual/Extra Items
    
    /// <summary>
    /// Add an extra item by scanning or searching
    /// </summary>
    private async Task AddExtraItemAsync()
    {
        // This would open equipment search dialog
        // For now, show a message
        MessageBox.Show(
            "To add an extra item:\n\n" +
            "1. Scan the item's QR code in the scan field, or\n" +
            "2. Use 'Add Manual Item' for items without QR codes",
            "Add Extra Item",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    
    /// <summary>
    /// Add a manual item (consumable/no QR)
    /// </summary>
    private async Task AddManualItemAsync()
    {
        try
        {
            // Show simplified dialog for adding unregistered items
            var dialog = new Views.Dialogs.AddUnregisteredItemDialog(_dbService, _inwardManifest?.ToLocationId);
            
            if (dialog.ShowDialog() == true && dialog.CreatedItem != null)
            {
                var unregisteredItem = dialog.CreatedItem;
                
                // Link to manifest
                unregisteredItem.ManifestId = _inwardManifest!.ManifestId;
                await _dbService.SaveUnregisteredItemAsync(unregisteredItem);
                
                // Add to extra items list
                var extraItem = new VerificationItemViewModel
                {
                    ItemId = Guid.NewGuid(),
                    Name = unregisteredItem.Name,
                    Description = unregisteredItem.Description,
                    SerialNumber = unregisteredItem.SerialNumber,
                    Status = VerificationStatus.Extra,
                    IsExtraItem = true,
                    IsManuallyAdded = true,
                    UnregisteredItemId = unregisteredItem.UnregisteredItemId,
                    VerifiedAt = DateTime.UtcNow
                };
                
                ExtraItems.Add(extraItem);
                
                // Save to manifest
                var manifestItem = new ManifestItem
                {
                    ItemId = extraItem.ItemId,
                    ManifestId = _inwardManifest.ManifestId,
                    Name = unregisteredItem.Name,
                    Description = unregisteredItem.Description,
                    SerialNumber = unregisteredItem.SerialNumber,
                    Quantity = unregisteredItem.Quantity,
                    VerificationStatus = VerificationStatus.Extra,
                    IsExtraItem = true,
                    IsManuallyAdded = true,
                    UnregisteredItemId = unregisteredItem.UnregisteredItemId,
                    VerifiedAt = DateTime.UtcNow
                };
                
                await _dbService.SaveManifestItemAsync(manifestItem);
                
                LastScanResult = $"➕ Added: {unregisteredItem.Name} (pending review)";
                UpdateProgressProperties();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to add item: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    #region Complete/Save
    
    /// <summary>
    /// Save progress without completing
    /// </summary>
    private async Task SaveProgressAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Saving progress...";
            
            // Update inward manifest
            if (_inwardManifest != null)
            {
                _inwardManifest.VerifiedItemCount = VerifiedCount;
                _inwardManifest.DiscrepancyCount = MissingCount + DamagedCount;
                _inwardManifest.UpdatedAt = DateTime.UtcNow;
                
                await _dbService.SaveManifestAsync(_inwardManifest);
            }
            
            StatusMessage = "Progress saved";
            LastScanResult = "✓ Progress saved - you can continue later";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Complete the verification process
    /// </summary>
    private async Task CompleteVerificationAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Completing verification...";
            
            // Confirm if there are pending items
            if (PendingCount > 0)
            {
                var result = MessageBox.Show(
                    $"There are {PendingCount} items not yet verified.\n\n" +
                    "Do you want to mark them as MISSING and complete?",
                    "Pending Items",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    MarkAllPendingAsMissing();
                }
                else if (result != MessageBoxResult.No)
                {
                    IsBusy = false;
                    return;
                }
            }
            
            // Update inward manifest
            if (_inwardManifest != null)
            {
                _inwardManifest.Status = ManifestStatus.Completed;
                _inwardManifest.VerificationStatus = HasDiscrepancies 
                    ? ShipmentVerificationStatus.CompletedWithDiscrepancies 
                    : ShipmentVerificationStatus.Completed;
                _inwardManifest.VerificationCompletedAt = DateTime.UtcNow;
                _inwardManifest.VerifiedItemCount = VerifiedCount;
                _inwardManifest.DiscrepancyCount = MissingCount + DamagedCount;
                _inwardManifest.TotalItems = TotalExpectedItems + ExtraCount;
                _inwardManifest.HasDiscrepancies = HasDiscrepancies;
                _inwardManifest.ReceivedDate = DateTime.UtcNow;
                _inwardManifest.CompletedDate = DateTime.UtcNow;
                _inwardManifest.VerificationSummary = GenerateVerificationSummary();
                
                await _dbService.SaveManifestAsync(_inwardManifest);
            }
            
            // Update outward manifest status
            if (_outwardManifest != null)
            {
                _outwardManifest.Status = HasDiscrepancies 
                    ? ManifestStatus.PartiallyReceived 
                    : ManifestStatus.Received;
                _outwardManifest.VerificationStatus = _inwardManifest?.VerificationStatus 
                    ?? ShipmentVerificationStatus.Completed;
                _outwardManifest.ReceivedDate = DateTime.UtcNow;
                _outwardManifest.HasDiscrepancies = HasDiscrepancies;
                
                await _dbService.SaveManifestAsync(_outwardManifest);
            }
            
            // Transfer equipment locations for verified items
            await TransferVerifiedItemsAsync();
            
            // Create notifications for missing items
            await CreateMissingItemNotificationsAsync();
            
            // Transition to complete state
            WorkflowState = VerificationWorkflowState.Complete;
            StatusMessage = "Verification completed!";
            
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to complete verification:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Transfer verified items to destination location
    /// </summary>
    private async Task TransferVerifiedItemsAsync()
    {
        if (_outwardManifest?.ToLocationId == null) return;
        
        var destinationId = _outwardManifest.ToLocationId.Value;
        
        foreach (var item in ExpectedItems.Where(i => i.Status == VerificationStatus.Verified || 
                                                       i.Status == VerificationStatus.Damaged))
        {
            if (item.EquipmentId.HasValue)
            {
                await _dbService.UpdateEquipmentLocationAsync(item.EquipmentId.Value, destinationId);
            }
        }
        
        // Also update extra items that were from another location
        foreach (var item in ExtraItems.Where(i => i.EquipmentId.HasValue))
        {
            await _dbService.UpdateEquipmentLocationAsync(item.EquipmentId!.Value, destinationId);
        }
    }
    
    /// <summary>
    /// Create notifications for missing items
    /// </summary>
    private async Task CreateMissingItemNotificationsAsync()
    {
        if (_outwardManifest == null) return;
        
        foreach (var item in ExpectedItems.Where(i => i.Status == VerificationStatus.Missing))
        {
            var notification = new ManifestNotification
            {
                ManifestId = _outwardManifest.ManifestId,
                ManifestItemId = item.ItemId,
                EquipmentId = item.EquipmentId,
                NotificationType = "Missing",
                Message = $"Equipment missing from shipment: {item.Name ?? item.AssetNumber}",
                Details = $"Item was on manifest {_outwardManifest.ManifestNumber} but not received at destination.",
                LocationId = _outwardManifest.FromLocationId,  // Notify source location
                RequiresAction = true,
                CreatedBy = _authService.CurrentUser?.UserId
            };
            
            await _dbService.SaveManifestNotificationAsync(notification);
        }
    }
    
    /// <summary>
    /// Generate summary text
    /// </summary>
    private string GenerateVerificationSummary()
    {
        var parts = new List<string>
        {
            $"Verified: {VerifiedCount}/{TotalExpectedItems}"
        };
        
        if (MissingCount > 0) parts.Add($"Missing: {MissingCount}");
        if (DamagedCount > 0) parts.Add($"Damaged: {DamagedCount}");
        if (ExtraCount > 0) parts.Add($"Extra items: {ExtraCount}");
        
        return string.Join(" | ", parts);
    }
    
    private void Cancel()
    {
        if (WorkflowState == VerificationWorkflowState.Verifying && VerifiedCount > 0)
        {
            var result = MessageBox.Show(
                "You have verification progress. Do you want to save before closing?",
                "Save Progress?",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _ = SaveProgressAsync();
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return;
            }
        }
        
        DialogResult = false;
    }
    
    private void UpdateProgressProperties()
    {
        OnPropertyChanged(nameof(TotalExpectedItems));
        OnPropertyChanged(nameof(VerifiedCount));
        OnPropertyChanged(nameof(MissingCount));
        OnPropertyChanged(nameof(DamagedCount));
        OnPropertyChanged(nameof(ExtraCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressPercentage));
        OnPropertyChanged(nameof(HasDiscrepancies));
        OnPropertyChanged(nameof(CanComplete));
        OnPropertyChanged(nameof(DiscrepancySummary));
    }
    
    #endregion
}

/// <summary>
/// Workflow states for verification process
/// </summary>
public enum VerificationWorkflowState
{
    ScanShipment,
    Verifying,
    Complete
}

/// <summary>
/// ViewModel for individual verification items
/// </summary>
public class VerificationItemViewModel : ViewModelBase
{
    private VerificationStatus _status = VerificationStatus.Pending;
    private string? _damageNotes;
    
    public VerificationItemViewModel() { }
    
    public VerificationItemViewModel(ManifestItem item)
    {
        ItemId = item.ItemId;
        EquipmentId = item.EquipmentId;
        AssetNumber = item.AssetNumber;
        UniqueId = item.UniqueId;
        Name = item.Name;
        SerialNumber = item.SerialNumber;
        Quantity = item.Quantity;
        ConditionAtSend = item.ConditionAtSend;
        _status = item.VerificationStatus;
        _damageNotes = item.DamageNotes;
        IsExtraItem = item.IsExtraItem;
        IsManuallyAdded = item.IsManuallyAdded;
        UnregisteredItemId = item.UnregisteredItemId;
        VerifiedAt = item.VerifiedAt;
    }
    
    public Guid ItemId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? AssetNumber { get; set; }
    public string? UniqueId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; } = 1;
    public string? ConditionAtSend { get; set; }
    public bool IsExtraItem { get; set; }
    public bool IsManuallyAdded { get; set; }
    public Guid? UnregisteredItemId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    
    public VerificationStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(IsVerified));
                OnPropertyChanged(nameof(IsPending));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }
    
    public string? DamageNotes
    {
        get => _damageNotes;
        set => SetProperty(ref _damageNotes, value);
    }
    
    public string StatusDisplay => Status switch
    {
        VerificationStatus.Pending => "⏳ Pending",
        VerificationStatus.Verified => "✓ Verified",
        VerificationStatus.Damaged => "⚠️ Damaged",
        VerificationStatus.Missing => "❌ Missing",
        VerificationStatus.Extra => "➕ Extra",
        VerificationStatus.Wrong => "✗ Wrong",
        _ => "Unknown"
    };
    
    public string StatusColor => Status switch
    {
        VerificationStatus.Verified => "#2ECC71",
        VerificationStatus.Damaged => "#F39C12",
        VerificationStatus.Missing => "#E74C3C",
        VerificationStatus.Extra => "#3498DB",
        _ => "#95A5A6"
    };
    
    public bool IsVerified => Status == VerificationStatus.Verified || Status == VerificationStatus.Damaged;
    public bool IsPending => Status == VerificationStatus.Pending;
    
    public string DisplayName => Name ?? AssetNumber ?? UniqueId ?? "Unknown Item";
}
