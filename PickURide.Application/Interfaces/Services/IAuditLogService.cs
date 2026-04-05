using PickURide.Application.Models;
using System;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services;

public interface IAuditLogService
{
    Task LogActionAsync(AuditLogRequest request);

    Task<PagedResult<AuditLogDto>> GetAllAuditLogsAsync(int pageNumber, int pageSize, string? actionFilter, string? entityTypeFilter, DateTime? startDate, DateTime? endDate);

    Task<PagedResult<AuditLogDto>> GetUserAuditLogsAsync(Guid userId, string userType, int pageNumber, int pageSize, string? actionFilter, DateTime? startDate, DateTime? endDate);

    Task<AuditLogDto?> GetAuditLogByIdAsync(Guid auditLogId, Guid? requestingUserId, string? requestingUserType);
}

