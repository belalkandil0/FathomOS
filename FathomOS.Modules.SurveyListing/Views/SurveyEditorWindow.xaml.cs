using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.Collections.Concurrent;
using System.Linq;
using MahApps.Metro.Controls;
using FathomOS.Core.Models;
using FathomOS.Core.Parsers;
using FathomOS.Core.Services;
using CalcSmoothing = FathomOS.Core.Calculations;
using FathomOS.Modules.SurveyListing.Models;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace FathomOS.Modules.SurveyListing.Views;

public partial class SurveyEditorWindow : MetroWindow
{
    #region Fields
    
    // Data
    private List<SurveyPoint> _surveyPoints = new();
    private List<EditablePoint> _editablePoints = new();
    private List<EditablePoint> _digitizedPoints = new();
    private RouteData? _routeData;
    private FieldLayout? _fieldLayout;
    
    // Services
    private readonly SmoothingService _smoothingService = new();
    private SmoothingSettings _smoothingSettings = new();
    
    // Undo/Redo system
    private Stack<EditorUndoAction> _undoStack = new();
    private Stack<EditorUndoAction> _redoStack = new();
    private const int MAX_UNDO_LEVELS = 50;
    
    // Layers
    public ObservableCollection<EditorLayer> Layers { get; } = new();
    private EditorLayer? _selectedLayer;
    
    // View state
    private double _zoomLevel = 1.0;
    private double _minX, _maxX, _minY, _maxY;
    private double _dataWidth, _dataHeight;
    private Point _lastMousePos;
    private bool _isPanning;
    private bool _isSelecting;
    private bool _isDraggingPoints;
    private Point _dragStartWorld;
    private List<Point> _dragOriginalPositions = new();
    private Path? _dragPreviewPath; // Lightweight drag preview
    private Point _selectionStart;
    private Rectangle? _selectionRect;
    
    // Tools
    private enum EditorTool { Pan, Select, Digitize, Measure, Insert, MeasureInterval, Polyline }
    private EditorTool _currentTool = EditorTool.Pan;
    
    // Polyline creation
    private List<EditablePoint> _currentPolylinePoints = new();
    private bool _isDrawingPolyline = false;
    private double _pickedZValue = 0; // Z value picked from an existing point
    
    // Selection
    private List<EditablePoint> _selectedPoints = new();
    
    // Snap settings
    private bool _snapEnabled = true;
    private bool _snapEndpoint = true;
    private bool _snapMidpoint = true;
    private bool _snapNearest = true;
    private bool _snapIntersection = false;
    private bool _snapGrid = false;
    private bool _snapPerpendicular = false;
    private double _snapRadius = 15.0;
    private double _gridSnapSize = 10.0;
    #pragma warning disable CS0414 // Field is assigned but never used (reserved for future use)
    private Point? _lastSnapPoint = null;  // For snap indicator
    #pragma warning restore CS0414
    
    // Measure tool
    private List<Point> _measurePoints = new();
    private List<Line> _measureLines = new();
    private double _measureCumulative = 0;
    private bool _measurePolygonClosed = false;
    
    // Insert blocks
    private List<UIElement> _insertedBlocks = new();
    private Color _blockColor = Colors.Yellow;
    private double _blockSize = 15;
    
    // MEASURE interval tool (AutoCAD-style)
    #pragma warning disable CS0414 // Fields are assigned but never used (reserved for future measure interval feature)
    private double _measureIntervalDistance = 1.0;  // Distance between points
    #pragma warning restore CS0414
    private List<EditablePoint> _measureIntervalPoints = new();
    #pragma warning disable CS0414
    private Polyline? _selectedPolylineForMeasure = null;
    #pragma warning restore CS0414
    
    // Point size scaling constants
    private const double MIN_POINT_SIZE = 3.0;   // Minimum point size in pixels
    private const double MAX_POINT_SIZE = 20.0;  // Maximum point size in pixels
    private const double BASE_POINT_SIZE = 8.0;  // Base point size at zoom 1.0
    
    // Update throttling
    private DateTime _lastSmoothingUpdate = DateTime.MinValue;
    private bool _isUpdating;
    private bool _isLoadingProperties;
    
    // Performance optimization fields
    private DispatcherTimer? _redrawTimer;
    private bool _redrawPending;
    private DateTime _lastRedrawRequest = DateTime.MinValue;
    private const int REDRAW_THROTTLE_MS = 16; // ~60 FPS max
    private const int REDRAW_MIN_INTERVAL_MS = 8; // Minimum 8ms between redraws
    
    // Viewport culling
    private Rect _visibleViewport = new Rect();
    private bool _viewportCullingEnabled = false; // Disabled by default - LOD handles performance better
    private const double VIEWPORT_PADDING = 100; // Extra padding for culling
    
    // Geometry caching
    private Dictionary<string, Geometry>? _geometryCache;
    #pragma warning disable CS0414 // Field is assigned but never used (reserved for cache invalidation)
    private int _geometryCacheVersion = 0;
    #pragma warning restore CS0414
    
    // Level of detail (LOD) for point rendering - configurable
    private int _maxPointsToDraw = 100000; // Increased from 10,000 - modern GPUs handle this easily
    private bool _lodEnabled = true; // Can be disabled for precision work
    private const double MIN_POINT_SPACING_PIXELS = 2.0; // Minimum pixel spacing between points
    
    // Parallel processing
    private readonly SemaphoreSlim _renderSemaphore = new SemaphoreSlim(1, 1);
    
    // Callback to notify Step 6 when changes are applied
    private Action<List<SurveyPoint>>? _onChangesApplied;
    private Action<EditorSyncData>? _onSyncDataApplied; // New comprehensive sync callback
    private List<SurveyPoint>? _sourcePointsList; // Reference to original list from Step 6
    
    // Track spline and interval points created in Editor
    private List<SurveyPoint> _editorSplinePoints = new();
    private List<(double X, double Y, double Z, double Distance)> _editorIntervalPoints = new();
    private List<int> _deletedPointIndices = new();
    
    #endregion

    public SurveyEditorWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeLayers();
        InitializePerformanceOptimizations();
        
        // Add keyboard event handlers for shortcuts
        // PreviewKeyDown fires before child elements consume the event
        this.PreviewKeyDown += Window_KeyDown;
        this.KeyDown += Window_KeyDown;
        this.Focusable = true;
        
        // Ensure focus is set when window is activated
        this.Activated += (s, e) => { this.Focus(); };
    }
    
    private void InitializePerformanceOptimizations()
    {
        // Enable hardware acceleration
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
        // Note: CachingHint is set in XAML as an attached property
        
        // Initialize geometry cache
        _geometryCache = new Dictionary<string, Geometry>();
        
        // Setup throttled redraw timer
        _redrawTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(REDRAW_MIN_INTERVAL_MS),
            IsEnabled = false
        };
        _redrawTimer.Tick += (s, e) =>
        {
            _redrawTimer.IsEnabled = false;
            if (_redrawPending)
            {
                _redrawPending = false;
                // Use fire-and-forget pattern for async operation
                _ = RedrawCanvasAsync();
            }
        };
        
        // Don't calculate viewport here - it will be done in Window_Loaded
    }

    #region Initialization
    
    private void InitializeLayers()
    {
        Layers.Add(EditorLayer.CreateDefault(EditorLayerType.Grid));
        Layers.Add(EditorLayer.CreateDefault(EditorLayerType.FieldLayout));
        // Route layer is NOT created by default - only when route data is loaded
        // Smoothed layer is NOT created by default - only when user enables smoothing
        
        // Survey Raw - create separate Points and Lines sub-layers
        var rawParent = EditorLayer.CreateDefault(EditorLayerType.SurveyRaw);
        rawParent.Name = "Survey Raw";
        rawParent.IsVisible = false;  // Parent is just a container, hide it
        Layers.Add(rawParent);
        
        var (rawPoints, rawLines) = EditorLayer.CreateSubLayers("Survey Raw", Colors.LimeGreen, 30);
        rawLines.Thickness = 1.5;
        rawPoints.PointSize = 4;
        Layers.Add(rawLines);
        Layers.Add(rawPoints);
        
        // Digitized Points (points only by default)
        Layers.Add(EditorLayer.CreateDefault(EditorLayerType.Digitized));
        
        // Subscribe to layer changes
        foreach (var layer in Layers)
        {
            layer.PropertyChanged += Layer_PropertyChanged;
        }
        
        // Subscribe to collection changes to update smoothing dropdown
        Layers.CollectionChanged += (s, e) => UpdateSmoothingSourceDropdown();
        
        // Initial population of smoothing dropdown
        UpdateSmoothingSourceDropdown();
    }
    
    private void UpdateSmoothingSourceDropdown()
    {
        if (CboSmoothingSource == null) return;
        
        var selectedLayer = CboSmoothingSource.SelectedItem as EditorLayer;
        
        // Get all layers that could have points for smoothing
        // Include: Survey layers, Custom, Digitized, Interval, Spline, Polyline, PointsOnly
        // Exclude: Grid, FieldLayout, container layers without points
        var layersWithPoints = Layers.Where(l => 
            l.LayerType != EditorLayerType.Grid && 
            l.LayerType != EditorLayerType.FieldLayout &&
            !string.IsNullOrEmpty(l.Name) &&
            (l.Points != null && l.Points.Count > 0 || 
             l.LayerType == EditorLayerType.SurveyRaw || 
             l.LayerType == EditorLayerType.SurveySmoothed ||
             l.ParentLayerName == "Survey Raw" ||
             l.ParentLayerName?.Contains("Smoothed") == true)
        ).ToList();
        
        CboSmoothingSource.ItemsSource = layersWithPoints;
        
        // Try to restore selection or select first item
        if (selectedLayer != null && layersWithPoints.Contains(selectedLayer))
        {
            CboSmoothingSource.SelectedItem = selectedLayer;
        }
        else if (layersWithPoints.Count > 0)
        {
            // Try to select Survey Raw Points by default
            var rawPoints = layersWithPoints.FirstOrDefault(l => l.Name == "Survey Raw - Points");
            CboSmoothingSource.SelectedItem = rawPoints ?? layersWithPoints.First();
        }
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CalculateBounds();
        ZoomToFit();
        UpdateVisibleViewport();
        RequestRedraw();
    }
    
    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Check for unsaved changes
        int modifiedCount = _editablePoints.Count(p => p.IsModified) + _digitizedPoints.Count;
        if (modifiedCount > 0)
        {
            var result = MessageBox.Show(
                $"You have {modifiedCount} unsaved changes. Apply changes before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                ApplyChangesToSource();
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
        }
    }
    
    /// <summary>
    /// Handle keyboard shortcuts
    /// </summary>
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        bool shift = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        
        switch (e.Key)
        {
            case Key.Delete:
                // Delete selected points
                if (_selectedPoints.Count > 0)
                {
                    DeleteSelectedPoints();
                    e.Handled = true;
                }
                break;
                
            case Key.A:
                // Ctrl+A = Select All
                if (ctrl)
                {
                    SelectAllPoints();
                    e.Handled = true;
                }
                break;
                
            case Key.Escape:
                // Escape = Deselect all / Cancel current operation / Cancel polyline
                if (_isDrawingPolyline)
                {
                    CancelPolyline();
                    e.Handled = true;
                }
                else if (_currentTool == EditorTool.Select && _isSelecting)
                {
                    _isSelecting = false;
                    RequestRedraw();
                }
                else if (_selectedPoints.Count > 0)
                {
                    ClearSelection();
                }
                else if (_currentTool != EditorTool.Select)
                {
                    // Return to Select tool
                    _currentTool = EditorTool.Select;
                    UpdateToolButtonStates();
                }
                e.Handled = true;
                break;
                
            case Key.Enter:
                // Enter = Finish polyline
                if (_isDrawingPolyline && _currentPolylinePoints.Count >= 2)
                {
                    FinishPolyline(false);
                    e.Handled = true;
                }
                break;
                
            case Key.C:
                // C = Close polyline (connect end to start)
                if (_isDrawingPolyline && _currentPolylinePoints.Count >= 3 && !ctrl)
                {
                    FinishPolyline(true);
                    e.Handled = true;
                }
                break;
                
            case Key.Back:
                // Backspace = Remove last polyline point
                if (_isDrawingPolyline && _currentPolylinePoints.Count > 0)
                {
                    _currentPolylinePoints.RemoveAt(_currentPolylinePoints.Count - 1);
                    BtnFinishPolyline.IsEnabled = _currentPolylinePoints.Count >= 2;
                    BtnClosePolyline.IsEnabled = _currentPolylinePoints.Count >= 3;
                    TxtPolylineStatus.Text = _currentPolylinePoints.Count > 0 
                        ? $"Points: {_currentPolylinePoints.Count} | Backspace removed last point"
                        : "Click to start polyline...";
                    if (_currentPolylinePoints.Count == 0)
                        _isDrawingPolyline = false;
                    RedrawCanvas();
                    e.Handled = true;
                }
                break;
                
            case Key.F:
                // F = Zoom to Fit
                ZoomToFit();
                e.Handled = true;
                break;
                
            case Key.Z:
                // Ctrl+Z = Undo
                if (ctrl)
                {
                    PerformUndo();
                    e.Handled = true;
                }
                break;
                
            case Key.Y:
                // Ctrl+Y = Redo
                if (ctrl)
                {
                    PerformRedo();
                    e.Handled = true;
                }
                break;
                
            case Key.S:
                // Ctrl+S = Save/Apply changes, S alone = Select tool
                if (ctrl)
                {
                    ApplyChangesToSource();
                    e.Handled = true;
                }
                else if (!IsTextBoxFocused())
                {
                    _currentTool = EditorTool.Select;
                    UpdateToolButtonStates();
                    TxtStatus.Text = "Tool: Select (S)";
                    e.Handled = true;
                }
                break;
                
            case Key.Left:
            case Key.Right:
            case Key.Up:
            case Key.Down:
                // Arrow keys = Move selected points (with Shift for fine movement)
                if (_selectedPoints.Count > 0)
                {
                    double moveAmount = shift ? 0.1 : 1.0; // meters (fine vs coarse)
                    MoveSelectedPoints(e.Key, moveAmount);
                    e.Handled = true;
                }
                break;
                
            case Key.D1:
            case Key.NumPad1:
                // Allow number input - don't handle
                break;
                
            case Key.D2:
            case Key.NumPad2:
                // Allow number input - don't handle
                break;
                
            case Key.D3:
            case Key.NumPad3:
                // Allow number input - don't handle
                break;
                
            case Key.D4:
            case Key.NumPad4:
                // Allow number input - don't handle
                break;
                
            case Key.P:
                // P = Pan tool
                if (!IsTextBoxFocused())
                {
                    _currentTool = EditorTool.Pan;
                    UpdateToolButtonStates();
                    TxtStatus.Text = "Tool: Pan (P)";
                    e.Handled = true;
                }
                break;
                
            case Key.M:
                // M = Measure tool
                if (!IsTextBoxFocused())
                {
                    _currentTool = EditorTool.Measure;
                    UpdateToolButtonStates();
                    TxtStatus.Text = "Tool: Measure (M)";
                    e.Handled = true;
                }
                break;
                
            case Key.D:
                // D = Digitize tool
                if (!IsTextBoxFocused())
                {
                    _currentTool = EditorTool.Digitize;
                    UpdateToolButtonStates();
                    TxtStatus.Text = "Tool: Digitize (D)";
                    e.Handled = true;
                }
                break;
                
            case Key.H:
            case Key.F1:
                // H or F1 = Show help
                ShowKeyboardShortcutsHelp();
                e.Handled = true;
                break;
        }
    }
    
    /// <summary>
    /// Check if a TextBox or editable control is focused
    /// </summary>
    private bool IsTextBoxFocused()
    {
        var focusedElement = Keyboard.FocusedElement;
        return focusedElement is System.Windows.Controls.TextBox || 
               focusedElement is System.Windows.Controls.ComboBox;
    }
    
    /// <summary>
    /// Show keyboard shortcuts help dialog
    /// </summary>
    private void ShowKeyboardShortcutsHelp()
    {
        try
        {
            var helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening help: {ex.Message}", "Help Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    /// <summary>
    /// Select all visible/editable points
    /// </summary>
    private void SelectAllPoints()
    {
        _selectedPoints.Clear();
        
        // Check if main survey layer is locked
        var smoothedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveySmoothed);
        var rawLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveyRaw);
        bool mainLayerLocked = (smoothedLayer?.IsLocked == true) && (rawLayer?.IsLocked == true);
        
        // Only select from main points if layer is NOT locked
        if (!mainLayerLocked)
        {
            foreach (var pt in _editablePoints)
            {
                if (!_deletedPointIndices.Contains(pt.OriginalIndex))
                {
                    pt.IsSelected = true;
                    _selectedPoints.Add(pt);
                }
            }
        }
        
        // Also select from unlocked custom layers
        foreach (var layer in Layers.Where(l => l.Points != null && l.IsSelectable && !l.IsLocked))
        {
            foreach (var pt in layer.Points!)
            {
                pt.IsSelected = true;
                _selectedPoints.Add(pt);
            }
        }
        
        // Also select from digitized layer if not locked
        var digitizedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.Digitized);
        if (digitizedLayer?.IsLocked != true)
        {
            foreach (var pt in _digitizedPoints)
            {
                pt.IsSelected = true;
                _selectedPoints.Add(pt);
            }
        }
        
        UpdateSelectionUI();
        RequestRedraw();
        TxtStatus.Text = $"Selected all {_selectedPoints.Count} points";
    }
    
    /// <summary>
    /// Delete selected points (mark for deletion)
    /// </summary>
    private void DeleteSelectedPoints()
    {
        if (_selectedPoints.Count == 0) return;
        
        // Check if any selected points are from locked layers
        // (This can happen if user selected points, then locked the layer)
        var smoothedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveySmoothed);
        var rawLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveyRaw);
        var digitizedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.Digitized);
        
        // Filter out points from locked layers
        var deletablePoints = _selectedPoints.Where(pt => 
        {
            // Check if this point is from the main editable points (SurveySmoothed/Raw)
            if (_editablePoints.Contains(pt))
            {
                return smoothedLayer?.IsLocked != true || rawLayer?.IsLocked != true;
            }
            // Check if from digitized layer
            if (_digitizedPoints.Contains(pt))
            {
                return digitizedLayer?.IsLocked != true;
            }
            // For custom layer points, check that layer
            foreach (var layer in Layers.Where(l => l.Points != null && l.IsLocked))
            {
                if (layer.Points!.Contains(pt)) return false;
            }
            return true;
        }).ToList();
        
        if (deletablePoints.Count == 0)
        {
            TxtStatus.Text = "🔒 Cannot delete - layer is locked";
            return;
        }
        
        if (deletablePoints.Count < _selectedPoints.Count)
        {
            TxtStatus.Text = $"🔒 Some points skipped (locked layers)";
        }
        
        // Create undo action before deleting
        var pointsToDelete = deletablePoints.Where(pt => !_deletedPointIndices.Contains(pt.OriginalIndex)).ToList();
        if (pointsToDelete.Count > 0)
        {
            PushUndoAction(EditorUndoAction.CreatePointsDeleted(pointsToDelete));
        }
        
        int deletedCount = 0;
        foreach (var pt in deletablePoints)
        {
            if (!_deletedPointIndices.Contains(pt.OriginalIndex))
            {
                _deletedPointIndices.Add(pt.OriginalIndex);
                pt.IsDeleted = true;
                deletedCount++;
            }
        }
        
        ClearSelection();
        RequestRedraw();
        TxtStatus.Text = $"Marked {deletedCount} points for deletion";
    }
    
    /// <summary>
    /// Move selected points with arrow keys
    /// </summary>
    private void MoveSelectedPoints(Key direction, double amount)
    {
        if (_selectedPoints.Count == 0) return;
        
        // Check if any selected points are from locked layers
        var smoothedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveySmoothed);
        var rawLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveyRaw);
        var digitizedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.Digitized);
        
        // Filter out points from locked layers
        var movablePoints = _selectedPoints.Where(pt => 
        {
            if (_editablePoints.Contains(pt))
            {
                return smoothedLayer?.IsLocked != true || rawLayer?.IsLocked != true;
            }
            if (_digitizedPoints.Contains(pt))
            {
                return digitizedLayer?.IsLocked != true;
            }
            foreach (var layer in Layers.Where(l => l.Points != null && l.IsLocked))
            {
                if (layer.Points!.Contains(pt)) return false;
            }
            return true;
        }).ToList();
        
        if (movablePoints.Count == 0)
        {
            TxtStatus.Text = "🔒 Cannot move - layer is locked";
            return;
        }
        
        // Store original positions for undo
        var originalPositions = movablePoints.Select(p => new Point(p.X, p.Y)).ToList();
        
        double dx = 0, dy = 0;
        
        switch (direction)
        {
            case Key.Left:  dx = -amount; break;
            case Key.Right: dx = amount; break;
            case Key.Up:    dy = amount; break;
            case Key.Down:  dy = -amount; break;
        }
        
        foreach (var pt in movablePoints)
        {
            pt.X += dx;
            pt.Y += dy;
        }
        
        // Push undo action
        PushUndoAction(EditorUndoAction.CreatePointsMoved(movablePoints, originalPositions));
        
        RequestRedraw();
        
        if (movablePoints.Count < _selectedPoints.Count)
        {
            TxtStatus.Text = $"🔒 Moved {movablePoints.Count} points (some locked)";
        }
        else
        {
            TxtStatus.Text = $"Moved {movablePoints.Count} points by ({dx:F1}, {dy:F1})";
        }
    }
    
    #endregion

    #region Data Loading
    
    /// <summary>
    /// Load survey data into the editor
    /// </summary>
    /// <param name="points">Survey points to load</param>
    /// <param name="onChangesApplied">Optional callback when changes are applied (for syncing with Step 6)</param>
    /// <param name="sourcePointsList">Optional reference to original list from Step 6 (for direct updates)</param>
    /// <param name="onSyncDataApplied">Comprehensive sync callback for all editor changes</param>
    public void LoadSurveyData(IList<SurveyPoint> points, Action<List<SurveyPoint>>? onChangesApplied = null, 
        List<SurveyPoint>? sourcePointsList = null, Action<EditorSyncData>? onSyncDataApplied = null)
    {
        _surveyPoints = points.ToList();
        _editablePoints = points.Select((p, i) => EditablePoint.FromSurveyPoint(p, i)).ToList();
        _onChangesApplied = onChangesApplied;
        _onSyncDataApplied = onSyncDataApplied;
        _sourcePointsList = sourcePointsList;
        
        // Clear editor-specific data
        _editorSplinePoints.Clear();
        _editorIntervalPoints.Clear();
        _deletedPointIndices.Clear();
        
        TxtTotalPoints.Text = _surveyPoints.Count.ToString();
        TxtStatus.Text = $"Loaded {_surveyPoints.Count} survey points (raw data - enable smoothing if needed)";
        
        CalculateBounds();
        
        // DON'T auto-apply smoothing - let user choose when to smooth
        // All smoothing checkboxes start unchecked by default
        
        RedrawCanvas();
    }
    
    /// <summary>
    /// Load survey data with a pre-generated spline layer
    /// </summary>
    /// <param name="points">Main survey points</param>
    /// <param name="splinePoints">Spline-fitted points</param>
    /// <param name="route">Optional route data</param>
    /// <param name="onChangesApplied">Optional callback when changes are applied (for syncing with Step 6)</param>
    /// <param name="sourcePointsList">Optional reference to original list from Step 6 (for direct updates)</param>
    /// <param name="onSyncDataApplied">Comprehensive sync callback for all editor changes</param>
    public void LoadSurveyDataWithSpline(IList<SurveyPoint> points, IList<SurveyPoint> splinePoints, 
        RouteData? route = null, Action<List<SurveyPoint>>? onChangesApplied = null, 
        List<SurveyPoint>? sourcePointsList = null, Action<EditorSyncData>? onSyncDataApplied = null)
    {
        // Load main survey data
        _surveyPoints = points.ToList();
        _editablePoints = points.Select((p, i) => EditablePoint.FromSurveyPoint(p, i)).ToList();
        _onChangesApplied = onChangesApplied;
        _onSyncDataApplied = onSyncDataApplied;
        _sourcePointsList = sourcePointsList;
        
        // Clear and store editor spline points
        _editorSplinePoints = splinePoints.ToList();
        _editorIntervalPoints.Clear();
        _deletedPointIndices.Clear();
        
        // Load route if provided
        if (route != null)
            _routeData = route;
        
        // Create spline layer
        var splineLayer = new EditorLayer(EditorLayerType.Custom, "Spline Fit", Colors.Orange)
        {
            IsVisible = true,
            IsLocked = false,
            PointSize = 4,
            Thickness = 2,
            Opacity = 1.0,
            ZIndex = Layers.Count + 1,
            IsUserCreated = true,
            Points = new ObservableCollection<EditablePoint>()
        };
        
        foreach (var pt in splinePoints)
        {
            splineLayer.Points.Add(new EditablePoint
            {
                X = pt.SmoothedEasting ?? pt.Easting,
                Y = pt.SmoothedNorthing ?? pt.Northing,
                Z = pt.CorrectedDepth ?? pt.Depth ?? 0,
                OriginalX = pt.Easting,
                OriginalY = pt.Northing,
                OriginalZ = pt.Depth ?? 0,
                IsModified = false
            });
        }
        
        splineLayer.PropertyChanged += Layer_PropertyChanged;
        Layers.Add(splineLayer);
        
        TxtTotalPoints.Text = _surveyPoints.Count.ToString();
        TxtStatus.Text = $"Loaded {_surveyPoints.Count} survey points + {splinePoints.Count} spline points (raw data)";
        
        CalculateBounds();
        
        // DON'T auto-apply smoothing - let user choose
        // All smoothing checkboxes start unchecked by default
        
        RedrawCanvas();
    }
    
    /// <summary>
    /// Load interval points as a new layer
    /// </summary>
    public void LoadIntervalPoints(List<(double X, double Y, double Z, double Distance)> intervalPoints)
    {
        if (intervalPoints == null || intervalPoints.Count == 0) return;
        
        // Create interval points layer
        var intervalLayer = new EditorLayer(EditorLayerType.Custom, "Interval Points", Colors.Magenta)
        {
            IsVisible = true,
            IsLocked = false,
            PointSize = 4,
            Thickness = 1,
            Opacity = 1.0,
            ZIndex = Layers.Count + 1,
            IsUserCreated = true,
            Points = new ObservableCollection<EditablePoint>()
        };
        
        foreach (var pt in intervalPoints)
        {
            intervalLayer.Points.Add(new EditablePoint
            {
                X = pt.X,
                Y = pt.Y,
                Z = pt.Z,
                OriginalX = pt.X,
                OriginalY = pt.Y,
                OriginalZ = pt.Z,
                Kp = pt.Distance / 1000.0,  // Convert to KP (km)
                IsModified = false
            });
        }
        
        intervalLayer.PropertyChanged += Layer_PropertyChanged;
        Layers.Add(intervalLayer);
        
        TxtStatus.Text = $"{TxtStatus.Text} + {intervalPoints.Count} interval points";
        CalculateBounds();
    }
    
    public void LoadRouteData(RouteData route)
    {
        _routeData = route;
        
        // Create Route layer if it doesn't exist
        if (!Layers.Any(l => l.LayerType == EditorLayerType.Route))
        {
            var routeLayer = EditorLayer.CreateDefault(EditorLayerType.Route);
            routeLayer.PropertyChanged += Layer_PropertyChanged;
            Layers.Insert(0, routeLayer);  // Insert at beginning so it draws behind survey data
        }
        
        CalculateBounds();
        RedrawCanvas();
    }
    
    public void LoadFieldLayout(string dxfPath)
    {
        try
        {
            var parser = new DxfLayoutParser();
            _fieldLayout = parser.Parse(dxfPath);
            
            if (_fieldLayout.TotalEntities == 0)
            {
                MessageBox.Show("The DXF file was loaded but contains no visible entities.\n\n" +
                    "This may be due to:\n" +
                    "• All entities being on frozen/hidden layers\n" +
                    "• The file containing only unsupported entity types\n" +
                    "• The file being empty",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            CalculateBounds();
            RedrawCanvas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading field layout:\n\n{ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            _fieldLayout = null;
        }
    }
    
    #endregion

    #region Bounds & Coordinate Transform
    
    private void CalculateBounds()
    {
        _minX = double.MaxValue;
        _maxX = double.MinValue;
        _minY = double.MaxValue;
        _maxY = double.MinValue;
        
        bool hasData = false;
        
        // Survey points
        foreach (var pt in _editablePoints)
        {
            _minX = Math.Min(_minX, pt.X);
            _maxX = Math.Max(_maxX, pt.X);
            _minY = Math.Min(_minY, pt.Y);
            _maxY = Math.Max(_maxY, pt.Y);
            hasData = true;
        }
        
        // Route
        if (_routeData != null)
        {
            foreach (var seg in _routeData.Segments)
            {
                _minX = Math.Min(_minX, Math.Min(seg.StartEasting, seg.EndEasting));
                _maxX = Math.Max(_maxX, Math.Max(seg.StartEasting, seg.EndEasting));
                _minY = Math.Min(_minY, Math.Min(seg.StartNorthing, seg.EndNorthing));
                _maxY = Math.Max(_maxY, Math.Max(seg.StartNorthing, seg.EndNorthing));
                hasData = true;
            }
        }
        
        // Field layout
        if (_fieldLayout != null && _fieldLayout.TotalEntities > 0)
        {
            _minX = Math.Min(_minX, _fieldLayout.MinX);
            _maxX = Math.Max(_maxX, _fieldLayout.MaxX);
            _minY = Math.Min(_minY, _fieldLayout.MinY);
            _maxY = Math.Max(_maxY, _fieldLayout.MaxY);
            hasData = true;
        }
        
        // Handle no data case - use default bounds
        if (!hasData)
        {
            _minX = 0;
            _maxX = 1000;
            _minY = 0;
            _maxY = 1000;
        }
        
        // Calculate dimensions with padding
        _dataWidth = _maxX - _minX;
        _dataHeight = _maxY - _minY;
        
        // Ensure minimum dimensions
        if (_dataWidth <= 0) _dataWidth = 1000;
        if (_dataHeight <= 0) _dataHeight = 1000;
        
        // Add 5% padding
        double paddingX = _dataWidth * 0.05;
        double paddingY = _dataHeight * 0.05;
        _minX -= paddingX;
        _maxX += paddingX;
        _minY -= paddingY;
        _maxY += paddingY;
        _dataWidth = _maxX - _minX;
        _dataHeight = _maxY - _minY;
    }
    
    private Point WorldToScreen(double worldX, double worldY)
    {
        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;
        
        if (canvasWidth <= 0 || canvasHeight <= 0)
            return new Point(0, 0);
        
        double scaleX = canvasWidth / _dataWidth;
        double scaleY = canvasHeight / _dataHeight;
        double scale = Math.Min(scaleX, scaleY);
        
        double offsetX = (canvasWidth - _dataWidth * scale) / 2;
        double offsetY = (canvasHeight - _dataHeight * scale) / 2;
        
        double screenX = offsetX + (worldX - _minX) * scale;
        double screenY = canvasHeight - (offsetY + (worldY - _minY) * scale); // Flip Y
        
        return new Point(screenX, screenY);
    }
    
    private (double X, double Y) ScreenToWorld(Point screenPoint)
    {
        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;
        
        if (canvasWidth <= 0 || canvasHeight <= 0)
            return (0, 0);
        
        // PROFESSIONAL SOLUTION: Use WPF's Transform.Inverse to properly convert coordinates
        // Step 1: Apply inverse of RenderTransform to get canvas LOCAL coordinates
        // GetPosition(MainCanvas) returns coordinates that need to be inverse-transformed
        Point canvasLocalPoint = screenPoint;
        
        var renderTransform = MainCanvas.RenderTransform;
        if (renderTransform != null && renderTransform != Transform.Identity)
        {
            try
            {
                var inverseTransform = renderTransform.Inverse;
                if (inverseTransform != null)
                {
                    canvasLocalPoint = inverseTransform.Transform(screenPoint);
                }
            }
            catch
            {
                // If transform is not invertible, use original point
            }
        }
        
        // Step 2: Convert from canvas local coordinates to world coordinates
        // This is the inverse of WorldToScreen
        double baseScaleX = canvasWidth / _dataWidth;
        double baseScaleY = canvasHeight / _dataHeight;
        double baseScale = Math.Min(baseScaleX, baseScaleY);
        
        double offsetX = (canvasWidth - _dataWidth * baseScale) / 2;
        double offsetY = (canvasHeight - _dataHeight * baseScale) / 2;
        
        // WorldToScreen: screenX = offsetX + (worldX - _minX) * baseScale
        // Inverse: worldX = _minX + (canvasLocalX - offsetX) / baseScale
        double worldX = _minX + (canvasLocalPoint.X - offsetX) / baseScale;
        
        // WorldToScreen: screenY = canvasHeight - (offsetY + (worldY - _minY) * baseScale)
        // Inverse: worldY = _minY + (canvasHeight - canvasLocalY - offsetY) / baseScale
        double worldY = _minY + (canvasHeight - canvasLocalPoint.Y - offsetY) / baseScale;
        
        return (worldX, worldY);
    }
    
    /// <summary>
    /// Calculate point size that scales with zoom like AutoCAD
    /// Returns a canvas pixel size that will appear at the desired screen size
    /// Since canvas has RenderTransform (scale), we divide by zoom to keep apparent size constant
    /// </summary>
    private double GetZoomResponsivePointSize(double layerPointSize = 4.0)
    {
        // The canvas is scaled by _zoomLevel via RenderTransform
        // So a 4px dot drawn on canvas appears as 4*_zoomLevel pixels on screen
        // To keep the APPARENT size constant, we divide by zoom level
        
        double zoomLevel = _zoomLevel;
        if (zoomLevel <= 0) zoomLevel = 1.0;
        
        // Target screen size is the layer point size (default 4px)
        // Divide by zoom to keep apparent size constant
        double canvasSize = layerPointSize / zoomLevel;
        
        // Clamp to reasonable canvas pixel limits
        // Minimum 0.5px for rendering, maximum 50px to avoid huge memory usage
        return Math.Max(0.5, Math.Min(50.0, canvasSize));
    }
    
    /// <summary>
    /// Get layer-aware line thickness that scales with zoom
    /// Divides by zoom to keep apparent thickness constant
    /// </summary>
    private double GetLayerLineThickness(double layerThickness)
    {
        double zoomLevel = _zoomLevel;
        if (zoomLevel <= 0) zoomLevel = 1.0;
        
        // Divide by zoom to keep apparent thickness constant
        double canvasThickness = layerThickness / zoomLevel;
        
        // Clamp to reasonable limits
        return Math.Max(0.3, Math.Min(20.0, canvasThickness));
    }
    
    /// <summary>
    /// Get zoom-responsive hit area size for point selection
    /// Hit area should be in canvas coordinates but feel the same size on screen
    /// </summary>
    private double GetZoomResponsiveHitAreaSize()
    {
        double zoomLevel = _zoomLevel;
        if (zoomLevel <= 0) zoomLevel = 1.0;
        
        // We want hit area to be about 8-10 pixels on SCREEN for easy clicking
        // So divide by zoom to get canvas coordinates
        double targetScreenSize = 10.0;
        double canvasSize = targetScreenSize / zoomLevel;
        
        // At very high zoom, don't let hit area get too small in canvas space
        // At very low zoom, don't let it get too huge
        return Math.Max(2.0, Math.Min(30.0, canvasSize));
    }
    
    /// <summary>
    /// Get the current effective snap radius in world coordinates
    /// </summary>
    private double GetWorldSnapRadius()
    {
        if (MainCanvas.ActualWidth <= 0 || MainCanvas.ActualHeight <= 0)
            return _snapRadius;
            
        double scale = Math.Min(
            MainCanvas.ActualWidth / _dataWidth,
            MainCanvas.ActualHeight / _dataHeight);
            
        return _snapRadius / (_zoomLevel * scale);
    }
    
    #endregion

    #region Canvas Drawing
    
    /// <summary>
    /// Request a redraw with throttling
    /// </summary>
    private void RequestRedraw()
    {
        if (MainCanvas == null) return;
        
        _redrawPending = true;
        var now = DateTime.Now;
        
        // If enough time has passed, redraw immediately
        if ((now - _lastRedrawRequest).TotalMilliseconds >= REDRAW_THROTTLE_MS)
        {
            _lastRedrawRequest = now;
            _ = RedrawCanvasAsync();
        }
        else
        {
            // Schedule redraw via timer
            if (_redrawTimer != null && !_redrawTimer.IsEnabled)
            {
                _redrawTimer.IsEnabled = true;
            }
        }
    }
    
    /// <summary>
    /// Update visible viewport for culling
    /// </summary>
    private void UpdateVisibleViewport()
    {
        if (MainCanvas == null || MainCanvas.ActualWidth <= 0 || MainCanvas.ActualHeight <= 0)
        {
            // Use safe defaults if canvas not ready
            if (_dataWidth > 0 && _dataHeight > 0)
            {
                _visibleViewport = new Rect(_minX, _minY, _dataWidth, _dataHeight);
            }
            else
            {
                _visibleViewport = new Rect(0, 0, 1000, 1000); // Default viewport
            }
            return;
        }
        
        // Ensure data bounds are valid
        if (_dataWidth <= 0) _dataWidth = 1000;
        if (_dataHeight <= 0) _dataHeight = 1000;
        
        var transform = MainCanvas.RenderTransform as TransformGroup;
        var scale = transform?.Children.OfType<ScaleTransform>().FirstOrDefault();
        var translate = transform?.Children.OfType<TranslateTransform>().FirstOrDefault();
        
        double scaleX = scale?.ScaleX ?? 1.0;
        double scaleY = scale?.ScaleY ?? 1.0;
        double offsetX = translate?.X ?? 0;
        double offsetY = translate?.Y ?? 0;
        
        // Calculate visible world coordinates
        double canvasWidth = MainCanvas.ActualWidth;
        double canvasHeight = MainCanvas.ActualHeight;
        
        double baseScale = Math.Min(canvasWidth / _dataWidth, canvasHeight / _dataHeight);
        if (baseScale <= 0) baseScale = 1.0; // Prevent division by zero
        double effectiveScale = baseScale * _zoomLevel;
        if (effectiveScale <= 0) effectiveScale = 1.0; // Prevent division by zero
        
        // Convert screen bounds to world bounds
        double worldLeft = _minX - (offsetX / effectiveScale) - (VIEWPORT_PADDING / effectiveScale);
        double worldRight = _maxX - (offsetX / effectiveScale) + (VIEWPORT_PADDING / effectiveScale);
        double worldTop = _maxY + (offsetY / effectiveScale) + (VIEWPORT_PADDING / effectiveScale);
        double worldBottom = _minY + (offsetY / effectiveScale) - (VIEWPORT_PADDING / effectiveScale);
        
        _visibleViewport = new Rect(
            Math.Max(_minX, worldLeft),
            Math.Max(_minY, worldBottom),
            Math.Min(_dataWidth, Math.Max(0, worldRight - worldLeft)),
            Math.Min(_dataHeight, Math.Max(0, worldTop - worldBottom))
        );
    }
    
    /// <summary>
    /// Check if a point is visible in the viewport
    /// </summary>
    private bool IsPointVisible(double x, double y)
    {
        if (!_viewportCullingEnabled) return true;
        return _visibleViewport.Contains(x, y);
    }
    
    /// <summary>
    /// Async redraw with performance optimizations
    /// </summary>
    private async Task RedrawCanvasAsync()
    {
        if (MainCanvas == null || _isUpdating) return;
        
        // Prevent concurrent redraws
        if (!await _renderSemaphore.WaitAsync(0))
            return;
        
        try
        {
            _isUpdating = true;
            
            // Update viewport on UI thread first
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateVisibleViewport();
            }, DispatcherPriority.Background);
            
            // Capture viewport for background thread
            var viewport = _visibleViewport;
            
            // Run heavy calculations on background thread
            var renderData = await Task.Run(() => PrepareRenderData(viewport));
            
            // Update UI on UI thread
            await Dispatcher.InvokeAsync(() =>
            {
                if (MainCanvas == null) return;
                
                try
                {
                    MainCanvas.Children.Clear();
                    
                    // Draw in order of z-index using optimized methods
                    foreach (var layer in Layers.OrderBy(l => l.ZIndex))
                    {
                        if (!layer.IsVisible) continue;
                        
                        try
                        {
                            switch (layer.LayerType)
                            {
                                case EditorLayerType.Grid:
                                    DrawGridOptimized(layer);
                                    DrawCoordinateIndicators(); // Add coordinate axes and origin
                                    break;
                                case EditorLayerType.FieldLayout:
                                    DrawFieldLayoutOptimized(layer);
                                    break;
                                case EditorLayerType.Route:
                                    DrawRouteOptimized(layer);
                                    break;
                                case EditorLayerType.SurveyRaw:
                                    // Parent layer - skip if hidden
                                    break;
                                case EditorLayerType.SurveySmoothed:
                                    // Parent layer - skip if hidden
                                    break;
                                case EditorLayerType.PointsOnly:
                                    DrawPointsOnlyLayer(layer, renderData);
                                    break;
                                case EditorLayerType.LinesOnly:
                                    DrawLinesOnlyLayer(layer, renderData);
                                    break;
                                case EditorLayerType.Digitized:
                                    DrawDigitizedPointsOptimized(layer);
                                    break;
                                case EditorLayerType.Custom:
                                    DrawCustomLayerOptimized(layer);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error drawing layer {layer.LayerType}: {ex.Message}");
                        }
                    }
                    
                    // Draw KP labels if enabled
                    if (ChkShowKpLabels?.IsChecked == true)
                    {
                        DrawKpLabelsOptimized();
                    }
                    
                    // Draw selection highlights last
                    DrawSelectionOptimized();
                    
                    // Draw current polyline being created
                    DrawPolylinePreview();
                    
                    // Update mini-map (less frequently)
                    UpdateMiniMap();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in RedrawCanvasAsync: {ex.Message}");
                }
            }, DispatcherPriority.Render);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in RedrawCanvasAsync: {ex.Message}");
        }
        finally
        {
            _isUpdating = false;
            _renderSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Prepare render data on background thread
    /// </summary>
    private RenderData PrepareRenderData(Rect viewport)
    {
        var data = new RenderData();
        
        // Calculate visible points with parallel processing
        if (_editablePoints.Count > 0)
        {
            var visiblePoints = new ConcurrentBag<EditablePoint>();
            var visiblePointsOriginal = new ConcurrentBag<EditablePoint>();
            
            Parallel.ForEach(_editablePoints, point =>
            {
                if (IsPointVisibleInViewport(point.X, point.Y, viewport))
                {
                    visiblePoints.Add(point);
                }
                if (IsPointVisibleInViewport(point.OriginalX, point.OriginalY, viewport))
                {
                    visiblePointsOriginal.Add(point);
                }
            });
            
            data.VisiblePoints = visiblePoints.ToList();
            data.VisiblePointsOriginal = visiblePointsOriginal.ToList();
            
            // Apply level of detail - sample points if too many (only if LOD is enabled)
            if (_lodEnabled && data.VisiblePoints.Count > _maxPointsToDraw)
            {
                int step = (int)Math.Ceiling((double)data.VisiblePoints.Count / _maxPointsToDraw);
                data.VisiblePoints = data.VisiblePoints.Where((p, i) => i % step == 0).ToList();
            }
        }
        
        return data;
    }
    
    /// <summary>
    /// Check if point is visible in viewport (thread-safe version)
    /// </summary>
    private bool IsPointVisibleInViewport(double x, double y, Rect viewport)
    {
        if (!_viewportCullingEnabled) return true;
        return viewport.Contains(x, y);
    }
    
    /// <summary>
    /// Render data prepared on background thread
    /// </summary>
    private class RenderData
    {
        public List<EditablePoint> VisiblePoints { get; set; } = new();
        public List<EditablePoint> VisiblePointsOriginal { get; set; } = new();
    }
    
    /// <summary>
    /// Legacy synchronous redraw (for compatibility)
    /// </summary>
    private void RedrawCanvas()
    {
        RequestRedraw();
    }
    
    private void DrawCustomLayer(EditorLayer layer)
    {
        if (layer.Points == null || layer.Points.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        var pen = new Pen(brush, layer.Thickness);
        
        // Draw lines
        for (int i = 1; i < layer.Points.Count; i++)
        {
            var p1 = WorldToScreen(layer.Points[i - 1].X, layer.Points[i - 1].Y);
            var p2 = WorldToScreen(layer.Points[i].X, layer.Points[i].Y);
            
            MainCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
        
        // Draw points using layer's point size setting
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        
        foreach (var pt in layer.Points)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            
            var marker = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = brush,
            };
            
            Canvas.SetLeft(marker, screenPt.X - dotSize / 2);
            Canvas.SetTop(marker, screenPt.Y - dotSize / 2);
            MainCanvas.Children.Add(marker);
        }
    }
    
    private void DrawKpLabels()
    {
        // Get KP interval from UI
        if (!double.TryParse(TxtKpInterval?.Text, out double kpInterval) || kpInterval <= 0)
            kpInterval = 0.1; // Default 100m
        
        // Find points with KP values
        var pointsWithKp = _editablePoints.Where(p => p.Kp.HasValue).OrderBy(p => p.Kp!.Value).ToList();
        if (pointsWithKp.Count == 0) return;
        
        double minKp = pointsWithKp.FirstOrDefault()?.Kp!.Value ?? 0;
        double maxKp = pointsWithKp.LastOrDefault()?.Kp!.Value ?? 0;
        
        // Draw KP labels at intervals
        double startKp = Math.Ceiling(minKp / kpInterval) * kpInterval;
        
        for (double kp = startKp; kp <= maxKp; kp += kpInterval)
        {
            // Find the closest point to this KP
            var closestPoint = pointsWithKp
                .OrderBy(p => Math.Abs(p.Kp!.Value - kp))
                .FirstOrDefault();
            
            if (closestPoint == null) continue;
            
            // Only draw if close enough to actual KP value
            if (Math.Abs(closestPoint.Kp!.Value - kp) > kpInterval * 0.5) continue;
            
            var screenPt = WorldToScreen(closestPoint.X, closestPoint.Y);
            
            // Draw KP marker
            var marker = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.Yellow,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            
            Canvas.SetLeft(marker, screenPt.X - 4);
            Canvas.SetTop(marker, screenPt.Y - 4);
            MainCanvas.Children.Add(marker);
            
            // Draw KP label
            var label = new TextBlock
            {
                Text = $"KP {kp:F3}",
                Foreground = Brushes.Yellow,
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                FontSize = 10,
                Padding = new Thickness(3, 1, 3, 1)
            };
            
            Canvas.SetLeft(label, screenPt.X + 6);
            Canvas.SetTop(label, screenPt.Y - 8);
            MainCanvas.Children.Add(label);
        }
    }
    
    private void DrawGrid(EditorLayer layer)
    {
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Calculate appropriate grid spacing based on data size
        double gridSize = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(_dataWidth, _dataHeight) / 10)));
        
        // Vertical lines
        double startX = Math.Floor(_minX / gridSize) * gridSize;
        for (double x = startX; x <= _maxX; x += gridSize)
        {
            var p1 = WorldToScreen(x, _minY);
            var p2 = WorldToScreen(x, _maxY);
            
            MainCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
        
        // Horizontal lines
        double startY = Math.Floor(_minY / gridSize) * gridSize;
        for (double y = startY; y <= _maxY; y += gridSize)
        {
            var p1 = WorldToScreen(_minX, y);
            var p2 = WorldToScreen(_maxX, y);
            
            MainCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
    }
    
    private void DrawFieldLayout(EditorLayer layer)
    {
        if (_fieldLayout == null) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Draw lines
        foreach (var line in _fieldLayout.Lines)
        {
            var p1 = WorldToScreen(line.StartX, line.StartY);
            var p2 = WorldToScreen(line.EndX, line.EndY);
            
            MainCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
        
        // Draw polylines
        foreach (var poly in _fieldLayout.Polylines)
        {
            for (int i = 0; i < poly.Vertices.Count - 1; i++)
            {
                var p1 = WorldToScreen(poly.Vertices[i].X, poly.Vertices[i].Y);
                var p2 = WorldToScreen(poly.Vertices[i + 1].X, poly.Vertices[i + 1].Y);
                
                MainCanvas.Children.Add(new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = brush,
                    StrokeThickness = layer.Thickness
                });
            }
        }
    }
    
    private void DrawRoute(EditorLayer layer)
    {
        if (_routeData == null) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        foreach (var segment in _routeData.Segments)
        {
            if (segment.IsStraight)
            {
                var p1 = WorldToScreen(segment.StartEasting, segment.StartNorthing);
                var p2 = WorldToScreen(segment.EndEasting, segment.EndNorthing);
                
                MainCanvas.Children.Add(new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = brush,
                    StrokeThickness = layer.Thickness
                });
            }
            else
            {
                DrawRouteArc(segment, brush, layer.Thickness);
            }
        }
    }
    
    private void DrawRouteArc(RouteSegment segment, Brush brush, double thickness)
    {
        var (centerE, centerN) = segment.GetArcCenter();
        double radius = Math.Abs(segment.Radius);
        
        double startAngle = Math.Atan2(segment.StartNorthing - centerN, segment.StartEasting - centerE);
        double endAngle = Math.Atan2(segment.EndNorthing - centerN, segment.EndEasting - centerE);
        
        if (segment.IsClockwise && endAngle > startAngle)
            endAngle -= 2 * Math.PI;
        else if (!segment.IsClockwise && endAngle < startAngle)
            endAngle += 2 * Math.PI;
        
        int numSegments = 20;
        double angleStep = (endAngle - startAngle) / numSegments;
        
        for (int i = 0; i < numSegments; i++)
        {
            double angle1 = startAngle + i * angleStep;
            double angle2 = startAngle + (i + 1) * angleStep;
            
            double x1 = centerE + radius * Math.Cos(angle1);
            double y1 = centerN + radius * Math.Sin(angle1);
            double x2 = centerE + radius * Math.Cos(angle2);
            double y2 = centerN + radius * Math.Sin(angle2);
            
            var p1 = WorldToScreen(x1, y1);
            var p2 = WorldToScreen(x2, y2);
            
            MainCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = thickness
            });
        }
    }
    
    private void DrawSurveyRaw(EditorLayer layer)
    {
        if (ChkShowComparison?.IsChecked != true) return;
        if (_editablePoints.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        var pointBrush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity * 0.7 };
        
        // Calculate gap threshold based on median segment length
        double gapThreshold = CalculateGapThreshold(_editablePoints.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList());
        
        // Draw lines with gap detection - don't connect distant points
        for (int i = 0; i < _editablePoints.Count - 1; i++)
        {
            double dx = _editablePoints[i + 1].OriginalX - _editablePoints[i].OriginalX;
            double dy = _editablePoints[i + 1].OriginalY - _editablePoints[i].OriginalY;
            double segmentLength = Math.Sqrt(dx * dx + dy * dy);
            
            // Skip drawing line if there's a gap (segment too long)
            if (segmentLength > gapThreshold) continue;
            
            var p1 = WorldToScreen(_editablePoints[i].OriginalX, _editablePoints[i].OriginalY);
            var p2 = WorldToScreen(_editablePoints[i + 1].OriginalX, _editablePoints[i + 1].OriginalY);
            
            MainCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = layer.Thickness,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            });
        }
        
        // Draw points - zoom-responsive tiny dots
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        foreach (var pt in _editablePoints)
        {
            var screenPt = WorldToScreen(pt.OriginalX, pt.OriginalY);
            
            var ellipse = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = pointBrush
            };
            
            Canvas.SetLeft(ellipse, screenPt.X - dotSize / 2);
            Canvas.SetTop(ellipse, screenPt.Y - dotSize / 2);
            MainCanvas.Children.Add(ellipse);
        }
    }
    
    /// <summary>
    /// Calculate gap threshold based on median segment length
    /// </summary>
    private double CalculateGapThreshold(List<(double X, double Y, double Z)> points, double multiplier = 10.0)
    {
        if (points.Count < 2) return double.MaxValue;
        
        var segmentLengths = new List<double>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            double dx = points[i + 1].X - points[i].X;
            double dy = points[i + 1].Y - points[i].Y;
            segmentLengths.Add(Math.Sqrt(dx * dx + dy * dy));
        }
        
        if (segmentLengths.Count == 0) return double.MaxValue;
        
        segmentLengths.Sort();
        double median = segmentLengths[segmentLengths.Count / 2];
        
        return median * multiplier;
    }
    
    private void DrawSurveySmoothed(EditorLayer layer)
    {
        if (_editablePoints.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Calculate gap threshold to avoid connecting distant points
        double gapThreshold = CalculateGapThreshold(_editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList());
        
        // Draw lines with gap detection
        for (int i = 0; i < _editablePoints.Count - 1; i++)
        {
            double dx = _editablePoints[i + 1].X - _editablePoints[i].X;
            double dy = _editablePoints[i + 1].Y - _editablePoints[i].Y;
            double segmentLength = Math.Sqrt(dx * dx + dy * dy);
            
            // Skip drawing line if there's a gap (segment too long)
            if (segmentLength > gapThreshold) continue;
            
            var p1 = WorldToScreen(_editablePoints[i].X, _editablePoints[i].Y);
            var p2 = WorldToScreen(_editablePoints[i + 1].X, _editablePoints[i + 1].Y);
            
            MainCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
        
        // Draw points with zoom-responsive size
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        foreach (var pt in _editablePoints)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            
            var ellipse = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = brush,
                Tag = pt,
                Cursor = layer.IsLocked ? Cursors.Arrow : Cursors.Hand
            };
            
            if (!layer.IsLocked)
            {
                ellipse.MouseLeftButtonDown += Point_MouseDown;
            }
            
            Canvas.SetLeft(ellipse, screenPt.X - dotSize / 2);
            Canvas.SetTop(ellipse, screenPt.Y - dotSize / 2);
            MainCanvas.Children.Add(ellipse);
        }
    }
    
    private void DrawDigitizedPoints(EditorLayer layer)
    {
        if (_digitizedPoints.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Zoom-responsive sizes - same as normal points
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        double crossSize = dotSize;  // Same size as normal points
        
        foreach (var pt in _digitizedPoints)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            
            // Draw small cross marker for digitized points
            MainCanvas.Children.Add(new Line
            {
                X1 = screenPt.X - crossSize, Y1 = screenPt.Y,
                X2 = screenPt.X + crossSize, Y2 = screenPt.Y,
                Stroke = brush,
                StrokeThickness = 1
            });
            
            MainCanvas.Children.Add(new Line
            {
                X1 = screenPt.X, Y1 = screenPt.Y - crossSize,
                X2 = screenPt.X, Y2 = screenPt.Y + crossSize,
                Stroke = brush,
                StrokeThickness = 1
            });
            
            var ellipse = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = brush,
                Tag = pt
            };
            
            Canvas.SetLeft(ellipse, screenPt.X - dotSize / 2);
            Canvas.SetTop(ellipse, screenPt.Y - dotSize / 2);
            MainCanvas.Children.Add(ellipse);
        }
        
        TxtDigitizedCount.Text = $"Digitized Points: {_digitizedPoints.Count}";
    }
    
    private void DrawSelection()
    {
        if (_selectedPoints.Count == 0) return;
        
        // Find the layer this point belongs to and get its actual point size
        double layerPointSize = 4.0; // Default
        var activeLayer = Layers.FirstOrDefault(l => l.IsVisible && 
            (l.LayerType == EditorLayerType.SurveySmoothed || 
             l.LayerType == EditorLayerType.SurveyRaw ||
             l.LayerType == EditorLayerType.Digitized ||
             l.LayerType == EditorLayerType.PointsOnly));
        if (activeLayer != null)
        {
            layerPointSize = activeLayer.PointSize;
        }
        
        // Get the actual rendered point size - selection should match the point exactly
        double selectionSize = GetZoomResponsivePointSize(layerPointSize);
        
        foreach (var pt in _selectedPoints)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            
            // Draw solid cyan dot (no ring/circle outline)
            var marker = new Ellipse
            {
                Width = selectionSize,
                Height = selectionSize,
                Fill = Brushes.Cyan,
                Stroke = null  // No ring
            };
            
            Canvas.SetLeft(marker, screenPt.X - selectionSize / 2);
            Canvas.SetTop(marker, screenPt.Y - selectionSize / 2);
            MainCanvas.Children.Add(marker);
        }
    }
    
    private void UpdateMiniMap()
    {
        MiniMapCanvas.Children.Clear();
        
        if (_editablePoints.Count == 0 && _routeData == null) return;
        
        double mapWidth = MiniMapCanvas.ActualWidth;
        double mapHeight = MiniMapCanvas.ActualHeight;
        
        if (mapWidth <= 0 || mapHeight <= 0) return;
        
        double scaleX = mapWidth / _dataWidth;
        double scaleY = mapHeight / _dataHeight;
        double scale = Math.Min(scaleX, scaleY) * 0.9;
        
        double offsetX = (mapWidth - _dataWidth * scale) / 2;
        double offsetY = (mapHeight - _dataHeight * scale) / 2;
        
        Point MiniMapTransform(double x, double y)
        {
            return new Point(
                offsetX + (x - _minX) * scale,
                mapHeight - (offsetY + (y - _minY) * scale)
            );
        }
        
        // Draw route
        if (_routeData != null)
        {
            foreach (var seg in _routeData.Segments)
            {
                var p1 = MiniMapTransform(seg.StartEasting, seg.StartNorthing);
                var p2 = MiniMapTransform(seg.EndEasting, seg.EndNorthing);
                
                MiniMapCanvas.Children.Add(new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1
                });
            }
        }
        
        // Draw survey points as a simple path
        if (_editablePoints.Count > 1)
        {
            for (int i = 0; i < _editablePoints.Count - 1; i += Math.Max(1, _editablePoints.Count / 50))
            {
                var p1 = MiniMapTransform(_editablePoints[i].X, _editablePoints[i].Y);
                int nextIdx = Math.Min(i + Math.Max(1, _editablePoints.Count / 50), _editablePoints.Count - 1);
                var p2 = MiniMapTransform(_editablePoints[nextIdx].X, _editablePoints[nextIdx].Y);
                
                MiniMapCanvas.Children.Add(new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = Brushes.Cyan,
                    StrokeThickness = 1
                });
            }
        }
        
        // Draw view rectangle showing current visible area
        if (MainCanvas.ActualWidth > 0 && MainCanvas.ActualHeight > 0)
        {
            // Calculate visible world coordinates
            var topLeft = ScreenToWorld(new Point(0, 0));
            var bottomRight = ScreenToWorld(new Point(MainCanvas.ActualWidth, MainCanvas.ActualHeight));
            
            // Clamp to data bounds
            double visMinX = Math.Max(topLeft.X, _minX);
            double visMaxX = Math.Min(bottomRight.X, _maxX);
            double visMinY = Math.Max(bottomRight.Y, _minY);
            double visMaxY = Math.Min(topLeft.Y, _maxY);
            
            // Transform to minimap coordinates
            var rectTopLeft = MiniMapTransform(visMinX, visMaxY);
            var rectBottomRight = MiniMapTransform(visMaxX, visMinY);
            
            double rectWidth = Math.Abs(rectBottomRight.X - rectTopLeft.X);
            double rectHeight = Math.Abs(rectBottomRight.Y - rectTopLeft.Y);
            
            // Only draw if rectangle is meaningful
            if (rectWidth > 2 && rectHeight > 2 && rectWidth < mapWidth * 0.95 && rectHeight < mapHeight * 0.95)
            {
                var viewRect = new System.Windows.Shapes.Rectangle
                {
                    Width = rectWidth,
                    Height = rectHeight,
                    Stroke = Brushes.Yellow,
                    StrokeThickness = 1.5,
                    Fill = new SolidColorBrush(Color.FromArgb(30, 255, 255, 0))
                };
                
                Canvas.SetLeft(viewRect, Math.Min(rectTopLeft.X, rectBottomRight.X));
                Canvas.SetTop(viewRect, Math.Min(rectTopLeft.Y, rectBottomRight.Y));
                MiniMapCanvas.Children.Add(viewRect);
            }
        }
    }
    
    #region Optimized Drawing Methods (PathGeometry-based)
    
    /// <summary>
    /// Optimized grid drawing using PathGeometry
    /// </summary>
    private void DrawGridOptimized(EditorLayer layer)
    {
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        var geometry = new PathGeometry();
        
        // Calculate appropriate grid spacing based on zoom
        double gridSize = Math.Pow(10, Math.Floor(Math.Log10(Math.Max(_dataWidth, _dataHeight) / 10)));
        
        // Adjust grid spacing based on zoom level to avoid too many/few lines
        while (gridSize * _zoomLevel < 20) gridSize *= 2; // Increase if lines too close
        while (gridSize * _zoomLevel > 200) gridSize /= 2; // Decrease if lines too far
        
        // Use full data bounds for grid (simpler and more reliable)
        double startX = Math.Floor(_minX / gridSize) * gridSize;
        double endX = Math.Ceiling(_maxX / gridSize) * gridSize;
        double startY = Math.Floor(_minY / gridSize) * gridSize;
        double endY = Math.Ceiling(_maxY / gridSize) * gridSize;
        
        // Vertical lines
        for (double x = startX; x <= endX; x += gridSize)
        {
            var p1 = WorldToScreen(x, _minY);
            var p2 = WorldToScreen(x, _maxY);
            geometry.Figures.Add(new PathFigure
            {
                StartPoint = p1,
                IsClosed = false,
                Segments = { new LineSegment(p2, true) }
            });
        }
        
        // Horizontal lines
        for (double y = startY; y <= endY; y += gridSize)
        {
            var p1 = WorldToScreen(_minX, y);
            var p2 = WorldToScreen(_maxX, y);
            geometry.Figures.Add(new PathFigure
            {
                StartPoint = p1,
                IsClosed = false,
                Segments = { new LineSegment(p2, true) }
            });
        }
        
        if (geometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
    }
    
    /// <summary>
    /// Draw coordinate indicators: origin marker, N/E axes, and coordinate labels
    /// </summary>
    private void DrawCoordinateIndicators()
    {
        // Draw origin marker at data center
        double originX = (_minX + _maxX) / 2;
        double originY = (_minY + _maxY) / 2;
        var originScreen = WorldToScreen(originX, originY);
        
        // Origin crosshair
        double crossSize = 15;
        var originBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 };
        
        // Horizontal line of origin cross
        MainCanvas.Children.Add(new Line
        {
            X1 = originScreen.X - crossSize, Y1 = originScreen.Y,
            X2 = originScreen.X + crossSize, Y2 = originScreen.Y,
            Stroke = originBrush,
            StrokeThickness = 2
        });
        
        // Vertical line of origin cross
        MainCanvas.Children.Add(new Line
        {
            X1 = originScreen.X, Y1 = originScreen.Y - crossSize,
            X2 = originScreen.X, Y2 = originScreen.Y + crossSize,
            Stroke = originBrush,
            StrokeThickness = 2
        });
        
        // Origin circle
        var originCircle = new Ellipse
        {
            Width = 10,
            Height = 10,
            Stroke = originBrush,
            StrokeThickness = 2,
            Fill = new SolidColorBrush(Colors.Transparent)
        };
        Canvas.SetLeft(originCircle, originScreen.X - 5);
        Canvas.SetTop(originCircle, originScreen.Y - 5);
        MainCanvas.Children.Add(originCircle);
        
        // Draw compass rose in bottom-left corner
        DrawCompassRose();
        
        // Draw coordinate labels at corners
        DrawCoordinateLabels();
    }
    
    /// <summary>
    /// Draw compass rose showing N/E directions in corner
    /// </summary>
    private void DrawCompassRose()
    {
        double margin = 60;
        double centerX = margin;
        double centerY = MainCanvas.ActualHeight - margin;
        double arrowLength = 35;
        double arrowHeadSize = 8;
        
        var axisBrush = new SolidColorBrush(Colors.White) { Opacity = 0.9 };
        var northBrush = new SolidColorBrush(Colors.Red) { Opacity = 0.9 };
        var eastBrush = new SolidColorBrush(Colors.LimeGreen) { Opacity = 0.9 };
        
        // Background circle
        var bgCircle = new Ellipse
        {
            Width = 90,
            Height = 90,
            Fill = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
            Stroke = new SolidColorBrush(Colors.Gray),
            StrokeThickness = 1
        };
        Canvas.SetLeft(bgCircle, centerX - 45);
        Canvas.SetTop(bgCircle, centerY - 45);
        MainCanvas.Children.Add(bgCircle);
        
        // North arrow (Y+ direction, pointing up)
        MainCanvas.Children.Add(new Line
        {
            X1 = centerX, Y1 = centerY,
            X2 = centerX, Y2 = centerY - arrowLength,
            Stroke = northBrush,
            StrokeThickness = 2.5
        });
        
        // North arrowhead
        var northHead = new Polygon
        {
            Points = new PointCollection
            {
                new Point(centerX, centerY - arrowLength - arrowHeadSize),
                new Point(centerX - arrowHeadSize/2, centerY - arrowLength),
                new Point(centerX + arrowHeadSize/2, centerY - arrowLength)
            },
            Fill = northBrush
        };
        MainCanvas.Children.Add(northHead);
        
        // East arrow (X+ direction, pointing right)
        MainCanvas.Children.Add(new Line
        {
            X1 = centerX, Y1 = centerY,
            X2 = centerX + arrowLength, Y2 = centerY,
            Stroke = eastBrush,
            StrokeThickness = 2.5
        });
        
        // East arrowhead
        var eastHead = new Polygon
        {
            Points = new PointCollection
            {
                new Point(centerX + arrowLength + arrowHeadSize, centerY),
                new Point(centerX + arrowLength, centerY - arrowHeadSize/2),
                new Point(centerX + arrowLength, centerY + arrowHeadSize/2)
            },
            Fill = eastBrush
        };
        MainCanvas.Children.Add(eastHead);
        
        // N label
        var nLabel = new TextBlock
        {
            Text = "N",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = northBrush
        };
        Canvas.SetLeft(nLabel, centerX - 5);
        Canvas.SetTop(nLabel, centerY - arrowLength - arrowHeadSize - 18);
        MainCanvas.Children.Add(nLabel);
        
        // E label
        var eLabel = new TextBlock
        {
            Text = "E",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = eastBrush
        };
        Canvas.SetLeft(eLabel, centerX + arrowLength + arrowHeadSize + 5);
        Canvas.SetTop(eLabel, centerY - 8);
        MainCanvas.Children.Add(eLabel);
    }
    
    /// <summary>
    /// Draw coordinate labels at data corners showing Easting/Northing values
    /// </summary>
    private void DrawCoordinateLabels()
    {
        var labelBrush = new SolidColorBrush(Colors.White) { Opacity = 0.8 };
        var bgBrush = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30));
        
        // Format coordinate values
        string FormatCoord(double val) => val.ToString("N1");
        
        // Top-right corner label (max E, max N)
        var topRight = WorldToScreen(_maxX, _maxY);
        if (topRight.X > 100 && topRight.Y > 20)
        {
            var trBorder = new Border
            {
                Background = bgBrush,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 2, 4, 2),
                Child = new TextBlock
                {
                    Text = $"E:{FormatCoord(_maxX)}\nN:{FormatCoord(_maxY)}",
                    FontSize = 10,
                    Foreground = labelBrush
                }
            };
            Canvas.SetLeft(trBorder, Math.Min(topRight.X - 80, MainCanvas.ActualWidth - 90));
            Canvas.SetTop(trBorder, Math.Max(topRight.Y, 5));
            MainCanvas.Children.Add(trBorder);
        }
        
        // Bottom-left corner label (min E, min N)
        var bottomLeft = WorldToScreen(_minX, _minY);
        if (bottomLeft.X < MainCanvas.ActualWidth - 100 && bottomLeft.Y < MainCanvas.ActualHeight - 40)
        {
            var blBorder = new Border
            {
                Background = bgBrush,
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 2, 4, 2),
                Child = new TextBlock
                {
                    Text = $"E:{FormatCoord(_minX)}\nN:{FormatCoord(_minY)}",
                    FontSize = 10,
                    Foreground = labelBrush
                }
            };
            Canvas.SetLeft(blBorder, Math.Max(bottomLeft.X + 5, 100));
            Canvas.SetTop(blBorder, Math.Min(bottomLeft.Y - 35, MainCanvas.ActualHeight - 45));
            MainCanvas.Children.Add(blBorder);
        }
    }
    
    /// <summary>
    /// Optimized field layout drawing
    /// </summary>
    private void DrawFieldLayoutOptimized(EditorLayer layer)
    {
        if (_fieldLayout == null) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        var geometry = new PathGeometry();
        
        // Draw lines (only visible ones)
        foreach (var line in _fieldLayout.Lines)
        {
            if (!IsLineVisible(line.StartX, line.StartY, line.EndX, line.EndY))
                continue;
                
            var p1 = WorldToScreen(line.StartX, line.StartY);
            var p2 = WorldToScreen(line.EndX, line.EndY);
            geometry.Figures.Add(new PathFigure
            {
                StartPoint = p1,
                IsClosed = false,
                Segments = { new LineSegment(p2, true) }
            });
        }
        
        // Draw polylines
        foreach (var poly in _fieldLayout.Polylines)
        {
            for (int i = 0; i < poly.Vertices.Count - 1; i++)
            {
                var v1 = poly.Vertices[i];
                var v2 = poly.Vertices[i + 1];
                
                if (!IsLineVisible(v1.X, v1.Y, v2.X, v2.Y))
                    continue;
                    
                var p1 = WorldToScreen(v1.X, v1.Y);
                var p2 = WorldToScreen(v2.X, v2.Y);
                geometry.Figures.Add(new PathFigure
                {
                    StartPoint = p1,
                    IsClosed = false,
                    Segments = { new LineSegment(p2, true) }
                });
            }
        }
        
        if (geometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
    }
    
    /// <summary>
    /// Optimized route drawing
    /// </summary>
    private void DrawRouteOptimized(EditorLayer layer)
    {
        if (_routeData == null) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        var geometry = new PathGeometry();
        
        foreach (var segment in _routeData.Segments)
        {
            if (segment.IsStraight)
            {
                if (!IsLineVisible(segment.StartEasting, segment.StartNorthing, segment.EndEasting, segment.EndNorthing))
                    continue;
                    
                var p1 = WorldToScreen(segment.StartEasting, segment.StartNorthing);
                var p2 = WorldToScreen(segment.EndEasting, segment.EndNorthing);
                geometry.Figures.Add(new PathFigure
                {
                    StartPoint = p1,
                    IsClosed = false,
                    Segments = { new LineSegment(p2, true) }
                });
            }
            else
            {
                DrawRouteArcOptimized(segment, brush, layer.Thickness, geometry);
            }
        }
        
        if (geometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
    }
    
    private void DrawRouteArcOptimized(RouteSegment segment, Brush brush, double thickness, PathGeometry geometry)
    {
        var (centerE, centerN) = segment.GetArcCenter();
        double radius = Math.Abs(segment.Radius);
        
        // Check if arc is visible
        if (!IsPointVisible(centerE, centerN) && 
            !IsPointVisible(segment.StartEasting, segment.StartNorthing) &&
            !IsPointVisible(segment.EndEasting, segment.EndNorthing))
            return;
        
        double startAngle = Math.Atan2(segment.StartNorthing - centerN, segment.StartEasting - centerE);
        double endAngle = Math.Atan2(segment.EndNorthing - centerN, segment.EndEasting - centerE);
        
        if (segment.IsClockwise && endAngle > startAngle)
            endAngle -= 2 * Math.PI;
        else if (!segment.IsClockwise && endAngle < startAngle)
            endAngle += 2 * Math.PI;
        
        int numSegments = 20;
        double angleStep = (endAngle - startAngle) / numSegments;
        
        var figure = new PathFigure();
        bool isFirst = true;
        
        for (int i = 0; i <= numSegments; i++)
        {
            double angle = startAngle + i * angleStep;
            double x = centerE + radius * Math.Cos(angle);
            double y = centerN + radius * Math.Sin(angle);
            var screenPt = WorldToScreen(x, y);
            
            if (isFirst)
            {
                figure.StartPoint = screenPt;
                isFirst = false;
            }
            else
            {
                figure.Segments.Add(new LineSegment(screenPt, true));
            }
        }
        
        geometry.Figures.Add(figure);
    }
    
    /// <summary>
    /// Optimized survey raw drawing with PathGeometry and viewport culling
    /// </summary>
    private void DrawSurveyRawOptimized(EditorLayer layer, RenderData renderData)
    {
        if (ChkShowComparison?.IsChecked != true) return;
        if (renderData.VisiblePointsOriginal.Count == 0) return;
        
        var lineBrush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity * 0.6 };
        var pointBrush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        var points = renderData.VisiblePointsOriginal.OrderBy(p => p.OriginalIndex).ToList();
        
        // LOD for large datasets
        int maxPointsForRaw = 10000;
        var pointsToDraw = points;
        int stride = 1;
        if (points.Count > maxPointsForRaw)
        {
            stride = points.Count / maxPointsForRaw + 1;
            pointsToDraw = points.Where((p, i) => i % stride == 0).ToList();
        }
        
        // Calculate gap threshold
        double gapThreshold = CalculateGapThreshold(
            pointsToDraw.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList());
        
        // === LAYER 1: Draw POLYLINES (track lines) ===
        var lineGeometry = new PathGeometry();
        PathFigure? currentFigure = null;
        
        for (int i = 0; i < pointsToDraw.Count; i++)
        {
            var p = pointsToDraw[i];
            var screenPt = WorldToScreen(p.OriginalX, p.OriginalY);
            
            if (i == 0)
            {
                currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
            }
            else
            {
                var prevP = pointsToDraw[i - 1];
                double dx = p.OriginalX - prevP.OriginalX;
                double dy = p.OriginalY - prevP.OriginalY;
                double segmentLength = Math.Sqrt(dx * dx + dy * dy);
                
                if (segmentLength > gapThreshold * stride)
                {
                    if (currentFigure != null && currentFigure.Segments.Count > 0)
                    {
                        lineGeometry.Figures.Add(currentFigure);
                    }
                    currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
                }
                else
                {
                    currentFigure?.Segments.Add(new LineSegment(screenPt, true));
                }
            }
        }
        
        if (currentFigure != null && currentFigure.Segments.Count > 0)
        {
            lineGeometry.Figures.Add(currentFigure);
        }
        
        // Draw lines as dashed (to differentiate from smoothed track)
        if (lineGeometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = lineGeometry,
                Stroke = lineBrush,
                StrokeThickness = Math.Max(0.5, layer.Thickness * 0.7),
                StrokeDashArray = new DoubleCollection { 6, 3 },  // Longer dashes
                IsHitTestVisible = false  // Lines don't capture clicks
            });
        }
        
        // === LAYER 2: Draw POINTS (separate entity, solid dots) ===
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        if (dotSize >= 0.5 && pointsToDraw.Count < 15000)
        {
            var pointGeometry = new GeometryGroup();
            foreach (var pt in pointsToDraw)
            {
                var screenPt = WorldToScreen(pt.OriginalX, pt.OriginalY);
                pointGeometry.Children.Add(new EllipseGeometry(screenPt, dotSize / 2, dotSize / 2));
            }
            
            if (pointGeometry.Children.Count > 0)
            {
                // Solid points on top of lines (AutoCAD style)
                MainCanvas.Children.Add(new Path
                {
                    Data = pointGeometry,
                    Fill = pointBrush,
                    Stroke = null,  // No outline on points
                    IsHitTestVisible = false
                });
            }
        }
    }
    
    /// <summary>
    /// Optimized survey smoothed drawing
    /// </summary>
    private void DrawSurveySmoothedOptimized(EditorLayer layer, RenderData renderData)
    {
        if (renderData.VisiblePoints.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Calculate gap threshold
        double gapThreshold = CalculateGapThreshold(
            renderData.VisiblePoints.Select(p => (p.X, p.Y, p.Z)).ToList());
        
        var points = renderData.VisiblePoints.OrderBy(p => p.OriginalIndex).ToList();
        
        // LOD for large datasets - limit rendering for performance
        int maxPointsForLines = 15000;
        int maxPointsForInteractiveHits = 5000;  // Limit interactive hit areas
        
        var pointsForLines = points;
        int lineStride = 1;
        if (points.Count > maxPointsForLines)
        {
            lineStride = points.Count / maxPointsForLines + 1;
            pointsForLines = points.Where((p, i) => i % lineStride == 0).ToList();
        }
        
        // Draw lines using PathGeometry with gap detection
        var lineGeometry = new PathGeometry();
        PathFigure? currentFigure = null;
        
        for (int i = 0; i < pointsForLines.Count; i++)
        {
            var p = pointsForLines[i];
            var screenPt = WorldToScreen(p.X, p.Y);
            
            if (i == 0)
            {
                currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
            }
            else
            {
                var prevP = pointsForLines[i - 1];
                double dx = p.X - prevP.X;
                double dy = p.Y - prevP.Y;
                double segmentLength = Math.Sqrt(dx * dx + dy * dy);
                
                // Account for stride in gap detection
                if (segmentLength > gapThreshold * lineStride)
                {
                    if (currentFigure != null && currentFigure.Segments.Count > 0)
                    {
                        lineGeometry.Figures.Add(currentFigure);
                    }
                    currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
                }
                else
                {
                    currentFigure?.Segments.Add(new LineSegment(screenPt, true));
                }
            }
        }
        
        if (currentFigure != null && currentFigure.Segments.Count > 0)
        {
            lineGeometry.Figures.Add(currentFigure);
        }
        
        if (lineGeometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = lineGeometry,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
        
        // Draw interactive hit areas (limit for performance)
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        double hitAreaSize = GetZoomResponsiveHitAreaSize();
        
        if (!layer.IsLocked)
        {
            // Only create interactive hit areas for a limited number of points
            var pointsForInteraction = points;
            if (points.Count > maxPointsForInteractiveHits)
            {
                int hitStride = points.Count / maxPointsForInteractiveHits + 1;
                pointsForInteraction = points.Where((p, i) => i % hitStride == 0).ToList();
            }
            
            foreach (var pt in pointsForInteraction)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                
                var ellipse = new Ellipse
                {
                    Width = hitAreaSize,
                    Height = hitAreaSize,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Tag = pt,
                    Cursor = Cursors.Hand
                };
                
                ellipse.MouseLeftButtonDown += Point_MouseDown;
                Canvas.SetLeft(ellipse, screenPt.X - hitAreaSize / 2);
                Canvas.SetTop(ellipse, screenPt.Y - hitAreaSize / 2);
                MainCanvas.Children.Add(ellipse);
            }
        }
        
        // Draw visible point dots (use LOD)
        if (dotSize >= 0.5 && pointsForLines.Count < 20000)
        {
            var pointGeometry = new GeometryGroup();
            foreach (var pt in pointsForLines)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                pointGeometry.Children.Add(new EllipseGeometry(screenPt, dotSize / 2, dotSize / 2));
            }
            
            if (pointGeometry.Children.Count > 0)
            {
                MainCanvas.Children.Add(new Path
                {
                    Data = pointGeometry,
                    Fill = brush,
                    IsHitTestVisible = false
                });
            }
        }
    }
    
    /// <summary>
    /// Draw points only layer (AutoCAD style - no lines)
    /// </summary>
    private void DrawPointsOnlyLayer(EditorLayer layer, RenderData renderData)
    {
        // Check if this layer has its own points collection (user-created layers)
        if (layer.Points != null && layer.Points.Count > 0)
        {
            DrawPointsOnlyLayerCustom(layer);
            return;
        }
        
        // Determine which point set to use based on parent layer name
        List<EditablePoint> points;
        bool isSmoothed = layer.ParentLayerName?.Contains("Smoothed") == true;
        
        if (isSmoothed)
        {
            points = renderData.VisiblePoints.OrderBy(p => p.OriginalIndex).ToList();
        }
        else
        {
            // Raw layer - use original coordinates
            points = renderData.VisiblePointsOriginal.OrderBy(p => p.OriginalIndex).ToList();
        }
        
        if (points.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        double hitAreaSize = GetZoomResponsiveHitAreaSize();
        
        // LOD for large datasets
        var pointsToDraw = points;
        if (points.Count > 15000)
        {
            int stride = points.Count / 15000 + 1;
            pointsToDraw = points.Where((p, i) => i % stride == 0).ToList();
        }
        
        // Create interactive hit areas for selection (if layer is selectable)
        if (layer.IsSelectable && !layer.IsLocked)
        {
            var interactionPoints = pointsToDraw;
            if (pointsToDraw.Count > 5000)
            {
                int hitStride = pointsToDraw.Count / 5000 + 1;
                interactionPoints = pointsToDraw.Where((p, i) => i % hitStride == 0).ToList();
            }
            
            foreach (var pt in interactionPoints)
            {
                double x = isSmoothed ? pt.X : pt.OriginalX;
                double y = isSmoothed ? pt.Y : pt.OriginalY;
                var screenPt = WorldToScreen(x, y);
                
                var ellipse = new Ellipse
                {
                    Width = hitAreaSize,
                    Height = hitAreaSize,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Tag = pt,
                    Cursor = Cursors.Hand
                };
                
                ellipse.MouseLeftButtonDown += Point_MouseDown;
                Canvas.SetLeft(ellipse, screenPt.X - hitAreaSize / 2);
                Canvas.SetTop(ellipse, screenPt.Y - hitAreaSize / 2);
                MainCanvas.Children.Add(ellipse);
            }
        }
        
        // Draw visible point dots (solid fill, no circles)
        if (dotSize >= 0.5)
        {
            var pointGeometry = new GeometryGroup();
            foreach (var pt in pointsToDraw)
            {
                double x = isSmoothed ? pt.X : pt.OriginalX;
                double y = isSmoothed ? pt.Y : pt.OriginalY;
                var screenPt = WorldToScreen(x, y);
                pointGeometry.Children.Add(new EllipseGeometry(screenPt, dotSize / 2, dotSize / 2));
            }
            
            if (pointGeometry.Children.Count > 0)
            {
                MainCanvas.Children.Add(new Path
                {
                    Data = pointGeometry,
                    Fill = brush,
                    IsHitTestVisible = false  // Let transparent ellipses handle clicks
                });
            }
        }
    }
    
    /// <summary>
    /// Draw points-only layer for layers with their own Points collection (interval points, etc.)
    /// </summary>
    private void DrawPointsOnlyLayerCustom(EditorLayer layer)
    {
        if (layer.Points == null || layer.Points.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        double hitAreaSize = GetZoomResponsiveHitAreaSize();
        
        // Filter to visible points
        var visiblePoints = layer.Points.Where(p => IsPointVisible(p.X, p.Y)).ToList();
        if (visiblePoints.Count == 0) return;
        
        // LOD for large datasets
        var pointsToDraw = visiblePoints;
        if (visiblePoints.Count > 15000)
        {
            int stride = visiblePoints.Count / 15000 + 1;
            pointsToDraw = visiblePoints.Where((p, i) => i % stride == 0).ToList();
        }
        
        // Create interactive hit areas for selection (if layer is selectable)
        if (layer.IsSelectable && !layer.IsLocked)
        {
            var interactionPoints = pointsToDraw;
            if (pointsToDraw.Count > 5000)
            {
                int hitStride = pointsToDraw.Count / 5000 + 1;
                interactionPoints = pointsToDraw.Where((p, i) => i % hitStride == 0).ToList();
            }
            
            foreach (var pt in interactionPoints)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                
                var ellipse = new Ellipse
                {
                    Width = hitAreaSize,
                    Height = hitAreaSize,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Tag = pt,
                    Cursor = Cursors.Hand
                };
                
                ellipse.MouseLeftButtonDown += Point_MouseDown;
                Canvas.SetLeft(ellipse, screenPt.X - hitAreaSize / 2);
                Canvas.SetTop(ellipse, screenPt.Y - hitAreaSize / 2);
                MainCanvas.Children.Add(ellipse);
            }
        }
        
        // Draw visible point dots (solid fill, no circles)
        if (dotSize >= 0.5)
        {
            var pointGeometry = new GeometryGroup();
            foreach (var pt in pointsToDraw)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                pointGeometry.Children.Add(new EllipseGeometry(screenPt, dotSize / 2, dotSize / 2));
            }
            
            if (pointGeometry.Children.Count > 0)
            {
                MainCanvas.Children.Add(new Path
                {
                    Data = pointGeometry,
                    Fill = brush,
                    IsHitTestVisible = false
                });
            }
        }
    }
    
    /// <summary>
    /// Draw lines only layer (AutoCAD style - polyline, no points)
    /// </summary>
    private void DrawLinesOnlyLayer(EditorLayer layer, RenderData renderData)
    {
        // Check if this layer has its own points collection (user-created layers like spline)
        if (layer.Points != null && layer.Points.Count > 0)
        {
            DrawLinesOnlyLayerCustom(layer);
            return;
        }
        
        // Determine which point set to use based on parent layer name
        List<EditablePoint> points;
        bool isSmoothed = layer.ParentLayerName?.Contains("Smoothed") == true;
        
        if (isSmoothed)
        {
            points = renderData.VisiblePoints.OrderBy(p => p.OriginalIndex).ToList();
        }
        else
        {
            // Raw layer - use original coordinates
            points = renderData.VisiblePointsOriginal.OrderBy(p => p.OriginalIndex).ToList();
        }
        
        if (points.Count < 2) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Calculate gap threshold
        List<(double X, double Y, double Z)> coordPoints;
        if (isSmoothed)
        {
            coordPoints = points.Select(p => (p.X, p.Y, p.Z)).ToList();
        }
        else
        {
            coordPoints = points.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList();
        }
        double gapThreshold = CalculateGapThreshold(coordPoints);
        
        // LOD for large datasets
        var pointsToDraw = points;
        int stride = 1;
        if (points.Count > 15000)
        {
            stride = points.Count / 15000 + 1;
            pointsToDraw = points.Where((p, i) => i % stride == 0).ToList();
        }
        
        // Draw lines using PathGeometry with gap detection (never close the path)
        var lineGeometry = new PathGeometry();
        PathFigure? currentFigure = null;
        
        for (int i = 0; i < pointsToDraw.Count; i++)
        {
            var pt = pointsToDraw[i];
            double x = isSmoothed ? pt.X : pt.OriginalX;
            double y = isSmoothed ? pt.Y : pt.OriginalY;
            var screenPt = WorldToScreen(x, y);
            
            if (i == 0)
            {
                currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
            }
            else
            {
                var prevPt = pointsToDraw[i - 1];
                double prevX = isSmoothed ? prevPt.X : prevPt.OriginalX;
                double prevY = isSmoothed ? prevPt.Y : prevPt.OriginalY;
                
                double dx = x - prevX;
                double dy = y - prevY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                // Gap detection - accounting for stride
                if (distance > gapThreshold * stride)
                {
                    if (currentFigure != null && currentFigure.Segments.Count > 0)
                    {
                        lineGeometry.Figures.Add(currentFigure);
                    }
                    currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
                }
                else
                {
                    currentFigure?.Segments.Add(new LineSegment(screenPt, true));
                }
            }
        }
        
        // Add final figure (never closed)
        if (currentFigure != null && currentFigure.Segments.Count > 0)
        {
            lineGeometry.Figures.Add(currentFigure);
        }
        
        if (lineGeometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = lineGeometry,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
    }
    
    /// <summary>
    /// Draw lines-only layer for layers with their own Points collection (spline, etc.)
    /// </summary>
    private void DrawLinesOnlyLayerCustom(EditorLayer layer)
    {
        if (layer.Points == null || layer.Points.Count < 2) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Filter visible points
        var visiblePoints = layer.Points.Where(p => IsPointVisible(p.X, p.Y)).ToList();
        if (visiblePoints.Count < 2) return;
        
        // Calculate gap threshold
        double gapThreshold = CalculateGapThreshold(visiblePoints.Select(p => (p.X, p.Y, p.Z)).ToList());
        
        // LOD for large datasets
        var pointsToDraw = visiblePoints;
        int stride = 1;
        if (visiblePoints.Count > 15000)
        {
            stride = visiblePoints.Count / 15000 + 1;
            pointsToDraw = visiblePoints.Where((p, i) => i % stride == 0).ToList();
        }
        
        // Draw lines using PathGeometry with gap detection (never close the path)
        var lineGeometry = new PathGeometry();
        PathFigure? currentFigure = null;
        
        for (int i = 0; i < pointsToDraw.Count; i++)
        {
            var pt = pointsToDraw[i];
            var screenPt = WorldToScreen(pt.X, pt.Y);
            
            if (i == 0)
            {
                currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
            }
            else
            {
                var prevPt = pointsToDraw[i - 1];
                double dx = pt.X - prevPt.X;
                double dy = pt.Y - prevPt.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                // Gap detection - accounting for stride
                if (distance > gapThreshold * stride)
                {
                    if (currentFigure != null && currentFigure.Segments.Count > 0)
                    {
                        lineGeometry.Figures.Add(currentFigure);
                    }
                    currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
                }
                else
                {
                    currentFigure?.Segments.Add(new LineSegment(screenPt, true));
                }
            }
        }
        
        // Add final figure (never closed)
        if (currentFigure != null && currentFigure.Segments.Count > 0)
        {
            lineGeometry.Figures.Add(currentFigure);
        }
        
        if (lineGeometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = lineGeometry,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
    }
    
    /// <summary>
    /// Optimized custom layer drawing
    /// </summary>
    private void DrawCustomLayerOptimized(EditorLayer layer)
    {
        if (layer.Points == null || layer.Points.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        
        // Filter visible points
        var visiblePoints = layer.Points.Where(p => IsPointVisible(p.X, p.Y)).ToList();
        if (visiblePoints.Count == 0) return;
        
        // Calculate gap threshold (10x average spacing)
        double gapThreshold = CalculateGapThreshold(visiblePoints.Select(p => (p.X, p.Y, p.Z)).ToList());
        
        // Performance optimization for large datasets - aggressive LOD
        var pointsToDraw = visiblePoints;
        int stride = 1;
        int maxPointsForLines = 10000; // Max points for line rendering
        int maxPointsForDots = 15000;  // Max points for dot rendering
        
        if (visiblePoints.Count > maxPointsForLines)
        {
            stride = visiblePoints.Count / maxPointsForLines + 1;
            pointsToDraw = visiblePoints.Where((p, i) => i % stride == 0).ToList();
        }
        
        // Draw lines using PathGeometry with gap detection
        var lineGeometry = new PathGeometry();
        PathFigure? currentFigure = null;
        
        for (int i = 0; i < pointsToDraw.Count; i++)
        {
            var p = pointsToDraw[i];
            var screenPt = WorldToScreen(p.X, p.Y);
            
            if (i == 0)
            {
                // Start first figure
                currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
            }
            else
            {
                var prevP = pointsToDraw[i - 1];
                double dx = p.X - prevP.X;
                double dy = p.Y - prevP.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                // Gap detection - if distance is too large (accounting for stride), start new segment
                // UNLESS layer is explicitly closed (user-created polylines)
                if (!layer.IsClosed && distance > gapThreshold * stride)
                {
                    // Close current figure and start new one
                    if (currentFigure != null && currentFigure.Segments.Count > 0)
                    {
                        lineGeometry.Figures.Add(currentFigure);
                    }
                    currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
                }
                else
                {
                    // Add line segment to current figure
                    currentFigure?.Segments.Add(new LineSegment(screenPt, true));
                }
            }
        }
        
        // Add final figure and handle closed polylines
        if (currentFigure != null && currentFigure.Segments.Count > 0)
        {
            // If layer is closed, close the figure (connect last to first)
            if (layer.IsClosed && pointsToDraw.Count > 2)
            {
                currentFigure.IsClosed = true;
            }
            lineGeometry.Figures.Add(currentFigure);
        }
        
        if (lineGeometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = lineGeometry,
                Stroke = brush,
                StrokeThickness = layer.Thickness
            });
        }
        
        // Draw points only if zoomed in enough and not too many points
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        if (dotSize >= 0.5 && pointsToDraw.Count < maxPointsForDots)
        {
            var pointGeometry = new GeometryGroup();
            foreach (var pt in pointsToDraw)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                pointGeometry.Children.Add(new EllipseGeometry(screenPt, dotSize / 2, dotSize / 2));
            }
            
            if (pointGeometry.Children.Count > 0)
            {
                MainCanvas.Children.Add(new Path
                {
                    Data = pointGeometry,
                    Fill = brush,
                    IsHitTestVisible = false  // Points don't capture clicks directly
                });
            }
        }
        
        // Add interactive hit areas for selection (if layer is not locked)
        if (!layer.IsLocked && layer.Points != null)
        {
            double hitAreaSize = GetZoomResponsiveHitAreaSize();
            int maxInteractivePoints = 3000;
            
            var interactivePoints = pointsToDraw;
            if (pointsToDraw.Count > maxInteractivePoints)
            {
                int hitStride = pointsToDraw.Count / maxInteractivePoints + 1;
                interactivePoints = pointsToDraw.Where((p, i) => i % hitStride == 0).ToList();
            }
            
            foreach (var pt in interactivePoints)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                
                var ellipse = new Ellipse
                {
                    Width = hitAreaSize,
                    Height = hitAreaSize,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Tag = pt,  // Store the EditablePoint for selection
                    Cursor = Cursors.Hand
                };
                
                // Use same Point_MouseDown handler - works for any EditablePoint
                ellipse.MouseLeftButtonDown += Point_MouseDown;
                Canvas.SetLeft(ellipse, screenPt.X - hitAreaSize / 2);
                Canvas.SetTop(ellipse, screenPt.Y - hitAreaSize / 2);
                MainCanvas.Children.Add(ellipse);
            }
        }
    }
    
    /// <summary>
    /// Optimized digitized points drawing
    /// </summary>
    private void DrawDigitizedPointsOptimized(EditorLayer layer)
    {
        if (_digitizedPoints.Count == 0) return;
        
        var visiblePoints = _digitizedPoints.Where(p => IsPointVisible(p.X, p.Y)).ToList();
        if (visiblePoints.Count == 0) return;
        
        var brush = new SolidColorBrush(layer.Color) { Opacity = layer.Opacity };
        double dotSize = GetZoomResponsivePointSize(layer.PointSize);
        double crossSize = dotSize;  // Same size as normal points (was * 2)
        
        // Use PathGeometry for crosses
        var crossGeometry = new PathGeometry();
        var pointGeometry = new GeometryGroup();
        
        foreach (var pt in visiblePoints)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            
            // Cross lines (subtle indicator that it's a digitized point)
            crossGeometry.Figures.Add(new PathFigure
            {
                StartPoint = new Point(screenPt.X - crossSize, screenPt.Y),
                IsClosed = false,
                Segments = { new LineSegment(new Point(screenPt.X + crossSize, screenPt.Y), true) }
            });
            crossGeometry.Figures.Add(new PathFigure
            {
                StartPoint = new Point(screenPt.X, screenPt.Y - crossSize),
                IsClosed = false,
                Segments = { new LineSegment(new Point(screenPt.X, screenPt.Y + crossSize), true) }
            });
            
            // Point - same size as regular points
            pointGeometry.Children.Add(new EllipseGeometry(screenPt, dotSize / 2, dotSize / 2));
        }
        
        if (crossGeometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = crossGeometry,
                Stroke = brush,
                StrokeThickness = 1
            });
        }
        
        if (pointGeometry.Children.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = pointGeometry,
                Fill = brush,
                IsHitTestVisible = false
            });
        }
        
        // Add interactive hit areas for selection (if layer not locked)
        if (!layer.IsLocked)
        {
            double hitAreaSize = GetZoomResponsiveHitAreaSize();
            
            foreach (var pt in visiblePoints)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                
                var ellipse = new Ellipse
                {
                    Width = hitAreaSize,
                    Height = hitAreaSize,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Tag = pt,
                    Cursor = Cursors.Hand
                };
                
                ellipse.MouseLeftButtonDown += Point_MouseDown;
                Canvas.SetLeft(ellipse, screenPt.X - hitAreaSize / 2);
                Canvas.SetTop(ellipse, screenPt.Y - hitAreaSize / 2);
                MainCanvas.Children.Add(ellipse);
            }
        }
        
        TxtDigitizedCount.Text = $"Digitized Points: {_digitizedPoints.Count}";
    }
    
    /// <summary>
    /// Optimized selection drawing
    /// </summary>
    private void DrawSelectionOptimized()
    {
        if (_selectedPoints.Count == 0) return;
        
        var visibleSelected = _selectedPoints.Where(p => IsPointVisible(p.X, p.Y)).ToList();
        if (visibleSelected.Count == 0) return;
        
        // Find the layer this point belongs to and get its actual point size
        double layerPointSize = 4.0; // Default
        var activeLayer = Layers.FirstOrDefault(l => l.IsVisible && 
            (l.LayerType == EditorLayerType.SurveySmoothed || 
             l.LayerType == EditorLayerType.SurveyRaw ||
             l.LayerType == EditorLayerType.Digitized ||
             l.LayerType == EditorLayerType.PointsOnly));
        if (activeLayer != null)
        {
            layerPointSize = activeLayer.PointSize;
        }
        
        // Get the actual rendered point size - selection should match the point exactly
        double pointSize = GetZoomResponsivePointSize(layerPointSize);
        
        // Draw selected points as SOLID CYAN dots (same size as regular points, no ring)
        var pointGeometry = new GeometryGroup();
        
        foreach (var pt in visibleSelected)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            pointGeometry.Children.Add(new EllipseGeometry(screenPt, pointSize / 2, pointSize / 2));
        }
        
        if (pointGeometry.Children.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = pointGeometry,
                Fill = Brushes.Cyan,  // Solid cyan fill - no ring
                Stroke = null,        // No outline/ring
                IsHitTestVisible = false
            });
        }
    }
    
    /// <summary>
    /// Lightweight drag preview - only updates selected points during drag
    /// Much faster than full redraw
    /// </summary>
    private void UpdateDragPreview()
    {
        // Remove old preview
        if (_dragPreviewPath != null)
        {
            MainCanvas.Children.Remove(_dragPreviewPath);
            _dragPreviewPath = null;
        }
        
        if (_selectedPoints.Count == 0) return;
        
        // Get point size
        double pointSize = GetZoomResponsivePointSize();
        double previewSize = pointSize * 1.5; // Slightly larger for visibility
        
        var previewGeometry = new GeometryGroup();
        
        foreach (var pt in _selectedPoints)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            previewGeometry.Children.Add(new EllipseGeometry(screenPt, previewSize / 2, previewSize / 2));
        }
        
        // Create preview path with distinctive style
        _dragPreviewPath = new Path
        {
            Data = previewGeometry,
            Stroke = Brushes.Yellow,
            StrokeThickness = 1.5,
            Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0)), // Semi-transparent yellow
            IsHitTestVisible = false
        };
        
        MainCanvas.Children.Add(_dragPreviewPath);
    }
    
    /// <summary>
    /// Clear drag preview and do full redraw
    /// </summary>
    private void EndDragPreview()
    {
        if (_dragPreviewPath != null)
        {
            MainCanvas.Children.Remove(_dragPreviewPath);
            _dragPreviewPath = null;
        }
        
        // Now do the full redraw to show final positions
        RequestRedraw();
    }
    
    /// <summary>
    /// Optimized KP labels drawing
    /// </summary>
    private void DrawKpLabelsOptimized()
    {
        if (!double.TryParse(TxtKpInterval?.Text, out double kpInterval) || kpInterval <= 0)
            kpInterval = 0.1;
        
        var pointsWithKp = _editablePoints
            .Where(p => p.Kp.HasValue && IsPointVisible(p.X, p.Y))
            .OrderBy(p => p.Kp!.Value)
            .ToList();
        
        if (pointsWithKp.Count == 0) return;
        
        double minKp = pointsWithKp.FirstOrDefault()?.Kp!.Value ?? 0;
        double maxKp = pointsWithKp.LastOrDefault()?.Kp!.Value ?? 0;
        double startKp = Math.Ceiling(minKp / kpInterval) * kpInterval;
        
        for (double kp = startKp; kp <= maxKp; kp += kpInterval)
        {
            var closestPoint = pointsWithKp
                .OrderBy(p => Math.Abs(p.Kp!.Value - kp))
                .FirstOrDefault();
            
            if (closestPoint == null || Math.Abs(closestPoint.Kp!.Value - kp) > kpInterval * 0.5)
                continue;
            
            var screenPt = WorldToScreen(closestPoint.X, closestPoint.Y);
            
            var marker = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.Yellow,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            
            Canvas.SetLeft(marker, screenPt.X - 4);
            Canvas.SetTop(marker, screenPt.Y - 4);
            MainCanvas.Children.Add(marker);
            
            var label = new TextBlock
            {
                Text = $"KP {kp:F3}",
                Foreground = Brushes.Yellow,
                Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                FontSize = 10,
                Padding = new Thickness(3, 1, 3, 1)
            };
            
            Canvas.SetLeft(label, screenPt.X + 6);
            Canvas.SetTop(label, screenPt.Y - 8);
            MainCanvas.Children.Add(label);
        }
    }
    
    /// <summary>
    /// Check if a line is visible in the viewport
    /// </summary>
    private bool IsLineVisible(double x1, double y1, double x2, double y2)
    {
        if (!_viewportCullingEnabled) return true;
        
        // Check if line intersects viewport
        return _visibleViewport.Contains(x1, y1) ||
               _visibleViewport.Contains(x2, y2) ||
               LineIntersectsRect(x1, y1, x2, y2, _visibleViewport);
    }
    
    /// <summary>
    /// Check if line intersects rectangle
    /// </summary>
    private bool LineIntersectsRect(double x1, double y1, double x2, double y2, Rect rect)
    {
        // Simple bounding box check first
        double minX = Math.Min(x1, x2);
        double maxX = Math.Max(x1, x2);
        double minY = Math.Min(y1, y2);
        double maxY = Math.Max(y1, y2);
        
        if (maxX < rect.Left || minX > rect.Right || maxY < rect.Bottom || minY > rect.Top)
            return false;
        
        // More detailed intersection check if needed
        return true;
    }
    
    /// <summary>
    /// Draw the polyline currently being created
    /// </summary>
    private void DrawPolylinePreview()
    {
        if (!_isDrawingPolyline || _currentPolylinePoints.Count == 0) return;
        
        var brush = new SolidColorBrush(Colors.Orange);
        var dashedPen = new Pen(brush, 2) 
        { 
            DashStyle = DashStyles.Dash 
        };
        
        double dotSize = GetZoomResponsivePointSize() * 1.5;
        
        // Draw points
        var pointGeometry = new GeometryGroup();
        foreach (var pt in _currentPolylinePoints)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            pointGeometry.Children.Add(new EllipseGeometry(screenPt, dotSize / 2, dotSize / 2));
        }
        
        if (pointGeometry.Children.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = pointGeometry,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 1,
                IsHitTestVisible = false
            });
        }
        
        // Draw lines connecting points
        if (_currentPolylinePoints.Count >= 2)
        {
            var lineGeometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = WorldToScreen(_currentPolylinePoints[0].X, _currentPolylinePoints[0].Y),
                IsClosed = false
            };
            
            for (int i = 1; i < _currentPolylinePoints.Count; i++)
            {
                var screenPt = WorldToScreen(_currentPolylinePoints[i].X, _currentPolylinePoints[i].Y);
                figure.Segments.Add(new LineSegment(screenPt, true));
            }
            
            lineGeometry.Figures.Add(figure);
            
            MainCanvas.Children.Add(new Path
            {
                Data = lineGeometry,
                Stroke = brush,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            });
        }
        
        // Draw label showing point count
        if (_currentPolylinePoints.Count > 0)
        {
            var lastPt = _currentPolylinePoints[^1];
            var screenPt = WorldToScreen(lastPt.X, lastPt.Y);
            
            var label = new TextBlock
            {
                Text = $"Pt {_currentPolylinePoints.Count}",
                FontSize = 10,
                Foreground = brush,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0))
            };
            
            Canvas.SetLeft(label, screenPt.X + 8);
            Canvas.SetTop(label, screenPt.Y - 15);
            MainCanvas.Children.Add(label);
        }
    }
    
    #endregion
    
    #endregion

    #region Zoom & Pan
    
    private void ZoomToFit()
    {
        _zoomLevel = 1.0;
        CanvasScale.ScaleX = 1.0;
        CanvasScale.ScaleY = 1.0;
        CanvasTranslate.X = 0;
        CanvasTranslate.Y = 0;
        
        UpdateZoomDisplay();
        RequestRedraw();
    }
    
    private void SetZoom(double newZoom, Point? center = null)
    {
        newZoom = Math.Max(0.1, Math.Min(50, newZoom));
        
        if (center.HasValue)
        {
            // Zoom around mouse position
            double factor = newZoom / _zoomLevel;
            
            CanvasTranslate.X = center.Value.X - (center.Value.X - CanvasTranslate.X) * factor;
            CanvasTranslate.Y = center.Value.Y - (center.Value.Y - CanvasTranslate.Y) * factor;
        }
        
        _zoomLevel = newZoom;
        CanvasScale.ScaleX = newZoom;
        CanvasScale.ScaleY = newZoom;
        
        UpdateZoomDisplay();
        RequestRedraw(); // Throttled redraw
    }
    
    private void UpdateZoomDisplay()
    {
        TxtZoomLevel.Text = $"Zoom: {_zoomLevel * 100:F0}%";
    }
    
    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomLevel * 1.25);
    }
    
    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomLevel / 1.25);
    }
    
    private void ZoomFit_Click(object sender, RoutedEventArgs e)
    {
        ZoomToFit();
    }
    
    private void ZoomActual_Click(object sender, RoutedEventArgs e)
    {
        SetZoom(1.0);
        CanvasTranslate.X = 0;
        CanvasTranslate.Y = 0;
    }
    
    #endregion

    #region Mouse Handlers
    
    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var pos = e.GetPosition(MainCanvas);
        double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
        SetZoom(_zoomLevel * factor, pos);
        RequestRedraw(); // Throttled redraw
    }
    
    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Ensure window has focus for keyboard events (Delete key, etc.)
        this.Focus();
        
        var pos = e.GetPosition(this);
        var canvasPos = e.GetPosition(MainCanvas);  // Canvas coordinates for selection
        _lastMousePos = pos;
        
        switch (_currentTool)
        {
            case EditorTool.Pan:
                _isPanning = true;
                MainCanvas.Cursor = Cursors.ScrollAll;
                MainCanvas.CaptureMouse();
                break;
                
            case EditorTool.Select:
                {
                    // AutoCAD-style selection:
                    // Single click - select nearest point (clear others unless Ctrl held)
                    // Ctrl+click - toggle point selection
                    // Shift+click - remove from selection
                    // Click empty space - clear selection (then start rectangle if dragging)
                    
                    var (worldX, worldY) = ScreenToWorld(canvasPos);
                    
                    // Find nearest point within tolerance (respects layer lock)
                    var nearestPoint = FindNearestPoint(worldX, worldY, GetWorldSnapRadius() * 2);
                    
                    bool ctrlHeld = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                    bool shiftHeld = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    
                    if (nearestPoint != null)
                    {
                        // Clicked on a point (on an unlocked layer)
                        if (ctrlHeld)
                        {
                            // Toggle selection
                            if (_selectedPoints.Contains(nearestPoint))
                            {
                                _selectedPoints.Remove(nearestPoint);
                                nearestPoint.IsSelected = false;
                            }
                            else
                            {
                                _selectedPoints.Add(nearestPoint);
                                nearestPoint.IsSelected = true;
                            }
                        }
                        else if (shiftHeld)
                        {
                            // Remove from selection
                            if (_selectedPoints.Contains(nearestPoint))
                            {
                                _selectedPoints.Remove(nearestPoint);
                                nearestPoint.IsSelected = false;
                            }
                        }
                        else
                        {
                            // Clear and select only this point
                            ClearSelection();
                            _selectedPoints.Add(nearestPoint);
                            nearestPoint.IsSelected = true;
                        }
                        
                        UpdateSelectionUI();
                        RedrawCanvas();
                        e.Handled = true;
                    }
                    else
                    {
                        // No selectable point found - check if clicking on a LOCKED point
                        if (IsClickingOnLockedPoint(worldX, worldY, GetWorldSnapRadius() * 2))
                        {
                            TxtStatus.Text = "🔒 Layer is locked - cannot select";
                            e.Handled = true;
                        }
                        else
                        {
                            // Clicked empty space - start rectangle selection
                            if (!ctrlHeld)
                            {
                                ClearSelection();
                            }
                            _isSelecting = true;
                            _selectionStart = canvasPos;  // Use canvas coordinates for selection rectangle
                            MainCanvas.CaptureMouse();
                        }
                    }
                }
                break;
                
            case EditorTool.Digitize:
                {
                    var (worldX, worldY) = ScreenToWorld(e.GetPosition(MainCanvas));
                    var snapped = ApplySnap(worldX, worldY);
                    AddDigitizedPoint(snapped.X, snapped.Y);
                }
                break;
                
            case EditorTool.Measure:
                {
                    var (worldX, worldY) = ScreenToWorld(e.GetPosition(MainCanvas));
                    AddMeasurePoint(worldX, worldY);
                }
                break;
                
            case EditorTool.Insert:
                {
                    var (worldX, worldY) = ScreenToWorld(e.GetPosition(MainCanvas));
                    InsertBlockAt(worldX, worldY);
                }
                break;
                
            case EditorTool.Polyline:
                {
                    var (worldX, worldY) = ScreenToWorld(e.GetPosition(MainCanvas));
                    var snapped = ApplySnap(worldX, worldY);
                    AddPolylinePoint(snapped.X, snapped.Y);
                }
                break;
        }
    }
    
    /// <summary>
    /// Find the nearest editable point within the given tolerance
    /// </summary>
    private EditablePoint? FindNearestPoint(double worldX, double worldY, double tolerance)
    {
        EditablePoint? nearest = null;
        double nearestDist = double.MaxValue;
        
        // Check if the main survey layers are locked
        var smoothedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveySmoothed || 
            (l.LayerType == EditorLayerType.PointsOnly && l.Name.Contains("Smoothed")));
        bool mainLayerLocked = smoothedLayer?.IsLocked == true;
        
        // Only search _editablePoints if the layer is NOT locked
        if (!mainLayerLocked)
        {
            foreach (var pt in _editablePoints)
            {
                if (pt.IsDeleted) continue; // Skip deleted points
                
                double dx = pt.X - worldX;
                double dy = pt.Y - worldY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist < tolerance && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = pt;
                }
            }
        }
        
        // Also check custom layers' points (only if not locked)
        foreach (var layer in Layers.Where(l => l.Points != null && l.IsVisible && !l.IsLocked))
        {
            foreach (var pt in layer.Points!)
            {
                double dx = pt.X - worldX;
                double dy = pt.Y - worldY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist < tolerance && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = pt;
                }
            }
        }
        
        // Check digitized points layer (if not locked)
        var digitizedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.Digitized);
        if (digitizedLayer?.IsLocked != true)
        {
            foreach (var pt in _digitizedPoints)
            {
                double dx = pt.X - worldX;
                double dy = pt.Y - worldY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist < tolerance && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = pt;
                }
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Check if all editable layers are locked
    /// </summary>
    private bool AreAllEditableLayersLocked()
    {
        var smoothedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveySmoothed);
        var rawLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveyRaw);
        var digitizedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.Digitized);
        
        bool mainLocked = (smoothedLayer?.IsLocked != false) && (rawLayer?.IsLocked != false);
        bool digitizedLocked = digitizedLayer?.IsLocked != false;
        
        // Check if any custom layers with points are unlocked
        bool anyCustomUnlocked = Layers.Any(l => l.Points != null && l.IsSelectable && !l.IsLocked);
        
        return mainLocked && digitizedLocked && !anyCustomUnlocked;
    }
    
    /// <summary>
    /// Check if there's a point near the click position that's on a LOCKED layer
    /// Returns true if user is clicking on a locked point (to show feedback)
    /// </summary>
    private bool IsClickingOnLockedPoint(double worldX, double worldY, double tolerance)
    {
        // Check main editable points
        var smoothedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveySmoothed);
        if (smoothedLayer?.IsLocked == true)
        {
            foreach (var pt in _editablePoints)
            {
                double dx = pt.X - worldX;
                double dy = pt.Y - worldY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < tolerance) return true;
            }
        }
        
        // Check digitized points
        var digitizedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.Digitized);
        if (digitizedLayer?.IsLocked == true)
        {
            foreach (var pt in _digitizedPoints)
            {
                double dx = pt.X - worldX;
                double dy = pt.Y - worldY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < tolerance) return true;
            }
        }
        
        // Check locked custom layers
        foreach (var layer in Layers.Where(l => l.Points != null && l.IsLocked))
        {
            foreach (var pt in layer.Points!)
            {
                double dx = pt.X - worldX;
                double dy = pt.Y - worldY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < tolerance) return true;
            }
        }
        
        return false;
    }
    
    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            MainCanvas.Cursor = Cursors.Arrow;
            MainCanvas.ReleaseMouseCapture();
        }
        
        if (_isDraggingPoints)
        {
            _isDraggingPoints = false;
            MainCanvas.ReleaseMouseCapture();
            
            // Check if actually moved
            bool moved = false;
            for (int i = 0; i < _selectedPoints.Count && i < _dragOriginalPositions.Count; i++)
            {
                if (Math.Abs(_selectedPoints[i].X - _dragOriginalPositions[i].X) > 0.001 ||
                    Math.Abs(_selectedPoints[i].Y - _dragOriginalPositions[i].Y) > 0.001)
                {
                    moved = true;
                    break;
                }
            }
            
            if (moved)
            {
                // Create undo action
                PushUndoAction(EditorUndoAction.CreatePointsMoved(_selectedPoints.ToList(), _dragOriginalPositions));
                TxtStatus.Text = $"Moved {_selectedPoints.Count} point(s)";
                UpdateStats();
            }
            else
            {
                TxtStatus.Text = "Ready";
            }
            
            _dragOriginalPositions.Clear();
            
            // Clear drag preview and do full redraw
            EndDragPreview();
        }
        
        if (_isSelecting)
        {
            _isSelecting = false;
            MainCanvas.ReleaseMouseCapture();
            
            if (_selectionRect != null)
            {
                MainCanvas.Children.Remove(_selectionRect);
                _selectionRect = null;
            }
            
            // Select points in rectangle - use canvas coordinates
            var endPos = e.GetPosition(MainCanvas);
            SelectPointsInRect(_selectionStart, endPos);
        }
    }
    
    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Start panning with right mouse button
        _lastMousePos = e.GetPosition(this);
        _isPanning = true;
        MainCanvas.Cursor = Cursors.ScrollAll;
        MainCanvas.CaptureMouse();
    }
    
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        var canvasPos = e.GetPosition(MainCanvas);
        
        // Update cursor position display
        var (worldX, worldY) = ScreenToWorld(canvasPos);
        TxtCursorPos.Text = $"X: {worldX:F2}  Y: {worldY:F2}";
        
        // Show snap indicator when in digitize or measure mode
        if (_snapEnabled && (_currentTool == EditorTool.Digitize || _currentTool == EditorTool.Measure))
        {
            var snapped = ApplySnap(worldX, worldY);
            bool didSnap = Math.Abs(snapped.X - worldX) > 0.001 || Math.Abs(snapped.Y - worldY) > 0.001;
            
            // Update snap indicator
            UpdateSnapIndicator(didSnap ? WorldToScreen(snapped.X, snapped.Y) : (Point?)null);
            
            if (didSnap)
            {
                TxtStatus.Text = $"Snap: X={snapped.X:F3}  Y={snapped.Y:F3}";
            }
        }
        
        if (_isPanning)
        {
            double dx = pos.X - _lastMousePos.X;
            double dy = pos.Y - _lastMousePos.Y;
            
            CanvasTranslate.X += dx;
            CanvasTranslate.Y += dy;
            
            _lastMousePos = pos;
            RequestRedraw(); // Throttled redraw
        }
        
        // Handle point dragging with lightweight preview
        if (_isDraggingPoints && _selectedPoints.Count > 0)
        {
            double dx = worldX - _dragStartWorld.X;
            double dy = worldY - _dragStartWorld.Y;
            
            // Update point positions in memory (for final result)
            for (int i = 0; i < _selectedPoints.Count && i < _dragOriginalPositions.Count; i++)
            {
                _selectedPoints[i].X = _dragOriginalPositions[i].X + dx;
                _selectedPoints[i].Y = _dragOriginalPositions[i].Y + dy;
                _selectedPoints[i].IsModified = true;
            }
            
            TxtStatus.Text = $"Dragging: Δ({dx:F2}, {dy:F2})";
            
            // Use lightweight drag preview instead of full redraw
            UpdateDragPreview();
        }
        
        if (_isSelecting)
        {
            // Draw selection rectangle with AutoCAD-style colors
            // Left-to-right (Window) = Blue solid
            // Right-to-left (Crossing) = Green dashed
            bool isWindowSelection = canvasPos.X > _selectionStart.X;
            
            if (_selectionRect == null)
            {
                _selectionRect = new Rectangle();
                MainCanvas.Children.Add(_selectionRect);
            }
            
            // Update rectangle style based on direction
            double zoomLevel = _zoomLevel > 0 ? _zoomLevel : 1.0;
            double strokeThickness = 1.0 / zoomLevel;
            
            if (isWindowSelection)
            {
                // Window selection - solid blue
                _selectionRect.Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                _selectionRect.StrokeDashArray = null;
                _selectionRect.Fill = new SolidColorBrush(Color.FromArgb(40, 0, 120, 215));
            }
            else
            {
                // Crossing selection - dashed green
                _selectionRect.Stroke = new SolidColorBrush(Color.FromRgb(0, 200, 80));
                _selectionRect.StrokeDashArray = new DoubleCollection { 4, 2 };
                _selectionRect.Fill = new SolidColorBrush(Color.FromArgb(40, 0, 200, 80));
            }
            _selectionRect.StrokeThickness = strokeThickness;
            
            // Use canvas coordinates for rectangle positioning
            double x = Math.Min(_selectionStart.X, canvasPos.X);
            double y = Math.Min(_selectionStart.Y, canvasPos.Y);
            double w = Math.Abs(canvasPos.X - _selectionStart.X);
            double h = Math.Abs(canvasPos.Y - _selectionStart.Y);
            
            Canvas.SetLeft(_selectionRect, x);
            Canvas.SetTop(_selectionRect, y);
            _selectionRect.Width = w;
            _selectionRect.Height = h;
        }
        
        // Update measure tool preview line if measuring
        if (_currentTool == EditorTool.Measure && _measurePoints.Count > 0 && _measureLines.Count > 0)
        {
            // Use world-to-screen conversion for consistency with how lines are drawn
            var screenEndPt = WorldToScreen(worldX, worldY);
            var lastLine = _measureLines[^1];
            lastLine.X2 = screenEndPt.X;
            lastLine.Y2 = screenEndPt.Y;
            
            var lastPoint = _measurePoints[^1];
            double distance = Math.Sqrt(Math.Pow(worldX - lastPoint.X, 2) + Math.Pow(worldY - lastPoint.Y, 2));
            TxtStatus.Text = $"Measuring: {distance:F3}";
        }
    }
    
    // Snap indicator element
    private Ellipse? _snapIndicator = null;
    
    private void UpdateSnapIndicator(Point? snapPoint)
    {
        // Remove existing indicator
        if (_snapIndicator != null)
        {
            MainCanvas.Children.Remove(_snapIndicator);
            _snapIndicator = null;
        }
        
        if (snapPoint.HasValue)
        {
            // AutoCAD-style small snap marker
            // Zoom-responsive: divide by zoom to keep apparent size constant
            double zoomLevel = _zoomLevel > 0 ? _zoomLevel : 1.0;
            double indicatorSize = 8.0 / zoomLevel;
            double strokeThickness = 1.0 / zoomLevel;
            
            _snapIndicator = new Ellipse
            {
                Width = indicatorSize,
                Height = indicatorSize,
                Stroke = Brushes.Yellow,
                StrokeThickness = strokeThickness,
                Fill = null  // No fill - just outline
            };
            
            Canvas.SetLeft(_snapIndicator, snapPoint.Value.X - indicatorSize / 2);
            Canvas.SetTop(_snapIndicator, snapPoint.Value.Y - indicatorSize / 2);
            Panel.SetZIndex(_snapIndicator, 1000);
            MainCanvas.Children.Add(_snapIndicator);
        }
    }
    
    private void Canvas_MouseLeave(object sender, MouseEventArgs e)
    {
        TxtCursorPos.Text = "X: -  Y: -";
    }
    
    private void Point_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_currentTool != EditorTool.Select) return;
        
        if (sender is Ellipse ellipse && ellipse.Tag is EditablePoint point)
        {
            var canvasPos = e.GetPosition(MainCanvas);
            var (worldX, worldY) = ScreenToWorld(canvasPos);
            
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                // Toggle selection
                if (_selectedPoints.Contains(point))
                    _selectedPoints.Remove(point);
                else
                    _selectedPoints.Add(point);
                
                point.IsSelected = _selectedPoints.Contains(point);
                UpdateSelectionUI();
                RedrawCanvas();
            }
            else if (_selectedPoints.Contains(point))
            {
                // Start dragging selected points
                _isDraggingPoints = true;
                _dragStartWorld = new Point(worldX, worldY);
                _dragOriginalPositions = _selectedPoints.Select(p => new Point(p.X, p.Y)).ToList();
                MainCanvas.CaptureMouse();
                TxtStatus.Text = $"Dragging {_selectedPoints.Count} point(s)...";
            }
            else
            {
                // Single select and start drag
                ClearSelection();
                _selectedPoints.Add(point);
                point.IsSelected = true;
                
                _isDraggingPoints = true;
                _dragStartWorld = new Point(worldX, worldY);
                _dragOriginalPositions = _selectedPoints.Select(p => new Point(p.X, p.Y)).ToList();
                MainCanvas.CaptureMouse();
                
                UpdateSelectionUI();
                RedrawCanvas();
                TxtStatus.Text = "Dragging point...";
            }
            
            e.Handled = true;
        }
    }
    
    #endregion

    #region Selection
    
    private void ClearSelection()
    {
        foreach (var pt in _selectedPoints)
            pt.IsSelected = false;
        _selectedPoints.Clear();
        UpdateSelectionUI();
    }
    
    private void SelectPointsInRect(Point start, Point end)
    {
        // Both start and end are now in canvas coordinates
        double minX = Math.Min(start.X, end.X);
        double maxX = Math.Max(start.X, end.X);
        double minY = Math.Min(start.Y, end.Y);
        double maxY = Math.Max(start.Y, end.Y);
        
        // AutoCAD-style selection:
        // Left-to-right drag = Window selection (only fully enclosed points)
        // Right-to-left drag = Crossing selection (any touched points)
        bool isWindowSelection = end.X > start.X;
        
        // Check if main survey layer is locked
        var smoothedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveySmoothed);
        var rawLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.SurveyRaw);
        bool mainLayerLocked = (smoothedLayer?.IsLocked == true) && (rawLayer?.IsLocked == true);
        
        // Only select from main editable points if layer is NOT locked
        if (!mainLayerLocked)
        {
            foreach (var pt in _editablePoints)
            {
                if (_deletedPointIndices.Contains(pt.OriginalIndex)) continue;
                
                // WorldToScreen gives canvas coordinates (before transform)
                var screenPt = WorldToScreen(pt.X, pt.Y);
                
                bool isInside = screenPt.X >= minX && screenPt.X <= maxX &&
                                screenPt.Y >= minY && screenPt.Y <= maxY;
                
                if (isInside)
                {
                    if (!_selectedPoints.Contains(pt))
                    {
                        _selectedPoints.Add(pt);
                        pt.IsSelected = true;
                    }
                }
            }
        }
        
        // Also check custom layers with their own points (only if not locked)
        foreach (var layer in Layers.Where(l => l.Points != null && l.IsSelectable && !l.IsLocked))
        {
            foreach (var pt in layer.Points!)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                
                bool isInside = screenPt.X >= minX && screenPt.X <= maxX &&
                                screenPt.Y >= minY && screenPt.Y <= maxY;
                
                if (isInside && !_selectedPoints.Contains(pt))
                {
                    _selectedPoints.Add(pt);
                    pt.IsSelected = true;
                }
            }
        }
        
        // Check digitized points if layer is not locked
        var digitizedLayer = Layers.FirstOrDefault(l => l.LayerType == EditorLayerType.Digitized);
        if (digitizedLayer?.IsLocked != true)
        {
            foreach (var pt in _digitizedPoints)
            {
                var screenPt = WorldToScreen(pt.X, pt.Y);
                
                bool isInside = screenPt.X >= minX && screenPt.X <= maxX &&
                                screenPt.Y >= minY && screenPt.Y <= maxY;
                
                if (isInside && !_selectedPoints.Contains(pt))
                {
                    _selectedPoints.Add(pt);
                    pt.IsSelected = true;
                }
            }
        }
        
        UpdateSelectionUI();
        RedrawCanvas();
    }
    
    private void UpdateSelectionUI()
    {
        int count = _selectedPoints.Count;
        TxtSelectedCount.Text = count.ToString();
        
        SelectionInfo.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtSelectionInfo.Text = $"{count} point{(count == 1 ? "" : "s")} selected";
        
        BtnDeleteSelected.IsEnabled = count > 0;
        BtnResetSelected.IsEnabled = count > 0;
        
        // Show point properties for single selection
        if (count == 1)
        {
            PointPropertiesPanel.Visibility = Visibility.Visible;
            PointPropertiesContent.Visibility = Visibility.Visible;
            
            _isLoadingProperties = true;
            var pt = _selectedPoints[0];
            TxtPointIndex.Text = pt.OriginalIndex.ToString();
            TxtPointEasting.Text = pt.X.ToString("F3");
            TxtPointNorthing.Text = pt.Y.ToString("F3");
            TxtPointDepth.Text = pt.Z.ToString("F3");
            TxtPointKp.Text = pt.Kp.HasValue ? pt.Kp.Value.ToString("F4") : "-";
            TxtPointDcc.Text = pt.Dcc.HasValue ? pt.Dcc.Value.ToString("F3") : "-";
            TxtPointDal.Text = pt.Dal.HasValue ? $"{pt.Dal.Value:F3} m" : "-";
            _isLoadingProperties = false;
        }
        else
        {
            PointPropertiesPanel.Visibility = Visibility.Collapsed;
            PointPropertiesContent.Visibility = Visibility.Collapsed;
        }
    }
    
    /// <summary>
    /// Update tool button checked states to match current tool
    /// </summary>
    private void UpdateToolButtonStates()
    {
        ToolPan.IsChecked = (_currentTool == EditorTool.Pan);
        ToolSelect.IsChecked = (_currentTool == EditorTool.Select);
        ToolDigitize.IsChecked = (_currentTool == EditorTool.Digitize);
        ToolMeasure.IsChecked = (_currentTool == EditorTool.Measure);
        ToolInsert.IsChecked = (_currentTool == EditorTool.Insert);
        ToolMeasureInterval.IsChecked = (_currentTool == EditorTool.MeasureInterval);
        
        // Update tool panels and cursor based on current tool state
        UpdateToolState();
    }
    
    /// <summary>
    /// Update tool panels and cursor based on current tool state
    /// </summary>
    private void UpdateToolState()
    {
        // Hide all tool panels first
        DigitizePanel.Visibility = Visibility.Collapsed;
        DigitizeContent.Visibility = Visibility.Collapsed;
        MeasureToolHeader.Visibility = Visibility.Collapsed;
        MeasureToolPanel.Visibility = Visibility.Collapsed;
        InsertBlockHeader.Visibility = Visibility.Collapsed;
        InsertBlockPanel.Visibility = Visibility.Collapsed;
        MeasureIntervalHeader.Visibility = Visibility.Collapsed;
        MeasureIntervalPanel.Visibility = Visibility.Collapsed;
        DrawModePanel.Visibility = Visibility.Collapsed;
        
        // Set cursor and show appropriate panel based on tool
        switch (_currentTool)
        {
            case EditorTool.Pan:
                MainCanvas.Cursor = Cursors.Arrow;
                break;
            case EditorTool.Select:
                MainCanvas.Cursor = Cursors.Cross;
                break;
            case EditorTool.Digitize:
                MainCanvas.Cursor = Cursors.Pen;
                DigitizePanel.Visibility = Visibility.Visible;
                DigitizeContent.Visibility = Visibility.Visible;
                break;
            case EditorTool.Measure:
                MainCanvas.Cursor = Cursors.Cross;
                MeasureToolHeader.Visibility = Visibility.Visible;
                MeasureToolPanel.Visibility = Visibility.Visible;
                break;
            case EditorTool.Insert:
                MainCanvas.Cursor = Cursors.Cross;
                InsertBlockHeader.Visibility = Visibility.Visible;
                InsertBlockPanel.Visibility = Visibility.Visible;
                break;
            case EditorTool.MeasureInterval:
                MainCanvas.Cursor = Cursors.Cross;
                MeasureIntervalHeader.Visibility = Visibility.Visible;
                MeasureIntervalPanel.Visibility = Visibility.Visible;
                break;
        }
    }
    
    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPoints.Count == 0) return;
        
        var result = MessageBox.Show(
            $"Delete {_selectedPoints.Count} selected point(s)?\n\nThis will remove points from all layers including Survey data.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            int deletedCount = _selectedPoints.Count;
            
            // Create undo action
            PushUndoAction(EditorUndoAction.CreatePointsDeleted(_selectedPoints.ToList()));
            
            foreach (var pt in _selectedPoints.ToList())
            {
                // Remove from main editable points
                _editablePoints.Remove(pt);
                
                // Remove from digitized points
                _digitizedPoints.Remove(pt);
                
                // Remove from ALL layer Points collections
                foreach (var layer in Layers.Where(l => l.Points != null))
                {
                    layer.Points!.Remove(pt);
                }
                
                // Mark as deleted/excluded if it has a source point
                if (pt.SourcePoint != null)
                {
                    pt.SourcePoint.IsExcluded = true;
                }
                
                // Track deletion for undo
                if (pt.OriginalIndex >= 0 && !_deletedPointIndices.Contains(pt.OriginalIndex))
                {
                    _deletedPointIndices.Add(pt.OriginalIndex);
                }
            }
            
            ClearSelection();
            RedrawCanvas();
            UpdateStats();
            TxtStatus.Text = $"Deleted {deletedCount} point(s)";
        }
    }
    
    private void ResetSelected_Click(object sender, RoutedEventArgs e)
    {
        foreach (var pt in _selectedPoints)
        {
            pt.ResetToOriginal();
        }
        
        RedrawCanvas();
        UpdateStats();
        UpdateSelectionUI();
    }
    
    #endregion

    #region Smoothing
    
    private void SmoothingSource_Changed(object sender, SelectionChangedEventArgs e)
    {
        // Refresh smoothing when source layer changes
        ApplySmoothing();
    }
    
    private void Smoothing_Changed(object sender, RoutedEventArgs e)
    {
        SyncSmoothingTextBoxes();
        ApplySmoothing();  // Real-time update
    }
    
    private void Smoothing_Changed(object sender, SelectionChangedEventArgs e)
    {
        SyncSmoothingTextBoxes();
        ApplySmoothing();  // Real-time update
    }
    
    // Handler for slider ValueChanged events
    private void Smoothing_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SyncSmoothingTextBoxes();
        ApplySmoothing();  // Real-time update
    }
    
    private void SyncSmoothingTextBoxes()
    {
        if (_isUpdating) return;
        _isUpdating = true;
        
        // Sync slider values to text boxes
        if (TxtPositionWindow != null && SliderPositionWindow != null)
            TxtPositionWindow.Text = ((int)SliderPositionWindow.Value).ToString();
        if (TxtDepthWindow != null && SliderDepthWindow != null)
            TxtDepthWindow.Text = ((int)SliderDepthWindow.Value).ToString();
        if (TxtAltitudeWindow != null && SliderAltitudeWindow != null)
            TxtAltitudeWindow.Text = ((int)SliderAltitudeWindow.Value).ToString();
        if (TxtThreshold != null && SliderThreshold != null)
            TxtThreshold.Text = SliderThreshold.Value.ToString("F2");
        
        _isUpdating = false;
    }
    
    private void ApplySmoothing()
    {
        if (_surveyPoints.Count == 0) return;
        if (_isUpdating) return;  // Skip if already updating to prevent recursion
        
        // Throttle rapid updates (reduced from 50ms to 16ms for smoother response)
        if ((DateTime.Now - _lastSmoothingUpdate).TotalMilliseconds < 16)
            return;
        _lastSmoothingUpdate = DateTime.Now;
        
        // Check if any smoothing is enabled
        bool positionEnabled = ChkSmoothPosition?.IsChecked == true;
        bool depthEnabled = ChkSmoothDepth?.IsChecked == true;
        bool altitudeEnabled = ChkSmoothAltitude?.IsChecked == true;
        bool anySmoothingEnabled = positionEnabled || depthEnabled || altitudeEnabled;
        
        // Auto-create or remove smoothed layer based on smoothing state
        EnsureSmoothedLayerExists(anySmoothingEnabled);
        
        if (!anySmoothingEnabled)
        {
            // No smoothing enabled - reset to original values
            for (int i = 0; i < _editablePoints.Count && i < _surveyPoints.Count; i++)
            {
                _editablePoints[i].X = _surveyPoints[i].Easting;
                _editablePoints[i].Y = _surveyPoints[i].Northing;
            }
            
            // Reset statistics
            TxtRmsDisplacement.Text = "-";
            TxtMaxDisplacement.Text = "-";
            TxtMeanDisplacement.Text = "-";
            TxtPointsModified.Text = "0";
            TxtModifiedCount.Text = "0";
            
            RedrawCanvas();
            return;
        }
        
        // Get settings from UI - prefer TextBox for extended range, fallback to slider
        var posMethod = GetSmoothingMethod(CboPositionMethod);
        int posWindow = GetWindowValue(TxtPositionWindow, SliderPositionWindow, 5);
        
        var depthMethod = GetSmoothingMethod(CboDepthMethod);
        int depthWindow = GetWindowValue(TxtDepthWindow, SliderDepthWindow, 5);
        
        var altMethod = GetSmoothingMethod(CboAltitudeMethod);
        int altWindow = GetWindowValue(TxtAltitudeWindow, SliderAltitudeWindow, 5);
        
        double threshold = GetThresholdValue(TxtThreshold, SliderThreshold, 1.0);
        
        // Build smoothing settings
        _smoothingSettings.SmoothEasting = positionEnabled;
        _smoothingSettings.SmoothNorthing = positionEnabled;
        _smoothingSettings.SmoothDepth = depthEnabled;
        _smoothingSettings.Method = posMethod;
        _smoothingSettings.WindowSize = posWindow;
        _smoothingSettings.Threshold = threshold;
        
        // Apply smoothing to survey points
        _smoothingService.ApplySmoothing(_surveyPoints, _smoothingSettings);
        
        // Update editable points from survey points and mark as modified
        for (int i = 0; i < _editablePoints.Count && i < _surveyPoints.Count; i++)
        {
            double newX, newY;
            if (positionEnabled)
            {
                newX = _surveyPoints[i].SmoothedEasting ?? _surveyPoints[i].Easting;
                newY = _surveyPoints[i].SmoothedNorthing ?? _surveyPoints[i].Northing;
            }
            else
            {
                newX = _surveyPoints[i].Easting;
                newY = _surveyPoints[i].Northing;
            }
            
            // Check if position changed significantly
            bool posChanged = Math.Abs(newX - _editablePoints[i].OriginalX) > 0.0001 || 
                              Math.Abs(newY - _editablePoints[i].OriginalY) > 0.0001;
            
            _editablePoints[i].X = newX;
            _editablePoints[i].Y = newY;
            
            // Mark as modified if position changed from original
            if (posChanged)
            {
                _editablePoints[i].IsModified = true;
            }
        }
        
        // Update smoothed layer with current smoothed points
        UpdateSmoothedLayerPoints();
        
        // Calculate statistics
        var stats = _smoothingService.CalculateStatistics(_surveyPoints);
        TxtRmsDisplacement.Text = $"{stats.RmsDisplacement:F4}";
        TxtMaxDisplacement.Text = $"{stats.MaxDisplacement:F4}";
        TxtMeanDisplacement.Text = $"{stats.MeanDisplacement:F4}";
        
        int modified = _editablePoints.Count(p => 
            Math.Abs(p.X - p.OriginalX) > 0.0001 || Math.Abs(p.Y - p.OriginalY) > 0.0001);
        TxtPointsModified.Text = modified.ToString();
        TxtModifiedCount.Text = modified.ToString();
        
        RedrawCanvas();
    }
    
    /// <summary>
    /// Get window value from TextBox (extended range) or Slider (limited range)
    /// </summary>
    private int GetWindowValue(System.Windows.Controls.TextBox? textBox, Slider? slider, int defaultValue)
    {
        if (textBox != null && int.TryParse(textBox.Text, out int txtValue) && txtValue >= 3)
            return Math.Min(txtValue, 999);  // Cap at 999
        if (slider != null)
            return (int)slider.Value;
        return defaultValue;
    }
    
    /// <summary>
    /// Get threshold value from TextBox or Slider
    /// </summary>
    private double GetThresholdValue(System.Windows.Controls.TextBox? textBox, Slider? slider, double defaultValue)
    {
        if (textBox != null && double.TryParse(textBox.Text, out double txtValue) && txtValue >= 0.01)
            return Math.Min(txtValue, 100);  // Cap at 100
        if (slider != null)
            return slider.Value;
        return defaultValue;
    }
    
    /// <summary>
    /// Ensure smoothed layer exists when smoothing is enabled, hide when disabled
    /// </summary>
    private void EnsureSmoothedLayerExists(bool smoothingEnabled)
    {
        var smoothedLinesLayer = Layers.FirstOrDefault(l => l.Name == "Survey Smoothed - Lines");
        var smoothedPointsLayer = Layers.FirstOrDefault(l => l.Name == "Survey Smoothed - Points");
        
        if (smoothingEnabled)
        {
            // Create smoothed layers if they don't exist
            if (smoothedLinesLayer == null || smoothedPointsLayer == null)
            {
                // Find where to insert (after Survey Raw layers)
                int insertIndex = Layers.Count;
                for (int i = 0; i < Layers.Count; i++)
                {
                    if (Layers[i].Name?.Contains("Survey Raw") == true)
                    {
                        insertIndex = i + 1;
                    }
                }
                
                // Create smoothed layer group
                var (smoothedPoints, smoothedLines) = EditorLayer.CreateSubLayers("Survey Smoothed", Colors.Orange, 50);
                smoothedLines.Thickness = 2.0;
                smoothedPoints.PointSize = 4;  // Same as normal points
                smoothedLines.IsVisible = true;
                smoothedPoints.IsVisible = true;
                
                // Insert at appropriate position
                if (smoothedLinesLayer == null)
                {
                    if (insertIndex < Layers.Count)
                        Layers.Insert(insertIndex, smoothedLines);
                    else
                        Layers.Add(smoothedLines);
                }
                if (smoothedPointsLayer == null)
                {
                    var linesIdx = Layers.IndexOf(smoothedLines);
                    if (linesIdx >= 0 && linesIdx + 1 <= Layers.Count)
                        Layers.Insert(linesIdx + 1, smoothedPoints);
                    else
                        Layers.Add(smoothedPoints);
                }
                
                // Subscribe to layer changes
                foreach (var layer in Layers.Where(l => l.Name?.Contains("Survey Smoothed") == true))
                {
                    layer.PropertyChanged -= Layer_PropertyChanged;
                    layer.PropertyChanged += Layer_PropertyChanged;
                }
                
                TxtStatus.Text = "Smoothed layer created - adjust settings to see real-time changes";
            }
            else
            {
                // Make sure layers are visible
                smoothedLinesLayer.IsVisible = true;
                smoothedPointsLayer.IsVisible = true;
            }
        }
        else
        {
            // Hide smoothed layers when smoothing is disabled (but don't remove)
            if (smoothedLinesLayer != null) smoothedLinesLayer.IsVisible = false;
            if (smoothedPointsLayer != null) smoothedPointsLayer.IsVisible = false;
        }
    }
    
    /// <summary>
    /// Update smoothed layer points from current _editablePoints
    /// </summary>
    private void UpdateSmoothedLayerPoints()
    {
        var smoothedLinesLayer = Layers.FirstOrDefault(l => l.Name == "Survey Smoothed - Lines");
        var smoothedPointsLayer = Layers.FirstOrDefault(l => l.Name == "Survey Smoothed - Points");
        
        if (smoothedLinesLayer == null && smoothedPointsLayer == null) return;
        
        // Create points list from current smoothed positions
        var smoothedPoints = _editablePoints.Select((p, idx) => new EditablePoint
        {
            X = p.X,
            Y = p.Y,
            Z = p.Z,
            OriginalX = p.OriginalX,
            OriginalY = p.OriginalY,
            OriginalZ = p.OriginalZ,
            OriginalIndex = idx
        }).ToList();
        
        if (smoothedLinesLayer != null)
        {
            if (smoothedLinesLayer.Points == null)
                smoothedLinesLayer.Points = new System.Collections.ObjectModel.ObservableCollection<EditablePoint>();
            smoothedLinesLayer.Points.Clear();
            foreach (var pt in smoothedPoints)
                smoothedLinesLayer.Points.Add(pt);
        }
        
        if (smoothedPointsLayer != null)
        {
            if (smoothedPointsLayer.Points == null)
                smoothedPointsLayer.Points = new System.Collections.ObjectModel.ObservableCollection<EditablePoint>();
            smoothedPointsLayer.Points.Clear();
            foreach (var pt in smoothedPoints)
                smoothedPointsLayer.Points.Add(pt);
        }
    }
    
    // TextBox handlers for manual input
    private void ThresholdText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || TxtThreshold == null || SliderThreshold == null) return;
        if (double.TryParse(TxtThreshold.Text, out double value) && value >= 0.01 && value <= 100)
        {
            _isUpdating = true;
            // Clamp to slider range for slider, but allow full range in textbox
            SliderThreshold.Value = Math.Min(Math.Max(value, 0.01), 10.0);
            _isUpdating = false;
            ApplySmoothing();
        }
    }
    
    private void PositionWindowText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || TxtPositionWindow == null || SliderPositionWindow == null) return;
        if (int.TryParse(TxtPositionWindow.Text, out int value) && value >= 3 && value <= 999)
        {
            _isUpdating = true;
            // Clamp to slider range for slider, but allow full range in textbox
            SliderPositionWindow.Value = Math.Min(Math.Max(value, 3), 101);
            _isUpdating = false;
            ApplySmoothing();
        }
    }
    
    private void DepthWindowText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || TxtDepthWindow == null || SliderDepthWindow == null) return;
        if (int.TryParse(TxtDepthWindow.Text, out int value) && value >= 3 && value <= 999)
        {
            _isUpdating = true;
            // Clamp to slider range for slider, but allow full range in textbox
            SliderDepthWindow.Value = Math.Min(Math.Max(value, 3), 101);
            _isUpdating = false;
            ApplySmoothing();
        }
    }
    
    private void AltitudeWindowText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || TxtAltitudeWindow == null || SliderAltitudeWindow == null) return;
        if (int.TryParse(TxtAltitudeWindow.Text, out int value) && value >= 3 && value <= 999)
        {
            _isUpdating = true;
            // Clamp to slider range for slider, but allow full range in textbox
            SliderAltitudeWindow.Value = Math.Min(Math.Max(value, 3), 101);
            _isUpdating = false;
            ApplySmoothing();
        }
    }
    
    private void ApplySmoothing_Click(object sender, RoutedEventArgs e)
    {
        if (ChkSmoothPosition.IsChecked != true && ChkSmoothDepth.IsChecked != true && ChkSmoothAltitude.IsChecked != true)
        {
            MessageBox.Show("Please enable at least one smoothing option (Position, Depth, or Altitude).", 
                "No Smoothing Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        ApplySmoothing();
        TxtStatus.Text = "Smoothing applied to preview";
        MessageBox.Show("Smoothing applied. Use 'Apply Changes' to save to project.", "Smoothing Applied", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private void SplineTensionText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || TxtSplineTension == null || SliderSplineTension == null) return;
        if (double.TryParse(TxtSplineTension.Text, out double value) && value >= 0 && value <= 2)
        {
            _isUpdating = true;
            SliderSplineTension.Value = value;
            _isUpdating = false;
        }
    }
    
    private void SplineTensionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating || TxtSplineTension == null) return;
        _isUpdating = true;
        TxtSplineTension.Text = SliderSplineTension.Value.ToString("F2");
        _isUpdating = false;
    }
    
    private void SplineOutputMultiplierText_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || TxtSplineOutputMultiplier == null || SliderSplineOutputMultiplier == null) return;
        if (double.TryParse(TxtSplineOutputMultiplier.Text, out double value) && value >= 1 && value <= 20)
        {
            _isUpdating = true;
            SliderSplineOutputMultiplier.Value = value;
            _isUpdating = false;
        }
    }
    
    private void SplineOutputMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating || TxtSplineOutputMultiplier == null) return;
        _isUpdating = true;
        TxtSplineOutputMultiplier.Text = ((int)SliderSplineOutputMultiplier.Value).ToString();
        _isUpdating = false;
    }
    
    #endregion

    #region Digitization
    
    private void AddDigitizedPoint(double worldX, double worldY)
    {
        // Check if a layer is selected
        var selectedLayer = LayerList.SelectedItem as EditorLayer;
        
        var point = new EditablePoint
        {
            X = worldX,
            Y = worldY,
            Z = 0,
            OriginalX = worldX,
            OriginalY = worldY,
            OriginalZ = 0,
            OriginalIndex = -1,
            IsModified = true,
            IsAdded = true
        };
        
        // If a layer is selected, add point to that layer
        if (selectedLayer != null)
        {
            // For Survey Raw/Smoothed sub-layers (Points/Lines), add to _editablePoints
            // because these layers draw from renderData, not layer.Points
            if (selectedLayer.ParentLayerName?.Contains("Survey") == true || 
                selectedLayer.LayerType == EditorLayerType.SurveyRaw ||
                selectedLayer.LayerType == EditorLayerType.SurveySmoothed)
            {
                point.Index = _editablePoints.Count;
                point.LayerName = selectedLayer.Name;
                _editablePoints.Add(point);
                
                // Push to undo stack
                PushUndoAction(EditorUndoAction.CreatePointsAdded(new List<EditablePoint> { point }));
                
                RedrawCanvas();
                TxtStatus.Text = $"Added point to survey data at ({worldX:F2}, {worldY:F2})";
                return;
            }
            
            // For other layers, use their Points collection
            if (selectedLayer.Points == null)
            {
                selectedLayer.Points = new ObservableCollection<EditablePoint>();
            }
            
            point.Index = selectedLayer.Points.Count + 1;
            point.LayerName = selectedLayer.Name;
            selectedLayer.Points.Add(point);
            
            // Push to undo stack
            PushUndoAction(EditorUndoAction.CreatePointsAdded(new List<EditablePoint> { point }));
            
            RedrawCanvas();
            TxtStatus.Text = $"Added point to '{selectedLayer.Name}' at ({worldX:F2}, {worldY:F2})";
            return;
        }
        
        // No layer selected - add to digitized points (default)
        point.Index = _digitizedPoints.Count + 1000000;
        point.LayerName = "Digitized";
        _digitizedPoints.Add(point);
        
        // Push to undo stack
        PushUndoAction(EditorUndoAction.CreatePointsAdded(new List<EditablePoint> { point }));
        
        RedrawCanvas();
        TxtStatus.Text = $"Added digitized point at ({worldX:F2}, {worldY:F2}) - Select a layer to add points to it";
    }
    
    private void ClearDigitized_Click(object sender, RoutedEventArgs e)
    {
        _digitizedPoints.Clear();
        RedrawCanvas();
    }
    
    #endregion

    #region Tools
    
    private void Tool_Click(object sender, RoutedEventArgs e)
    {
        // Hide all tool panels first
        DigitizePanel.Visibility = Visibility.Collapsed;
        DigitizeContent.Visibility = Visibility.Collapsed;
        MeasureToolHeader.Visibility = Visibility.Collapsed;
        MeasureToolPanel.Visibility = Visibility.Collapsed;
        InsertBlockHeader.Visibility = Visibility.Collapsed;
        InsertBlockPanel.Visibility = Visibility.Collapsed;
        MeasureIntervalHeader.Visibility = Visibility.Collapsed;
        MeasureIntervalPanel.Visibility = Visibility.Collapsed;
        DrawModePanel.Visibility = Visibility.Collapsed;
        
        if (ToolPan.IsChecked == true)
        {
            _currentTool = EditorTool.Pan;
            MainCanvas.Cursor = Cursors.Arrow;
        }
        else if (ToolSelect.IsChecked == true)
        {
            _currentTool = EditorTool.Select;
            MainCanvas.Cursor = Cursors.Cross;
        }
        else if (ToolDigitize.IsChecked == true)
        {
            _currentTool = EditorTool.Digitize;
            MainCanvas.Cursor = Cursors.Pen;
            DigitizePanel.Visibility = Visibility.Visible;
            DigitizeContent.Visibility = Visibility.Visible;
        }
        else if (ToolMeasure.IsChecked == true)
        {
            _currentTool = EditorTool.Measure;
            MainCanvas.Cursor = Cursors.Cross;
            MeasureToolHeader.Visibility = Visibility.Visible;
            MeasureToolPanel.Visibility = Visibility.Visible;
            
            // Clear any existing measurement
            ClearMeasurement();
        }
        else if (ToolInsert.IsChecked == true)
        {
            _currentTool = EditorTool.Insert;
            MainCanvas.Cursor = Cursors.Cross;
            InsertBlockHeader.Visibility = Visibility.Visible;
            InsertBlockPanel.Visibility = Visibility.Visible;
        }
        else if (ToolMeasureInterval.IsChecked == true)
        {
            _currentTool = EditorTool.MeasureInterval;
            MainCanvas.Cursor = Cursors.Cross;
            MeasureIntervalHeader.Visibility = Visibility.Visible;
            MeasureIntervalPanel.Visibility = Visibility.Visible;
            
            // Populate source layer dropdown with ALL available layers that have data
            CboMeasureSourceLayer.Items.Clear();
            
            // Add survey layers (smoothed and raw)
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.SurveySmoothed || 
                                                     l.LayerType == EditorLayerType.SurveyRaw))
            {
                CboMeasureSourceLayer.Items.Add(layer.Name);
            }
            
            // Add Points sub-layers
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.PointsOnly))
            {
                CboMeasureSourceLayer.Items.Add(layer.Name);
            }
            
            // Add Lines sub-layers  
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.LinesOnly))
            {
                CboMeasureSourceLayer.Items.Add(layer.Name);
            }
            
            // Add custom layers with points
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.Custom && l.Points?.Count > 0))
            {
                CboMeasureSourceLayer.Items.Add(layer.Name);
            }
            
            // Add spline layer if available
            if (_editorSplinePoints.Count > 0)
            {
                CboMeasureSourceLayer.Items.Add("Spline Fitted");
            }
            
            if (CboMeasureSourceLayer.Items.Count > 0)
                CboMeasureSourceLayer.SelectedIndex = 0;
            
            // Set default end KP from data (for KP mode)
            if (_editablePoints.Any(p => p.Kp.HasValue))
            {
                var maxKp = _editablePoints.Where(p => p.Kp.HasValue).Max(p => p.Kp!.Value);
                TxtMeasureEndKp.Text = maxKp.ToString("F3");
            }
            
            // Calculate DAL so distance mode can work
            CalculateDistanceAlongLine();
            
            TxtMeasureIntervalStatus.Text = "Select mode and source layer, then click 'Generate Points'";
        }
        
        TxtStatus.Text = $"Tool: {_currentTool}";
    }
    
    #endregion
    
    #region Snap Functions
    
    private void SnapOption_Changed(object sender, RoutedEventArgs e)
    {
        _snapEnabled = ChkSnapEnabled?.IsChecked == true;
        _snapEndpoint = ChkSnapEndpoint?.IsChecked == true;
        _snapMidpoint = ChkSnapMidpoint?.IsChecked == true;
        _snapNearest = ChkSnapNearest?.IsChecked == true;
        _snapIntersection = ChkSnapIntersection?.IsChecked == true;
        _snapGrid = ChkSnapGrid?.IsChecked == true;
        _snapPerpendicular = ChkSnapPerpendicular?.IsChecked == true;
        
        if (double.TryParse(TxtSnapRadius?.Text, out double radius))
            _snapRadius = radius;
        if (double.TryParse(TxtGridSnapSize?.Text, out double gridSize))
            _gridSnapSize = gridSize;
    }
    
    private (double X, double Y) ApplySnap(double worldX, double worldY)
    {
        if (!_snapEnabled) return (worldX, worldY);
        
        double bestSnapX = worldX;
        double bestSnapY = worldY;
        double bestDistance = double.MaxValue;
        
        // Convert snap radius from screen to world coordinates
        double worldSnapRadius = _snapRadius / (_zoomLevel * Math.Min(
            MainCanvas.ActualWidth / _dataWidth,
            MainCanvas.ActualHeight / _dataHeight));
        
        // Grid snap
        if (_snapGrid)
        {
            double gridX = Math.Round(worldX / _gridSnapSize) * _gridSnapSize;
            double gridY = Math.Round(worldY / _gridSnapSize) * _gridSnapSize;
            double dist = Math.Sqrt(Math.Pow(worldX - gridX, 2) + Math.Pow(worldY - gridY, 2));
            if (dist < bestDistance && dist < worldSnapRadius)
            {
                bestSnapX = gridX;
                bestSnapY = gridY;
                bestDistance = dist;
            }
        }
        
        // Endpoint snap - snap to survey points
        if (_snapEndpoint)
        {
            foreach (var pt in _editablePoints)
            {
                double dist = Math.Sqrt(Math.Pow(worldX - pt.X, 2) + Math.Pow(worldY - pt.Y, 2));
                if (dist < bestDistance && dist < worldSnapRadius)
                {
                    bestSnapX = pt.X;
                    bestSnapY = pt.Y;
                    bestDistance = dist;
                }
            }
            
            // Also snap to digitized points
            foreach (var pt in _digitizedPoints)
            {
                double dist = Math.Sqrt(Math.Pow(worldX - pt.X, 2) + Math.Pow(worldY - pt.Y, 2));
                if (dist < bestDistance && dist < worldSnapRadius)
                {
                    bestSnapX = pt.X;
                    bestSnapY = pt.Y;
                    bestDistance = dist;
                }
            }
        }
        
        // Midpoint snap
        if (_snapMidpoint && _editablePoints.Count > 1)
        {
            for (int i = 0; i < _editablePoints.Count - 1; i++)
            {
                double midX = (_editablePoints[i].X + _editablePoints[i + 1].X) / 2;
                double midY = (_editablePoints[i].Y + _editablePoints[i + 1].Y) / 2;
                double dist = Math.Sqrt(Math.Pow(worldX - midX, 2) + Math.Pow(worldY - midY, 2));
                if (dist < bestDistance && dist < worldSnapRadius)
                {
                    bestSnapX = midX;
                    bestSnapY = midY;
                    bestDistance = dist;
                }
            }
        }
        
        // Nearest point on line snap
        if (_snapNearest && _editablePoints.Count > 1)
        {
            for (int i = 0; i < _editablePoints.Count - 1; i++)
            {
                var nearest = GetNearestPointOnLine(
                    _editablePoints[i].X, _editablePoints[i].Y,
                    _editablePoints[i + 1].X, _editablePoints[i + 1].Y,
                    worldX, worldY);
                    
                double dist = Math.Sqrt(Math.Pow(worldX - nearest.X, 2) + Math.Pow(worldY - nearest.Y, 2));
                if (dist < bestDistance && dist < worldSnapRadius)
                {
                    bestSnapX = nearest.X;
                    bestSnapY = nearest.Y;
                    bestDistance = dist;
                }
            }
        }
        
        // Route snap
        if (_routeData != null && (_snapEndpoint || _snapNearest))
        {
            foreach (var seg in _routeData.Segments)
            {
                if (_snapEndpoint)
                {
                    // Start point
                    double dist1 = Math.Sqrt(Math.Pow(worldX - seg.StartEasting, 2) + Math.Pow(worldY - seg.StartNorthing, 2));
                    if (dist1 < bestDistance && dist1 < worldSnapRadius)
                    {
                        bestSnapX = seg.StartEasting;
                        bestSnapY = seg.StartNorthing;
                        bestDistance = dist1;
                    }
                    
                    // End point
                    double dist2 = Math.Sqrt(Math.Pow(worldX - seg.EndEasting, 2) + Math.Pow(worldY - seg.EndNorthing, 2));
                    if (dist2 < bestDistance && dist2 < worldSnapRadius)
                    {
                        bestSnapX = seg.EndEasting;
                        bestSnapY = seg.EndNorthing;
                        bestDistance = dist2;
                    }
                }
                
                if (_snapNearest)
                {
                    var nearest = GetNearestPointOnLine(
                        seg.StartEasting, seg.StartNorthing,
                        seg.EndEasting, seg.EndNorthing,
                        worldX, worldY);
                        
                    double dist = Math.Sqrt(Math.Pow(worldX - nearest.X, 2) + Math.Pow(worldY - nearest.Y, 2));
                    if (dist < bestDistance && dist < worldSnapRadius)
                    {
                        bestSnapX = nearest.X;
                        bestSnapY = nearest.Y;
                        bestDistance = dist;
                    }
                }
            }
        }
        
        return (bestSnapX, bestSnapY);
    }
    
    private (double X, double Y) GetNearestPointOnLine(double x1, double y1, double x2, double y2, double px, double py)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        double lengthSq = dx * dx + dy * dy;
        
        if (lengthSq == 0) return (x1, y1);
        
        double t = Math.Max(0, Math.Min(1, ((px - x1) * dx + (py - y1) * dy) / lengthSq));
        return (x1 + t * dx, y1 + t * dy);
    }
    
    #endregion
    
    #region Measure Tool
    
    private void ClearMeasurement()
    {
        foreach (var line in _measureLines)
        {
            MainCanvas.Children.Remove(line);
        }
        _measureLines.Clear();
        _measurePoints.Clear();
        _measureCumulative = 0;
        _measurePolygonClosed = false;
        
        UpdateMeasureDisplay();
    }
    
    private void ClearMeasure_Click(object sender, RoutedEventArgs e)
    {
        ClearMeasurement();
        RedrawCanvas();
    }
    
    private void CloseMeasurePolygon_Click(object sender, RoutedEventArgs e)
    {
        if (_measurePoints.Count >= 3 && !_measurePolygonClosed)
        {
            // Add closing line
            var startPt = WorldToScreen(_measurePoints[0].X, _measurePoints[0].Y);
            var endPt = WorldToScreen(_measurePoints[^1].X, _measurePoints[^1].Y);
            
            var closingLine = new Line
            {
                X1 = startPt.X, Y1 = startPt.Y,
                X2 = endPt.X, Y2 = endPt.Y,
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            MainCanvas.Children.Add(closingLine);
            _measureLines.Add(closingLine);
            
            // Add closing distance to cumulative
            double closingDist = Math.Sqrt(
                Math.Pow(_measurePoints[^1].X - _measurePoints[0].X, 2) +
                Math.Pow(_measurePoints[^1].Y - _measurePoints[0].Y, 2));
            _measureCumulative += closingDist;
            
            _measurePolygonClosed = true;
            UpdateMeasureDisplay();
        }
    }
    
    private void AddMeasurePoint(double worldX, double worldY)
    {
        var snapped = ApplySnap(worldX, worldY);
        var newPoint = new Point(snapped.X, snapped.Y);
        
        // Get zoom-responsive sizes (divide by zoom to keep apparent size constant)
        double zoomLevel = _zoomLevel > 0 ? _zoomLevel : 1.0;
        double markerSize = 8.0 / zoomLevel;  // Target 8px on screen
        double lineThickness = 2.0 / zoomLevel;  // Target 2px on screen
        double strokeThickness = 1.0 / zoomLevel;  // Target 1px on screen
        
        if (_measurePoints.Count > 0)
        {
            var lastPoint = _measurePoints[^1];
            double distance = Math.Sqrt(
                Math.Pow(newPoint.X - lastPoint.X, 2) +
                Math.Pow(newPoint.Y - lastPoint.Y, 2));
            _measureCumulative += distance;
            
            // Draw line
            var startPt = WorldToScreen(lastPoint.X, lastPoint.Y);
            var endPt = WorldToScreen(newPoint.X, newPoint.Y);
            
            var line = new Line
            {
                X1 = startPt.X, Y1 = startPt.Y,
                X2 = endPt.X, Y2 = endPt.Y,
                Stroke = Brushes.Lime,
                StrokeThickness = lineThickness
            };
            MainCanvas.Children.Add(line);
            _measureLines.Add(line);
        }
        
        // Draw point marker
        var screenPt = WorldToScreen(newPoint.X, newPoint.Y);
        var marker = new Ellipse
        {
            Width = markerSize, 
            Height = markerSize,
            Fill = Brushes.Lime,
            Stroke = Brushes.White,
            StrokeThickness = strokeThickness
        };
        Canvas.SetLeft(marker, screenPt.X - markerSize / 2);
        Canvas.SetTop(marker, screenPt.Y - markerSize / 2);
        MainCanvas.Children.Add(marker);
        
        _measurePoints.Add(newPoint);
        UpdateMeasureDisplay();
    }
    
    private void UpdateMeasureDisplay()
    {
        if (_measurePoints.Count == 0)
        {
            TxtMeasurePoint1.Text = "-";
            TxtMeasurePoint2.Text = "-";
            TxtMeasureDistance.Text = "-";
            TxtMeasureCumulative.Text = "-";
            TxtMeasureArea.Text = "-";
            return;
        }
        
        // Point 1
        TxtMeasurePoint1.Text = $"E:{_measurePoints[0].X:F2} N:{_measurePoints[0].Y:F2}";
        
        // Point 2 (last point)
        if (_measurePoints.Count > 1)
        {
            var last = _measurePoints[^1];
            var prev = _measurePoints[^2];
            TxtMeasurePoint2.Text = $"E:{last.X:F2} N:{last.Y:F2}";
            
            // Last segment distance
            double lastDist = Math.Sqrt(
                Math.Pow(last.X - prev.X, 2) +
                Math.Pow(last.Y - prev.Y, 2));
            TxtMeasureDistance.Text = $"{lastDist:F3}";
        }
        else
        {
            TxtMeasurePoint2.Text = "-";
            TxtMeasureDistance.Text = "-";
        }
        
        // Cumulative
        TxtMeasureCumulative.Text = $"{_measureCumulative:F3}";
        
        // Area (if polygon is closed or has 3+ points)
        if (_measurePoints.Count >= 3)
        {
            double area = CalculatePolygonArea(_measurePoints, _measurePolygonClosed);
            TxtMeasureArea.Text = $"{Math.Abs(area):F2} sq units";
        }
        else
        {
            TxtMeasureArea.Text = "-";
        }
    }
    
    private double CalculatePolygonArea(List<Point> points, bool isClosed)
    {
        if (points.Count < 3) return 0;
        
        double area = 0;
        int n = points.Count;
        
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }
        
        return area / 2.0;
    }
    
    #endregion
    
    #region Insert Blocks
    
    private void BlockSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtBlockSize != null)
        {
            _blockSize = SliderBlockSize.Value;
            TxtBlockSize.Text = $"{_blockSize:F0}";
        }
    }
    
    private void ChooseBlockColor_Click(object sender, RoutedEventArgs e)
    {
        var colorDialog = new System.Windows.Forms.ColorDialog();
        colorDialog.Color = System.Drawing.Color.FromArgb(_blockColor.A, _blockColor.R, _blockColor.G, _blockColor.B);
        
        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _blockColor = Color.FromArgb(colorDialog.Color.A, colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
            BlockColorPreview.Background = new SolidColorBrush(_blockColor);
        }
    }
    
    private void ClearBlocks_Click(object sender, RoutedEventArgs e)
    {
        foreach (var block in _insertedBlocks)
        {
            MainCanvas.Children.Remove(block);
        }
        _insertedBlocks.Clear();
        TxtBlocksPlaced.Text = "Blocks Placed: 0";
    }
    
    private void BlockText_Checked(object sender, RoutedEventArgs e)
    {
        if (TextLabelOptions != null)
            TextLabelOptions.Visibility = Visibility.Visible;
    }
    
    private void BlockText_Unchecked(object sender, RoutedEventArgs e)
    {
        if (TextLabelOptions != null)
            TextLabelOptions.Visibility = Visibility.Collapsed;
    }
    
    private void InsertBlockAt(double worldX, double worldY)
    {
        var snapped = ApplySnap(worldX, worldY);
        var screenPt = WorldToScreen(snapped.X, snapped.Y);
        
        // Get zoom-responsive block size (divide by zoom to keep apparent size constant)
        double zoomLevel = _zoomLevel > 0 ? _zoomLevel : 1.0;
        double blockSize = _blockSize / zoomLevel;
        double strokeThickness = 1.0 / zoomLevel;
        double crossStrokeThickness = 2.0 / zoomLevel;
        
        UIElement? block = null;
        var brush = new SolidColorBrush(_blockColor);
        
        if (BlockCircle.IsChecked == true)
        {
            block = new Ellipse
            {
                Width = blockSize,
                Height = blockSize,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = strokeThickness
            };
            Canvas.SetLeft(block, screenPt.X - blockSize / 2);
            Canvas.SetTop(block, screenPt.Y - blockSize / 2);
        }
        else if (BlockSquare.IsChecked == true)
        {
            block = new Rectangle
            {
                Width = blockSize,
                Height = blockSize,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = strokeThickness
            };
            Canvas.SetLeft(block, screenPt.X - blockSize / 2);
            Canvas.SetTop(block, screenPt.Y - blockSize / 2);
        }
        else if (BlockCross.IsChecked == true)
        {
            var crossPath = new Path
            {
                Stroke = brush,
                StrokeThickness = crossStrokeThickness,
                Data = new GeometryGroup
                {
                    Children = new GeometryCollection
                    {
                        new LineGeometry(new Point(-blockSize/2, 0), new Point(blockSize/2, 0)),
                        new LineGeometry(new Point(0, -blockSize/2), new Point(0, blockSize/2))
                    }
                }
            };
            Canvas.SetLeft(crossPath, screenPt.X);
            Canvas.SetTop(crossPath, screenPt.Y);
            block = crossPath;
        }
        else if (BlockTriangle.IsChecked == true)
        {
            var triangle = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(0, -blockSize/2),
                    new Point(-blockSize/2, blockSize/2),
                    new Point(blockSize/2, blockSize/2)
                },
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = strokeThickness
            };
            Canvas.SetLeft(triangle, screenPt.X);
            Canvas.SetTop(triangle, screenPt.Y);
            block = triangle;
        }
        else if (BlockText.IsChecked == true)
        {
            var textBlock = new TextBlock
            {
                Text = TxtBlockLabel?.Text ?? "Label",
                Foreground = brush,
                FontSize = _blockSize,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(textBlock, screenPt.X);
            Canvas.SetTop(textBlock, screenPt.Y - _blockSize / 2);
            block = textBlock;
        }
        
        if (block != null)
        {
            MainCanvas.Children.Add(block);
            _insertedBlocks.Add(block);
            TxtBlocksPlaced.Text = $"Blocks Placed: {_insertedBlocks.Count}";
        }
    }
    
    #endregion
    
    #region Layer Properties
    
    private void Layer_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorLayer.IsVisible) ||
            e.PropertyName == nameof(EditorLayer.Color) ||
            e.PropertyName == nameof(EditorLayer.Thickness) ||
            e.PropertyName == nameof(EditorLayer.PointSize) ||
            e.PropertyName == nameof(EditorLayer.Opacity))
        {
            RedrawCanvas();
        }
    }
    
    private void LayerList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedLayer = LayerList.SelectedItem as EditorLayer;
        
        // Enable/disable delete button based on selection
        BtnDeleteLayer.IsEnabled = _selectedLayer?.IsUserCreated == true;
        
        // Update layer properties panel
        UpdateLayerPropertiesPanel();
    }
    
    private void UpdateLayerPropertiesPanel()
    {
        _isLoadingProperties = true;
        
        if (_selectedLayer == null)
        {
            TxtSelectedLayerName.Text = "No layer selected";
            LayerColorPreview.Background = Brushes.Gray;
            SliderLayerThickness.Value = 1.5;
            SliderLayerPointSize.Value = 6;
            SliderLayerOpacity.Value = 1;
        }
        else
        {
            TxtSelectedLayerName.Text = _selectedLayer.Name;
            LayerColorPreview.Background = new SolidColorBrush(_selectedLayer.Color);
            SliderLayerThickness.Value = _selectedLayer.Thickness;
            SliderLayerPointSize.Value = _selectedLayer.PointSize;
            SliderLayerOpacity.Value = _selectedLayer.Opacity;
            
            TxtLayerThickness.Text = $"{_selectedLayer.Thickness:F1}";
            TxtLayerPointSize.Text = $"{_selectedLayer.PointSize:F0}";
            TxtLayerOpacity.Text = $"{_selectedLayer.Opacity * 100:F0}%";
        }
        
        _isLoadingProperties = false;
    }
    
    private void ChooseLayerColor_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedLayer == null) return;
        
        var colorDialog = new System.Windows.Forms.ColorDialog();
        colorDialog.Color = System.Drawing.Color.FromArgb(
            _selectedLayer.Color.A, _selectedLayer.Color.R, 
            _selectedLayer.Color.G, _selectedLayer.Color.B);
        colorDialog.FullOpen = true;
        
        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = Color.FromArgb(
                colorDialog.Color.A, colorDialog.Color.R, 
                colorDialog.Color.G, colorDialog.Color.B);
            _selectedLayer.Color = newColor;
            LayerColorPreview.Background = new SolidColorBrush(newColor);
            RedrawCanvas();
        }
    }
    
    private void LayerThickness_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoadingProperties || _selectedLayer == null) return;
        
        _selectedLayer.Thickness = SliderLayerThickness.Value;
        TxtLayerThickness.Text = $"{SliderLayerThickness.Value:F1}";
        RedrawCanvas();
    }
    
    private void LayerPointSize_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoadingProperties || _selectedLayer == null) return;
        
        _selectedLayer.PointSize = SliderLayerPointSize.Value;
        TxtLayerPointSize.Text = $"{SliderLayerPointSize.Value:F0}";
        RedrawCanvas();
    }
    
    private void LayerOpacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoadingProperties || _selectedLayer == null) return;
        
        _selectedLayer.Opacity = SliderLayerOpacity.Value;
        TxtLayerOpacity.Text = $"{SliderLayerOpacity.Value * 100:F0}%";
        RedrawCanvas();
    }
    
    private void LayerColor_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is EditorLayer layer)
        {
            // Simple color picker - cycle through preset colors
            var colors = new[] { Colors.Red, Colors.Lime, Colors.Cyan, Colors.Yellow, 
                               Colors.Orange, Colors.Magenta, Colors.White, Colors.DimGray,
                               Colors.Pink, Colors.LightBlue, Colors.LightGreen, Colors.Gold };
            
            int currentIndex = Array.IndexOf(colors, layer.Color);
            int nextIndex = (currentIndex + 1) % colors.Length;
            layer.Color = colors[nextIndex];
        }
    }
    
    private void DuplicateLayer_Click(object sender, RoutedEventArgs e)
    {
        var selectedLayer = LayerList.SelectedItem as EditorLayer;
        if (selectedLayer == null)
        {
            MessageBox.Show("Please select a layer to duplicate.", "Duplicate Layer", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        // Create duplicate layer with copied data
        string newName = $"{selectedLayer.Name} (Copy)";
        var duplicateLayer = new EditorLayer(EditorLayerType.Custom, newName, GetNextLayerColor())
        {
            Thickness = selectedLayer.Thickness,
            PointSize = selectedLayer.PointSize,
            Opacity = selectedLayer.Opacity,
            IsUserCreated = true,
            ZIndex = selectedLayer.ZIndex + 1,
            IsClosed = selectedLayer.IsClosed
        };
        
        // Copy points
        duplicateLayer.Points = new ObservableCollection<EditablePoint>();
        
        // Get source points based on layer type
        IEnumerable<EditablePoint> sourcePoints;
        
        // Handle Survey Raw layer and its sub-layers (Points/Lines)
        if (selectedLayer.LayerType == EditorLayerType.SurveyRaw ||
            selectedLayer.ParentLayerName == "Survey Raw")
        {
            // For Survey Raw, use original coordinates
            sourcePoints = _editablePoints.Select(p => new EditablePoint
            {
                X = p.OriginalX,
                Y = p.OriginalY,
                Z = p.OriginalZ,
                OriginalX = p.OriginalX,
                OriginalY = p.OriginalY,
                OriginalZ = p.OriginalZ,
                OriginalIndex = p.OriginalIndex,
                SourcePoint = p.SourcePoint,
                Kp = p.Kp,
                Dcc = p.Dcc
            });
        }
        // Handle Survey Smoothed layer and its sub-layers
        else if (selectedLayer.LayerType == EditorLayerType.SurveySmoothed ||
                 selectedLayer.ParentLayerName?.Contains("Smoothed") == true)
        {
            // For Survey Smoothed, use current (smoothed) coordinates
            sourcePoints = _editablePoints.Select(p => new EditablePoint
            {
                X = p.X,
                Y = p.Y,
                Z = p.Z,
                OriginalX = p.X,
                OriginalY = p.Y,
                OriginalZ = p.Z,
                OriginalIndex = p.OriginalIndex,
                SourcePoint = p.SourcePoint,
                Kp = p.Kp,
                Dcc = p.Dcc
            });
        }
        else if (selectedLayer.Points != null && selectedLayer.Points.Count > 0)
        {
            // For any other layer with points (including polylines), copy those points
            sourcePoints = selectedLayer.Points;
        }
        // Handle Grid and FieldLayout - no points to duplicate
        else if (selectedLayer.LayerType == EditorLayerType.Grid ||
                 selectedLayer.LayerType == EditorLayerType.FieldLayout)
        {
            MessageBox.Show("Grid and Field Layout layers cannot be duplicated.", "Duplicate Layer", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        // Handle Route layer
        else if (selectedLayer.LayerType == EditorLayerType.Route)
        {
            // Duplicate route data
            if (_routeData != null && _routeData.Segments != null)
            {
                sourcePoints = _routeData.Segments.Select((s, i) => new EditablePoint
                {
                    X = s.StartEasting,
                    Y = s.StartNorthing,
                    Z = 0,
                    OriginalX = s.StartEasting,
                    OriginalY = s.StartNorthing,
                    OriginalZ = 0,
                    Kp = s.StartKp,
                    OriginalIndex = i
                });
            }
            else
            {
                MessageBox.Show("Route layer has no data to duplicate.", "Duplicate Layer", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
        else
        {
            MessageBox.Show("Selected layer has no points to duplicate.", "Duplicate Layer", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        foreach (var pt in sourcePoints)
        {
            duplicateLayer.Points.Add(new EditablePoint
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
                ProcessedDepth = pt.ProcessedDepth,
                ProcessedAltitude = pt.ProcessedAltitude,
                LayerName = newName,
                IsModified = false
            });
        }
        
        // Subscribe to changes
        duplicateLayer.PropertyChanged += Layer_PropertyChanged;
        
        // Add to layers
        Layers.Add(duplicateLayer);
        RedrawCanvas();
        
        TxtStatus.Text = $"Layer duplicated: {newName} ({duplicateLayer.Points.Count} points)";
    }
    
    private void DeleteLayer_Click(object sender, RoutedEventArgs e)
    {
        var selectedLayer = LayerList.SelectedItem as EditorLayer;
        if (selectedLayer == null)
        {
            MessageBox.Show("Please select a layer to delete.", "Delete Layer", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        // Don't allow deleting core data layers (Raw, Smoothed, Route, Grid, Field Layout)
        if (selectedLayer.LayerType == EditorLayerType.SurveyRaw ||
            selectedLayer.LayerType == EditorLayerType.SurveySmoothed ||
            selectedLayer.LayerType == EditorLayerType.Route ||
            selectedLayer.LayerType == EditorLayerType.Grid ||
            selectedLayer.LayerType == EditorLayerType.FieldLayout)
        {
            // Check if it's a sub-layer (Points/Lines) - those can be hidden but not deleted
            if (selectedLayer.ParentLayerName != null)
            {
                MessageBox.Show("Sub-layers (Points/Lines) cannot be deleted. Use visibility toggle instead.", 
                    "Delete Layer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Core data layers cannot be deleted. Use visibility toggle to hide them.", 
                    "Delete Layer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return;
        }
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete the layer '{selectedLayer.Name}'?",
            "Delete Layer", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            selectedLayer.PropertyChanged -= Layer_PropertyChanged;
            Layers.Remove(selectedLayer);
            TxtStatus.Text = $"Deleted layer '{selectedLayer.Name}'";
            RedrawCanvas();
        }
    }
    
    private void ChangeLayerColor_Click(object sender, RoutedEventArgs e)
    {
        var selectedLayer = LayerList.SelectedItem as EditorLayer;
        if (selectedLayer == null) return;
        
        // Cycle to next color
        var colors = new[] { Colors.Red, Colors.Lime, Colors.Cyan, Colors.Yellow, 
                           Colors.Orange, Colors.Magenta, Colors.White, Colors.DimGray,
                           Colors.Pink, Colors.LightBlue, Colors.LightGreen, Colors.Gold,
                           Colors.Salmon, Colors.Turquoise, Colors.Violet, Colors.Coral };
        
        int currentIndex = Array.IndexOf(colors, selectedLayer.Color);
        int nextIndex = (currentIndex + 1) % colors.Length;
        selectedLayer.Color = colors[nextIndex];
    }
    
    private void RenameLayer_Click(object sender, RoutedEventArgs e)
    {
        var selectedLayer = LayerList.SelectedItem as EditorLayer;
        if (selectedLayer == null) return;
        
        RenameLayerDialog(selectedLayer);
    }
    
    /// <summary>
    /// Handle double-click on layer name to rename
    /// </summary>
    private void LayerName_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) // Double-click
        {
            if (sender is TextBlock textBlock && textBlock.Tag is EditorLayer layer)
            {
                e.Handled = true;
                RenameLayerDialog(layer);
            }
        }
    }
    
    /// <summary>
    /// Show rename dialog for a layer
    /// </summary>
    private void RenameLayerDialog(EditorLayer layer)
    {
        var dialog = new RenameDialog(layer.Name);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.NewName))
        {
            layer.Name = dialog.NewName;
            TxtStatus.Text = $"Layer renamed to '{dialog.NewName}'";
        }
    }
    
    private static int _layerColorIndex = 0;
    private static Color GetNextLayerColor()
    {
        var colors = new[] { Colors.Magenta, Colors.Orange, Colors.Pink, Colors.LightBlue,
                            Colors.LightGreen, Colors.Salmon, Colors.Khaki, Colors.Plum,
                            Colors.Coral, Colors.Turquoise, Colors.Gold, Colors.Violet };
        var color = colors[_layerColorIndex % colors.Length];
        _layerColorIndex++;
        return color;
    }
    
    private void ShowKpLabels_Click(object sender, RoutedEventArgs e)
    {
        RedrawCanvas();
    }
    
    private void KpInterval_Changed(object sender, TextChangedEventArgs e)
    {
        if (ChkShowKpLabels.IsChecked == true)
        {
            RedrawCanvas();
        }
    }
    
    #endregion

    #region Point Properties
    
    private void PointProperty_Changed(object sender, TextChangedEventArgs e)
    {
        if (_selectedPoints.Count != 1) return;
        
        var pt = _selectedPoints[0];
        
        if (double.TryParse(TxtPointEasting.Text, out double easting))
            pt.X = easting;
        if (double.TryParse(TxtPointNorthing.Text, out double northing))
            pt.Y = northing;
        if (double.TryParse(TxtPointDepth.Text, out double depth))
            pt.Z = depth;
        
        pt.IsModified = true;
        
        // Delayed redraw
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, 
            new Action(RedrawCanvas));
    }
    
    #endregion

    #region Apply Changes
    
    private void ApplyChanges_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Apply all changes and sync to project
            ApplyChangesToSource();
            
            // Update the survey points with current edits (from SaveToProject)
            foreach (var editPt in _editablePoints.Where(p => p.SourcePoint != null && !p.IsDeleted))
            {
                var source = editPt.SourcePoint!;
                
                // Update position if modified
                if (editPt.IsModified)
                {
                    source.SmoothedEasting = editPt.X;
                    source.SmoothedNorthing = editPt.Y;
                }
                
                // Store processed depth/altitude
                if (editPt.ProcessedDepth.HasValue)
                {
                    source.CalculatedZ = editPt.ProcessedDepth;
                }
            }
            
            // Mark deleted points as excluded
            foreach (var editPt in _editablePoints.Where(p => p.IsDeleted && p.SourcePoint != null))
            {
                editPt.SourcePoint!.IsExcluded = true;
            }
            
            MessageBox.Show("All changes applied and synced to Step 6.\n\nSmoothing, spline, interval data, and point edits have been saved.", 
                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
            TxtStatus.Text = "Changes applied and saved to project";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error applying changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ApplyChangesToSource()
    {
        // Apply editable point changes back to survey points
        for (int i = 0; i < _editablePoints.Count && i < _surveyPoints.Count; i++)
        {
            _surveyPoints[i].SmoothedEasting = _editablePoints[i].X;
            _surveyPoints[i].SmoothedNorthing = _editablePoints[i].Y;
        }
        
        // If we have a direct reference to Step 6's list, update it directly
        if (_sourcePointsList != null)
        {
            for (int i = 0; i < _editablePoints.Count && i < _sourcePointsList.Count; i++)
            {
                _sourcePointsList[i].SmoothedEasting = _editablePoints[i].X;
                _sourcePointsList[i].SmoothedNorthing = _editablePoints[i].Y;
            }
        }
        
        // Collect all editor data for comprehensive sync
        var syncData = CollectEditorSyncData();
        
        // Use comprehensive sync callback if available
        if (_onSyncDataApplied != null)
        {
            _onSyncDataApplied(syncData);
        }
        // Fall back to simple callback
        else if (_onChangesApplied != null)
        {
            _onChangesApplied(_surveyPoints);
        }
        
        // Mark all as not modified
        foreach (var pt in _editablePoints)
        {
            pt.OriginalX = pt.X;
            pt.OriginalY = pt.Y;
            pt.IsModified = false;
        }
        
        UpdateStats();
    }
    
    /// <summary>
    /// Collect all data from Editor for syncing back to Step 6
    /// </summary>
    private EditorSyncData CollectEditorSyncData()
    {
        var syncData = new EditorSyncData
        {
            SurveyPoints = _surveyPoints.ToList(),
            DeletedPointIndices = _deletedPointIndices.ToList()
        };
        
        // Collect spline points from Spline Fit layer (or stored editor spline points)
        var splineLayer = Layers.FirstOrDefault(l => l.Name == "Spline Fit" && l.Points != null);
        if (splineLayer?.Points != null && splineLayer.Points.Count > 0)
        {
            syncData.SplinePoints = splineLayer.Points.Select(p => new SurveyPoint
            {
                Easting = p.X,
                Northing = p.Y,
                SmoothedEasting = p.X,
                SmoothedNorthing = p.Y,
                Depth = p.Z,
                CorrectedDepth = p.Z
            }).ToList();
        }
        else if (_editorSplinePoints.Count > 0)
        {
            syncData.SplinePoints = _editorSplinePoints.ToList();
        }
        
        // Collect interval points from Interval Points layer (or stored editor interval points)
        var intervalLayer = Layers.FirstOrDefault(l => l.Name == "Interval Points" && l.Points != null);
        if (intervalLayer?.Points != null && intervalLayer.Points.Count > 0)
        {
            syncData.IntervalPoints = intervalLayer.Points.Select((p, i) => (p.X, p.Y, p.Z, i * 10.0)).ToList();
        }
        else if (_editorIntervalPoints.Count > 0)
        {
            syncData.IntervalPoints = _editorIntervalPoints.ToList();
        }
        
        // Collect interval points from _measureIntervalPoints if available
        if (_measureIntervalPoints.Count > 0 && syncData.IntervalPoints.Count == 0)
        {
            // Convert EditablePoint to tuple format with distance
            double totalDistance = 0;
            syncData.IntervalPoints = new List<(double X, double Y, double Z, double Distance)>();
            
            for (int i = 0; i < _measureIntervalPoints.Count; i++)
            {
                var pt = _measureIntervalPoints[i];
                if (i > 0)
                {
                    var prevPt = _measureIntervalPoints[i - 1];
                    totalDistance += Math.Sqrt(
                        Math.Pow(pt.X - prevPt.X, 2) + 
                        Math.Pow(pt.Y - prevPt.Y, 2));
                }
                syncData.IntervalPoints.Add((pt.X, pt.Y, pt.Z, totalDistance));
            }
        }
        
        // Collect added/digitized points
        if (_digitizedPoints.Count > 0)
        {
            syncData.AddedPoints = _digitizedPoints.Select(p => new SurveyPoint
            {
                Easting = p.X,
                Northing = p.Y,
                SmoothedEasting = p.X,
                SmoothedNorthing = p.Y,
                Depth = p.Z,
                CorrectedDepth = p.Z
            }).ToList();
        }
        
        return syncData;
    }
    
    private void UpdateStats()
    {
        int modified = _editablePoints.Count(p => p.IsModified);
        TxtModifiedCount.Text = modified.ToString();
    }
    
    #endregion

    #region Charts
    
    private void ShowCharts_Click(object sender, RoutedEventArgs e)
    {
        var chartWindow = new ChartWindow();
        chartWindow.LoadSurveyData(_surveyPoints, _routeData);
        chartWindow.Show();
    }
    
    private void ShowHelp_Click(object sender, RoutedEventArgs e)
    {
        ShowKeyboardShortcutsHelp();
    }
    
    #endregion
    
    #region 3D Viewer
    
    private void Show3DView_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var viewer = new Viewer3DWindow();
            
            // Load all editor layers dynamically
            viewer.LoadEditorLayers(Layers, _editablePoints.ToList());
            
            // Load route if available
            if (_routeData != null)
            {
                viewer.LoadRoute(_routeData);
            }
            
            // Load field layout if available
            if (_fieldLayout != null)
            {
                viewer.LoadFieldLayout(_fieldLayout);
            }
            
            // Add measure interval points if any
            if (_measureIntervalPoints.Count > 0)
            {
                viewer.AddCustomLayer("Interval Points", Colors.Magenta, 
                    _measureIntervalPoints.Select(p => (p.X, p.Y, p.Z)).ToList(), false);
            }
            
            // Add spline points if any
            if (_editorSplinePoints.Count > 0)
            {
                viewer.AddCustomLayer("Spline Fitted", Colors.Orange, 
                    _editorSplinePoints.Select(p => (p.Easting, p.Northing, p.Depth ?? 0.0)).ToList(), true);
            }
            
            // Add digitized points if any
            if (_digitizedPoints.Count > 0)
            {
                viewer.AddCustomLayer("Digitized Points", Colors.Yellow, 
                    _digitizedPoints.Select(p => (p.X, p.Y, p.Z)).ToList(), false);
            }
            
            viewer.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening 3D viewer: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    #region Point Management
    
    private void AddPoint_Click(object sender, RoutedEventArgs e)
    {
        // Switch to digitize mode
        ToolDigitize.IsChecked = true;
        Tool_Click(ToolDigitize, new RoutedEventArgs());
        
        TxtStatus.Text = "Click on canvas to add new points";
    }
    
    #endregion
    
    #region MEASURE Interval Tool (AutoCAD-style)
    
    /// <summary>
    /// Generate points at specified intervals along the survey track
    /// Similar to AutoCAD's MEASURE command - supports both distance and KP modes
    /// </summary>
    private void GenerateMeasureIntervalPoints_Click(object sender, RoutedEventArgs e)
    {
        // Check which mode is selected
        if (RdoMeasureDistance != null && RdoMeasureDistance.IsChecked == true)
        {
            GenerateMeasureIntervalPoints_Distance();
            return;
        }
        
        // KP mode (original logic)
        if (_editablePoints.Count == 0)
        {
            TxtMeasureIntervalStatus.Text = "Error: No survey data loaded";
            return;
        }
        
        // Parse interval distance
        if (!double.TryParse(TxtMeasureIntervalDistance.Text, out double interval) || interval <= 0)
        {
            TxtMeasureIntervalStatus.Text = "Error: Invalid interval distance";
            return;
        }
        
        // Parse start and end KP
        if (!double.TryParse(TxtMeasureStartKp.Text, out double startKp))
            startKp = 0;
            
        double endKp = double.MaxValue;
        if (!string.IsNullOrWhiteSpace(TxtMeasureEndKp.Text))
        {
            if (!double.TryParse(TxtMeasureEndKp.Text, out endKp))
            {
                TxtMeasureIntervalStatus.Text = "Error: Invalid End KP";
                return;
            }
        }
        
        // Get points sorted by KP
        var pointsWithKp = _editablePoints
            .Where(p => p.Kp.HasValue && p.Kp.Value >= startKp && p.Kp.Value <= endKp)
            .OrderBy(p => p.Kp!.Value)
            .ToList();
        
        if (pointsWithKp.Count < 2)
        {
            TxtMeasureIntervalStatus.Text = "Error: Not enough points with KP values in range";
            return;
        }
        
        // Clear previous interval points
        _measureIntervalPoints.Clear();
        _editorIntervalPoints.Clear(); // Also store in sync list
        
        // Generate points at intervals
        double currentKp = Math.Ceiling(startKp / interval) * interval; // Start at next interval boundary
        double distance = 0; // Track cumulative distance
        EditablePoint? lastPoint = null;
        
        while (currentKp <= endKp)
        {
            // Find position by interpolating between nearest KP points
            var before = pointsWithKp.LastOrDefault(p => p.Kp!.Value <= currentKp);
            var after = pointsWithKp.FirstOrDefault(p => p.Kp!.Value >= currentKp);
            
            if (before != null && after != null && before != after)
            {
                // Interpolate position
                double kpRange = after.Kp!.Value - before.Kp!.Value;
                double t = (currentKp - before.Kp!.Value) / kpRange;
                
                double x = before.X + t * (after.X - before.X);
                double y = before.Y + t * (after.Y - before.Y);
                double z = before.Z + t * (after.Z - before.Z);
                
                var newPoint = new EditablePoint
                {
                    X = x,
                    Y = y,
                    Z = z,
                    OriginalX = x,
                    OriginalY = y,
                    OriginalZ = z,
                    Kp = currentKp,
                    IsModified = false
                };
                _measureIntervalPoints.Add(newPoint);
                
                // Calculate cumulative distance for sync
                if (lastPoint != null)
                {
                    distance += Math.Sqrt(Math.Pow(x - lastPoint.X, 2) + Math.Pow(y - lastPoint.Y, 2));
                }
                _editorIntervalPoints.Add((x, y, z, distance));
                lastPoint = newPoint;
            }
            else if (before != null && Math.Abs(before.Kp!.Value - currentKp) < 0.0001)
            {
                // Exact match
                var newPoint = new EditablePoint
                {
                    X = before.X,
                    Y = before.Y,
                    Z = before.Z,
                    OriginalX = before.X,
                    OriginalY = before.Y,
                    OriginalZ = before.Z,
                    Kp = currentKp,
                    IsModified = false
                };
                _measureIntervalPoints.Add(newPoint);
                
                // Calculate cumulative distance for sync
                if (lastPoint != null)
                {
                    distance += Math.Sqrt(Math.Pow(before.X - lastPoint.X, 2) + Math.Pow(before.Y - lastPoint.Y, 2));
                }
                _editorIntervalPoints.Add((before.X, before.Y, before.Z, distance));
                lastPoint = newPoint;
            }
            
            currentKp += interval;
        }
        
        TxtMeasureIntervalStatus.Text = $"Generated {_measureIntervalPoints.Count} points at {interval} KP intervals (will sync on Apply)";
        
        // Redraw to show the points
        RedrawCanvas();
        DrawMeasureIntervalPoints();
    }
    
    private void DrawMeasureIntervalPoints()
    {
        if (_measureIntervalPoints.Count == 0) return;
        
        double dotSize = GetZoomResponsivePointSize();
        double markerSize = Math.Max(4.0, dotSize * 1.5);  // Slightly larger than dots
        var brush = new SolidColorBrush(Colors.Magenta);
        
        foreach (var pt in _measureIntervalPoints)
        {
            var screenPt = WorldToScreen(pt.X, pt.Y);
            
            // Draw diamond marker for interval points
            var points = new PointCollection
            {
                new Point(screenPt.X, screenPt.Y - markerSize),
                new Point(screenPt.X + markerSize, screenPt.Y),
                new Point(screenPt.X, screenPt.Y + markerSize),
                new Point(screenPt.X - markerSize, screenPt.Y)
            };
            
            var diamond = new Polygon
            {
                Points = points,
                Fill = brush,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            
            MainCanvas.Children.Add(diamond);
            
            // Draw KP label
            if (pt.Kp.HasValue)
            {
                var label = new TextBlock
                {
                    Text = $"KP {pt.Kp.Value:F3}",
                    Foreground = Brushes.Magenta,
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    FontSize = 9,
                    Padding = new Thickness(2, 1, 2, 1)
                };
                
                Canvas.SetLeft(label, screenPt.X + markerSize + 2);
                Canvas.SetTop(label, screenPt.Y - 6);
                MainCanvas.Children.Add(label);
            }
        }
    }
    
    private void ClearMeasureIntervalPoints_Click(object sender, RoutedEventArgs e)
    {
        _measureIntervalPoints.Clear();
        TxtMeasureIntervalStatus.Text = "Interval points cleared";
        RedrawCanvas();
    }
    
    private void AddMeasureIntervalToLayer_Click(object sender, RoutedEventArgs e)
    {
        if (_measureIntervalPoints.Count == 0)
        {
            TxtMeasureIntervalStatus.Text = "No interval points to add. Generate points first.";
            return;
        }
        
        // Parse interval for layer name
        double.TryParse(TxtMeasureIntervalDistance.Text, out double interval);
        
        // Create a new layer for the interval points - use PointsOnly type so no lines are drawn
        var newLayer = new EditorLayer(EditorLayerType.PointsOnly, $"Interval Points ({interval} KP)", Colors.Magenta)
        {
            IsVisible = true,
            IsLocked = false,
            PointSize = 4,  // Same as normal points
            Thickness = 1,
            Opacity = 1.0,
            ZIndex = Layers.Count,
            IsUserCreated = true,
            Points = new ObservableCollection<EditablePoint>(),
            IsClosed = false  // Explicitly set - no closed polygon
        };
        
        // Copy interval points to the layer
        foreach (var pt in _measureIntervalPoints)
        {
            newLayer.Points.Add(new EditablePoint
            {
                X = pt.X,
                Y = pt.Y,
                Z = pt.Z,
                OriginalX = pt.OriginalX,
                OriginalY = pt.OriginalY,
                OriginalZ = pt.OriginalZ,
                Kp = pt.Kp,
                IsModified = false
            });
        }
        
        // Subscribe to changes
        newLayer.PropertyChanged += Layer_PropertyChanged;
        
        // Add to layers
        Layers.Add(newLayer);
        
        // Clear temporary interval points
        _measureIntervalPoints.Clear();
        
        TxtMeasureIntervalStatus.Text = $"Added {newLayer.Points!.Count} points to layer '{newLayer.Name}'";
        RedrawCanvas();
    }
    
    #endregion
    
    #region Profile View
    
    private bool _isProfileVisible = false;
    private double _profileZoomLevel = 1.0;
    private double _profileExaggeration = 10.0;
    private bool _profileUseKp = true; // true = KP, false = DAL
    private bool _isProfilePanning = false;
    private Point _lastProfileMousePos;
    
    private void ToggleProfileView_Click(object sender, RoutedEventArgs e)
    {
        _isProfileVisible = BtnToggleProfile.IsChecked == true;
        
        if (_isProfileVisible)
        {
            ProfileRow.Height = new GridLength(1, GridUnitType.Star);
            ProfileSplitter.Visibility = Visibility.Visible;
            ProfileViewGrid.Visibility = Visibility.Visible;
            RedrawProfileCanvas();
        }
        else
        {
            ProfileRow.Height = new GridLength(0);
            ProfileSplitter.Visibility = Visibility.Collapsed;
            ProfileViewGrid.Visibility = Visibility.Collapsed;
        }
    }
    
    private void ProfileXAxis_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CboProfileXAxis == null) return;
        _profileUseKp = CboProfileXAxis.SelectedIndex == 0;
        RedrawProfileCanvas();
    }
    
    private void ProfileExaggeration_Changed(object sender, TextChangedEventArgs e)
    {
        if (TxtProfileExaggeration == null) return;
        if (double.TryParse(TxtProfileExaggeration.Text, out double exag) && exag > 0)
        {
            _profileExaggeration = exag;
            RedrawProfileCanvas();
        }
    }
    
    private void RedrawProfileCanvas()
    {
        if (ProfileCanvas == null || !_isProfileVisible) return;
        if (_editablePoints.Count == 0) return;
        
        ProfileCanvas.Children.Clear();
        
        // Calculate DAL for all points if not already done
        CalculateDistanceAlongLine();
        
        // Get X values (KP or DAL) and Y values (depth)
        var pointsForProfile = _editablePoints
            .Where(p => _profileUseKp ? p.Kp.HasValue : p.Dal.HasValue)
            .OrderBy(p => _profileUseKp ? p.Kp!.Value : p.Dal!.Value)
            .ToList();
        
        if (pointsForProfile.Count == 0) return;
        
        // Calculate bounds
        double minX = pointsForProfile.Min(p => _profileUseKp ? p.Kp!.Value : p.Dal!.Value);
        double maxX = pointsForProfile.Max(p => _profileUseKp ? p.Kp!.Value : p.Dal!.Value);
        double minZ = pointsForProfile.Min(p => p.Z);
        double maxZ = pointsForProfile.Max(p => p.Z);
        
        // Apply exaggeration to depth
        double dataWidth = maxX - minX;
        double dataHeight = (maxZ - minZ) * _profileExaggeration;
        
        if (dataWidth < 0.001) dataWidth = 1;
        if (dataHeight < 0.001) dataHeight = 1;
        
        double canvasW = ProfileCanvas.ActualWidth;
        double canvasH = ProfileCanvas.ActualHeight;
        double margin = 50;
        
        double scaleX = (canvasW - 2 * margin) / dataWidth;
        double scaleY = (canvasH - 2 * margin) / dataHeight;
        double scale = Math.Min(scaleX, scaleY) * _profileZoomLevel;
        
        Func<double, double, Point> transform = (x, z) =>
        {
            double screenX = margin + (x - minX) * scale + ProfileCanvasTranslate.X;
            double screenY = margin + (z - minZ) * _profileExaggeration * scale + ProfileCanvasTranslate.Y;
            return new Point(screenX, screenY);
        };
        
        // Draw profile line with gap detection
        var brush = new SolidColorBrush(Colors.Cyan);
        double gapThreshold = CalculateGapThreshold(pointsForProfile.Select(p => (
            _profileUseKp ? p.Kp!.Value : p.Dal!.Value, 0.0, p.Z)).ToList()) * _profileExaggeration;
        
        for (int i = 0; i < pointsForProfile.Count - 1; i++)
        {
            double x1 = _profileUseKp ? pointsForProfile[i].Kp!.Value : pointsForProfile[i].Dal!.Value;
            double x2 = _profileUseKp ? pointsForProfile[i + 1].Kp!.Value : pointsForProfile[i + 1].Dal!.Value;
            
            // Gap detection for profile
            if (Math.Abs(x2 - x1) > gapThreshold / _profileExaggeration) continue;
            
            var p1 = transform(x1, pointsForProfile[i].Z);
            var p2 = transform(x2, pointsForProfile[i + 1].Z);
            
            ProfileCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = 1.5
            });
        }
        
        // Draw points
        double pointSize = 4 * _profileZoomLevel;
        foreach (var pt in pointsForProfile)
        {
            double x = _profileUseKp ? pt.Kp!.Value : pt.Dal!.Value;
            var screenPt = transform(x, pt.Z);
            
            var ellipse = new Ellipse
            {
                Width = pointSize,
                Height = pointSize,
                Fill = pt.IsSelected ? Brushes.Yellow : brush
            };
            
            Canvas.SetLeft(ellipse, screenPt.X - pointSize / 2);
            Canvas.SetTop(ellipse, screenPt.Y - pointSize / 2);
            ProfileCanvas.Children.Add(ellipse);
        }
        
        TxtProfileZoom.Text = $"Zoom: {_profileZoomLevel * 100:F0}%";
    }
    
    private void CalculateDistanceAlongLine()
    {
        if (_editablePoints.Count == 0) return;
        
        _editablePoints[0].Dal = 0;
        double cumulative = 0;
        
        for (int i = 1; i < _editablePoints.Count; i++)
        {
            double dx = _editablePoints[i].X - _editablePoints[i - 1].X;
            double dy = _editablePoints[i].Y - _editablePoints[i - 1].Y;
            double dz = _editablePoints[i].Z - _editablePoints[i - 1].Z;
            cumulative += Math.Sqrt(dx * dx + dy * dy + dz * dz);
            _editablePoints[i].Dal = cumulative;
        }
        
        // Update track length display
        if (_editablePoints.Count > 0)
        {
            var lastPoint = _editablePoints.LastOrDefault();
            if (lastPoint != null && lastPoint.Dal.HasValue)
            {
                TxtTrackLength.Text = $"{lastPoint.Dal.Value:F2} m";
            }
        }
    }
    
    private void ProfileCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        double factor = e.Delta > 0 ? 1.15 : 1 / 1.15;
        _profileZoomLevel *= factor;
        _profileZoomLevel = Math.Max(0.1, Math.Min(50, _profileZoomLevel));
        RedrawProfileCanvas();
    }
    
    private void ProfileCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastProfileMousePos = e.GetPosition(ProfileCanvas);
        _isProfilePanning = true;
        ProfileCanvas.CaptureMouse();
    }
    
    private void ProfileCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isProfilePanning = false;
        ProfileCanvas.ReleaseMouseCapture();
    }
    
    private void ProfileCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastProfileMousePos = e.GetPosition(ProfileCanvas);
        _isProfilePanning = true;
        ProfileCanvas.CaptureMouse();
    }
    
    private void ProfileCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isProfilePanning = false;
        ProfileCanvas.ReleaseMouseCapture();
    }
    
    private void ProfileCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isProfilePanning)
        {
            var pos = e.GetPosition(ProfileCanvas);
            ProfileCanvasTranslate.X += pos.X - _lastProfileMousePos.X;
            ProfileCanvasTranslate.Y += pos.Y - _lastProfileMousePos.Y;
            _lastProfileMousePos = pos;
            RedrawProfileCanvas();
        }
    }
    
    #endregion
    
    #region CAD Export
    
    private void ExportCadScript_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CadExportDialog();
        dialog.Owner = this;
        
        // Add ALL layers (not just visible ones) - let user choose in dialog
        foreach (var layer in Layers)
        {
            List<(double X, double Y, double Z)> points = new();
            bool isPolyline = true;
            
            if (layer.LayerType == EditorLayerType.SurveyRaw)
            {
                points = _editablePoints.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList();
                isPolyline = true;
            }
            else if (layer.LayerType == EditorLayerType.SurveySmoothed)
            {
                points = _editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList();
                isPolyline = true;
            }
            else if (layer.LayerType == EditorLayerType.PointsOnly)
            {
                // Points-only sub-layer - check if it has its own points
                if (layer.Points != null && layer.Points.Count > 0)
                {
                    points = layer.Points.Select(p => (p.X, p.Y, p.Z)).ToList();
                }
                else if (layer.ParentLayerName?.Contains("Smoothed") == true)
                {
                    points = _editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList();
                }
                else
                {
                    points = _editablePoints.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList();
                }
                isPolyline = false;  // Export as individual points
            }
            else if (layer.LayerType == EditorLayerType.LinesOnly)
            {
                // Lines-only sub-layer - check if it has its own points
                if (layer.Points != null && layer.Points.Count > 0)
                {
                    points = layer.Points.Select(p => (p.X, p.Y, p.Z)).ToList();
                }
                else if (layer.ParentLayerName?.Contains("Smoothed") == true)
                {
                    points = _editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList();
                }
                else
                {
                    points = _editablePoints.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList();
                }
                isPolyline = true;  // Export as polyline
            }
            else if (layer.LayerType == EditorLayerType.Digitized)
            {
                points = _digitizedPoints.Select(p => (p.X, p.Y, p.Z)).ToList();
                isPolyline = false;
            }
            else if (layer.LayerType == EditorLayerType.Custom && layer.Points != null)
            {
                points = layer.Points.Select(p => (p.X, p.Y, p.Z)).ToList();
                isPolyline = true;  // Custom layers default to polyline
            }
            
            if (points.Count > 0)
            {
                dialog.AddLayer(layer.Name, layer.Color, points, isPolyline);
            }
        }
        
        // Add measure interval points if any (temporary, not yet added as layer)
        if (_measureIntervalPoints.Count > 0)
        {
            dialog.AddLayer("MEASURE Interval Points", Colors.Magenta, 
                _measureIntervalPoints.Select(p => (p.X, p.Y, p.Z)).ToList(), false);
        }
        
        // Add editor spline points if any (not yet in a layer)
        if (_editorSplinePoints.Count > 0)
        {
            // Check if already added as a layer
            bool hasSplineLayer = Layers.Any(l => l.Name.Contains("Spline"));
            if (!hasSplineLayer)
            {
                dialog.AddLayer("Spline Fitted", Colors.Orange, 
                    _editorSplinePoints.Select(p => (p.Easting, p.Northing, p.Depth ?? 0.0)).ToList(), true);
            }
        }
        
        // Add editor interval points if any (not yet in a layer)
        if (_editorIntervalPoints.Count > 0)
        {
            bool hasIntervalLayer = Layers.Any(l => l.Name.Contains("Interval"));
            if (!hasIntervalLayer)
            {
                dialog.AddLayer("Interval Points", Colors.Yellow, 
                    _editorIntervalPoints.Select(p => (p.X, p.Y, p.Z)).ToList(), false);
            }
        }
        
        // Add route data if available
        if (_routeData?.Segments.Count > 0)
        {
            var routePoints = new List<(double X, double Y, double Z)>();
            foreach (var seg in _routeData.Segments)
            {
                routePoints.Add((seg.StartEasting, seg.StartNorthing, 0));
            }
            if (_routeData.Segments.Count > 0)
            {
                var lastSeg = _routeData.Segments[^1];
                routePoints.Add((lastSeg.EndEasting, lastSeg.EndNorthing, 0));
            }
            dialog.AddLayer("Route Line", Colors.Red, routePoints, true);
        }
        
        // Add field layout entities if available
        if (_fieldLayout != null)
        {
            // Add lines from field layout
            if (_fieldLayout.Lines.Count > 0)
            {
                var layoutLines = new List<(double X, double Y, double Z)>();
                foreach (var line in _fieldLayout.Lines)
                {
                    layoutLines.Add((line.StartX, line.StartY, 0));
                    layoutLines.Add((line.EndX, line.EndY, 0));
                }
                if (layoutLines.Count > 0)
                {
                    dialog.AddLayer("Field Layout - Lines", Colors.Gray, layoutLines, false);
                }
            }
            
            // Add polylines from field layout
            if (_fieldLayout.Polylines.Count > 0)
            {
                foreach (var poly in _fieldLayout.Polylines)
                {
                    var polyPoints = poly.Vertices.Select(v => (v.X, v.Y, v.Z)).ToList();
                    var layerName = string.IsNullOrEmpty(poly.Layer) ? "Polyline" : poly.Layer;
                    dialog.AddLayer($"Field Layout - {layerName}", Colors.DarkCyan, polyPoints, true);
                }
            }
        }
        
        // Add digitized points as separate layer if any
        if (_digitizedPoints.Count > 0)
        {
            bool hasDigitizedLayer = Layers.Any(l => l.LayerType == EditorLayerType.Digitized);
            if (!hasDigitizedLayer)
            {
                dialog.AddLayer("Digitized Points", Colors.Lime, 
                    _digitizedPoints.Select(p => (p.X, p.Y, p.Z)).ToList(), false);
            }
        }
        
        dialog.ShowDialog();
        
        if (dialog.DialogResultOk && !string.IsNullOrEmpty(dialog.ExportedFilePath))
        {
            TxtStatus.Text = $"CAD script exported to {System.IO.Path.GetFileName(dialog.ExportedFilePath)}";
        }
    }
    
    #endregion
    
    #region Spline Fitting
    
    private List<(double X, double Y, double Z)>? _splinePreviewPoints;
    
    private void ExpandSpline_Click(object sender, RoutedEventArgs e)
    {
        SplineFittingPanel.Visibility = BtnExpandSpline.IsChecked == true 
            ? Visibility.Visible 
            : Visibility.Collapsed;
        
        if (BtnExpandSpline.IsChecked == true)
        {
            // Populate source layer dropdown with ALL available layers
            CboSplineSourceLayer.Items.Clear();
            
            // Add survey layers
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.SurveySmoothed || 
                                                     l.LayerType == EditorLayerType.SurveyRaw))
            {
                CboSplineSourceLayer.Items.Add(layer.Name);
            }
            
            // Add Points sub-layers
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.PointsOnly))
            {
                CboSplineSourceLayer.Items.Add(layer.Name);
            }
            
            // Add Lines sub-layers
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.LinesOnly))
            {
                CboSplineSourceLayer.Items.Add(layer.Name);
            }
            
            // Add custom layers with points
            foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.Custom && l.Points?.Count > 0))
            {
                CboSplineSourceLayer.Items.Add(layer.Name);
            }
            
            if (CboSplineSourceLayer.Items.Count > 0)
                CboSplineSourceLayer.SelectedIndex = 0;
        }
    }
    
    private void PreviewSpline_Click(object sender, RoutedEventArgs e)
    {
        _ = GenerateSplineAsync(preview: true);
    }
    
    private void ApplySpline_Click(object sender, RoutedEventArgs e)
    {
        _ = GenerateSplineAsync(preview: false);
    }
    
    private void ClearSpline_Click(object sender, RoutedEventArgs e)
    {
        _splinePreviewPoints = null;
        RedrawCanvas();
    }
    
    private async Task GenerateSplineAsync(bool preview)
    {
        if (_editablePoints.Count < 2)
        {
            MessageBox.Show("Not enough points to generate spline.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Get algorithm
        var selectedItem = CboSplineAlgorithm.SelectedItem as ComboBoxItem;
        var algorithmTag = selectedItem?.Tag?.ToString() ?? "CatmullRom";
        
        var algorithm = algorithmTag switch
        {
            "CubicSpline" => SplineAlgorithm.CubicSpline,
            "BSpline" => SplineAlgorithm.BSpline,
            "AutoCadPedit" => SplineAlgorithm.AutoCadPedit,
            _ => SplineAlgorithm.CatmullRom
        };
        
        // Get parameters
        double tension = SliderSplineTension.Value;
        double multiplier = SliderSplineOutputMultiplier?.Value ?? 2.0;
        int outputPoints = (int)(_editablePoints.Count * multiplier); // Calculate from multiplier
        
        // Prepare input points
        var inputPoints = _editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList();
        
        // Show processing indicator
        TxtStatus.Text = "Processing spline... Please wait.";
        IsEnabled = false; // Disable window while processing
        Mouse.OverrideCursor = Cursors.Wait;
        
        List<(double X, double Y, double Z)> splinePoints = new();
        
        try
        {
            // Run heavy computation on background thread
            await Task.Run(() =>
            {
                var splineService = new SplineService();
                splinePoints = splineService.GenerateSpline(inputPoints, algorithm, tension, outputPoints);
            });
            
            if (preview)
            {
                _splinePreviewPoints = splinePoints;
                RedrawCanvas();
                DrawSplinePreview();
                TxtStatus.Text = $"Preview: {splinePoints.Count} spline points generated";
            }
            else
            {
                // Store spline points for sync back to Step 6
                _editorSplinePoints = splinePoints.Select(pt => new SurveyPoint
                {
                    Easting = pt.X,
                    Northing = pt.Y,
                    SmoothedEasting = pt.X,
                    SmoothedNorthing = pt.Y,
                    Depth = pt.Z,
                    CorrectedDepth = pt.Z
                }).ToList();
                
                // Create main layer (lines) for spline
                var splineLineLayer = new EditorLayer(EditorLayerType.LinesOnly, "Spline Fit (Lines)", Colors.Orange)
                {
                    IsVisible = true,
                    IsLocked = false,
                    PointSize = 4,
                    Thickness = 2,
                    Opacity = 1.0,
                    ZIndex = Layers.Count,
                    IsUserCreated = true,
                    ParentLayerName = "Spline Fit",
                    Points = new ObservableCollection<EditablePoint>()
                };
                
                foreach (var pt in splinePoints)
                {
                    splineLineLayer.Points.Add(new EditablePoint
                    {
                        X = pt.X,
                        Y = pt.Y,
                        Z = pt.Z,
                        OriginalX = pt.X,
                        OriginalY = pt.Y,
                        OriginalZ = pt.Z,
                        IsModified = false
                    });
                }
                
                splineLineLayer.PropertyChanged += Layer_PropertyChanged;
                Layers.Add(splineLineLayer);
                
                // Create sub-layer (points only) for spline
                var splinePointLayer = new EditorLayer(EditorLayerType.PointsOnly, "Spline Fit (Points)", Colors.Orange)
                {
                    IsVisible = true,
                    IsLocked = false,
                    PointSize = 4,
                    Thickness = 1,
                    Opacity = 1.0,
                    ZIndex = Layers.Count,
                    IsUserCreated = true,
                    ParentLayerName = "Spline Fit",
                    Points = new ObservableCollection<EditablePoint>()
                };
                
                foreach (var pt in splinePoints)
                {
                    splinePointLayer.Points.Add(new EditablePoint
                    {
                        X = pt.X,
                        Y = pt.Y,
                        Z = pt.Z,
                        OriginalX = pt.X,
                        OriginalY = pt.Y,
                        OriginalZ = pt.Z,
                        IsModified = false
                    });
                }
                
                splinePointLayer.PropertyChanged += Layer_PropertyChanged;
                Layers.Add(splinePointLayer);
                
                _splinePreviewPoints = null;
                TxtStatus.Text = $"Created spline layers with {splinePoints.Count} points (will sync on Apply)";
                RedrawCanvas();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating spline: {ex.Message}", "Spline Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Spline generation failed";
        }
        finally
        {
            // Re-enable UI
            IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
    }
    
    private void DrawSplinePreview()
    {
        if (_splinePreviewPoints == null || _splinePreviewPoints.Count < 2) return;
        
        var brush = new SolidColorBrush(Colors.Orange) { Opacity = 0.8 };
        
        // Performance: Use LOD for large splines
        var pointsToDraw = _splinePreviewPoints;
        if (_splinePreviewPoints.Count > 5000)
        {
            int stride = _splinePreviewPoints.Count / 5000 + 1;
            pointsToDraw = _splinePreviewPoints.Where((p, i) => i % stride == 0).ToList();
        }
        
        // Calculate gap threshold
        double gapThreshold = CalculateGapThreshold(pointsToDraw);
        
        // Use PathGeometry for efficient rendering with gap detection
        var geometry = new PathGeometry();
        PathFigure? currentFigure = null;
        
        for (int i = 0; i < pointsToDraw.Count; i++)
        {
            var p = pointsToDraw[i];
            var screenPt = WorldToScreen(p.X, p.Y);
            
            if (i == 0)
            {
                currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
            }
            else
            {
                var prevP = pointsToDraw[i - 1];
                double dx = p.X - prevP.X;
                double dy = p.Y - prevP.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                // Gap detection
                if (distance > gapThreshold)
                {
                    if (currentFigure != null && currentFigure.Segments.Count > 0)
                    {
                        geometry.Figures.Add(currentFigure);
                    }
                    currentFigure = new PathFigure { StartPoint = screenPt, IsClosed = false };
                }
                else
                {
                    currentFigure?.Segments.Add(new LineSegment(screenPt, true));
                }
            }
        }
        
        if (currentFigure != null && currentFigure.Segments.Count > 0)
        {
            geometry.Figures.Add(currentFigure);
        }
        
        if (geometry.Figures.Count > 0)
        {
            MainCanvas.Children.Add(new Path
            {
                Data = geometry,
                Stroke = brush,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 }
            });
        }
    }
    
    #endregion
    
    #region MEASURE Mode Handlers
    
    private void MeasureMode_Changed(object sender, RoutedEventArgs e)
    {
        // Guard against null during initialization
        if (DistanceModePanel == null || KpModePanel == null || CboMeasureSourceLayer == null)
            return;
            
        bool isDistanceMode = RdoMeasureDistance?.IsChecked == true;
        DistanceModePanel.Visibility = isDistanceMode ? Visibility.Visible : Visibility.Collapsed;
        KpModePanel.Visibility = isDistanceMode ? Visibility.Collapsed : Visibility.Visible;
        
        // Populate source layer dropdown
        CboMeasureSourceLayer.Items.Clear();
        foreach (var layer in Layers.Where(l => l.LayerType == EditorLayerType.SurveySmoothed || 
                                                 l.LayerType == EditorLayerType.SurveyRaw ||
                                                 (l.LayerType == EditorLayerType.Custom && l.Points?.Count > 0)))
        {
            CboMeasureSourceLayer.Items.Add(layer.Name);
        }
        if (CboMeasureSourceLayer.Items.Count > 0)
            CboMeasureSourceLayer.SelectedIndex = 0;
    }
    
    private void GenerateMeasureIntervalPoints_Distance()
    {
        // Get source layer points
        var sourcePoints = GetSourceLayerPoints();
        if (sourcePoints.Count < 2)
        {
            TxtMeasureIntervalStatus.Text = "Error: Source layer has insufficient points";
            return;
        }
        
        // Parse distance parameters
        if (!double.TryParse(TxtDistanceInterval.Text, out double interval) || interval <= 0)
        {
            TxtMeasureIntervalStatus.Text = "Error: Invalid interval distance";
            return;
        }
        
        double.TryParse(TxtDistanceStart.Text, out double startDist);
        double? endDist = null;
        if (!string.IsNullOrWhiteSpace(TxtDistanceEnd.Text) && double.TryParse(TxtDistanceEnd.Text, out double ed))
        {
            endDist = ed;
        }
        
        // Use DistanceCalculator to generate points
        var distCalc = new DistanceCalculator();
        var results = distCalc.GeneratePointsAtDistanceIntervals(sourcePoints, interval, startDist, endDist);
        
        // Clear and populate interval points
        _measureIntervalPoints.Clear();
        _editorIntervalPoints.Clear(); // Also store in sync list
        
        foreach (var pt in results)
        {
            _measureIntervalPoints.Add(new EditablePoint
            {
                X = pt.X,
                Y = pt.Y,
                Z = pt.Z,
                OriginalX = pt.X,
                OriginalY = pt.Y,
                OriginalZ = pt.Z,
                Dal = pt.DAL,
                IsModified = false
            });
            
            // Store for sync back to Step 6
            _editorIntervalPoints.Add((pt.X, pt.Y, pt.Z, pt.DAL));
        }
        
        TxtMeasureIntervalStatus.Text = $"Generated {_measureIntervalPoints.Count} points at {interval}m intervals (will sync on Apply)";
        RedrawCanvas();
        DrawMeasureIntervalPoints();
    }
    
    private List<(double X, double Y, double Z)> GetSourceLayerPoints()
    {
        var layerName = CboMeasureSourceLayer.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(layerName)) return new List<(double, double, double)>();
        
        // Special case: Spline Fitted (not a layer, but stored separately)
        if (layerName == "Spline Fitted" && _editorSplinePoints.Count > 0)
        {
            return _editorSplinePoints.Select(p => (p.Easting, p.Northing, p.Depth ?? 0.0)).ToList();
        }
        
        var layer = Layers.FirstOrDefault(l => l.Name == layerName);
        if (layer == null) return new List<(double, double, double)>();
        
        if (layer.LayerType == EditorLayerType.SurveySmoothed)
        {
            return _editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList();
        }
        else if (layer.LayerType == EditorLayerType.SurveyRaw)
        {
            return _editablePoints.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList();
        }
        else if (layer.LayerType == EditorLayerType.PointsOnly)
        {
            // Points sub-layer
            if (layer.ParentLayerName?.Contains("Smoothed") == true)
            {
                return _editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList();
            }
            else
            {
                return _editablePoints.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList();
            }
        }
        else if (layer.LayerType == EditorLayerType.LinesOnly)
        {
            // Lines sub-layer
            if (layer.ParentLayerName?.Contains("Smoothed") == true)
            {
                return _editablePoints.Select(p => (p.X, p.Y, p.Z)).ToList();
            }
            else
            {
                return _editablePoints.Select(p => (p.OriginalX, p.OriginalY, p.OriginalZ)).ToList();
            }
        }
        else if (layer.Points != null && layer.Points.Count > 0)
        {
            return layer.Points.Select(p => (p.X, p.Y, p.Z)).ToList();
        }
        
        return new List<(double, double, double)>();
    }
    
    #endregion
    
    #region Additional Handlers
    
    // Panel visibility state
    private double _leftPanelWidth = 250;
    private double _rightPanelWidth = 300;
    private bool _leftPanelVisible = true;
    private bool _rightPanelVisible = true;
    
    private void ToggleLeftPanel_Click(object sender, RoutedEventArgs e)
    {
        _leftPanelVisible = !_leftPanelVisible;
        
        if (_leftPanelVisible)
        {
            LeftPanelColumn.Width = new GridLength(_leftPanelWidth);
            LeftPanelColumn.MinWidth = 200;
            LeftPanel.Visibility = Visibility.Visible;
            BtnToggleLeftPanel.Content = "◀";
        }
        else
        {
            _leftPanelWidth = LeftPanelColumn.Width.Value;
            LeftPanelColumn.Width = new GridLength(0);
            LeftPanelColumn.MinWidth = 0;
            LeftPanel.Visibility = Visibility.Collapsed;
            BtnToggleLeftPanel.Content = "▶";
        }
    }
    
    private void ToggleRightPanel_Click(object sender, RoutedEventArgs e)
    {
        _rightPanelVisible = !_rightPanelVisible;
        
        if (_rightPanelVisible)
        {
            RightPanelColumn.Width = new GridLength(_rightPanelWidth);
            RightPanelColumn.MinWidth = 250;
            RightPanel.Visibility = Visibility.Visible;
            BtnToggleRightPanel.Content = "▶";
        }
        else
        {
            _rightPanelWidth = RightPanelColumn.Width.Value;
            RightPanelColumn.Width = new GridLength(0);
            RightPanelColumn.MinWidth = 0;
            RightPanel.Visibility = Visibility.Collapsed;
            BtnToggleRightPanel.Content = "◀";
        }
    }
    
    private void AddLayer_Click(object sender, RoutedEventArgs e)
    {
        // Show rename dialog to get layer name
        var defaultName = $"Custom Layer {Layers.Count + 1}";
        var dialog = new RenameDialog(defaultName, "New Layer", "Enter a name for the new layer:");
        dialog.Owner = this;
        
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.NewName))
        {
            // Pick a color from palette
            var colors = new[] { Colors.LimeGreen, Colors.Cyan, Colors.Yellow, Colors.Orange, 
                                 Colors.Magenta, Colors.DeepSkyBlue, Colors.Coral, Colors.MediumPurple };
            var layerColor = colors[Layers.Count % colors.Length];
            
            var newLayer = new EditorLayer(EditorLayerType.Custom, dialog.NewName, layerColor)
            {
                IsVisible = true,
                IsLocked = false,
                PointSize = 4,  // Same as normal points
                Thickness = 1.5,
                Opacity = 1.0,
                ZIndex = Layers.Count,
                IsUserCreated = true,
                IsSelectable = true,
                Points = new ObservableCollection<EditablePoint>()
            };
            
            newLayer.PropertyChanged += Layer_PropertyChanged;
            Layers.Add(newLayer);
            
            // Select the new layer
            LayerList.SelectedItem = newLayer;
            
            TxtStatus.Text = $"Created new layer '{newLayer.Name}' - click on canvas to add points to this layer";
        }
    }
    
    private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            MainCanvas.Cursor = Cursors.Arrow;
            MainCanvas.ReleaseMouseCapture();
        }
    }
    
    #endregion
    
    #region Polyline Creation
    
    private void AddPolylinePoint(double worldX, double worldY)
    {
        // Check if we're in "Pick from Point" mode and need to pick Z first
        var sourceTag = (CboZValueSource.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "ProcessedDepth";
        
        if (sourceTag == "PickFromPoint" && RbPolyline3D.IsChecked == true)
        {
            // Find the nearest point and pick its Z value
            var nearestPt = FindNearestPointForZ(worldX, worldY);
            if (nearestPt != null)
            {
                // Get the processed depth from the nearest point
                if (nearestPt.SourcePoint != null)
                {
                    var src = nearestPt.SourcePoint;
                    _pickedZValue = src.SmoothedDepth ?? src.CalculatedZ ?? src.CorrectedDepth ?? src.Depth ?? nearestPt.Z;
                }
                else
                {
                    _pickedZValue = nearestPt.ProcessedDepth ?? nearestPt.Z;
                }
                TxtPolylineStatus.Text = $"Picked Z: {_pickedZValue:F2}m - click to place points";
            }
        }
        
        // Get Z value based on selected source
        double z = GetZValueForPolyline(worldX, worldY);
        
        var point = new EditablePoint
        {
            X = worldX,
            Y = worldY,
            Z = z,
            OriginalX = worldX,
            OriginalY = worldY,
            OriginalZ = z,
            Index = _currentPolylinePoints.Count,
            LayerName = "Polyline",
            IsAdded = true,
            ProcessedDepth = z,
            ProcessedAltitude = z
        };
        
        _currentPolylinePoints.Add(point);
        _isDrawingPolyline = true;
        
        // Update UI
        bool is3D = RbPolyline3D.IsChecked == true;
        TxtPolylineStatus.Text = $"Points: {_currentPolylinePoints.Count}" + (is3D ? $" | Z: {z:F2}m" : "");
        BtnFinishPolyline.IsEnabled = _currentPolylinePoints.Count >= 2;
        BtnClosePolyline.IsEnabled = _currentPolylinePoints.Count >= 3;
        
        RedrawCanvas();
    }
    
    private double GetZValueForPolyline(double worldX, double worldY)
    {
        if (RbPolyline2D.IsChecked == true)
            return 0;
        
        var sourceTag = (CboZValueSource.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "ProcessedDepth";
        
        switch (sourceTag)
        {
            case "ProcessedDepth":
                // Find nearest point and get its LATEST processed depth
                // Priority: SmoothedDepth > CalculatedZ > CorrectedDepth > Depth
                var nearestForDepth = FindNearestPointForZ(worldX, worldY);
                if (nearestForDepth != null)
                {
                    // First check if we have stored processed depth
                    if (nearestForDepth.ProcessedDepth.HasValue)
                        return nearestForDepth.ProcessedDepth.Value;
                    
                    // Otherwise get from source point with priority
                    if (nearestForDepth.SourcePoint != null)
                    {
                        var src = nearestForDepth.SourcePoint;
                        // Use the latest processed value in order of priority
                        if (src.SmoothedDepth.HasValue)
                            return src.SmoothedDepth.Value;
                        if (src.CalculatedZ.HasValue)
                            return src.CalculatedZ.Value;
                        if (src.CorrectedDepth.HasValue)
                            return src.CorrectedDepth.Value;
                        if (src.Depth.HasValue)
                            return src.Depth.Value;
                    }
                    
                    return nearestForDepth.Z;
                }
                return 0;
                
            case "ProcessedAltitude":
                // Find nearest point and get its LATEST processed altitude
                // Priority: SmoothedAltitude > Altitude
                var nearestForAlt = FindNearestPointForZ(worldX, worldY);
                if (nearestForAlt != null)
                {
                    // First check if we have stored processed altitude
                    if (nearestForAlt.ProcessedAltitude.HasValue)
                        return nearestForAlt.ProcessedAltitude.Value;
                    
                    // Otherwise get from source point with priority
                    if (nearestForAlt.SourcePoint != null)
                    {
                        var src = nearestForAlt.SourcePoint;
                        if (src.SmoothedAltitude.HasValue)
                            return src.SmoothedAltitude.Value;
                        if (src.Altitude.HasValue)
                            return src.Altitude.Value;
                    }
                    
                    return 0;
                }
                return 0;
                
            case "PickFromPoint":
                // User will click on a point to sample Z - handled separately
                // For now return 0, actual pick happens on click
                return _pickedZValue;
                
            case "Manual":
                if (double.TryParse(TxtManualZ.Text, out double manualZ))
                    return manualZ;
                return 0;
                
            default:
                return 0;
        }
    }
    
    private EditablePoint? FindNearestPointForZ(double worldX, double worldY)
    {
        EditablePoint? nearest = null;
        double nearestDist = double.MaxValue;
        
        foreach (var pt in _editablePoints.Where(p => !p.IsDeleted))
        {
            double dx = pt.X - worldX;
            double dy = pt.Y - worldY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = pt;
            }
        }
        
        return nearest;
    }
    
    private void CreatePolylineFromSelection_Click(object sender, RoutedEventArgs e)
    {
        // Check for selected layer with points
        var selectedLayer = LayerList.SelectedItem as EditorLayer;
        bool hasSelectedLayer = selectedLayer?.Points?.Count >= 2;
        bool hasSelectedPoints = _selectedPoints.Count >= 2;
        
        if (!hasSelectedLayer && !hasSelectedPoints)
        {
            MessageBox.Show("Please either:\n\n" +
                "• Select a layer with at least 2 points, OR\n" +
                "• Select at least 2 points using the Select tool\n\n" +
                "Then click Polyline to create.", 
                "No Points Available", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Show polyline options dialog
        ShowPolylineOptionsDialog(hasSelectedPoints, hasSelectedLayer, selectedLayer);
    }
    
    private void ShowPolylineOptionsDialog(bool hasSelectedPoints, bool hasSelectedLayer, EditorLayer? selectedLayer)
    {
        var dialog = new Window
        {
            Title = "Create Polyline",
            Width = 350,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48))
        };
        
        var mainStack = new StackPanel { Margin = new Thickness(15) };
        
        // Source selection
        var sourceLabel = new TextBlock { Text = "Point Source:", Foreground = Brushes.White, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
        mainStack.Children.Add(sourceLabel);
        
        var rbSelectedPoints = new System.Windows.Controls.RadioButton 
        { 
            Content = $"Selected Points ({_selectedPoints.Count} points)", 
            Foreground = Brushes.White, 
            IsEnabled = hasSelectedPoints,
            IsChecked = hasSelectedPoints,
            Margin = new Thickness(0, 0, 0, 3)
        };
        mainStack.Children.Add(rbSelectedPoints);
        
        var rbSelectedLayer = new System.Windows.Controls.RadioButton 
        { 
            Content = hasSelectedLayer ? $"Selected Layer: {selectedLayer!.Name} ({selectedLayer.Points!.Count} points)" : "Selected Layer (none)", 
            Foreground = Brushes.White, 
            IsEnabled = hasSelectedLayer,
            IsChecked = hasSelectedLayer && !hasSelectedPoints,
            Margin = new Thickness(0, 0, 0, 10)
        };
        mainStack.Children.Add(rbSelectedLayer);
        
        // Polyline type
        var typeLabel = new TextBlock { Text = "Polyline Type:", Foreground = Brushes.White, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
        mainStack.Children.Add(typeLabel);
        
        var rb2D = new System.Windows.Controls.RadioButton { Content = "2D Polyline (X, Y only)", Foreground = Brushes.White, IsChecked = true, Margin = new Thickness(0, 0, 0, 3) };
        mainStack.Children.Add(rb2D);
        
        var rb3D = new System.Windows.Controls.RadioButton { Content = "3D Polyline (X, Y, Z)", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) };
        mainStack.Children.Add(rb3D);
        
        // Z value source (only for 3D)
        var zSourceLabel = new TextBlock { Text = "Z Value Source (3D only):", Foreground = Brushes.White, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) };
        mainStack.Children.Add(zSourceLabel);
        
        var rbProcessedDepth = new System.Windows.Controls.RadioButton { Content = "Processed Depth", Foreground = Brushes.White, IsChecked = true, Margin = new Thickness(0, 0, 0, 3) };
        mainStack.Children.Add(rbProcessedDepth);
        
        var rbAltitude = new System.Windows.Controls.RadioButton { Content = "Altitude", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 3) };
        mainStack.Children.Add(rbAltitude);
        
        var rbOriginalZ = new System.Windows.Controls.RadioButton { Content = "Original Z", Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 15) };
        mainStack.Children.Add(rbOriginalZ);
        
        // Buttons
        var buttonPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        
        var btnCreate = new System.Windows.Controls.Button 
        { 
            Content = "Create", 
            Width = 80, 
            Height = 28, 
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        
        var btnCancel = new System.Windows.Controls.Button 
        { 
            Content = "Cancel", 
            Width = 80, 
            Height = 28,
            Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        
        buttonPanel.Children.Add(btnCreate);
        buttonPanel.Children.Add(btnCancel);
        mainStack.Children.Add(buttonPanel);
        
        dialog.Content = mainStack;
        
        btnCancel.Click += (s, args) => dialog.DialogResult = false;
        btnCreate.Click += (s, args) =>
        {
            // Get source points
            List<EditablePoint> sourcePoints;
            if (rbSelectedPoints.IsChecked == true)
            {
                sourcePoints = _selectedPoints.OrderBy(p => p.OriginalIndex).ToList();
            }
            else
            {
                sourcePoints = selectedLayer!.Points!.ToList();
            }
            
            bool is3D = rb3D.IsChecked == true;
            
            // Determine Z source
            Func<EditablePoint, double> getZ;
            string zSource;
            if (rbProcessedDepth.IsChecked == true)
            {
                getZ = p => p.ProcessedDepth ?? p.Z;
                zSource = "Depth";
            }
            else if (rbAltitude.IsChecked == true)
            {
                getZ = p => p.ProcessedAltitude ?? 0;
                zSource = "Altitude";
            }
            else
            {
                getZ = p => p.OriginalZ;
                zSource = "OriginalZ";
            }
            
            // Create the polyline layer
            string layerName = $"Polyline #{Layers.Count(l => l.Name.StartsWith("Polyline")) + 1} ({(is3D ? "3D-" + zSource : "2D")})";
            
            var polylineLayer = new EditorLayer(EditorLayerType.Custom, layerName, Colors.Orange)
            {
                IsVisible = true,
                IsLocked = false,
                PointSize = 4,  // Same as normal points
                Thickness = 2.0,
                Opacity = 1.0,
                ZIndex = Layers.Count,
                IsUserCreated = true,
                Points = new ObservableCollection<EditablePoint>(),
                IsClosed = false
            };
            
            foreach (var pt in sourcePoints)
            {
                var newPoint = new EditablePoint
                {
                    X = pt.X,
                    Y = pt.Y,
                    Z = is3D ? getZ(pt) : 0,
                    OriginalX = pt.OriginalX,
                    OriginalY = pt.OriginalY,
                    OriginalZ = pt.OriginalZ,
                    Kp = pt.Kp,
                    OriginalIndex = pt.OriginalIndex,
                    LayerName = layerName,
                    ProcessedDepth = pt.ProcessedDepth,
                    ProcessedAltitude = pt.ProcessedAltitude
                };
                polylineLayer.Points.Add(newPoint);
            }
            
            polylineLayer.PropertyChanged += Layer_PropertyChanged;
            Layers.Add(polylineLayer);
            
            PushUndoAction(EditorUndoAction.CreatePolylineCreated(polylineLayer.Points.ToList(), layerName));
            ClearSelection();
            
            TxtStatus.Text = $"Created '{layerName}' with {polylineLayer.Points.Count} points";
            RedrawCanvas();
            
            dialog.DialogResult = true;
        };
        
        dialog.ShowDialog();
    }
    
    private void FinishPolyline_Click(object sender, RoutedEventArgs e)
    {
        FinishPolyline(false);
    }
    
    private void ClosePolyline_Click(object sender, RoutedEventArgs e)
    {
        FinishPolyline(true);
    }
    
    private void CancelPolyline_Click(object sender, RoutedEventArgs e)
    {
        CancelPolyline();
    }
    
    private void FinishPolyline(bool close)
    {
        if (_currentPolylinePoints.Count < 2)
        {
            MessageBox.Show("A polyline requires at least 2 points.", "Cannot Finish", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Create a new layer for the polyline
        bool is3D = RbPolyline3D.IsChecked == true;
        string layerName = is3D ? $"Polyline 3D #{Layers.Count(l => l.Name.StartsWith("Polyline")) + 1}" 
                                : $"Polyline 2D #{Layers.Count(l => l.Name.StartsWith("Polyline")) + 1}";
        
        var polylineLayer = new EditorLayer(EditorLayerType.Custom, layerName, Colors.Orange)
        {
            IsVisible = true,
            IsLocked = false,
            PointSize = 4,  // Same as normal points
            Thickness = 2.0,
            Opacity = 1.0,
            ZIndex = Layers.Count,
            IsUserCreated = true,
            Points = new ObservableCollection<EditablePoint>(_currentPolylinePoints),
            IsClosed = close
        };
        
        // Set layer name on all points
        foreach (var pt in polylineLayer.Points)
        {
            pt.LayerName = layerName;
        }
        
        polylineLayer.PropertyChanged += Layer_PropertyChanged;
        Layers.Add(polylineLayer);
        
        // Add to undo stack
        PushUndoAction(EditorUndoAction.CreatePolylineCreated(_currentPolylinePoints.ToList(), layerName));
        
        // Reset polyline state
        _currentPolylinePoints.Clear();
        _isDrawingPolyline = false;
        
        // Update UI
        TxtPolylineStatus.Text = $"Polyline '{layerName}' created with {polylineLayer.Points.Count} points";
        BtnFinishPolyline.IsEnabled = false;
        BtnClosePolyline.IsEnabled = false;
        
        RedrawCanvas();
        TxtStatus.Text = $"Created {layerName}";
    }
    
    private void CancelPolyline()
    {
        _currentPolylinePoints.Clear();
        _isDrawingPolyline = false;
        
        TxtPolylineStatus.Text = "Polyline cancelled. Click to start new polyline...";
        BtnFinishPolyline.IsEnabled = false;
        BtnClosePolyline.IsEnabled = false;
        
        RedrawCanvas();
    }
    
    private void CboZValueSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CboZValueSource == null || TxtManualZ == null) return;
        
        var tag = (CboZValueSource.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "ProcessedDepth";
        
        // Enable manual entry textbox only when Manual is selected
        TxtManualZ.IsEnabled = (tag == "Manual");
        
        // Reset picked Z value when changing mode
        if (tag != "PickFromPoint")
        {
            _pickedZValue = 0;
        }
        
        // Update status text based on selection
        if (_isDrawingPolyline)
        {
            switch (tag)
            {
                case "ProcessedDepth":
                    TxtPolylineStatus.Text = $"Points: {_currentPolylinePoints.Count} | Z from processed depth";
                    break;
                case "ProcessedAltitude":
                    TxtPolylineStatus.Text = $"Points: {_currentPolylinePoints.Count} | Z from processed altitude";
                    break;
                case "PickFromPoint":
                    TxtPolylineStatus.Text = $"Points: {_currentPolylinePoints.Count} | Click point to pick Z";
                    break;
                case "Manual":
                    TxtPolylineStatus.Text = $"Points: {_currentPolylinePoints.Count} | Z = {TxtManualZ.Text}m (manual)";
                    break;
            }
        }
        else
        {
            switch (tag)
            {
                case "ProcessedDepth":
                    TxtPolylineStatus.Text = "Click to start polyline (Z from Step 6 processed depth)";
                    break;
                case "ProcessedAltitude":
                    TxtPolylineStatus.Text = "Click to start polyline (Z from processed altitude)";
                    break;
                case "PickFromPoint":
                    TxtPolylineStatus.Text = "Click on a point to pick Z, then draw polyline";
                    break;
                case "Manual":
                    TxtPolylineStatus.Text = $"Click to start polyline (Z = {TxtManualZ.Text}m)";
                    break;
            }
        }
    }
    
    #endregion
    
    #region Undo/Redo System
    
    private void PushUndoAction(EditorUndoAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear(); // Clear redo stack when new action is performed
        
        // Limit undo stack size
        if (_undoStack.Count > MAX_UNDO_LEVELS)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < MAX_UNDO_LEVELS; i++)
            {
                _undoStack.Push(items[items.Length - 1 - i]);
            }
        }
        
        UpdateUndoRedoButtons();
    }
    
    private void UpdateUndoRedoButtons()
    {
        BtnUndo.IsEnabled = _undoStack.Count > 0;
        BtnRedo.IsEnabled = _redoStack.Count > 0;
        
        if (_undoStack.Count > 0)
            BtnUndo.ToolTip = $"Undo: {_undoStack.Peek().Description}";
        else
            BtnUndo.ToolTip = "Nothing to undo";
            
        if (_redoStack.Count > 0)
            BtnRedo.ToolTip = $"Redo: {_redoStack.Peek().Description}";
        else
            BtnRedo.ToolTip = "Nothing to redo";
    }
    
    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        PerformUndo();
    }
    
    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        PerformRedo();
    }
    
    private void PerformUndo()
    {
        if (_undoStack.Count == 0) return;
        
        var action = _undoStack.Pop();
        
        switch (action.ActionType)
        {
            case UndoActionType.PointsDeleted:
                // Restore deleted points
                if (action.PointSnapshots != null)
                {
                    foreach (var snapshot in action.PointSnapshots)
                    {
                        var point = _editablePoints.FirstOrDefault(p => p.Index == snapshot.Index || p.OriginalIndex == snapshot.Index);
                        if (point != null)
                        {
                            point.IsDeleted = false;
                            point.IsExcluded = false;
                        }
                        else
                        {
                            // Point was completely removed, need to re-add
                            var restored = snapshot.ToEditablePoint();
                            _editablePoints.Add(restored);
                        }
                    }
                }
                break;
                
            case UndoActionType.PointsAdded:
                // Remove added points
                if (action.PointSnapshots != null)
                {
                    foreach (var snapshot in action.PointSnapshots)
                    {
                        var point = _editablePoints.FirstOrDefault(p => p.Index == snapshot.Index);
                        if (point != null)
                        {
                            _editablePoints.Remove(point);
                        }
                        
                        var digitized = _digitizedPoints.FirstOrDefault(p => p.Index == snapshot.Index);
                        if (digitized != null)
                        {
                            _digitizedPoints.Remove(digitized);
                        }
                    }
                }
                break;
                
            case UndoActionType.PointsMoved:
                // Restore original positions
                if (action.PointSnapshots != null)
                {
                    foreach (var snapshot in action.PointSnapshots)
                    {
                        var point = _editablePoints.FirstOrDefault(p => p.Index == snapshot.Index || p.OriginalIndex == snapshot.Index);
                        if (point != null)
                        {
                            point.X = snapshot.X;
                            point.Y = snapshot.Y;
                            point.Z = snapshot.Z;
                        }
                    }
                }
                break;
                
            case UndoActionType.PolylineCreated:
                // Remove the polyline layer
                if (!string.IsNullOrEmpty(action.LayerName))
                {
                    var layer = Layers.FirstOrDefault(l => l.Name == action.LayerName);
                    if (layer != null)
                    {
                        Layers.Remove(layer);
                    }
                }
                break;
                
            case UndoActionType.LayerCreated:
                // Remove the layer
                if (!string.IsNullOrEmpty(action.LayerName))
                {
                    var layer = Layers.FirstOrDefault(l => l.Name == action.LayerName);
                    if (layer != null)
                    {
                        Layers.Remove(layer);
                    }
                }
                break;
                
            case UndoActionType.PointExcluded:
                // Un-exclude the point
                if (action.PointSnapshots != null && action.PointSnapshots.Count > 0)
                {
                    var snapshot = action.PointSnapshots[0];
                    // Find point in the layer and un-exclude it
                    var layer = Layers.FirstOrDefault(l => l.Name == action.LayerName);
                    if (layer?.Points != null)
                    {
                        var point = layer.Points.FirstOrDefault(p => p.Index == snapshot.Index);
                        if (point != null)
                        {
                            point.IsExcluded = false;
                        }
                    }
                }
                break;
        }
        
        // Push to redo stack
        _redoStack.Push(action);
        UpdateUndoRedoButtons();
        RedrawCanvas();
        TxtStatus.Text = $"Undone: {action.Description}";
    }
    
    private void PerformRedo()
    {
        if (_redoStack.Count == 0) return;
        
        var action = _redoStack.Pop();
        
        switch (action.ActionType)
        {
            case UndoActionType.PointsDeleted:
                // Re-delete points
                if (action.PointSnapshots != null)
                {
                    foreach (var snapshot in action.PointSnapshots)
                    {
                        var point = _editablePoints.FirstOrDefault(p => p.Index == snapshot.Index || p.OriginalIndex == snapshot.Index);
                        if (point != null)
                        {
                            point.IsDeleted = true;
                        }
                    }
                }
                break;
                
            case UndoActionType.PointsAdded:
                // Re-add points
                if (action.PointSnapshots != null)
                {
                    foreach (var snapshot in action.PointSnapshots)
                    {
                        var point = snapshot.ToEditablePoint();
                        _editablePoints.Add(point);
                        if (point.IsAdded)
                        {
                            _digitizedPoints.Add(point);
                        }
                    }
                }
                break;
                
            case UndoActionType.PointsMoved:
                // This is complex - we'd need to store the "after" positions too
                // For now, just note that redo for moves isn't fully supported
                break;
                
            case UndoActionType.PolylineCreated:
            case UndoActionType.LayerCreated:
                // Re-create the layer - we don't store full layer data, so this is limited
                break;
                
            case UndoActionType.PointExcluded:
                // Re-exclude the point
                if (action.PointSnapshots != null && action.PointSnapshots.Count > 0)
                {
                    var snapshot = action.PointSnapshots[0];
                    var layer = Layers.FirstOrDefault(l => l.Name == action.LayerName);
                    if (layer?.Points != null)
                    {
                        var point = layer.Points.FirstOrDefault(p => p.Index == snapshot.Index);
                        if (point != null)
                        {
                            point.IsExcluded = true;
                        }
                    }
                }
                break;
        }
        
        // Push back to undo stack
        _undoStack.Push(action);
        UpdateUndoRedoButtons();
        RedrawCanvas();
        TxtStatus.Text = $"Redone: {action.Description}";
    }
    
    #endregion
    
    #region Save to Project
    
    private void SaveToProject_Click(object sender, RoutedEventArgs e)
    {
        SaveToProject();
    }
    
    private void SaveToProject()
    {
        try
        {
            // Apply any pending changes first
            ApplyChangesToSource();
            
            // Update the survey points with current edits
            foreach (var editPt in _editablePoints.Where(p => p.SourcePoint != null && !p.IsDeleted))
            {
                var source = editPt.SourcePoint!;
                
                // Update position if modified
                if (editPt.IsModified)
                {
                    source.SmoothedEasting = editPt.X;
                    source.SmoothedNorthing = editPt.Y;
                }
                
                // Store processed depth/altitude
                if (editPt.ProcessedDepth.HasValue)
                {
                    source.CalculatedZ = editPt.ProcessedDepth;
                }
            }
            
            // Mark deleted points
            foreach (var editPt in _editablePoints.Where(p => p.IsDeleted && p.SourcePoint != null))
            {
                editPt.SourcePoint!.IsExcluded = true;
            }
            
            // Notify that data has been saved
            _onChangesApplied?.Invoke(_surveyPoints);
            
            MessageBox.Show("Changes saved to project. Step 6 data will be updated when you return.", 
                "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            
            TxtStatus.Text = "Changes saved to project";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving to project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    #region Create Smoothed Layer (On Demand)
    
    private void CreateSmoothedLayer_Click(object sender, RoutedEventArgs e)
    {
        if (!ChkSmoothPosition.IsChecked == true && !ChkSmoothDepth.IsChecked == true && !ChkSmoothAltitude.IsChecked == true)
        {
            MessageBox.Show("Please enable at least one smoothing option (Position, Depth, or Altitude).", 
                "No Smoothing Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            // Apply current smoothing settings and create a new layer
            var smoothedPoints = ApplySmoothingToPoints();
            
            if (smoothedPoints.Count == 0)
            {
                MessageBox.Show("No points to smooth.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Create smoothed layer with Points and Lines sub-layers
            string baseName = $"Smoothed {DateTime.Now:HH:mm:ss}";
            
            var smoothedParent = new EditorLayer(EditorLayerType.SurveySmoothed, baseName, Colors.Cyan)
            {
                IsVisible = false,
                ZIndex = Layers.Count
            };
            Layers.Add(smoothedParent);
            
            var (pointsLayer, linesLayer) = EditorLayer.CreateSubLayers(baseName, Colors.Cyan, Layers.Count);
            pointsLayer.Points = new ObservableCollection<EditablePoint>(smoothedPoints);
            linesLayer.Points = new ObservableCollection<EditablePoint>(smoothedPoints);
            pointsLayer.PointSize = 4;  // Same as normal points
            linesLayer.Thickness = 2.0;
            
            pointsLayer.PropertyChanged += Layer_PropertyChanged;
            linesLayer.PropertyChanged += Layer_PropertyChanged;
            Layers.Add(linesLayer);
            Layers.Add(pointsLayer);
            
            // Add to undo stack
            PushUndoAction(EditorUndoAction.CreateLayerCreated(baseName));
            
            RedrawCanvas();
            TxtStatus.Text = $"Created smoothed layer '{baseName}' with {smoothedPoints.Count} points";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating smoothed layer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private List<EditablePoint> ApplySmoothingToPoints()
    {
        var result = new List<EditablePoint>();
        var sourcePoints = _editablePoints.Where(p => !p.IsDeleted).OrderBy(p => p.OriginalIndex).ToList();
        
        if (sourcePoints.Count < 3) return result;
        
        // Get smoothing settings
        var posMethod = GetSmoothingMethod(CboPositionMethod);
        var depthMethod = GetSmoothingMethod(CboDepthMethod);
        var altMethod = GetSmoothingMethod(CboAltitudeMethod);
        
        int posWindow = (int)SliderPositionWindow.Value;
        int depthWindow = (int)SliderDepthWindow.Value;
        int altWindow = (int)SliderAltitudeWindow.Value;
        double threshold = SliderThreshold.Value;
        
        // Detect gaps in data to protect endpoints around gaps
        var gapIndices = DetectGaps(sourcePoints);
        var protectedIndices = new HashSet<int>();
        
        // Protect points near gaps
        foreach (var gapIndex in gapIndices)
        {
            for (int i = Math.Max(0, gapIndex - 3); i <= Math.Min(sourcePoints.Count - 1, gapIndex + 3); i++)
            {
                protectedIndices.Add(i);
            }
        }
        
        // Also protect first and last 3 points
        for (int i = 0; i < Math.Min(3, sourcePoints.Count); i++)
            protectedIndices.Add(i);
        for (int i = Math.Max(0, sourcePoints.Count - 3); i < sourcePoints.Count; i++)
            protectedIndices.Add(i);
        
        // Extract arrays for smoothing
        var xs = sourcePoints.Select(p => p.X).ToArray();
        var ys = sourcePoints.Select(p => p.Y).ToArray();
        var zs = sourcePoints.Select(p => p.Z).ToArray();
        
        // Apply smoothing
        double[] smoothedX = xs;
        double[] smoothedY = ys;
        double[] smoothedZ = zs;
        
        if (ChkSmoothPosition.IsChecked == true)
        {
            smoothedX = ApplySmoothingAlgorithm(xs, posMethod, posWindow, threshold);
            smoothedY = ApplySmoothingAlgorithm(ys, posMethod, posWindow, threshold);
        }
        
        if (ChkSmoothDepth.IsChecked == true)
        {
            smoothedZ = ApplySmoothingAlgorithm(zs, depthMethod, depthWindow, threshold);
        }
        
        // Restore protected points
        for (int i = 0; i < sourcePoints.Count; i++)
        {
            if (protectedIndices.Contains(i))
            {
                smoothedX[i] = xs[i];
                smoothedY[i] = ys[i];
                smoothedZ[i] = zs[i];
            }
        }
        
        // Create result points
        for (int i = 0; i < sourcePoints.Count; i++)
        {
            var source = sourcePoints[i];
            result.Add(new EditablePoint
            {
                X = smoothedX[i],
                Y = smoothedY[i],
                Z = smoothedZ[i],
                OriginalX = source.X,
                OriginalY = source.Y,
                OriginalZ = source.Z,
                OriginalIndex = source.OriginalIndex,
                Index = i,
                SourcePoint = source.SourcePoint,
                Kp = source.Kp,
                Dal = source.Dal,
                ProcessedDepth = smoothedZ[i],
                ProcessedAltitude = source.ProcessedAltitude,
                LayerName = "Smoothed"
            });
        }
        
        return result;
    }
    
    private List<int> DetectGaps(List<EditablePoint> points)
    {
        var gaps = new List<int>();
        if (points.Count < 2) return gaps;
        
        // Calculate average distance between consecutive points
        var distances = new List<double>();
        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i-1].X;
            double dy = points[i].Y - points[i-1].Y;
            distances.Add(Math.Sqrt(dx * dx + dy * dy));
        }
        
        double avgDist = distances.Average();
        double stdDev = Math.Sqrt(distances.Average(d => Math.Pow(d - avgDist, 2)));
        double threshold = avgDist + 3 * stdDev; // 3 sigma rule
        
        // Find gaps (distances significantly larger than average)
        for (int i = 0; i < distances.Count; i++)
        {
            if (distances[i] > threshold)
            {
                gaps.Add(i + 1); // Index of point after the gap
            }
        }
        
        return gaps;
    }
    
    private SmoothingMethod GetSmoothingMethod(System.Windows.Controls.ComboBox comboBox)
    {
        var tag = (comboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "MedianFilter";
        return tag switch
        {
            "MovingAverage" => SmoothingMethod.MovingAverage,
            "MedianFilter" => SmoothingMethod.MedianFilter,
            "ThresholdBased" => SmoothingMethod.ThresholdBased,
            "KalmanFilter" => SmoothingMethod.KalmanFilter,
            _ => SmoothingMethod.MedianFilter
        };
    }
    
    private double[] ApplySmoothingAlgorithm(double[] data, SmoothingMethod method, int windowSize, double threshold)
    {
        var result = new double[data.Length];
        int halfWindow = windowSize / 2;
        
        for (int i = 0; i < data.Length; i++)
        {
            int start = Math.Max(0, i - halfWindow);
            int end = Math.Min(data.Length - 1, i + halfWindow);
            
            switch (method)
            {
                case SmoothingMethod.MovingAverage:
                    double sum = 0;
                    int count = 0;
                    for (int j = start; j <= end; j++)
                    {
                        sum += data[j];
                        count++;
                    }
                    result[i] = sum / count;
                    break;
                    
                case SmoothingMethod.MedianFilter:
                    var window = new List<double>();
                    for (int j = start; j <= end; j++)
                        window.Add(data[j]);
                    window.Sort();
                    result[i] = window[window.Count / 2];
                    break;
                    
                case SmoothingMethod.ThresholdBased:
                    // Only modify if deviation from neighbors exceeds threshold
                    if (i > 0 && i < data.Length - 1)
                    {
                        double neighborAvg = (data[i - 1] + data[i + 1]) / 2;
                        double deviation = Math.Abs(data[i] - neighborAvg);
                        if (deviation > threshold)
                            result[i] = neighborAvg;
                        else
                            result[i] = data[i];
                    }
                    else
                    {
                        result[i] = data[i];
                    }
                    break;
                    
                case SmoothingMethod.KalmanFilter:
                    // Simple Kalman filter implementation
                    result[i] = data[i]; // Simplified - use full service for real Kalman
                    break;
                    
                default:
                    result[i] = data[i];
                    break;
            }
        }
        
        return result;
    }
    
    #endregion
}


