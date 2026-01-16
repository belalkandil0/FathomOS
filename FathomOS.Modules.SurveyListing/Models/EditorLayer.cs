using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using FathomOS.Core.Models;
using Color = System.Windows.Media.Color;

namespace FathomOS.Modules.SurveyListing.Models;

/// <summary>
/// Data class for synchronizing all Editor changes back to Step 6
/// </summary>
public class EditorSyncData
{
    /// <summary>
    /// Main survey points with smoothing applied
    /// </summary>
    public List<SurveyPoint> SurveyPoints { get; set; } = new();
    
    /// <summary>
    /// Spline-fitted points generated in Editor
    /// </summary>
    public List<SurveyPoint> SplinePoints { get; set; } = new();
    
    /// <summary>
    /// Interval measure points generated in Editor
    /// </summary>
    public List<(double X, double Y, double Z, double Distance)> IntervalPoints { get; set; } = new();
    
    /// <summary>
    /// Points added by user in Editor (digitized)
    /// </summary>
    public List<SurveyPoint> AddedPoints { get; set; } = new();
    
    /// <summary>
    /// Indices of points deleted in Editor
    /// </summary>
    public List<int> DeletedPointIndices { get; set; } = new();
    
    /// <summary>
    /// Whether spline was applied
    /// </summary>
    public bool HasSpline => SplinePoints.Count > 0;
    
    /// <summary>
    /// Whether interval measure was applied
    /// </summary>
    public bool HasIntervalPoints => IntervalPoints.Count > 0;
    
    /// <summary>
    /// Whether points were added
    /// </summary>
    public bool HasAddedPoints => AddedPoints.Count > 0;
    
    /// <summary>
    /// Whether points were deleted
    /// </summary>
    public bool HasDeletedPoints => DeletedPointIndices.Count > 0;
}

/// <summary>
/// Layer types in the survey editor
/// </summary>
public enum EditorLayerType
{
    Grid,
    FieldLayout,
    Route,
    SurveyRaw,
    SurveySmoothed,
    SurveyCalculated,
    Digitized,
    Selection,
    Annotations,
    Custom,          // User-created duplicate layers
    KpLabels,        // KP labels layer
    // Sub-layer types (AutoCAD style separation)
    PointsOnly,      // Points layer (no lines)
    LinesOnly        // Lines/Polyline layer (no points)
}

/// <summary>
/// Display mode for a layer
/// </summary>
public enum LayerDisplayMode
{
    PointsAndLines,  // Show both (default)
    PointsOnly,      // Show only points
    LinesOnly        // Show only polyline
}

/// <summary>
/// Represents a layer in the survey editor with visibility and style settings
/// </summary>
public class EditorLayer : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private bool _isVisible = true;
    private bool _isLocked = false;
    private bool _isSelectable = true;
    private Color _color = Colors.White;
    private double _thickness = 1.0;
    private double _pointSize = 4.0;
    private double _opacity = 1.0;
    private int _zIndex = 0;
    private EditorLayerType _layerType;
    private bool _isUserCreated = false;
    private ObservableCollection<EditablePoint>? _points;
    private LayerDisplayMode _displayMode = LayerDisplayMode.PointsAndLines;
    private string? _parentLayerName;
    private bool _isSubLayer = false;
    private bool _isClosed = false;

    public EditorLayer(EditorLayerType type, string name, Color color)
    {
        LayerType = type;
        Name = name;
        Color = color;
        ZIndex = GetDefaultZIndex(type);
    }
    
    /// <summary>
    /// Indicates if this polyline/polygon is closed (last point connects to first)
    /// </summary>
    public bool IsClosed
    {
        get => _isClosed;
        set { _isClosed = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Display mode: Points only, Lines only, or Both
    /// </summary>
    public LayerDisplayMode DisplayMode
    {
        get => _displayMode;
        set { _displayMode = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Parent layer name (for sub-layers)
    /// </summary>
    public string? ParentLayerName
    {
        get => _parentLayerName;
        set { _parentLayerName = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Indicates if this is a sub-layer (Points or Lines)
    /// </summary>
    public bool IsSubLayer
    {
        get => _isSubLayer;
        set { _isSubLayer = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Points owned by this layer (for duplicated/custom layers)
    /// </summary>
    public ObservableCollection<EditablePoint>? Points
    {
        get => _points;
        set { _points = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Indicates if this layer was created by the user (can be deleted)
    /// </summary>
    public bool IsUserCreated
    {
        get => _isUserCreated;
        set { _isUserCreated = value; OnPropertyChanged(); }
    }

    public EditorLayerType LayerType
    {
        get => _layerType;
        set { _layerType = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(VisibilityIcon)); }
    }

    public bool IsLocked
    {
        get => _isLocked;
        set { _isLocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(LockIcon)); }
    }

    public bool IsSelectable
    {
        get => _isSelectable;
        set { _isSelectable = value; OnPropertyChanged(); }
    }

    public Color Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(); OnPropertyChanged(nameof(ColorBrush)); }
    }

    public double Thickness
    {
        get => _thickness;
        set { _thickness = Math.Max(0.1, value); OnPropertyChanged(); }
    }

    public double PointSize
    {
        get => _pointSize;
        set { _pointSize = Math.Max(1, value); OnPropertyChanged(); }
    }

    public double Opacity
    {
        get => _opacity;
        set { _opacity = Math.Max(0, Math.Min(1, value)); OnPropertyChanged(); }
    }

    public int ZIndex
    {
        get => _zIndex;
        set { _zIndex = value; OnPropertyChanged(); }
    }

    // UI Helpers
    public string VisibilityIcon => IsVisible ? "👁" : "👁‍🗨";
    public string LockIcon => IsLocked ? "🔒" : "🔓";
    public SolidColorBrush ColorBrush => new SolidColorBrush(Color);

    private static int GetDefaultZIndex(EditorLayerType type)
    {
        return type switch
        {
            EditorLayerType.Grid => 0,
            EditorLayerType.FieldLayout => 10,
            EditorLayerType.Route => 20,
            EditorLayerType.SurveyRaw => 30,
            EditorLayerType.LinesOnly => 31,   // Lines slightly above
            EditorLayerType.PointsOnly => 32,  // Points on top
            EditorLayerType.SurveySmoothed => 40,
            EditorLayerType.SurveyCalculated => 50,
            EditorLayerType.Digitized => 60,
            EditorLayerType.Custom => 55,
            EditorLayerType.KpLabels => 80,
            EditorLayerType.Selection => 100,
            EditorLayerType.Annotations => 90,
            _ => 50
        };
    }

    public static EditorLayer CreateDefault(EditorLayerType type)
    {
        return type switch
        {
            EditorLayerType.Grid => new EditorLayer(type, "Grid", Color.FromRgb(60, 60, 60)) { Thickness = 0.5, IsSelectable = false },
            EditorLayerType.FieldLayout => new EditorLayer(type, "Field Layout", Colors.DimGray) { Thickness = 1.0 },
            EditorLayerType.Route => new EditorLayer(type, "Route", Colors.Red) { Thickness = 2.0 },
            EditorLayerType.SurveyRaw => new EditorLayer(type, "Survey (Raw)", Colors.LimeGreen) { Thickness = 1.5, PointSize = 4, DisplayMode = LayerDisplayMode.PointsAndLines },
            EditorLayerType.SurveySmoothed => new EditorLayer(type, "Survey (Smoothed)", Colors.Cyan) { Thickness = 2.0, PointSize = 4, DisplayMode = LayerDisplayMode.PointsAndLines },
            EditorLayerType.SurveyCalculated => new EditorLayer(type, "Survey (Calculated)", Colors.Yellow) { Thickness = 2.0, PointSize = 4, DisplayMode = LayerDisplayMode.PointsAndLines },
            EditorLayerType.Digitized => new EditorLayer(type, "Digitized Points", Colors.Orange) { Thickness = 2.0, PointSize = 4, DisplayMode = LayerDisplayMode.PointsOnly },
            EditorLayerType.Selection => new EditorLayer(type, "Selection", Colors.White) { Thickness = 2.0, PointSize = 8, IsSelectable = false },
            EditorLayerType.Annotations => new EditorLayer(type, "Annotations", Colors.White) { IsSelectable = false },
            EditorLayerType.KpLabels => new EditorLayer(type, "KP Labels", Colors.White) { IsSelectable = false },
            EditorLayerType.Custom => new EditorLayer(type, "Custom Layer", Colors.Magenta) { Thickness = 2.0, PointSize = 4, IsUserCreated = true },
            EditorLayerType.PointsOnly => new EditorLayer(type, "Points", Colors.Cyan) { PointSize = 4, IsSubLayer = true, DisplayMode = LayerDisplayMode.PointsOnly },
            EditorLayerType.LinesOnly => new EditorLayer(type, "Lines", Colors.Cyan) { Thickness = 2.0, IsSubLayer = true, DisplayMode = LayerDisplayMode.LinesOnly },
            _ => new EditorLayer(type, type.ToString(), Colors.White)
        };
    }
    
    /// <summary>
    /// Create sub-layers (Points and Lines) for a parent layer
    /// </summary>
    public static (EditorLayer pointsLayer, EditorLayer linesLayer) CreateSubLayers(string parentName, Color color, int baseZIndex)
    {
        var pointsLayer = new EditorLayer(EditorLayerType.PointsOnly, $"{parentName} - Points", color)
        {
            PointSize = 4,  // Same as normal points
            IsSubLayer = true,
            ParentLayerName = parentName,
            DisplayMode = LayerDisplayMode.PointsOnly,
            ZIndex = baseZIndex + 2,
            IsSelectable = true
        };
        
        var linesLayer = new EditorLayer(EditorLayerType.LinesOnly, $"{parentName} - Lines", color)
        {
            Thickness = 2.0,
            IsSubLayer = true,
            ParentLayerName = parentName,
            DisplayMode = LayerDisplayMode.LinesOnly,
            ZIndex = baseZIndex + 1,
            IsSelectable = false  // Lines not selectable, only points
        };
        
        return (pointsLayer, linesLayer);
    }
    
    /// <summary>
    /// Create a duplicate of this layer with its data
    /// </summary>
    public EditorLayer Duplicate(string newName)
    {
        var duplicate = new EditorLayer(EditorLayerType.Custom, newName, GetNextColor())
        {
            Thickness = this.Thickness,
            PointSize = this.PointSize,
            Opacity = this.Opacity,
            IsUserCreated = true,
            ZIndex = this.ZIndex + 1
        };
        
        // Deep copy points if this layer has them
        if (Points != null && Points.Count > 0)
        {
            duplicate.Points = new ObservableCollection<EditablePoint>();
            foreach (var pt in Points)
            {
                duplicate.Points.Add(new EditablePoint
                {
                    X = pt.X,
                    Y = pt.Y,
                    Z = pt.Z,
                    OriginalX = pt.OriginalX,
                    OriginalY = pt.OriginalY,
                    OriginalZ = pt.OriginalZ,
                    OriginalIndex = pt.OriginalIndex,
                    SourcePoint = pt.SourcePoint,
                    Kp = pt.Kp,
                    Dcc = pt.Dcc,
                    IsModified = false
                });
            }
        }
        
        return duplicate;
    }
    
    private static int _colorIndex = 0;
    private static readonly Color[] _availableColors = new[]
    {
        Colors.Magenta, Colors.Orange, Colors.Pink, Colors.LightBlue,
        Colors.LightGreen, Colors.Salmon, Colors.Khaki, Colors.Plum,
        Colors.Coral, Colors.Turquoise, Colors.Gold, Colors.Violet
    };
    
    private static Color GetNextColor()
    {
        var color = _availableColors[_colorIndex % _availableColors.Length];
        _colorIndex++;
        return color;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// An editable point in the survey editor
/// </summary>
public class EditablePoint : INotifyPropertyChanged
{
    private double _x;
    private double _y;
    private double _z;
    private double? _kp;
    private double? _dcc;
    private double? _dal; // Distance Along Line
    private bool _isSelected;
    private bool _isModified;
    private bool _isDeleted;  // Marked for deletion
    private bool _isAdded;    // Newly digitized point
    private int _originalIndex;
    private SurveyPoint? _sourcePoint;

    public double X
    {
        get => _x;
        set { _x = value; IsModified = true; OnPropertyChanged(); }
    }

    public double Y
    {
        get => _y;
        set { _y = value; IsModified = true; OnPropertyChanged(); }
    }

    public double Z
    {
        get => _z;
        set { _z = value; IsModified = true; OnPropertyChanged(); }
    }
    
    public double? Kp
    {
        get => _kp;
        set { _kp = value; OnPropertyChanged(); }
    }
    
    public double? Dcc
    {
        get => _dcc;
        set { _dcc = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Distance Along Line from start of track
    /// </summary>
    public double? Dal
    {
        get => _dal;
        set { _dal = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public bool IsModified
    {
        get => _isModified;
        set { _isModified = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Marked for deletion (will show greyed out in tables)
    /// </summary>
    public bool IsDeleted
    {
        get => _isDeleted;
        set { _isDeleted = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Newly added/digitized point
    /// </summary>
    public bool IsAdded
    {
        get => _isAdded;
        set { _isAdded = value; OnPropertyChanged(); }
    }
    
    private bool _isExcluded;
    /// <summary>
    /// Excluded from generated layers (interval/smoothed/spline)
    /// When user deletes a point from a generated layer, it's marked excluded
    /// </summary>
    public bool IsExcluded
    {
        get => _isExcluded;
        set { _isExcluded = value; OnPropertyChanged(); }
    }
    
    private string _layerName = "";
    /// <summary>
    /// Name of the layer this point belongs to
    /// </summary>
    public string LayerName
    {
        get => _layerName;
        set { _layerName = value; OnPropertyChanged(); }
    }
    
    private double? _processedDepth;
    /// <summary>
    /// Processed depth after DAL calculation and smoothing
    /// </summary>
    public double? ProcessedDepth
    {
        get => _processedDepth;
        set { _processedDepth = value; OnPropertyChanged(); }
    }
    
    private double? _processedAltitude;
    /// <summary>
    /// Processed altitude after calculations and smoothing
    /// </summary>
    public double? ProcessedAltitude
    {
        get => _processedAltitude;
        set { _processedAltitude = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Point index (for undo/redo tracking)
    /// </summary>
    public int Index { get; set; }

    public int OriginalIndex
    {
        get => _originalIndex;
        set { _originalIndex = value; OnPropertyChanged(); }
    }

    public SurveyPoint? SourcePoint
    {
        get => _sourcePoint;
        set { _sourcePoint = value; OnPropertyChanged(); }
    }

    // Store original values for reset
    public double OriginalX { get; set; }
    public double OriginalY { get; set; }
    public double OriginalZ { get; set; }

    public void ResetToOriginal()
    {
        _x = OriginalX;
        _y = OriginalY;
        _z = OriginalZ;
        IsModified = false;
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Z));
    }

    public void ApplyToSource()
    {
        if (SourcePoint != null)
        {
            SourcePoint.SmoothedEasting = X;
            SourcePoint.SmoothedNorthing = Y;
        }
    }

    public static EditablePoint FromSurveyPoint(SurveyPoint point, int index)
    {
        // Get the latest processed depth (priority: SmoothedDepth > CalculatedZ > CorrectedDepth > Depth)
        double? processedDepth = point.SmoothedDepth ?? point.CalculatedZ ?? point.CorrectedDepth ?? point.Depth;
        
        // Get the latest processed altitude (priority: SmoothedAltitude > Altitude)
        double? processedAltitude = point.SmoothedAltitude ?? point.Altitude;
        
        return new EditablePoint
        {
            X = point.SmoothedEasting ?? point.Easting,
            Y = point.SmoothedNorthing ?? point.Northing,
            Z = point.CalculatedZ ?? point.Depth ?? 0,
            OriginalX = point.Easting,
            OriginalY = point.Northing,
            OriginalZ = point.Depth ?? 0,
            OriginalIndex = index,
            Index = index,
            SourcePoint = point,
            Kp = point.Kp,
            Dcc = point.Dcc,
            ProcessedDepth = processedDepth,
            ProcessedAltitude = processedAltitude,
            IsModified = false
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
