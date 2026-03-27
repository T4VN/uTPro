namespace uTPro.Feature.AuditLog.Models;

public class AuditEntryViewModel
{
    public int Id { get; set; }
    public DateTime EventDateUtc { get; set; }
    public int PerformingUserId { get; set; }
    public string PerformingDetails { get; set; } = string.Empty;
    public string PerformingIp { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventDetails { get; set; } = string.Empty;
    public int AffectedUserId { get; set; }
    public string AffectedDetails { get; set; } = string.Empty;
}

public class LogEntryViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime DateStamp { get; set; }
    public string LogHeader { get; set; } = string.Empty;
    public string LogComment { get; set; } = string.Empty;
    public int NodeId { get; set; }
    public string EntityType { get; set; } = string.Empty;
}

public class AuditLogPagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public long Total { get; set; }
}

public class AuditLogFilterRequest
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string? EventType { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
