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
/// ViewModel for Step 2: Route File (RLX) and Field Layout
/// </summary>
public class Step2ViewModel : INotifyPropertyChanged
{
    private readonly RlxParser _parser;
    private readonly DxfLayoutParser _dxfParser;
    
    private string _filePath = string.Empty;
    private RouteData? _routeData;
    private bool _isLoaded;
    private string _statusMessage = "No route file loaded";
    
    // Field Layout
    private string _fieldLayoutPath = string.Empty;
    private FieldLayout? _fieldLayout;
    private bool _isFieldLayoutLoaded;
    private string _fieldLayoutStatus = "No field layout loaded";

    public Step2ViewModel(Project project)
    {
        _parser = new RlxParser();
        _dxfParser = new DxfLayoutParser();
        LoadProject(project);
    }

    #region Route File Properties

    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileName)); }
    }

    public string FileName => string.IsNullOrEmpty(_filePath) ? "None" : Path.GetFileName(_filePath);

    public RouteData? RouteData
    {
        get => _routeData;
        private set { _routeData = value; OnPropertyChanged(); UpdateRouteInfo(); }
    }

    public bool IsLoaded
    {
        get => _isLoaded;
        private set { _isLoaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(PreviewEnabled)); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    // Route information properties
    public string RouteName => _routeData?.Name ?? "-";
    public string RouteUnit => _routeData?.CoordinateUnit.GetDisplayName() ?? "-";
    public string SegmentCount => _routeData != null 
        ? $"{_routeData.Segments.Count} ({_routeData.StraightSegmentCount} straight, {_routeData.ArcSegmentCount} arc)" 
        : "-";
    public string KpRange => _routeData != null 
        ? $"{_routeData.StartKp:F6} to {_routeData.EndKp:F6}" 
        : "-";
    public string TotalLength => _routeData != null 
        ? $"{_routeData.TotalLength:F6} km" 
        : "-";

    #endregion

    #region Field Layout Properties

    public string FieldLayoutPath
    {
        get => _fieldLayoutPath;
        set { _fieldLayoutPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(FieldLayoutFileName)); }
    }

    public string FieldLayoutFileName => string.IsNullOrEmpty(_fieldLayoutPath) ? "None" : Path.GetFileName(_fieldLayoutPath);

    public FieldLayout? FieldLayout
    {
        get => _fieldLayout;
        private set { _fieldLayout = value; OnPropertyChanged(); UpdateFieldLayoutInfo(); }
    }

    public bool IsFieldLayoutLoaded
    {
        get => _isFieldLayoutLoaded;
        private set { _isFieldLayoutLoaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(PreviewEnabled)); }
    }

    public string FieldLayoutStatus
    {
        get => _fieldLayoutStatus;
        private set { _fieldLayoutStatus = value; OnPropertyChanged(); }
    }

    public string FieldLayoutInfo => _fieldLayout != null 
        ? $"{_fieldLayout.TotalEntities} entities"
        : "-";

    public bool PreviewEnabled => IsLoaded || IsFieldLayoutLoaded;

    private bool _isRouteRequired = true;
    public bool IsRouteRequired
    {
        get => _isRouteRequired;
        set { _isRouteRequired = value; OnPropertyChanged(); }
    }

    #endregion

    #region Route File Methods

    public void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Route Files (*.rlx)|*.rlx|All Files (*.*)|*.*",
            Title = "Select Route File"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFile(dialog.FileName);
        }
    }

    public void LoadFile(string path)
    {
        try
        {
            FilePath = path;
            StatusMessage = "Loading...";

            RouteData = _parser.Parse(path);
            
            // Validate
            var issues = RouteData.Validate();
            if (issues.Count > 0)
            {
                StatusMessage = $"Loaded with {issues.Count} warning(s)";
                MessageBox.Show(
                    "Route loaded with warnings:\n\n" + string.Join("\n", issues),
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                StatusMessage = "Route loaded successfully";
            }

            IsLoaded = true;
            
            // Update ProcessingTracker for crib sheet
            ProcessingTracker.Instance.OnRouteFileLoaded(path);
            
            // Notify that preview should be updated
            OnRouteDataChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsLoaded = false;
            RouteData = null;
            
            MessageBox.Show(
                $"Error loading route file:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #endregion

    #region Field Layout Methods

    public void BrowseFieldLayout()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
            Title = "Select Field Layout DXF"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFieldLayout(dialog.FileName);
        }
    }

    public void LoadFieldLayout(string path)
    {
        try
        {
            FieldLayoutPath = path;
            FieldLayoutStatus = "Loading...";

            FieldLayout = _dxfParser.Parse(path);
            FieldLayoutStatus = $"Loaded: {FieldLayout.TotalEntities} entities";
            IsFieldLayoutLoaded = true;
            
            // Update ProcessingTracker for crib sheet
            ProcessingTracker.Instance.OnFieldLayoutLoaded(path);
            
            // Notify that preview should be updated
            OnFieldLayoutChanged();
        }
        catch (Exception ex)
        {
            FieldLayoutStatus = $"Error: {ex.Message}";
            IsFieldLayoutLoaded = false;
            FieldLayout = null;
            
            MessageBox.Show(
                $"Error loading field layout:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void ClearFieldLayout()
    {
        FieldLayoutPath = string.Empty;
        FieldLayout = null;
        IsFieldLayoutLoaded = false;
        FieldLayoutStatus = "No field layout loaded";
        OnFieldLayoutChanged();
    }

    #endregion

    #region Project Load/Save

    public void LoadProject(Project project)
    {
        // Load route file
        if (!string.IsNullOrEmpty(project.RouteFilePath) && File.Exists(project.RouteFilePath))
        {
            LoadFile(project.RouteFilePath);
        }
        else
        {
            FilePath = project.RouteFilePath;
            IsLoaded = false;
            RouteData = null;
            StatusMessage = string.IsNullOrEmpty(project.RouteFilePath) 
                ? "No route file loaded" 
                : "File not found";
        }

        // Load field layout if exists
        if (!string.IsNullOrEmpty(project.FieldLayoutDxfPath) && File.Exists(project.FieldLayoutDxfPath))
        {
            LoadFieldLayout(project.FieldLayoutDxfPath);
        }
        else
        {
            FieldLayoutPath = project.FieldLayoutDxfPath;
            IsFieldLayoutLoaded = false;
            FieldLayout = null;
            FieldLayoutStatus = string.IsNullOrEmpty(project.FieldLayoutDxfPath)
                ? "No field layout loaded"
                : "File not found";
        }
    }

    public void SaveToProject(Project project)
    {
        project.RouteFilePath = FilePath;
        project.FieldLayoutDxfPath = FieldLayoutPath;
    }

    public bool Validate(bool routeRequired = true)
    {
        if (routeRequired && (!IsLoaded || RouteData == null))
        {
            MessageBox.Show(
                "Please load a route file before continuing.\n\n" +
                "Tip: If you don't need KP/DCC calculations, you can disable them in Step 1 " +
                "to skip the route requirement.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when route data changes (for 3D preview update)
    /// </summary>
    public event EventHandler? RouteDataChanged;

    /// <summary>
    /// Raised when field layout changes (for 3D preview update)
    /// </summary>
    public event EventHandler? FieldLayoutChanged;

    protected virtual void OnRouteDataChanged()
    {
        RouteDataChanged?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnFieldLayoutChanged()
    {
        FieldLayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    private void UpdateRouteInfo()
    {
        OnPropertyChanged(nameof(RouteName));
        OnPropertyChanged(nameof(RouteUnit));
        OnPropertyChanged(nameof(SegmentCount));
        OnPropertyChanged(nameof(KpRange));
        OnPropertyChanged(nameof(TotalLength));
    }

    private void UpdateFieldLayoutInfo()
    {
        OnPropertyChanged(nameof(FieldLayoutInfo));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
