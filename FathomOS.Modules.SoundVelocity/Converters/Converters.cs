using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FathomOS.Modules.SoundVelocity.Converters;

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

public class StepToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string targetStepStr && int.TryParse(targetStepStr, out int targetStep))
        {
            return currentStep == targetStep ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        
        return value.ToString() switch
        {
            "Valeport" => "Valeport CTD (Tab-delimited)",
            "QINSyExport" => "QINSy Export/CSV (Comma-delimited)",
            "QINSyLog" => "QINSy Log (Space-delimited)",
            "FixedWidth" => "Fixed Width (Tritech BP3)",
            "Semicolon" => "Semicolon Delimited (SAIV)",
            "CtdSvWithSalinity" => "CTD/SV Profile (using Salinity)",
            "SvOnly" => "SV Profile Only (External SV)",
            "CtdSvWithConductivity" => "CTD/SV Profile (using Conductivity)",
            "ExternalSource" => "From Input File",
            "ChenMillero" => "Chen & Millero (depth ≤1000m)",
            "DelGrosso" => "Del Grosso (depth >1000m)",
            "Auto" => "Auto (Chen&Millero ≤1000m, Del Grosso >1000m)",
            "UnescoEOS80" => "UNESCO EOS-80",
            "Depth" => "Depth (meters)",
            "Pressure" => "Pressure (dbar)",
            "DMS" => "DD;MM;SS.ss (DMS)",
            "DM" => "DD;MM.mmmm (DM)",
            "DD" => "DD.dddd (Decimal)",
            // Smoothing methods
            "None" => "No Smoothing",
            "MovingAverage" => "Moving Average",
            "Gaussian" => "Gaussian Weighted",
            "SavitzkyGolay" => "Savitzky-Golay",
            "MedianFilter" => "Median Filter (Spike Removal)",
            _ => value.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
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

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
            return !string.IsNullOrEmpty(s) ? Visibility.Visible : Visibility.Collapsed;
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class IndexPlusOneConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Convert from 0-based column index to 1-based ComboBox selection (with -1 = "Not Used" at index 0)
        if (value is int i)
            return i + 1; // -1 becomes 0, 0 becomes 1, etc.
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Convert from 1-based ComboBox selection back to 0-based column index
        if (value is int i)
            return i - 1; // 0 becomes -1, 1 becomes 0, etc.
        return -1;
    }
}
