using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly PickURideDbContext _context;

    public AuditLogRepository(PickURideDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(AuditLogModel auditLog)
    {
        var entity = new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            UserId = auditLog.UserId,
            UserType = auditLog.UserType,
            Action = auditLog.Action,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            RequestData = auditLog.RequestData,
            ResponseData = auditLog.ResponseData,
            IpAddress = auditLog.IpAddress,
            UserAgent = auditLog.UserAgent,
            Status = auditLog.Status,
            ErrorMessage = auditLog.ErrorMessage,
            Timestamp = auditLog.Timestamp,
            Duration = auditLog.Duration
        };

        _context.AuditLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLogDto>> GetAllAsync(int pageNumber, int pageSize, string? actionFilter, string? entityTypeFilter, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        if (!string.IsNullOrWhiteSpace(entityTypeFilter))
        {
            query = query.Where(a => a.EntityType == entityTypeFilter);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        var skip = (pageNumber - 1) * pageSize;

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditLogId,
                UserId = a.UserId,
                UserType = a.UserType,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                RequestData = a.RequestData,
                ResponseData = a.ResponseData,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Status = a.Status,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp,
                Duration = a.Duration
            })
            .ToListAsync();
    }

    public async Task<List<AuditLogDto>> GetByUserIdAsync(Guid userId, string userType, int pageNumber, int pageSize, string? actionFilter, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.AuditLogs
            .Where(a => a.UserId == userId && a.UserType == userType);

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        var skip = (pageNumber - 1) * pageSize;

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditLogId,
                UserId = a.UserId,
                UserType = a.UserType,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                RequestData = a.RequestData,
                ResponseData = a.ResponseData,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Status = a.Status,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp,
                Duration = a.Duration
            })
            .ToListAsync();
    }

    public async Task<AuditLogDto?> GetByIdAsync(Guid auditLogId)
    {
        var auditLog = await _context.AuditLogs
            .Where(a => a.AuditLogId == auditLogId)
            .Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditLogId,
                UserId = a.UserId,
                UserType = a.UserType,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                RequestData = a.RequestData,
                ResponseData = a.ResponseData,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                Status = a.Status,
                ErrorMessage = a.ErrorMessage,
                Timestamp = a.Timestamp,
                Duration = a.Duration
            })
            .FirstOrDefaultAsync();

        return auditLog;
    }

    public async Task<int> GetTotalCountAsync(string? actionFilter, string? entityTypeFilter, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        if (!string.IsNullOrWhiteSpace(entityTypeFilter))
        {
            query = query.Where(a => a.EntityType == entityTypeFilter);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetTotalCountByUserIdAsync(Guid userId, string userType, string? actionFilter, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.AuditLogs
            .Where(a => a.UserId == userId && a.UserType == userType);

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        return await query.CountAsync();
    }
}

