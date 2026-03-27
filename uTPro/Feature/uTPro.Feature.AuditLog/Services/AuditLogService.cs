using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;
using uTPro.Feature.AuditLog.Models;

namespace uTPro.Feature.AuditLog.Services;

class DIAuditLogService : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.Services.AddScoped<IAuditLogService, AuditLogService>();
}

public interface IAuditLogService
{
    AuditLogPagedResult<AuditEntryViewModel> GetAuditEntries(AuditLogFilterRequest filter);
    AuditLogPagedResult<LogEntryViewModel> GetLogEntries(AuditLogFilterRequest filter);
    IEnumerable<string> GetDistinctEventTypes();
    IEnumerable<string> GetDistinctLogHeaders();
}

internal class AuditLogService(
    IScopeProvider scopeProvider,
    ILogger<AuditLogService> logger
) : IAuditLogService
{
    public AuditLogPagedResult<AuditEntryViewModel> GetAuditEntries(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var sql = scope.SqlContext.Sql()
            .Select("*")
            .From("umbracoAudit");

        var whereClauses = new List<string>();
        var parameters = new List<object>();
        int paramIndex = 0;

        if (!string.IsNullOrWhiteSpace(filter.EventType))
        {
            whereClauses.Add($"eventType = @{paramIndex}");
            parameters.Add(filter.EventType);
            paramIndex++;
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            whereClauses.Add($"(eventDetails LIKE @{paramIndex} OR performingDetails LIKE @{paramIndex} OR affectedDetails LIKE @{paramIndex})");
            parameters.Add($"%{filter.SearchTerm}%");
            paramIndex++;
        }

        if (filter.DateFrom.HasValue)
        {
            whereClauses.Add($"eventDateUtc >= @{paramIndex}");
            parameters.Add(filter.DateFrom.Value);
            paramIndex++;
        }

        if (filter.DateTo.HasValue)
        {
            whereClauses.Add($"eventDateUtc <= @{paramIndex}");
            parameters.Add(filter.DateTo.Value);
            paramIndex++;
        }

        if (whereClauses.Count > 0)
        {
            sql = sql.Where(string.Join(" AND ", whereClauses), parameters.ToArray());
        }

        sql = sql.OrderByDescending("eventDateUtc");

        try
        {
            var page = db.Page<AuditEntryDto>(filter.Skip / Math.Max(filter.Take, 1) + 1, filter.Take, sql);
            return new AuditLogPagedResult<AuditEntryViewModel>
            {
                Items = page.Items.Select(MapToAuditViewModel),
                Total = page.TotalItems
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching audit entries");
            return new AuditLogPagedResult<AuditEntryViewModel> { Items = [], Total = 0 };
        }
    }

    public AuditLogPagedResult<LogEntryViewModel> GetLogEntries(AuditLogFilterRequest filter)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;

        var sql = scope.SqlContext.Sql()
            .Select("l.id, l.userId, l.DateStamp, l.logHeader, l.logComment, l.NodeId, l.entityType, u.userName")
            .From("umbracoLog l")
            .LeftJoin("umbracoUser u").On("l.userId = u.id");

        var whereClauses = new List<string>();
        var parameters = new List<object>();
        int paramIndex = 0;

        if (!string.IsNullOrWhiteSpace(filter.EventType))
        {
            whereClauses.Add($"l.logHeader = @{paramIndex}");
            parameters.Add(filter.EventType);
            paramIndex++;
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            whereClauses.Add($"(l.logComment LIKE @{paramIndex} OR u.userName LIKE @{paramIndex})");
            parameters.Add($"%{filter.SearchTerm}%");
            paramIndex++;
        }

        if (filter.DateFrom.HasValue)
        {
            whereClauses.Add($"l.DateStamp >= @{paramIndex}");
            parameters.Add(filter.DateFrom.Value);
            paramIndex++;
        }

        if (filter.DateTo.HasValue)
        {
            whereClauses.Add($"l.DateStamp <= @{paramIndex}");
            parameters.Add(filter.DateTo.Value);
            paramIndex++;
        }

        if (whereClauses.Count > 0)
        {
            sql = sql.Where(string.Join(" AND ", whereClauses), parameters.ToArray());
        }

        sql = sql.OrderByDescending("l.DateStamp");

        try
        {
            var page = db.Page<LogEntryDto>(filter.Skip / Math.Max(filter.Take, 1) + 1, filter.Take, sql);
            return new AuditLogPagedResult<LogEntryViewModel>
            {
                Items = page.Items.Select(MapToLogViewModel),
                Total = page.TotalItems
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching log entries");
            return new AuditLogPagedResult<LogEntryViewModel> { Items = [], Total = 0 };
        }
    }

    public IEnumerable<string> GetDistinctEventTypes()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        try
        {
            var sql = scope.SqlContext.Sql()
                .Select("DISTINCT eventType")
                .From("umbracoAudit");
            return db.Fetch<string>(sql);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching distinct event types");
            return [];
        }
    }

    public IEnumerable<string> GetDistinctLogHeaders()
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var db = scope.Database;
        try
        {
            var sql = scope.SqlContext.Sql()
                .Select("DISTINCT logHeader")
                .From("umbracoLog");
            return db.Fetch<string>(sql);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching distinct log headers");
            return [];
        }
    }

    private static AuditEntryViewModel MapToAuditViewModel(AuditEntryDto dto) => new()
    {
        Id = dto.Id,
        EventDateUtc = dto.EventDateUtc,
        PerformingUserId = dto.PerformingUserId,
        PerformingDetails = dto.PerformingDetails ?? string.Empty,
        PerformingIp = dto.PerformingIp ?? string.Empty,
        EventType = dto.EventType ?? string.Empty,
        EventDetails = dto.EventDetails ?? string.Empty,
        AffectedUserId = dto.AffectedUserId,
        AffectedDetails = dto.AffectedDetails ?? string.Empty
    };

    private static LogEntryViewModel MapToLogViewModel(LogEntryDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        UserName = dto.UserName ?? $"User {dto.UserId}",
        DateStamp = dto.DateStamp,
        LogHeader = dto.LogHeader ?? string.Empty,
        LogComment = dto.LogComment ?? string.Empty,
        NodeId = dto.NodeId,
        EntityType = dto.EntityType ?? string.Empty
    };
}

// DTOs matching database table columns
internal class AuditEntryDto
{
    public int Id { get; set; }
    public int PerformingUserId { get; set; }
    public string? PerformingDetails { get; set; }
    public string? PerformingIp { get; set; }
    public DateTime EventDateUtc { get; set; }
    public string? EventType { get; set; }
    public string? EventDetails { get; set; }
    public int AffectedUserId { get; set; }
    public string? AffectedDetails { get; set; }
}

internal class LogEntryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime DateStamp { get; set; }
    public string? LogHeader { get; set; }
    public string? LogComment { get; set; }
    public int NodeId { get; set; }
    public string? EntityType { get; set; }
}
