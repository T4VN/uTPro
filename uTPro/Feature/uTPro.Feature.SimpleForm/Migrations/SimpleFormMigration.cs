using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Scoping;

namespace uTPro.Feature.SimpleForm.Migrations;

public class CreateSimpleFormTables : MigrationBase
{
    public CreateSimpleFormTables(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        // Drop incomplete tables from failed previous migration
        if (TableExists("utpro_SimpleFormSubmission"))
            Delete.Table("utpro_SimpleFormSubmission").Do();
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
                .WithColumn("StoreSubmissions").AsBoolean().WithDefaultValue(true)
                .WithColumn("IsEnabled").AsBoolean().WithDefaultValue(true)
                .WithColumn("CreatedUtc").AsDateTime().NotNullable()
                .WithColumn("UpdatedUtc").AsDateTime().NotNullable()
                .Do();

        Create.Table("utpro_SimpleFormSubmission")
                .WithColumn("Id").AsInt32().NotNullable().Identity().PrimaryKey("PK_utpro_SimpleFormSubmission")
                .WithColumn("FormId").AsInt32().NotNullable()
                .WithColumn("DataJson").AsCustom("NTEXT").Nullable()
                .WithColumn("IpAddress").AsString(100).Nullable()
                .WithColumn("UserAgent").AsString(500).Nullable()
                .WithColumn("CreatedUtc").AsDateTime().NotNullable()
                .Do();
    }
}

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
            .To<CreateSimpleFormTables>("simpleform-002")
            .To<SeedContactForm>("simpleform-003-seed");

        var upgrader = new Upgrader(plan);
        upgrader.Execute(_migrationPlanExecutor, _coreScopeProvider, _keyValueService);

        return Task.CompletedTask;
    }
}

public class SeedContactForm : MigrationBase
{
    public SeedContactForm(IMigrationContext context) : base(context) { }

    protected override void Migrate()
    {
        var now = DateTime.UtcNow;
        var fieldsJson = @"[
  {""id"":""f1"",""type"":""text"",""label"":""Name"",""name"":""name"",""placeholder"":""Name"",""required"":true,""sortOrder"":0,""colSpan"":1,""validationMessage"":""Please enter your name""},
  {""id"":""f2"",""type"":""email"",""label"":""Email"",""name"":""email"",""placeholder"":""Email"",""required"":true,""sortOrder"":1,""colSpan"":1,""validationMessage"":""Please enter a valid email""},
  {""id"":""f3"",""type"":""textarea"",""label"":""Message"",""name"":""message"",""placeholder"":""Message"",""required"":true,""sortOrder"":2,""colSpan"":2,""validationMessage"":""Please enter your message""}
]";

        // Only seed if no form with this alias exists
        var existing = Context.Database.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM utpro_SimpleForm WHERE Alias = @0", "contact-us");

        if (existing == 0)
        {
            Context.Database.Execute(@"
                INSERT INTO utpro_SimpleForm (Name, Alias, FieldsJson, SuccessMessage, RedirectUrl, EmailTo, EmailSubject, StoreSubmissions, IsEnabled, CreatedUtc, UpdatedUtc)
                VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)",
                "Contact Us",
                "contact-us",
                fieldsJson,
                "Thank you for contacting us! We will get back to you soon.",
                "",
                "",
                "New Contact Form Submission",
                true,
                true,
                now,
                now);
        }
    }
}
