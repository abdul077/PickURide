using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogActionAsync(AuditLogRequest request)
    {
        var auditLog = new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = request.UserId,
            UserType = request.UserType,
            Action = request.Action,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            OldValues = request.OldValues,
            NewValues = request.NewValues,
            RequestData = request.RequestData,
            ResponseData = request.ResponseData,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            Status = request.Status,
            ErrorMessage = request.ErrorMessage,
            Timestamp = DateTime.UtcNow,
            Duration = request.Duration
        };

        await _auditLogRepository.CreateAsync(auditLog);
    }

    public async Task<PagedResult<AuditLogDto>> GetAllAuditLogsAsync(int pageNumber, int pageSize, string? actionFilter, string? entityTypeFilter, DateTime? startDate, DateTime? endDate)
    {
        var data = await _auditLogRepository.GetAllAsync(pageNumber, pageSize, actionFilter, entityTypeFilter, startDate, endDate);
        var totalCount = await _auditLogRepository.GetTotalCountAsync(actionFilter, entityTypeFilter, startDate, endDate);

        return new PagedResult<AuditLogDto>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<AuditLogDto>> GetUserAuditLogsAsync(Guid userId, string userType, int pageNumber, int pageSize, string? actionFilter, DateTime? startDate, DateTime? endDate)
    {
        var data = await _auditLogRepository.GetByUserIdAsync(userId, userType, pageNumber, pageSize, actionFilter, startDate, endDate);
        var totalCount = await _auditLogRepository.GetTotalCountByUserIdAsync(userId, userType, actionFilter, startDate, endDate);

        return new PagedResult<AuditLogDto>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<AuditLogDto?> GetAuditLogByIdAsync(Guid auditLogId, Guid? requestingUserId, string? requestingUserType)
    {
        var auditLog = await _auditLogRepository.GetByIdAsync(auditLogId);

        if (auditLog == null)
        {
            return null;
        }

        // Authorization check: Admin can see all, others can only see their own
        if (requestingUserType != "Admin")
        {
            if (auditLog.UserId != requestingUserId || auditLog.UserType != requestingUserType)
            {
                return null; // Unauthorized
            }
        }

        return auditLog;
    }
}

