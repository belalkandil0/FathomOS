using System;
using SharpDX;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;

namespace FathomOS.Modules.VesselGyroCalibration.Services;

/// <summary>
/// Service for creating 3D visualizations of vessel configuration
/// </summary>
public class Visualization3DService
{
    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D CreateVesselGeometry(float length = 80, float width = 16, float height = 12)
    {
        var builder = new MeshBuilder();
        builder.AddBox(new Vector3(0, 0, height / 2), length, width, height);
        builder.AddBox(new Vector3(-length * 0.2f, 0, height + 4), length * 0.25f, width * 0.6f, 8);
        builder.AddCone(new Vector3(length / 2, 0, height / 2), new Vector3(1, 0, 0), 
                       width / 2, 0, length * 0.15f, true, false, 16);
        return builder.ToMeshGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D CreateSeaPlaneGeometry(float size = 200)
    {
        var builder = new MeshBuilder();
        builder.AddBox(new Vector3(0, 0, -1), size, size, 0.5f);
        return builder.ToMeshGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D CreateArrowGeometry(Vector3 position, float heading, float length = 30)
    {
        var builder = new MeshBuilder();
        float radians = (float)(heading * Math.PI / 180.0);
        var direction = new Vector3((float)Math.Sin(radians), (float)Math.Cos(radians), 0);
        var end = position + direction * length;
        builder.AddCylinder(position, end, 0.5f, 8);
        builder.AddCone(end, direction, 2, 0, 5, true, false, 8);
        return builder.ToMeshGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.LineGeometry3D CreateCompassRoseGeometry(float radius = 50)
    {
        var builder = new LineBuilder();
        builder.AddLine(new Vector3(0, -radius, 0), new Vector3(0, radius, 0));
        builder.AddLine(new Vector3(-radius, 0, 0), new Vector3(radius, 0, 0));
        
        int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(i * 2 * Math.PI / segments);
            float angle2 = (float)((i + 1) * 2 * Math.PI / segments);
            var p1 = new Vector3((float)Math.Cos(angle1) * radius, (float)Math.Sin(angle1) * radius, 0);
            var p2 = new Vector3((float)Math.Cos(angle2) * radius, (float)Math.Sin(angle2) * radius, 0);
            builder.AddLine(p1, p2);
        }
        
        return builder.ToLineGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.LineGeometry3D CreateCOArcGeometry(double coValue, float radius = 40)
    {
        var builder = new LineBuilder();
        int segments = Math.Max(8, (int)Math.Abs(coValue / 5));
        float startAngle = 90;
        float endAngle = (float)(90 - coValue);
        
        for (int i = 0; i < segments; i++)
        {
            float t1 = i / (float)segments;
            float t2 = (i + 1) / (float)segments;
            float angle1 = (float)((startAngle + (endAngle - startAngle) * t1) * Math.PI / 180);
            float angle2 = (float)((startAngle + (endAngle - startAngle) * t2) * Math.PI / 180);
            var p1 = new Vector3((float)Math.Cos(angle1) * radius, (float)Math.Sin(angle1) * radius, 5);
            var p2 = new Vector3((float)Math.Cos(angle2) * radius, (float)Math.Sin(angle2) * radius, 5);
            builder.AddLine(p1, p2);
        }
        
        return builder.ToLineGeometry3D();
    }

    public PhongMaterial CreateVesselMaterial()
    {
        return new PhongMaterial
        {
            DiffuseColor = new Color4(0.3f, 0.3f, 0.35f, 1.0f),
            SpecularColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f),
            SpecularShininess = 20
        };
    }

    public PhongMaterial CreateSeaMaterial()
    {
        return new PhongMaterial
        {
            DiffuseColor = new Color4(0.02f, 0.08f, 0.15f, 0.9f),
            SpecularColor = new Color4(0.1f, 0.1f, 0.1f, 1.0f),
            SpecularShininess = 10
        };
    }

    public PhongMaterial CreateArrowMaterial(bool isReference)
    {
        return new PhongMaterial
        {
            DiffuseColor = isReference 
                ? new Color4(0.2f, 0.6f, 0.2f, 1.0f)
                : new Color4(0.2f, 0.4f, 0.8f, 1.0f),
            SpecularColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f),
            SpecularShininess = 20
        };
    }
    
    /// <summary>
    /// Load vessel model from OBJ file, or create procedural geometry if not found.
    /// Place vessel.obj and vessel.mtl in the Assets folder.
    /// </summary>
    public (HelixToolkit.Wpf.SharpDX.MeshGeometry3D Geometry, PhongMaterial Material) LoadOrCreateVesselModel()
    {
        var loader = new ObjModelLoaderService();
        var (objGeometry, objMaterial) = loader.LoadVesselModel();
        
        if (objGeometry != null && objMaterial != null)
        {
            System.Diagnostics.Debug.WriteLine("Using OBJ vessel model from Assets folder");
            return (objGeometry, objMaterial);
        }
        
        // Fallback to procedural geometry
        System.Diagnostics.Debug.WriteLine("Using procedural vessel geometry (vessel.obj not found in Assets)");
        return (CreateVesselGeometry(), CreateVesselMaterial());
    }
    
    /// <summary>
    /// Check if custom OBJ model is available
    /// </summary>
    public bool HasCustomVesselModel()
    {
        var loader = new ObjModelLoaderService();
        return loader.VesselModelExists();
    }
}
