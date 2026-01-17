using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Views;

/// <summary>
/// View for reviewing and processing unregistered items received during shipment verification.
/// Allows converting items to equipment, marking as consumables, or rejecting.
/// </summary>
public partial class UnregisteredItemsView : UserControl, INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private UnregisteredItem? _selectedItem;
    private string _searchText = "";
    private string _statusFilter = "All Statuses";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ObservableCollection<UnregisteredItem> AllItems { get; } = new();
    public ObservableCollection<UnregisteredItem> FilteredItems { get; } = new();
    
    public UnregisteredItemsView(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        
        // Load theme
        var themeUri = new Uri("/FathomOS.Modules.EquipmentInventory;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        DataContext = this;
        
        Loaded += async (s, e) => await LoadItemsAsync();
    }
    
    #region Data Loading
    
    private async Task LoadItemsAsync()
    {
        try
        {
            var items = await _dbService.GetUnregisteredItemsAsync();
            
            AllItems.Clear();
            foreach (var item in items.OrderByDescending(i => i.CreatedAt))
            {
                AllItems.Add(item);
            }
            
            UpdateCounts();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading unregistered items: {ex.Message}");
        }
    }
    
    private void UpdateCounts()
    {
        PendingCount.Text = AllItems.Count(i => i.Status == UnregisteredItemStatus.PendingReview).ToString();
        ConvertedCount.Text = AllItems.Count(i => i.Status == UnregisteredItemStatus.ConvertedToEquipment).ToString();
        ConsumableCount.Text = AllItems.Count(i => i.Status == UnregisteredItemStatus.KeptAsConsumable).ToString();
        RejectedCount.Text = AllItems.Count(i => i.Status == UnregisteredItemStatus.Rejected).ToString();
    }
    
    private void ApplyFilters()
    {
        var filtered = AllItems.AsEnumerable();
        
        // Apply status filter
        filtered = _statusFilter switch
        {
            "Pending Review" => filtered.Where(i => i.Status == UnregisteredItemStatus.PendingReview),
            "Converted" => filtered.Where(i => i.Status == UnregisteredItemStatus.ConvertedToEquipment),
            "Consumable" => filtered.Where(i => i.Status == UnregisteredItemStatus.KeptAsConsumable),
            "Rejected" => filtered.Where(i => i.Status == UnregisteredItemStatus.Rejected),
            _ => filtered
        };
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLower();
            filtered = filtered.Where(i =>
                (i.Name?.ToLower().Contains(search) ?? false) ||
                (i.SerialNumber?.ToLower().Contains(search) ?? false) ||
                (i.Manufacturer?.ToLower().Contains(search) ?? false) ||
                (i.Model?.ToLower().Contains(search) ?? false) ||
                (i.PartNumber?.ToLower().Contains(search) ?? false) ||
                (i.Manifest?.ManifestNumber?.ToLower().Contains(search) ?? false));
        }
        
        FilteredItems.Clear();
        foreach (var item in filtered)
        {
            FilteredItems.Add(item);
        }
        
        ItemsList.ItemsSource = FilteredItems;
        EmptyState.Visibility = FilteredItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        ApplyFilters();
    }
    
    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusFilter.SelectedItem is ComboBoxItem item)
        {
            _statusFilter = item.Content?.ToString() ?? "All Statuses";
            ApplyFilters();
        }
    }
    
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadItemsAsync();
    }
    
    private void ItemCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is UnregisteredItem item)
        {
            SelectItem(item);
        }
    }
    
    private void SelectItem(UnregisteredItem item)
    {
        _selectedItem = item;
        
        NoSelectionState.Visibility = Visibility.Collapsed;
        SelectedItemDetails.Visibility = Visibility.Visible;
        
        // Populate details
        DetailName.Text = item.Name;
        DetailStatus.Text = item.Status.ToString();
        DetailStatusBadge.Background = GetStatusBrush(item.Status);
        DetailQuantity.Text = $"Qty: {item.Quantity}";
        
        DetailSerial.Text = item.SerialNumber ?? "-";
        DetailPartNumber.Text = item.PartNumber ?? "-";
        DetailManufacturer.Text = item.Manufacturer ?? "-";
        DetailModel.Text = item.Model ?? "-";
        DetailDescription.Text = item.Description ?? "-";
        
        DetailManifest.Text = item.Manifest?.ManifestNumber ?? "-";
        DetailLocation.Text = item.CurrentLocation?.Name ?? "-";
        
        DetailCreatedAt.Text = item.CreatedAt.ToString("MMM dd, yyyy HH:mm");
        DetailReviewedAt.Text = item.ReviewedAt?.ToString("MMM dd, yyyy HH:mm") ?? "-";
        
        // Review notes
        if (!string.IsNullOrWhiteSpace(item.ReviewNotes))
        {
            ReviewNotesPanel.Visibility = Visibility.Visible;
            DetailReviewNotes.Text = item.ReviewNotes;
        }
        else
        {
            ReviewNotesPanel.Visibility = Visibility.Collapsed;
        }
        
        // Update action buttons
        UpdateActionButtons(item);
    }
    
    private void UpdateActionButtons(UnregisteredItem item)
    {
        ActionButtons.Children.Clear();
        
        if (item.Status == UnregisteredItemStatus.PendingReview)
        {
            // Primary action: Convert to Equipment
            AddActionButton("Convert to Equipment", "SwapHorizontal", ConvertToEquipment_Click);
            AddActionButton("Mark as Consumable", "PackageVariant", MarkAsConsumable_Click, isSecondary: true);
            AddActionButton("Reject Item", "Close", RejectItem_Click, isSecondary: true);
        }
        else if (item.Status == UnregisteredItemStatus.ConvertedToEquipment && item.ConvertedEquipmentId.HasValue)
        {
            // Show link to equipment
            AddActionButton("View Equipment", "Eye", ViewEquipment_Click);
        }
        else if (item.Status == UnregisteredItemStatus.KeptAsConsumable || item.Status == UnregisteredItemStatus.Rejected)
        {
            // Allow reverting to pending
            AddActionButton("Revert to Pending", "Undo", RevertToPending_Click, isSecondary: true);
        }
    }
    
    private void AddActionButton(string text, string iconKind, RoutedEventHandler handler, bool isSecondary = false)
    {
        var button = new Button
        {
            Style = (Style)FindResource(isSecondary ? "SecondaryButton" : "PrimaryButton"),
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
        };
        
        var stack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
        var icon = new MahApps.Metro.IconPacks.PackIconMaterial 
        { 
            Kind = (MahApps.Metro.IconPacks.PackIconMaterialKind)Enum.Parse(typeof(MahApps.Metro.IconPacks.PackIconMaterialKind), iconKind),
            Width = 16, 
            Height = 16, 
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var textBlock = new TextBlock { Text = text };
        
        stack.Children.Add(icon);
        stack.Children.Add(textBlock);
        button.Content = stack;
        button.Click += handler;
        
        ActionButtons.Children.Add(button);
    }
    
    private Brush GetStatusBrush(UnregisteredItemStatus status)
    {
        return status switch
        {
            UnregisteredItemStatus.PendingReview => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            UnregisteredItemStatus.ConvertedToEquipment => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            UnregisteredItemStatus.KeptAsConsumable => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            UnregisteredItemStatus.Rejected => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
            _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
        };
    }
    
    #endregion
    
    #region Action Handlers
    
    private async void ConvertToEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        
        // Open dialog to finalize equipment details
        var dialog = new Dialogs.ConvertToEquipmentDialog(_dbService, _selectedItem);
        dialog.Owner = Window.GetWindow(this);
        
        if (dialog.ShowDialog() == true && dialog.CreatedEquipment != null)
        {
            try
            {
                // Update the unregistered item
                _selectedItem.Status = UnregisteredItemStatus.ConvertedToEquipment;
                _selectedItem.ConvertedEquipmentId = dialog.CreatedEquipment.EquipmentId;
                _selectedItem.ReviewedAt = DateTime.UtcNow;
                _selectedItem.ReviewNotes = $"Converted to equipment: {dialog.CreatedEquipment.AssetNumber}";
                
                await _dbService.SaveUnregisteredItemAsync(_selectedItem);
                await LoadItemsAsync();
                
                MessageBox.Show($"Item converted to equipment successfully.\n\nAsset Number: {dialog.CreatedEquipment.AssetNumber}",
                    "Conversion Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private async void MarkAsConsumable_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        
        var result = MessageBox.Show(
            $"Mark '{_selectedItem.Name}' as a consumable?\n\n" +
            "Consumables are not tracked in the inventory system.",
            "Mark as Consumable",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            _selectedItem.Status = UnregisteredItemStatus.KeptAsConsumable;
            _selectedItem.ReviewedAt = DateTime.UtcNow;
            _selectedItem.ReviewNotes = "Marked as consumable - not tracked";
            
            await _dbService.SaveUnregisteredItemAsync(_selectedItem);
            await LoadItemsAsync();
            
            MessageBox.Show("Item marked as consumable.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void RejectItem_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        
        // Show rejection reason dialog
        var dialog = new Dialogs.RejectionReasonDialog();
        dialog.Owner = Window.GetWindow(this);
        dialog.Title = "Reject Item";
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            _selectedItem.Status = UnregisteredItemStatus.Rejected;
            _selectedItem.ReviewedAt = DateTime.UtcNow;
            _selectedItem.ReviewNotes = $"Rejected: {dialog.RejectionReason}";
            
            await _dbService.SaveUnregisteredItemAsync(_selectedItem);
            await LoadItemsAsync();
            
            MessageBox.Show("Item rejected.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ViewEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem?.ConvertedEquipmentId == null) return;
        
        // Navigate to equipment - this would typically be handled by the main view model
        MessageBox.Show($"Equipment ID: {_selectedItem.ConvertedEquipmentId}\n\n" +
            "Navigate to Equipment view to see details.",
            "View Equipment", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private async void RevertToPending_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedItem == null) return;
        
        var result = MessageBox.Show(
            $"Revert '{_selectedItem.Name}' to pending review?",
            "Revert Status",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            _selectedItem.Status = UnregisteredItemStatus.PendingReview;
            _selectedItem.ReviewedAt = null;
            _selectedItem.ReviewNotes = null;
            _selectedItem.ConvertedEquipmentId = null;
            
            await _dbService.SaveUnregisteredItemAsync(_selectedItem);
            await LoadItemsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
