using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FathomOS.Modules.UsblVerification.Converters;

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

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

public class PassFailToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool pass)
        {
            return pass ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)) // Green
                        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // Red
        }
        return new SolidColorBrush(System.Windows.Media.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))  // Green
                     : new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // Red
        }
        return new SolidColorBrush(System.Windows.Media.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PassFailToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool pass)
            return pass ? "PASS" : "FAIL";
        return "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DoubleToFormattedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            string format = parameter as string ?? "F3";
            return d.ToString(format, CultureInfo.InvariantCulture);
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double d))
            return d;
        return 0.0;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter as string == "Invert";
        bool isNull = value == null || (value is string s && string.IsNullOrEmpty(s));
        
        // Also handle integers - treat 0 as "null" (not visible)
        if (value is int intVal)
            isNull = intVal == 0;
        
        if (invert)
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

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

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ThemeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isDark)
            return isDark ? "WeatherSunny" : "WeatherNight";
        return "WeatherSunny";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class MultiValuePassFailConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null) return System.Windows.Media.Brushes.Gray;
        
        foreach (var val in values)
        {
            if (val is bool b && !b)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // Red
        }
        
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)); // Green
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PassFailToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool pass)
            return pass ? "CheckCircle" : "CloseCircle";
        return "HelpCircle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts current step and parameter to background brush for step indicators
/// </summary>
public class StepToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string paramStr && int.TryParse(paramStr, out int targetStep))
        {
            if (currentStep == targetStep)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(41, 128, 185)); // Current - Blue
            else if (currentStep > targetStep)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)); // Completed - Green
            else
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141)); // Future - Gray
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts step number to TabControl index (step 1 = index 0)
/// </summary>
public class StepToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int step)
            return step - 1;
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index)
            return index + 1;
        return 1;
    }
}

/// <summary>
/// Converts bool to success/warning background
/// </summary>
public class BoolToSuccessBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 46, 204, 113)); // Light green
        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 243, 156, 18)); // Light orange/warning
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to icon name for status
/// </summary>
public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            // Check if parameter contains custom icon names in format "TrueIcon|FalseIcon"
            if (parameter is string param && param.Contains('|'))
            {
                var icons = param.Split('|');
                return b ? icons[0] : icons[1];
            }
            return b ? "CheckCircle" : "AlertCircle";
        }
        return "HelpCircle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool pass/fail to result banner background
/// </summary>
public class BoolToResultBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))  // Green for pass
                     : new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)); // Red for fail
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141)); // Gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts quality score (0-100) to color
/// </summary>
public class QualityScoreToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double score)
        {
            if (score >= 90) return new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));   // Green
            if (score >= 75) return new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96));    // Dark Green
            if (score >= 60) return new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));   // Orange
            if (score >= 40) return new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 126, 34));   // Dark Orange
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));                      // Red
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(127, 140, 141)); // Gray
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts quality score to gauge angle (0-180 degrees)
/// </summary>
public class QualityScoreToAngleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double score)
        {
            // Clamp to 0-100 range and convert to 0-180 degrees
            score = Math.Max(0, Math.Min(100, score));
            return score * 1.8; // 100% = 180 degrees
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts quality score to grade text
/// </summary>
public class QualityScoreToGradeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double score)
        {
            if (score >= 90) return "Excellent";
            if (score >= 75) return "Good";
            if (score >= 60) return "Fair";
            if (score >= 40) return "Poor";
            return "Critical";
        }
        return "N/A";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts hex color string to SolidColorBrush
/// </summary>
public class HexToColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
                }
            }
            catch { }
        }
        return new SolidColorBrush(System.Windows.Media.Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts percentage (0-100) to width for progress bars
/// </summary>
public class PercentToWidthConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 200;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            var max = MaxWidth;
            if (parameter is double maxParam)
                max = maxParam;
            else if (parameter is string paramStr && double.TryParse(paramStr, out double parsed))
                max = parsed;
            
            return (percent / 100.0) * max;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Multi-value converter for displaying point info
/// </summary>
public class PointInfoConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 4)
        {
            var index = values[0];
            var easting = values[1] is double e ? e.ToString("F3") : "-";
            var northing = values[2] is double n ? n.ToString("F3") : "-";
            var depth = values[3] is double d ? d.ToString("F2") : "-";
            
            return $"Point #{index}: E={easting}, N={northing}, Depth={depth}";
        }
        return "No point selected";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to status text (Ready/Pending)
/// </summary>
public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "Ready" : "Pending";
        return "Pending";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to accent background for loaded state
/// </summary>
public class BoolToAccentBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 0, 212, 255)); // Accent with alpha
        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(20, 128, 128, 128)); // Gray with alpha
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to text color based on loaded state
/// </summary>
public class BoolToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(System.Windows.Media.Colors.White);
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184)); // Secondary text
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts processing status to appropriate display
/// </summary>
public class ProcessingStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "NotProcessed" => "Not Processed",
                "Processing" => "Processing...",
                "Pass" => "PASSED",
                "Fail" => "FAILED",
                _ => status
            };
        }
        return "Not Processed";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts processing status to color
/// </summary>
public class ProcessingStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Pass" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),    // Green
                "Fail" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),     // Red
                "Processing" => new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)), // Blue
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184))         // Gray
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
