using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using FathomOS.Core.Models;
using FathomOS.Core.Parsers;
using FathomOS.Modules.SurveyListing.Models;
using Media3D = System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace FathomOS.Modules.SurveyListing.Views;

/// <summary>
/// 3D Survey Viewer using HelixToolkit.Wpf.SharpDX for DirectX 11 accelerated rendering.
/// Visualizes survey routes, survey points (2D plan), and seabed tracks (3D with depth).
/// </summary>
public partial class Viewer3DWindow : MetroWindow
{
    private DefaultEffectsManager? _effectsManager;
    
    // Line geometry models
    private LineGeometryModel3D? _gridModel;
    private LineGeometryModel3D? _routeModel;
    private LineGeometryModel3D? _survey2DModel;
    private LineGeometryModel3D? _seabed3DModel;
    private LineGeometryModel3D? _axesModel;  // Coordinate axes
    
    // Data storage
    private List<SurveyPoint>? _surveyPoints;
    private RouteData? _routeData;
    private FieldLayout? _fieldLayout;
    private List<EditorLayer>? _editorLayers;
    private List<EditablePoint>? _editablePoints;
    
    // Data bounds
    private double _minX, _maxX, _minY, _maxY, _minZ, _maxZ;
    private double _centerX, _centerY, _centerZ;
    private double _dataSize;
    
    // Current exaggeration
    private double _currentExaggeration = 10.0;
    private bool _isInitialized = false;
    
    public Viewer3DWindow()
    {
        InitializeComponent();
    }
    
    public Viewer3DWindow(List<EditorLayer> layers) : this()
    {
        _editorLayers = layers;
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Initialize DirectX effects manager
            _effectsManager = new DefaultEffectsManager();
            Viewport.EffectsManager = _effectsManager;
            
            // Create line geometry models with colors
            _gridModel = new LineGeometryModel3D
            {
                Color = Colors.Gray,
                Thickness = 0.5
            };
            
            _routeModel = new LineGeometryModel3D
            {
                Color = Colors.Red,
                Thickness = 3
            };
            
            _survey2DModel = new LineGeometryModel3D
            {
                Color = Colors.Lime,
                Thickness = 2
            };
            
            _seabed3DModel = new LineGeometryModel3D
            {
                Color = Colors.DodgerBlue,
                Thickness = 2
            };
            
            _axesModel = new LineGeometryModel3D
            {
                Color = Colors.White,
                Thickness = 3
            };
            
            // Add models to viewport
            Viewport.Items.Add(_gridModel);
            Viewport.Items.Add(_axesModel);
            Viewport.Items.Add(_routeModel);
            Viewport.Items.Add(_survey2DModel);
            Viewport.Items.Add(_seabed3DModel);
            
            _isInitialized = true;
            
            // Build initial geometry
            BuildAllGeometry();
            
            // Zoom to fit data
            ZoomToData();
            
            StatusText.Text = "3D Viewer initialized - Use mouse to rotate (left), pan (middle/right), zoom (scroll)";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error initializing 3D viewer: {ex.Message}";
        }
    }
    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Cleanup DirectX resources
        _effectsManager?.Dispose();
    }
    
    #region Public Data Loading Methods
    
    /// <summary>
    /// Load survey data into the 3D viewer
    /// </summary>
    public void LoadSurveyData(List<SurveyPoint> points)
    {
        _surveyPoints = points;
        if (_isInitialized)
        {
            BuildSurveyGeometry();
            BuildSeabedGeometry();
            UpdateDataBounds();
            ZoomToData();
        }
        DataInfo.Text = $"Points: {points.Count}";
    }
    
    /// <summary>
    /// Load route data into the 3D viewer
    /// </summary>
    public void LoadRouteData(RouteData? route)
    {
        _routeData = route;
        if (_isInitialized && route != null)
        {
            BuildRouteGeometry();
            UpdateDataBounds();
            ZoomToData();
        }
    }
    
    /// <summary>
    /// Load route data (alternative method name)
    /// </summary>
    public void LoadRoute(RouteData? route)
    {
        LoadRouteData(route);
    }
    
    /// <summary>
    /// Load field layout from FieldLayout object
    /// </summary>
    public void LoadFieldLayout(FieldLayout? layout)
    {
        _fieldLayout = layout;
        // Field layout can be added as additional geometry if needed
    }
    
    /// <summary>
    /// Load field layout from file path
    /// </summary>
    public void LoadFieldLayout(string? filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
        {
            try
            {
                var parser = new DxfLayoutParser();
                _fieldLayout = parser.Parse(filePath);
            }
            catch
            {
                StatusText.Text = "Failed to load field layout";
            }
        }
    }
    
    /// <summary>
    /// Load editor layers into the 3D viewer
    /// </summary>
    public void LoadEditorLayers(List<EditorLayer> layers)
    {
        _editorLayers = layers;
        if (_isInitialized)
        {
            BuildAllGeometry();
            ZoomToData();
        }
    }
    
    /// <summary>
    /// Load editor layers with editable points
    /// </summary>
    public void LoadEditorLayers(IEnumerable<EditorLayer> layers, List<EditablePoint> points)
    {
        _editorLayers = layers.ToList();
        _editablePoints = points;
        
        // Convert editable points to survey points for visualization
        // EditablePoint uses X, Y, Z properties
        _surveyPoints = points.Select(p => new SurveyPoint
        {
            Easting = p.X,
            Northing = p.Y,
            Depth = p.Z
        }).ToList();
        
        // Calculate center FIRST before building geometry
        UpdateDataBoundsFromPoints();
        
        if (_isInitialized)
        {
            BuildAllGeometry();
            ZoomToData();
        }
        
        DataInfo.Text = $"Points: {points.Count}";
    }
    
    /// <summary>
    /// Update data bounds from survey points (without rebuilding center)
    /// </summary>
    private void UpdateDataBoundsFromPoints()
    {
        _minX = _minY = _minZ = double.MaxValue;
        _maxX = _maxY = _maxZ = double.MinValue;
        
        if (_surveyPoints != null && _surveyPoints.Count > 0)
        {
            foreach (var p in _surveyPoints)
            {
                _minX = Math.Min(_minX, p.Easting);
                _maxX = Math.Max(_maxX, p.Easting);
                _minY = Math.Min(_minY, p.Northing);
                _maxY = Math.Max(_maxY, p.Northing);
                
                if (p.Depth.HasValue)
                {
                    _minZ = Math.Min(_minZ, -p.Depth.Value * _currentExaggeration);
                    _maxZ = Math.Max(_maxZ, -p.Depth.Value * _currentExaggeration);
                }
            }
            
            _centerX = (_minX + _maxX) / 2;
            _centerY = (_minY + _maxY) / 2;
            _centerZ = (_minZ + _maxZ) / 2;
            
            double sizeX = _maxX - _minX;
            double sizeY = _maxY - _minY;
            double sizeZ = Math.Abs(_maxZ - _minZ);
            
            _dataSize = Math.Max(sizeX, Math.Max(sizeY, sizeZ));
            if (_dataSize < 100) _dataSize = 10000;
        }
    }
    
    /// <summary>
    /// Add a custom layer with tuple points
    /// </summary>
    public void AddCustomLayer(string name, Color color, List<(double X, double Y, double Z)> points, bool asLine)
    {
        // Custom layers could be added as additional LineGeometryModel3D
        StatusText.Text = $"Added layer: {name} ({points.Count} points)";
    }
    
    /// <summary>
    /// Add a custom layer with Point3D points
    /// </summary>
    public void AddCustomLayer(string name, List<Point3D> points, Color color)
    {
        StatusText.Text = $"Added layer: {name} ({points.Count} points)";
    }
    
    /// <summary>
    /// Add a custom layer with survey points
    /// </summary>
    public void AddCustomLayer(string name, List<SurveyPoint> points, Color color)
    {
        StatusText.Text = $"Added layer: {name} ({points.Count} points)";
    }
    
    #endregion
    
    #region Geometry Building
    
    private void BuildAllGeometry()
    {
        BuildGridGeometry();
        BuildAxesGeometry();  // Coordinate axes
        BuildRouteGeometry();
        BuildSurveyGeometry();
        BuildSeabedGeometry();
        UpdateDataBounds();
    }
    
    private void BuildGridGeometry()
    {
        if (_gridModel == null) return;
        
        var builder = new LineBuilder();
        
        // Create grid based on data size if available, otherwise default
        double gridSize = _dataSize > 100 ? _dataSize * 1.5 : 10000;
        double step = gridSize / 10;
        
        // Round step to nice value
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(step)));
        step = Math.Ceiling(step / magnitude) * magnitude;
        
        for (double x = -gridSize / 2; x <= gridSize / 2; x += step)
        {
            builder.AddLine(new Vector3((float)x, (float)(-gridSize / 2), 0),
                           new Vector3((float)x, (float)(gridSize / 2), 0));
        }
        
        for (double y = -gridSize / 2; y <= gridSize / 2; y += step)
        {
            builder.AddLine(new Vector3((float)(-gridSize / 2), (float)y, 0),
                           new Vector3((float)(gridSize / 2), (float)y, 0));
        }
        
        _gridModel.Geometry = builder.ToLineGeometry3D();
    }
    
    private void BuildAxesGeometry()
    {
        if (_axesModel == null) return;
        
        var builder = new LineBuilder();
        
        // Determine axis length based on data
        double axisLength = _dataSize > 100 ? _dataSize * 0.3 : 2000;
        
        // X axis (Easting) - Red
        builder.AddLine(new Vector3(0, 0, 0), new Vector3((float)axisLength, 0, 0));
        
        // Y axis (Northing) - Green  
        builder.AddLine(new Vector3(0, 0, 0), new Vector3(0, (float)axisLength, 0));
        
        // Z axis (Up) - Blue
        builder.AddLine(new Vector3(0, 0, 0), new Vector3(0, 0, (float)(axisLength / 2)));
        
        // Arrow heads for X (Easting)
        float arrowSize = (float)(axisLength * 0.05);
        builder.AddLine(new Vector3((float)axisLength, 0, 0), 
                       new Vector3((float)axisLength - arrowSize, arrowSize/2, 0));
        builder.AddLine(new Vector3((float)axisLength, 0, 0), 
                       new Vector3((float)axisLength - arrowSize, -arrowSize/2, 0));
        
        // Arrow heads for Y (Northing)
        builder.AddLine(new Vector3(0, (float)axisLength, 0), 
                       new Vector3(arrowSize/2, (float)axisLength - arrowSize, 0));
        builder.AddLine(new Vector3(0, (float)axisLength, 0), 
                       new Vector3(-arrowSize/2, (float)axisLength - arrowSize, 0));
        
        // Arrow heads for Z (Up)
        builder.AddLine(new Vector3(0, 0, (float)(axisLength / 2)), 
                       new Vector3(arrowSize/2, 0, (float)(axisLength / 2) - arrowSize));
        builder.AddLine(new Vector3(0, 0, (float)(axisLength / 2)), 
                       new Vector3(-arrowSize/2, 0, (float)(axisLength / 2) - arrowSize));
        
        _axesModel.Geometry = builder.ToLineGeometry3D();
    }
    
    private void BuildRouteGeometry()
    {
        if (_routeModel == null || _routeData?.Segments == null || _routeData.Segments.Count == 0) return;
        
        var builder = new LineBuilder();
        
        foreach (var segment in _routeData.Segments)
        {
            double startX = segment.StartEasting - _centerX;
            double startY = segment.StartNorthing - _centerY;
            double endX = segment.EndEasting - _centerX;
            double endY = segment.EndNorthing - _centerY;
            
            if (segment.IsArc && Math.Abs(segment.Radius) > 0.001)
            {
                try
                {
                    // Get arc center using the method
                    var (centerEasting, centerNorthing) = segment.GetArcCenter();
                    double cx = centerEasting - _centerX;
                    double cy = centerNorthing - _centerY;
                    double radius = Math.Abs(segment.Radius);
                    
                    // Calculate start and end angles
                    double startAngle = Math.Atan2(startY - cy, startX - cx);
                    double endAngle = Math.Atan2(endY - cy, endX - cx);
                    
                    // Calculate arc angle (handle wraparound)
                    double arcAngle = endAngle - startAngle;
                    if (segment.IsClockwise)
                    {
                        if (arcAngle > 0) arcAngle -= 2 * Math.PI;
                    }
                    else
                    {
                        if (arcAngle < 0) arcAngle += 2 * Math.PI;
                    }
                    
                    // Approximate arc with line segments
                    int arcSegments = Math.Max(16, (int)(Math.Abs(arcAngle) * 180 / Math.PI / 5));
                    double angleStep = arcAngle / arcSegments;
                    
                    double prevX = startX, prevY = startY;
                    
                    for (int i = 1; i <= arcSegments; i++)
                    {
                        double angle = startAngle + (angleStep * i);
                        double x = cx + radius * Math.Cos(angle);
                        double y = cy + radius * Math.Sin(angle);
                        
                        builder.AddLine(new Vector3((float)prevX, (float)prevY, 0),
                                       new Vector3((float)x, (float)y, 0));
                        
                        prevX = x;
                        prevY = y;
                    }
                }
                catch
                {
                    // Fallback to straight line if arc calculation fails
                    builder.AddLine(new Vector3((float)startX, (float)startY, 0),
                                   new Vector3((float)endX, (float)endY, 0));
                }
            }
            else
            {
                // Straight segment
                builder.AddLine(new Vector3((float)startX, (float)startY, 0),
                               new Vector3((float)endX, (float)endY, 0));
            }
        }
        
        _routeModel.Geometry = builder.ToLineGeometry3D();
    }
    
    private void BuildSurveyGeometry()
    {
        if (_survey2DModel == null || _surveyPoints == null || _surveyPoints.Count < 2) return;
        
        var builder = new LineBuilder();
        
        // Build survey line (2D plan view at Z=0)
        for (int i = 0; i < _surveyPoints.Count - 1; i++)
        {
            var p1 = _surveyPoints[i];
            var p2 = _surveyPoints[i + 1];
            
            double x1 = p1.Easting - _centerX;
            double y1 = p1.Northing - _centerY;
            double x2 = p2.Easting - _centerX;
            double y2 = p2.Northing - _centerY;
            
            builder.AddLine(new Vector3((float)x1, (float)y1, 0),
                           new Vector3((float)x2, (float)y2, 0));
        }
        
        _survey2DModel.Geometry = builder.ToLineGeometry3D();
    }
    
    private void BuildSeabedGeometry()
    {
        if (_seabed3DModel == null || _surveyPoints == null || _surveyPoints.Count < 2) return;
        
        var builder = new LineBuilder();
        double exag = _currentExaggeration;
        
        double minDepth = double.MaxValue;
        double maxDepth = double.MinValue;
        
        // Build seabed line (3D with depth)
        for (int i = 0; i < _surveyPoints.Count - 1; i++)
        {
            var p1 = _surveyPoints[i];
            var p2 = _surveyPoints[i + 1];
            
            double x1 = p1.Easting - _centerX;
            double y1 = p1.Northing - _centerY;
            double z1 = -(p1.Depth ?? 0) * exag; // Negative because depth is positive downward
            
            double x2 = p2.Easting - _centerX;
            double y2 = p2.Northing - _centerY;
            double z2 = -(p2.Depth ?? 0) * exag;
            
            builder.AddLine(new Vector3((float)x1, (float)y1, (float)z1),
                           new Vector3((float)x2, (float)y2, (float)z2));
            
            if (p1.Depth.HasValue)
            {
                minDepth = Math.Min(minDepth, p1.Depth.Value);
                maxDepth = Math.Max(maxDepth, p1.Depth.Value);
            }
        }
        
        _seabed3DModel.Geometry = builder.ToLineGeometry3D();
        
        // Update depth info
        if (minDepth != double.MaxValue && maxDepth != double.MinValue)
        {
            DepthInfo.Text = $"Depth Range: {minDepth:F1}m to {maxDepth:F1}m";
        }
    }
    
    private void UpdateDataBounds()
    {
        _minX = _minY = _minZ = double.MaxValue;
        _maxX = _maxY = _maxZ = double.MinValue;
        
        // Get bounds from survey points
        if (_surveyPoints != null && _surveyPoints.Count > 0)
        {
            foreach (var p in _surveyPoints)
            {
                _minX = Math.Min(_minX, p.Easting);
                _maxX = Math.Max(_maxX, p.Easting);
                _minY = Math.Min(_minY, p.Northing);
                _maxY = Math.Max(_maxY, p.Northing);
                
                if (p.Depth.HasValue)
                {
                    _minZ = Math.Min(_minZ, -p.Depth.Value * _currentExaggeration);
                    _maxZ = Math.Max(_maxZ, -p.Depth.Value * _currentExaggeration);
                }
            }
        }
        
        // Get bounds from route
        if (_routeData?.Segments != null)
        {
            foreach (var seg in _routeData.Segments)
            {
                _minX = Math.Min(_minX, Math.Min(seg.StartEasting, seg.EndEasting));
                _maxX = Math.Max(_maxX, Math.Max(seg.StartEasting, seg.EndEasting));
                _minY = Math.Min(_minY, Math.Min(seg.StartNorthing, seg.EndNorthing));
                _maxY = Math.Max(_maxY, Math.Max(seg.StartNorthing, seg.EndNorthing));
            }
        }
        
        // Calculate center and size
        if (_minX != double.MaxValue)
        {
            _centerX = (_minX + _maxX) / 2;
            _centerY = (_minY + _maxY) / 2;
            _centerZ = (_minZ + _maxZ) / 2;
            
            double sizeX = _maxX - _minX;
            double sizeY = _maxY - _minY;
            double sizeZ = Math.Abs(_maxZ - _minZ);
            
            _dataSize = Math.Max(sizeX, Math.Max(sizeY, sizeZ));
            if (_dataSize < 100) _dataSize = 10000; // Default if no data
        }
        else
        {
            _centerX = _centerY = _centerZ = 0;
            _dataSize = 10000;
        }
    }
    
    #endregion
    
    #region Camera Controls
    
    private void ZoomToData()
    {
        if (Viewport.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera) return;
        
        // Calculate camera distance based on data size and FOV
        double fov = camera.FieldOfView * Math.PI / 180;
        double distance = (_dataSize / 2) / Math.Tan(fov / 2) * 1.5;
        
        // Position camera at 45 degree angle
        double angle = 45 * Math.PI / 180;
        double camX = distance * Math.Cos(angle);
        double camY = -distance * Math.Cos(angle);
        double camZ = distance * Math.Sin(angle);
        
        camera.Position = new Media3D.Point3D(camX, camY, camZ);
        camera.LookDirection = new Media3D.Vector3D(-camX, -camY, -camZ);
        camera.UpDirection = new Media3D.Vector3D(0, 0, 1);
        
        // Update near/far planes
        camera.NearPlaneDistance = Math.Max(0.1, distance / 1000);
        camera.FarPlaneDistance = distance * 10;
    }
    
    private void ViewTop_Click(object sender, RoutedEventArgs e)
    {
        if (Viewport.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera) return;
        
        double distance = _dataSize * 1.5;
        camera.Position = new Media3D.Point3D(0, 0, distance);
        camera.LookDirection = new Media3D.Vector3D(0, 0, -1);
        camera.UpDirection = new Media3D.Vector3D(0, 1, 0);
        
        StatusText.Text = "Top View (Plan)";
    }
    
    private void ViewFront_Click(object sender, RoutedEventArgs e)
    {
        if (Viewport.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera) return;
        
        double distance = _dataSize * 1.5;
        camera.Position = new Media3D.Point3D(0, -distance, 0);
        camera.LookDirection = new Media3D.Vector3D(0, 1, 0);
        camera.UpDirection = new Media3D.Vector3D(0, 0, 1);
        
        StatusText.Text = "Front View";
    }
    
    private void ViewSide_Click(object sender, RoutedEventArgs e)
    {
        if (Viewport.Camera is not HelixToolkit.Wpf.SharpDX.PerspectiveCamera camera) return;
        
        double distance = _dataSize * 1.5;
        camera.Position = new Media3D.Point3D(distance, 0, 0);
        camera.LookDirection = new Media3D.Vector3D(-1, 0, 0);
        camera.UpDirection = new Media3D.Vector3D(0, 0, 1);
        
        StatusText.Text = "Side View";
    }
    
    private void View3D_Click(object sender, RoutedEventArgs e)
    {
        ZoomToData();
        StatusText.Text = "3D Perspective View";
    }
    
    private void ViewFit_Click(object sender, RoutedEventArgs e)
    {
        UpdateDataBounds();
        ZoomToData();
        StatusText.Text = "Zoomed to fit all data";
    }
    
    #endregion
    
    #region UI Event Handlers
    
    /// <summary>
    /// Handle keyboard shortcuts for camera control
    /// </summary>
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case System.Windows.Input.Key.T:
                ViewTop_Click(null, null);
                break;
            case System.Windows.Input.Key.F:
                ViewFront_Click(null, null);
                break;
            case System.Windows.Input.Key.S:
                ViewSide_Click(null, null);
                break;
            case System.Windows.Input.Key.D3:
            case System.Windows.Input.Key.NumPad3:
                View3D_Click(null, null);
                break;
            case System.Windows.Input.Key.H:
            case System.Windows.Input.Key.Home:
                ViewFit_Click(null, null);
                break;
            case System.Windows.Input.Key.G:
                ShowGrid.IsChecked = !ShowGrid.IsChecked;
                LayerVisibility_Changed(null, null);
                break;
            case System.Windows.Input.Key.R:
                ShowRoute.IsChecked = !ShowRoute.IsChecked;
                LayerVisibility_Changed(null, null);
                break;
            case System.Windows.Input.Key.OemPlus:
            case System.Windows.Input.Key.Add:
                // Increase exaggeration
                ExaggerationSlider.Value = Math.Min(50, ExaggerationSlider.Value + 5);
                break;
            case System.Windows.Input.Key.OemMinus:
            case System.Windows.Input.Key.Subtract:
                // Decrease exaggeration
                ExaggerationSlider.Value = Math.Max(1, ExaggerationSlider.Value - 5);
                break;
        }
    }
    
    private void ExaggerationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isInitialized) return;
        
        _currentExaggeration = e.NewValue;
        BuildSeabedGeometry();
        
        StatusText.Text = $"Depth exaggeration: {_currentExaggeration:F0}x";
    }
    
    private void LayerVisibility_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized) return;
        
        if (_gridModel != null) _gridModel.IsRendering = ShowGrid.IsChecked == true;
        if (_routeModel != null) _routeModel.IsRendering = ShowRoute.IsChecked == true;
        if (_survey2DModel != null) _survey2DModel.IsRendering = ShowSurvey.IsChecked == true;
        if (_seabed3DModel != null) _seabed3DModel.IsRendering = ShowSeabed.IsChecked == true;
    }
    
    #endregion
}

/// <summary>
/// Simple 3D point for custom layers
/// </summary>
public class Point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    
    public Point3D() { }
    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
