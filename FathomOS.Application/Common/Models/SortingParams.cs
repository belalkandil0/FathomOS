namespace FathomOS.Application.Common.Models;

/// <summary>
/// Represents sorting direction.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Ascending order (A-Z, 0-9, oldest first).
    /// </summary>
    Ascending,

    /// <summary>
    /// Descending order (Z-A, 9-0, newest first).
    /// </summary>
    Descending
}

/// <summary>
/// Parameters for sorted queries.
/// </summary>
public sealed record SortingParams
{
    /// <summary>
    /// Gets or sets the property name to sort by.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Gets or sets the sort direction.
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;

    /// <summary>
    /// Gets a value indicating whether sorting is enabled.
    /// </summary>
    public bool IsSortingEnabled => !string.IsNullOrWhiteSpace(SortBy);

    /// <summary>
    /// Gets a value indicating whether sorting is descending.
    /// </summary>
    public bool IsDescending => SortDirection == SortDirection.Descending;

    /// <summary>
    /// Creates sorting parameters for ascending order.
    /// </summary>
    /// <param name="sortBy">The property to sort by</param>
    /// <returns>SortingParams configured for ascending order</returns>
    public static SortingParams Ascending(string sortBy) => new()
    {
        SortBy = sortBy,
        SortDirection = SortDirection.Ascending
    };

    /// <summary>
    /// Creates sorting parameters for descending order.
    /// </summary>
    /// <param name="sortBy">The property to sort by</param>
    /// <returns>SortingParams configured for descending order</returns>
    public static SortingParams Descending(string sortBy) => new()
    {
        SortBy = sortBy,
        SortDirection = SortDirection.Descending
    };

    /// <summary>
    /// Creates default sorting parameters (no sorting).
    /// </summary>
    /// <returns>Default SortingParams</returns>
    public static SortingParams None() => new();
}

/// <summary>
/// Represents a single sort column specification.
/// </summary>
/// <param name="PropertyName">The property to sort by</param>
/// <param name="Direction">The sort direction</param>
public sealed record SortColumn(string PropertyName, SortDirection Direction);

/// <summary>
/// Parameters for multi-column sorting.
/// </summary>
public sealed record MultiSortParams
{
    /// <summary>
    /// Gets the sort columns in order of priority.
    /// </summary>
    public IReadOnlyList<SortColumn> SortColumns { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether any sorting is specified.
    /// </summary>
    public bool HasSorting => SortColumns.Count > 0;

    /// <summary>
    /// Creates multi-sort parameters from a list of sort columns.
    /// </summary>
    /// <param name="sortColumns">The sort columns</param>
    /// <returns>MultiSortParams with the specified columns</returns>
    public static MultiSortParams From(params SortColumn[] sortColumns) => new()
    {
        SortColumns = sortColumns.ToList()
    };
}
