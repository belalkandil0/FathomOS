// ============================================================================
// Fathom OS - Core Library
// File: Mvvm/ObservableObject.cs
// Purpose: Lightweight base class for models that need property change notification
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Core.Mvvm;

/// <summary>
/// A lightweight base class for objects that need to implement <see cref="INotifyPropertyChanged"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the minimal implementation for property change notification,
/// suitable for model objects and other lightweight classes that don't need the
/// full functionality of <see cref="ViewModelBase"/>.
/// </para>
/// <para>
/// The implementation is thread-safe - property change notifications can be raised
/// from any thread, and the event handler reference is captured atomically.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Person : ObservableObject
/// {
///     private string _name = string.Empty;
///
///     public string Name
///     {
///         get => _name;
///         set => SetProperty(ref _name, value);
///     }
/// }
/// </code>
/// </example>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">
    /// The name of the property that changed. This parameter is optional and can be
    /// provided automatically when invoked from a property setter using <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <remarks>
    /// This method is thread-safe and can be called from any thread.
    /// </remarks>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        // Capture the handler reference atomically to ensure thread safety
        var handler = PropertyChanged;
        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the backing field to the specified value and raises <see cref="PropertyChanged"/>
    /// if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">A reference to the backing field.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="propertyName">
    /// The name of the property. This parameter is optional and can be provided automatically
    /// when invoked from a property setter using <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was changed and the <see cref="PropertyChanged"/> event was raised;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// private int _count;
    /// public int Count
    /// {
    ///     get => _count;
    ///     set => SetProperty(ref _count, value);
    /// }
    /// </code>
    /// </example>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets the backing field to the specified value, raises <see cref="PropertyChanged"/>
    /// if the value has changed, and invokes a callback action.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">A reference to the backing field.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="onChanged">
    /// A callback action that is invoked after the property value has changed and
    /// the <see cref="PropertyChanged"/> event has been raised.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property. This parameter is optional and can be provided automatically
    /// when invoked from a property setter using <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was changed and the <see cref="PropertyChanged"/> event was raised;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// private string _filter = string.Empty;
    /// public string Filter
    /// {
    ///     get => _filter;
    ///     set => SetProperty(ref _filter, value, RefreshFilteredItems);
    /// }
    /// </code>
    /// </example>
    protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return false;
        }

        onChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for multiple properties at once.
    /// </summary>
    /// <param name="propertyNames">The names of the properties that changed.</param>
    /// <remarks>
    /// This method is useful when a single operation affects multiple properties
    /// and you want to notify all of them efficiently.
    /// </remarks>
    /// <example>
    /// <code>
    /// public void Reset()
    /// {
    ///     _firstName = string.Empty;
    ///     _lastName = string.Empty;
    ///     OnPropertiesChanged(nameof(FirstName), nameof(LastName), nameof(FullName));
    /// }
    /// </code>
    /// </example>
    protected void OnPropertiesChanged(params string[] propertyNames)
    {
        if (propertyNames == null)
        {
            return;
        }

        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
