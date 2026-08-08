using Examine;
using Examine.Search;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using uTPro.Extension.CurrentSite;

namespace uTPro.Extension.Search;

/// <summary>
/// Full-text search service using Umbraco's Examine ExternalIndex (Lucene).
/// Uses ManagedQuery for proper tokenization and relevance scoring,
/// combined with in-memory path filtering for scope restriction.
/// </summary>
internal sealed class SearchExtension : ISearchExtension
{
    private readonly IExamineManager _examineManager;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly ICurrentSiteExtension _currentSite;

    public SearchExtension(
        IExamineManager examineManager,
        IUmbracoContextAccessor umbracoContextAccessor,
        ICurrentSiteExtension currentSite)
    {
        _examineManager = examineManager;
        _umbracoContextAccessor = umbracoContextAccessor;
        _currentSite = currentSite;
    }

    /// <inheritdoc />
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

        // ManagedQuery handles tokenization, fuzzy matching, and field boosting automatically
        IBooleanOperation boolOp;
        if (fields != null && fields.Length > 0)
        {
            boolOp = examineQuery.ManagedQuery(query.Trim(), fields);
        }
        else
        {
            boolOp = examineQuery.ManagedQuery(query.Trim());
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
                var allMatched = new List<SearchResultItem>();

                foreach (var searchResult in searchResults)
                {
                    if (!int.TryParse(searchResult.Id, out var nodeId))
                        continue;

                    var content = contentCache.GetById(nodeId);
                    if (content == null || !content.IsPublished(culture))
                        continue;

                    // Path scope filter: check if content is a descendant of the scope node
                    if (pathFilter != null)
                    {
                        var contentPath = "," + content.Path + ",";
                        if (!contentPath.Contains(pathFilter))
                            continue;
                    }

                    allMatched.Add(new SearchResultItem
                    {
                        Content = content,
                        Score = searchResult.Score
                    });
                }

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
    /// </summary>
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
}
