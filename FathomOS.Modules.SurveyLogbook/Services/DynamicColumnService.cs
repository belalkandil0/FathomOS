// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/DynamicColumnService.cs
// Purpose: Manages dynamic column generation for Survey Log based on field config
// Version: 9.0.0
// ============================================================================

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Service for generating dynamic DataGrid columns based on field configuration.
/// Provides column definitions for Survey Log view and export functionality.
/// </summary>
public class DynamicColumnService
{
    #region Column Definitions
    
    /// <summary>
    /// Represents a column definition for the Survey Log DataGrid.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// Header text displayed in the column.
        /// </summary>
        public string Header { get; set; } = string.Empty;
        
        /// <summary>
        /// Property path for data binding (e.g., "Easting", "DynamicFields[MyField]").
        /// </summary>
        public string BindingPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Format string for numeric values (e.g., "N3", "F2").
        /// </summary>
        public string? StringFormat { get; set; }
        
        /// <summary>
        /// Column width (Auto, *, or fixed pixels).
        /// </summary>
        public DataGridLength Width { get; set; } = DataGridLength.Auto;
        
        /// <summary>
        /// Whether this is a core column (always shown) or dynamic.
        /// </summary>
        public bool IsCoreColumn { get; set; }
        
        /// <summary>
        /// Data type for export formatting.
        /// </summary>
        public FieldDataType DataType { get; set; } = FieldDataType.String;
        
        /// <summary>
        /// Field name for dynamic fields.
        /// </summary>
        public string? FieldName { get; set; }
        
        /// <summary>
        /// Unit string for display (e.g., "m", "deg").
        /// </summary>
        public string? Unit { get; set; }
    }
    
    #endregion
    
    #region Core Columns (Always Displayed)
    
    /// <summary>
    /// Gets the core columns that are always displayed in Survey Log.
    /// </summary>
    public static List<ColumnDefinition> GetCoreColumns()
    {
        return new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                Header = "Time",
                BindingPath = "Timestamp",
                StringFormat = "HH:mm:ss",
                Width = new DataGridLength(80),
                IsCoreColumn = true,
                DataType = FieldDataType.DateTime
            },
            new ColumnDefinition
            {
                Header = "Date",
                BindingPath = "Timestamp",
                StringFormat = "dd/MM/yyyy",
                Width = new DataGridLength(90),
                IsCoreColumn = true,
                DataType = FieldDataType.DateTime
            },
            new ColumnDefinition
            {
                Header = "Type",
                BindingPath = "EntryTypeDisplay",
                Width = new DataGridLength(120),
                IsCoreColumn = true,
                DataType = FieldDataType.String
            },
            new ColumnDefinition
            {
                Header = "Description",
                BindingPath = "Description",
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                IsCoreColumn = true,
                DataType = FieldDataType.String
            },
            new ColumnDefinition
            {
                Header = "Source",
                BindingPath = "Source",
                Width = new DataGridLength(100),
                IsCoreColumn = true,
                DataType = FieldDataType.String
            }
        };
    }
    
    #endregion
    
    #region Dynamic Columns from Field Configuration
    
    /// <summary>
    /// Generates column definitions from user-defined field configuration.
    /// </summary>
    /// <param name="fields">List of user-defined field definitions</param>
    /// <returns>List of column definitions for fields marked as ShowInLog</returns>
    public static List<ColumnDefinition> GetDynamicColumns(List<UserFieldDefinition>? fields)
    {
        var columns = new List<ColumnDefinition>();
        
        if (fields == null || fields.Count == 0)
        {
            // Return default position columns if no configuration
            return GetDefaultPositionColumns();
        }
        
        foreach (var field in fields.Where(f => f.ShowInLog).OrderBy(f => f.Position))
        {
            var column = new ColumnDefinition
            {
                Header = field.FieldName,
                FieldName = field.FieldName,
                DataType = field.DataType,
                Unit = field.Unit,
                IsCoreColumn = false
            };
            
            // Use dynamic field binding for user-configured fields
            column.BindingPath = GetBindingPath(field);
            column.StringFormat = GetStringFormat(field);
            column.Width = GetColumnWidth(field.DataType);
            
            columns.Add(column);
        }
        
        return columns;
    }
    
    /// <summary>
    /// Gets the default position columns when no field configuration exists.
    /// </summary>
    public static List<ColumnDefinition> GetDefaultPositionColumns()
    {
        return new List<ColumnDefinition>
        {
            new ColumnDefinition
            {
                Header = "Easting",
                BindingPath = "Easting",
                StringFormat = "N3",
                Width = new DataGridLength(110),
                DataType = FieldDataType.Easting
            },
            new ColumnDefinition
            {
                Header = "Northing",
                BindingPath = "Northing",
                StringFormat = "N3",
                Width = new DataGridLength(120),
                DataType = FieldDataType.Northing
            },
            new ColumnDefinition
            {
                Header = "KP",
                BindingPath = "Kp",
                StringFormat = "N3",
                Width = new DataGridLength(80),
                DataType = FieldDataType.KP
            },
            new ColumnDefinition
            {
                Header = "DCC",
                BindingPath = "Dcc",
                StringFormat = "N2",
                Width = new DataGridLength(70),
                DataType = FieldDataType.DCC
            }
        };
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Gets the appropriate binding path for a field definition.
    /// Maps common field types to SurveyLogEntry properties.
    /// </summary>
    private static string GetBindingPath(UserFieldDefinition field)
    {
        // Map known data types to built-in properties
        return field.DataType switch
        {
            FieldDataType.Easting => "Easting",
            FieldDataType.Northing => "Northing",
            FieldDataType.HeightDepth => "Height",
            FieldDataType.Depth => "Depth",
            FieldDataType.KP => "Kp",
            FieldDataType.DCC => "Dcc",
            FieldDataType.DOL => "DOL",
            FieldDataType.DAL => "DAL",
            FieldDataType.HeadingBearing => "Heading",
            FieldDataType.DateTime => "Timestamp",
            FieldDataType.Latitude => "Latitude",
            FieldDataType.Longitude => "Longitude",
            FieldDataType.Roll => "Roll",
            FieldDataType.Pitch => "Pitch",
            FieldDataType.Heave => "Heave",
            FieldDataType.Speed => "SMG",
            FieldDataType.Course => "CMG",
            FieldDataType.EventNumber => "EventNumber",
            FieldDataType.Age => "Age",
            // For other types, use dynamic field lookup
            _ => $"DynamicFields[{field.FieldName}]"
        };
    }
    
    /// <summary>
    /// Gets the string format for a field based on its data type and decimal places.
    /// </summary>
    private static string? GetStringFormat(UserFieldDefinition field)
    {
        return field.DataType switch
        {
            FieldDataType.Integer or FieldDataType.EventNumber => "N0",
            FieldDataType.Decimal => $"N{field.DecimalPlaces}",
            FieldDataType.Easting or FieldDataType.Northing => $"N{Math.Max(field.DecimalPlaces, 3)}",
            FieldDataType.KP or FieldDataType.DAL => $"N{Math.Max(field.DecimalPlaces, 3)}",
            FieldDataType.DCC or FieldDataType.DOL => $"N{Math.Max(field.DecimalPlaces, 2)}",
            FieldDataType.HeightDepth or FieldDataType.Depth => $"N{Math.Max(field.DecimalPlaces, 2)}",
            FieldDataType.HeadingBearing or FieldDataType.Course => $"N{Math.Max(field.DecimalPlaces, 1)}",
            FieldDataType.Roll or FieldDataType.Pitch or FieldDataType.Heave => $"N{Math.Max(field.DecimalPlaces, 2)}",
            FieldDataType.Speed or FieldDataType.Age => $"N{Math.Max(field.DecimalPlaces, 2)}",
            FieldDataType.Latitude or FieldDataType.Longitude => "N6",
            FieldDataType.DateTime => "HH:mm:ss",
            _ => null
        };
    }
    
    /// <summary>
    /// Gets the appropriate column width for a data type.
    /// </summary>
    private static DataGridLength GetColumnWidth(FieldDataType dataType)
    {
        return dataType switch
        {
            FieldDataType.Easting or FieldDataType.Northing => new DataGridLength(120),
            FieldDataType.Latitude or FieldDataType.Longitude => new DataGridLength(130),
            FieldDataType.KP => new DataGridLength(90),
            FieldDataType.DCC or FieldDataType.DOL => new DataGridLength(80),
            FieldDataType.HeightDepth => new DataGridLength(90),
            FieldDataType.HeadingBearing => new DataGridLength(80),
            FieldDataType.Roll or FieldDataType.Pitch or FieldDataType.Heave => new DataGridLength(70),
            FieldDataType.Speed => new DataGridLength(80),
            FieldDataType.Integer or FieldDataType.EventNumber => new DataGridLength(70),
            FieldDataType.DateTime => new DataGridLength(90),
            FieldDataType.String => new DataGridLength(120),
            _ => new DataGridLength(100)
        };
    }
    
    #endregion
    
    #region DataGrid Column Generation
    
    /// <summary>
    /// Creates a DataGridTextColumn from a column definition.
    /// </summary>
    public static DataGridTextColumn CreateDataGridColumn(ColumnDefinition definition)
    {
        var column = new DataGridTextColumn
        {
            Header = definition.Header,
            Width = definition.Width,
            IsReadOnly = true
        };
        
        // Create binding
        var binding = new Binding(definition.BindingPath);
        
        if (!string.IsNullOrEmpty(definition.StringFormat))
        {
            binding.StringFormat = definition.StringFormat;
        }
        
        // Handle null values gracefully
        binding.TargetNullValue = string.Empty;
        binding.FallbackValue = string.Empty;
        
        column.Binding = binding;
        
        return column;
    }
    
    /// <summary>
    /// Generates all columns for the Survey Log DataGrid.
    /// </summary>
    /// <param name="settings">Application settings containing field configuration</param>
    /// <param name="includeActions">Whether to include the actions column</param>
    /// <returns>List of DataGridColumn objects</returns>
    public static List<DataGridColumn> GenerateAllColumns(ApplicationSettings settings, bool includeActions = true)
    {
        var columns = new List<DataGridColumn>();
        
        // Add core columns
        foreach (var def in GetCoreColumns())
        {
            columns.Add(CreateDataGridColumn(def));
        }
        
        // Add dynamic columns based on field configuration
        var dynamicDefs = GetDynamicColumns(settings.NaviPacFields);
        foreach (var def in dynamicDefs)
        {
            columns.Add(CreateDataGridColumn(def));
        }
        
        return columns;
    }
    
    #endregion
    
    #region Export Helpers
    
    /// <summary>
    /// Gets all column definitions for export purposes.
    /// </summary>
    public static List<ColumnDefinition> GetExportColumns(ApplicationSettings settings)
    {
        var allColumns = new List<ColumnDefinition>();
        allColumns.AddRange(GetCoreColumns());
        allColumns.AddRange(GetDynamicColumns(settings.NaviPacFields));
        return allColumns;
    }
    
    /// <summary>
    /// Gets the export value for a specific column from a log entry.
    /// </summary>
    public static object? GetColumnValue(SurveyLogEntry entry, ColumnDefinition column)
    {
        if (column.IsCoreColumn)
        {
            // Core columns map to direct properties
            return column.BindingPath switch
            {
                "Timestamp" => entry.Timestamp,
                "EntryTypeDisplay" => entry.EntryTypeDisplay,
                "Description" => entry.Description,
                "Source" => entry.Source,
                _ => null
            };
        }
        
        // Check if it's a mapped property (comprehensive list)
        var value = column.BindingPath switch
        {
            "Easting" => (object?)entry.Easting,
            "Northing" => entry.Northing,
            "Kp" => entry.Kp,
            "Dcc" => entry.Dcc,
            "Depth" => entry.Depth,
            "Height" => entry.Height,
            "Heading" => entry.Heading,
            "Latitude" => entry.Latitude,
            "Longitude" => entry.Longitude,
            "Roll" => entry.Roll,
            "Pitch" => entry.Pitch,
            "Heave" => entry.Heave,
            "DOL" => entry.DOL,
            "DAL" => entry.DAL,
            "SMG" => entry.SMG,
            "CMG" => entry.CMG,
            "Age" => entry.Age,
            "EventNumber" => entry.EventNumber,
            _ => null
        };
        
        // If not a mapped property, check dynamic fields
        if (value == null && column.FieldName != null)
        {
            value = entry.GetDynamicField<object>(column.FieldName);
        }
        
        return value;
    }
    
    /// <summary>
    /// Gets the formatted export value for a specific column.
    /// </summary>
    public static string GetFormattedColumnValue(SurveyLogEntry entry, ColumnDefinition column)
    {
        var value = GetColumnValue(entry, column);
        
        if (value == null)
            return string.Empty;
        
        if (!string.IsNullOrEmpty(column.StringFormat) && value is IFormattable formattable)
        {
            return formattable.ToString(column.StringFormat, null);
        }
        
        return value.ToString() ?? string.Empty;
    }
    
    #endregion
}
