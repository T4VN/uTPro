using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Migrations;
using uTPro.Feature.FormBuilder.Models;

namespace uTPro.Feature.FormBuilder.Migrations
{
    internal class AddFormsTables(IMigrationContext context) : AsyncMigrationBase(context)
    {
        protected override Task MigrateAsync()
        {
            if (!this.TableExists(nameof(uTProForms)))
            {
                this.Create.Table<uTProForms>(false).Do();
                this.Logger.LogInformation("Database table {DbTable} was created", nameof(uTProForms));
            }
            if (!this.TableExists(nameof(uTProFormFields)))
            {
                this.Create.Table<uTProFormFields>(false).Do();
                this.Logger.LogInformation("Database table {DbTable} was created", nameof(uTProFormFields));
            }
            if (!this.TableExists(nameof(uTProFormCollection)))
            {
                this.Create.Table<uTProFormCollection>(false).Do();
                this.Logger.LogInformation("Database table {DbTable} was created", nameof(uTProFormCollection));
            }
            if (!this.TableExists(nameof(uTProFormCollectionData)))
            {
                this.Create.Table<uTProFormCollectionData>(false).Do();
                this.Logger.LogInformation("Database table {DbTable} was created", nameof(uTProFormCollectionData));
            }
            if (!this.TableExists(nameof(uTProFormFieldValues)))
            {
                this.Create.Table<uTProFormFieldValues>(false).Do();
                this.Logger.LogInformation("Database table {DbTable} was created", nameof(uTProFormFieldValues));
            }
            return Task.CompletedTask;
        }

    }
}
