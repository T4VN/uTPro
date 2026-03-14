using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;
using uTPro.Feature.FormBuilder.Migrations;

namespace uTPro.Feature.FormBuilder.Components
{
    public class DatabaseComponent : IAsyncComponent
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMigrationPlanExecutor _migrationPlanExecutor;
        private readonly IKeyValueService _keyValueService;
        private readonly IRuntimeState _runtimeState;

        public DatabaseComponent(
          IScopeProvider scopeProvider,
          IMigrationPlanExecutor migrationPlanExecutor,
          IKeyValueService keyValueService,
          IRuntimeState runtimeState)
        {
            this._scopeProvider = scopeProvider;
            this._migrationPlanExecutor = migrationPlanExecutor;
            this._keyValueService = keyValueService;
            this._runtimeState = runtimeState;
        }

        //public void Initialize()
        //{
        //}

        public Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
        {

            if (this._runtimeState.Level == RuntimeLevel.Run)
                new Upgrader((MigrationPlan)new uTProFormMigrationPlan()).ExecuteAsync(this._migrationPlanExecutor, (Umbraco.Cms.Core.Scoping.ICoreScopeProvider)this._scopeProvider, this._keyValueService);
            return Task.CompletedTask;
        }

        public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
