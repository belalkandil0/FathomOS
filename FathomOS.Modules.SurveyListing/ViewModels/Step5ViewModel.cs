using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using FathomOS.Core.Models;
using FathomOS.Core.Parsers;
using FathomOS.Modules.SurveyListing.Services;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace FathomOS.Modules.SurveyListing.ViewModels;

/// <summary>
/// ViewModel for Step 5: Tide & Corrections
/// </summary>
public class Step5ViewModel : INotifyPropertyChanged
{
    private readonly TideParser _tideParser;
    private string _tideFilePath = string.Empty;
    private TideData? _tideData;
    private bool _isTideLoaded;
    private bool _useFeetForTide;
    private bool _applyTidalCorrections = false;  // Default unchecked
    private bool _applyVerticalOffsets = false;   // Default unchecked
    private double? _bathyToAltimeterOffset = null;  // Default blank
    private double? _bathyToRovRefOffset = null;     // Default blank
    private double _depthExaggeration = 10.0;
    private string _statusMessage = "No tide file loaded (optional)";

    public Step5ViewModel(Project project)
    {
        _tideParser = new TideParser();
        SurveyFixes = new ObservableCollection<SurveyFix>();
        LoadProject(project);
    }

    // Tide file
    public string TideFilePath
    {
        get => _tideFilePath;
        set { _tideFilePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(TideFileName)); }
    }

    public string TideFileName => string.IsNullOrEmpty(_tideFilePath) ? "None" : Path.GetFileName(_tideFilePath);

    public TideData? TideData
    {
        get => _tideData;
        private set { _tideData = value; OnPropertyChanged(); UpdateTideInfo(); }
    }

    public bool IsTideLoaded
    {
        get => _isTideLoaded;
        private set { _isTideLoaded = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    // Tide info properties
    public string TideTimeRange => TideData?.StartTime.HasValue == true
        ? $"{TideData.StartTime:yyyy-MM-dd HH:mm} to {TideData.EndTime:yyyy-MM-dd HH:mm}"
        : "-";

    public string TideRecordCount => TideData != null ? $"{TideData.RecordCount:N0} records" : "-";

    public string TideRange
    {
        get
        {
            if (TideData == null) return "-";
            var stats = TideData.GetStatistics();
            return UseFeetForTide
                ? $"{stats.MinTideFeet:F3} to {stats.MaxTideFeet:F3} ft"
                : $"{stats.MinTideMeters:F3} to {stats.MaxTideMeters:F3} m";
        }
    }

    // Conversion option
    public bool UseFeetForTide
    {
        get => _useFeetForTide;
        set 
        { 
            _useFeetForTide = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(TideRange));
            OnPropertyChanged(nameof(TideUnitLabel));
        }
    }

    public string TideUnitLabel => UseFeetForTide ? "(converted to feet)" : "(meters from file)";

    // Correction options
    public bool ApplyTidalCorrections
    {
        get => _applyTidalCorrections;
        set { _applyTidalCorrections = value; OnPropertyChanged(); }
    }

    public bool ApplyVerticalOffsets
    {
        get => _applyVerticalOffsets;
        set { _applyVerticalOffsets = value; OnPropertyChanged(); }
    }

    public double? BathyToAltimeterOffset
    {
        get => _bathyToAltimeterOffset;
        set { _bathyToAltimeterOffset = value; OnPropertyChanged(); }
    }

    public double? BathyToRovRefOffset
    {
        get => _bathyToRovRefOffset;
        set { _bathyToRovRefOffset = value; OnPropertyChanged(); }
    }

    public double DepthExaggeration
    {
        get => _depthExaggeration;
        set { _depthExaggeration = value; OnPropertyChanged(); }
    }

    // Survey fixes
    public ObservableCollection<SurveyFix> SurveyFixes { get; }

    public void BrowseTideFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Tide Files (*.txt;*.tid)|*.txt;*.tid|All Files (*.*)|*.*",
            Title = "Select Tide File"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadTideFile(dialog.FileName);
        }
    }

    public void LoadTideFile(string path)
    {
        try
        {
            TideFilePath = path;
            StatusMessage = "Loading tide file...";

            TideData = _tideParser.Parse(path);
            IsTideLoaded = true;

            var stats = TideData.GetStatistics();
            StatusMessage = $"Loaded: {TideData.RecordCount:N0} records, range {stats.TidalRangeMeters:F3}m";
            
            // Update ProcessingTracker for crib sheet
            ProcessingTracker.Instance.OnTideFileLoaded(path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsTideLoaded = false;
            TideData = null;

            MessageBox.Show(
                $"Error loading tide file:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void ClearTideFile()
    {
        TideFilePath = string.Empty;
        TideData = null;
        IsTideLoaded = false;
        StatusMessage = "No tide file loaded (optional)";
    }

    private void UpdateTideInfo()
    {
        OnPropertyChanged(nameof(TideTimeRange));
        OnPropertyChanged(nameof(TideRecordCount));
        OnPropertyChanged(nameof(TideRange));
    }

    // Survey fix management
    public void AddSurveyFix()
    {
        var fix = new SurveyFix
        {
            Name = $"Fix {SurveyFixes.Count + 1}"
        };
        SurveyFixes.Add(fix);
    }

    public void RemoveSurveyFix(SurveyFix fix)
    {
        SurveyFixes.Remove(fix);
    }

    public void ImportFixesFromCsv()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Import Survey Fixes"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = File.ReadAllLines(dialog.FileName).Skip(1); // Skip header
                int imported = 0;

                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 4)
                    {
                        var fix = new SurveyFix
                        {
                            Name = parts[0].Trim(),
                            Kp = double.Parse(parts[1]),
                            Easting = double.Parse(parts[2]),
                            Northing = double.Parse(parts[3]),
                            Depth = parts.Length > 4 && !string.IsNullOrEmpty(parts[4]) 
                                ? double.Parse(parts[4]) : null,
                            Notes = parts.Length > 5 ? parts[5].Trim() : string.Empty
                        };
                        SurveyFixes.Add(fix);
                        imported++;
                    }
                }

                MessageBox.Show($"Imported {imported} survey fixes.", "Import Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing fixes:\n\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public void LoadProject(Project project)
    {
        // Load tide file
        if (!string.IsNullOrEmpty(project.TideFilePath) && File.Exists(project.TideFilePath))
        {
            LoadTideFile(project.TideFilePath);
        }
        else
        {
            TideFilePath = project.TideFilePath;
            IsTideLoaded = false;
            TideData = null;
        }

        UseFeetForTide = project.UseFeetForTide;

        // Load processing options
        ApplyTidalCorrections = project.ProcessingOptions.ApplyTidalCorrections;
        ApplyVerticalOffsets = project.ProcessingOptions.ApplyVerticalOffsets;
        BathyToAltimeterOffset = project.ProcessingOptions.BathyToAltimeterOffset;
        BathyToRovRefOffset = project.ProcessingOptions.BathyToRovRefOffset;
        DepthExaggeration = project.ProcessingOptions.DepthExaggeration;

        // Load survey fixes
        SurveyFixes.Clear();
        foreach (var fix in project.SurveyFixes)
        {
            SurveyFixes.Add(fix);
        }
    }

    public void SaveToProject(Project project)
    {
        project.TideFilePath = TideFilePath;
        project.UseFeetForTide = UseFeetForTide;

        project.ProcessingOptions.ApplyTidalCorrections = ApplyTidalCorrections;
        project.ProcessingOptions.ApplyVerticalOffsets = ApplyVerticalOffsets;
        project.ProcessingOptions.BathyToAltimeterOffset = BathyToAltimeterOffset;
        project.ProcessingOptions.BathyToRovRefOffset = BathyToRovRefOffset;
        project.ProcessingOptions.DepthExaggeration = DepthExaggeration;

        project.SurveyFixes = SurveyFixes.ToList();
    }

    public bool Validate()
    {
        // Tide file is optional, so this step is always valid
        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
