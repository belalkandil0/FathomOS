namespace FathomOS.Modules.TreeInclination.Converters;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FathomOS.Modules.TreeInclination.Models;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Only return Visible if value is explicitly true
        if (value is bool b && b)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v != Visibility.Visible;
}

/// <summary>Shows element when value is null, hides when not null. Use ConverterParameter="Inverse" to reverse.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        bool invert = parameter?.ToString()?.ToLower() == "inverse";
        
        if (invert)
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        return isNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Shows element when value is NOT null, hides when null</summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Filters collection to exclude closure points - for coordinate entry grids.
/// Closure point shares coordinates with corner 1, only depth differs.
/// </summary>
public class ClosureFilterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<CornerMeasurement> corners)
        {
            return corners.Where(c => !c.IsClosurePoint).ToList();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class QualityStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is QualityStatus status)
        {
            return status switch
            {
                QualityStatus.OK => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                QualityStatus.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                QualityStatus.Failed => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _ => new SolidColorBrush(Color.FromRgb(156, 163, 175))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class QualityStatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is QualityStatus status)
        {
            return status switch
            {
                QualityStatus.OK => new SolidColorBrush(Color.FromArgb(30, 34, 197, 94)),
                QualityStatus.Warning => new SolidColorBrush(Color.FromArgb(30, 245, 158, 11)),
                QualityStatus.Failed => new SolidColorBrush(Color.FromArgb(30, 239, 68, 68)),
                _ => new SolidColorBrush(Colors.Transparent)
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class QualityStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is QualityStatus status)
        {
            return status switch
            {
                QualityStatus.OK => "✓ OK",
                QualityStatus.Warning => "⚠ WARNING",
                QualityStatus.Failed => "✗ FAILED",
                _ => "—"
            };
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToThemeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool isDark && isDark ? "☀️" : "🌙";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class DoubleToFormattedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            string format = parameter as string ?? "F3";
            return d.ToString(format, culture);
        }
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, NumberStyles.Any, culture, out double result))
            return result;
        return 0.0;
    }
}

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null)
            return Enum.Parse(targetType, parameter.ToString()!);
        return Binding.DoNothing;
    }
}

public class NullToDefaultConverter : IValueConverter
{
    public object DefaultValue { get; set; } = "—";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            return parameter ?? DefaultValue;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// Aliases for XAML compatibility
public class StatusToBackgroundConverter : QualityStatusToBackgroundConverter { }
public class StatusToTextConverter : QualityStatusToTextConverter { }

/// <summary>
/// Converts step number and current step to background color for step indicators.
/// Active step = accent color, completed steps = success color, future steps = gray.
/// </summary>
public class StepToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush AccentBrush = new(Color.FromRgb(59, 130, 246));   // Blue
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(34, 197, 94));  // Green
    private static readonly SolidColorBrush InactiveBrush = new(Color.FromRgb(71, 85, 105)); // Gray
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int currentStep || parameter is not string stepParam)
            return InactiveBrush;
            
        if (!int.TryParse(stepParam, out int thisStep))
            return InactiveBrush;
        
        if (thisStep == currentStep)
            return AccentBrush;  // Active step
        else if (thisStep < currentStep)
            return SuccessBrush; // Completed step
        else
            return InactiveBrush; // Future step
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// For status strings like "Pass", "Fail", "Warning" - converts to appropriate color.
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = value?.ToString()?.ToLower() ?? "";
        bool isBackground = parameter?.ToString() == "Background";
        
        return status switch
        {
            "pass" or "ok" or "success" => isBackground 
                ? new SolidColorBrush(Color.FromRgb(34, 197, 94)) 
                : Brushes.Green,
            "warning" or "caution" => isBackground 
                ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) 
                : Brushes.Orange,
            "fail" or "error" or "failed" => isBackground 
                ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) 
                : Brushes.Red,
            _ => isBackground 
                ? new SolidColorBrush(Color.FromRgb(100, 116, 139)) 
                : Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
