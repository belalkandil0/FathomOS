namespace FathomOS.Application.Common.Models;

/// <summary>
/// Represents a paginated result set with metadata.
/// </summary>
/// <typeparam name="T">The type of items in the result set</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Gets the index of the first item on the current page (1-based).
    /// </summary>
    public int FirstItemIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    /// <summary>
    /// Gets the index of the last item on the current page (1-based).
    /// </summary>
    public int LastItemIndex => Math.Min(PageNumber * PageSize, TotalCount);

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="totalCount">The total number of items</param>
    /// <param name="pageNumber">The current page number</param>
    /// <param name="pageSize">The page size</param>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="pageNumber">The page number</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>An empty PagedResult</returns>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PagedResult<T>([], 0, pageNumber, pageSize);
    }

    /// <summary>
    /// Maps the items to a new type.
    /// </summary>
    /// <typeparam name="TResult">The target type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A new PagedResult with mapped items</returns>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        var mappedItems = Items.Select(mapper).ToList();
        return new PagedResult<TResult>(mappedItems, TotalCount, PageNumber, PageSize);
    }
}

/// <summary>
/// Parameters for paginated queries.
/// </summary>
public sealed record PaginationParams
{
    /// <summary>
    /// Gets the default page size.
    /// </summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Gets the maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    /// <summary>
    /// Gets the number of items to skip.
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;

    /// <summary>
    /// Gets the number of items to take.
    /// </summary>
    public int Take => PageSize;

    /// <summary>
    /// Creates default pagination parameters.
    /// </summary>
    /// <returns>Default PaginationParams</returns>
    public static PaginationParams Default() => new();
}
