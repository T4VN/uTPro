using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using uTPro.Feature.AuditLog.Models;
using uTPro.Feature.AuditLog.Services;

namespace uTPro.Feature.AuditLog.Controllers;

[VersionedApiBackOfficeRoute("utpro/audit-log")]
[ApiExplorerSettings(GroupName = "uTPro Audit Log")]
public class AuditLogApiController(IAuditLogService auditLogService) : ManagementApiControllerBase
{
    [HttpGet("audit-entries")]
    public IActionResult GetAuditEntries(
        int skip = 0, int take = 20,
        string? eventType = null, string? searchTerm = null,
        DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var filter = new AuditLogFilterRequest
        {
            Skip = skip,
            Take = take,
            EventType = eventType,
            SearchTerm = searchTerm,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        return Ok(auditLogService.GetAuditEntries(filter));
    }

    [HttpGet("log-entries")]
    public IActionResult GetLogEntries(
        int skip = 0, int take = 20,
        string? eventType = null, string? searchTerm = null,
        DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var filter = new AuditLogFilterRequest
        {
            Skip = skip,
            Take = take,
            EventType = eventType,
            SearchTerm = searchTerm,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        return Ok(auditLogService.GetLogEntries(filter));
    }

    [HttpGet("event-types")]
    public IActionResult GetEventTypes()
        => Ok(auditLogService.GetDistinctEventTypes());

    [HttpGet("log-headers")]
    public IActionResult GetLogHeaders()
        => Ok(auditLogService.GetDistinctLogHeaders());
}
