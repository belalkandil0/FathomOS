// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Converters/Converters.cs
// Purpose: Value converters for XAML data binding
// Note: Copied from SurveyListing module as per integration guide
// ============================================================================

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Converters;

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

/// <summary>
/// Converts boolean to Visibility (true = Collapsed, false = Visible).
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}

/// <summary>
/// Converts null to a default value.
/// </summary>
public class NullToDefaultConverter : IValueConverter
{
    public object DefaultValue { get; set; } = "-";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            return parameter ?? DefaultValue;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts LogEntryType to a color for visual distinction.
/// </summary>
public class LogEntryTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LogEntryType entryType)
            return System.Windows.Media.Brushes.Gray;

        return entryType switch
        {
            // DVR entries - Blue tones
            LogEntryType.DvrRecordingStart => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
            LogEntryType.DvrRecordingEnd => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(41, 128, 185)),
            LogEntryType.DvrImageCaptured => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 188, 156)),
            
            // Position fixes - Green tones
            LogEntryType.PositionFix => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
            LogEntryType.CalibrationFix => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96)),
            LogEntryType.VerificationFix => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 160, 133)),
            LogEntryType.SetEastingNorthing => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 130, 76)),
            
            // NaviPac events - Orange tones
            LogEntryType.NaviPacLoggingStart => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 126, 34)),
            LogEntryType.NaviPacLoggingEnd => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 84, 0)),
            LogEntryType.NaviPacEvent => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)),
            
            // NaviScan events - Purple tones
            LogEntryType.NaviScanLoggingStart => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(155, 89, 182)),
            LogEntryType.NaviScanLoggingEnd => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(142, 68, 173)),
            
            // Waypoints - Teal
            LogEntryType.WaypointAdded => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 188, 212)),
            LogEntryType.WaypointDeleted => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 151, 167)),
            LogEntryType.WaypointModified => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 131, 143)),
            
            // Survey operations - Cyan
            LogEntryType.SurveyLineStart => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 172, 193)),
            LogEntryType.SurveyLineEnd => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 151, 167)),
            
            // Manual entries - White
            LogEntryType.ManualEntry => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189, 195, 199)),
            
            // System events - Gray
            LogEntryType.SystemEvent => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166)),
            
            // Errors - Red
            LogEntryType.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
            LogEntryType.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)),
            
            _ => System.Windows.Media.Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts LogEntryType to an icon character (Material Design Icons).
/// </summary>
public class LogEntryTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LogEntryType entryType)
            return "CircleOutline";

        return entryType switch
        {
            LogEntryType.DvrRecordingStart => "Video",
            LogEntryType.DvrRecordingEnd => "VideoOff",
            LogEntryType.DvrImageCaptured => "Camera",
            LogEntryType.PositionFix => "MapMarker",
            LogEntryType.CalibrationFix => "Crosshairs",
            LogEntryType.VerificationFix => "CheckCircle",
            LogEntryType.SetEastingNorthing => "CrosshairsGps",
            LogEntryType.NaviPacLoggingStart => "Play",
            LogEntryType.NaviPacLoggingEnd => "Stop",
            LogEntryType.NaviPacEvent => "Flag",
            LogEntryType.NaviScanLoggingStart => "PlayCircle",
            LogEntryType.NaviScanLoggingEnd => "StopCircle",
            LogEntryType.WaypointAdded => "MapMarkerPlus",
            LogEntryType.WaypointDeleted => "MapMarkerMinus",
            LogEntryType.WaypointModified => "MapMarkerRadius",
            LogEntryType.SurveyLineStart => "VectorLine",
            LogEntryType.SurveyLineEnd => "VectorLineVariant",
            LogEntryType.ManualEntry => "Pencil",
            LogEntryType.SystemEvent => "Information",
            LogEntryType.Error => "AlertCircle",
            LogEntryType.Warning => "Alert",
            _ => "CircleOutline"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean connection state to status text.
/// </summary>
public class ConnectionStateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
            return isConnected ? "Connected" : "Disconnected";
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean connection state to color.
/// </summary>
public class ConnectionStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
            return isConnected 
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))  // Green
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));  // Red
        return System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Multi-value converter for combining multiple values.
/// </summary>
public class MultiBoolToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0)
            return Visibility.Collapsed;

        bool allTrue = values.All(v => v is bool b && b);
        return allTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts DateTime to time-only string.
/// </summary>
public class DateTimeToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return dt.ToString("HH:mm:ss");
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts file size in bytes to human-readable format.
/// </summary>
public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes)
            return "0 B";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
