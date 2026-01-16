namespace FathomOS.Modules.NetworkTimeSync.Infrastructure;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FathomOS.Modules.NetworkTimeSync.Enums;

/// <summary>
/// Converts boolean to Visibility.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.ToLower() == "invert";
        bool boolValue = value is bool b && b;
        
        if (invert) boolValue = !boolValue;
        
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.ToLower() == "invert";
        bool result = value is Visibility v && v == Visibility.Visible;
        return invert ? !result : result;
    }
}

/// <summary>
/// Converts SyncStatus to color brush.
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SyncStatus status)
        {
            var colorStr = status switch
            {
                SyncStatus.Synced => "#6CCB5F",            // Green
                SyncStatus.OutOfSync => "#FF6B6B",         // Red
                SyncStatus.Unreachable => "#606060",       // Gray
                SyncStatus.Checking => "#60CDFF",          // Blue
                SyncStatus.Syncing => "#FCE100",           // Yellow
                SyncStatus.Error => "#FF6B6B",             // Red
                SyncStatus.AgentNotInstalled => "#FFA500", // Orange
                _ => "#A0A0A0"                              // Gray
            };

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts SyncStatus to status indicator symbol.
/// </summary>
public class StatusToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SyncStatus status)
        {
            return status switch
            {
                SyncStatus.Synced => "●",
                SyncStatus.OutOfSync => "●",
                SyncStatus.Unreachable => "○",
                SyncStatus.Checking => "◐",
                SyncStatus.Syncing => "◑",
                SyncStatus.Error => "✕",
                SyncStatus.AgentNotInstalled => "◯",
                _ => "○"
            };
        }

        return "○";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts TimeSourceType to display string.
/// </summary>
public class TimeSourceToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSourceType source)
        {
            return source switch
            {
                TimeSourceType.InternetNtp => "Internet NTP Servers",
                TimeSourceType.LocalNtpServer => "Local NTP Server",
                TimeSourceType.HostComputer => "This Computer (Fathom OS Host)",
                TimeSourceType.GpsSerial => "GPS Serial (NMEA)",
                _ => "Unknown"
            };
        }

        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts SyncMode to display string.
/// </summary>
public class SyncModeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SyncMode mode)
        {
            return mode switch
            {
                SyncMode.OneTime => "One-Time Sync",
                SyncMode.Continuous => "Continuous Monitoring",
                _ => "Unknown"
            };
        }

        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverse of BoolToVisibilityConverter.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        return boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

/// <summary>
/// Converts null to Visibility (null = Collapsed, not null = Visible).
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.ToLower() == "invert";
        bool isNotNull = value != null;
        
        if (invert) isNotNull = !isNotNull;
        
        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts count to Visibility (0 = Collapsed, >0 = Visible).
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter?.ToString()?.ToLower() == "invert";
        bool hasItems = value is int count && count > 0;
        
        if (invert) hasItems = !hasItems;
        
        return hasItems ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts string hex color to SolidColorBrush.
/// </summary>
public class HexToColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
            }
            catch
            {
                // Fall through to default
            }
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Multi-value converter for enabling buttons based on selection count.
/// </summary>
public class SelectionCountToEnabledConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 0 && values[0] is int count)
        {
            int minRequired = 1;
            if (parameter != null && int.TryParse(parameter.ToString(), out int parsed))
            {
                minRequired = parsed;
            }
            return count >= minRequired;
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
