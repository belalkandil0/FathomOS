using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.ViewModels;

namespace FathomOS.Modules.EquipmentInventory.Views;

/// <summary>
/// Comprehensive Manifest Management view with tabs for different workflow stages.
/// Handles Outbound, Inbound, In Transit, Pending Receipt, and Discrepancy views.
/// </summary>
public partial class ManifestManagementView : UserControl, INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private readonly MainViewModel _mainViewModel;
    private Manifest? _selectedManifest;
    private string _currentTab = "All";
    private string _searchText = "";
    private string _statusFilter = "All Statuses";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    // Collections
    public ObservableCollection<Manifest> AllManifests { get; } = new();
    public ObservableCollection<Manifest> FilteredManifests { get; } = new();
    
    // Counts for tab badges
    private int _totalManifestCount;
    public int TotalManifestCount
    {
        get => _totalManifestCount;
        set { _totalManifestCount = value; OnPropertyChanged(); }
    }
    
    private int _outboundCount;
    public int OutboundCount
    {
        get => _outboundCount;
        set { _outboundCount = value; OnPropertyChanged(); }
    }
    
    private int _inboundCount;
    public int InboundCount
    {
        get => _inboundCount;
        set { _inboundCount = value; OnPropertyChanged(); }
    }
    
    private int _inTransitCount;
    public int InTransitCount
    {
        get => _inTransitCount;
        set { _inTransitCount = value; OnPropertyChanged(); }
    }
    
    private int _pendingReceiptCount;
    public int PendingReceiptCount
    {
        get => _pendingReceiptCount;
        set { _pendingReceiptCount = value; OnPropertyChanged(); }
    }
    
    private int _discrepancyCount;
    public int DiscrepancyCount
    {
        get => _discrepancyCount;
        set { _discrepancyCount = value; OnPropertyChanged(); }
    }
    
    public ManifestManagementView(LocalDatabaseService dbService, MainViewModel mainViewModel)
    {
        _dbService = dbService;
        _mainViewModel = mainViewModel;
        
        // Load theme
        var themeUri = new Uri("/FathomOS.Modules.EquipmentInventory;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        DataContext = this;
        
        Loaded += async (s, e) => await LoadManifestsAsync();
    }
    
    #region Data Loading
    
    private async Task LoadManifestsAsync()
    {
        try
        {
            var manifests = await _dbService.GetManifestsAsync();
            
            AllManifests.Clear();
            foreach (var manifest in manifests.OrderByDescending(m => m.CreatedDate))
            {
                AllManifests.Add(manifest);
            }
            
            UpdateCounts();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading manifests: {ex.Message}");
        }
    }
    
    private void UpdateCounts()
    {
        TotalManifestCount = AllManifests.Count;
        OutboundCount = AllManifests.Count(m => m.Type == ManifestType.Outward);
        InboundCount = AllManifests.Count(m => m.Type == ManifestType.Inward);
        InTransitCount = AllManifests.Count(m => m.Status == ManifestStatus.InTransit || m.Status == ManifestStatus.Shipped);
        PendingReceiptCount = AllManifests.Count(m => 
            (m.Status == ManifestStatus.InTransit || m.Status == ManifestStatus.Shipped) &&
            m.ExpectedArrivalDate.HasValue && m.ExpectedArrivalDate.Value.Date <= DateTime.Today.AddDays(3));
        DiscrepancyCount = AllManifests.Count(m => m.HasDiscrepancies || m.DiscrepancyCount > 0);
    }
    
    private void ApplyFilters()
    {
        var filtered = AllManifests.AsEnumerable();
        
        // Apply tab filter
        filtered = _currentTab switch
        {
            "Outbound" => filtered.Where(m => m.Type == ManifestType.Outward),
            "Inbound" => filtered.Where(m => m.Type == ManifestType.Inward),
            "In Transit" => filtered.Where(m => m.Status == ManifestStatus.InTransit || m.Status == ManifestStatus.Shipped),
            "Pending Receipt" => filtered.Where(m => 
                (m.Status == ManifestStatus.InTransit || m.Status == ManifestStatus.Shipped) &&
                m.ExpectedArrivalDate.HasValue && m.ExpectedArrivalDate.Value.Date <= DateTime.Today.AddDays(3)),
            "Discrepancies" => filtered.Where(m => m.HasDiscrepancies || m.DiscrepancyCount > 0),
            _ => filtered
        };
        
        // Apply status filter
        if (_statusFilter != "All Statuses")
        {
            var statusToMatch = _statusFilter switch
            {
                "Draft" => ManifestStatus.Draft,
                "Submitted" => ManifestStatus.Submitted,
                "Approved" => ManifestStatus.Approved,
                "In Transit" => ManifestStatus.InTransit,
                "Received" => ManifestStatus.Received,
                "Completed" => ManifestStatus.Completed,
                _ => (ManifestStatus?)null
            };
            
            if (statusToMatch.HasValue)
                filtered = filtered.Where(m => m.Status == statusToMatch.Value);
        }
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLower();
            filtered = filtered.Where(m =>
                (m.ManifestNumber?.ToLower().Contains(search) ?? false) ||
                (m.FromLocation?.Name?.ToLower().Contains(search) ?? false) ||
                (m.ToLocation?.Name?.ToLower().Contains(search) ?? false) ||
                (m.TrackingNumber?.ToLower().Contains(search) ?? false) ||
                (m.CarrierName?.ToLower().Contains(search) ?? false));
        }
        
        FilteredManifests.Clear();
        foreach (var manifest in filtered)
        {
            FilteredManifests.Add(manifest);
        }
        
        // Update empty state visibility
        EmptyState.Visibility = FilteredManifests.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            _currentTab = tag;
            ApplyFilters();
        }
    }
    
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
    
    private async void RefreshManifests_Click(object sender, RoutedEventArgs e)
    {
        await LoadManifestsAsync();
    }
    
    private void ManifestCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is Manifest manifest)
        {
            SelectManifest(manifest);
        }
    }
    
    private void SelectManifest(Manifest manifest)
    {
        _selectedManifest = manifest;
        _mainViewModel.SelectedManifest = manifest;
        
        // Update details panel
        NoSelectionState.Visibility = Visibility.Collapsed;
        SelectedManifestDetails.Visibility = Visibility.Visible;
        
        // Populate details
        DetailManifestNumber.Text = manifest.ManifestNumber;
        DetailType.Text = manifest.Type.ToString().ToUpper();
        DetailStatus.Text = manifest.StatusDisplay;
        
        // Set type badge color
        DetailTypeBadge.Background = manifest.Type == ManifestType.Outward 
            ? new SolidColorBrush(Color.FromRgb(0, 180, 216)) 
            : new SolidColorBrush(Color.FromRgb(0, 212, 170));
        
        // Set status badge color
        DetailStatusBadge.Background = GetStatusBrush(manifest.Status);
        
        // Route
        DetailFromLocation.Text = manifest.FromLocation?.Name ?? "-";
        DetailToLocation.Text = manifest.ToLocation?.Name ?? "-";
        
        // Dates
        DetailCreatedDate.Text = manifest.CreatedDate.ToString("MMM dd, yyyy");
        DetailShippedDate.Text = manifest.ShippedDate?.ToString("MMM dd, yyyy") ?? "-";
        DetailExpectedDate.Text = manifest.ExpectedArrivalDate?.ToString("MMM dd, yyyy") ?? "-";
        DetailReceivedDate.Text = manifest.ReceivedDate?.ToString("MMM dd, yyyy") ?? "-";
        
        // Tracking
        DetailCarrier.Text = manifest.CarrierName ?? "-";
        DetailTrackingNumber.Text = manifest.TrackingNumber ?? "-";
        
        // Verification
        var showVerification = manifest.Status == ManifestStatus.InTransit || 
                               manifest.Status == ManifestStatus.Shipped ||
                               manifest.Status == ManifestStatus.Received ||
                               manifest.Status == ManifestStatus.PartiallyReceived;
        VerificationHeader.Visibility = showVerification ? Visibility.Visible : Visibility.Collapsed;
        VerificationStatusPanel.Visibility = showVerification ? Visibility.Visible : Visibility.Collapsed;
        
        if (showVerification)
        {
            DetailVerificationStatus.Text = manifest.VerificationStatus.ToString();
            DetailVerificationProgress.Text = $"{manifest.VerifiedItemCount} of {manifest.TotalItems} items verified";
            DetailVerifiedCount.Text = manifest.VerifiedItemCount.ToString();
        }
        
        // Items
        DetailItemsList.ItemsSource = manifest.Items?.Take(5).ToList();
        
        // Update action buttons
        UpdateActionButtons(manifest);
    }
    
    private void UpdateActionButtons(Manifest manifest)
    {
        ActionButtons.Children.Clear();
        
        switch (manifest.Status)
        {
            case ManifestStatus.Draft:
                AddActionButton("Submit for Approval", "SendOutline", SubmitManifest_Click);
                AddActionButton("Edit", "Pencil", EditManifest_Click, isSecondary: true);
                AddActionButton("Delete", "Delete", DeleteManifest_Click, isSecondary: true);
                break;
                
            case ManifestStatus.Submitted:
            case ManifestStatus.PendingApproval:
                AddActionButton("Approve", "Check", ApproveManifest_Click);
                AddActionButton("Reject", "Close", RejectManifest_Click, isSecondary: true);
                break;
                
            case ManifestStatus.Approved:
                AddActionButton("Mark as Shipped", "TruckDelivery", ShipManifest_Click);
                break;
                
            case ManifestStatus.InTransit:
            case ManifestStatus.Shipped:
                AddActionButton("Start Verification", "QrcodeScan", StartVerification_Click);
                AddActionButton("Mark as Received", "PackageVariantClosed", ReceiveManifest_Click, isSecondary: true);
                break;
                
            case ManifestStatus.PartiallyReceived:
                AddActionButton("Continue Verification", "QrcodeScan", StartVerification_Click);
                AddActionButton("Complete", "CheckAll", CompleteManifest_Click, isSecondary: true);
                break;
                
            case ManifestStatus.Received:
                AddActionButton("Complete", "CheckAll", CompleteManifest_Click);
                break;
        }
        
        // Always add export option
        AddActionButton("Export PDF", "FilePdfBox", ExportManifestPdf_Click, isSecondary: true);
    }
    
    private void AddActionButton(string text, string iconKind, RoutedEventHandler handler, bool isSecondary = false)
    {
        var button = new Button
        {
            Style = (Style)FindResource(isSecondary ? "SecondaryButton" : "PrimaryButton"),
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var stack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
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
    
    private Brush GetStatusBrush(ManifestStatus status)
    {
        return status switch
        {
            ManifestStatus.Draft => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
            ManifestStatus.Submitted => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            ManifestStatus.PendingApproval => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            ManifestStatus.Approved => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            ManifestStatus.Rejected => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
            ManifestStatus.InTransit => new SolidColorBrush(Color.FromRgb(0, 188, 212)),
            ManifestStatus.Shipped => new SolidColorBrush(Color.FromRgb(0, 188, 212)),
            ManifestStatus.PartiallyReceived => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            ManifestStatus.Received => new SolidColorBrush(Color.FromRgb(139, 195, 74)),
            ManifestStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            ManifestStatus.Cancelled => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
            _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
        };
    }
    
    #endregion
    
    #region Quick Action Handlers
    
    private void NewOutwardManifest_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.NewOutwardManifestCommand.Execute(null);
        _ = LoadManifestsAsync();
    }
    
    private void NewInwardManifest_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.NewInwardManifestCommand.Execute(null);
        _ = LoadManifestsAsync();
    }
    
    private void VerifyShipment_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.VerifyShipmentCommand.Execute(null);
        _ = LoadManifestsAsync();
    }
    
    #endregion
    
    #region Manifest Action Handlers
    
    private async void SubmitManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        var result = MessageBox.Show(
            $"Submit manifest {_selectedManifest.ManifestNumber} for approval?",
            "Submit Manifest",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            _selectedManifest.Status = ManifestStatus.Submitted;
            _selectedManifest.SubmittedDate = DateTime.UtcNow;
            await _dbService.UpdateManifestAsync(_selectedManifest);
            await LoadManifestsAsync();
            SelectManifest(_selectedManifest);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void EditManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        var dialog = new Dialogs.ManifestWizardDialog(_dbService, _selectedManifest.Type, _selectedManifest);
        dialog.Owner = Window.GetWindow(this);
        
        if (dialog.ShowDialog() == true)
        {
            _ = LoadManifestsAsync();
        }
    }
    
    private async void DeleteManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        var result = MessageBox.Show(
            $"Delete manifest {_selectedManifest.ManifestNumber}? This cannot be undone.",
            "Delete Manifest",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            await _dbService.DeleteManifestAsync(_selectedManifest.ManifestId);
            _selectedManifest = null;
            NoSelectionState.Visibility = Visibility.Visible;
            SelectedManifestDetails.Visibility = Visibility.Collapsed;
            await LoadManifestsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void ApproveManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        try
        {
            _selectedManifest.Status = ManifestStatus.Approved;
            _selectedManifest.ApprovedDate = DateTime.UtcNow;
            await _dbService.UpdateManifestAsync(_selectedManifest);
            await LoadManifestsAsync();
            SelectManifest(_selectedManifest);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void RejectManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        // Show rejection reason dialog
        var dialog = new Dialogs.RejectionReasonDialog();
        dialog.Owner = Window.GetWindow(this);
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            _selectedManifest.Status = ManifestStatus.Rejected;
            _selectedManifest.RejectionReason = dialog.RejectionReason;
            await _dbService.UpdateManifestAsync(_selectedManifest);
            await LoadManifestsAsync();
            SelectManifest(_selectedManifest);
            
            MessageBox.Show($"Manifest {_selectedManifest.ManifestNumber} has been rejected.",
                "Manifest Rejected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void ShipManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        // Show shipping details dialog
        var dialog = new Dialogs.ShippingDetailsDialog();
        dialog.Owner = Window.GetWindow(this);
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            _selectedManifest.Status = ManifestStatus.Shipped;
            _selectedManifest.ShippedDate = DateTime.UtcNow;
            _selectedManifest.ShippingMethod = dialog.ShippingMethod;
            _selectedManifest.CarrierName = dialog.CarrierName;
            _selectedManifest.TrackingNumber = dialog.TrackingNumber;
            _selectedManifest.ExpectedArrivalDate = dialog.ExpectedDeliveryDate;
            
            await _dbService.UpdateManifestAsync(_selectedManifest);
            await LoadManifestsAsync();
            SelectManifest(_selectedManifest);
            
            MessageBox.Show($"Manifest {_selectedManifest.ManifestNumber} has been marked as shipped.",
                "Shipment Confirmed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void StartVerification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        var dialog = new Dialogs.ShipmentVerificationDialog(_dbService, _selectedManifest);
        dialog.Owner = Window.GetWindow(this);
        
        if (dialog.ShowDialog() == true)
        {
            _ = LoadManifestsAsync();
            if (_selectedManifest != null)
                SelectManifest(_selectedManifest);
        }
    }
    
    private async void ReceiveManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        try
        {
            _selectedManifest.Status = ManifestStatus.Received;
            _selectedManifest.ReceivedDate = DateTime.UtcNow;
            await _dbService.UpdateManifestAsync(_selectedManifest);
            await LoadManifestsAsync();
            SelectManifest(_selectedManifest);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async void CompleteManifest_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        
        var result = MessageBox.Show(
            $"Complete manifest {_selectedManifest.ManifestNumber}?\n\n" +
            "This will update the location of all verified items to the destination location.",
            "Complete Manifest",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            // Update equipment locations for verified items
            if (_selectedManifest.ToLocationId.HasValue && _selectedManifest.Items?.Any() == true)
            {
                foreach (var item in _selectedManifest.Items.Where(i => 
                    i.EquipmentId.HasValue && 
                    (i.VerificationStatus == VerificationStatus.Verified || 
                     i.VerificationStatus == VerificationStatus.Pending))) // Include pending if not verified
                {
                    await _dbService.UpdateEquipmentLocationAsync(
                        item.EquipmentId!.Value, 
                        _selectedManifest.ToLocationId.Value);
                }
            }
            
            _selectedManifest.Status = ManifestStatus.Completed;
            _selectedManifest.CompletedDate = DateTime.UtcNow;
            await _dbService.UpdateManifestAsync(_selectedManifest);
            await LoadManifestsAsync();
            SelectManifest(_selectedManifest);
            
            MessageBox.Show($"Manifest {_selectedManifest.ManifestNumber} has been completed.\n" +
                "Equipment locations have been updated.",
                "Manifest Completed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ExportManifestPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedManifest == null) return;
        _mainViewModel.ExportManifestPdfCommand.Execute(null);
    }
    
    #endregion
    
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
