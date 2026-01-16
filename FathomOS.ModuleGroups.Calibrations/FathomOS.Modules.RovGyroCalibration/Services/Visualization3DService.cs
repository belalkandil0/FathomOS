using System;
using SharpDX;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using FathomOS.Modules.RovGyroCalibration.Models;

namespace FathomOS.Modules.RovGyroCalibration.Services;

/// <summary>
/// Service for creating 3D visualizations of ROV/Vessel configuration
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

    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D CreateRovGeometry(float length = 4, float width = 2.5f, float height = 1.8f)
    {
        var builder = new MeshBuilder();
        builder.AddBox(new Vector3(0, 0, 0), length, width, height);
        builder.AddSphere(new Vector3(length / 2 - 0.3f, 0, 0.3f), 0.4f, 12, 12);
        builder.AddCylinder(new Vector3(-length / 2 + 0.3f, width / 2, 0), 
                           new Vector3(-length / 2 + 0.3f, width / 2 + 0.5f, 0), 0.2f, 8);
        builder.AddCylinder(new Vector3(-length / 2 + 0.3f, -width / 2, 0), 
                           new Vector3(-length / 2 + 0.3f, -width / 2 - 0.5f, 0), 0.2f, 8);
        return builder.ToMeshGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D CreateSeaPlaneGeometry(float size = 200)
    {
        var builder = new MeshBuilder();
        builder.AddBox(new Vector3(0, 0, -1), size, size, 0.5f);
        return builder.ToMeshGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.LineGeometry3D CreateBaselineGeometry(RovConfiguration config)
    {
        return CreateBaselineGeometry(config.BaselineDistance, config.BaselineSide);
    }

    public HelixToolkit.Wpf.SharpDX.LineGeometry3D CreateBaselineGeometry(double distance, BaselineSide side)
    {
        var builder = new LineBuilder();
        float baselineLength = (float)distance;
        
        // Position baseline based on side
        float offsetY = side == BaselineSide.Port ? baselineLength : 
                        side == BaselineSide.Starboard ? -baselineLength : 0;
        float offsetX = side == BaselineSide.Forward ? baselineLength :
                        side == BaselineSide.Aft ? -baselineLength : 0;
        
        // Draw baseline line with markers
        if (side == BaselineSide.Port || side == BaselineSide.Starboard)
        {
            // Horizontal baseline (Port/Starboard)
            builder.AddLine(new Vector3(30, offsetY, -20), new Vector3(30 + 20, offsetY, -20));
            // End markers
            builder.AddLine(new Vector3(30, offsetY - 2, -20), new Vector3(30, offsetY + 2, -20));
            builder.AddLine(new Vector3(30 + 20, offsetY - 2, -20), new Vector3(30 + 20, offsetY + 2, -20));
        }
        else
        {
            // Vertical baseline (Forward/Aft)
            builder.AddLine(new Vector3(offsetX, 0, -20), new Vector3(offsetX, 20, -20));
            // End markers
            builder.AddLine(new Vector3(offsetX - 2, 0, -20), new Vector3(offsetX + 2, 0, -20));
            builder.AddLine(new Vector3(offsetX - 2, 20, -20), new Vector3(offsetX + 2, 20, -20));
        }
        
        return builder.ToLineGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.LineGeometry3D CreateCompassRoseGeometry(float radius = 60)
    {
        var builder = new LineBuilder();
        
        // Main directions
        builder.AddLine(new Vector3(0, -radius, 0), new Vector3(0, radius, 0)); // N-S
        builder.AddLine(new Vector3(-radius, 0, 0), new Vector3(radius, 0, 0)); // E-W
        
        // Circle
        int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(i * 2 * Math.PI / segments);
            float angle2 = (float)((i + 1) * 2 * Math.PI / segments);
            builder.AddLine(
                new Vector3((float)Math.Cos(angle1) * radius, (float)Math.Sin(angle1) * radius, 0),
                new Vector3((float)Math.Cos(angle2) * radius, (float)Math.Sin(angle2) * radius, 0));
        }
        
        return builder.ToLineGeometry3D();
    }

    public HelixToolkit.Wpf.SharpDX.LineGeometry3D CreateCOArcGeometry(double coAngle, float radius)
    {
        var builder = new LineBuilder();
        int segments = Math.Max(8, (int)Math.Abs(coAngle / 3));
        
        float startAngle = 90; // North
        float endAngle = (float)(90 - coAngle);
        
        for (int i = 0; i < segments; i++)
        {
            float t1 = i / (float)segments;
            float t2 = (i + 1) / (float)segments;
            float angle1 = (float)((startAngle + (endAngle - startAngle) * t1) * Math.PI / 180);
            float angle2 = (float)((startAngle + (endAngle - startAngle) * t2) * Math.PI / 180);
            var p1 = new Vector3((float)Math.Cos(angle1) * radius, (float)Math.Sin(angle1) * radius, 15);
            var p2 = new Vector3((float)Math.Cos(angle2) * radius, (float)Math.Sin(angle2) * radius, 15);
            builder.AddLine(p1, p2);
        }
        
        return builder.ToLineGeometry3D();
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

    public HelixToolkit.Wpf.SharpDX.LineGeometry3D CreateThetaArcGeometry(double theta, float radius = 25)
    {
        var builder = new LineBuilder();
        int segments = Math.Max(8, (int)Math.Abs(theta / 5));
        float startAngle = 90;
        float endAngle = (float)(90 - theta);
        
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
        
        float rad1 = (float)(startAngle * Math.PI / 180);
        float rad2 = (float)(endAngle * Math.PI / 180);
        builder.AddLine(new Vector3(0, 0, 5), new Vector3((float)Math.Cos(rad1) * radius, (float)Math.Sin(rad1) * radius, 5));
        builder.AddLine(new Vector3(0, 0, 5), new Vector3((float)Math.Cos(rad2) * radius, (float)Math.Sin(rad2) * radius, 5));
        return builder.ToLineGeometry3D();
    }

    public Vector3 CalculateRovPosition(RovConfiguration config, float scale = 5)
    {
        float x = config.BaselineSide == BaselineSide.Forward ? 50 : -50;
        return new Vector3(x, 0, -20);
    }

    public SharpDX.Matrix CalculateRovTransform(RovConfiguration config, float heading)
    {
        var position = CalculateRovPosition(config);
        float radians = (float)(heading * Math.PI / 180.0);
        var rotation = SharpDX.Matrix.RotationZ(radians);
        var translation = SharpDX.Matrix.Translation(position);
        float facingOffset = config.FacingDirection == RovFacingDirection.Aft ? 180 : 0;
        var facingRotation = SharpDX.Matrix.RotationZ((float)(facingOffset * Math.PI / 180.0));
        return facingRotation * rotation * translation;
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

    public PhongMaterial CreateRovMaterial()
    {
        return new PhongMaterial
        {
            DiffuseColor = new Color4(1.0f, 0.7f, 0.0f, 1.0f),
            SpecularColor = new Color4(0.3f, 0.3f, 0.3f, 1.0f),
            SpecularShininess = 30
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

    public PhongMaterial CreateArrowMaterial(bool isVessel)
    {
        return new PhongMaterial
        {
            DiffuseColor = isVessel 
                ? new Color4(0.2f, 0.6f, 0.2f, 1.0f)
                : new Color4(0.2f, 0.4f, 0.8f, 1.0f),
            SpecularColor = new Color4(0.2f, 0.2f, 0.2f, 1.0f),
            SpecularShininess = 20
        };
    }
    
    /// <summary>
    /// Load ROV model from OBJ file, or create procedural geometry if not found.
    /// Place rov.obj and rov.mtl in the Assets folder.
    /// </summary>
    public (HelixToolkit.Wpf.SharpDX.MeshGeometry3D Geometry, PhongMaterial Material) LoadOrCreateRovModel()
    {
        var loader = new ObjModelLoaderService();
        var (objGeometry, objMaterial) = loader.LoadRovModel();
        
        if (objGeometry != null && objMaterial != null)
        {
            System.Diagnostics.Debug.WriteLine("Using OBJ ROV model from Assets folder");
            return (objGeometry, objMaterial);
        }
        
        // Fallback to procedural geometry
        System.Diagnostics.Debug.WriteLine("Using procedural ROV geometry (rov.obj not found in Assets)");
        return (CreateRovGeometry(), CreateRovMaterial());
    }
    
    /// <summary>
    /// Load vessel model from OBJ file, or create procedural geometry if not found.
    /// Place vessel.obj and vessel.mtl in the Assets folder.
    /// </summary>
    public (HelixToolkit.Wpf.SharpDX.MeshGeometry3D Geometry, PhongMaterial Material) LoadOrCreateVesselModel()
    {
        var loader = new ObjModelLoaderService();
        var (objGeometry, objMaterial) = loader.LoadRovModel(); // Try loading vessel model
        
        // Fallback to procedural geometry
        System.Diagnostics.Debug.WriteLine("Using procedural vessel geometry");
        return (CreateVesselGeometry(), CreateVesselMaterial());
    }
    
    /// <summary>
    /// Check if custom OBJ model is available
    /// </summary>
    public bool HasCustomRovModel()
    {
        var loader = new ObjModelLoaderService();
        return loader.RovModelExists();
    }
}
