using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;

namespace FathomOS.Modules.RovGyroCalibration.Services;

/// <summary>
/// Service for loading OBJ/MTL 3D model files from the Assets folder.
/// Falls back to procedural geometry if files not found.
/// 
/// Place your model files in the Assets folder:
/// - rov.obj (required)
/// - rov.mtl (optional, for materials)
/// </summary>
public class ObjModelLoaderService
{
    private readonly string _assetsPath;
    
    public ObjModelLoaderService()
    {
        // Get the Assets folder path relative to the executing assembly
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
        _assetsPath = Path.Combine(assemblyDir, "Assets");
    }
    
    /// <summary>
    /// Check if the ROV model file exists
    /// </summary>
    public bool RovModelExists()
    {
        var objPath = Path.Combine(_assetsPath, "rov.obj");
        return File.Exists(objPath);
    }
    
    /// <summary>
    /// Load the ROV 3D model from OBJ file
    /// </summary>
    /// <returns>Tuple of (geometry, material) or null if file not found</returns>
    public (MeshGeometry3D? Geometry, PhongMaterial? Material) LoadRovModel()
    {
        var objPath = Path.Combine(_assetsPath, "rov.obj");
        var mtlPath = Path.Combine(_assetsPath, "rov.mtl");
        
        if (!File.Exists(objPath))
        {
            System.Diagnostics.Debug.WriteLine($"ROV OBJ not found at: {objPath}");
            return (null, null);
        }
        
        try
        {
            var (geometry, material) = LoadObjFile(objPath, mtlPath);
            System.Diagnostics.Debug.WriteLine($"Loaded ROV model: {geometry?.Positions?.Count ?? 0} vertices");
            return (geometry, material);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading ROV OBJ: {ex.Message}");
            return (null, null);
        }
    }
    
    /// <summary>
    /// Parse OBJ file and optional MTL file
    /// </summary>
    private (MeshGeometry3D? Geometry, PhongMaterial? Material) LoadObjFile(string objPath, string? mtlPath)
    {
        var positions = new Vector3Collection();
        var normals = new Vector3Collection();
        var textureCoords = new Vector2Collection();
        var indices = new IntCollection();
        
        // Temporary storage for OBJ data
        var tempPositions = new List<Vector3>();
        var tempNormals = new List<Vector3>();
        var tempTexCoords = new List<Vector2>();
        
        // Material info
        string? materialName = null;
        var materials = new Dictionary<string, ObjMaterial>();
        
        // Load MTL file if exists
        if (!string.IsNullOrEmpty(mtlPath) && File.Exists(mtlPath))
        {
            materials = LoadMtlFile(mtlPath);
        }
        
        // Parse OBJ file
        foreach (var line in File.ReadLines(objPath))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;
            
            var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            
            switch (parts[0].ToLowerInvariant())
            {
                case "v": // Vertex position
                    if (parts.Length >= 4)
                    {
                        tempPositions.Add(new Vector3(
                            ParseFloat(parts[1]),
                            ParseFloat(parts[2]),
                            ParseFloat(parts[3])
                        ));
                    }
                    break;
                    
                case "vn": // Vertex normal
                    if (parts.Length >= 4)
                    {
                        tempNormals.Add(new Vector3(
                            ParseFloat(parts[1]),
                            ParseFloat(parts[2]),
                            ParseFloat(parts[3])
                        ));
                    }
                    break;
                    
                case "vt": // Texture coordinate
                    if (parts.Length >= 3)
                    {
                        tempTexCoords.Add(new Vector2(
                            ParseFloat(parts[1]),
                            ParseFloat(parts[2])
                        ));
                    }
                    break;
                    
                case "usemtl": // Material reference
                    if (parts.Length >= 2)
                        materialName = parts[1];
                    break;
                    
                case "f": // Face
                    ParseFace(parts, tempPositions, tempNormals, tempTexCoords,
                              positions, normals, textureCoords, indices);
                    break;
            }
        }
        
        if (positions.Count == 0)
            return (null, null);
        
        // Create geometry
        var geometry = new MeshGeometry3D
        {
            Positions = positions,
            Indices = indices
        };
        
        if (normals.Count == positions.Count)
            geometry.Normals = normals;
        
        if (textureCoords.Count == positions.Count)
            geometry.TextureCoordinates = textureCoords;
        
        // Create material
        PhongMaterial material;
        if (!string.IsNullOrEmpty(materialName) && materials.TryGetValue(materialName, out var objMat))
        {
            material = new PhongMaterial
            {
                DiffuseColor = objMat.DiffuseColor,
                AmbientColor = objMat.AmbientColor,
                SpecularColor = objMat.SpecularColor,
                SpecularShininess = objMat.Shininess
            };
        }
        else
        {
            // Default ROV yellow/orange material
            material = new PhongMaterial
            {
                DiffuseColor = new Color4(0.9f, 0.7f, 0.1f, 1.0f),
                AmbientColor = new Color4(0.3f, 0.2f, 0.05f, 1.0f),
                SpecularColor = new Color4(0.4f, 0.4f, 0.3f, 1.0f),
                SpecularShininess = 25
            };
        }
        
        return (geometry, material);
    }
    
    /// <summary>
    /// Parse a face line (f v1/vt1/vn1 v2/vt2/vn2 v3/vt3/vn3 ...)
    /// Handles triangles and quads (triangulates quads)
    /// </summary>
    private void ParseFace(string[] parts,
                          List<Vector3> tempPositions,
                          List<Vector3> tempNormals,
                          List<Vector2> tempTexCoords,
                          Vector3Collection positions,
                          Vector3Collection normals,
                          Vector2Collection textureCoords,
                          IntCollection indices)
    {
        var faceVertices = new List<int>();
        
        for (int i = 1; i < parts.Length; i++)
        {
            var vertexParts = parts[i].Split('/');
            
            // Position index (required)
            int posIdx = int.Parse(vertexParts[0]) - 1; // OBJ is 1-indexed
            if (posIdx < 0) posIdx = tempPositions.Count + posIdx + 1; // Handle negative indices
            
            // Texture coord index (optional)
            int texIdx = -1;
            if (vertexParts.Length > 1 && !string.IsNullOrEmpty(vertexParts[1]))
            {
                texIdx = int.Parse(vertexParts[1]) - 1;
                if (texIdx < 0) texIdx = tempTexCoords.Count + texIdx + 1;
            }
            
            // Normal index (optional)
            int normIdx = -1;
            if (vertexParts.Length > 2 && !string.IsNullOrEmpty(vertexParts[2]))
            {
                normIdx = int.Parse(vertexParts[2]) - 1;
                if (normIdx < 0) normIdx = tempNormals.Count + normIdx + 1;
            }
            
            // Add vertex data
            int newIndex = positions.Count;
            faceVertices.Add(newIndex);
            
            if (posIdx >= 0 && posIdx < tempPositions.Count)
                positions.Add(tempPositions[posIdx]);
            else
                positions.Add(Vector3.Zero);
            
            if (normIdx >= 0 && normIdx < tempNormals.Count)
                normals.Add(tempNormals[normIdx]);
            else
                normals.Add(Vector3.UnitZ);
            
            if (texIdx >= 0 && texIdx < tempTexCoords.Count)
                textureCoords.Add(tempTexCoords[texIdx]);
            else
                textureCoords.Add(Vector2.Zero);
        }
        
        // Triangulate (fan triangulation for convex polygons)
        for (int i = 1; i < faceVertices.Count - 1; i++)
        {
            indices.Add(faceVertices[0]);
            indices.Add(faceVertices[i]);
            indices.Add(faceVertices[i + 1]);
        }
    }
    
    /// <summary>
    /// Load MTL material file
    /// </summary>
    private Dictionary<string, ObjMaterial> LoadMtlFile(string mtlPath)
    {
        var materials = new Dictionary<string, ObjMaterial>();
        ObjMaterial? currentMaterial = null;
        
        foreach (var line in File.ReadLines(mtlPath))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;
            
            var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            
            switch (parts[0].ToLowerInvariant())
            {
                case "newmtl":
                    if (parts.Length >= 2)
                    {
                        currentMaterial = new ObjMaterial { Name = parts[1] };
                        materials[parts[1]] = currentMaterial;
                    }
                    break;
                    
                case "kd": // Diffuse color
                    if (currentMaterial != null && parts.Length >= 4)
                    {
                        currentMaterial.DiffuseColor = new Color4(
                            ParseFloat(parts[1]),
                            ParseFloat(parts[2]),
                            ParseFloat(parts[3]),
                            1.0f
                        );
                    }
                    break;
                    
                case "ka": // Ambient color
                    if (currentMaterial != null && parts.Length >= 4)
                    {
                        currentMaterial.AmbientColor = new Color4(
                            ParseFloat(parts[1]),
                            ParseFloat(parts[2]),
                            ParseFloat(parts[3]),
                            1.0f
                        );
                    }
                    break;
                    
                case "ks": // Specular color
                    if (currentMaterial != null && parts.Length >= 4)
                    {
                        currentMaterial.SpecularColor = new Color4(
                            ParseFloat(parts[1]),
                            ParseFloat(parts[2]),
                            ParseFloat(parts[3]),
                            1.0f
                        );
                    }
                    break;
                    
                case "ns": // Shininess
                    if (currentMaterial != null && parts.Length >= 2)
                    {
                        currentMaterial.Shininess = ParseFloat(parts[1]);
                    }
                    break;
                    
                case "d": // Transparency
                    if (currentMaterial != null && parts.Length >= 2)
                    {
                        float alpha = ParseFloat(parts[1]);
                        currentMaterial.DiffuseColor = new Color4(
                            currentMaterial.DiffuseColor.Red,
                            currentMaterial.DiffuseColor.Green,
                            currentMaterial.DiffuseColor.Blue,
                            alpha
                        );
                    }
                    break;
            }
        }
        
        return materials;
    }
    
    private static float ParseFloat(string value)
    {
        return float.Parse(value, CultureInfo.InvariantCulture);
    }
    
    /// <summary>
    /// Internal class for storing MTL material data
    /// </summary>
    private class ObjMaterial
    {
        public string Name { get; set; } = "";
        public Color4 DiffuseColor { get; set; } = new Color4(0.8f, 0.8f, 0.8f, 1.0f);
        public Color4 AmbientColor { get; set; } = new Color4(0.2f, 0.2f, 0.2f, 1.0f);
        public Color4 SpecularColor { get; set; } = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
        public float Shininess { get; set; } = 30f;
    }
}
