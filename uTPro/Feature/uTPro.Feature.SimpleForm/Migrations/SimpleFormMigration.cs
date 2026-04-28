using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;

namespace uTPro.Feature.SimpleForm.Migrations;

// ── v1: Create all tables with final schema ──

public class CreateSimpleFormTablesV1 : MigrationBase
{
    public CreateSimpleFormTablesV1(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        if (TableExists("utpro_SimpleFormEntry"))
            Delete.Table("utpro_SimpleFormEntry").Do();
        if (TableExists("utpro_SimpleForm"))
            Delete.Table("utpro_SimpleForm").Do();

        Create.Table("utpro_SimpleForm")
            .WithColumn("Id").AsInt32().NotNullable().Identity().PrimaryKey("PK_utpro_SimpleForm")
            .WithColumn("Name").AsString(255).NotNullable()
            .WithColumn("Alias").AsString(255).NotNullable().Unique("IX_utpro_SimpleForm_Alias")
            .WithColumn("FieldsJson").AsCustom("NTEXT").Nullable()
            .WithColumn("SuccessMessage").AsString(1000).Nullable()
            .WithColumn("RedirectUrl").AsString(500).Nullable()
            .WithColumn("EmailTo").AsString(500).Nullable()
            .WithColumn("EmailSubject").AsString(500).Nullable()
            .WithColumn("StoreEntries").AsBoolean().WithDefaultValue(true)
            .WithColumn("IsEnabled").AsBoolean().WithDefaultValue(true)
            .WithColumn("VisibleColumnsJson").AsCustom("NTEXT").Nullable()
            .WithColumn("EnableRenderApi").AsBoolean().WithDefaultValue(false)
            .WithColumn("EnableEntriesApi").AsBoolean().WithDefaultValue(false)
            .WithColumn("CreatedUtc").AsDateTime().NotNullable()
            .WithColumn("UpdatedUtc").AsDateTime().NotNullable()
            .Do();

        Create.Table("utpro_SimpleFormEntry")
            .WithColumn("Id").AsInt32().NotNullable().Identity().PrimaryKey("PK_utpro_SimpleFormEntry")
            .WithColumn("FormId").AsInt32().NotNullable()
            .WithColumn("DataJson").AsCustom("NTEXT").Nullable()
            .WithColumn("IpAddress").AsString(100).Nullable()
            .WithColumn("UserAgent").AsString(500).Nullable()
            .WithColumn("CreatedUtc").AsDateTime().NotNullable()
            .Do();
    }
}

// ── v1: Seed default Contact Us form ──

public class SeedContactFormV1 : MigrationBase
{
    public SeedContactFormV1(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        var now = DateTime.UtcNow;
        var fieldsJson = @"[
  {""id"":""f1"",""type"":""text"",""label"":""Name"",""name"":""name"",""placeholder"":""Name"",""required"":true,""sortOrder"":0,""validationMessage"":""Please enter your name""},
  {""id"":""f2"",""type"":""email"",""label"":""Email"",""name"":""email"",""placeholder"":""Email"",""required"":true,""sortOrder"":1,""validationMessage"":""Please enter a valid email""},
  {""id"":""f3"",""type"":""textarea"",""label"":""Message"",""name"":""message"",""placeholder"":""Message"",""required"":true,""sortOrder"":2,""validationMessage"":""Please enter your message""}
]";

        var existing = Context.Database.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM utpro_SimpleForm WHERE Alias = @0", "contact-us");

        if (existing == 0)
        {
            Context.Database.Execute(@"
                INSERT INTO utpro_SimpleForm
                    (Name, Alias, FieldsJson, SuccessMessage, RedirectUrl, EmailTo, EmailSubject,
                     StoreEntries, IsEnabled, VisibleColumnsJson, EnableRenderApi, EnableEntriesApi, CreatedUtc, UpdatedUtc)
                VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)",
                "Contact Us",
                "contact-us",
                fieldsJson,
                "Thank you for contacting us! We will get back to you soon.",
                "",
                "",
                "New Contact Form Entry",
                true,   // StoreEntries
                true,   // IsEnabled
                null,   // VisibleColumnsJson
                false,  // EnableRenderApi
                false,  // EnableEntriesApi
                now,
                now);
        }
    }
}

// ── v2: Add GroupsJson column ──

public class AddGroupsJsonColumnV2 : MigrationBase
{
    public AddGroupsJsonColumnV2(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        if (!ColumnExists("utpro_SimpleForm", "GroupsJson"))
        {
            Alter.Table("utpro_SimpleForm")
                .AddColumn("GroupsJson").AsCustom("NTEXT").Nullable()
                .Do();
        }
    }
}

// ── v2: Migrate existing FieldsJson into GroupsJson ──

public class MigrateFieldsToGroupsV2 : MigrationBase
{
    public MigrateFieldsToGroupsV2(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        // Find all forms that have fields but no groups yet
        // NTEXT columns cannot use = or <> operators directly, so use DATALENGTH / IS NULL checks
        var rows = Context.Database.Fetch<dynamic>(
            "SELECT Id, CAST(FieldsJson AS NVARCHAR(MAX)) AS FieldsJson FROM utpro_SimpleForm WHERE FieldsJson IS NOT NULL AND DATALENGTH(FieldsJson) > 4 AND (GroupsJson IS NULL OR DATALENGTH(GroupsJson) <= 4)");

        foreach (var row in rows)
        {
            int id = (int)row.Id;
            string fieldsJson = (string)row.FieldsJson;

            // Skip if fieldsJson is empty array
            if (string.IsNullOrWhiteSpace(fieldsJson) || fieldsJson.Trim() == "[]")
                continue;

            // Wrap existing fields into a single default group with 1 column (width=12)
            var groupsJson = @"[{""id"":""g_default"",""name"":"""",""cssClass"":"""",""columns"":[{""id"":""c_default"",""width"":12,""fields"":" + fieldsJson + @"}],""sortOrder"":0}]";

            Context.Database.Execute(
                "UPDATE utpro_SimpleForm SET GroupsJson = CAST(@0 AS NTEXT), FieldsJson = CAST('[]' AS NTEXT) WHERE Id = @1",
                groupsJson, id);
        }
    }
}

// ── v3: Convert old flat group.fields format to new group.columns[].fields[] format ──

public class ConvertGroupsToColumnFormatV3 : MigrationBase
{
    public ConvertGroupsToColumnFormatV3(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        // Find forms that have GroupsJson with old format (has "fields" at group level instead of "columns")
        var rows = Context.Database.Fetch<dynamic>(
            "SELECT Id, CAST(GroupsJson AS NVARCHAR(MAX)) AS GroupsJson FROM utpro_SimpleForm WHERE GroupsJson IS NOT NULL AND DATALENGTH(GroupsJson) > 4");

        foreach (var row in rows)
        {
            int id = (int)row.Id;
            string json = (string)row.GroupsJson;
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]") continue;

            // Check if it's old format: contains "fields" at group level but no "columns" array with objects
            // Old format: [{"fields":[...],"columns":1,...}]
            // New format: [{"columns":[{"width":12,"fields":[...]}],...}]
            // Simple heuristic: if JSON contains "\"columns\":" followed by a number, it's old format
            if (!json.Contains("\"columns\":[{")) // new format has "columns":[{...}]
            {
                try
                {
                    var opts = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
                    var oldGroups = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    var newGroups = new System.Collections.Generic.List<object>();

                    foreach (var g in oldGroups.EnumerateArray())
                    {
                        var gId = g.TryGetProperty("id", out var idProp) ? idProp.GetString() : System.Guid.NewGuid().ToString("N");
                        var gName = g.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "";
                        var gCss = g.TryGetProperty("cssClass", out var cssProp) ? cssProp.GetString() : "";
                        var gSort = g.TryGetProperty("sortOrder", out var sortProp) ? sortProp.GetInt32() : 0;

                        // Get old fields array
                        var fieldsJson = "[]";
                        if (g.TryGetProperty("fields", out var fieldsProp) && fieldsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            fieldsJson = fieldsProp.GetRawText();
                        }

                        newGroups.Add(new
                        {
                            id = gId,
                            name = gName,
                            cssClass = gCss,
                            columns = new[] { new { id = "c_" + gId, width = 12, fields = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(fieldsJson) } },
                            sortOrder = gSort
                        });
                    }

                    var newJson = System.Text.Json.JsonSerializer.Serialize(newGroups, opts);
                    Context.Database.Execute(
                        "UPDATE utpro_SimpleForm SET GroupsJson = CAST(@0 AS NTEXT) WHERE Id = @1",
                        newJson, id);
                }
                catch
                {
                    // Skip if JSON parsing fails — don't break migration
                }
            }
        }
    }
}

// ── v4: Update default Contact Us form to use proper 2-group layout ──

public class UpdateContactFormLayoutV4 : MigrationBase
{
    public UpdateContactFormLayoutV4(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        var groupsJson = @"[
  {
    ""id"":""g1"",""name"":"""",""cssClass"":"""",""sortOrder"":0,
    ""columns"":[
      {""id"":""g1c1"",""width"":6,""fields"":[
        {""id"":""f1"",""type"":""text"",""label"":""Name"",""name"":""name"",""placeholder"":""Name"",""required"":true,""sortOrder"":0,""validationMessage"":""Please enter your name""}
      ]},
      {""id"":""g1c2"",""width"":6,""fields"":[
        {""id"":""f2"",""type"":""email"",""label"":""Email"",""name"":""email"",""placeholder"":""Email"",""required"":true,""sortOrder"":0,""validationMessage"":""Please enter a valid email""}
      ]}
    ]
  },
  {
    ""id"":""g2"",""name"":"""",""cssClass"":"""",""sortOrder"":1,
    ""columns"":[
      {""id"":""g2c1"",""width"":12,""fields"":[
        {""id"":""f3"",""type"":""textarea"",""label"":""Message"",""name"":""message"",""placeholder"":""Message"",""required"":true,""sortOrder"":0,""validationMessage"":""Please enter your message""}
      ]}
    ]
  }
]";

        Context.Database.Execute(
            "UPDATE utpro_SimpleForm SET GroupsJson = CAST(@0 AS NTEXT), FieldsJson = CAST('[]' AS NTEXT) WHERE Alias = @1",
            groupsJson, "contact-us");
    }
}

// ── Migration runner ──

public class RunSimpleFormMigration : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<Umbraco.Cms.Core.Notifications.UmbracoApplicationStartedNotification,
            SimpleFormMigrationHandler>();
    }
}

public class SimpleFormMigrationHandler
    : Umbraco.Cms.Core.Events.INotificationAsyncHandler<Umbraco.Cms.Core.Notifications.UmbracoApplicationStartedNotification>
{
    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly ICoreScopeProvider _coreScopeProvider;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;

    public SimpleFormMigrationHandler(
        ICoreScopeProvider coreScopeProvider,
        IMigrationPlanExecutor migrationPlanExecutor,
        IKeyValueService keyValueService,
        IRuntimeState runtimeState)
    {
        _coreScopeProvider = coreScopeProvider;
        _migrationPlanExecutor = migrationPlanExecutor;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
    }

    public Task HandleAsync(Umbraco.Cms.Core.Notifications.UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (_runtimeState.Level < RuntimeLevel.Run) return Task.CompletedTask;

        var plan = new MigrationPlan("uTPro.SimpleForm");
        plan.From(string.Empty)
            .To<CreateSimpleFormTablesV1>("simpleform-v1-001-tables")
            .To<SeedContactFormV1>("simpleform-v1-002-seed")
            .To<AddGroupsJsonColumnV2>("simpleform-v2-001-groups")
            .To<MigrateFieldsToGroupsV2>("simpleform-v2-003-migrate-fields-fix")
            .To<ConvertGroupsToColumnFormatV3>("simpleform-v3-001-column-format")
            .To<UpdateContactFormLayoutV4>("simpleform-v4-001-contact-layout");

        // Handle failed v2-002 state
        plan.From("simpleform-v2-002-migrate-fields")
            .To<MigrateFieldsToGroupsV2>("simpleform-v2-003-migrate-fields-fix");

        var upgrader = new Upgrader(plan);
        upgrader.Execute(_migrationPlanExecutor, _coreScopeProvider, _keyValueService);

        return Task.CompletedTask;
    }
}
