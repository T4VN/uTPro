using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace uTPro.Extension.CurrentSite
{
    class DICurrentItemExtension : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<ICurrentItemExtension, CurrentItemExtension>();
    }

    public interface ICurrentItemExtension
    {
        GlobalRoot Root { get; }
        GlobalFolderSites FolderSite { get; }
        GlobalFolderSettings FolderSettings { get; }
        IPublishedContent? Current { get; }
        IPublishedContent? PageHome { get; }
        IPublishedContent? PageErrors { get; }
    }

    /// <summary>
    /// Scoped service that resolves the current site's content tree nodes.
    /// All properties are memoized per-request to avoid repeated tree traversals
    /// and domain lookups (previously each property getter re-computed from scratch
    /// on every access — 20+ times per page render).
    /// </summary>
    internal sealed class CurrentItemExtension(
        ILogger<CurrentItemExtension> logger,
        ICurrentSiteExtension currentSite
        ) : ICurrentItemExtension
    {
        private readonly ICurrentSiteExtension _currentSite = currentSite;
        private readonly ILogger<CurrentItemExtension> _logger = logger;

        // Memoization fields
        private GlobalRoot? _root;
        private bool _rootResolved;

        private GlobalFolderSites? _folderSite;
        private bool _folderSiteResolved;

        private GlobalFolderSettings? _folderSettings;
        private bool _folderSettingsResolved;

        private IPublishedContent? _current;
        private bool _currentResolved;

        private IPublishedContent? _pageHome;
        private bool _pageHomeResolved;

        private IPublishedContent? _pageErrors;
        private bool _pageErrorsResolved;

        public GlobalRoot Root
        {
            get
            {
                if (!_rootResolved)
                {
                    _root = (GlobalRoot)GetItemByAlias(Current, GlobalRoot.ModelTypeAlias, true);
                    _rootResolved = true;
                }
                return _root!;
            }
        }

        public GlobalFolderSites FolderSite
        {
            get
            {
                if (!_folderSiteResolved)
                {
                    var folderSite = PageHome?.Parent<GlobalFolderSites>() ?? null;
                    folderSite ??= (GlobalFolderSites)GetItemByAlias(PageHome, GlobalFolderSites.ModelTypeAlias, true);
                    _folderSite = folderSite ?? throw new Exception(nameof(GlobalFolderSites) + " is null");
                    _folderSiteResolved = true;
                }
                return _folderSite!;
            }
        }

        public GlobalFolderSettings FolderSettings
        {
            get
            {
                if (!_folderSettingsResolved)
                {
                    if (FolderSite.GlobalSettings != null)
                    {
                        _folderSettings = (GlobalFolderSettings)FolderSite.GlobalSettings;
                    }
                    else
                    {
                        _folderSettings = Root.FirstChild<GlobalFolderSettings>()
                            ?? throw new Exception(nameof(GlobalFolderSettings) + " is null");
                    }
                    _folderSettingsResolved = true;
                }
                return _folderSettings!;
            }
        }

        public IPublishedContent? Current
        {
            get
            {
                if (!_currentResolved)
                {
                    var published = _currentSite.UContext.PublishedRequest?.PublishedContent;
                    _current = published ?? PageHome;
                    _currentResolved = true;
                }
                return _current;
            }
        }

        public IPublishedContent? PageHome
        {
            get
            {
                if (!_pageHomeResolved)
                {
                    try
                    {
                        _pageHome = GetItemWithDomain();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Get PageHome error");
                        _pageHome = null;
                    }
                    _pageHomeResolved = true;
                }
                return _pageHome;
            }
        }

        public IPublishedContent? PageErrors
        {
            get
            {
                if (!_pageErrorsResolved)
                {
                    try
                    {
                        var pageHome = PageHome;
                        if (pageHome != null)
                        {
                            var pageNotFound = pageHome.Value<IPublishedContent>(
                                nameof(GlobalPageNavigationConfigSettingForHomePage.PageNotFound));
                            _pageErrors = pageNotFound;
                        }
                        _pageErrors ??= FolderSite.FirstChild<GlobalPageError>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Get PageErrors error");
                        _pageErrors = null;
                    }
                    _pageErrorsResolved = true;
                }
                return _pageErrors;
            }
        }

        private IPublishedContent? GetItemWithDomain()
        {
            int contentid = _currentSite.UContext.PublishedRequest?.Domain?.ContentId ?? 0;
            if (contentid != 0)
            {
                return _currentSite.UContext.Content?.GetById(contentid);
            }

            // Fallback: find domain
            var domains = _currentSite.GetDomains(true);
            if (domains == null) return null;
            foreach (var domain in domains)
            {
                if (domain == null) continue;
                var currentItem = _currentSite.UContext.Content?.GetById(domain.ContentId);
                if (currentItem != null)
                    return currentItem;
            }
            return null;
        }

        private IPublishedContent GetItemByAlias(IPublishedContent? item, string alias, bool isFirst)
        {
            var current = item ?? throw new Exception("Not found item: " + alias);

            if (string.Equals(current.ContentType?.Alias, GlobalPageError.ModelTypeAlias, StringComparison.OrdinalIgnoreCase))
            {
                return GetItemByAlias(PageErrors?.Parent(), alias, isFirst);
            }

            // Quick check
            if (string.Equals(current.ContentType?.Alias, alias, StringComparison.OrdinalIgnoreCase))
                return current;

            // Traverse parents (cheapest path)
            var parent = current.Parent();
            while (parent != null)
            {
                if (string.Equals(parent.ContentType?.Alias, alias, StringComparison.OrdinalIgnoreCase))
                    return parent;
                parent = parent.Parent();
            }

            // Fallback to path-based traversal
            var (idToken, pathIds) = GetIdParent(current.Path ?? string.Empty, isFirst);
            while (!string.IsNullOrEmpty(idToken))
            {
                if (int.TryParse(idToken, out var idRoot))
                {
                    var p = _currentSite.UContext.Content?.GetById(idRoot);
                    if (p != null && string.Equals(p.ContentType?.Alias, alias, StringComparison.OrdinalIgnoreCase))
                        return p;
                }

                if (string.IsNullOrEmpty(pathIds))
                    break;

                (idToken, pathIds) = GetIdParent(pathIds, false);
            }

            throw new Exception("Not found item: " + alias);
        }

        private static (string, string?) GetIdParent(string pathId, bool isRoot = false)
        {
            if (string.IsNullOrEmpty(pathId))
                return (string.Empty, null);

            string pathIds = pathId;

            if (pathId.StartsWith("-1", StringComparison.Ordinal))
            {
                int firstComma = pathId.IndexOf(',');
                pathIds = (firstComma >= 0 && firstComma + 1 < pathId.Length) ? pathId[(firstComma + 1)..] : string.Empty;
            }

            if (string.IsNullOrEmpty(pathIds))
                return (string.Empty, pathIds);

            if (isRoot)
            {
                int firstComma = pathIds.IndexOf(',');
                var root = firstComma >= 0 ? pathIds[..firstComma] : pathIds;
                return (root ?? string.Empty, pathIds);
            }

            int lastComma = pathIds.LastIndexOf(',');
            if (lastComma < 0)
            {
                return (pathIds, null);
            }
            int prevComma = pathIds.LastIndexOf(',', lastComma - 1);
            string parentStr;
            if (prevComma >= 0)
            {
                parentStr = pathIds.Substring(prevComma + 1, lastComma - prevComma - 1);
            }
            else
            {
                parentStr = pathIds[..lastComma];
            }
            return (parentStr, pathIds);
        }
    }
}
