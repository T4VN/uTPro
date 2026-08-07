using Umbraco.Cms.Core.Models.PublishedContent;

namespace uTPro.Extension.Search;

/// <summary>
/// Search service interface for full-text search using Examine ExternalIndex.
/// Inject as <c>ISearchExtension</c> in views and controllers.
/// </summary>
public interface ISearchExtension
{
    /// <summary>
    /// Execute a full-text search against the ExternalIndex.
    /// </summary>
    /// <param name="query">The search term (user input).</param>
    /// <param name="scope">Search scope: "allSite", "currentFolder", or "selectedSource".</param>
    /// <param name="scopeNodeId">Node ID to restrict results to descendants of (when scope is path-based).</param>
    /// <param name="page">Current page number (1-based).</param>
    /// <param name="pageSize">Number of results per page.</param>
    /// <param name="fields">Optional: specific fields to search in. Null = all searchable fields.</param>
    /// <param name="orderBy">Optional sort fields (e.g. "updateDate Long desc").</param>
    /// <returns>A <see cref="SearchResultSet"/> with paged results and total count.</returns>
    SearchResultSet Search(
        string query,
        string? scope = "selectedSource",
        int? scopeNodeId = null,
        int page = 1,
        int pageSize = 10,
        string[]? fields = null,
        string[]? orderBy = null);
}

/// <summary>
/// Holds the paginated search results.
/// </summary>
public sealed class SearchResultSet
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public long TotalResults { get; set; }
    public int TotalPages { get; set; }
    public List<SearchResultItem> Items { get; set; } = new();
}

/// <summary>
/// A single search result item with its resolved content and relevance score.
/// </summary>
public sealed class SearchResultItem
{
    public IPublishedContent Content { get; set; } = null!;
    public float Score { get; set; }
}
