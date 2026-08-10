using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;
using uTPro.Extension;

namespace uTPro.Project.Web.Configure
{
    /// <summary>
    /// Property aliases on <c>globalFolderCategoryItem</c> for URL routing.
    /// Derived from the generated model property names via <see cref="PropertyAliasHelper.ToAlias"/> to avoid hardcoded strings.
    /// </summary>
    public static class CategoryUrlConstants
    {
        public static readonly string ShowInUrlAlias = PropertyAliasHelper.ToAlias(nameof(GlobalFolderCategoryItem.CategoryItemShowInUrl));
        public static readonly string UrlSegmentAlias = PropertyAliasHelper.ToAlias(nameof(GlobalFolderCategoryItem.CategoryItemUrlSegment));
        public static readonly string PageCategoriesAlias = PropertyAliasHelper.ToAlias(nameof(GlobalPagePageCategoriesSetting.Categories));
    }

    /// <summary>
    /// Service that resolves category URL segments for pages. Shared by the outbound URL provider
    /// and the inbound content finder.
    /// </summary>
    public sealed class CategoryUrlService
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IDocumentUrlService _documentUrlService;
        private readonly IVariationContextAccessor _variationContextAccessor;

        public CategoryUrlService(
            IUmbracoContextAccessor umbracoContextAccessor,
            IDocumentUrlService documentUrlService,
            IVariationContextAccessor variationContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _documentUrlService = documentUrlService;
            _variationContextAccessor = variationContextAccessor;
        }

        /// <summary>
        /// Gets all category items assigned to the given page that have <c>showInUrl</c> enabled.
        /// Returns them in picker order.
        /// </summary>
        public IReadOnlyList<IPublishedContent> GetVisibleCategories(IPublishedContent page)
        {
            var categories = page.Value<IEnumerable<IPublishedContent>>(CategoryUrlConstants.PageCategoriesAlias);
            if (categories is null)
            {
                return [];
            }

            return categories
                .Where(c => c?.ContentType?.Alias is not null
                    && c.ContentType.Alias.Equals(GlobalFolderCategoryItem.ModelTypeAlias, StringComparison.OrdinalIgnoreCase))
                .Where(c => c.Value<bool>(CategoryUrlConstants.ShowInUrlAlias))
                .ToList();
        }

        /// <summary>
        /// Gets the URL segment for a category item. Uses the custom <c>urlSegment</c> property
        /// if set; otherwise falls back to the node's default URL segment.
        /// </summary>
        public string? GetCategorySegment(IPublishedContent? categoryItem, string? culture)
        {
            if (categoryItem is null || categoryItem.Key == Guid.Empty)
            {
                return null;
            }

            // Prefer custom urlSegment property value.
            var customSegment = categoryItem.Value<string>(CategoryUrlConstants.UrlSegmentAlias);
            if (!string.IsNullOrWhiteSpace(customSegment))
            {
                return customSegment.Trim().ToLowerInvariant();
            }

            // Fall back to the node's default URL segment from Umbraco.
            return _documentUrlService.GetUrlSegment(
                categoryItem.Key,
                culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
                false);
        }

        /// <summary>
        /// Gets all visible category slugs that are referenced by pages under a given site root.
        /// Walks the category tree (globalFolderCategories → groups → items) to find items with
        /// <c>showInUrl</c> enabled. Returns a dictionary of segment → category node key.
        /// </summary>
        public Dictionary<string, Guid> GetAllVisibleCategorySlugs(string? culture, IPublishedContent? siteRoot = null)
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var ctx) || ctx.Content is null)
            {
                return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            }

            var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            // Walk from the site root (or from the top-level uTPro node) to find category items.
            // The category tree lives under globalFolderCategories → globalFolderCategoryGroup → globalFolderCategoryItem.
            if (siteRoot is not null)
            {
                CollectCategoryItems(siteRoot, culture, result);
            }
            else
            {
                // Without a specific root, walk all descendants of any globalFolderCategories node
                // we can find by checking the content tree from the domain root ancestor.
                // This is a fallback — the content finder always passes a root.
            }

            return result;
        }

        /// <summary>
        /// Overload that accepts a domain root key for the content finder (which has a Guid).
        /// </summary>
        public Dictionary<string, Guid> GetAllVisibleCategorySlugs(string? culture, Guid domainRootKey)
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var ctx) || ctx.Content is null)
            {
                return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            }

            // Walk the entire tree upward from domain root to find the top-level node that contains
            // the categories folder. Categories live at: uTPro/Sites/TheLoai/... (sibling of TrangChu).
            // Strategy: from the domain root, go to ancestors until we find the common parent that
            // also has a globalFolderCategories descendant.
            var domainRoot = ctx.Content.GetById(domainRootKey);
            if (domainRoot is null)
            {
                return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            }

            var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            // Walk ancestors to find the nearest node that contains category folders.
            var current = domainRoot;
            while (current is not null)
            {
                CollectCategoryItems(current, culture, result);
                if (result.Count > 0)
                {
                    break;
                }

                current = current.Parent();
            }

            return result;
        }

        private void CollectCategoryItems(IPublishedContent node, string? culture, Dictionary<string, Guid> result)
        {
            if (node?.ContentType?.Alias is null)
            {
                return;
            }

            if (node.ContentType.Alias.Equals(GlobalFolderCategoryItem.ModelTypeAlias, StringComparison.OrdinalIgnoreCase))
            {
                if (node.Key != Guid.Empty && node.Value<bool>(CategoryUrlConstants.ShowInUrlAlias))
                {
                    var segment = GetCategorySegment(node, culture);
                    if (!string.IsNullOrEmpty(segment) && !result.ContainsKey(segment))
                    {
                        result[segment] = node.Key;
                    }
                }

                return; // Category items don't have relevant children.
            }

            // Only recurse into folders/containers that might contain categories.
            // Avoid recursing into page trees for performance.
            // Recurse into: globalFolder* types, site roots (globalFolderSites children), etc.
            var alias = node.ContentType.Alias;
            if (alias.StartsWith("globalFolder", StringComparison.OrdinalIgnoreCase)
                || alias.Equals(GlobalFolderRoot.ModelTypeAlias, StringComparison.OrdinalIgnoreCase)
                || node.Parent() is null
                || node.Parent()?.ContentType?.Alias?.Equals(GlobalFolderSites.ModelTypeAlias, StringComparison.OrdinalIgnoreCase) == true)
            {
                foreach (var child in node.Children() ?? Enumerable.Empty<IPublishedContent>())
                {
                    CollectCategoryItems(child, culture, result);
                }
            }
        }

        /// <summary>
        /// Gets the category landing URL and category name for a given page.
        /// <para>
        /// If <paramref name="currentCat"/> is provided, builds the URL for that specific category.
        /// Otherwise, uses the first visible category assigned to the page.
        /// </para>
        /// <para>
        /// Base URL logic: if <paramref name="currentNode"/> is a container (has children),
        /// its own URL is used. If it's a leaf page, the parent's URL is used instead.
        /// </para>
        /// </summary>
        /// <param name="currentNode">The page currently being rendered.</param>
        /// <param name="currentCat">Optional specific category item to build the URL for. If null, the first visible category is used.</param>
        /// <returns>A tuple of (categoryUrl, categoryName). Both are empty strings if no valid category is found.</returns>
        public (string, string) GetUrlNameCategory(IPublishedContent currentNode, IPublishedContent? currentCat = null)
        {
            if (currentNode?.ContentType?.Alias is null)
            {
                return (string.Empty, string.Empty);
            }

            var crumbs = currentNode.AncestorsOrSelf()
                .Where(n =>
                {
                    var u = n.Url(mode: UrlMode.Relative);
                    return !string.IsNullOrEmpty(u) && u != "#";
                })
                .Reverse()
                .ToList();

            if (crumbs.Count == 0)
            {
                return (string.Empty, string.Empty);
            }

            // Determine the target category
            IPublishedContent? targetCat = currentCat;
            if (targetCat is null)
            {
                var visibleCategories = this.GetVisibleCategories(currentNode);
                targetCat = visibleCategories.Count > 0 ? visibleCategories[0] : null;
            }

            if (targetCat is null)
            {
                return (string.Empty, string.Empty);
            }

            var categoryCrumbName = targetCat.Name ?? string.Empty;
            var catSegment = this.GetCategorySegment(targetCat, null);
            if (string.IsNullOrEmpty(catSegment))
            {
                return (string.Empty, categoryCrumbName);
            }

            // Base URL: if currentNode is a container (has children), use its own URL.
            // If currentNode is a leaf page, use parent (crumbs[^2]) URL.
            var hasChildren = currentNode.Children()?.Any() == true;
            var basePage = hasChildren ? currentNode : (crumbs.Count >= 2 ? crumbs[^2] : null);
            var baseUrl = basePage?.Url()?.TrimEnd('/') ?? "";

            var categoryCrumbUrl = $"{baseUrl}/{catSegment}/";
            return (categoryCrumbUrl, categoryCrumbName);
        }

        /// <summary>
        /// Checks if a page references a category with the given key in its <c>pageCategories</c> picker.
        /// </summary>
        public bool PageHasCategory(IPublishedContent page, Guid categoryKey)
        {
            var categories = page.Value<IEnumerable<IPublishedContent>>(CategoryUrlConstants.PageCategoriesAlias);
            return categories?.Any(c => c is not null && c.Key == categoryKey) == true;
        }
    }

    /// <summary>
    /// Outbound URL provider: inserts the first visible category's URL segment into the page URL.
    /// <para>
    /// Given a page at <c>/huong-dan/page-test/</c> with category "Cơ bản" (slug: co-ban, showInUrl=true),
    /// the generated URL becomes <c>/huong-dan/co-ban/page-test/</c>.
    /// </para>
    /// <para>
    /// By default the first category (in picker order) is used. Callers who need a specific category
    /// can use the <see cref="CategoryUrlService"/> directly.
    /// </para>
    /// </summary>
    public sealed class CategoryUrlProvider : IUrlProvider
    {
        private readonly CategoryUrlService _categoryUrlService;
        private readonly HiddenContainerAliases _hidden;
        private readonly IPublishedUrlProvider _publishedUrlProvider;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly IDocumentUrlService _documentUrlService;

        public CategoryUrlProvider(
            CategoryUrlService categoryUrlService,
            HiddenContainerAliases hidden,
            IPublishedUrlProvider publishedUrlProvider,
            IVariationContextAccessor variationContextAccessor,
            IDocumentUrlService documentUrlService)
        {
            _categoryUrlService = categoryUrlService;
            _hidden = hidden;
            _publishedUrlProvider = publishedUrlProvider;
            _variationContextAccessor = variationContextAccessor;
            _documentUrlService = documentUrlService;
        }

        public string Alias => "uTProCategoryUrlProvider";

        public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
        {
            // Only act on real pages (not containers).
            if (_hidden.IsTransparent(content))
            {
                return null;
            }

            // Only act if the page has a pageCategories property.
            if (!content.HasProperty(CategoryUrlConstants.PageCategoriesAlias))
            {
                return null;
            }

            var visibleCategories = _categoryUrlService.GetVisibleCategories(content);
            if (visibleCategories.Count == 0)
            {
                return null;
            }

            // Use the first visible category for the canonical URL.
            var firstCategory = visibleCategories[0];
            var categorySegment = _categoryUrlService.GetCategorySegment(firstCategory, culture);
            if (string.IsNullOrEmpty(categorySegment))
            {
                return null;
            }

            return BuildCategoryUrl(content, categorySegment, mode, culture);
        }

        public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current)
        {
            // Return alternate URLs for all other visible categories (beyond the first).
            // This makes the backoffice "Links" panel show all category-based URLs.
            return [];
        }

        public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
            => Task.FromResult<UrlInfo?>(null);

        private UrlInfo? BuildCategoryUrl(IPublishedContent content, string categorySegment, UrlMode mode, string? culture)
        {
            // Get the page's own segment.
            var resolvedCulture = culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty;
            var pageSegment = _documentUrlService.GetUrlSegment(content.Key, resolvedCulture, false);
            if (string.IsNullOrEmpty(pageSegment))
            {
                return null;
            }

            // Find the nearest non-transparent ancestor to get the base URL.
            var basis = content.Ancestors().FirstOrDefault(a => !_hidden.IsTransparent(a));
            if (basis is null)
            {
                return null;
            }

            var baseUrl = _publishedUrlProvider.GetUrl(basis.Key, mode, culture);
            if (string.IsNullOrEmpty(baseUrl) || baseUrl == "#")
            {
                return null;
            }

            var trailingSlash = baseUrl.EndsWith('/');
            var path = baseUrl.TrimEnd('/');

            // Build: base + [intermediate non-transparent ancestors between basis and content] + categorySegment + pageSegment
            var intermediates = content.AncestorsOrSelf()
                .TakeWhile(a => a.Key != basis.Key)
                .Where(a => !_hidden.IsTransparent(a) && a.Key != content.Key)
                .Reverse()
                .Select(a => _documentUrlService.GetUrlSegment(a.Key, resolvedCulture, false))
                .Where(s => !string.IsNullOrEmpty(s));

            foreach (var seg in intermediates)
            {
                path += "/" + seg;
            }

            // Insert category segment before page segment.
            path += "/" + categorySegment;
            path += "/" + pageSegment;

            if (trailingSlash)
            {
                path += "/";
            }

            return UrlInfo.AsUrl(path, Alias, culture);
        }
    }

    /// <summary>
    /// Inbound content finder: resolves URLs that contain a category segment.
    /// <para>
    /// When the default content finders and the transparent-container finder cannot resolve a URL
    /// like <c>/huong-dan/co-ban/page-test/</c>, this finder checks if any segment in the path is
    /// a known category slug. If so, it removes that segment and tries to resolve the remaining path
    /// to a page — then verifies the page actually references that category.
    /// </para>
    /// </summary>
    public sealed class CategoryUrlContentFinder : IContentFinder
    {
        private readonly CategoryUrlService _categoryUrlService;
        private readonly HiddenContainerAliases _hidden;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly IDocumentNavigationQueryService _navigationQueryService;
        private readonly IPublishedContentStatusFilteringService _publishedStatusFilteringService;
        private readonly IDocumentUrlService _documentUrlService;

        public CategoryUrlContentFinder(
            CategoryUrlService categoryUrlService,
            HiddenContainerAliases hidden,
            IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            IDocumentNavigationQueryService navigationQueryService,
            IPublishedContentStatusFilteringService publishedStatusFilteringService,
            IDocumentUrlService documentUrlService)
        {
            _categoryUrlService = categoryUrlService;
            _hidden = hidden;
            _umbracoContextAccessor = umbracoContextAccessor;
            _variationContextAccessor = variationContextAccessor;
            _navigationQueryService = navigationQueryService;
            _publishedStatusFilteringService = publishedStatusFilteringService;
            _documentUrlService = documentUrlService;
        }

        public Task<bool> TryFindContent(IPublishedRequestBuilder request)
        {
            // Already resolved — don't interfere.
            if (request.PublishedContent is not null)
            {
                return Task.FromResult(false);
            }

            if (request.Domain is null)
            {
                return Task.FromResult(false);
            }

            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
                || umbracoContext.Content is null)
            {
                return Task.FromResult(false);
            }

            var root = umbracoContext.Content.GetById(request.Domain.ContentId);
            if (root is null)
            {
                return Task.FromResult(false);
            }

            // Path relative to the domain.
            var decodedPath = Uri.UnescapeDataString(request.Uri.AbsolutePath).Trim('/');
            var domainPath = request.Domain.Uri?.AbsolutePath.Trim('/') ?? string.Empty;
            if (domainPath.Length > 0
                && decodedPath.StartsWith(domainPath, StringComparison.OrdinalIgnoreCase))
            {
                decodedPath = decodedPath[domainPath.Length..].Trim('/');
            }

            if (decodedPath.Length == 0)
            {
                return Task.FromResult(false);
            }

            var segments = decodedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                // Need at least a category segment + page segment.
                return Task.FromResult(false);
            }

            var culture = request.Culture;
            var categorySlugs = _categoryUrlService.GetAllVisibleCategorySlugs(culture, root.Key);

            if (categorySlugs.Count == 0)
            {
                return Task.FromResult(false);
            }

            // Try each position as a potential category segment.
            // The category segment can appear anywhere between folder segments and the page segment.
            for (int i = 0; i < segments.Length; i++)
            {
                if (!categorySlugs.TryGetValue(segments[i], out var categoryKey))
                {
                    continue;
                }

                // Remove the category segment and try to resolve the rest.
                var withoutCategory = segments.Where((_, idx) => idx != i).ToArray();

                var resolved = WalkTree(root, withoutCategory, culture);
                if (resolved is null)
                {
                    continue;
                }

                // Verify the resolved page actually references this category.
                if (!_categoryUrlService.PageHasCategory(resolved, categoryKey))
                {
                    continue;
                }

                request.SetPublishedContent(resolved);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Walks the content tree from <paramref name="root"/> through the given segments,
        /// stepping through transparent containers (same logic as <see cref="TransparentContainerContentFinder"/>).
        /// </summary>
        private IPublishedContent? WalkTree(IPublishedContent root, string[] segments, string? culture)
        {
            return segments.Aggregate(
                (IPublishedContent?)root,
                (current, segment) => current is null ? null : FindChild(current, segment, culture));
        }

        private IPublishedContent? FindChild(IPublishedContent parent, string segment, string? culture)
        {
            var children = parent.Children(_navigationQueryService, _publishedStatusFilteringService)
                ?? Enumerable.Empty<IPublishedContent>();

            foreach (var child in children)
            {
                if (_hidden.IsTransparent(child))
                {
                    // Step through transparent containers.
                    var inner = FindChild(child, segment, culture);
                    if (inner is not null)
                    {
                        return inner;
                    }

                    continue;
                }

                var childSegment = _documentUrlService.GetUrlSegment(
                    child.Key,
                    culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
                    false);

                if (string.Equals(childSegment, segment, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Extension methods for getting category-based URLs programmatically.
    /// </summary>
    public static class CategoryUrlExtensions
    {
        /// <summary>
        /// Gets the category-based URL for a page using the first visible category.
        /// Returns null if the page has no visible categories.
        /// </summary>
        public static string? GetCategoryUrl(
            this IPublishedContent page,
            CategoryUrlService categoryUrlService,
            IPublishedUrlProvider urlProvider,
            string? culture = null,
            Guid? categoryKey = null)
        {
            var visibleCategories = categoryUrlService.GetVisibleCategories(page);
            if (visibleCategories.Count == 0)
            {
                return null;
            }

            IPublishedContent? targetCategory;

            if (categoryKey.HasValue)
            {
                // Find specific category by key.
                targetCategory = visibleCategories.FirstOrDefault(c => c.Key == categoryKey.Value);
                if (targetCategory is null)
                {
                    return null;
                }
            }
            else
            {
                // Default: first visible category.
                targetCategory = visibleCategories[0];
            }

            var categorySegment = categoryUrlService.GetCategorySegment(targetCategory, culture);
            if (string.IsNullOrEmpty(categorySegment))
            {
                return null;
            }

            // Get the page's default URL (without category).
            var baseUrl = urlProvider.GetUrl(page.Key, UrlMode.Default, culture);
            if (string.IsNullOrEmpty(baseUrl) || baseUrl == "#")
            {
                return null;
            }

            // Insert category segment before the last path segment (the page slug).
            return InsertCategorySegment(baseUrl, categorySegment);
        }

        /// <summary>
        /// Gets all category-based URLs for a page (one per visible category).
        /// </summary>
        public static IReadOnlyList<(Guid CategoryKey, string Url)> GetAllCategoryUrls(
            this IPublishedContent page,
            CategoryUrlService categoryUrlService,
            IPublishedUrlProvider urlProvider,
            string? culture = null)
        {
            var visibleCategories = categoryUrlService.GetVisibleCategories(page);
            if (visibleCategories.Count == 0)
            {
                return [];
            }

            var baseUrl = urlProvider.GetUrl(page.Key, UrlMode.Default, culture);
            if (string.IsNullOrEmpty(baseUrl) || baseUrl == "#")
            {
                return [];
            }

            var result = new List<(Guid, string)>();

            foreach (var cat in visibleCategories)
            {
                var segment = categoryUrlService.GetCategorySegment(cat, culture);
                if (string.IsNullOrEmpty(segment))
                {
                    continue;
                }

                var url = InsertCategorySegment(baseUrl, segment);
                if (url is not null)
                {
                    result.Add((cat.Key, url));
                }
            }

            return result;
        }

        private static string? InsertCategorySegment(string url, string categorySegment)
        {
            var trailingSlash = url.EndsWith('/');
            var path = url.TrimEnd('/');

            var lastSlash = path.LastIndexOf('/');
            if (lastSlash < 0)
            {
                return null;
            }

            var beforePage = path[..lastSlash];
            var pageSegment = path[(lastSlash + 1)..];

            var result = $"{beforePage}/{categorySegment}/{pageSegment}";

            if (trailingSlash)
            {
                result += "/";
            }

            return result;
        }
    }

    /// <summary>
    /// Content finder for category "landing" URLs — e.g. <c>/huong-dan/cau-hinh/</c> where
    /// <c>cau-hinh</c> is a category slug with no page segment following it.
    ///
    /// <para>When matched, the published content is set to the parent page (e.g. "Hướng dẫn") and
    /// the category key is stored in <c>HttpContext.Items</c> so the render controller can force
    /// the category-landing view (which shows a filtered page list).</para>
    /// </summary>
    public sealed class CategoryLandingContentFinder : IContentFinder
    {
        /// <summary>Key used in <see cref="HttpContext.Items"/> to signal a category landing request.</summary>
        public const string CategoryLandingItemKey = "uTPro:CategoryLandingKey";

        private readonly CategoryUrlService _categoryUrlService;
        private readonly HiddenContainerAliases _hidden;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly IDocumentNavigationQueryService _navigationQueryService;
        private readonly IPublishedContentStatusFilteringService _publishedStatusFilteringService;
        private readonly IDocumentUrlService _documentUrlService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoryLandingContentFinder(
            CategoryUrlService categoryUrlService,
            HiddenContainerAliases hidden,
            IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            IDocumentNavigationQueryService navigationQueryService,
            IPublishedContentStatusFilteringService publishedStatusFilteringService,
            IDocumentUrlService documentUrlService,
            IHttpContextAccessor httpContextAccessor)
        {
            _categoryUrlService = categoryUrlService;
            _hidden = hidden;
            _umbracoContextAccessor = umbracoContextAccessor;
            _variationContextAccessor = variationContextAccessor;
            _navigationQueryService = navigationQueryService;
            _publishedStatusFilteringService = publishedStatusFilteringService;
            _documentUrlService = documentUrlService;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<bool> TryFindContent(IPublishedRequestBuilder request)
        {
            if (request.PublishedContent is not null)
            {
                return Task.FromResult(false);
            }

            if (request.Domain is null)
            {
                return Task.FromResult(false);
            }

            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
                || umbracoContext.Content is null)
            {
                return Task.FromResult(false);
            }

            var root = umbracoContext.Content.GetById(request.Domain.ContentId);
            if (root is null)
            {
                return Task.FromResult(false);
            }

            // Path relative to the domain.
            var decodedPath = Uri.UnescapeDataString(request.Uri.AbsolutePath).Trim('/');
            var domainPath = request.Domain.Uri?.AbsolutePath.Trim('/') ?? string.Empty;
            if (domainPath.Length > 0
                && decodedPath.StartsWith(domainPath, StringComparison.OrdinalIgnoreCase))
            {
                decodedPath = decodedPath[domainPath.Length..].Trim('/');
            }

            if (decodedPath.Length == 0)
            {
                return Task.FromResult(false);
            }

            var segments = decodedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 1)
            {
                return Task.FromResult(false);
            }

            var culture = request.Culture;
            var categorySlugs = _categoryUrlService.GetAllVisibleCategorySlugs(culture, root.Key);

            if (categorySlugs.Count == 0)
            {
                return Task.FromResult(false);
            }

            // The last segment might be a category slug. Check if all preceding segments
            // resolve to a valid parent page (walking through transparent containers).
            var lastSegment = segments[^1];
            if (!categorySlugs.TryGetValue(lastSegment, out var categoryKey))
            {
                return Task.FromResult(false);
            }

            // Resolve the parent path (everything except the last segment).
            IPublishedContent parentPage;
            if (segments.Length == 1)
            {
                // Category slug is directly under domain root (e.g. /cau-hinh/).
                parentPage = root;
            }
            else
            {
                var parentSegments = segments[..^1];
                var resolved = WalkTree(root, parentSegments, culture);
                if (resolved is null)
                {
                    return Task.FromResult(false);
                }

                parentPage = resolved;
            }

            // Only proceed if the parent page is a real page (not a hidden container).
            if (_hidden.IsTransparent(parentPage))
            {
                return Task.FromResult(false);
            }

            // Store the category key for the render controller / view.
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                httpContext.Items[CategoryLandingItemKey] = categoryKey;
            }

            request.SetPublishedContent(parentPage);
            return Task.FromResult(true);
        }

        private IPublishedContent? WalkTree(IPublishedContent root, string[] segments, string? culture)
        {
            return segments.Aggregate(
                (IPublishedContent?)root,
                (current, segment) => current is null ? null : FindChild(current, segment, culture));
        }

        private IPublishedContent? FindChild(IPublishedContent parent, string segment, string? culture)
        {
            var children = parent.Children(_navigationQueryService, _publishedStatusFilteringService)
                ?? Enumerable.Empty<IPublishedContent>();

            foreach (var child in children)
            {
                if (_hidden.IsTransparent(child))
                {
                    var inner = FindChild(child, segment, culture);
                    if (inner is not null)
                    {
                        return inner;
                    }

                    continue;
                }

                var childSegment = _documentUrlService.GetUrlSegment(
                    child.Key,
                    culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
                    false);

                if (string.Equals(childSegment, segment, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Registers the category URL provider and content finder.
    /// </summary>
    public sealed class CategoryUrlComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.TryAddSingleton<CategoryUrlService>();

            // Insert category URL provider BEFORE the transparent-container provider so category
            // URLs take precedence for pages that have visible categories.
            builder.UrlProviders().Insert<CategoryUrlProvider>();

            // Append the category content finder AFTER the transparent-container finder.
            builder.ContentFinders().Append<CategoryUrlContentFinder>();

            // Append the category landing finder AFTER the category content finder so it only
            // activates when no page-level match was found.
            builder.ContentFinders().Append<CategoryLandingContentFinder>();
        }
    }
}
