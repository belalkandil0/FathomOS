using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.IconPacks;

// Explicit alias to avoid System.Drawing ambiguity
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace FathomOS.Modules.EquipmentInventory.Converters;

/// <summary>
/// Converts boolean to Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>
/// Converts boolean to Visibility (true = Collapsed, false = Visible)
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v != Visibility.Visible;
}

/// <summary>
/// Inverts a boolean value
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

/// <summary>
/// Converts a hex color string (e.g., "#FF0000") to SolidColorBrush
/// </summary>
public class StringToSolidColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorString && !string.IsNullOrEmpty(colorString))
        {
            try
            {
                var color = (MediaColor)MediaColorConverter.ConvertFromString(colorString);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts byte array (PNG/image bytes) to BitmapImage for display
/// </summary>
public class BytesToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
            return null;
        
        try
        {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts null/empty to Collapsed, non-null to Visible
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null || (value is string s && string.IsNullOrEmpty(s));
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts null/empty to Visible, non-null to Collapsed
/// </summary>
public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null || (value is string s && string.IsNullOrEmpty(s));
        return isNull ? Visibility.Visible : Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts int > 0 to Visible, 0 or less to Collapsed
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int i) return i > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (value is long l) return l > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts EquipmentStatus to color brush
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Available" => new SolidColorBrush(MediaColor.FromRgb(46, 204, 113)),
            "InUse" => new SolidColorBrush(MediaColor.FromRgb(52, 152, 219)),
            "InTransit" => new SolidColorBrush(MediaColor.FromRgb(243, 156, 18)),
            "UnderRepair" => new SolidColorBrush(MediaColor.FromRgb(155, 89, 182)),
            "InStorage" => new SolidColorBrush(MediaColor.FromRgb(149, 165, 166)),
            "Condemned" or "Disposed" => new SolidColorBrush(MediaColor.FromRgb(231, 76, 60)),
            "Reserved" => new SolidColorBrush(MediaColor.FromRgb(26, 188, 156)),
            _ => new SolidColorBrush(MediaColor.FromRgb(127, 140, 141))
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts ManifestStatus to color brush
/// </summary>
public class ManifestStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Draft" => new SolidColorBrush(MediaColor.FromRgb(127, 140, 141)),
            "Submitted" or "PendingApproval" => new SolidColorBrush(MediaColor.FromRgb(243, 156, 18)),
            "Approved" => new SolidColorBrush(MediaColor.FromRgb(52, 152, 219)),
            "InTransit" => new SolidColorBrush(MediaColor.FromRgb(155, 89, 182)),
            "Received" or "Completed" => new SolidColorBrush(MediaColor.FromRgb(46, 204, 113)),
            "Rejected" or "Cancelled" => new SolidColorBrush(MediaColor.FromRgb(231, 76, 60)),
            _ => new SolidColorBrush(MediaColor.FromRgb(127, 140, 141))
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts boolean alert condition to warning color
/// </summary>
public class BoolToAlertColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b 
            ? new SolidColorBrush(MediaColor.FromRgb(231, 76, 60)) 
            : new SolidColorBrush(Colors.Transparent);
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts days until expiry to appropriate color
/// </summary>
public class DaysToExpiryColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int days) return new SolidColorBrush(Colors.Gray);
        
        return days switch
        {
            <= 0 => new SolidColorBrush(MediaColor.FromRgb(231, 76, 60)),
            <= 7 => new SolidColorBrush(MediaColor.FromRgb(243, 156, 18)),
            <= 30 => new SolidColorBrush(MediaColor.FromRgb(241, 196, 15)),
            _ => new SolidColorBrush(MediaColor.FromRgb(46, 204, 113))
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to step indicator dot fill color (for wizard step indicators)
/// </summary>
public class StepDotConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
            return new SolidColorBrush(MediaColor.FromRgb(0, 122, 204)); // Active blue
        return new SolidColorBrush(MediaColor.FromRgb(128, 128, 128)); // Inactive gray
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts bool to step indicator line fill color (for wizard step indicators)
/// </summary>
public class StepLineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isCompleted && isCompleted)
            return new SolidColorBrush(MediaColor.FromRgb(0, 122, 204)); // Completed blue
        return new SolidColorBrush(MediaColor.FromRgb(200, 200, 200)); // Incomplete light gray
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts VerificationStatus to color brush for manifest item verification
/// </summary>
public class VerificationStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Pending" => new SolidColorBrush(MediaColor.FromRgb(158, 158, 158)),
            "Verified" => new SolidColorBrush(MediaColor.FromRgb(76, 175, 80)),
            "Damaged" => new SolidColorBrush(MediaColor.FromRgb(255, 152, 0)),
            "Missing" => new SolidColorBrush(MediaColor.FromRgb(244, 67, 54)),
            "Extra" => new SolidColorBrush(MediaColor.FromRgb(33, 150, 243)),
            "Wrong" => new SolidColorBrush(MediaColor.FromRgb(156, 39, 176)),
            _ => new SolidColorBrush(MediaColor.FromRgb(158, 158, 158))
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts ManifestType to icon kind string
/// </summary>
public class ManifestTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Outward" => MahApps.Metro.IconPacks.PackIconMaterialKind.TruckDeliveryOutline,
            "Inward" => MahApps.Metro.IconPacks.PackIconMaterialKind.TruckCheckOutline,
            _ => MahApps.Metro.IconPacks.PackIconMaterialKind.PackageVariant
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts ManifestType to background brush
/// </summary>
public class ManifestTypeToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Outward" => new SolidColorBrush(MediaColor.FromRgb(0, 180, 216)),
            "Inward" => new SolidColorBrush(MediaColor.FromRgb(0, 212, 170)),
            _ => new SolidColorBrush(MediaColor.FromRgb(158, 158, 158))
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts int to bool (true if greater than zero)
/// Singleton pattern for use in DataTrigger bindings
/// </summary>
public class IntGreaterThanZeroConverter : IValueConverter
{
    public static readonly IntGreaterThanZeroConverter Instance = new();
    
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int i) return i > 0;
        if (value is long l) return l > 0;
        if (value is string s && int.TryParse(s, out int parsed)) return parsed > 0;
        return false;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Multi-value converter for combining multiple boolean conditions
/// Returns Visible only if all values are true
/// </summary>
public class MultiBoolToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0) return Visibility.Collapsed;
        
        foreach (var value in values)
        {
            if (value is not bool b || !b)
                return Visibility.Collapsed;
        }
        return Visibility.Visible;
    }
    
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts UnregisteredItemStatus to background color brush
/// </summary>
public class UnregisteredItemStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return new SolidColorBrush(Color.FromRgb(108, 117, 125)); // Gray
        
        var statusStr = value.ToString();
        return statusStr switch
        {
            "PendingReview" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),     // Warning yellow
            "ConvertedToEquipment" => new SolidColorBrush(Color.FromRgb(40, 167, 69)), // Success green
            "KeptAsConsumable" => new SolidColorBrush(Color.FromRgb(23, 162, 184)),    // Info cyan
            "Rejected" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),              // Danger red
            _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))                      // Gray
        };
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts notification type to icon
/// </summary>
public class NotificationTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string notificationType)
        {
            return notificationType.ToLowerInvariant() switch
            {
                "missing" => PackIconMaterialKind.PackageVariantRemove,
                "damaged" => PackIconMaterialKind.PackageVariantClosed,
                "extra" => PackIconMaterialKind.PackageVariantPlus,
                "wrong" => PackIconMaterialKind.SwapHorizontal,
                "certification" => PackIconMaterialKind.Certificate,
                "calibration" => PackIconMaterialKind.Tune,
                _ => PackIconMaterialKind.Bell
            };
        }
        return PackIconMaterialKind.Bell;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts notification type to color
/// </summary>
public class NotificationTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string notificationType)
        {
            return notificationType.ToLowerInvariant() switch
            {
                "missing" => new SolidColorBrush(Color.FromRgb(220, 53, 69)),      // Red
                "damaged" => new SolidColorBrush(Color.FromRgb(255, 193, 7)),      // Yellow
                "extra" => new SolidColorBrush(Color.FromRgb(23, 162, 184)),        // Cyan
                "wrong" => new SolidColorBrush(Color.FromRgb(255, 128, 0)),         // Orange
                "certification" => new SolidColorBrush(Color.FromRgb(111, 66, 193)), // Purple
                "calibration" => new SolidColorBrush(Color.FromRgb(32, 201, 151)),  // Teal
                _ => new SolidColorBrush(Color.FromRgb(108, 117, 125))               // Gray
            };
        }
        return new SolidColorBrush(Color.FromRgb(108, 117, 125));
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
