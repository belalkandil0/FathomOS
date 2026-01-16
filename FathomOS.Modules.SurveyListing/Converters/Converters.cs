using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FathomOS.Core.Models;

namespace FathomOS.Modules.SurveyListing.Converters;

/// <summary>
/// Converts boolean to Visibility
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
        if (value is Visibility v)
            return v == Visibility.Visible;
        return false;
    }
}

/// <summary>
/// Converts boolean to Visibility (inverse)
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
        if (value is Visibility v)
            return v != Visibility.Visible;
        return true;
    }
}

/// <summary>
/// Converts LengthUnit enum to display name
/// </summary>
public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LengthUnit unit)
            return unit.GetDisplayName();
        
        if (value is SurveyType surveyType)
            return surveyType switch
            {
                SurveyType.Seabed => "Seabed Survey",
                SurveyType.RovDynamic => "ROV / Dynamic Survey",
                SurveyType.Pipelay => "Pipelay Survey",
                SurveyType.EFL => "EFL Survey",
                SurveyType.SFL => "SFL Survey",
                SurveyType.Umbilical => "Umbilical Survey",
                SurveyType.Cable => "Cable Survey",
                SurveyType.Touchdown => "Touchdown Monitoring",
                SurveyType.AsBuilt => "As-Built Survey",
                SurveyType.PreLay => "Pre-Lay Survey",
                SurveyType.PostLay => "Post-Lay Survey",
                SurveyType.FreeSpan => "Free Span Survey",
                SurveyType.Inspection => "Inspection Survey",
                SurveyType.Custom => "Custom Survey",
                _ => surveyType.ToString()
            };
        
        if (value is KpDccMode kpDccMode)
            return kpDccMode switch
            {
                KpDccMode.Both => "KP & DCC (requires route file)",
                KpDccMode.KpOnly => "KP Only (requires route file)",
                KpDccMode.DccOnly => "DCC Only (requires route file)",
                KpDccMode.None => "None (X, Y, Z only - no route needed)",
                _ => kpDccMode.ToString()
            };

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null/empty string to a default value
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
    {
        throw new NotImplementedException();
    }
}
