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
using Umbraco.Extensions;

namespace uTPro.Project.Web.Configure
{
    /// <summary>
    /// Makes "container" nodes (see <see cref="HiddenContainerAliases"/>, e.g. <c>globalFolderPages</c>)
    /// transparent in the public URL: a child at
    /// <c>/huong-dan/<b>pages</b>/hd1</c> in the content tree is served at <c>/huong-dan/hd1</c>.
    ///
    /// <para>Two halves work together:</para>
    /// <list type="bullet">
    ///   <item><see cref="TransparentContainerUrlProvider"/> — outbound: builds the clean URL
    ///     (drops the container segment) whenever a node sits under a container.</item>
    ///   <item><see cref="TransparentContainerContentFinder"/> — inbound: resolves an incoming
    ///     clean URL back to the real node by walking the tree and stepping "through" containers.</item>
    /// </list>
    /// </summary>
    public sealed class TransparentContainerUrlProvider : IUrlProvider
    {
        private readonly HiddenContainerAliases _hidden;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly IPublishedUrlProvider _publishedUrlProvider;
        private readonly IDocumentUrlService _documentUrlService;

        public TransparentContainerUrlProvider(
            HiddenContainerAliases hidden,
            IVariationContextAccessor variationContextAccessor,
            IPublishedUrlProvider publishedUrlProvider,
            IDocumentUrlService documentUrlService)
        {
            _hidden = hidden;
            _variationContextAccessor = variationContextAccessor;
            _publishedUrlProvider = publishedUrlProvider;
            _documentUrlService = documentUrlService;
        }

        public string Alias => "uTProTransparentContainerUrlProvider";

        // Resolves a node's URL segment via the non-obsolete IDocumentUrlService. Falls back to the
        // ambient variation context culture when no explicit culture is supplied.
        private string? GetSegment(IPublishedContent node, string? culture)
            => _documentUrlService.GetUrlSegment(
                node.Key,
                culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
                false);

        public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
        {
            // The container itself has no URL — leave it to HiddenUrlProvider (which returns a
            // "no URL" message). Without this guard we'd fabricate a URL for the container, because
            // it also sits under the always-hidden root containers (globalFolderRoot / globalFolderSites).
            if (_hidden.Contains(content.ContentType.Alias))
            {
                return null;
            }

            // Only act when this node actually sits under a hidden container; otherwise let the
            // default provider build the URL as usual.
            if (!content.Ancestors().Any(a => _hidden.Contains(a.ContentType.Alias)))
            {
                return null;
            }

            // Nearest ancestor that is NOT a hidden container — its URL is our starting point.
            // Resolving that ancestor through the aggregate provider re-enters this provider for
            // that node, which returns null (it isn't under a container) so the default provider
            // builds it — no infinite recursion.
            var basis = content.Ancestors().FirstOrDefault(a => !_hidden.Contains(a.ContentType.Alias));
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

            // Segments from just below `basis` down to `content`, skipping any hidden containers.
            // AncestorsOrSelf() yields self first then walks up; TakeWhile stops before `basis`.
            var tail = content.AncestorsOrSelf()
                .TakeWhile(a => a.Key != basis.Key)
                .Where(a => !_hidden.Contains(a.ContentType.Alias))
                .Reverse()
                .Select(a => GetSegment(a, culture))
                .Where(s => !string.IsNullOrEmpty(s));

            foreach (var segment in tail)
            {
                path += "/" + segment;
            }

            if (trailingSlash)
            {
                path += "/";
            }

            return UrlInfo.AsUrl(path, Alias, culture);
        }

        public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

        // Preview always uses the real tree path; defer to the default provider.
        public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
            => Task.FromResult<UrlInfo?>(null);
    }

    /// <summary>
    /// Resolves an incoming "clean" URL (with container segments omitted) back to the real content
    /// node. Runs only as a fallback — after the default content finders have failed — and walks
    /// the tree from the domain root, treating hidden containers as transparent (their children are
    /// matched as if they were direct children of the container's parent).
    /// </summary>
    public sealed class TransparentContainerContentFinder : IContentFinder
    {
        private readonly HiddenContainerAliases _hidden;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly IDocumentNavigationQueryService _navigationQueryService;
        private readonly IPublishedContentStatusFilteringService _publishedStatusFilteringService;
        private readonly IDocumentUrlService _documentUrlService;

        public TransparentContainerContentFinder(
            HiddenContainerAliases hidden,
            IUmbracoContextAccessor umbracoContextAccessor,
            IVariationContextAccessor variationContextAccessor,
            IDocumentNavigationQueryService navigationQueryService,
            IPublishedContentStatusFilteringService publishedStatusFilteringService,
            IDocumentUrlService documentUrlService)
        {
            _hidden = hidden;
            _umbracoContextAccessor = umbracoContextAccessor;
            _variationContextAccessor = variationContextAccessor;
            _navigationQueryService = navigationQueryService;
            _publishedStatusFilteringService = publishedStatusFilteringService;
            _documentUrlService = documentUrlService;
        }

        // Resolves a node's URL segment via the non-obsolete IDocumentUrlService.
        private string? GetSegment(IPublishedContent node, string? culture)
            => _documentUrlService.GetUrlSegment(
                node.Key,
                culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
                false);

        public Task<bool> TryFindContent(IPublishedRequestBuilder request)
        {
            // Something already matched (default finders) — don't interfere.
            if (request.PublishedContent is not null)
            {
                return Task.FromResult(false);
            }

            // We resolve relative to the matched domain's root node.
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

            // Path relative to the domain (strip any path-based domain prefix, e.g. "/en").
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
            var culture = request.Culture;

            var current = root;
            var skippedContainer = false;

            foreach (var segment in segments)
            {
                var match = FindChild(current, segment, culture, ref skippedContainer);
                if (match is null)
                {
                    return Task.FromResult(false);
                }

                current = match;
            }

            // If nothing was skipped, the default finders would already have resolved this URL,
            // so there's no reason to claim it here.
            if (!skippedContainer)
            {
                return Task.FromResult(false);
            }

            request.SetPublishedContent(current);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Finds the child of <paramref name="parent"/> whose URL segment matches
        /// <paramref name="segment"/>. Hidden containers are stepped through transparently: their
        /// own children are considered as candidates (and this is recorded via
        /// <paramref name="skippedContainer"/>).
        /// </summary>
        private IPublishedContent? FindChild(
            IPublishedContent parent, string segment, string? culture, ref bool skippedContainer)
        {
            var children = parent.Children(_navigationQueryService, _publishedStatusFilteringService)
                ?? Enumerable.Empty<IPublishedContent>();

            foreach (var child in children)
            {
                if (_hidden.Contains(child.ContentType.Alias))
                {
                    var inner = FindChild(child, segment, culture, ref skippedContainer);
                    if (inner is not null)
                    {
                        skippedContainer = true;
                        return inner;
                    }

                    continue;
                }

                if (string.Equals(
                        GetSegment(child, culture),
                        segment,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Registers the transparent-container URL provider (before the default provider) and content
    /// finder (after the default finders, as a fallback).
    /// </summary>
    public sealed class TransparentContainerComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Shared alias set (also registered by HiddenUrlComposer — TryAdd keeps it a singleton).
            builder.Services.TryAddSingleton<HiddenContainerAliases>();

            builder.UrlProviders().Insert<TransparentContainerUrlProvider>();
            builder.ContentFinders().Append<TransparentContainerContentFinder>();
        }
    }
}
