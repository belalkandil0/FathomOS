using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FathomOS.Modules.RovGyroCalibration.Models;

namespace FathomOS.Modules.RovGyroCalibration.Converters;

#region Visibility Converters

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
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

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value == null ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => !string.IsNullOrEmpty(value?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count) return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

#endregion

#region Bool Converters

public class BoolToStringConverter : IValueConverter
{
    public string TrueValue { get; set; } = "Yes";
    public string FalseValue { get; set; } = "No";
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? TrueValue : FalseValue;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

public class BoolToDoubleConverter : IValueConverter
{
    public double TrueValue { get; set; } = 1.0;
    public double FalseValue { get; set; } = 0.5;
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? TrueValue : FalseValue;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

#endregion

#region Color Converters - FULLY QUALIFIED

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b 
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PointStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PointStatus status)
        {
            return status switch
            {
                PointStatus.Accepted => new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                PointStatus.Rejected => new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                PointStatus.Pending => new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166))
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PointStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PointStatus status)
        {
            return status switch
            {
                PointStatus.Accepted => new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                PointStatus.Rejected => new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                PointStatus.Pending => new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166))
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class QcStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is QcStatus status)
        {
            return status switch
            {
                QcStatus.Pass => new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                QcStatus.Warning => new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)),
                QcStatus.Fail => new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166))
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class QcStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is QcStatus status)
        {
            return status switch
            {
                QcStatus.Pass => new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                QcStatus.Warning => new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)),
                QcStatus.Fail => new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166))
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ValueToColorConverter : IValueConverter
{
    public double Threshold { get; set; } = 0.5;
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return Math.Abs(d) <= Threshold
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ZScoreToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double zScore)
        {
            if (zScore <= 2.0)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
            if (zScore <= 3.0)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15));
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IterationStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConverged)
        {
            return isConverged
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15));
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

#endregion

#region Icon Converters

public class QcStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is QcStatus status)
        {
            return status switch
            {
                QcStatus.Pass => "CheckCircle",
                QcStatus.Warning => "AlertCircle",
                QcStatus.Fail => "CloseCircle",
                _ => "HelpCircle"
            };
        }
        return "HelpCircle";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class PointStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PointStatus status)
        {
            return status switch
            {
                PointStatus.Accepted => "CheckCircle",
                PointStatus.Rejected => "CloseCircle",
                PointStatus.Pending => "Clock",
                _ => "HelpCircle"
            };
        }
        return "HelpCircle";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToIconConverter : IValueConverter
{
    public string TrueIcon { get; set; } = "Check";
    public string FalseIcon { get; set; } = "Close";
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? TrueIcon : FalseIcon;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

#endregion

#region Format Converters

public class DoubleFormatConverter : IValueConverter
{
    public string Format { get; set; } = "F2";
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return d.ToString(parameter?.ToString() ?? Format);
        if (value is float f) return f.ToString(parameter?.ToString() ?? Format);
        return value?.ToString() ?? "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (double.TryParse(value?.ToString(), out double result)) return result;
        return 0.0;
    }
}

public class AngleFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return $"{d:F2}°";
        return value?.ToString() ?? "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var str = value?.ToString()?.Replace("°", "").Trim();
        if (double.TryParse(str, out double result)) return result;
        return 0.0;
    }
}

public class PercentFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return $"{d:F1}%";
        if (value is int i) return $"{i}%";
        return value?.ToString() ?? "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>Alias for PercentFormatConverter for XAML compatibility</summary>
public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return $"{d:F1}%";
        if (value is int i) return $"{i}%";
        return value?.ToString() ?? "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class DateTimeFormatConverter : IValueConverter
{
    public string Format { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt) return dt.ToString(parameter?.ToString() ?? Format);
        return value?.ToString() ?? "";
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

#endregion

#region Enum Converters

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null)
        {
            var enumType = parameter.GetType();
            if (enumType.IsEnum)
                return parameter;
        }
        return System.Windows.Data.Binding.DoNothing;
    }
}

public class EnumToDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return "";
        return value.ToString()?.Replace("_", " ") ?? "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

#endregion

#region Math Converters

public class MultiplyConverter : IValueConverter
{
    public double Factor { get; set; } = 1.0;
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            double factor = Factor;
            if (parameter != null && double.TryParse(parameter.ToString(), out double p))
                factor = p;
            return d * factor;
        }
        return value;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class AddConverter : IValueConverter
{
    public double Offset { get; set; } = 0;
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            double offset = Offset;
            if (parameter != null && double.TryParse(parameter.ToString(), out double p))
                offset = p;
            return d + offset;
        }
        return value;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>General status to brush converter for various status types</summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Handle string status
        if (value is string status)
        {
            return status.ToLowerInvariant() switch
            {
                "pass" or "passed" or "accepted" or "success" or "complete" or "completed" => 
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                "fail" or "failed" or "rejected" or "error" => 
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                "warning" or "caution" => 
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15)),
                "pending" or "processing" or "running" => 
                    new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166))
            };
        }
        
        // Handle bool
        if (value is bool b)
        {
            return b 
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
        }
        
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(149, 165, 166));
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

#endregion
