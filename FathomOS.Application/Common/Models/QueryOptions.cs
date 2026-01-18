namespace FathomOS.Application.Common.Models;

/// <summary>
/// Combines pagination, sorting, and filtering options for queries.
/// </summary>
public sealed record QueryOptions
{
    /// <summary>
    /// Gets the pagination parameters.
    /// </summary>
    public PaginationParams Pagination { get; init; } = new();

    /// <summary>
    /// Gets the sorting parameters.
    /// </summary>
    public SortingParams Sorting { get; init; } = new();

    /// <summary>
    /// Gets the filter parameters.
    /// </summary>
    public FilterParams Filters { get; init; } = new();

    /// <summary>
    /// Gets or sets a search term for full-text search.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Gets a value indicating whether a search term is provided.
    /// </summary>
    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm);

    /// <summary>
    /// Creates default query options.
    /// </summary>
    /// <returns>Default QueryOptions</returns>
    public static QueryOptions Default() => new();

    /// <summary>
    /// Creates query options with pagination only.
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>QueryOptions with pagination</returns>
    public static QueryOptions WithPagination(int pageNumber, int pageSize) => new()
    {
        Pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize }
    };

    /// <summary>
    /// Creates query options with search term.
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <returns>QueryOptions with search</returns>
    public static QueryOptions WithSearch(string searchTerm) => new()
    {
        SearchTerm = searchTerm
    };

    /// <summary>
    /// Creates a new instance with updated pagination.
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>Updated QueryOptions</returns>
    public QueryOptions WithPage(int pageNumber, int pageSize) => this with
    {
        Pagination = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize }
    };

    /// <summary>
    /// Creates a new instance with updated sorting.
    /// </summary>
    /// <param name="sortBy">The property to sort by</param>
    /// <param name="descending">Whether to sort descending</param>
    /// <returns>Updated QueryOptions</returns>
    public QueryOptions WithSort(string sortBy, bool descending = false) => this with
    {
        Sorting = new SortingParams
        {
            SortBy = sortBy,
            SortDirection = descending ? SortDirection.Descending : SortDirection.Ascending
        }
    };
}
