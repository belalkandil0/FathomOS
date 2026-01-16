using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.Data;

namespace FathomOS.Modules.EquipmentInventory.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly DashboardService _dashboardService;
    private bool _isBusy;
    private DateTime _lastUpdated;
    private DashboardSummary? _summary;
    
    public DashboardViewModel()
    {
        var dbService = new LocalDatabaseService();
        _dashboardService = new DashboardService(dbService);
        
        StatusChartData = new ObservableCollection<ChartItem>();
        CategoryChartData = new ObservableCollection<ChartItem>();
        Alerts = new ObservableCollection<AlertItem>();
        RecentActivity = new ObservableCollection<ActivityItem>();
        
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        ViewAlertCommand = new RelayCommand(ViewAlert);
        
        _ = LoadDataAsync();
    }
    
    #region Properties
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public DateTime LastUpdated
    {
        get => _lastUpdated;
        set => SetProperty(ref _lastUpdated, value);
    }
    
    public DashboardSummary? Summary
    {
        get => _summary;
        set => SetProperty(ref _summary, value);
    }
    
    public ObservableCollection<ChartItem> StatusChartData { get; }
    public ObservableCollection<ChartItem> CategoryChartData { get; }
    public ObservableCollection<AlertItem> Alerts { get; }
    public ObservableCollection<ActivityItem> RecentActivity { get; }
    
    #endregion
    
    #region Commands
    
    public ICommand RefreshCommand { get; }
    public ICommand ViewAlertCommand { get; }
    
    #endregion
    
    #region Methods
    
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            
            // Load summary
            Summary = await _dashboardService.GetDashboardSummaryAsync();
            
            // Load status chart data
            var statusData = await _dashboardService.GetEquipmentByStatusAsync();
            StatusChartData.Clear();
            double maxStatus = statusData.Max(s => s.Value);
            foreach (var item in statusData)
            {
                StatusChartData.Add(new ChartItem
                {
                    Label = item.Label,
                    Value = (int)item.Value,
                    Color = item.Color,
                    BarWidth = maxStatus > 0 ? (item.Value / maxStatus) * 200 : 0
                });
            }
            
            // Load category chart data
            var categoryData = await _dashboardService.GetEquipmentByCategoryAsync();
            CategoryChartData.Clear();
            double maxCategory = categoryData.Any() ? categoryData.Max(c => c.Value) : 0;
            foreach (var item in categoryData.Take(8))
            {
                CategoryChartData.Add(new ChartItem
                {
                    Label = item.Label,
                    Value = (int)item.Value,
                    Color = "#4A90D9",
                    BarWidth = maxCategory > 0 ? (item.Value / maxCategory) * 180 : 0
                });
            }
            
            // Load alerts
            var alerts = await _dashboardService.GetEquipmentAlertsAsync();
            Alerts.Clear();
            foreach (var alert in alerts.Take(10))
            {
                Alerts.Add(alert);
            }
            
            // Load recent activity
            var activity = await _dashboardService.GetRecentActivityAsync(10);
            RecentActivity.Clear();
            foreach (var item in activity)
            {
                RecentActivity.Add(item);
            }
            
            LastUpdated = DateTime.Now;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void ViewAlert(object? parameter)
    {
        if (parameter is AlertItem alert)
        {
            // Navigate to equipment details
            System.Diagnostics.Debug.WriteLine($"View alert for: {alert.AssetNumber}");
        }
    }
    
    #endregion
}

public class ChartItem
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = "#4A90D9";
    public double BarWidth { get; set; }
}
