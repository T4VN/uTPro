using Umbraco.Cms.Core.Packaging;
using Umbraco.Cms.Infrastructure.Migrations;
using uTPro.Feature.VideoAnalyzer.Persistence;

namespace uTPro.Feature.VideoAnalyzer.Migrations;

/// <summary>
/// Package migration plan for the Video Analyzer feature. Tables are Umbraco-migration-managed
/// (NPoco, cross-DB) so they work with whatever provider umbracoDbDSN points at —
/// including the PostgreSQL provider this solution ships with. The plan runs automatically
/// at startup via builder.PackageMigrationPlans().
/// </summary>
public sealed class VideoAnalyzerMigrationPlan : PackageMigrationPlan
{
    public VideoAnalyzerMigrationPlan() : base("uTPro.VideoAnalyzer")
    {
    }

    protected override void DefinePlan()
    {
        From(string.Empty)
            .To<CreateVideoAnalyzerTables>("va-1-create-tables");
    }
}

/// <summary>Creates the vaVideoAnalysis and vaChatMessage tables plus lookup indexes.</summary>
public sealed class CreateVideoAnalyzerTables : AsyncMigrationBase
{
    public CreateVideoAnalyzerTables(IMigrationContext context) : base(context)
    {
    }

    protected override Task MigrateAsync()
    {
        Create.Table<VaVideoAnalysis>().Do();
        Create.Table<VaChatMessage>().Do();

        // Single-column indexes (the migration index builder has no composite helper):
        // Platform + Status + MemberId are the three lookup paths the API uses.
        Create.Index("IX_vaVideoAnalysis_Platform")
            .OnTable("vaVideoAnalysis")
            .OnColumn("Platform").Ascending()
            .Do();
        Create.Index("IX_vaVideoAnalysis_VideoId")
            .OnTable("vaVideoAnalysis")
            .OnColumn("VideoId").Ascending()
            .Do();
        Create.Index("IX_vaVideoAnalysis_Status")
            .OnTable("vaVideoAnalysis")
            .OnColumn("Status").Ascending()
            .Do();
        Create.Index("IX_vaVideoAnalysis_InitiatedByMemberId")
            .OnTable("vaVideoAnalysis")
            .OnColumn("InitiatedByMemberId").Ascending()
            .Do();
        Create.Index("IX_vaChatMessage_AnalysisId")
            .OnTable("vaChatMessage")
            .OnColumn("AnalysisId").Ascending()
            .Do();

        return Task.CompletedTask;
    }
}
