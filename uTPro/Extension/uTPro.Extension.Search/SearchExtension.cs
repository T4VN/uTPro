using Examine;
using Examine.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using uTPro.Extension.CurrentSite;

namespace uTPro.Extension.Search;

/// <summary>
/// Full-text search service using Umbraco's Examine ExternalIndex (Lucene).
/// Uses ManagedQuery for proper tokenization and relevance scoring,
/// combined with in-memory path filtering for scope restriction.
/// When uTPro.Feature.SearchPlus is installed, automatically expands queries with synonyms.
/// </summary>
internal sealed class SearchExtension : ISearchExtension
{
    private readonly IExamineManager _examineManager;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly ICurrentSiteExtension _currentSite;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a search extension with the services required to query Umbraco content and expand search terms.
    /// </summary>
    /// <param name="examineManager">The Examine manager used to access search indexes.</param>
    /// <param name="umbracoContextAccessor">Provides access to the current Umbraco context.</param>
    /// <param name="currentSite">Provides information about the current site.</param>
    /// <param name="serviceProvider">Resolves optional search services, including synonym providers.</param>
    public SearchExtension(
        IExamineManager examineManager,
        IUmbracoContextAccessor umbracoContextAccessor,
        ICurrentSiteExtension currentSite,
        IServiceProvider serviceProvider)
    {
        _examineManager = examineManager;
        _umbracoContextAccessor = umbracoContextAccessor;
        _currentSite = currentSite;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Searches published site content using the specified query, scope, fields, ordering, and pagination.
    /// </summary>
    /// <param name="scope">The search scope; use <c>"allSite"</c> to search the entire site.</param>
    /// <param name="scopeNodeId">The node ID that limits results to its content tree.</param>
    /// <param name="fields">The fields to search.</param>
    /// <param name="orderBy">The field ordering specifications to apply.</param>
    /// <returns>The search results and pagination metadata.</returns>
    public SearchResultSet Search(
        string query,
        string? scope = "selectedSource",
        int? scopeNodeId = null,
        int page = 1,
        int pageSize = 10,
        string[]? fields = null,
        string[]? orderBy = null)
    {
        var result = new SearchResultSet { Query = query, Page = page, PageSize = pageSize };

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return result;

        if (!_examineManager.TryGetIndex(Constants.UmbracoIndexes.ExternalIndexName, out var index))
            return result;

        var searcher = index.Searcher;
        var locale = _currentSite.CurrentCulture.TwoLetterISOLanguageName;

        // Build the Examine query using ManagedQuery (best for full-text relevance)
        var examineQuery = searcher.CreateQuery("content");

        // Expand query with synonyms if SearchPlus is installed
        var searchTerms = ExpandWithSynonyms(query.Trim());

        // Build OR query across all expanded terms
        IBooleanOperation boolOp;
        if (searchTerms.Count == 1)
        {
            // Single term (no synonyms found) — use standard ManagedQuery
            boolOp = fields != null && fields.Length > 0
                ? examineQuery.ManagedQuery(searchTerms[0], fields)
                : examineQuery.ManagedQuery(searchTerms[0]);
        }
        else
        {
            // Multiple synonyms — OR them together for broader results
            boolOp = fields != null && fields.Length > 0
                ? examineQuery.ManagedQuery(searchTerms[0], fields)
                : examineQuery.ManagedQuery(searchTerms[0]);

            for (var i = 1; i < searchTerms.Count; i++)
            {
                boolOp = fields != null && fields.Length > 0
                    ? boolOp.Or().ManagedQuery(searchTerms[i], fields)
                    : boolOp.Or().ManagedQuery(searchTerms[i]);
            }
        }

        // Apply ordering
        IOrdering ordered = ApplyOrdering(boolOp, orderBy);

        // Determine the scope node ID for in-memory path filtering
        int? filterScopeId = null;
        if (scope != "allSite" && scopeNodeId.HasValue && scopeNodeId.Value > 0)
        {
            filterScopeId = scopeNodeId.Value;
        }
        else if (scope != "allSite")
        {
            filterScopeId = _currentSite.GetItem()?.PageHome?.Id;
        }

        // Execute search — fetch more results for in-memory filtering
        // We request a larger window to account for path filtering reducing the set
        var maxFetch = filterScopeId.HasValue ? pageSize * 10 : pageSize;
        var searchResults = ordered.Execute(QueryOptions.SkipTake(0, maxFetch));

        // Resolve IPublishedContent from search result IDs with path + culture filtering
        if (_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            var contentCache = umbracoContext.Content;
            if (contentCache != null)
            {
                var culture = _currentSite.CurrentCulture.Name;
                var pathFilter = filterScopeId.HasValue ? $",{filterScopeId.Value}," : null;

                var allMatched = searchResults
                    .Where(sr => int.TryParse(sr.Id, out _))
                    .Select(sr => new { SearchResult = sr, NodeId = int.Parse(sr.Id) })
                    .Select(x => new { x.SearchResult, Content = contentCache.GetById(x.NodeId) })
                    .Where(x => x.Content != null && x.Content.IsPublished(culture))
                    .Where(x => pathFilter == null || ("," + x.Content!.Path + ",").Contains(pathFilter))
                    .Select(x => new SearchResultItem
                    {
                        Content = x.Content!,
                        Score = x.SearchResult.Score
                    })
                    .ToList();

                result.TotalResults = allMatched.Count;
                result.TotalPages = (int)Math.Ceiling((double)allMatched.Count / pageSize);

                // Apply pagination in-memory
                var skip = (Math.Max(page, 1) - 1) * pageSize;
                result.Items = allMatched.Skip(skip).Take(pageSize).ToList();
            }
        }

        return result;
    }

    /// <summary>
    /// Apply dynamic ordering from string array.
    /// Format: "fieldName [SortType] [asc|desc]"
    /// Examples: "updateDate Long desc", "nodeName String asc", "sortOrder Int"
    /// <summary>
    /// Applies the requested field ordering to a query.
    /// </summary>
    /// <param name="orderParams">Ordering specifications containing a field name, optional sort type, and optional <c>desc</c> direction.</param>
    /// <returns>The query with the specified ordering applied.</returns>
    private static IOrdering ApplyOrdering(IBooleanOperation query, string[]? orderParams)
    {
        if (orderParams == null || orderParams.Length == 0)
            return query;

        IOrdering result = query;
        foreach (var order in orderParams)
        {
            var parts = order.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var fieldName = parts[0];
            var sortType = SortType.String;
            var descending = false;

            if (parts.Length > 1 && Enum.TryParse<SortType>(parts[1], true, out var parsedType))
            {
                sortType = parsedType;
            }

            if (parts.Length > 2 && parts[^1].Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                descending = true;
            }
            else if (parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
            {
                descending = true;
            }

            var sortableField = new SortableField(fieldName, sortType);
            result = descending
                ? result.OrderByDescending(sortableField)
                : result.OrderBy(sortableField);
        }

        return result;
    }

    /// <summary>
    /// Expand query with synonyms if uTPro.Feature.SearchPlus is installed.
    /// Uses runtime service resolution so SearchPlus remains an optional dependency.
    /// Falls back to original query if SearchPlus is not installed.
    /// <summary>
    /// Expands a search query with available synonym terms.
    /// </summary>
    /// <param name="query">The search query to expand.</param>
    /// <returns>The expanded search terms, or the original query when synonym expansion is unavailable.</returns>
    private IReadOnlyList<string> ExpandWithSynonyms(string query)
    {
        try
        {
            // Try to resolve ISynonymProvider from DI (only available if SearchPlus is installed)
            var synonymProviderType = Type.GetType(
                "uTPro.Feature.SearchPlus.Services.ISynonymProvider, uTPro.Feature.SearchPlus");

            if (synonymProviderType == null)
                return new[] { query };

            var synonymProvider = _serviceProvider.GetService(synonymProviderType);
            if (synonymProvider == null)
                return new[] { query };

            // Call Expand(string) via reflection to avoid compile-time dependency
            var expandMethod = synonymProviderType.GetMethod("Expand");
            if (expandMethod == null)
                return new[] { query };

            var result = expandMethod.Invoke(synonymProvider, new object[] { query }) as IReadOnlyList<string>;
            return result is { Count: > 0 } ? result : new[] { query };
        }
        catch
        {
            return new[] { query };
        }
    }
}
