namespace FathomOS.Domain.Common;

/// <summary>
/// Base class for value objects in the domain model.
/// Value objects are immutable and compared by their structural equality (all property values).
/// They have no identity - two value objects with the same values are considered equal.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components used for equality comparison.
    /// Derived classes must override this to return all properties that define the value object's identity.
    /// </summary>
    /// <returns>An enumerable of objects representing the equality components</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether this value object equals another value object.
    /// </summary>
    /// <param name="other">The value object to compare with</param>
    /// <returns>True if the value objects are structurally equal; otherwise, false</returns>
    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as ValueObject);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (current, component) =>
                HashCode.Combine(current, component?.GetHashCode() ?? 0));
    }

    /// <summary>
    /// Equality operator for value objects.
    /// </summary>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator for value objects.
    /// </summary>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Creates a copy of this value object.
    /// Since value objects are immutable, this returns the same instance.
    /// Override in derived classes if deep copying is required.
    /// </summary>
    /// <returns>A reference to this value object</returns>
    public virtual ValueObject Copy()
    {
        return (ValueObject)MemberwiseClone();
    }
}

/// <summary>
/// Base class for single-value value objects (wrapper types).
/// Simplifies creation of value objects that wrap a single primitive value.
/// </summary>
/// <typeparam name="T">The type of the wrapped value</typeparam>
public abstract class SingleValueObject<T> : ValueObject
    where T : notnull
{
    /// <summary>
    /// Gets the wrapped value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Initializes a new instance wrapping the specified value.
    /// </summary>
    /// <param name="value">The value to wrap</param>
    protected SingleValueObject(T value)
    {
        Value = value;
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Implicit conversion to the wrapped value type.
    /// </summary>
    public static implicit operator T(SingleValueObject<T> valueObject)
    {
        return valueObject.Value;
    }
}
