using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLogModel auditLog);

    Task<List<AuditLogDto>> GetAllAsync(int pageNumber, int pageSize, string? actionFilter, string? entityTypeFilter, DateTime? startDate, DateTime? endDate);

    Task<List<AuditLogDto>> GetByUserIdAsync(Guid userId, string userType, int pageNumber, int pageSize, string? actionFilter, DateTime? startDate, DateTime? endDate);

    Task<AuditLogDto?> GetByIdAsync(Guid auditLogId);

    Task<int> GetTotalCountAsync(string? actionFilter, string? entityTypeFilter, DateTime? startDate, DateTime? endDate);

    Task<int> GetTotalCountByUserIdAsync(Guid userId, string userType, string? actionFilter, DateTime? startDate, DateTime? endDate);
}

