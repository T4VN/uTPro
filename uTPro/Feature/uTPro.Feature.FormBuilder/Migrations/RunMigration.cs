using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace uTPro.Feature.FormBuilder.Migrations
{
    internal class RunMigration : INotificationHandler<UmbracoApplicationStartingNotification>, INotificationHandler
    {
        private readonly IMigrationPlanExecutor _migrationPlanExecutor;
        private readonly ICoreScopeProvider _coreScopeProvider;
        private readonly IKeyValueService _keyValueService;
        private readonly IRuntimeState _runtimeState;

        public RunMigration(
          ICoreScopeProvider coreScopeProvider,
          IMigrationPlanExecutor migrationPlanExecutor,
          IKeyValueService keyValueService,
          IRuntimeState runtimeState)
        {
            this._migrationPlanExecutor = migrationPlanExecutor;
            this._coreScopeProvider = coreScopeProvider;
            this._keyValueService = keyValueService;
            this._runtimeState = runtimeState;
        }

        public void Handle(
          UmbracoApplicationStartingNotification notification)
        {
            if (this._runtimeState.Level == Umbraco.Cms.Core.RuntimeLevel.Run)
                new Upgrader((MigrationPlan)new uTProFormMigrationPlan()).ExecuteAsync(this._migrationPlanExecutor, this._coreScopeProvider, this._keyValueService);

        }
    }
}
