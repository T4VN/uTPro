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
        //GlobalFolderArchives? FolderArchives { get; }
        IPublishedContent? Current { get; }
        IPublishedContent? PageHome { get; }
        IPublishedContent? PageErrors { get; }
    }
    internal class CurrentItemExtension : ICurrentItemExtension, IDisposable
    {
        ~CurrentItemExtension()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
            }
            // free native resources if there are any.
        }

        readonly ICurrentSiteExtension _currentSite;
        readonly ILogger<CurrentItemExtension> _logger;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public CurrentItemExtension(
            ILogger<CurrentItemExtension> logger,
            ICurrentSiteExtension currentSite)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _logger = logger;
            _currentSite = currentSite;

        }

        public GlobalRoot Root
        {
            get
            {
                return (GlobalRoot)this.GetItemByAlias(this.Current, GlobalRoot.ModelTypeAlias, true);
            }
        }

        public GlobalFolderSites FolderSite
        {
            get
            {
                var folderSite = this.PageHome?.Parent<GlobalFolderSites>() ?? null;
                if (folderSite == null)
                {
                    folderSite = (GlobalFolderSites)GetItemByAlias(this.PageHome, GlobalFolderSites.ModelTypeAlias, true);
                }
                return folderSite ?? throw new Exception(nameof(GlobalFolderSites) + " is null");
            }
        }

        public GlobalFolderSettings FolderSettings
        {
            get
            {
                if (this.FolderSite.GlobalSettings != null)
                {
                    return (GlobalFolderSettings)this.FolderSite.GlobalSettings;
                }
                return this.Root.FirstChild<GlobalFolderSettings>() ?? throw new Exception(nameof(GlobalFolderSettings) + " is null");
            }
        }

        //public GlobalFolderArchives? FolderArchives
        //{
        //    get
        //    {
        //        return this.Root.FirstChild<GlobalFolderArchives>();
        //    }
        //}

        public IPublishedContent? Current
        {
            get
            {
                IPublishedContent? currentItem = null;
                if (_currentSite.UContext.PublishedRequest?.PublishedContent != null)
                {
                    currentItem = _currentSite.UContext.PublishedRequest?.PublishedContent;
                }
                else
                {
                    currentItem = this.PageHome;
                }
                return currentItem;
            }
        }

        public IPublishedContent? PageHome
        {
            get
            {
                try
                {
                    return this.GetItemWithDomain() ?? null;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Get PageHome error");
                    return null;
                }
            }
        }
        public IPublishedContent? PageErrors
        {
            get
            {
                try
                {
                    var pageHome = this.PageHome;
                    if (pageHome != null)
                    {
                        var pageNotFound = pageHome.Value<IPublishedContent>(nameof(GlobalPageNavigationConfigSettingForHomePage.PageNotFound));
                        return pageNotFound;
                    }
                    return this.FolderSite.FirstChild<GlobalPageError>() ?? null;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Get PageErrors error");
                    return null;
                }
            }
        }

        private IPublishedContent? GetItemWithDomain()
        {
            int contentid = _currentSite.UContext.PublishedRequest?.Domain?.ContentId ?? 0;
            if (contentid != 0)
            {
                var item = _currentSite.UContext.Content?.GetById(contentid);
                return item;
            }
            else//find domain
            {
                var domains = _currentSite.GetDomains(true).GetAwaiter().GetResult();
                if (domains == null) return null;
                foreach (var domain in domains)
                {
                    if (domain == null) continue;
                    var currentItem = _currentSite.UContext.Content?.GetById(domain.ContentId);
                    if (currentItem != null)
                        return currentItem;
                }
            }
            return null;
        }

        private IPublishedContent GetItemByAlias(IPublishedContent? item, string alias, bool isFirst)
        {
            // Start from provided item
            var current = item;
            if (current == null)
                throw new Exception("Not found item: " + alias);

            // quick check
            if (string.Equals(current.ContentType?.Alias, alias, StringComparison.OrdinalIgnoreCase))
                return current;

            // try traversing parents first (cheaper, no parsing)
            var parent = current.Parent();
            while (parent != null)
            {
                if (string.Equals(parent.ContentType?.Alias, alias, StringComparison.OrdinalIgnoreCase))
                    return parent;
                parent = parent.Parent();
            }

            // fallback to path-based traversal (optimized parsing)
            var (idToken, pathIds) = GetIdParent(current.Path ?? string.Empty, isFirst);
            while (!string.IsNullOrEmpty(idToken))
            {
                if (int.TryParse(idToken, out var idRoot))
                {
                    var p = _currentSite.UContext.Content?.GetById(idRoot);
                    if (p != null)
                    {
                        if (string.Equals(p.ContentType?.Alias, alias, StringComparison.OrdinalIgnoreCase))
                            return p;
                    }
                }

                if (string.IsNullOrEmpty(pathIds))
                    break;

                (idToken, pathIds) = GetIdParent(pathIds ?? string.Empty, false);
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
                pathIds = (firstComma >= 0 && firstComma + 1 < pathId.Length) ? pathId.Substring(firstComma + 1) : string.Empty;
            }

            if (string.IsNullOrEmpty(pathIds))
                return (string.Empty, pathIds);

            if (isRoot)
            {
                int firstComma = pathIds.IndexOf(',');
                var root = firstComma >= 0 ? pathIds.Substring(0, firstComma) : pathIds;
                return (root ?? string.Empty, pathIds);
            }

            int lastComma = pathIds.LastIndexOf(',');
            if (lastComma < 0)
            {
                return (pathIds, null);
            }
            int prevComma = pathIds.LastIndexOf(',', lastComma - 1);
            string parent;
            if (prevComma >= 0)
            {
                parent = pathIds.Substring(prevComma + 1, lastComma - prevComma - 1);
            }
            else
            {
                parent = pathIds.Substring(0, lastComma);
            }
            return (parent, pathIds);
        }

    }
}
