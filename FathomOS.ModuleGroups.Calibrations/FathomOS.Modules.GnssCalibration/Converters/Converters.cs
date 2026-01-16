using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Reflection;

namespace FathomOS.Modules.GnssCalibration.Converters;

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
/// Converts null/empty to a default value.
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
/// Converts step index to Visibility for wizard step content.
/// </summary>
public class StepToActiveConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepStr && int.TryParse(stepStr, out int targetStep))
        {
            return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts step index to completed state (step < current).
/// </summary>
public class StepToCompletedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepStr && int.TryParse(stepStr, out int targetStep))
        {
            return currentStep > targetStep;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean rejection status to color.
/// </summary>
public class RejectedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRejected)
        {
            return isRejected 
                ? new SolidColorBrush(Color.FromRgb(231, 76, 60))   // Red for rejected
                : new SolidColorBrush(Color.FromRgb(46, 204, 113)); // Green for accepted
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean to status text (Accepted/Rejected).
/// </summary>
public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isRejected)
        {
            return isRejected ? "Rejected" : "Accepted";
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Formats double values with specified decimal places.
/// </summary>
public class DoubleFormatConverter : IValueConverter
{
    public int DecimalPlaces { get; set; } = 3;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            int decimals = parameter is string s && int.TryParse(s, out int p) ? p : DecimalPlaces;
            return d.ToString($"F{decimals}");
        }
        return value?.ToString() ?? "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && double.TryParse(s, out double d))
            return d;
        return 0.0;
    }
}

/// <summary>
/// Converts step index to step indicator brush.
/// </summary>
public class StepIndicatorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is int currentStep && values[1] is int thisStep)
        {
            if (currentStep == thisStep)
                return Application.Current.FindResource("AccentBrush") ?? Brushes.DodgerBlue;
            if (currentStep > thisStep)
                return Application.Current.FindResource("SuccessBrush") ?? Brushes.Green;
            return Application.Current.FindResource("BorderBrush") ?? Brushes.Gray;
        }
        return Brushes.Gray;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts null to Visibility.Visible, non-null to Visibility.Collapsed.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null || (value is string s && string.IsNullOrEmpty(s)) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts non-null to Visibility.Visible, null to Visibility.Collapsed.
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null && !(value is string s && string.IsNullOrEmpty(s)) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts enum value to bool for RadioButton binding.
/// </summary>
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
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts boolean pass/fail to color (green for pass, red for fail).
/// Used for tolerance check status display.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool passed)
        {
            // Green for PASS, Red for FAIL
            return passed 
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))   // Green #4CAF50
                : new SolidColorBrush(Color.FromRgb(244, 67, 54));  // Red #F44336
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gray for unknown
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
