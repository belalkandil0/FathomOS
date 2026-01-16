using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.ViewModels;

public class EquipmentHistoryViewModel : ViewModelBase
{
    private readonly EquipmentHistoryService _historyService;
    private readonly Guid _equipmentId;
    
    private string _assetNumber;
    private string _equipmentName;
    private string _selectedActionFilter = "All";
    private DateTime? _startDate;
    private DateTime? _endDate;
    private bool _isEmpty;
    
    public EquipmentHistoryViewModel(Guid equipmentId, string assetNumber, string equipmentName)
    {
        _equipmentId = equipmentId;
        _assetNumber = assetNumber;
        _equipmentName = equipmentName;
        
        var dbService = new LocalDatabaseService();
        _historyService = new EquipmentHistoryService(dbService);
        
        HistoryItems = new ObservableCollection<EquipmentHistoryItem>();
        ActionFilters = new ObservableCollection<string> 
        { 
            "All", "Created", "Updated", "StatusChanged", "Transferred", 
            "Certified", "Calibrated", "Serviced", "PhotoAdded", "DocumentAdded" 
        };
        
        ExportCommand = new RelayCommand(_ => Export());
        
        _ = LoadHistoryAsync();
    }
    
    #region Properties
    
    public string AssetNumber
    {
        get => _assetNumber;
        set => SetProperty(ref _assetNumber, value);
    }
    
    public string EquipmentName
    {
        get => _equipmentName;
        set => SetProperty(ref _equipmentName, value);
    }
    
    public string SelectedActionFilter
    {
        get => _selectedActionFilter;
        set { if (SetProperty(ref _selectedActionFilter, value)) _ = ApplyFiltersAsync(); }
    }
    
    public DateTime? StartDate
    {
        get => _startDate;
        set { if (SetProperty(ref _startDate, value)) _ = ApplyFiltersAsync(); }
    }
    
    public DateTime? EndDate
    {
        get => _endDate;
        set { if (SetProperty(ref _endDate, value)) _ = ApplyFiltersAsync(); }
    }
    
    public bool IsEmpty
    {
        get => _isEmpty;
        set => SetProperty(ref _isEmpty, value);
    }
    
    public ObservableCollection<EquipmentHistoryItem> HistoryItems { get; }
    public ObservableCollection<string> ActionFilters { get; }
    
    #endregion
    
    #region Commands
    
    public ICommand ExportCommand { get; }
    
    #endregion
    
    #region Methods
    
    private List<EquipmentHistoryItem> _allItems = new();
    
    private async Task LoadHistoryAsync()
    {
        _allItems = await _historyService.GetEquipmentHistoryAsync(_equipmentId);
        await ApplyFiltersAsync();
    }
    
    private Task ApplyFiltersAsync()
    {
        var filtered = _allItems.AsEnumerable();
        
        if (SelectedActionFilter != "All" && Enum.TryParse<HistoryAction>(SelectedActionFilter, out var action))
        {
            filtered = filtered.Where(h => h.Action == action);
        }
        
        if (StartDate.HasValue)
        {
            filtered = filtered.Where(h => h.PerformedAt >= StartDate.Value);
        }
        
        if (EndDate.HasValue)
        {
            filtered = filtered.Where(h => h.PerformedAt <= EndDate.Value.AddDays(1));
        }
        
        HistoryItems.Clear();
        foreach (var item in filtered.OrderByDescending(h => h.PerformedAt))
        {
            HistoryItems.Add(item);
        }
        
        IsEmpty = HistoryItems.Count == 0;
        
        return Task.CompletedTask;
    }
    
    private void Export()
    {
        if (!HistoryItems.Any())
        {
            MessageBox.Show("No history items to export.", "Export", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var saveDialog = new SaveFileDialog
        {
            Title = "Export Equipment History",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"History_{AssetNumber}_{DateTime.Now:yyyyMMdd}"
        };
        
        if (saveDialog.ShowDialog() != true) return;
        
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Equipment History");
            
            // Title
            worksheet.Cell(1, 1).Value = $"Equipment History - {AssetNumber}";
            worksheet.Range(1, 1, 1, 6).Merge();
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            
            worksheet.Cell(2, 1).Value = $"Equipment: {EquipmentName}";
            worksheet.Range(2, 1, 2, 6).Merge();
            
            worksheet.Cell(3, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
            worksheet.Range(3, 1, 3, 6).Merge();
            
            // Headers
            var headerRow = 5;
            worksheet.Cell(headerRow, 1).Value = "Date/Time";
            worksheet.Cell(headerRow, 2).Value = "Action";
            worksheet.Cell(headerRow, 3).Value = "Description";
            worksheet.Cell(headerRow, 4).Value = "Performed By";
            worksheet.Cell(headerRow, 5).Value = "Old Value";
            worksheet.Cell(headerRow, 6).Value = "New Value";
            
            var headers = worksheet.Range(headerRow, 1, headerRow, 6);
            headers.Style.Font.Bold = true;
            headers.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            headers.Style.Font.FontColor = XLColor.White;
            
            // Data
            var row = headerRow + 1;
            foreach (var item in HistoryItems)
            {
                worksheet.Cell(row, 1).Value = item.PerformedAt.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cell(row, 2).Value = item.Action.ToString();
                worksheet.Cell(row, 3).Value = item.Description ?? "";
                worksheet.Cell(row, 4).Value = item.PerformedByName ?? "";
                worksheet.Cell(row, 5).Value = item.OldValue ?? "";
                worksheet.Cell(row, 6).Value = item.NewValue ?? "";
                row++;
            }
            
            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
            
            workbook.SaveAs(saveDialog.FileName);
            
            MessageBox.Show($"History exported successfully to:\n{saveDialog.FileName}", 
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting history: {ex.Message}", 
                "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
}
