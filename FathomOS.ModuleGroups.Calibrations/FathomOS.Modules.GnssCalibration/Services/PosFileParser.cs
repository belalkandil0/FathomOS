using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Multi-format POS file parser supporting RTKLIB, NovAtel, Trimble, and custom CSV formats.
/// Includes auto-detection of format, coordinate system, and time format.
/// </summary>
public class PosFileParser
{
    private readonly List<string> _warnings = new();
    
    // GPS epoch: January 6, 1980
    private static readonly DateTime GpsEpoch = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);
    
    /// <summary>
    /// Parse a POS file with the specified settings.
    /// </summary>
    public PosParseResult Parse(string filePath, PosParseSettings settings)
    {
        _warnings.Clear();
        var result = new PosParseResult();
        
        if (!File.Exists(filePath))
        {
            result.Success = false;
            result.ErrorMessage = $"File not found: {filePath}";
            return result;
        }
        
        try
        {
            var lines = File.ReadAllLines(filePath);
            
            if (lines.Length == 0)
            {
                result.Success = false;
                result.ErrorMessage = "File is empty.";
                return result;
            }
            
            // Auto-detect format if needed
            if (settings.Format == PosFileFormat.AutoDetect)
            {
                settings.Format = DetectFormat(lines);
                result.DetectedFormat = settings.Format;
            }
            else
            {
                result.DetectedFormat = settings.Format;
            }
            
            // Parse based on detected/specified format
            switch (settings.Format)
            {
                case PosFileFormat.RTKLIB:
                    ParseRtklib(lines, settings, result);
                    break;
                case PosFileFormat.NovAtel:
                    ParseNovAtel(lines, settings, result);
                    break;
                case PosFileFormat.Trimble:
                    ParseTrimble(lines, settings, result);
                    break;
                case PosFileFormat.CustomCSV:
                default:
                    ParseCustomCsv(lines, settings, result);
                    break;
            }
            
            // Post-processing
            if (result.Points.Count > 0)
            {
                // Auto-detect coordinate system if needed
                if (settings.CoordinateSystem == PosCoordinateSystem.AutoDetect)
                {
                    result.DetectedCoordinateSystem = DetectCoordinateSystem(result.Points);
                }
                else
                {
                    result.DetectedCoordinateSystem = settings.CoordinateSystem;
                }
                
                // Calculate statistics
                result.StartTime = result.Points.Min(p => p.DateTime);
                result.EndTime = result.Points.Max(p => p.DateTime);
                result.Success = true;
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "No valid data points parsed from file.";
            }
            
            result.Warnings = _warnings.ToList();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Parse error: {ex.Message}";
        }
        
        return result;
    }
    
    /// <summary>
    /// Detect the file format from content.
    /// </summary>
    public PosFileFormat DetectFormat(string[] lines)
    {
        if (lines.Length == 0) return PosFileFormat.CustomCSV;
        
        // Check for RTKLIB format (starts with % header)
        if (lines.Any(l => l.TrimStart().StartsWith("%")))
        {
            return PosFileFormat.RTKLIB;
        }
        
        // Find first data line (skip empty and comment lines)
        var firstDataLine = lines.FirstOrDefault(l => 
            !string.IsNullOrWhiteSpace(l) && 
            !l.TrimStart().StartsWith("#") &&
            !l.TrimStart().StartsWith("%"));
        
        if (string.IsNullOrEmpty(firstDataLine))
            return PosFileFormat.CustomCSV;
        
        // Check for NovAtel format (GPS Week as first field, typically 4 digits ~2000+)
        var commaFields = firstDataLine.Split(',');
        if (commaFields.Length >= 5)
        {
            if (int.TryParse(commaFields[0].Trim(), out int week) && week > 1500 && week < 3000)
            {
                // Looks like GPS week
                return PosFileFormat.NovAtel;
            }
        }
        
        // Check for Trimble format (Date in first column, UTM-like coordinates)
        if (commaFields.Length >= 4)
        {
            // Check if third column looks like Easting (6-7 digit number)
            if (double.TryParse(commaFields[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                if (val > 100000 && val < 1000000) // UTM Easting range
                {
                    return PosFileFormat.Trimble;
                }
            }
        }
        
        // Check for RTKLIB data format (space-separated, starts with date)
        var spaceFields = Regex.Split(firstDataLine.Trim(), @"\s+");
        if (spaceFields.Length >= 5)
        {
            // RTKLIB: YYYY/MM/DD HH:MM:SS.SSS Lat Lon Height ...
            if (Regex.IsMatch(spaceFields[0], @"\d{4}/\d{2}/\d{2}"))
            {
                return PosFileFormat.RTKLIB;
            }
        }
        
        return PosFileFormat.CustomCSV;
    }
    
    /// <summary>
    /// Detect coordinate system from data values.
    /// </summary>
    private PosCoordinateSystem DetectCoordinateSystem(List<PosDataPoint> points)
    {
        if (points.Count == 0) return PosCoordinateSystem.Projected;
        
        var sample = points.First();
        
        // Check if coordinates look like lat/lon (small values, typically -180 to 180)
        if (Math.Abs(sample.Easting) < 180 && Math.Abs(sample.Northing) < 90)
        {
            return PosCoordinateSystem.Geographic;
        }
        
        // Otherwise assume projected (UTM-like, large values)
        return PosCoordinateSystem.Projected;
    }
    
    #region RTKLIB Parser
    
    /// <summary>
    /// Parse RTKLIB format:
    /// %  GPST                  latitude(deg) longitude(deg)  height(m)   Q  ns   sdn(m)   sde(m)   sdu(m)  sdne(m)  sdeu(m)  sdun(m) age(s)  ratio
    /// 2024/12/19 14:30:00.000   59.12345678    5.67890123     45.123    1   12   0.012    0.015    0.025   0.001    0.002    0.001   0.5    99.9
    /// </summary>
    private void ParseRtklib(string[] lines, PosParseSettings settings, PosParseResult result)
    {
        int index = 0;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip header lines (start with %)
            if (trimmed.StartsWith("%"))
            {
                // Extract column headers if present
                if (trimmed.Contains("latitude") || trimmed.Contains("GPST"))
                {
                    result.Headers = Regex.Split(trimmed.TrimStart('%').Trim(), @"\s+").ToList();
                }
                continue;
            }
            
            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;
            
            result.TotalRecords++;
            
            try
            {
                // Split by whitespace
                var fields = Regex.Split(trimmed, @"\s+");
                
                if (fields.Length < 5)
                {
                    _warnings.Add($"Line {result.TotalRecords}: Insufficient fields ({fields.Length})");
                    result.ErrorRecords++;
                    continue;
                }
                
                var point = new PosDataPoint
                {
                    Index = ++index,
                    RawLine = line
                };
                
                // Parse date/time (first two fields: YYYY/MM/DD HH:MM:SS.SSS)
                if (DateTime.TryParse($"{fields[0]} {fields[1]}", CultureInfo.InvariantCulture, 
                    DateTimeStyles.AssumeUniversal, out var dt))
                {
                    point.DateTime = dt;
                }
                
                // Parse coordinates (fields 2, 3, 4: lat, lon, height)
                point.Latitude = ParseDoubleOrNull(fields, 2);
                point.Longitude = ParseDoubleOrNull(fields, 3);
                point.Height = ParseDouble(fields, 4);
                
                // For RTKLIB, coordinates are geographic - store in Easting/Northing too
                // (will be converted later if needed)
                if (point.Latitude.HasValue && point.Longitude.HasValue)
                {
                    point.Easting = point.Longitude.Value;
                    point.Northing = point.Latitude.Value;
                }
                
                // Quality indicator (field 5)
                if (fields.Length > 5 && int.TryParse(fields[5], out int q))
                {
                    point.Quality = (PosSolutionQuality)q;
                }
                
                // Satellite count (field 6)
                if (fields.Length > 6 && int.TryParse(fields[6], out int ns))
                {
                    point.SatelliteCount = ns;
                }
                
                // Sigma values (fields 7, 8, 9: sdn, sde, sdu)
                if (fields.Length > 7) point.SigmaNorth = ParseDoubleOrNull(fields, 7);
                if (fields.Length > 8) point.SigmaEast = ParseDoubleOrNull(fields, 8);
                if (fields.Length > 9) point.SigmaUp = ParseDoubleOrNull(fields, 9);
                
                // Apply quality filter
                if (ApplyQualityFilter(point, settings, result))
                {
                    result.Points.Add(point);
                }
            }
            catch (Exception ex)
            {
                _warnings.Add($"Line {result.TotalRecords}: Parse error - {ex.Message}");
                result.ErrorRecords++;
            }
        }
        
        result.DetectedTimeFormat = PosTimeFormat.UtcDateTime;
    }
    
    #endregion
    
    #region NovAtel Parser
    
    /// <summary>
    /// Parse NovAtel/Waypoint format:
    /// GPSWeek,GPSSec,Lat,Lon,Height,sigN,sigE,sigU,Q,ns,...
    /// 2345,356400.000,59.12345678,5.67890123,45.123,0.012,0.015,0.025,1,12
    /// </summary>
    private void ParseNovAtel(string[] lines, PosParseSettings settings, PosParseResult result)
    {
        int index = 0;
        bool headerSkipped = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip empty and comment lines
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;
            
            // Check for header line
            if (!headerSkipped && (trimmed.Contains("GPSWeek") || trimmed.Contains("Week") || 
                                   trimmed.Contains("Lat") || !char.IsDigit(trimmed[0])))
            {
                result.Headers = trimmed.Split(settings.Delimiter).Select(h => h.Trim()).ToList();
                headerSkipped = true;
                continue;
            }
            
            result.TotalRecords++;
            
            try
            {
                var fields = trimmed.Split(settings.Delimiter);
                
                if (fields.Length < 5)
                {
                    _warnings.Add($"Line {result.TotalRecords}: Insufficient fields ({fields.Length})");
                    result.ErrorRecords++;
                    continue;
                }
                
                var point = new PosDataPoint
                {
                    Index = ++index,
                    RawLine = line
                };
                
                // Parse GPS Week and Seconds
                if (int.TryParse(fields[0].Trim(), out int gpsWeek))
                {
                    point.GpsWeek = gpsWeek;
                }
                
                if (double.TryParse(fields[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double gpsSec))
                {
                    point.GpsSecondsOfWeek = gpsSec;
                }
                
                // Convert GPS time to DateTime
                if (point.GpsWeek.HasValue && point.GpsSecondsOfWeek.HasValue)
                {
                    point.DateTime = GpsToUtc(point.GpsWeek.Value, point.GpsSecondsOfWeek.Value);
                }
                
                // Parse coordinates (fields 2, 3, 4: lat, lon, height)
                point.Latitude = ParseDoubleOrNull(fields, 2);
                point.Longitude = ParseDoubleOrNull(fields, 3);
                point.Height = ParseDouble(fields, 4);
                
                if (point.Latitude.HasValue && point.Longitude.HasValue)
                {
                    point.Easting = point.Longitude.Value;
                    point.Northing = point.Latitude.Value;
                }
                
                // Sigma values (fields 5, 6, 7 if present)
                if (fields.Length > 5) point.SigmaNorth = ParseDoubleOrNull(fields, 5);
                if (fields.Length > 6) point.SigmaEast = ParseDoubleOrNull(fields, 6);
                if (fields.Length > 7) point.SigmaUp = ParseDoubleOrNull(fields, 7);
                
                // Quality (field 8 if present)
                if (fields.Length > 8 && int.TryParse(fields[8].Trim(), out int q))
                {
                    point.Quality = (PosSolutionQuality)q;
                }
                
                // Satellite count (field 9 if present)
                if (fields.Length > 9 && int.TryParse(fields[9].Trim(), out int ns))
                {
                    point.SatelliteCount = ns;
                }
                
                if (ApplyQualityFilter(point, settings, result))
                {
                    result.Points.Add(point);
                }
            }
            catch (Exception ex)
            {
                _warnings.Add($"Line {result.TotalRecords}: Parse error - {ex.Message}");
                result.ErrorRecords++;
            }
        }
        
        result.DetectedTimeFormat = PosTimeFormat.GpsWeekSeconds;
    }
    
    #endregion
    
    #region Trimble Parser
    
    /// <summary>
    /// Parse Trimble/Leica format:
    /// Date,Time,Easting,Northing,Height,Code,Description,...
    /// 2024/12/19,14:30:00.000,500000.123,6500000.456,45.123,GPS,Point1
    /// </summary>
    private void ParseTrimble(string[] lines, PosParseSettings settings, PosParseResult result)
    {
        int index = 0;
        bool headerSkipped = false;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;
            
            // Check for header
            if (!headerSkipped && (trimmed.Contains("Date") || trimmed.Contains("Easting") || 
                                   trimmed.Contains("Time") || !char.IsDigit(trimmed[0])))
            {
                result.Headers = trimmed.Split(settings.Delimiter).Select(h => h.Trim()).ToList();
                headerSkipped = true;
                continue;
            }
            
            result.TotalRecords++;
            
            try
            {
                var fields = trimmed.Split(settings.Delimiter);
                
                if (fields.Length < 4)
                {
                    _warnings.Add($"Line {result.TotalRecords}: Insufficient fields ({fields.Length})");
                    result.ErrorRecords++;
                    continue;
                }
                
                var point = new PosDataPoint
                {
                    Index = ++index,
                    RawLine = line
                };
                
                // Parse date/time (first two fields)
                string dateTimeStr = fields.Length > 1 && fields[1].Contains(":") 
                    ? $"{fields[0]} {fields[1]}" 
                    : fields[0];
                
                if (DateTime.TryParse(dateTimeStr, CultureInfo.InvariantCulture, 
                    DateTimeStyles.AssumeUniversal, out var dt))
                {
                    point.DateTime = dt;
                }
                
                // Determine column offset (if time is separate column)
                int offset = fields.Length > 1 && fields[1].Contains(":") ? 2 : 1;
                
                // Parse coordinates (Easting, Northing, Height)
                point.Easting = ParseDouble(fields, offset);
                point.Northing = ParseDouble(fields, offset + 1);
                point.Height = ParseDouble(fields, offset + 2);
                
                // Sigma values if present
                if (fields.Length > offset + 3) point.SigmaEast = ParseDoubleOrNull(fields, offset + 3);
                if (fields.Length > offset + 4) point.SigmaNorth = ParseDoubleOrNull(fields, offset + 4);
                if (fields.Length > offset + 5) point.SigmaUp = ParseDoubleOrNull(fields, offset + 5);
                
                if (ApplyQualityFilter(point, settings, result))
                {
                    result.Points.Add(point);
                }
            }
            catch (Exception ex)
            {
                _warnings.Add($"Line {result.TotalRecords}: Parse error - {ex.Message}");
                result.ErrorRecords++;
            }
        }
        
        result.DetectedTimeFormat = PosTimeFormat.UtcDateTime;
        result.DetectedCoordinateSystem = PosCoordinateSystem.Projected;
    }
    
    #endregion
    
    #region Custom CSV Parser
    
    /// <summary>
    /// Parse custom CSV format using column indices from settings.
    /// </summary>
    private void ParseCustomCsv(string[] lines, PosParseSettings settings, PosParseResult result)
    {
        int index = 0;
        int headerLinesSkipped = 0;
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;
            
            if (!string.IsNullOrEmpty(settings.CommentPrefix) && trimmed.StartsWith(settings.CommentPrefix))
                continue;
            
            // Skip header lines
            if (headerLinesSkipped < settings.HeaderLines)
            {
                result.Headers = trimmed.Split(settings.Delimiter).Select(h => h.Trim()).ToList();
                headerLinesSkipped++;
                continue;
            }
            
            result.TotalRecords++;
            
            try
            {
                var fields = trimmed.Split(settings.Delimiter);
                
                var point = new PosDataPoint
                {
                    Index = ++index,
                    RawLine = line
                };
                
                // Parse time based on format
                point.DateTime = ParseTimeByFormat(fields, settings);
                
                // Parse coordinates
                if (settings.LatOrEastColumnIndex >= 0)
                    point.Easting = ParseDouble(fields, settings.LatOrEastColumnIndex);
                if (settings.LonOrNorthColumnIndex >= 0)
                    point.Northing = ParseDouble(fields, settings.LonOrNorthColumnIndex);
                if (settings.HeightColumnIndex >= 0)
                    point.Height = ParseDouble(fields, settings.HeightColumnIndex);
                
                // If coordinates look geographic, also set Lat/Lon
                if (Math.Abs(point.Easting) <= 180 && Math.Abs(point.Northing) <= 90)
                {
                    point.Longitude = point.Easting;
                    point.Latitude = point.Northing;
                }
                
                // Parse quality/accuracy
                if (settings.QualityColumnIndex >= 0 && 
                    int.TryParse(GetField(fields, settings.QualityColumnIndex), out int q))
                {
                    point.Quality = (PosSolutionQuality)q;
                }
                
                if (settings.SatCountColumnIndex >= 0 &&
                    int.TryParse(GetField(fields, settings.SatCountColumnIndex), out int ns))
                {
                    point.SatelliteCount = ns;
                }
                
                point.SigmaNorth = settings.SigmaNorthColumnIndex >= 0 
                    ? ParseDoubleOrNull(fields, settings.SigmaNorthColumnIndex) : null;
                point.SigmaEast = settings.SigmaEastColumnIndex >= 0 
                    ? ParseDoubleOrNull(fields, settings.SigmaEastColumnIndex) : null;
                point.SigmaUp = settings.SigmaUpColumnIndex >= 0 
                    ? ParseDoubleOrNull(fields, settings.SigmaUpColumnIndex) : null;
                
                if (point.IsValid && ApplyQualityFilter(point, settings, result))
                {
                    result.Points.Add(point);
                }
            }
            catch (Exception ex)
            {
                _warnings.Add($"Line {result.TotalRecords}: Parse error - {ex.Message}");
                result.ErrorRecords++;
            }
        }
        
        result.DetectedTimeFormat = settings.TimeFormat;
    }
    
    #endregion
    
    #region Helper Methods
    
    private DateTime ParseTimeByFormat(string[] fields, PosParseSettings settings)
    {
        try
        {
            switch (settings.TimeFormat)
            {
                case PosTimeFormat.GpsWeekSeconds:
                    if (settings.GpsWeekColumnIndex >= 0 && settings.GpsSecColumnIndex >= 0)
                    {
                        int week = int.Parse(GetField(fields, settings.GpsWeekColumnIndex));
                        double sec = double.Parse(GetField(fields, settings.GpsSecColumnIndex), 
                            CultureInfo.InvariantCulture);
                        return GpsToUtc(week, sec);
                    }
                    break;
                    
                case PosTimeFormat.UnixTimestamp:
                    if (settings.DateColumnIndex >= 0)
                    {
                        double unix = double.Parse(GetField(fields, settings.DateColumnIndex),
                            CultureInfo.InvariantCulture);
                        return DateTimeOffset.FromUnixTimeSeconds((long)unix).UtcDateTime
                            .AddMilliseconds((unix % 1) * 1000);
                    }
                    break;
                    
                case PosTimeFormat.Iso8601:
                case PosTimeFormat.UtcDateTime:
                default:
                    string dateStr = settings.DateColumnIndex >= 0 
                        ? GetField(fields, settings.DateColumnIndex) : "";
                    string timeStr = settings.TimeColumnIndex >= 0 
                        ? GetField(fields, settings.TimeColumnIndex) : "";
                    
                    string combined = !string.IsNullOrEmpty(timeStr) 
                        ? $"{dateStr} {timeStr}" : dateStr;
                    
                    if (DateTime.TryParse(combined, CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var dt))
                    {
                        return dt;
                    }
                    break;
            }
        }
        catch 
        { 
            // Parsing failed - MinValue signals invalid time
        }
        
        return DateTime.MinValue;
    }
    
    /// <summary>
    /// Convert GPS Week and Seconds of Week to UTC DateTime.
    /// </summary>
    public static DateTime GpsToUtc(int gpsWeek, double secondsOfWeek)
    {
        // GPS epoch + weeks + seconds
        // Note: GPS time does not include leap seconds, but we'll ignore that for simplicity
        var gpsTime = GpsEpoch
            .AddDays(gpsWeek * 7)
            .AddSeconds(secondsOfWeek);
        
        return gpsTime;
    }
    
    /// <summary>
    /// Convert UTC DateTime to GPS Week and Seconds of Week.
    /// </summary>
    public static (int Week, double Seconds) UtcToGps(DateTime utc)
    {
        var diff = utc - GpsEpoch;
        int week = (int)(diff.TotalDays / 7);
        double seconds = diff.TotalSeconds - (week * 7 * 86400);
        return (week, seconds);
    }
    
    private bool ApplyQualityFilter(PosDataPoint point, PosParseSettings settings, PosParseResult result)
    {
        // Quality filter
        if (settings.MinQuality > 0 && (int)point.Quality > settings.MinQuality)
        {
            point.IsExcluded = true;
            point.ExclusionReason = $"Quality {point.Quality} below minimum";
            result.FilteredRecords++;
            return false;
        }
        
        // Accuracy filter
        if (settings.MaxHorizontalAccuracy > 0 && point.HorizontalAccuracy.HasValue &&
            point.HorizontalAccuracy.Value > settings.MaxHorizontalAccuracy)
        {
            point.IsExcluded = true;
            point.ExclusionReason = $"Horizontal accuracy {point.HorizontalAccuracy:F3}m exceeds limit";
            result.FilteredRecords++;
            return false;
        }
        
        // Satellite filter
        if (settings.MinSatellites > 0 && point.SatelliteCount.HasValue &&
            point.SatelliteCount.Value < settings.MinSatellites)
        {
            point.IsExcluded = true;
            point.ExclusionReason = $"Satellite count {point.SatelliteCount} below minimum";
            result.FilteredRecords++;
            return false;
        }
        
        return true;
    }
    
    private string GetField(string[] fields, int index)
    {
        return index >= 0 && index < fields.Length ? fields[index].Trim() : "";
    }
    
    private double ParseDouble(string[] fields, int index)
    {
        if (index < 0 || index >= fields.Length)
            return double.NaN;
        
        var value = fields[index].Trim();
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        
        return double.NaN;
    }
    
    private double? ParseDoubleOrNull(string[] fields, int index)
    {
        var value = ParseDouble(fields, index);
        return double.IsNaN(value) ? null : value;
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// Get column headers from a file without full parsing.
    /// </summary>
    public List<string> GetColumnHeaders(string filePath, PosParseSettings? settings = null)
    {
        if (!File.Exists(filePath))
            return new List<string>();
        
        try
        {
            var lines = File.ReadLines(filePath).Take(20).ToArray();
            var format = settings?.Format ?? DetectFormat(lines);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;
                
                // For RTKLIB, look for % header
                if (format == PosFileFormat.RTKLIB && trimmed.StartsWith("%"))
                {
                    return Regex.Split(trimmed.TrimStart('%').Trim(), @"\s+").ToList();
                }
                
                // For other formats, return first non-empty, non-numeric line
                var delimiter = settings?.Delimiter ?? ',';
                var fields = trimmed.Split(delimiter);
                
                // If first field doesn't start with a digit, it's probably a header
                if (fields.Length > 0 && !char.IsDigit(fields[0].TrimStart()[0]))
                {
                    return fields.Select(f => f.Trim()).ToList();
                }
            }
        }
        catch 
        { 
            // File read error - return empty headers
        }
        
        return new List<string>();
    }
    
    /// <summary>
    /// Get a preview of the file data (first N rows).
    /// </summary>
    public List<string[]> GetPreview(string filePath, int maxRows = 10, char delimiter = ',')
    {
        var preview = new List<string[]>();
        
        if (!File.Exists(filePath))
            return preview;
        
        try
        {
            var lines = File.ReadLines(filePath).Take(maxRows + 5);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Try different delimiters if comma doesn't work well
                var fields = line.Split(delimiter);
                if (fields.Length < 3)
                {
                    fields = Regex.Split(line.Trim(), @"\s+");
                }
                
                preview.Add(fields);
                
                if (preview.Count >= maxRows)
                    break;
            }
        }
        catch 
        { 
            // File read error - return whatever preview we have
        }
        
        return preview;
    }
    
    #endregion
}
