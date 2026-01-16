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
/// ViewModel for Step 3: Survey Data (NPD) with batch file support
/// </summary>
public class Step3ViewModel : INotifyPropertyChanged
{
    private readonly NpdParser _parser;
    private ColumnMapping _selectedTemplate;
    private string _selectedDepthColumn = string.Empty;
    private string _selectedAltitudeColumn = string.Empty;
    private bool _hasDateTimeSplit = true;
    private string _dateFormat = "dd/MM/yyyy";
    private string _timeFormat = "HH:mm:ss";
    private int _totalRecordCount;
    private string _statusMessage = "No files loaded";

    public Step3ViewModel(Project project)
    {
        _parser = new NpdParser();
        Files = new ObservableCollection<SurveyFileInfo>();
        AvailableDepthColumns = new ObservableCollection<string>();
        AvailableAltitudeColumns = new ObservableCollection<string>();
        AllColumns = new ObservableCollection<string>();
        
        // Initialize templates
        Templates = new ObservableCollection<ColumnMapping>(ColumnMappingTemplates.AllTemplates);
        _selectedTemplate = ColumnMappingTemplates.NaviPacDefault;

        LoadProject(project);
    }

    // Collections
    public ObservableCollection<SurveyFileInfo> Files { get; }
    public ObservableCollection<ColumnMapping> Templates { get; }
    public ObservableCollection<string> AvailableDepthColumns { get; }
    public ObservableCollection<string> AvailableAltitudeColumns { get; }
    public ObservableCollection<string> AllColumns { get; }

    // Properties
    public ColumnMapping SelectedTemplate
    {
        get => _selectedTemplate;
        set 
        { 
            _selectedTemplate = value; 
            OnPropertyChanged();
            ApplyTemplate();
        }
    }

    public string SelectedDepthColumn
    {
        get => _selectedDepthColumn;
        set { _selectedDepthColumn = value; OnPropertyChanged(); }
    }

    public string SelectedAltitudeColumn
    {
        get => _selectedAltitudeColumn;
        set { _selectedAltitudeColumn = value; OnPropertyChanged(); }
    }

    public bool HasDateTimeSplit
    {
        get => _hasDateTimeSplit;
        set { _hasDateTimeSplit = value; OnPropertyChanged(); }
    }

    public string DateFormat
    {
        get => _dateFormat;
        set { _dateFormat = value; OnPropertyChanged(); }
    }

    public string TimeFormat
    {
        get => _timeFormat;
        set { _timeFormat = value; OnPropertyChanged(); }
    }

    public int TotalRecordCount
    {
        get => _totalRecordCount;
        private set { _totalRecordCount = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool HasFiles => Files.Count > 0;

    public void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Survey Data Files (*.npd;*.csv)|*.npd;*.csv|All Files (*.*)|*.*",
            Title = "Select Survey Data Files",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                AddFile(file);
            }
        }
    }

    public void AddFile(string path)
    {
        if (Files.Any(f => f.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show($"File already added: {Path.GetFileName(path)}", "Info", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var fileInfo = new FileInfo(path);
            var info = new SurveyFileInfo
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                FileSize = FormatFileSize(fileInfo.Length),
                ModifiedDate = fileInfo.LastWriteTime
            };

            // Get quick stats
            var columns = _parser.GetAllColumns(path);
            info.ColumnCount = columns.Count;

            Files.Add(info);
            
            // Update available columns from first file
            if (Files.Count == 1)
            {
                UpdateAvailableColumns(path);
            }

            UpdateStatus();
            OnPropertyChanged(nameof(HasFiles));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding file: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void RemoveFile(SurveyFileInfo file)
    {
        Files.Remove(file);
        UpdateStatus();
        OnPropertyChanged(nameof(HasFiles));
    }

    public void ClearFiles()
    {
        Files.Clear();
        AvailableDepthColumns.Clear();
        AvailableAltitudeColumns.Clear();
        AllColumns.Clear();
        TotalRecordCount = 0;
        StatusMessage = "No files loaded";
        OnPropertyChanged(nameof(HasFiles));
    }

    public void MoveFileUp(SurveyFileInfo file)
    {
        int index = Files.IndexOf(file);
        if (index > 0)
        {
            Files.Move(index, index - 1);
        }
    }

    public void MoveFileDown(SurveyFileInfo file)
    {
        int index = Files.IndexOf(file);
        if (index < Files.Count - 1)
        {
            Files.Move(index, index + 1);
        }
    }

    public void AutoDetectColumns()
    {
        if (Files.Count == 0)
        {
            MessageBox.Show("Please add at least one file first.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        UpdateAvailableColumns(Files[0].FilePath);
        
        // Try to auto-select best depth column
        var bathyColumn = AvailableDepthColumns.FirstOrDefault(c => 
            c.Contains("Bathy", StringComparison.OrdinalIgnoreCase));
        if (bathyColumn != null)
        {
            SelectedDepthColumn = bathyColumn;
        }
        else if (AvailableDepthColumns.Count > 0)
        {
            SelectedDepthColumn = AvailableDepthColumns[0];
        }

        // Try to auto-select best altitude column
        var altColumn = AvailableAltitudeColumns.FirstOrDefault(c => 
            c.Contains("Alt", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("Altitude", StringComparison.OrdinalIgnoreCase));
        if (altColumn != null)
        {
            SelectedAltitudeColumn = altColumn;
        }
        else if (AvailableAltitudeColumns.Count > 0)
        {
            SelectedAltitudeColumn = AvailableAltitudeColumns[0];
        }

        MessageBox.Show(
            $"Detected {AllColumns.Count} columns\n" +
            $"Found {AvailableDepthColumns.Count} potential depth columns\n" +
            $"Found {AvailableAltitudeColumns.Count} potential altitude columns",
            "Auto-Detection Complete",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void UpdateAvailableColumns(string filePath)
    {
        try
        {
            AllColumns.Clear();
            AvailableDepthColumns.Clear();
            AvailableAltitudeColumns.Clear();

            var columns = _parser.GetAllColumns(filePath);
            foreach (var col in columns)
            {
                AllColumns.Add(col);
            }

            var depthCols = _parser.GetAvailableDepthColumns(filePath);
            foreach (var col in depthCols)
            {
                AvailableDepthColumns.Add(col);
            }

            // For altitude, use the same numeric columns as depth
            // but prioritize columns with "Alt" or "Altitude" in the name
            foreach (var col in depthCols)
            {
                AvailableAltitudeColumns.Add(col);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error reading columns: {ex.Message}";
        }
    }

    private void ApplyTemplate()
    {
        if (_selectedTemplate != null)
        {
            HasDateTimeSplit = _selectedTemplate.HasDateTimeSplit;
            DateFormat = _selectedTemplate.DateFormat;
            TimeFormat = _selectedTemplate.TimeFormat;
        }
    }

    private void UpdateStatus()
    {
        if (Files.Count == 0)
        {
            StatusMessage = "No files loaded";
            TotalRecordCount = 0;
        }
        else
        {
            StatusMessage = $"{Files.Count} file(s) loaded";
            
            // Update ProcessingTracker for crib sheet
            ProcessingTracker.Instance.OnSurveyDataLoaded(Files.Select(f => f.FilePath));
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }

    public ColumnMapping GetCurrentMapping()
    {
        var mapping = _selectedTemplate.Clone();
        mapping.HasDateTimeSplit = HasDateTimeSplit;
        mapping.DateFormat = DateFormat;
        mapping.TimeFormat = TimeFormat;
        mapping.DepthColumnPattern = SelectedDepthColumn;
        mapping.AltitudeColumnPattern = SelectedAltitudeColumn;
        return mapping;
    }

    public void LoadProject(Project project)
    {
        ClearFiles();

        foreach (var file in project.SurveyDataFiles)
        {
            if (File.Exists(file))
            {
                AddFile(file);
            }
        }

        // Load mapping settings
        var mapping = project.ColumnMapping;
        var template = Templates.FirstOrDefault(t => t.Name == mapping.Name);
        if (template != null)
        {
            SelectedTemplate = template;
        }

        HasDateTimeSplit = mapping.HasDateTimeSplit;
        DateFormat = mapping.DateFormat;
        TimeFormat = mapping.TimeFormat;
        SelectedDepthColumn = project.SelectedDepthColumn;
        SelectedAltitudeColumn = project.SelectedAltitudeColumn;
    }

    public void SaveToProject(Project project)
    {
        project.SurveyDataFiles = Files.Select(f => f.FilePath).ToList();
        project.ColumnMapping = GetCurrentMapping();
        project.SelectedDepthColumn = SelectedDepthColumn;
        project.SelectedAltitudeColumn = SelectedAltitudeColumn;
    }

    public bool Validate()
    {
        if (Files.Count == 0)
        {
            MessageBox.Show(
                "Please add at least one survey data file.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Information about a survey data file
/// </summary>
public class SurveyFileInfo : INotifyPropertyChanged
{
    private bool _isSelected;

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
    public int ColumnCount { get; set; }
    public int RecordCount { get; set; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
