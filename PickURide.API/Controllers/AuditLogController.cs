using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Extensions;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using System;
using System.Threading.Tasks;

namespace PickURide.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpPost("get-all-audit-logs")]
    public async Task<IActionResult> GetAllAuditLogs([FromBody] AuditLogFilterRequest? request)
    {
        // Only Admin can access all logs
        if (!HttpContext.IsAdmin())
        {
            return Unauthorized(new { message = "Only administrators can access all audit logs." });
        }

        request ??= new AuditLogFilterRequest();

        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;

        var result = await _auditLogService.GetAllAuditLogsAsync(
            pageNumber,
            pageSize,
            request.ActionFilter,
            request.EntityTypeFilter,
            request.StartDate,
            request.EndDate
        );

        return Ok(result);
    }

    [HttpPost("get-my-audit-logs")]
    public async Task<IActionResult> GetMyAuditLogs([FromBody] AuditLogFilterRequest? request)
    {
        var userId = HttpContext.GetUserId();
        var userType = HttpContext.GetUserType();

        if (!userId.HasValue || string.IsNullOrEmpty(userType))
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        request ??= new AuditLogFilterRequest();

        var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        var pageSize = request.PageSize > 0 && request.PageSize <= 100 ? request.PageSize : 20;

        var result = await _auditLogService.GetUserAuditLogsAsync(
            userId.Value,
            userType,
            pageNumber,
            pageSize,
            request.ActionFilter,
            request.StartDate,
            request.EndDate
        );

        return Ok(MapToResponse(result));
    }

    [HttpPost("get-audit-log/{auditLogId}")]
    public async Task<IActionResult> GetAuditLogById(Guid auditLogId)
    {
        var userId = HttpContext.GetUserId();
        var userType = HttpContext.GetUserType();

        var result = await _auditLogService.GetAuditLogByIdAsync(auditLogId, userId, userType);

        if (result == null)
        {
            return NotFound(new { message = "Audit log not found or you don't have permission to view it." });
        }

        // Return the full audit log; mapping is only for the "my" endpoint
        return Ok(result);
    }

    private static PagedResult<AuditLogResponseDto> MapToResponse(PagedResult<AuditLogDto> result)
    {
        return new PagedResult<AuditLogResponseDto>
        {
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            Data = result.Data.Select(a => new AuditLogResponseDto
            {
                Timespan = a.Timespan,
                EntityType = a.EntityType,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                ResponseData = a.ResponseData,
                Status = a.Status
            }).ToList()
        };
    }
}

public class AuditLogResponseDto
{
    public int? Timespan { get; set; }
    public string? EntityType { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ResponseData { get; set; }
    public string Status { get; set; } = string.Empty;
}

// Request model for filtering audit logs
public class AuditLogFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? ActionFilter { get; set; }
    public string? EntityTypeFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

