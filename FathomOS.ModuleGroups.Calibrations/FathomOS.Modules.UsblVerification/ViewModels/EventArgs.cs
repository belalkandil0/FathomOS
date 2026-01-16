using System.Collections.Generic;
using System.ComponentModel;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.ViewModels;

/// <summary>
/// Event args for requesting single file column mapping dialog
/// </summary>
public class ColumnMappingEventArgs : System.EventArgs
{
    public string FilePath { get; set; } = "";
    public UsblColumnMapping? ResultMapping { get; set; }
    public bool Confirmed { get; set; }
}

/// <summary>
/// Event args for requesting batch column mapping dialog
/// </summary>
public class BatchColumnMappingEventArgs : System.EventArgs
{
    public List<string> FilePaths { get; set; } = new();
    public Dictionary<string, UsblColumnMapping>? ResultMappings { get; set; }
    public bool Confirmed { get; set; }
}

/// <summary>
/// Represents a step in the workflow for the header display
/// </summary>
public class StepInfo : INotifyPropertyChanged
{
    private bool _isCurrent;
    private bool _isCompleted;
    
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    
    public bool IsCurrent
    {
        get => _isCurrent;
        set { _isCurrent = value; OnPropertyChanged(nameof(IsCurrent)); }
    }
    
    public bool IsCompleted
    {
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents a loaded file in the UI
/// </summary>
public class LoadedFileInfo : ViewModelBase
{
    private string _filePath = "";
    private string _fileName = "";
    private int _recordCount;
    private string _headingLabel = "";
    private UsblColumnMapping _mapping = new();
    
    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }
    
    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }
    
    public int RecordCount
    {
        get => _recordCount;
        set { _recordCount = value; OnPropertyChanged(); }
    }
    
    public string HeadingLabel
    {
        get => _headingLabel;
        set { _headingLabel = value; OnPropertyChanged(); }
    }
    
    public UsblColumnMapping Mapping
    {
        get => _mapping;
        set { _mapping = value; OnPropertyChanged(); }
    }
    
    private double _actualHeading;
    public double ActualHeading
    {
        get => _actualHeading;
        set { _actualHeading = value; OnPropertyChanged(); }
    }
}
