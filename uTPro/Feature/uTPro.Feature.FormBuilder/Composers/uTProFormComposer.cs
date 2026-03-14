using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;
using uTPro.Feature.FormBuilder.Migrations;

namespace uTPro.Feature.FormBuilder.Composers
{
    internal class uTProFormComposer : IComposer, IDiscoverable
    {
        public void Compose(IUmbracoBuilder builder)
        {
            Umbraco.Cms.Core.DependencyInjection.UmbracoBuilderExtensions.AddNotificationHandler<UmbracoApplicationStartingNotification, RunMigration>(builder);
        }
    }
}
