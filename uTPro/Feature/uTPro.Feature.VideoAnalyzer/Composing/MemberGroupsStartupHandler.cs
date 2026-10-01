using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using uTPro.Feature.VideoAnalyzer.Configuration;

namespace uTPro.Feature.VideoAnalyzer.Composing;

/// <summary>
/// Creates the two member groups the analyzer is gated by (when missing) at startup.
/// Members still need to be assigned to them manually in the backoffice.
/// </summary>
internal sealed class MemberGroupsStartupHandler(
    IMemberGroupService memberGroupService,
    IOptions<VideoAnalyzerOptions> options,
    ILogger<MemberGroupsStartupHandler> logger)
    : INotificationHandler<UmbracoApplicationStartingNotification>
{
    public void Handle(UmbracoApplicationStartingNotification notification)
    {
        if (notification.RuntimeLevel < RuntimeLevel.Run)
        {
            return;
        }

        foreach (var groupName in new[] { options.Value.MemberUserGroup, options.Value.MemberAdminGroup })
        {
            try
            {
                var existing = memberGroupService.GetByNameAsync(groupName).GetAwaiter().GetResult();
                if (existing is null)
                {
                    memberGroupService.CreateAsync(new MemberGroup { Name = groupName })
                        .GetAwaiter().GetResult();
                    logger.LogInformation("VideoAnalyzer: created member group '{Group}'.", groupName);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "VideoAnalyzer: could not ensure member group '{Group}'.", groupName);
            }
        }
    }
}
