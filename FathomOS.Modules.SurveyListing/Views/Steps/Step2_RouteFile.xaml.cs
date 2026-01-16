using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FathomOS.Core.Models;
using FathomOS.Core.Parsers;
using FathomOS.Modules.SurveyListing.ViewModels;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.Views.Steps;

public partial class Step2_RouteFile : UserControl
{
    public Step2_RouteFile()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step2ViewModel vm)
        {
            vm.RouteDataChanged += OnRouteDataChanged;
            vm.FieldLayoutChanged += OnFieldLayoutChanged;
            UpdatePreview();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void OnRouteDataChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void OnFieldLayoutChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step2ViewModel vm)
        {
            vm.BrowseFile();
        }
    }

    private void BrowseFieldLayout_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step2ViewModel vm)
        {
            vm.BrowseFieldLayout();
        }
    }

    private void ClearFieldLayout_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step2ViewModel vm)
        {
            vm.ClearFieldLayout();
        }
    }

    private void Open3DViewer_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step2ViewModel vm)
        {
            try
            {
                var viewer = new Viewer3DWindow();
                
                // Load route if available
                if (vm.RouteData != null)
                {
                    viewer.LoadRouteData(vm.RouteData);
                }
                
                // Load field layout if available
                if (vm.FieldLayout != null && !string.IsNullOrEmpty(vm.FieldLayoutPath))
                {
                    viewer.LoadFieldLayout(vm.FieldLayoutPath);
                }
                
                viewer.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening 3D Viewer:\n\n{ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// Draw 2D preview of route and field layout
    /// </summary>
    private void UpdatePreview()
    {
        if (PreviewCanvas == null) return;
        
        PreviewCanvas.Children.Clear();
        
        if (DataContext is not Step2ViewModel vm) return;
        
        var canvasWidth = PreviewCanvas.ActualWidth;
        var canvasHeight = PreviewCanvas.ActualHeight;
        
        if (canvasWidth <= 0 || canvasHeight <= 0) return;
        
        // Calculate bounds
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        
        // Get route bounds
        if (vm.RouteData != null)
        {
            foreach (var seg in vm.RouteData.Segments)
            {
                minX = Math.Min(minX, Math.Min(seg.StartEasting, seg.EndEasting));
                maxX = Math.Max(maxX, Math.Max(seg.StartEasting, seg.EndEasting));
                minY = Math.Min(minY, Math.Min(seg.StartNorthing, seg.EndNorthing));
                maxY = Math.Max(maxY, Math.Max(seg.StartNorthing, seg.EndNorthing));
            }
        }
        
        // Get field layout bounds
        if (vm.FieldLayout != null)
        {
            minX = Math.Min(minX, vm.FieldLayout.MinX);
            maxX = Math.Max(maxX, vm.FieldLayout.MaxX);
            minY = Math.Min(minY, vm.FieldLayout.MinY);
            maxY = Math.Max(maxY, vm.FieldLayout.MaxY);
        }
        
        if (minX == double.MaxValue) return; // No data
        
        // Add margin
        double margin = 20;
        double dataWidth = maxX - minX;
        double dataHeight = maxY - minY;
        
        if (dataWidth <= 0) dataWidth = 1;
        if (dataHeight <= 0) dataHeight = 1;
        
        // Calculate scale to fit
        double scaleX = (canvasWidth - 2 * margin) / dataWidth;
        double scaleY = (canvasHeight - 2 * margin) / dataHeight;
        double scale = Math.Min(scaleX, scaleY);
        
        // Calculate offset to center
        double offsetX = margin + (canvasWidth - 2 * margin - dataWidth * scale) / 2;
        double offsetY = margin + (canvasHeight - 2 * margin - dataHeight * scale) / 2;
        
        // Transform function
        Point Transform(double x, double y)
        {
            return new Point(
                offsetX + (x - minX) * scale,
                canvasHeight - (offsetY + (y - minY) * scale) // Flip Y
            );
        }
        
        // Draw field layout (gray, behind route)
        if (vm.FieldLayout != null)
        {
            DrawFieldLayout(vm.FieldLayout, Transform, Colors.DimGray);
        }
        
        // Draw route (red, on top)
        if (vm.RouteData != null)
        {
            foreach (var segment in vm.RouteData.Segments)
            {
                if (segment.IsStraight)
                {
                    var p1 = Transform(segment.StartEasting, segment.StartNorthing);
                    var p2 = Transform(segment.EndEasting, segment.EndNorthing);
                    
                    var line = new Line
                    {
                        X1 = p1.X, Y1 = p1.Y,
                        X2 = p2.X, Y2 = p2.Y,
                        Stroke = new SolidColorBrush(Colors.Red),
                        StrokeThickness = 2
                    };
                    PreviewCanvas.Children.Add(line);
                }
                else
                {
                    // Draw arc as polyline
                    var points = GetArcPoints(segment, 20, Transform);
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var line = new Line
                        {
                            X1 = points[i].X, Y1 = points[i].Y,
                            X2 = points[i + 1].X, Y2 = points[i + 1].Y,
                            Stroke = new SolidColorBrush(Colors.Red),
                            StrokeThickness = 2
                        };
                        PreviewCanvas.Children.Add(line);
                    }
                }
            }
        }
    }

    private List<Point> GetArcPoints(RouteSegment segment, int numSegments, Func<double, double, Point> transform)
    {
        var points = new List<Point>();
        var (centerE, centerN) = segment.GetArcCenter();
        double radius = Math.Abs(segment.Radius);

        double startAngle = Math.Atan2(segment.StartNorthing - centerN, segment.StartEasting - centerE);
        double endAngle = Math.Atan2(segment.EndNorthing - centerN, segment.EndEasting - centerE);

        if (segment.IsClockwise && endAngle > startAngle)
            endAngle -= 2 * Math.PI;
        else if (!segment.IsClockwise && endAngle < startAngle)
            endAngle += 2 * Math.PI;

        double angleStep = (endAngle - startAngle) / numSegments;

        for (int i = 0; i <= numSegments; i++)
        {
            double angle = startAngle + i * angleStep;
            double x = centerE + radius * Math.Cos(angle);
            double y = centerN + radius * Math.Sin(angle);
            points.Add(transform(x, y));
        }
        return points;
    }

    private void DrawFieldLayout(FieldLayout layout, Func<double, double, Point> transform, Color color)
    {
        var brush = new SolidColorBrush(color);
        
        // Draw lines
        foreach (var line in layout.Lines)
        {
            var p1 = transform(line.StartX, line.StartY);
            var p2 = transform(line.EndX, line.EndY);
            PreviewCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = 1
            });
        }
        
        // Draw polylines
        foreach (var poly in layout.Polylines)
        {
            for (int i = 0; i < poly.Vertices.Count - 1; i++)
            {
                var p1 = transform(poly.Vertices[i].X, poly.Vertices[i].Y);
                var p2 = transform(poly.Vertices[i + 1].X, poly.Vertices[i + 1].Y);
                PreviewCanvas.Children.Add(new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = brush,
                    StrokeThickness = 1
                });
            }
            
            // Close polyline if needed
            if (poly.IsClosed && poly.Vertices.Count > 2)
            {
                var p1 = transform(poly.Vertices[^1].X, poly.Vertices[^1].Y);
                var p2 = transform(poly.Vertices[0].X, poly.Vertices[0].Y);
                PreviewCanvas.Children.Add(new Line
                {
                    X1 = p1.X, Y1 = p1.Y,
                    X2 = p2.X, Y2 = p2.Y,
                    Stroke = brush,
                    StrokeThickness = 1
                });
            }
        }
        
        // Draw circles
        foreach (var circle in layout.Circles)
        {
            var center = transform(circle.CenterX, circle.CenterY);
            var edgePoint = transform(circle.CenterX + circle.Radius, circle.CenterY);
            double screenRadius = Math.Abs(edgePoint.X - center.X);
            
            if (screenRadius > 1 && screenRadius < 1000)
            {
                PreviewCanvas.Children.Add(new Ellipse
                {
                    Width = screenRadius * 2,
                    Height = screenRadius * 2,
                    Stroke = brush,
                    StrokeThickness = 1,
                    Margin = new Thickness(center.X - screenRadius, center.Y - screenRadius, 0, 0)
                });
            }
        }
        
        // Draw arcs
        foreach (var arc in layout.Arcs)
        {
            DrawArc(arc, transform, brush);
        }
        
        // Draw points
        foreach (var point in layout.Points)
        {
            var p = transform(point.X, point.Y);
            PreviewCanvas.Children.Add(new Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = brush,
                Margin = new Thickness(p.X - 2, p.Y - 2, 0, 0)
            });
        }
    }

    private void DrawArc(LayoutArc arc, Func<double, double, Point> transform, SolidColorBrush brush)
    {
        // Draw arc as line segments
        int numSegments = 20;
        double startAngleRad = arc.StartAngle * Math.PI / 180;
        double endAngleRad = arc.EndAngle * Math.PI / 180;
        
        // Ensure we go the right direction
        if (endAngleRad < startAngleRad)
            endAngleRad += 2 * Math.PI;
            
        double angleStep = (endAngleRad - startAngleRad) / numSegments;
        
        for (int i = 0; i < numSegments; i++)
        {
            double angle1 = startAngleRad + i * angleStep;
            double angle2 = startAngleRad + (i + 1) * angleStep;
            
            double x1 = arc.CenterX + arc.Radius * Math.Cos(angle1);
            double y1 = arc.CenterY + arc.Radius * Math.Sin(angle1);
            double x2 = arc.CenterX + arc.Radius * Math.Cos(angle2);
            double y2 = arc.CenterY + arc.Radius * Math.Sin(angle2);
            
            var p1 = transform(x1, y1);
            var p2 = transform(x2, y2);
            
            PreviewCanvas.Children.Add(new Line
            {
                X1 = p1.X, Y1 = p1.Y,
                X2 = p2.X, Y2 = p2.Y,
                Stroke = brush,
                StrokeThickness = 1
            });
        }
    }
}
