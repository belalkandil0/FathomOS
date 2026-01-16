using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// ViewModel for Position Fixes view.
/// Handles display, filtering, and management of position fixes including calibrations and waypoints.
/// </summary>
public class PositionFixesViewModel : ViewModelBase
{
    #region Private Fields

    private readonly ObservableCollection<PositionFix> _positionFixes = new();
    private readonly ICollectionView _positionFixesView;
    private PositionFix? _selectedPositionFix;
    private string _filterText = "";
    private string _selectedFixType = "All";
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;

    #endregion

    #region Constructor

    public PositionFixesViewModel()
    {
        _positionFixesView = CollectionViewSource.GetDefaultView(_positionFixes);
        _positionFixesView.Filter = FilterFix;

        // Initialize fix types
        FixTypes = new ObservableCollection<string>
        {
            "All",
            "SetEastingNorthing",
            "Waypoint",
            "Calibration",
            "Manual"
        };

        // Initialize commands
        AddPositionFixCommand = new RelayCommand(_ => AddPositionFix());
        ExportFixesCommand = new RelayCommand(_ => ExportFixes(), _ => _positionFixes.Count > 0);
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
        DeleteFixCommand = new RelayCommand(param => DeleteFix(param as PositionFix), _ => SelectedPositionFix != null);
    }

    #endregion

    #region Properties

    public ICollectionView PositionFixesView => _positionFixesView;

    public ObservableCollection<string> FixTypes { get; }

    public PositionFix? SelectedPositionFix
    {
        get => _selectedPositionFix;
        set => SetProperty(ref _selectedPositionFix, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                _positionFixesView.Refresh();
        }
    }

    public string SelectedFixType
    {
        get => _selectedFixType;
        set
        {
            if (SetProperty(ref _selectedFixType, value))
                _positionFixesView.Refresh();
        }
    }

    public DateTime? FilterStartDate
    {
        get => _filterStartDate;
        set
        {
            if (SetProperty(ref _filterStartDate, value))
                _positionFixesView.Refresh();
        }
    }

    public DateTime? FilterEndDate
    {
        get => _filterEndDate;
        set
        {
            if (SetProperty(ref _filterEndDate, value))
                _positionFixesView.Refresh();
        }
    }

    public int TotalFixes => _positionFixes.Count;

    public int SetEastingNorthingCount => _positionFixes.Count(f => 
        f.PositionFixType == Models.PositionFixType.SetEastingNorthing);

    public int WaypointCount => _positionFixes.Count(f => 
        f.PositionFixType == Models.PositionFixType.Waypoint);

    public int CalibrationCount => _positionFixes.Count(f => 
        f.PositionFixType == Models.PositionFixType.Calibration);

    #endregion

    #region Commands

    public ICommand AddPositionFixCommand { get; }
    public ICommand ExportFixesCommand { get; }
    public ICommand ClearFilterCommand { get; }
    public ICommand DeleteFixCommand { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads position fixes from an existing collection.
    /// </summary>
    public void LoadFixes(IEnumerable<PositionFix> fixes)
    {
        _positionFixes.Clear();
        foreach (var fix in fixes)
        {
            _positionFixes.Add(fix);
        }
        
        UpdateStatistics();
    }

    /// <summary>
    /// Adds a new position fix to the collection.
    /// </summary>
    public void AddFix(PositionFix fix)
    {
        _positionFixes.Add(fix);
        UpdateStatistics();
    }

    /// <summary>
    /// Gets all position fixes.
    /// </summary>
    public IEnumerable<PositionFix> GetAllFixes()
    {
        return _positionFixes.ToList();
    }

    /// <summary>
    /// Refreshes the position fixes view.
    /// </summary>
    public void RefreshFixes()
    {
        _positionFixesView.Refresh();
        UpdateStatistics();
    }

    /// <summary>
    /// Clears all position fixes.
    /// </summary>
    public void ClearFixes()
    {
        _positionFixes.Clear();
        UpdateStatistics();
    }

    #endregion

    #region Private Methods

    private bool FilterFix(object obj)
    {
        if (obj is not PositionFix fix)
            return false;

        // Fix type filter
        if (SelectedFixType != "All")
        {
            if (!Enum.TryParse<Models.PositionFixType>(SelectedFixType, out var fixType))
                return false;
            if (fix.PositionFixType != fixType)
                return false;
        }

        // Text filter
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var searchText = FilterText.ToLower();
            var matchesText = fix.ObjectName?.ToLower().Contains(searchText) == true ||
                              fix.Description?.ToLower().Contains(searchText) == true;
            
            if (!matchesText) return false;
        }

        // Date range filter
        if (FilterStartDate.HasValue && fix.Date < FilterStartDate.Value.Date)
            return false;

        if (FilterEndDate.HasValue && fix.Date > FilterEndDate.Value.Date)
            return false;

        return true;
    }

    private void AddPositionFix()
    {
        // Open dialog to add new position fix
        var dialog = new Views.PositionFixDialog();
        if (dialog.ShowDialog() == true && dialog.CreatedFix != null)
        {
            AddFix(dialog.CreatedFix);
        }
    }

    private void DeleteFix(PositionFix? fix)
    {
        if (fix == null) return;

        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to delete this position fix?",
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _positionFixes.Remove(fix);
            UpdateStatistics();
        }
    }

    private void ExportFixes()
    {
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Position Fixes",
                Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                FileName = $"Position_Fixes_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() != true) return;

            var fixes = _positionFixesView.Cast<PositionFix>().ToList();
            
            if (saveDialog.FilterIndex == 1) // Excel
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Position Fixes");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Timestamp";
                worksheet.Cell(1, 2).Value = "Fix Type";
                worksheet.Cell(1, 3).Value = "Name";
                worksheet.Cell(1, 4).Value = "Required Easting";
                worksheet.Cell(1, 5).Value = "Required Northing";
                worksheet.Cell(1, 6).Value = "Computed Easting";
                worksheet.Cell(1, 7).Value = "Computed Northing";
                worksheet.Cell(1, 8).Value = "Error Easting";
                worksheet.Cell(1, 9).Value = "Error Northing";
                worksheet.Cell(1, 10).Value = "Source";
                worksheet.Cell(1, 11).Value = "Description";
                
                // Style header
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGreen;
                
                // Data
                for (int i = 0; i < fixes.Count; i++)
                {
                    var fix = fixes[i];
                    worksheet.Cell(i + 2, 1).Value = fix.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    worksheet.Cell(i + 2, 2).Value = fix.FixType.ToString();
                    worksheet.Cell(i + 2, 3).Value = fix.Name;
                    worksheet.Cell(i + 2, 4).Value = fix.RequiredEasting;
                    worksheet.Cell(i + 2, 5).Value = fix.RequiredNorthing;
                    worksheet.Cell(i + 2, 6).Value = fix.ComputedEasting;
                    worksheet.Cell(i + 2, 7).Value = fix.ComputedNorthing;
                    worksheet.Cell(i + 2, 8).Value = fix.ErrorEasting;
                    worksheet.Cell(i + 2, 9).Value = fix.ErrorNorthing;
                    worksheet.Cell(i + 2, 10).Value = fix.Source;
                    worksheet.Cell(i + 2, 11).Value = fix.Description;
                }
                
                // Format number columns
                worksheet.Columns(4, 9).Style.NumberFormat.Format = "0.000";
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(saveDialog.FileName);
            }
            else // CSV
            {
                var lines = new List<string>
                {
                    "Timestamp,Fix Type,Name,Required Easting,Required Northing,Computed Easting,Computed Northing,Error Easting,Error Northing,Source,Description"
                };
                
                foreach (var fix in fixes)
                {
                    lines.Add($"\"{fix.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\",\"{fix.FixType}\",\"{fix.Name}\",{fix.RequiredEasting:F3},{fix.RequiredNorthing:F3},{fix.ComputedEasting:F3},{fix.ComputedNorthing:F3},{fix.ErrorEasting:F3},{fix.ErrorNorthing:F3},\"{fix.Source}\",\"{fix.Description}\"");
                }
                
                File.WriteAllLines(saveDialog.FileName, lines);
            }
            
            System.Windows.MessageBox.Show($"Exported {fixes.Count} position fixes to:\n{saveDialog.FileName}",
                "Export Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Export failed: {ex.Message}",
                "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ClearFilter()
    {
        FilterText = "";
        SelectedFixType = "All";
        FilterStartDate = null;
        FilterEndDate = null;
    }

    private void UpdateStatistics()
    {
        OnPropertyChanged(nameof(TotalFixes));
        OnPropertyChanged(nameof(SetEastingNorthingCount));
        OnPropertyChanged(nameof(WaypointCount));
        OnPropertyChanged(nameof(CalibrationCount));
    }

    #endregion
}
