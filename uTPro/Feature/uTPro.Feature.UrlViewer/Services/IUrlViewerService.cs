using uTPro.Feature.UrlViewer.Models;

namespace uTPro.Feature.UrlViewer.Services;

public interface IUrlViewerService
{
    Task<UrlViewerResponse> FetchUrlAsync(UrlViewerRequest request, CancellationToken cancellationToken = default);
}
