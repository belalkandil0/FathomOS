// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: ViewModels/ViewModelBase.cs
// Purpose: Base class for ViewModels with INotifyPropertyChanged support
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// Base class for all ViewModels providing property change notification.
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
}
