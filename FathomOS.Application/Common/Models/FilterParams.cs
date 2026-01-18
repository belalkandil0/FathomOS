namespace FathomOS.Application.Common.Models;

/// <summary>
/// Represents filter comparison operators.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Equals comparison.
    /// </summary>
    Equals,

    /// <summary>
    /// Not equals comparison.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Greater than comparison.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal comparison.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than comparison.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal comparison.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Contains (substring) comparison.
    /// </summary>
    Contains,

    /// <summary>
    /// Starts with comparison.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Ends with comparison.
    /// </summary>
    EndsWith,

    /// <summary>
    /// In list comparison.
    /// </summary>
    In,

    /// <summary>
    /// Not in list comparison.
    /// </summary>
    NotIn,

    /// <summary>
    /// Is null comparison.
    /// </summary>
    IsNull,

    /// <summary>
    /// Is not null comparison.
    /// </summary>
    IsNotNull,

    /// <summary>
    /// Between two values (inclusive).
    /// </summary>
    Between
}

/// <summary>
/// Represents a single filter condition.
/// </summary>
public sealed record FilterCondition
{
    /// <summary>
    /// Gets the property name to filter on.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the filter operator.
    /// </summary>
    public FilterOperator Operator { get; init; } = FilterOperator.Equals;

    /// <summary>
    /// Gets the value to compare against.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Gets the second value for range comparisons (Between operator).
    /// </summary>
    public object? SecondValue { get; init; }

    /// <summary>
    /// Creates an equals filter condition.
    /// </summary>
    public static FilterCondition Equals(string propertyName, object value) => new()
    {
        PropertyName = propertyName,
        Operator = FilterOperator.Equals,
        Value = value
    };

    /// <summary>
    /// Creates a contains filter condition.
    /// </summary>
    public static FilterCondition Contains(string propertyName, string value) => new()
    {
        PropertyName = propertyName,
        Operator = FilterOperator.Contains,
        Value = value
    };

    /// <summary>
    /// Creates a between filter condition.
    /// </summary>
    public static FilterCondition Between(string propertyName, object minValue, object maxValue) => new()
    {
        PropertyName = propertyName,
        Operator = FilterOperator.Between,
        Value = minValue,
        SecondValue = maxValue
    };
}

/// <summary>
/// Logic operators for combining filter conditions.
/// </summary>
public enum FilterLogic
{
    /// <summary>
    /// All conditions must be true (AND).
    /// </summary>
    And,

    /// <summary>
    /// Any condition can be true (OR).
    /// </summary>
    Or
}

/// <summary>
/// Parameters for filtered queries.
/// </summary>
public sealed record FilterParams
{
    /// <summary>
    /// Gets the filter conditions.
    /// </summary>
    public IReadOnlyList<FilterCondition> Conditions { get; init; } = [];

    /// <summary>
    /// Gets the logic operator for combining conditions.
    /// </summary>
    public FilterLogic Logic { get; init; } = FilterLogic.And;

    /// <summary>
    /// Gets a value indicating whether any filters are specified.
    /// </summary>
    public bool HasFilters => Conditions.Count > 0;

    /// <summary>
    /// Creates filter parameters with AND logic.
    /// </summary>
    public static FilterParams And(params FilterCondition[] conditions) => new()
    {
        Conditions = conditions.ToList(),
        Logic = FilterLogic.And
    };

    /// <summary>
    /// Creates filter parameters with OR logic.
    /// </summary>
    public static FilterParams Or(params FilterCondition[] conditions) => new()
    {
        Conditions = conditions.ToList(),
        Logic = FilterLogic.Or
    };

    /// <summary>
    /// Creates empty filter parameters.
    /// </summary>
    public static FilterParams None() => new();
}
