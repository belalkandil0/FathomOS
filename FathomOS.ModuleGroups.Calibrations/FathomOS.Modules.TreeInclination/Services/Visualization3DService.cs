namespace FathomOS.Modules.TreeInclination.Services;

using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using FathomOS.Modules.TreeInclination.Models;

/// <summary>3D visualization service using HelixToolkit.Wpf</summary>
public class Visualization3DService
{
    private readonly Color _structureColor = Color.FromRgb(59, 130, 246);
    private readonly Color _cornerColor = Color.FromRgb(34, 197, 94);
    private readonly Color _vectorColor = Color.FromRgb(239, 68, 68);
    private readonly Color _gridColor = Color.FromRgb(75, 85, 99);

    public Model3DGroup CreateStructureModel(InclinationProject project, double exaggeration = 10.0)
    {
        var modelGroup = new Model3DGroup();

        var corners = project.Corners.Where(c => !c.IsClosurePoint).OrderBy(c => c.Index).ToList();
        if (corners.Count < 3) return modelGroup;

        // Calculate bounds for normalization
        double minX = corners.Min(c => c.X);
        double maxX = corners.Max(c => c.X);
        double minY = corners.Min(c => c.Y);
        double maxY = corners.Max(c => c.Y);
        double minZ = corners.Min(c => c.CorrectedDepth);
        double maxZ = corners.Max(c => c.CorrectedDepth);
        double meanZ = corners.Average(c => c.CorrectedDepth);

        double sizeX = Math.Max(maxX - minX, 0.1);
        double sizeY = Math.Max(maxY - minY, 0.1);
        double sizeZ = Math.Max((maxZ - minZ) * exaggeration, 0.1);
        double scale = 1.0 / Math.Max(sizeX, Math.Max(sizeY, sizeZ));

        double cx = (minX + maxX) / 2;
        double cy = (minY + maxY) / 2;

        // Create structure surface
        var surfaceMesh = CreateSurfaceMesh(corners, cx, cy, meanZ, scale, exaggeration);
        var surfaceMaterial = new DiffuseMaterial(new SolidColorBrush(_structureColor) { Opacity = 0.7 });
        var backMaterial = new DiffuseMaterial(new SolidColorBrush(_structureColor) { Opacity = 0.3 });
        modelGroup.Children.Add(new GeometryModel3D
        {
            Geometry = surfaceMesh,
            Material = surfaceMaterial,
            BackMaterial = backMaterial
        });

        // Create edge lines
        var edgeMesh = CreateEdgeMesh(corners, cx, cy, meanZ, scale, exaggeration, 0.005);
        var edgeMaterial = new DiffuseMaterial(new SolidColorBrush(_structureColor));
        modelGroup.Children.Add(new GeometryModel3D { Geometry = edgeMesh, Material = edgeMaterial });

        // Create corner spheres
        foreach (var corner in corners)
        {
            var sphereMesh = CreateSphereMesh(
                (corner.X - cx) * scale,
                (corner.Y - cy) * scale,
                -(corner.CorrectedDepth - meanZ) * scale * exaggeration,
                0.025);
            var sphereMaterial = new DiffuseMaterial(new SolidColorBrush(_cornerColor));
            modelGroup.Children.Add(new GeometryModel3D { Geometry = sphereMesh, Material = sphereMaterial });
        }

        // Create inclination vector
        if (project.Result != null && project.Result.TotalInclination > 0.001)
        {
            var vectorMesh = CreateVectorMesh(project.Result, exaggeration);
            var vectorMaterial = new EmissiveMaterial(new SolidColorBrush(_vectorColor));
            modelGroup.Children.Add(new GeometryModel3D { Geometry = vectorMesh, Material = vectorMaterial });
        }

        // Create reference grid
        var gridMesh = CreateGridMesh(0.5, 0.1, 0.001);
        var gridMaterial = new DiffuseMaterial(new SolidColorBrush(_gridColor) { Opacity = 0.4 });
        modelGroup.Children.Add(new GeometryModel3D { Geometry = gridMesh, Material = gridMaterial });

        return modelGroup;
    }

    private MeshGeometry3D CreateSurfaceMesh(List<CornerMeasurement> corners, double cx, double cy,
        double cz, double scale, double exaggeration)
    {
        var mesh = new MeshGeometry3D();

        // Add vertices
        foreach (var corner in corners)
        {
            mesh.Positions.Add(new Point3D(
                (corner.X - cx) * scale,
                (corner.Y - cy) * scale,
                -(corner.CorrectedDepth - cz) * scale * exaggeration));
        }

        // Add triangles (fan triangulation from center)
        if (corners.Count == 4)
        {
            // Quad: two triangles
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);
        }
        else
        {
            // Add centroid
            var center = new Point3D(
                corners.Average(c => (c.X - cx) * scale),
                corners.Average(c => (c.Y - cy) * scale),
                -corners.Average(c => (c.CorrectedDepth - cz) * scale * exaggeration));
            mesh.Positions.Add(center);
            int centerIdx = corners.Count;

            for (int i = 0; i < corners.Count; i++)
            {
                int next = (i + 1) % corners.Count;
                mesh.TriangleIndices.Add(centerIdx);
                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add(next);
            }
        }

        return mesh;
    }

    private MeshGeometry3D CreateEdgeMesh(List<CornerMeasurement> corners, double cx, double cy,
        double cz, double scale, double exaggeration, double radius)
    {
        var builder = new MeshBuilder(false, false);

        for (int i = 0; i < corners.Count; i++)
        {
            int next = (i + 1) % corners.Count;

            var p1 = new Point3D(
                (corners[i].X - cx) * scale,
                (corners[i].Y - cy) * scale,
                -(corners[i].CorrectedDepth - cz) * scale * exaggeration);

            var p2 = new Point3D(
                (corners[next].X - cx) * scale,
                (corners[next].Y - cy) * scale,
                -(corners[next].CorrectedDepth - cz) * scale * exaggeration);

            builder.AddCylinder(p1, p2, radius, 8);
        }

        return builder.ToMesh();
    }

    private MeshGeometry3D CreateSphereMesh(double x, double y, double z, double radius)
    {
        var builder = new MeshBuilder(false, false);
        builder.AddSphere(new Point3D(x, y, z), radius, 12, 12);
        return builder.ToMesh();
    }

    private MeshGeometry3D CreateVectorMesh(InclinationResult result, double exaggeration)
    {
        var builder = new MeshBuilder(false, false);

        double headingRad = result.InclinationHeading * Math.PI / 180.0;
        double length = 0.3;

        double dx = length * Math.Sin(headingRad);
        double dy = length * Math.Cos(headingRad);
        double dz = -length * Math.Sin(result.TotalInclination * Math.PI / 180.0) * exaggeration / 10;

        var start = new Point3D(0, 0, 0);
        var end = new Point3D(dx, dy, dz);

        builder.AddArrow(start, end, 0.015, 0.04, 12);

        return builder.ToMesh();
    }

    private MeshGeometry3D CreateGridMesh(double size, double step, double radius)
    {
        var builder = new MeshBuilder(false, false);

        for (double x = -size; x <= size; x += step)
        {
            builder.AddCylinder(new Point3D(x, -size, 0), new Point3D(x, size, 0), radius, 4);
        }

        for (double y = -size; y <= size; y += step)
        {
            builder.AddCylinder(new Point3D(-size, y, 0), new Point3D(size, y, 0), radius, 4);
        }

        return builder.ToMesh();
    }
}

/// <summary>Helper for viewport camera control</summary>
public static class ViewportHelper
{
    public static void ConfigureViewport(HelixViewport3D viewport)
    {
        viewport.Camera = new PerspectiveCamera
        {
            Position = new Point3D(1.5, 1.5, 1.2),
            LookDirection = new Vector3D(-1.5, -1.5, -1.2),
            UpDirection = new Vector3D(0, 0, 1),
            FieldOfView = 45
        };

        viewport.Background = new SolidColorBrush(Color.FromRgb(26, 29, 35));
        viewport.ShowCoordinateSystem = true;
        viewport.CoordinateSystemLabelForeground = Brushes.White;
    }

    public static void ResetCamera(HelixViewport3D viewport)
    {
        if (viewport.Camera is PerspectiveCamera camera)
        {
            camera.Position = new Point3D(1.5, 1.5, 1.2);
            camera.LookDirection = new Vector3D(-1.5, -1.5, -1.2);
            camera.UpDirection = new Vector3D(0, 0, 1);
        }
    }

    public static void SetTopView(HelixViewport3D viewport)
    {
        if (viewport.Camera is PerspectiveCamera camera)
        {
            camera.Position = new Point3D(0, 0, 2);
            camera.LookDirection = new Vector3D(0, 0, -1);
            camera.UpDirection = new Vector3D(0, 1, 0);
        }
    }

    public static void SetSideView(HelixViewport3D viewport)
    {
        if (viewport.Camera is PerspectiveCamera camera)
        {
            camera.Position = new Point3D(2, 0, 0);
            camera.LookDirection = new Vector3D(-1, 0, 0);
            camera.UpDirection = new Vector3D(0, 0, 1);
        }
    }
}
