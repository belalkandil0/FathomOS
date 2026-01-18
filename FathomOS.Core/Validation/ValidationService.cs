using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FathomOS.Core.Interfaces;
using ValidationResult = FathomOS.Core.Interfaces.ValidationResult;

namespace FathomOS.Core.Validation;

/// <summary>
/// Unified validation service providing entity validation with support for
/// fluent rules, attribute-based validation, and custom validators.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ConcurrentDictionary<Type, object> _validators = new();

    /// <inheritdoc />
    public ValidationResult Validate<T>(T entity)
    {
        if (entity == null)
            return ValidationResult.Failure("Entity", "Entity cannot be null");

        var result = new ValidationResult();

        // Apply attribute-based validation
        ValidateAttributes(entity, result);

        // Apply registered validator
        if (_validators.TryGetValue(typeof(T), out var validatorObj) && validatorObj is IValidator<T> validator)
        {
            var validatorResult = validator.Validate(entity);
            result.Merge(validatorResult);
        }

        return result;
    }

    /// <inheritdoc />
    public ValidationResult ValidateProperty<T>(T entity, string propertyName)
    {
        if (entity == null)
            return ValidationResult.Failure(propertyName, "Entity cannot be null");

        var result = new ValidationResult();
        var property = typeof(T).GetProperty(propertyName);

        if (property == null)
        {
            result.AddError(propertyName, $"Property '{propertyName}' not found");
            return result;
        }

        ValidatePropertyAttributes(entity, property, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            return ValidationResult.Failure("Entity", "Entity cannot be null");

        var result = new ValidationResult();

        // Apply attribute-based validation
        ValidateAttributes(entity, result);

        // Apply registered async validator
        if (_validators.TryGetValue(typeof(T), out var validatorObj) && validatorObj is IValidator<T> validator)
        {
            var validatorResult = await validator.ValidateAsync(entity, cancellationToken);
            result.Merge(validatorResult);
        }

        return result;
    }

    /// <inheritdoc />
    public void RegisterValidator<T>(IValidator<T> validator)
    {
        _validators[typeof(T)] = validator;
    }

    /// <inheritdoc />
    public bool IsValid<T>(T entity)
    {
        return Validate(entity).IsValid;
    }

    /// <inheritdoc />
    public void ValidateAndThrow<T>(T entity)
    {
        var result = Validate(entity);
        if (!result.IsValid)
        {
            throw new Interfaces.ValidationException(result);
        }
    }

    #region Private Methods

    private void ValidateAttributes<T>(T entity, ValidationResult result)
    {
        if (entity == null) return;

        var type = entity.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            ValidatePropertyAttributes(entity, property, result);
        }
    }

    private void ValidatePropertyAttributes<T>(T entity, PropertyInfo property, ValidationResult result)
    {
        if (entity == null) return;

        var value = property.GetValue(entity);
        var attributes = property.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>(true);

        foreach (var attribute in attributes)
        {
            var context = new ValidationContext(entity) { MemberName = property.Name };
            var validationResult = attribute.GetValidationResult(value, context);

            if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                result.AddError(property.Name, validationResult?.ErrorMessage ?? "Validation failed");
            }
        }

        // Check for custom validation attributes
        var customAttrs = property.GetCustomAttributes<ValidationAttributeBase>(true);
        foreach (var attr in customAttrs)
        {
            if (!attr.IsValid(value))
            {
                result.AddError(property.Name, attr.ErrorMessage ?? "Validation failed");
            }
        }
    }

    #endregion
}

/// <summary>
/// Base class for custom validation attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = true)]
public abstract class ValidationAttributeBase : Attribute
{
    /// <summary>
    /// Error message when validation fails.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Validates the value.
    /// </summary>
    /// <param name="value">Value to validate.</param>
    /// <returns>True if valid.</returns>
    public abstract bool IsValid(object? value);
}

/// <summary>
/// Validates that a numeric value is within a range.
/// </summary>
public class RangeValidationAttribute : ValidationAttributeBase
{
    public double Minimum { get; }
    public double Maximum { get; }

    public RangeValidationAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
        ErrorMessage = $"Value must be between {minimum} and {maximum}";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true;

        if (double.TryParse(value.ToString(), out var numValue))
        {
            return numValue >= Minimum && numValue <= Maximum;
        }

        return false;
    }
}

/// <summary>
/// Validates that a string is not null or whitespace.
/// </summary>
public class NotEmptyAttribute : ValidationAttributeBase
{
    public NotEmptyAttribute()
    {
        ErrorMessage = "Value cannot be empty";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return false;

        if (value is string s)
            return !string.IsNullOrWhiteSpace(s);

        return true;
    }
}

/// <summary>
/// Validates that a collection has items.
/// </summary>
public class NotEmptyCollectionAttribute : ValidationAttributeBase
{
    public NotEmptyCollectionAttribute()
    {
        ErrorMessage = "Collection cannot be empty";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return false;

        if (value is System.Collections.ICollection collection)
            return collection.Count > 0;

        if (value is System.Collections.IEnumerable enumerable)
            return enumerable.Cast<object>().Any();

        return true;
    }
}

/// <summary>
/// Validates that a value is positive.
/// </summary>
public class PositiveAttribute : ValidationAttributeBase
{
    public bool AllowZero { get; set; }

    public PositiveAttribute(bool allowZero = false)
    {
        AllowZero = allowZero;
        ErrorMessage = allowZero ? "Value must be zero or positive" : "Value must be positive";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true;

        if (double.TryParse(value.ToString(), out var numValue))
        {
            return AllowZero ? numValue >= 0 : numValue > 0;
        }

        return false;
    }
}

/// <summary>
/// Validates string length.
/// </summary>
public class StringLengthValidationAttribute : ValidationAttributeBase
{
    public int MinimumLength { get; }
    public int MaximumLength { get; }

    public StringLengthValidationAttribute(int maximumLength, int minimumLength = 0)
    {
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
        ErrorMessage = $"String length must be between {minimumLength} and {maximumLength}";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return MinimumLength == 0;

        if (value is string s)
        {
            return s.Length >= MinimumLength && s.Length <= MaximumLength;
        }

        return true;
    }
}

/// <summary>
/// Validates that a value matches a regex pattern.
/// </summary>
public class RegexValidationAttribute : ValidationAttributeBase
{
    public string Pattern { get; }

    public RegexValidationAttribute(string pattern)
    {
        Pattern = pattern;
        ErrorMessage = "Value does not match required pattern";
    }

    public override bool IsValid(object? value)
    {
        if (value == null) return true;

        if (value is string s)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(s, Pattern);
        }

        return true;
    }
}
