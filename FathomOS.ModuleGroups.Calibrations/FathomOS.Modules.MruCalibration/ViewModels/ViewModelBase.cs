using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.MruCalibration.ViewModels;

/// <summary>
/// Base class for all ViewModels providing INotifyPropertyChanged implementation
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    /// <summary>
    /// Set property and raise additional property changed notifications
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, string[] additionalProperties, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName)) return false;
        
        foreach (var prop in additionalProperties)
        {
            OnPropertyChanged(prop);
        }
        return true;
    }
}
