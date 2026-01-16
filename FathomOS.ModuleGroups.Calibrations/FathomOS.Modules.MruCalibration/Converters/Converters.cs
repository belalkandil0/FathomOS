using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FathomOS.Modules.MruCalibration.Models;

namespace FathomOS.Modules.MruCalibration.Converters;

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Collapsed)
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
/// Converts boolean to Visibility (true = Collapsed, false = Visible)
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
/// Inverts a boolean value
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return false;
    }
}

/// <summary>
/// Converts null to a default value
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
/// Converts step number to visual state (completed, current, pending)
/// </summary>
public class StepStateConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return "Pending";
        
        if (values[0] is int stepNumber && values[1] is int currentStep)
        {
            if (stepNumber < currentStep) return "Completed";
            if (stepNumber == currentStep) return "Current";
            return "Pending";
        }
        return "Pending";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts step completion status to brush color
/// </summary>
public class StepToBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return System.Windows.Media.Brushes.Gray;
        
        if (values[0] is int stepNumber && values[1] is int currentStep)
        {
            if (stepNumber < currentStep)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x96, 0x88));  // Teal - completed
            if (stepNumber == currentStep)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0x5D, 0x00));  // Subsea7 Orange - current
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x60, 0x60, 0x60));  // Gray - pending
        }
        return System.Windows.Media.Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts PointStatus to brush color
/// </summary>
public class PointStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PointStatus status)
        {
            return status switch
            {
                PointStatus.Accepted => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50)),  // Green
                PointStatus.Rejected => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36)),  // Red
                _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x98, 0x00))  // Orange - pending
            };
        }
        return System.Windows.Media.Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts AcceptanceDecision to display text
/// </summary>
public class AcceptanceDecisionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AcceptanceDecision decision)
        {
            return decision switch
            {
                AcceptanceDecision.Accepted => "Yes - C-O Accepted",
                AcceptanceDecision.Rejected => "No - C-O Rejected",
                _ => "Not Decided"
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Formats a double value with specified decimal places
/// </summary>
public class DoubleFormatConverter : IValueConverter
{
    public int DecimalPlaces { get; set; } = 3;
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            int decimals = DecimalPlaces;
            if (parameter is string s && int.TryParse(s, out int p))
                decimals = p;
            return d.ToString($"F{decimals}");
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, out double d))
            return d;
        return 0.0;
    }
}

/// <summary>
/// Formats percentage values
/// </summary>
public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{d:F1}%";
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts DateTime to formatted string
/// </summary>
public class DateTimeFormatConverter : IValueConverter
{
    public string Format { get; set; } = "dd/MM/yyyy HH:mm:ss";
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            string fmt = parameter as string ?? Format;
            return dt.ToString(fmt);
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && DateTime.TryParse(s, out DateTime dt))
            return dt;
        return DateTime.MinValue;
    }
}

/// <summary>
/// Converts CalibrationPurpose enum to boolean for radio buttons
/// </summary>
public class PurposeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CalibrationPurpose purpose && parameter is string param)
        {
            return purpose.ToString() == param;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is string param)
        {
            if (Enum.TryParse<CalibrationPurpose>(param, out var purpose))
                return purpose;
        }
        return CalibrationPurpose.Verification;
    }
}

/// <summary>
/// Enables or disables based on step completion
/// </summary>
public class StepEnabledConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return false;
        
        if (values[0] is int stepNumber && values[1] is int currentStep)
        {
            // Can access current step and all completed steps
            return stepNumber <= currentStep;
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts current step to visibility for step content panels
/// </summary>
public class StepToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepParam)
        {
            if (int.TryParse(stepParam, out int targetStep))
            {
                return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean to color for QC status display
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush PassedBrush = new(System.Windows.Media.Color.FromRgb(0x4C, 0xAF, 0x50)); // Green
    private static readonly SolidColorBrush FailedBrush = new(System.Windows.Media.Color.FromRgb(0x80, 0x80, 0x80)); // Gray

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool passed)
        {
            return passed ? PassedBrush : FailedBrush;
        }
        return FailedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts null/empty string to Visibility (null = Collapsed, non-null = Visible)
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return Visibility.Collapsed;
        
        if (value is string s && string.IsNullOrEmpty(s))
            return Visibility.Collapsed;
            
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
