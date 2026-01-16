using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// ViewModel for DVR Recordings view.
/// Handles display, filtering, and management of DVR recordings from the configured folder.
/// </summary>
public class DvrRecordingsViewModel : ViewModelBase
{
    #region Private Fields

    private readonly ObservableCollection<DvrRecording> _dvrRecordings = new();
    private readonly ICollectionView _dvrRecordingsView;
    private DvrRecording? _selectedDvrRecording;
    private string _filterText = "";
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;
    private string _dvrFolderPath = "";

    #endregion

    #region Constructor

    public DvrRecordingsViewModel()
    {
        _dvrRecordingsView = CollectionViewSource.GetDefaultView(_dvrRecordings);
        _dvrRecordingsView.Filter = FilterRecording;

        // Initialize commands
        RefreshDvrCommand = new RelayCommand(_ => RefreshDvrRecordings());
        BrowseDvrFolderCommand = new RelayCommand(_ => BrowseDvrFolder());
        OpenDvrFileCommand = new RelayCommand(param => OpenDvrFile(param as DvrRecording), _ => true);
        OpenDvrFolderCommand = new RelayCommand(param => OpenDvrFolder(param as DvrRecording), _ => true);
        ExportDvrListCommand = new RelayCommand(_ => ExportDvrList(), _ => _dvrRecordings.Count > 0);
        ClearFilterCommand = new RelayCommand(_ => ClearFilter());
    }

    #endregion

    #region Properties

    public ICollectionView DvrRecordingsView => _dvrRecordingsView;

    public DvrRecording? SelectedDvrRecording
    {
        get => _selectedDvrRecording;
        set => SetProperty(ref _selectedDvrRecording, value);
    }

    public string FilterText
    {
        get => _filterText;
        set
        {
            if (SetProperty(ref _filterText, value))
                _dvrRecordingsView.Refresh();
        }
    }

    public DateTime? FilterStartDate
    {
        get => _filterStartDate;
        set
        {
            if (SetProperty(ref _filterStartDate, value))
                _dvrRecordingsView.Refresh();
        }
    }

    public DateTime? FilterEndDate
    {
        get => _filterEndDate;
        set
        {
            if (SetProperty(ref _filterEndDate, value))
                _dvrRecordingsView.Refresh();
        }
    }

    public string DvrFolderPath
    {
        get => _dvrFolderPath;
        set => SetProperty(ref _dvrFolderPath, value);
    }

    public int TotalRecordings => _dvrRecordings.Count;

    public string TotalDuration
    {
        get
        {
            var total = TimeSpan.FromTicks(_dvrRecordings.Sum(r => r.Duration.Ticks));
            return $"{(int)total.TotalHours:D2}:{total.Minutes:D2}:{total.Seconds:D2}";
        }
    }

    public string TotalSize
    {
        get
        {
            var totalMb = _dvrRecordings.Sum(r => r.FileSizeMB);
            if (totalMb >= 1024)
                return $"{totalMb / 1024:F2} GB";
            return $"{totalMb:F1} MB";
        }
    }

    #endregion

    #region Commands

    public ICommand RefreshDvrCommand { get; }
    public ICommand BrowseDvrFolderCommand { get; }
    public ICommand OpenDvrFileCommand { get; }
    public ICommand OpenDvrFolderCommand { get; }
    public ICommand ExportDvrListCommand { get; }
    public ICommand ClearFilterCommand { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads DVR recordings from the specified folder path.
    /// </summary>
    public void LoadFromFolder(string folderPath)
    {
        DvrFolderPath = folderPath;
        RefreshDvrRecordings();
    }

    /// <summary>
    /// Loads DVR recordings from an existing collection.
    /// </summary>
    public void LoadRecordings(IEnumerable<DvrRecording> recordings)
    {
        _dvrRecordings.Clear();
        foreach (var recording in recordings)
        {
            _dvrRecordings.Add(recording);
        }
        
        OnPropertyChanged(nameof(TotalRecordings));
        OnPropertyChanged(nameof(TotalDuration));
        OnPropertyChanged(nameof(TotalSize));
    }

    /// <summary>
    /// Adds a new DVR recording to the collection.
    /// </summary>
    public void AddRecording(DvrRecording recording)
    {
        _dvrRecordings.Add(recording);
        OnPropertyChanged(nameof(TotalRecordings));
        OnPropertyChanged(nameof(TotalDuration));
        OnPropertyChanged(nameof(TotalSize));
    }

    /// <summary>
    /// Refreshes the DVR recordings list.
    /// </summary>
    public void RefreshRecordings()
    {
        RefreshDvrRecordings();
    }

    /// <summary>
    /// Clears all DVR recordings.
    /// </summary>
    public void ClearRecordings()
    {
        _dvrRecordings.Clear();
        OnPropertyChanged(nameof(TotalRecordings));
        OnPropertyChanged(nameof(TotalDuration));
        OnPropertyChanged(nameof(TotalSize));
    }

    #endregion

    #region Private Methods

    private bool FilterRecording(object obj)
    {
        if (obj is not DvrRecording recording)
            return false;

        // Text filter
        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            var searchText = FilterText.ToLower();
            var matchesText = recording.FileName?.ToLower().Contains(searchText) == true ||
                              recording.Description?.ToLower().Contains(searchText) == true ||
                              recording.Channel?.ToLower().Contains(searchText) == true;
            
            if (!matchesText) return false;
        }

        // Date range filter
        if (FilterStartDate.HasValue && recording.Date < FilterStartDate.Value.Date)
            return false;

        if (FilterEndDate.HasValue && recording.Date > FilterEndDate.Value.Date)
            return false;

        return true;
    }

    private void RefreshDvrRecordings()
    {
        if (string.IsNullOrEmpty(DvrFolderPath) || !Directory.Exists(DvrFolderPath))
            return;

        try
        {
            _dvrRecordings.Clear();

            var videoExtensions = new[] { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".ts" };
            var files = Directory.GetFiles(DvrFolderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => videoExtensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var recording = new DvrRecording
                {
                    Id = Guid.NewGuid(),
                    FileName = fileInfo.Name,
                    FilePath = file,
                    Date = fileInfo.CreationTime.Date,
                    StartDateTime = fileInfo.CreationTime,
                    FileSizeMB = fileInfo.Length / (1024.0 * 1024.0),
                    Channel = Path.GetDirectoryName(file)?.Split(Path.DirectorySeparatorChar).LastOrDefault() ?? "Unknown"
                };

                _dvrRecordings.Add(recording);
            }

            OnPropertyChanged(nameof(TotalRecordings));
            OnPropertyChanged(nameof(TotalDuration));
            OnPropertyChanged(nameof(TotalSize));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error scanning DVR folder: {ex.Message}",
                "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BrowseDvrFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select DVR Recordings Folder",
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(DvrFolderPath) && Directory.Exists(DvrFolderPath))
            dialog.SelectedPath = DvrFolderPath;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DvrFolderPath = dialog.SelectedPath;
            RefreshDvrRecordings();
        }
    }

    private void OpenDvrFile(DvrRecording? recording)
    {
        if (recording == null || string.IsNullOrEmpty(recording.FilePath))
            return;

        try
        {
            if (File.Exists(recording.FilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = recording.FilePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error opening file: {ex.Message}",
                "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void OpenDvrFolder(DvrRecording? recording)
    {
        if (recording == null || string.IsNullOrEmpty(recording.FilePath))
            return;

        try
        {
            var folder = Path.GetDirectoryName(recording.FilePath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{recording.FilePath}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error opening folder: {ex.Message}",
                "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void ExportDvrList()
    {
        try
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export DVR Recordings List",
                Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                FileName = $"DVR_Recordings_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() != true) return;

            var recordings = _dvrRecordingsView.Cast<DvrRecording>().ToList();
            
            if (saveDialog.FilterIndex == 1) // Excel
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("DVR Recordings");
                
                // Headers
                worksheet.Cell(1, 1).Value = "File Name";
                worksheet.Cell(1, 2).Value = "Recording Date";
                worksheet.Cell(1, 3).Value = "Duration";
                worksheet.Cell(1, 4).Value = "Size (MB)";
                worksheet.Cell(1, 5).Value = "Full Path";
                
                // Style header
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
                
                // Data
                for (int i = 0; i < recordings.Count; i++)
                {
                    var rec = recordings[i];
                    worksheet.Cell(i + 2, 1).Value = rec.FileName;
                    worksheet.Cell(i + 2, 2).Value = rec.RecordingDate.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cell(i + 2, 3).Value = rec.Duration.ToString(@"hh\:mm\:ss");
                    worksheet.Cell(i + 2, 4).Value = rec.FileSizeMB;
                    worksheet.Cell(i + 2, 5).Value = rec.FilePath;
                }
                
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(saveDialog.FileName);
            }
            else // CSV
            {
                var lines = new System.Collections.Generic.List<string>
                {
                    "File Name,Recording Date,Duration,Size (MB),Full Path"
                };
                
                foreach (var rec in recordings)
                {
                    lines.Add($"\"{rec.FileName}\",\"{rec.RecordingDate:yyyy-MM-dd HH:mm:ss}\",\"{rec.Duration:hh\\:mm\\:ss}\",{rec.FileSizeMB:F2},\"{rec.FilePath}\"");
                }
                
                File.WriteAllLines(saveDialog.FileName, lines);
            }
            
            System.Windows.MessageBox.Show($"Exported {recordings.Count} recordings to:\n{saveDialog.FileName}",
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
        FilterStartDate = null;
        FilterEndDate = null;
    }

    #endregion
}
