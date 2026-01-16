using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

/// <summary>
/// Equipment Failure Notification (EFN) Editor Dialog
/// Based on Subsea7 Form FO-GL-ITS-EQP-003
/// </summary>
public partial class DefectReportEditorDialog : MetroWindow
{
    private readonly LocalDatabaseService _dbService;
    private readonly DefectReport _report;
    private readonly Guid _userId;
    private readonly bool _isNew;
    private ObservableCollection<DefectReportPart> _parts;
    
    public DefectReport? SavedReport { get; private set; }
    
    public DefectReportEditorDialog(LocalDatabaseService dbService, DefectReport? report, Guid userId)
    {
        _dbService = dbService;
        _userId = userId;
        _isNew = report == null;
        _report = report ?? new DefectReport();
        _parts = new ObservableCollection<DefectReportPart>();
        
        // Load theme
        var themeService = new ThemeService();
        var settings = ModuleSettings.Load();
        themeService.ApplyTheme(settings.UseDarkTheme ? "Dark" : "Light");
        
        InitializeComponent();
        
        LoadComboBoxData();
        LoadReportData();
    }
    
    private async void LoadComboBoxData()
    {
        try
        {
            // Load locations
            var locations = await _dbService.GetLocationsAsync();
            LocationComboBox.ItemsSource = locations;
            
            // Load categories
            var categories = await _dbService.GetCategoriesAsync();
            CategoryComboBox.ItemsSource = categories;
            
            // Load fault categories
            FaultCategoryComboBox.ItemsSource = Enum.GetValues<FaultCategory>();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading data: {ex.Message}";
        }
    }
    
    private async void LoadReportData()
    {
        if (_isNew)
        {
            // New report
            ReportNumberTextBox.Text = "(Auto-generated on save)";
            ReportDatePicker.SelectedDate = DateTime.Today;
            Title = "New Equipment Failure Notification";
        }
        else
        {
            // Existing report
            Title = $"Edit EFN - {_report.ReportNumber}";
            ReportNumberTextBox.Text = _report.ReportNumber;
            ReportDatePicker.SelectedDate = _report.ReportDate;
            ClientProjectTextBox.Text = _report.ClientProject;
            
            if (_report.LocationId.HasValue)
            {
                var locations = LocationComboBox.ItemsSource as IEnumerable<Location>;
                LocationComboBox.SelectedItem = locations?.FirstOrDefault(l => l.LocationId == _report.LocationId);
            }
            
            RovSystemTextBox.Text = _report.RovSystem;
            WaterDepthTextBox.Text = _report.WorkingWaterDepthMetres?.ToString();
            
            // Equipment Origin
            SetEquipmentOrigin(_report.EquipmentOrigin);
            
            // Category
            if (_report.EquipmentCategoryId.HasValue)
            {
                var categories = CategoryComboBox.ItemsSource as IEnumerable<EquipmentCategory>;
                CategoryComboBox.SelectedItem = categories?.FirstOrDefault(c => c.CategoryId == _report.EquipmentCategoryId);
            }
            
            MajorComponentTextBox.Text = _report.MajorComponent;
            MinorComponentTextBox.Text = _report.MinorComponent;
            
            // Equipment Tab
            OwnerInternal.IsChecked = _report.OwnershipType == EquipmentOwnership.Internal;
            OwnerExternal.IsChecked = _report.OwnershipType == EquipmentOwnership.External;
            ResponsibilityStandard.IsChecked = _report.ResponsibilityType == ResponsibilityType.Standard;
            ResponsibilityProject.IsChecked = _report.ResponsibilityType == ResponsibilityType.Project;
            EquipmentOwnerTextBox.Text = _report.EquipmentOwner;
            SapIdTextBox.Text = _report.SapIdOrVendorAssetId;
            SerialNumberTextBox.Text = _report.SerialNumber;
            ManufacturerTextBox.Text = _report.Manufacturer;
            ModelTextBox.Text = _report.Model;
            
            // Symptoms Tab
            FaultCategoryComboBox.SelectedItem = _report.FaultCategory;
            SymptomsTextBox.Text = _report.DetailedSymptoms;
            PhotosYes.IsChecked = _report.PhotosAttached;
            PhotosNo.IsChecked = !_report.PhotosAttached;
            ActionTakenTextBox.Text = _report.ActionTaken;
            PartsYes.IsChecked = _report.PartsAvailableOnBoard;
            PartsNo.IsChecked = !_report.PartsAvailableOnBoard;
            ReplacementYes.IsChecked = _report.ReplacementRequired;
            ReplacementNo.IsChecked = !_report.ReplacementRequired;
            
            foreach (ComboBoxItem item in UrgencyComboBox.Items)
            {
                if (item.Tag?.ToString() == _report.ReplacementUrgency.ToString())
                {
                    UrgencyComboBox.SelectedItem = item;
                    break;
                }
            }
            
            CommentsTextBox.Text = _report.FurtherComments;
            NextPortCallDatePicker.SelectedDate = _report.NextPortCallDate;
            NextPortCallLocationTextBox.Text = _report.NextPortCallLocation;
            RepairDurationTextBox.Text = _report.RepairDurationMinutes?.ToString();
            DowntimeTextBox.Text = _report.DowntimeDurationMinutes?.ToString();
            
            // Load parts
            var parts = await _dbService.GetDefectReportPartsAsync(_report.DefectReportId);
            foreach (var part in parts)
                _parts.Add(part);
        }
        
        PartsDataGrid.ItemsSource = _parts;
    }
    
    private void SetEquipmentOrigin(string? origin)
    {
        if (string.IsNullOrEmpty(origin)) return;
        
        switch (origin)
        {
            case "Modular Handling System": OriginMHS.IsChecked = true; break;
            case "ROV": OriginROV.IsChecked = true; break;
            case "Simulator": OriginSimulator.IsChecked = true; break;
            case "Tooling": OriginTooling.IsChecked = true; break;
            case "Vessel / Rig": OriginVessel.IsChecked = true; break;
            case "Survey & Inspection": OriginSurvey.IsChecked = true; break;
        }
    }
    
    private string? GetSelectedEquipmentOrigin()
    {
        if (OriginMHS.IsChecked == true) return "Modular Handling System";
        if (OriginROV.IsChecked == true) return "ROV";
        if (OriginSimulator.IsChecked == true) return "Simulator";
        if (OriginTooling.IsChecked == true) return "Tooling";
        if (OriginVessel.IsChecked == true) return "Vessel / Rig";
        if (OriginSurvey.IsChecked == true) return "Survey & Inspection";
        return null;
    }
    
    private void PopulateReportFromForm()
    {
        _report.ReportDate = ReportDatePicker.SelectedDate ?? DateTime.Today;
        _report.ClientProject = ClientProjectTextBox.Text;
        _report.LocationId = (LocationComboBox.SelectedItem as Location)?.LocationId;
        _report.RovSystem = RovSystemTextBox.Text;
        
        if (double.TryParse(WaterDepthTextBox.Text, out double depth))
            _report.WorkingWaterDepthMetres = depth;
        
        _report.EquipmentOrigin = GetSelectedEquipmentOrigin();
        _report.EquipmentCategoryId = (CategoryComboBox.SelectedItem as EquipmentCategory)?.CategoryId;
        _report.MajorComponent = MajorComponentTextBox.Text;
        _report.MinorComponent = MinorComponentTextBox.Text;
        
        // Equipment Tab
        _report.OwnershipType = OwnerInternal.IsChecked == true ? EquipmentOwnership.Internal : EquipmentOwnership.External;
        _report.ResponsibilityType = ResponsibilityStandard.IsChecked == true ? ResponsibilityType.Standard : ResponsibilityType.Project;
        _report.EquipmentOwner = EquipmentOwnerTextBox.Text;
        _report.SapIdOrVendorAssetId = SapIdTextBox.Text;
        _report.SerialNumber = SerialNumberTextBox.Text;
        _report.Manufacturer = ManufacturerTextBox.Text;
        _report.Model = ModelTextBox.Text;
        
        // Symptoms Tab
        if (FaultCategoryComboBox.SelectedItem is FaultCategory fc)
            _report.FaultCategory = fc;
        _report.DetailedSymptoms = SymptomsTextBox.Text;
        _report.PhotosAttached = PhotosYes.IsChecked == true;
        _report.ActionTaken = ActionTakenTextBox.Text;
        _report.PartsAvailableOnBoard = PartsYes.IsChecked == true;
        _report.ReplacementRequired = ReplacementYes.IsChecked == true;
        
        if (UrgencyComboBox.SelectedItem is ComboBoxItem urgencyItem)
        {
            if (Enum.TryParse<ReplacementUrgency>(urgencyItem.Tag?.ToString(), out var urgency))
                _report.ReplacementUrgency = urgency;
        }
        
        _report.FurtherComments = CommentsTextBox.Text;
        _report.NextPortCallDate = NextPortCallDatePicker.SelectedDate;
        _report.NextPortCallLocation = NextPortCallLocationTextBox.Text;
        
        if (int.TryParse(RepairDurationTextBox.Text, out int repairDuration))
            _report.RepairDurationMinutes = repairDuration;
        
        if (int.TryParse(DowntimeTextBox.Text, out int downtime))
            _report.DowntimeDurationMinutes = downtime;
    }
    
    private bool ValidateForm()
    {
        // Required fields
        if (FaultCategoryComboBox.SelectedItem == null)
        {
            MessageBox.Show("Please select a fault category.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(MajorComponentTextBox.Text))
        {
            MessageBox.Show("Please enter the major component.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        return true;
    }
    
    private void AddPart_Click(object sender, RoutedEventArgs e)
    {
        var newPart = new DefectReportPart
        {
            LineNumber = _parts.Count + 1
        };
        _parts.Add(newPart);
    }
    
    private void RemovePart_Click(object sender, RoutedEventArgs e)
    {
        if (PartsDataGrid.SelectedItem is DefectReportPart selectedPart)
        {
            _parts.Remove(selectedPart);
            // Renumber parts
            for (int i = 0; i < _parts.Count; i++)
                _parts[i].LineNumber = i + 1;
        }
    }
    
    private async void SaveDraft_Click(object sender, RoutedEventArgs e)
    {
        await SaveReportAsync(DefectReportStatus.Draft);
    }
    
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm()) return;
        await SaveReportAsync(_report.Status);
    }
    
    private async Task SaveReportAsync(DefectReportStatus status)
    {
        try
        {
            PopulateReportFromForm();
            _report.Status = status;
            
            if (_isNew)
            {
                SavedReport = await _dbService.CreateDefectReportAsync(_report, _userId);
            }
            else
            {
                SavedReport = await _dbService.UpdateDefectReportAsync(_report, _userId);
            }
            
            // Save parts
            if (SavedReport != null)
            {
                await _dbService.SaveDefectReportPartsAsync(SavedReport.DefectReportId, _parts.ToList());
            }
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
