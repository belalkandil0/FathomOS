// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/NaviPacDataParser.cs
// Purpose: Parses NaviPac User Defined Output data based on field configuration
// Version: 9.0.0
// ============================================================================

using System.Globalization;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Parses NaviPac User Defined Output (UDO) data strings based on field configuration.
/// Handles various data types and formats defined in the EIVA NaviPac documentation.
/// </summary>
public class NaviPacDataParser
{
    private readonly List<UserFieldDefinition> _fieldDefinitions;
    private readonly string _separator;
    
    /// <summary>
    /// Event raised when a parsing warning occurs.
    /// </summary>
    public event EventHandler<string>? ParsingWarning;
    
    /// <summary>
    /// Creates a new NaviPacDataParser with the specified field configuration.
    /// </summary>
    /// <param name="fieldDefinitions">List of field definitions in order</param>
    /// <param name="separator">Field separator character (default: comma)</param>
    public NaviPacDataParser(List<UserFieldDefinition>? fieldDefinitions, string separator = ",")
    {
        _fieldDefinitions = fieldDefinitions ?? new List<UserFieldDefinition>();
        _separator = separator ?? ",";
    }
    
    #region Parsing Methods
    
    /// <summary>
    /// Parses a data string into a SurveyLogEntry with dynamic field values.
    /// </summary>
    /// <param name="dataString">Raw data string from NaviPac</param>
    /// <returns>Parsed SurveyLogEntry or null if parsing fails</returns>
    public SurveyLogEntry? Parse(string dataString)
    {
        if (string.IsNullOrWhiteSpace(dataString))
            return null;
        
        try
        {
            // Clean the data string (remove CR/LF, trim)
            var cleanData = dataString.Trim().TrimEnd('\r', '\n');
            
            // Split by separator
            var fields = SplitFields(cleanData);
            
            if (fields.Length == 0)
                return null;
            
            // Create new entry
            var entry = new SurveyLogEntry
            {
                EntryType = LogEntryType.NaviPacData,
                Source = "NaviPac UDO",
                RawData = dataString
            };
            
            // Parse each field according to configuration
            for (int i = 0; i < Math.Min(fields.Length, _fieldDefinitions.Count); i++)
            {
                var fieldDef = _fieldDefinitions[i];
                var fieldValue = fields[i].Trim();
                
                ParseField(entry, fieldDef, fieldValue);
            }
            
            // Generate description based on parsed data
            entry.Description = GenerateDescription(entry);
            
            return entry;
        }
        catch (Exception ex)
        {
            OnParsingWarning($"Failed to parse data: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Parses multiple data lines.
    /// </summary>
    /// <param name="dataLines">Array of data strings</param>
    /// <returns>List of parsed entries</returns>
    public List<SurveyLogEntry> ParseMultiple(IEnumerable<string> dataLines)
    {
        var entries = new List<SurveyLogEntry>();
        
        foreach (var line in dataLines)
        {
            var entry = Parse(line);
            if (entry != null)
            {
                entries.Add(entry);
            }
        }
        
        return entries;
    }
    
    #endregion
    
    #region Field Parsing
    
    /// <summary>
    /// Parses a single field value and stores it in the entry.
    /// </summary>
    private void ParseField(SurveyLogEntry entry, UserFieldDefinition fieldDef, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        
        try
        {
            switch (fieldDef.DataType)
            {
                case FieldDataType.DateTime:
                    ParseDateTime(entry, fieldDef, value);
                    break;
                    
                case FieldDataType.Easting:
                    if (TryParseDouble(value, out var easting))
                        entry.Easting = easting;
                    break;
                    
                case FieldDataType.Northing:
                    if (TryParseDouble(value, out var northing))
                        entry.Northing = northing;
                    break;
                    
                case FieldDataType.Latitude:
                case FieldDataType.Longitude:
                    ParseLatLong(entry, fieldDef, value);
                    break;
                    
                case FieldDataType.HeightDepth:
                    if (TryParseDouble(value, out var height))
                        entry.Height = height;
                    break;
                    
                case FieldDataType.Depth:
                    if (TryParseDouble(value, out var depth))
                        entry.Depth = depth;
                    break;
                    
                case FieldDataType.KP:
                    if (TryParseDouble(value, out var kp))
                        entry.Kp = kp;
                    break;
                    
                case FieldDataType.DCC:
                    if (TryParseDouble(value, out var dcc))
                        entry.Dcc = dcc;
                    break;
                    
                case FieldDataType.DOL:
                    if (TryParseDouble(value, out var dol))
                        entry.DOL = dol;
                    break;
                    
                case FieldDataType.DAL:
                    if (TryParseDouble(value, out var dal))
                        entry.DAL = dal;
                    break;
                    
                case FieldDataType.HeadingBearing:
                    if (TryParseDouble(value, out var heading))
                        entry.Heading = heading;
                    break;
                    
                case FieldDataType.Roll:
                    if (TryParseDouble(value, out var roll))
                        entry.Roll = roll;
                    break;
                    
                case FieldDataType.Pitch:
                    if (TryParseDouble(value, out var pitch))
                        entry.Pitch = pitch;
                    break;
                    
                case FieldDataType.Heave:
                    if (TryParseDouble(value, out var heave))
                        entry.Heave = heave;
                    break;
                    
                case FieldDataType.Speed:
                    if (TryParseDouble(value, out var smg))
                        entry.SMG = smg;
                    break;
                    
                case FieldDataType.Course:
                    if (TryParseDouble(value, out var cmg))
                        entry.CMG = cmg;
                    break;
                    
                case FieldDataType.Age:
                    if (TryParseDouble(value, out var age))
                        entry.Age = age;
                    break;
                    
                case FieldDataType.EventNumber:
                    if (int.TryParse(value, out var eventNum))
                        entry.EventNumber = eventNum;
                    break;
                    
                case FieldDataType.Integer:
                    if (int.TryParse(value, out var intVal))
                        entry.SetDynamicField(fieldDef.FieldName, intVal);
                    break;
                    
                case FieldDataType.Decimal:
                    if (TryParseDouble(value, out var decVal))
                        entry.SetDynamicField(fieldDef.FieldName, decVal);
                    break;
                    
                case FieldDataType.String:
                case FieldDataType.Auto:
                default:
                    entry.SetDynamicField(fieldDef.FieldName, value);
                    break;
            }
        }
        catch (Exception ex)
        {
            OnParsingWarning($"Failed to parse field '{fieldDef.FieldName}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Parses date/time values in various formats.
    /// </summary>
    private void ParseDateTime(SurveyLogEntry entry, UserFieldDefinition fieldDef, string value)
    {
        // Try common NaviPac date/time formats
        string[] formats = new[]
        {
            "HH:mm:ss.fff",
            "HH:mm:ss",
            "dd/MM/yyyy",
            "yyyy-MM-dd",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "dd.MM.yyyy HH:mm:ss",
            "HH:mm:ss dd/MM/yyyy",
            "HH:mm:ss dd.MM.yyyy"
        };
        
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, 
                DateTimeStyles.None, out var dateTime))
            {
                // If only time was parsed, combine with today's date
                if (format.StartsWith("HH"))
                {
                    entry.Timestamp = DateTime.Today.Add(dateTime.TimeOfDay);
                }
                else
                {
                    entry.Timestamp = dateTime;
                }
                return;
            }
        }
        
        // Try generic parse as fallback
        if (DateTime.TryParse(value, out var genericDateTime))
        {
            entry.Timestamp = genericDateTime;
        }
        else
        {
            entry.SetDynamicField(fieldDef.FieldName, value);
        }
    }
    
    /// <summary>
    /// Parses latitude/longitude values in various formats.
    /// </summary>
    private void ParseLatLong(SurveyLogEntry entry, UserFieldDefinition fieldDef, string value)
    {
        // Handle DDD°MMM.MMMMMM format
        if (value.Contains('°'))
        {
            var decimalDegrees = ParseDMSToDecimal(value);
            if (decimalDegrees.HasValue)
            {
                entry.SetDynamicField(fieldDef.FieldName, decimalDegrees.Value);
                return;
            }
        }
        
        // Handle decimal degrees
        if (TryParseDouble(value.TrimEnd('N', 'S', 'E', 'W', ' '), out var degrees))
        {
            // Check for direction indicator
            if (value.EndsWith("S", StringComparison.OrdinalIgnoreCase) || 
                value.EndsWith("W", StringComparison.OrdinalIgnoreCase))
            {
                degrees = -Math.Abs(degrees);
            }
            entry.SetDynamicField(fieldDef.FieldName, degrees);
        }
        else
        {
            entry.SetDynamicField(fieldDef.FieldName, value);
        }
    }
    
    /// <summary>
    /// Converts DMS (Degrees Minutes Seconds) format to decimal degrees.
    /// </summary>
    private double? ParseDMSToDecimal(string dms)
    {
        try
        {
            // Remove direction indicator
            var direction = 1.0;
            var cleanDms = dms.Trim();
            if (cleanDms.EndsWith("S", StringComparison.OrdinalIgnoreCase) || 
                cleanDms.EndsWith("W", StringComparison.OrdinalIgnoreCase))
            {
                direction = -1.0;
                cleanDms = cleanDms.TrimEnd('N', 'S', 'E', 'W', 'n', 's', 'e', 'w', ' ');
            }
            else if (cleanDms.EndsWith("N", StringComparison.OrdinalIgnoreCase) || 
                     cleanDms.EndsWith("E", StringComparison.OrdinalIgnoreCase))
            {
                cleanDms = cleanDms.TrimEnd('N', 'S', 'E', 'W', 'n', 's', 'e', 'w', ' ');
            }
            
            // Handle negative sign
            if (cleanDms.StartsWith("-"))
            {
                direction = -1.0;
                cleanDms = cleanDms.Substring(1);
            }
            
            // Split on degree symbol
            var parts = cleanDms.Split('°', '\'', '"', ' ');
            var numericParts = parts.Where(p => !string.IsNullOrWhiteSpace(p))
                                    .Select(p => double.TryParse(p, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : (double?)null)
                                    .Where(d => d.HasValue)
                                    .Select(d => d!.Value)
                                    .ToArray();
            
            if (numericParts.Length >= 1)
            {
                var degrees = numericParts[0];
                var minutes = numericParts.Length > 1 ? numericParts[1] : 0;
                var seconds = numericParts.Length > 2 ? numericParts[2] : 0;
                
                return direction * (Math.Abs(degrees) + minutes / 60.0 + seconds / 3600.0);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Splits the data string by separator, handling quoted strings.
    /// </summary>
    private string[] SplitFields(string data)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        
        foreach (var c in data)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c.ToString() == _separator && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        // Add last field
        fields.Add(current.ToString());
        
        return fields.ToArray();
    }
    
    /// <summary>
    /// Tries to parse a double value, handling various number formats.
    /// </summary>
    private bool TryParseDouble(string value, out double result)
    {
        // Remove any whitespace
        value = value.Trim();
        
        // Try invariant culture first
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            return true;
        
        // Try current culture
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            return true;
        
        result = 0;
        return false;
    }
    
    /// <summary>
    /// Generates a description string based on parsed entry data.
    /// </summary>
    private string GenerateDescription(SurveyLogEntry entry)
    {
        var parts = new List<string>();
        
        if (entry.Easting.HasValue && entry.Northing.HasValue)
        {
            parts.Add($"E:{entry.Easting:N2} N:{entry.Northing:N2}");
        }
        
        if (entry.Kp.HasValue)
        {
            parts.Add($"KP:{entry.Kp:N3}");
        }
        
        if (entry.Depth.HasValue)
        {
            parts.Add($"D:{entry.Depth:N2}");
        }
        
        if (entry.Heading.HasValue)
        {
            parts.Add($"Hdg:{entry.Heading:N1}°");
        }
        
        if (parts.Count == 0)
        {
            return "NaviPac Data";
        }
        
        return string.Join(" | ", parts);
    }
    
    /// <summary>
    /// Raises the ParsingWarning event.
    /// </summary>
    protected virtual void OnParsingWarning(string message)
    {
        ParsingWarning?.Invoke(this, message);
    }
    
    #endregion
    
    #region Static Factory Methods
    
    /// <summary>
    /// Creates a parser from application settings.
    /// </summary>
    public static NaviPacDataParser FromSettings(ApplicationSettings settings)
    {
        return new NaviPacDataParser(
            settings.NaviPacFields,
            settings.NaviPacFieldSeparator ?? ","
        );
    }
    
    /// <summary>
    /// Creates a parser with default field configuration.
    /// </summary>
    public static NaviPacDataParser CreateDefault()
    {
        var defaultFields = UserFieldDefinition.CreateDefaultFieldSet();
        return new NaviPacDataParser(defaultFields, ",");
    }
    
    #endregion
}
