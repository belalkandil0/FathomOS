using System;
using System.Collections.Generic;
using Point = System.Windows.Point;

namespace FathomOS.Modules.SurveyListing.Models;

/// <summary>
/// Types of undoable actions in the editor
/// </summary>
public enum UndoActionType
{
    PointDeleted,
    PointAdded,
    PointMoved,
    PointsDeleted,
    PointsAdded,
    PointsMoved,
    LayerCreated,
    LayerDeleted,
    SmoothingApplied,
    PolylineCreated,
    PointExcluded
}

/// <summary>
/// Represents an undoable action in the editor
/// </summary>
public class EditorUndoAction
{
    public UndoActionType ActionType { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    // Data storage for different action types
    public List<EditablePointSnapshot>? PointSnapshots { get; set; }
    public string? LayerName { get; set; }
    public object? AdditionalData { get; set; }
    
    /// <summary>
    /// Create an undo action for point deletion
    /// </summary>
    public static EditorUndoAction CreatePointsDeleted(List<EditablePoint> points)
    {
        return new EditorUndoAction
        {
            ActionType = UndoActionType.PointsDeleted,
            Description = $"Delete {points.Count} point(s)",
            PointSnapshots = points.ConvertAll(p => new EditablePointSnapshot(p))
        };
    }
    
    /// <summary>
    /// Create an undo action for point addition
    /// </summary>
    public static EditorUndoAction CreatePointsAdded(List<EditablePoint> points)
    {
        return new EditorUndoAction
        {
            ActionType = UndoActionType.PointsAdded,
            Description = $"Add {points.Count} point(s)",
            PointSnapshots = points.ConvertAll(p => new EditablePointSnapshot(p))
        };
    }
    
    /// <summary>
    /// Create an undo action for point movement
    /// </summary>
    public static EditorUndoAction CreatePointsMoved(List<EditablePoint> points, List<Point> originalPositions)
    {
        var snapshots = new List<EditablePointSnapshot>();
        for (int i = 0; i < points.Count; i++)
        {
            var snapshot = new EditablePointSnapshot(points[i]);
            snapshot.X = originalPositions[i].X;
            snapshot.Y = originalPositions[i].Y;
            snapshots.Add(snapshot);
        }
        
        return new EditorUndoAction
        {
            ActionType = UndoActionType.PointsMoved,
            Description = $"Move {points.Count} point(s)",
            PointSnapshots = snapshots
        };
    }
    
    /// <summary>
    /// Create an undo action for polyline creation
    /// </summary>
    public static EditorUndoAction CreatePolylineCreated(List<EditablePoint> points, string layerName)
    {
        return new EditorUndoAction
        {
            ActionType = UndoActionType.PolylineCreated,
            Description = $"Create polyline ({points.Count} points)",
            PointSnapshots = points.ConvertAll(p => new EditablePointSnapshot(p)),
            LayerName = layerName
        };
    }
    
    /// <summary>
    /// Create an undo action for layer creation
    /// </summary>
    public static EditorUndoAction CreateLayerCreated(string layerName)
    {
        return new EditorUndoAction
        {
            ActionType = UndoActionType.LayerCreated,
            Description = $"Create layer '{layerName}'",
            LayerName = layerName
        };
    }
    
    /// <summary>
    /// Create an undo action for point exclusion (from generated layers)
    /// </summary>
    public static EditorUndoAction CreatePointExcluded(EditablePoint point, string layerName)
    {
        return new EditorUndoAction
        {
            ActionType = UndoActionType.PointExcluded,
            Description = $"Exclude point from '{layerName}'",
            PointSnapshots = new List<EditablePointSnapshot> { new EditablePointSnapshot(point) },
            LayerName = layerName
        };
    }
}

/// <summary>
/// Snapshot of an editable point for undo/redo
/// </summary>
public class EditablePointSnapshot
{
    public int Index { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double OriginalX { get; set; }
    public double OriginalY { get; set; }
    public double OriginalZ { get; set; }
    public string LayerName { get; set; } = "";
    public bool IsDeleted { get; set; }
    public bool IsExcluded { get; set; }
    public double? ProcessedDepth { get; set; }
    public double? ProcessedAltitude { get; set; }
    
    public EditablePointSnapshot() { }
    
    public EditablePointSnapshot(EditablePoint point)
    {
        Index = point.Index;
        X = point.X;
        Y = point.Y;
        Z = point.Z;
        OriginalX = point.OriginalX;
        OriginalY = point.OriginalY;
        OriginalZ = point.OriginalZ;
        LayerName = point.LayerName;
        IsDeleted = point.IsDeleted;
        IsExcluded = point.IsExcluded;
        ProcessedDepth = point.ProcessedDepth;
        ProcessedAltitude = point.ProcessedAltitude;
    }
    
    /// <summary>
    /// Restore a point from this snapshot
    /// </summary>
    public EditablePoint ToEditablePoint()
    {
        return new EditablePoint
        {
            Index = Index,
            X = X,
            Y = Y,
            Z = Z,
            OriginalX = OriginalX,
            OriginalY = OriginalY,
            OriginalZ = OriginalZ,
            LayerName = LayerName,
            IsDeleted = IsDeleted,
            IsExcluded = IsExcluded,
            ProcessedDepth = ProcessedDepth,
            ProcessedAltitude = ProcessedAltitude
        };
    }
}
