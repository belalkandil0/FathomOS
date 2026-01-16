using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// 3D Visualization service for USBL verification data
/// Uses WPF 3D (Media3D) for compatibility without additional dependencies
/// </summary>
public class Visualization3DService
{
    private readonly AdvancedStatisticsService _statsService = new();
    
    #region Scene Creation
    
    /// <summary>
    /// Create complete 3D scene with all visualization elements
    /// </summary>
    public Model3DGroup CreateScene(UsblVerificationProject project, VerificationResults? results, 
        Visualization3DOptions options)
    {
        var scene = new Model3DGroup();
        
        // Get all observations
        var allSpinObs = project.AllSpinTests
            .SelectMany(s => s.Observations.Where(o => !o.IsExcluded))
            .ToList();
        
        if (allSpinObs.Count == 0) return scene;
        
        // Calculate bounds for normalization
        var bounds = CalculateBounds(allSpinObs, results);
        
        // Add lighting
        scene.Children.Add(CreateLighting());
        
        // Add coordinate axes
        if (options.ShowAxes)
        {
            scene.Children.Add(CreateAxes(bounds, options.AxisLength));
        }
        
        // Add grid plane
        if (options.ShowGrid)
        {
            scene.Children.Add(CreateGridPlane(bounds, options.GridSpacing));
        }
        
        // Add tolerance sphere/cylinder
        if (results?.SpinMeanPosition != null && options.ShowToleranceSphere)
        {
            scene.Children.Add(CreateToleranceSphere(
                results.SpinMeanPosition, project.Tolerance, bounds));
        }
        
        // Add statistical surfaces
        if (options.ShowStatisticalSurfaces && allSpinObs.Count > 3)
        {
            var stats = _statsService.CalculateRadialStatistics(allSpinObs, "All");
            
            // CEP sphere
            if (results?.SpinMeanPosition != null)
            {
                scene.Children.Add(CreateStatSphere(results.SpinMeanPosition, stats.CEP, bounds,
                    Color.FromArgb(60, 0, 255, 255), "CEP"));
                
                // R95 sphere
                scene.Children.Add(CreateStatSphere(results.SpinMeanPosition, stats.R95, bounds,
                    Color.FromArgb(40, 255, 255, 0), "R95"));
            }
        }
        
        // Add mean position marker
        if (results?.SpinMeanPosition != null && options.ShowMeanPosition)
        {
            scene.Children.Add(CreateMeanPositionMarker(results.SpinMeanPosition, bounds));
        }
        
        // Add spin test points by heading
        foreach (var spin in project.AllSpinTests)
        {
            var observations = spin.Observations.Where(o => !o.IsExcluded).ToList();
            if (observations.Count == 0) continue;
            
            var color = GetHeadingColor((int)spin.NominalHeading);
            scene.Children.Add(CreatePointCloud(observations, bounds, color, options.PointSize));
        }
        
        // Add transit points
        if (options.ShowTransitData)
        {
            var transit1Obs = project.Transit1.Observations.Where(o => !o.IsExcluded).ToList();
            var transit2Obs = project.Transit2.Observations.Where(o => !o.IsExcluded).ToList();
            
            if (transit1Obs.Count > 0)
            {
                scene.Children.Add(CreatePointCloud(transit1Obs, bounds, 
                    Color.FromRgb(0, 255, 255), options.PointSize * 0.8));
            }
            
            if (transit2Obs.Count > 0)
            {
                scene.Children.Add(CreatePointCloud(transit2Obs, bounds, 
                    Color.FromRgb(255, 165, 0), options.PointSize * 0.8));
            }
        }
        
        // Add vessel track
        if (options.ShowVesselTrack)
        {
            scene.Children.Add(CreateVesselTrack(allSpinObs, bounds));
        }
        
        // Add connecting lines from vessel to transponder
        if (options.ShowVesselToTransponderLines)
        {
            scene.Children.Add(CreateVesselToTransponderLines(allSpinObs, bounds));
        }
        
        return scene;
    }
    
    #endregion
    
    #region Bounds Calculation
    
    private SceneBounds CalculateBounds(List<UsblObservation> observations, VerificationResults? results)
    {
        if (observations.Count == 0)
        {
            return new SceneBounds 
            { 
                CenterE = 0, CenterN = 0, CenterD = 0, 
                Scale = 1, MinDepth = 0, MaxDepth = 10 
            };
        }
        
        double minE = observations.Min(o => o.TransponderEasting);
        double maxE = observations.Max(o => o.TransponderEasting);
        double minN = observations.Min(o => o.TransponderNorthing);
        double maxN = observations.Max(o => o.TransponderNorthing);
        double minD = observations.Min(o => o.TransponderDepth);
        double maxD = observations.Max(o => o.TransponderDepth);
        
        // Use mean position as center if available
        double centerE = results?.SpinMeanPosition?.Easting ?? (minE + maxE) / 2;
        double centerN = results?.SpinMeanPosition?.Northing ?? (minN + maxN) / 2;
        double centerD = results?.SpinMeanPosition?.Depth ?? (minD + maxD) / 2;
        
        // Calculate scale to normalize to reasonable viewport size
        double rangeE = maxE - minE;
        double rangeN = maxN - minN;
        double rangeD = maxD - minD;
        double maxRange = Math.Max(Math.Max(rangeE, rangeN), Math.Max(rangeD, 1));
        
        return new SceneBounds
        {
            CenterE = centerE,
            CenterN = centerN,
            CenterD = centerD,
            Scale = 10.0 / maxRange, // Normalize to ~10 units
            MinDepth = minD,
            MaxDepth = maxD,
            RangeE = rangeE,
            RangeN = rangeN,
            RangeD = rangeD
        };
    }
    
    private Point3D NormalizePoint(double easting, double northing, double depth, SceneBounds bounds)
    {
        // Transform to scene coordinates (centered, scaled)
        double x = (easting - bounds.CenterE) * bounds.Scale;
        double y = (northing - bounds.CenterN) * bounds.Scale;
        double z = -(depth - bounds.CenterD) * bounds.Scale; // Invert Z for depth
        
        return new Point3D(x, y, z);
    }
    
    #endregion
    
    #region Lighting
    
    private Model3DGroup CreateLighting()
    {
        var lights = new Model3DGroup();
        
        // Ambient light
        lights.Children.Add(new AmbientLight(Color.FromRgb(80, 80, 80)));
        
        // Directional lights from multiple angles
        lights.Children.Add(new DirectionalLight(
            Color.FromRgb(180, 180, 180),
            new Vector3D(-1, -1, -1)));
        
        lights.Children.Add(new DirectionalLight(
            Color.FromRgb(100, 100, 100),
            new Vector3D(1, 0.5, 0.5)));
        
        return lights;
    }
    
    #endregion
    
    #region Axes and Grid
    
    private Model3DGroup CreateAxes(SceneBounds bounds, double length)
    {
        var axes = new Model3DGroup();
        double axisLength = length * bounds.Scale;
        double thickness = 0.05;
        
        // X axis (Easting) - Red
        axes.Children.Add(CreateCylinder(
            new Point3D(0, 0, 0), 
            new Point3D(axisLength, 0, 0), 
            thickness, 
            Colors.Red));
        
        // Y axis (Northing) - Green
        axes.Children.Add(CreateCylinder(
            new Point3D(0, 0, 0), 
            new Point3D(0, axisLength, 0), 
            thickness, 
            Colors.Green));
        
        // Z axis (Depth) - Blue (pointing down)
        axes.Children.Add(CreateCylinder(
            new Point3D(0, 0, 0), 
            new Point3D(0, 0, -axisLength), 
            thickness, 
            Colors.Blue));
        
        return axes;
    }
    
    private Model3DGroup CreateGridPlane(SceneBounds bounds, double spacing)
    {
        var grid = new Model3DGroup();
        double scaledSpacing = spacing * bounds.Scale;
        double halfSize = 5 * scaledSpacing;
        double thickness = 0.02;
        
        var gridColor = Color.FromArgb(100, 128, 128, 128);
        
        // Grid lines parallel to X
        for (double y = -halfSize; y <= halfSize; y += scaledSpacing)
        {
            grid.Children.Add(CreateLine(
                new Point3D(-halfSize, y, 0),
                new Point3D(halfSize, y, 0),
                thickness, gridColor));
        }
        
        // Grid lines parallel to Y
        for (double x = -halfSize; x <= halfSize; x += scaledSpacing)
        {
            grid.Children.Add(CreateLine(
                new Point3D(x, -halfSize, 0),
                new Point3D(x, halfSize, 0),
                thickness, gridColor));
        }
        
        return grid;
    }
    
    #endregion
    
    #region Spheres and Markers
    
    private GeometryModel3D CreateToleranceSphere(TransponderPosition center, double radius, SceneBounds bounds)
    {
        var centerPoint = NormalizePoint(center.Easting, center.Northing, center.Depth, bounds);
        double scaledRadius = radius * bounds.Scale;
        
        var mesh = CreateSphereMesh(centerPoint, scaledRadius, 32, 16);
        
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(60, 0, 255, 0))));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), 30));
        
        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material
        };
    }
    
    private GeometryModel3D CreateStatSphere(TransponderPosition center, double radius, 
        SceneBounds bounds, Color color, string name)
    {
        var centerPoint = NormalizePoint(center.Easting, center.Northing, center.Depth, bounds);
        double scaledRadius = radius * bounds.Scale;
        
        var mesh = CreateSphereMesh(centerPoint, scaledRadius, 24, 12);
        
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        
        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material
        };
    }
    
    private Model3DGroup CreateMeanPositionMarker(TransponderPosition position, SceneBounds bounds)
    {
        var marker = new Model3DGroup();
        var center = NormalizePoint(position.Easting, position.Northing, position.Depth, bounds);
        double size = 0.3;
        
        // Create a 3D cross
        var redMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Red));
        
        // X bar
        marker.Children.Add(new GeometryModel3D(
            CreateBoxMesh(new Point3D(center.X - size, center.Y, center.Z), 
                         new Point3D(center.X + size, center.Y + 0.05, center.Z + 0.05)),
            redMaterial));
        
        // Y bar
        marker.Children.Add(new GeometryModel3D(
            CreateBoxMesh(new Point3D(center.X, center.Y - size, center.Z), 
                         new Point3D(center.X + 0.05, center.Y + size, center.Z + 0.05)),
            redMaterial));
        
        // Z bar
        marker.Children.Add(new GeometryModel3D(
            CreateBoxMesh(new Point3D(center.X, center.Y, center.Z - size), 
                         new Point3D(center.X + 0.05, center.Y + 0.05, center.Z + size)),
            redMaterial));
        
        return marker;
    }
    
    #endregion
    
    #region Point Cloud
    
    private Model3DGroup CreatePointCloud(List<UsblObservation> observations, SceneBounds bounds, 
        Color color, double pointSize)
    {
        var pointCloud = new Model3DGroup();
        
        var material = new MaterialGroup();
        material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
        material.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), 50));
        
        foreach (var obs in observations)
        {
            var point = NormalizePoint(obs.TransponderEasting, obs.TransponderNorthing, 
                obs.TransponderDepth, bounds);
            
            var sphereMesh = CreateSphereMesh(point, pointSize, 8, 4);
            pointCloud.Children.Add(new GeometryModel3D(sphereMesh, material));
        }
        
        return pointCloud;
    }
    
    #endregion
    
    #region Tracks and Lines
    
    private Model3DGroup CreateVesselTrack(List<UsblObservation> observations, SceneBounds bounds)
    {
        var track = new Model3DGroup();
        var sorted = observations.OrderBy(o => o.Timestamp).ToList();
        
        if (sorted.Count < 2) return track;
        
        var color = Color.FromArgb(150, 255, 255, 255);
        double thickness = 0.03;
        
        for (int i = 1; i < sorted.Count; i++)
        {
            var p1 = NormalizePoint(sorted[i-1].VesselEasting, sorted[i-1].VesselNorthing, 0, bounds);
            var p2 = NormalizePoint(sorted[i].VesselEasting, sorted[i].VesselNorthing, 0, bounds);
            
            track.Children.Add(CreateLine(p1, p2, thickness, color));
        }
        
        return track;
    }
    
    private Model3DGroup CreateVesselToTransponderLines(List<UsblObservation> observations, SceneBounds bounds)
    {
        var lines = new Model3DGroup();
        var color = Color.FromArgb(80, 200, 200, 200);
        double thickness = 0.01;
        
        // Only draw every Nth line to avoid clutter
        int step = Math.Max(1, observations.Count / 50);
        
        for (int i = 0; i < observations.Count; i += step)
        {
            var obs = observations[i];
            var vesselPoint = NormalizePoint(obs.VesselEasting, obs.VesselNorthing, 0, bounds);
            var transponderPoint = NormalizePoint(obs.TransponderEasting, obs.TransponderNorthing, 
                obs.TransponderDepth, bounds);
            
            lines.Children.Add(CreateLine(vesselPoint, transponderPoint, thickness, color));
        }
        
        return lines;
    }
    
    #endregion
    
    #region Mesh Primitives
    
    private MeshGeometry3D CreateSphereMesh(Point3D center, double radius, int slices, int stacks)
    {
        var mesh = new MeshGeometry3D();
        
        for (int stack = 0; stack <= stacks; stack++)
        {
            double phi = Math.PI * stack / stacks;
            double y = radius * Math.Cos(phi);
            double stackRadius = radius * Math.Sin(phi);
            
            for (int slice = 0; slice <= slices; slice++)
            {
                double theta = 2 * Math.PI * slice / slices;
                double x = stackRadius * Math.Cos(theta);
                double z = stackRadius * Math.Sin(theta);
                
                mesh.Positions.Add(new Point3D(center.X + x, center.Y + y, center.Z + z));
                mesh.Normals.Add(new Vector3D(x, y, z));
                mesh.TextureCoordinates.Add(new Point((double)slice / slices, (double)stack / stacks));
            }
        }
        
        // Create triangles
        for (int stack = 0; stack < stacks; stack++)
        {
            for (int slice = 0; slice < slices; slice++)
            {
                int current = stack * (slices + 1) + slice;
                int next = current + slices + 1;
                
                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(current + 1);
                
                mesh.TriangleIndices.Add(current + 1);
                mesh.TriangleIndices.Add(next);
                mesh.TriangleIndices.Add(next + 1);
            }
        }
        
        return mesh;
    }
    
    private MeshGeometry3D CreateBoxMesh(Point3D min, Point3D max)
    {
        var mesh = new MeshGeometry3D();
        
        // 8 vertices
        mesh.Positions.Add(new Point3D(min.X, min.Y, min.Z)); // 0
        mesh.Positions.Add(new Point3D(max.X, min.Y, min.Z)); // 1
        mesh.Positions.Add(new Point3D(max.X, max.Y, min.Z)); // 2
        mesh.Positions.Add(new Point3D(min.X, max.Y, min.Z)); // 3
        mesh.Positions.Add(new Point3D(min.X, min.Y, max.Z)); // 4
        mesh.Positions.Add(new Point3D(max.X, min.Y, max.Z)); // 5
        mesh.Positions.Add(new Point3D(max.X, max.Y, max.Z)); // 6
        mesh.Positions.Add(new Point3D(min.X, max.Y, max.Z)); // 7
        
        // 6 faces (2 triangles each)
        // Front
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2);
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(3);
        // Back
        mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(5);
        mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(7); mesh.TriangleIndices.Add(6);
        // Left
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(7);
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(7); mesh.TriangleIndices.Add(4);
        // Right
        mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(6);
        mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(2);
        // Top
        mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(2); mesh.TriangleIndices.Add(6);
        mesh.TriangleIndices.Add(3); mesh.TriangleIndices.Add(6); mesh.TriangleIndices.Add(7);
        // Bottom
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(4); mesh.TriangleIndices.Add(5);
        mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(5); mesh.TriangleIndices.Add(1);
        
        return mesh;
    }
    
    private GeometryModel3D CreateCylinder(Point3D start, Point3D end, double radius, Color color)
    {
        var mesh = new MeshGeometry3D();
        int segments = 8;
        
        Vector3D axis = end - start;
        double length = axis.Length;
        axis.Normalize();
        
        // Find perpendicular vectors
        Vector3D perp1 = Vector3D.CrossProduct(axis, new Vector3D(0, 0, 1));
        if (perp1.Length < 0.001)
            perp1 = Vector3D.CrossProduct(axis, new Vector3D(0, 1, 0));
        perp1.Normalize();
        Vector3D perp2 = Vector3D.CrossProduct(axis, perp1);
        perp2.Normalize();
        
        // Create vertices around start and end
        for (int i = 0; i <= segments; i++)
        {
            double angle = 2 * Math.PI * i / segments;
            Vector3D offset = radius * (Math.Cos(angle) * perp1 + Math.Sin(angle) * perp2);
            
            mesh.Positions.Add(start + offset);
            mesh.Positions.Add(end + offset);
        }
        
        // Create triangles
        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            int i1 = i * 2 + 1;
            int i2 = (i + 1) * 2;
            int i3 = (i + 1) * 2 + 1;
            
            mesh.TriangleIndices.Add(i0);
            mesh.TriangleIndices.Add(i1);
            mesh.TriangleIndices.Add(i2);
            
            mesh.TriangleIndices.Add(i2);
            mesh.TriangleIndices.Add(i1);
            mesh.TriangleIndices.Add(i3);
        }
        
        var material = new DiffuseMaterial(new SolidColorBrush(color));
        return new GeometryModel3D(mesh, material);
    }
    
    private GeometryModel3D CreateLine(Point3D start, Point3D end, double thickness, Color color)
    {
        return CreateCylinder(start, end, thickness, color);
    }
    
    #endregion
    
    #region Colors
    
    private Color GetHeadingColor(int heading)
    {
        return heading switch
        {
            0 => Color.FromRgb(255, 80, 80),      // Red
            90 => Color.FromRgb(80, 255, 80),     // Green
            180 => Color.FromRgb(80, 80, 255),    // Blue
            270 => Color.FromRgb(255, 80, 255),   // Magenta
            _ => Color.FromRgb(255, 255, 80)      // Yellow (default)
        };
    }
    
    #endregion
}

#region Supporting Classes

/// <summary>
/// Scene bounds for normalization
/// </summary>
public class SceneBounds
{
    public double CenterE { get; set; }
    public double CenterN { get; set; }
    public double CenterD { get; set; }
    public double Scale { get; set; }
    public double MinDepth { get; set; }
    public double MaxDepth { get; set; }
    public double RangeE { get; set; }
    public double RangeN { get; set; }
    public double RangeD { get; set; }
}

/// <summary>
/// Options for 3D visualization
/// </summary>
public class Visualization3DOptions
{
    // Display options
    public bool ShowAxes { get; set; } = true;
    public double AxisLength { get; set; } = 5.0;
    public bool ShowGrid { get; set; } = true;
    public double GridSpacing { get; set; } = 1.0;
    
    // Tolerance visualization
    public bool ShowToleranceSphere { get; set; } = true;
    public bool ShowStatisticalSurfaces { get; set; } = true;
    public bool ShowMeanPosition { get; set; } = true;
    
    // Data points
    public double PointSize { get; set; } = 0.15;
    public bool ShowTransitData { get; set; } = true;
    
    // Tracks
    public bool ShowVesselTrack { get; set; } = false;
    public bool ShowVesselToTransponderLines { get; set; } = false;
    
    // Camera
    public double CameraDistance { get; set; } = 20;
    public double CameraAngle { get; set; } = 45;
}

#endregion
