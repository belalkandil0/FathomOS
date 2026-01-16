using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

/// <summary>
/// Dialog for converting an unregistered item to a tracked equipment record.
/// Pre-populates fields from the unregistered item and allows user to finalize details.
/// </summary>
public partial class ConvertToEquipmentDialog : MetroWindow
{
    private readonly LocalDatabaseService _dbService;
    private readonly UnregisteredItem _sourceItem;
    
    /// <summary>
    /// The created equipment record (available after successful conversion)
    /// </summary>
    public Equipment? CreatedEquipment { get; private set; }
    
    public ConvertToEquipmentDialog(LocalDatabaseService dbService, UnregisteredItem sourceItem)
    {
        _dbService = dbService;
        _sourceItem = sourceItem;
        
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        Loaded += async (s, e) => await InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        try
        {
            // Load lookups
            var categories = await _dbService.GetCategoriesAsync();
            var types = await _dbService.GetEquipmentTypesAsync();
            var locations = await _dbService.GetLocationsAsync();
            
            CategoryCombo.ItemsSource = categories;
            TypeCombo.ItemsSource = types;
            LocationCombo.ItemsSource = locations;
            
            // Pre-populate from source item
            NameBox.Text = _sourceItem.Name;
            DescriptionBox.Text = _sourceItem.Description ?? "";
            SerialNumberBox.Text = _sourceItem.SerialNumber ?? "";
            PartNumberBox.Text = _sourceItem.PartNumber ?? "";
            ManufacturerBox.Text = _sourceItem.Manufacturer ?? "";
            ModelBox.Text = _sourceItem.Model ?? "";
            
            // Set category if suggested
            if (_sourceItem.SuggestedCategoryId.HasValue)
            {
                var suggestedCategory = categories.FirstOrDefault(c => c.CategoryId == _sourceItem.SuggestedCategoryId);
                if (suggestedCategory != null)
                    CategoryCombo.SelectedItem = suggestedCategory;
            }
            
            // Set type if suggested
            if (_sourceItem.SuggestedTypeId.HasValue)
            {
                var suggestedType = types.FirstOrDefault(t => t.TypeId == _sourceItem.SuggestedTypeId);
                if (suggestedType != null)
                    TypeCombo.SelectedItem = suggestedType;
            }
            
            // Set location
            if (_sourceItem.CurrentLocationId.HasValue)
            {
                var location = locations.FirstOrDefault(l => l.LocationId == _sourceItem.CurrentLocationId);
                if (location != null)
                    LocationCombo.SelectedItem = location;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            MessageBox.Show("Please enter a name for the equipment.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }
        
        if (CategoryCombo.SelectedItem == null)
        {
            MessageBox.Show("Please select a category.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            CategoryCombo.Focus();
            return;
        }
        
        if (LocationCombo.SelectedItem == null)
        {
            MessageBox.Show("Please select a location.", "Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            LocationCombo.Focus();
            return;
        }
        
        try
        {
            var category = (EquipmentCategory)CategoryCombo.SelectedItem;
            var location = (Location)LocationCombo.SelectedItem;
            var equipmentType = TypeCombo.SelectedItem as EquipmentType;
            
            // Generate asset number
            var assetNumber = await _dbService.GenerateAssetNumberAsync(category.Code);
            
            // Get status from combo
            var statusTag = (StatusCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Available";
            var status = Enum.TryParse<EquipmentStatus>(statusTag, out var parsedStatus) 
                ? parsedStatus 
                : EquipmentStatus.Available;
            
            // Get condition from combo
            var conditionTag = (ConditionCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Good";
            var condition = Enum.TryParse<EquipmentCondition>(conditionTag, out var parsedCondition) 
                ? parsedCondition 
                : EquipmentCondition.Good;
            
            // Create equipment
            var equipment = new Equipment
            {
                EquipmentId = Guid.NewGuid(),
                AssetNumber = assetNumber,
                Name = NameBox.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim(),
                CategoryId = category.CategoryId,
                TypeId = equipmentType?.TypeId,
                LocationId = location.LocationId,
                SerialNumber = string.IsNullOrWhiteSpace(SerialNumberBox.Text) ? null : SerialNumberBox.Text.Trim(),
                PartNumber = string.IsNullOrWhiteSpace(PartNumberBox.Text) ? null : PartNumberBox.Text.Trim(),
                Manufacturer = string.IsNullOrWhiteSpace(ManufacturerBox.Text) ? null : ManufacturerBox.Text.Trim(),
                Model = string.IsNullOrWhiteSpace(ModelBox.Text) ? null : ModelBox.Text.Trim(),
                Status = status,
                Condition = condition,
                Ownership = OwnershipType.Owned,
                Notes = string.IsNullOrWhiteSpace(NotesBox.Text) 
                    ? $"Converted from unregistered item (Manifest: {_sourceItem.Manifest?.ManifestNumber ?? "Unknown"})"
                    : NotesBox.Text.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            
            // Generate QR code
            equipment.QrCode = QRCodeService.GenerateEquipmentQrCodeValue(equipment.AssetNumber, equipment.EquipmentId);
            
            // Save equipment
            await _dbService.SaveEquipmentAsync(equipment);
            
            // Record history
            var historyEvent = new EquipmentEvent
            {
                EventId = Guid.NewGuid(),
                EquipmentId = equipment.EquipmentId,
                EventType = EventType.Created,
                Description = $"Equipment created from unregistered item via manifest {_sourceItem.Manifest?.ManifestNumber ?? "Unknown"}",
                Timestamp = DateTime.UtcNow
            };
            await _dbService.AddEquipmentEventAsync(historyEvent);
            
            CreatedEquipment = equipment;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating equipment: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
