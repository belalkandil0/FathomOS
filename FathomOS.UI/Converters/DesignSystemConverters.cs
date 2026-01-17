using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FathomOS.UI.Converters
{
    /// <summary>
    /// Converts a string to boolean (true if not null or empty).
    /// </summary>
    public class StringNonEmptyConverter : IValueConverter
    {
        public static readonly StringNonEmptyConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an object to boolean (true if not null).
    /// </summary>
    public class ObjectNotNullConverter : IValueConverter
    {
        public static readonly ObjectNotNullConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean to Visibility.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                bool invert = parameter as string == "Invert";
                if (invert)
                    boolValue = !boolValue;
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }

    /// <summary>
    /// Subtracts a value from a double. Parameter is the amount to subtract.
    /// </summary>
    public class SubtractConverter : IValueConverter
    {
        public static readonly SubtractConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), out double subtractValue))
                {
                    return Math.Max(0, doubleValue - subtractValue);
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multiplies a value by a factor. Parameter is the multiplication factor.
    /// </summary>
    public class MultiplyConverter : IValueConverter
    {
        public static readonly MultiplyConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue && parameter != null)
            {
                if (double.TryParse(parameter.ToString(), out double factor))
                {
                    return doubleValue * factor;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
