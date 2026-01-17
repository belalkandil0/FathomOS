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
/// View for reviewing and resolving manifest notifications (issues reported during verification).
/// </summary>
public partial class NotificationsView : UserControl, INotifyPropertyChanged
{
    private readonly LocalDatabaseService _dbService;
    private ManifestNotification? _selectedNotification;
    private string _searchText = "";
    private string _typeFilter = "All Types";
    private string _statusFilter = "Pending";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ObservableCollection<ManifestNotification> AllNotifications { get; } = new();
    public ObservableCollection<ManifestNotification> FilteredNotifications { get; } = new();
    
    public NotificationsView(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        
        // Load theme
        var themeUri = new Uri("/FathomOS.Modules.EquipmentInventory;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        DataContext = this;
        
        Loaded += async (s, e) => await LoadNotificationsAsync();
    }
    
    #region Data Loading
    
    private async Task LoadNotificationsAsync()
    {
        try
        {
            var notifications = await _dbService.GetManifestNotificationsAsync();
            
            AllNotifications.Clear();
            foreach (var notification in notifications.OrderByDescending(n => n.CreatedAt))
            {
                AllNotifications.Add(notification);
            }
            
            UpdateCounts();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
        }
    }
    
    private void UpdateCounts()
    {
        PendingCount.Text = AllNotifications.Count(n => !n.IsResolved).ToString();
        MissingCount.Text = AllNotifications.Count(n => 
            n.NotificationType.Equals("Missing", StringComparison.OrdinalIgnoreCase) && !n.IsResolved).ToString();
        DamagedCount.Text = AllNotifications.Count(n => 
            n.NotificationType.Equals("Damaged", StringComparison.OrdinalIgnoreCase) && !n.IsResolved).ToString();
        ResolvedCount.Text = AllNotifications.Count(n => n.IsResolved).ToString();
    }
    
    private void ApplyFilters()
    {
        var filtered = AllNotifications.AsEnumerable();
        
        // Apply status filter
        filtered = _statusFilter switch
        {
            "Pending" => filtered.Where(n => !n.IsResolved),
            "Resolved" => filtered.Where(n => n.IsResolved),
            _ => filtered
        };
        
        // Apply type filter
        if (_typeFilter != "All Types")
        {
            filtered = filtered.Where(n => 
                n.NotificationType.Equals(_typeFilter, StringComparison.OrdinalIgnoreCase));
        }
        
        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            var search = _searchText.ToLower();
            filtered = filtered.Where(n =>
                (n.Message?.ToLower().Contains(search) ?? false) ||
                (n.Details?.ToLower().Contains(search) ?? false) ||
                (n.Manifest?.ManifestNumber?.ToLower().Contains(search) ?? false));
        }
        
        FilteredNotifications.Clear();
        foreach (var notification in filtered)
        {
            FilteredNotifications.Add(notification);
        }
        
        NotificationsList.ItemsSource = FilteredNotifications;
        EmptyState.Visibility = FilteredNotifications.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        ApplyFilters();
    }
    
    private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeFilter.SelectedItem is ComboBoxItem item)
        {
            _typeFilter = item.Content?.ToString() ?? "All Types";
            ApplyFilters();
        }
    }
    
    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusFilter.SelectedItem is ComboBoxItem item)
        {
            _statusFilter = item.Content?.ToString() ?? "Pending";
            ApplyFilters();
        }
    }
    
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadNotificationsAsync();
    }
    
    private async void ResolveAll_Click(object sender, RoutedEventArgs e)
    {
        var pendingCount = AllNotifications.Count(n => !n.IsResolved);
        if (pendingCount == 0)
        {
            MessageBox.Show("No pending notifications to resolve.", "Info", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var result = MessageBox.Show(
            $"Mark all {pendingCount} pending notification(s) as resolved?",
            "Resolve All",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            foreach (var notification in AllNotifications.Where(n => !n.IsResolved))
            {
                await _dbService.ResolveNotificationAsync(notification.NotificationId, notes: "Bulk resolved");
            }
            
            await LoadNotificationsAsync();
            MessageBox.Show("All notifications resolved.", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void NotificationCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ManifestNotification notification)
        {
            SelectNotification(notification);
        }
    }
    
    private void SelectNotification(ManifestNotification notification)
    {
        _selectedNotification = notification;
        
        NoSelectionState.Visibility = Visibility.Collapsed;
        SelectedNotificationDetails.Visibility = Visibility.Visible;
        
        // Populate details
        DetailType.Text = notification.NotificationType.ToUpper();
        DetailTypeBadge.Background = GetTypeBrush(notification.NotificationType);
        DetailMessage.Text = notification.Message;
        
        // Status badge
        if (notification.IsResolved)
        {
            DetailStatusBadge.Visibility = Visibility.Visible;
            DetailStatusBadge.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            DetailStatus.Text = "Resolved";
        }
        else
        {
            DetailStatusBadge.Visibility = Visibility.Collapsed;
        }
        
        // Details
        if (!string.IsNullOrWhiteSpace(notification.Details))
        {
            DetailsPanel.Visibility = Visibility.Visible;
            DetailDetails.Text = notification.Details;
        }
        else
        {
            DetailsPanel.Visibility = Visibility.Collapsed;
        }
        
        // Source
        DetailManifest.Text = notification.Manifest?.ManifestNumber ?? "-";
        DetailLocation.Text = notification.Location?.Name ?? "-";
        
        // Equipment (if linked)
        if (notification.Equipment != null)
        {
            EquipmentPanel.Visibility = Visibility.Visible;
            DetailEquipmentName.Text = notification.Equipment.Name;
            DetailEquipmentAsset.Text = notification.Equipment.AssetNumber;
        }
        else
        {
            EquipmentPanel.Visibility = Visibility.Collapsed;
        }
        
        // Dates
        DetailCreatedAt.Text = notification.CreatedAt.ToString("MMM dd, yyyy HH:mm");
        DetailResolvedAt.Text = notification.ResolvedAt?.ToString("MMM dd, yyyy HH:mm") ?? "-";
        
        // Resolution notes
        if (notification.IsResolved && !string.IsNullOrWhiteSpace(notification.ResolutionNotes))
        {
            ResolutionPanel.Visibility = Visibility.Visible;
            DetailResolutionNotes.Text = notification.ResolutionNotes;
        }
        else
        {
            ResolutionPanel.Visibility = Visibility.Collapsed;
        }
        
        // Update action buttons
        UpdateActionButtons(notification);
    }
    
    private void UpdateActionButtons(ManifestNotification notification)
    {
        ActionButtons.Children.Clear();
        
        if (!notification.IsResolved)
        {
            AddActionButton("Resolve", "Check", ResolveNotification_Click);
            AddActionButton("Add Note", "CommentTextOutline", AddNote_Click, isSecondary: true);
        }
        else
        {
            AddActionButton("Reopen", "Undo", ReopenNotification_Click, isSecondary: true);
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
    
    private Brush GetTypeBrush(string notificationType)
    {
        return notificationType.ToLowerInvariant() switch
        {
            "missing" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
            "damaged" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            "extra" => new SolidColorBrush(Color.FromRgb(23, 162, 184)),
            "wrong" => new SolidColorBrush(Color.FromRgb(255, 128, 0)),
            "discrepancy" => new SolidColorBrush(Color.FromRgb(111, 66, 193)),
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))
        };
    }
    
    #endregion
    
    #region Action Handlers
    
    private async void ResolveNotification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedNotification == null) return;
        
        // Show resolution dialog
        var dialog = new Dialogs.ResolveNotificationDialog();
        dialog.Owner = Window.GetWindow(this);
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _dbService.ResolveNotificationAsync(
                    _selectedNotification.NotificationId, 
                    notes: dialog.ResolutionNotes);
                
                await LoadNotificationsAsync();
                MessageBox.Show("Notification resolved.", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedNotification == null) return;
        
        // For now, just show a message - could add a note dialog later
        MessageBox.Show("Note functionality will be added in a future update.", 
            "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private async void ReopenNotification_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedNotification == null) return;
        
        var result = MessageBox.Show(
            "Reopen this notification?",
            "Reopen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            _selectedNotification.IsResolved = false;
            _selectedNotification.ResolvedAt = null;
            _selectedNotification.ResolvedBy = null;
            _selectedNotification.ResolutionNotes = null;
            
            await _dbService.SaveManifestNotificationAsync(_selectedNotification);
            await LoadNotificationsAsync();
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
