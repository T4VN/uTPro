using Umbraco.Cms.Core.Sync;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Returns a fixed <see cref="ServerRole"/> configured via appsettings
/// (uTPro:Hosting:ServerRole). Used for explicit load-balancing setups where each
/// app has a known role (e.g. backoffice = SchedulingPublisher, website = Subscriber),
/// instead of Umbraco's automatic database election.
/// </summary>
public sealed class ConfigurableServerRoleAccessor(ServerRole role) : IServerRoleAccessor
{
    public ServerRole CurrentServerRole { get; } = role;
}
