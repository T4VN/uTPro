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
                return (GlobalRoot)this.GetItemRoot(Current);
            }
        }

        public GlobalFolderSites FolderSite
        {
            get
            {
                return this.PageHome?.Parent<GlobalFolderSites>() ?? throw new Exception(nameof(GlobalFolderSites) + " is null");
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

        private IPublishedContent GetItemRoot(IPublishedContent? item)
        {
            if (item == null)
            {
                item = this.PageHome;
            }
            string strIdRoot = GetIdRoot(item);
            if (!string.IsNullOrEmpty(strIdRoot))
            {
                int idRoot = int.Parse(strIdRoot);
                item = _currentSite.UContext.Content?.GetById(idRoot);
                if (item != null)
                {
                    if (item.ContentType.Alias == GlobalRoot.ModelTypeAlias)
                    {
                        return item;
                    }
                    else
                    {
                        return GetItemRoot(null);
                    }
                }
            }
            throw new Exception("Not found item: Folder Site");
        }

        private string GetIdRoot(IPublishedContent? item)
        {
            if (item == null)
            {
                return string.Empty;
            }
            return GetId(item.Path);
        }

        private static string GetId(string pathId)
        {
            if (string.IsNullOrEmpty(pathId))
            {
                return pathId;
            }
            else
            {
                if (pathId.StartsWith("-1"))
                {
                    int first = pathId.IndexOf(',') + 1;
                    pathId = pathId.Substring(first);//Remove id CURRENT
                }
                if (pathId.IndexOf(',') > 0)
                {
                    pathId = pathId.Substring(0, pathId.IndexOf(','));//Get id Parrent
                }
            }

            return pathId;
        }

    }
}
