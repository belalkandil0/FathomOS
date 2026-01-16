using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels;
using FathomOS.Modules.EquipmentInventory.Views.Dialogs;

using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class DashboardView : System.Windows.Controls.UserControl
{
    private readonly DashboardViewModel _viewModel;
    
    public DashboardView(LocalDatabaseService dbService)
    {
        InitializeComponent();
        _viewModel = new DashboardViewModel(dbService);
        DataContext = _viewModel;
    }
    
    public void RefreshData()
    {
        _viewModel.RefreshData();
    }
}

public class DashboardViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    
    public DashboardViewModel(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        
        // Initialize commands
        NewEquipmentCommand = new RelayCommand(_ => OpenNewEquipmentDialog());
        NewOutwardManifestCommand = new RelayCommand(_ => OpenNewManifestDialog(ManifestType.Outward));
        NewInwardManifestCommand = new RelayCommand(_ => OpenNewManifestDialog(ManifestType.Inward));
        ScanQrCommand = new RelayCommand(_ => OpenScanDialog());
        RefreshCommand = new RelayCommand(_ => RefreshData());
        
        LoadDashboardData();
    }
    
    #region Properties
    
    public string WelcomeMessage => $"Welcome back!";
    public string CurrentDate => DateTime.Now.ToString("dddd, MMMM d, yyyy");
    
    private int _totalEquipment;
    public int TotalEquipment { get => _totalEquipment; set => SetProperty(ref _totalEquipment, value); }
    
    private int _availableCount;
    public int AvailableCount { get => _availableCount; set => SetProperty(ref _availableCount, value); }
    
    private int _activeManifests;
    public int ActiveManifests { get => _activeManifests; set => SetProperty(ref _activeManifests, value); }
    
    private int _inTransitCount;
    public int InTransitCount { get => _inTransitCount; set => SetProperty(ref _inTransitCount, value); }
    
    private int _certAlerts;
    public int CertAlerts { get => _certAlerts; set => SetProperty(ref _certAlerts, value); }
    
    private int _expiringSoonCount;
    public int ExpiringSoonCount { get => _expiringSoonCount; set => SetProperty(ref _expiringSoonCount, value); }
    
    private int _totalLocations;
    public int TotalLocations { get => _totalLocations; set => SetProperty(ref _totalLocations, value); }
    
    private int _vesselsCount;
    public int VesselsCount { get => _vesselsCount; set => SetProperty(ref _vesselsCount, value); }
    
    private int _alertCount;
    public int AlertCount { get => _alertCount; set => SetProperty(ref _alertCount, value); }
    
    private string _equipmentTrend = "+0";
    public string EquipmentTrend { get => _equipmentTrend; set => SetProperty(ref _equipmentTrend, value); }
    
    public ObservableCollection<ActivityItem> RecentActivity { get; } = new();
    public ObservableCollection<AlertItem> Alerts { get; } = new();
    
    #endregion
    
    #region Commands
    
    public ICommand NewEquipmentCommand { get; }
    public ICommand NewOutwardManifestCommand { get; }
    public ICommand NewInwardManifestCommand { get; }
    public ICommand ScanQrCommand { get; }
    public ICommand RefreshCommand { get; }
    
    #endregion
    
    #region Command Handlers
    
    private void OpenNewEquipmentDialog()
    {
        try
        {
            var dialog = new EquipmentEditorDialog(_dbService);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var equipment = dialog.GetEquipment();
                if (equipment != null)
                {
                    // Add to recent activity
                    RecentActivity.Insert(0, new ActivityItem
                    {
                        Icon = "PackageVariantClosed",
                        Title = "Equipment Added",
                        Description = $"{equipment.Name} ({equipment.AssetNumber}) added to inventory",
                        TimeAgo = "Just now"
                    });
                    
                    // Refresh stats
                    RefreshData();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening equipment dialog: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void OpenNewManifestDialog(ManifestType type)
    {
        try
        {
            var dialog = new ManifestWizardDialog(_dbService, type);
            dialog.Owner = Application.Current.MainWindow;
            
            if (dialog.ShowDialog() == true)
            {
                var manifest = dialog.GetManifest();
                if (manifest != null)
                {
                    RecentActivity.Insert(0, new ActivityItem
                    {
                        Icon = type == ManifestType.Outward ? "TruckDelivery" : "PackageDown",
                        Title = $"{type} Manifest Created",
                        Description = $"Manifest {manifest.ManifestNumber} created",
                        TimeAgo = "Just now"
                    });
                    
                    RefreshData();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening manifest dialog: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void OpenScanDialog()
    {
        MessageBox.Show("QR Scanner functionality will be available in the mobile app.\n\nUse the Equipment search to find items by asset number or barcode.", 
            "QR Scanner", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    #endregion
    
    public void RefreshData()
    {
        LoadDashboardData();
    }
    
    private async void LoadDashboardData()
    {
        try
        {
            // Load equipment stats
            var equipment = await _dbService.GetAllEquipmentAsync();
            TotalEquipment = equipment.Count;
            AvailableCount = equipment.Count(e => e.Status == EquipmentStatus.Available);
            InTransitCount = equipment.Count(e => e.Status == EquipmentStatus.InTransit);
            
            // Calculate trend (items added in last 7 days)
            var recentCount = equipment.Count(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-7));
            EquipmentTrend = recentCount > 0 ? $"+{recentCount}" : "0";
            
            // Load manifests
            var manifests = await _dbService.GetAllManifestsAsync();
            ActiveManifests = manifests.Count(m => m.Status == ManifestStatus.InTransit || 
                                                   m.Status == ManifestStatus.Draft ||
                                                   m.Status == ManifestStatus.Submitted);
            
            // Load certifications
            var certs = await _dbService.GetAllCertificationsAsync();
            var now = DateTime.UtcNow;
            CertAlerts = certs.Count(c => c.ExpiryDate < now.AddDays(30));
            ExpiringSoonCount = certs.Count(c => c.ExpiryDate >= now && c.ExpiryDate < now.AddDays(30));
            
            // Load locations
            var locations = await _dbService.GetAllLocationsAsync();
            TotalLocations = locations.Count;
            VesselsCount = locations.Count(l => l.Type == LocationType.Vessel);
            
            // Load real recent activity from audit log or recent changes
            await LoadRecentActivityAsync(equipment, manifests);
            
            // Build alerts from actual data
            BuildAlerts(equipment, certs);
            
            AlertCount = Alerts.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
        }
    }
    
    private async Task LoadRecentActivityAsync(List<Equipment> equipment, List<Manifest> manifests)
    {
        RecentActivity.Clear();
        
        // Get recent equipment (last 5 added)
        var recentEquipment = equipment
            .OrderByDescending(e => e.CreatedAt)
            .Take(3)
            .ToList();
            
        foreach (var eq in recentEquipment)
        {
            RecentActivity.Add(new ActivityItem
            {
                Icon = "PackageVariantClosed",
                Title = "Equipment Added",
                Description = $"{eq.Name} ({eq.AssetNumber})",
                TimeAgo = GetTimeAgo(eq.CreatedAt)
            });
        }
        
        // Get recent manifests
        var recentManifests = manifests
            .OrderByDescending(m => m.CreatedAt)
            .Take(3)
            .ToList();
            
        foreach (var m in recentManifests)
        {
            var icon = m.Type == ManifestType.Outward ? "TruckDelivery" : "PackageDown";
            var status = m.Status == ManifestStatus.Completed ? "Completed" : "Created";
            RecentActivity.Add(new ActivityItem
            {
                Icon = icon,
                Title = $"Manifest {status}",
                Description = $"{m.Type} manifest {m.ManifestNumber}",
                TimeAgo = GetTimeAgo(m.CreatedAt)
            });
        }
        
        // Sort all by time and take top 6
        var sorted = RecentActivity.OrderByDescending(a => ParseTimeAgo(a.TimeAgo)).Take(6).ToList();
        RecentActivity.Clear();
        foreach (var item in sorted)
        {
            RecentActivity.Add(item);
        }
        
        // If no activity, show welcome message
        if (RecentActivity.Count == 0)
        {
            RecentActivity.Add(new ActivityItem
            {
                Icon = "InformationOutline",
                Title = "Welcome!",
                Description = "Add equipment or create manifests to see activity here",
                TimeAgo = ""
            });
        }
    }
    
    private void BuildAlerts(List<Equipment> equipment, List<Certification> certs)
    {
        Alerts.Clear();
        
        // Certification alerts
        var expiringSoon = certs.Where(c => c.ExpiryDate >= DateTime.UtcNow && 
                                           c.ExpiryDate < DateTime.UtcNow.AddDays(30)).ToList();
        if (expiringSoon.Any())
        {
            Alerts.Add(new AlertItem
            {
                Icon = "CertificateOutline",
                Title = "Certifications Expiring",
                Message = $"{expiringSoon.Count} certifications expire within 30 days",
                Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D29922")!)
            });
        }
        
        // Expired certifications
        var expired = certs.Where(c => c.ExpiryDate < DateTime.UtcNow).ToList();
        if (expired.Any())
        {
            Alerts.Add(new AlertItem
            {
                Icon = "AlertCircle",
                Title = "Expired Certifications",
                Message = $"{expired.Count} certifications have expired",
                Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935")!)
            });
        }
        
        // Equipment needing maintenance
        var needsMaint = equipment.Where(e => e.NextServiceDate.HasValue && 
                                              e.NextServiceDate.Value < DateTime.UtcNow.AddDays(7)).ToList();
        if (needsMaint.Any())
        {
            Alerts.Add(new AlertItem
            {
                Icon = "WrenchOutline",
                Title = "Maintenance Due",
                Message = $"{needsMaint.Count} items need servicing soon",
                Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")!)
            });
        }
    }
    
    private string GetTimeAgo(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime;
        
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return dateTime.ToString("MMM d");
    }
    
    private int ParseTimeAgo(string timeAgo)
    {
        if (string.IsNullOrEmpty(timeAgo)) return 0;
        if (timeAgo == "Just now") return int.MaxValue;
        if (timeAgo.Contains("min")) return 1000;
        if (timeAgo.Contains("h ago")) return 100;
        if (timeAgo.Contains("d ago")) return 10;
        return 1;
    }
}

public class ActivityItem
{
    public string Icon { get; set; } = "Circle";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string TimeAgo { get; set; } = "";
}

public class AlertItem
{
    public string Icon { get; set; } = "Alert";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public Brush Color { get; set; } = Brushes.Orange;
}
