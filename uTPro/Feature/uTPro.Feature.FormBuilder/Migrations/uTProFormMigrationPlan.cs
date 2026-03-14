using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Migrations;

namespace uTPro.Feature.FormBuilder.Migrations
{
    public class uTProFormMigrationPlan : MigrationPlan
    {
        public uTProFormMigrationPlan()
      : base("uTProFormMigration")
        {
            this.From(string.Empty)
                .To<AddFormsTables>("arro-webforms-tables");
        }
    }
}
