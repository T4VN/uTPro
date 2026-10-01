using NPoco;

namespace uTPro.Feature.VideoAnalyzer.Persistence;

/// <summary>One chat turn (question or answer) attached to an analysis.</summary>
[TableName("vaChatMessage")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class VaChatMessage
{
    [Column("Id")]
    public int Id { get; set; }

    /// <summary>FK to <see cref="VaVideoAnalysis.Id"/> (no enforced constraint — the analyzer owns cleanup).</summary>
    [Column("AnalysisId")]
    public int AnalysisId { get; set; }

    [Column("MemberId")]
    public int MemberId { get; set; }

    [Column("MemberName")]
    public string? MemberName { get; set; }

    /// <summary>"user" or "assistant".</summary>
    [Column("Role")]
    public string Role { get; set; } = "user";

    [Column("Content")]
    public string Content { get; set; } = string.Empty;

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}
