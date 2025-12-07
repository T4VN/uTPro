using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Cms.Web.Common.UmbracoContext;
using static Umbraco.Cms.Core.Constants.HttpContext;

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
                return (GlobalRoot)this.GetItemByAlias(Current, GlobalRoot.ModelTypeAlias, true);
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
                catch (Exception)
                {
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
                    if (this.PageHome != null)
                    {
                        var pageNotFound = this.PageHome.Value<IPublishedContent>(nameof(GlobalPageNavigationConfigSettingForHomePage.PageNotFound));
                        return pageNotFound;
                    }
                    return this.FolderSite.FirstChild<GlobalPageError>() ?? null;
                }
                catch (Exception)
                {
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
                var domains = _currentSite.GetDomains(true).GetAwaiter().GetResult().ToList();
                foreach (var domain in domains)
                {
                    if (domain != null)
                    {
                        var currentItem = _currentSite.UContext.Content?.GetById(domain.ContentId);
                        if (currentItem is null)
                            continue;
                        else
                        {
                            return currentItem;
                        }
                    }
                }
            }
            return null;
        }

        private IPublishedContent GetItemByAlias(IPublishedContent? item, string alias, bool isFisrt)
        {
            if (item == null)
            {
                item = this.PageHome;
            }
            else
            {
                if (item.ContentType.Alias == alias)
                {
                    return item;
                }
            }

            var (strIdRoot, pathIds) = GetIdParent(item?.Path ?? string.Empty, isFisrt);
            if (!string.IsNullOrEmpty(strIdRoot))
            {
                int idRoot = int.Parse(strIdRoot);
                item = _currentSite.UContext.Content?.GetById(idRoot);
                if (item != null)
                {
                    if (item.ContentType.Alias == alias)
                    {
                        return item;
                    }
                    else
                    {
                        (strIdRoot, pathIds) = GetIdParent(pathIds ?? string.Empty, false);
                        idRoot = int.Parse(strIdRoot);
                        item = _currentSite.UContext.Content?.GetById(idRoot);
                        return GetItemByAlias(item, alias, false);
                    }
                }
            }
            throw new Exception("Not found item: " + alias);
        }

        private static (string, string?) GetIdParent(string pathId, bool isRoot = false)
        {
            string? pathIds = pathId;
            if (string.IsNullOrEmpty(pathId))
            {
                return (pathId, null);
            }
            else
            {
                if (pathId.StartsWith("-1"))
                {
                    int first = pathId.IndexOf(',') + 1;
                    pathIds = pathId.Substring(first);//Remove id CURRENT
                }

                if (pathIds != null && pathIds.IndexOf(',') > 0)
                {
                    var arrayPathId = pathIds.Split(',');
                    if (isRoot)
                    {
                        pathId = arrayPathId.FirstOrDefault() ?? string.Empty;//id Root
                    }
                    else
                    {
                        if (arrayPathId.Length >= 2)
                        {
                            pathId = arrayPathId[arrayPathId.Length - 2];////Get id Parent
                        }
                    }
                }
            }

            return (pathId, pathIds);
        }

    }
}
