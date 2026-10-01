using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Scoping;
using uTPro.Feature.VideoAnalyzer.Models;
using uTPro.Feature.VideoAnalyzer.Persistence;

namespace uTPro.Feature.VideoAnalyzer.Services;

/// <summary>NPoco-backed repository over the analyzer's own tables. All datetimes are UTC.</summary>
internal sealed class VideoAnalyzerRepository(IScopeProvider scopeProvider)
    : IVideoAnalyzerRepository
{
    public async Task<VaVideoAnalysis?> GetLatestSuccessAsync(string platform, string videoId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("*").From("vaVideoAnalysis")
            .Where("Platform = @platform AND VideoId = @videoId AND Status = @status", new
            {
                platform,
                videoId,
                status = (int)AnalysisStatus.Success,
            })
            .OrderByDescending("CreatedAt");
        var rows = await scope.Database.FetchAsync<VaVideoAnalysis>(sql);
        return rows.FirstOrDefault();
    }

    public async Task<VaVideoAnalysis?> GetActiveAsync(string platform, string videoId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("*").From("vaVideoAnalysis")
            .Where("Platform = @platform AND VideoId = @videoId AND Status IN (@s0, @s1)", new
            {
                platform,
                videoId,
                s0 = (int)AnalysisStatus.Pending,
                s1 = (int)AnalysisStatus.Processing,
            })
            .OrderByDescending("CreatedAt");
        var rows = await scope.Database.FetchAsync<VaVideoAnalysis>(sql);
        return rows.FirstOrDefault();
    }

    public async Task<VaVideoAnalysis?> GetByKeyAsync(Guid key)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        return await scope.Database.FirstOrDefaultAsync<VaVideoAnalysis>("WHERE [Key] = @key", new { key });
    }

    public async Task<int> InsertAsync(VaVideoAnalysis entity)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var id = Convert.ToInt32(await scope.Database.InsertAsync(entity));
        entity.Id = id;
        return id;
    }

    public async Task UpdateAsync(VaVideoAnalysis entity)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.UpdateAsync(entity);
    }

    public async Task TouchHeartbeatAsync(int id, DateTime utcNow)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE vaVideoAnalysis SET HeartbeatAt = @utcNow WHERE Id = @id",
            new { id, utcNow });
    }

    public async Task IncrementCacheHitsAsync(int id)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.ExecuteAsync(
            "UPDATE vaVideoAnalysis SET CacheHits = CacheHits + 1 WHERE Id = @id",
            new { id });
    }

    public async Task<int> GetMaxVersionAsync(string platform, string videoId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("COALESCE(MAX(AnalysisVersion), 0) AS MaxVersion").From("vaVideoAnalysis")
            .Where("Platform = @platform AND VideoId = @videoId", new { platform, videoId });
        var max = await scope.Database.ExecuteScalarAsync<int?>(sql);
        return max ?? 0;
    }

    public async Task<int> CountInitiatedByAsync(int memberId, DateTime utcDayStart)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("COUNT(*) AS Cnt").From("vaVideoAnalysis")
            .Where("InitiatedByMemberId = @memberId AND CreatedAt >= @utcDayStart", new { memberId, utcDayStart });
        var count = await scope.Database.ExecuteScalarAsync<int>(sql);
        return count;
    }

    public async Task<List<VaVideoAnalysis>> GetStaleProcessingAsync(DateTime utcCutoff)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("*").From("vaVideoAnalysis")
            .Where("Status = @status AND HeartbeatAt IS NOT NULL AND HeartbeatAt < @utcCutoff", new
            {
                status = (int)AnalysisStatus.Processing,
                utcCutoff,
            });
        return await scope.Database.FetchAsync<VaVideoAnalysis>(sql);
    }

    public async Task AddChatMessageAsync(VaChatMessage message)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        await scope.Database.InsertAsync(message);
    }

    public async Task<List<VaChatMessage>> GetChatAsync(int analysisId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("*").From("vaChatMessage")
            .Where("AnalysisId = @analysisId", new { analysisId })
            .OrderBy("Id");
        return await scope.Database.FetchAsync<VaChatMessage>(sql);
    }

    public async Task<int> CountChatAsync(int analysisId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var sql = scope.SqlContext.Sql()
            .Select("COUNT(*) AS Cnt").From("vaChatMessage")
            .Where("AnalysisId = @analysisId", new { analysisId });
        return await scope.Database.ExecuteScalarAsync<int>(sql);
    }
}
