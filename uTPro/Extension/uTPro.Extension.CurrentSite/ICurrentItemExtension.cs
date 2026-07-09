using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace uTPro.Extension.CurrentSite
{
    public interface ICurrentItemExtension
    {
        GlobalRoot Root { get; }
        GlobalFolderSites FolderSite { get; }
        GlobalFolderSettings FolderSettings { get; }
        IPublishedContent? Current { get; }
        IPublishedContent? PageHome { get; }
        IPublishedContent? PageErrors { get; }
    }
}
