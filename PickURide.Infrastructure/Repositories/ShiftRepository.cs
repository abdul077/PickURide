using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Repositories
{
    public class ShiftRepository : IShiftRepository
    {
        private readonly PickURideDbContext _context;

        public ShiftRepository(PickURideDbContext context)
        {
            _context = context;
        }

        public async Task<string> ApplyForShiftAsync(ApplyShiftRequest request)
        {
            var shift = await _context.Shifts
                .Include(s => s.DriverShiftApplications)
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId);

            if (shift == null)
                return "Shift not found";

            if (shift.DriverShiftApplications.Any(a => a.DriverId == request.DriverId))
                return "Driver already applied";

            var approvedCount = shift.DriverShiftApplications.Count(a => a.Status == "Approved");
            if (approvedCount >= shift.MaxDriverCount)
                return "Max approved drivers reached";

            var application = new DriverShiftApplication
            {
                ApplicationId = Guid.NewGuid(),
                ShiftId = request.ShiftId,
                DriverId = request.DriverId,
                Status = "Pending",
                AppliedAt = DateTime.UtcNow
            };

            await _context.DriverShiftApplications.AddAsync(application);
            await _context.SaveChangesAsync();
            return "Applied successfully";
        }

        public async Task<string> ApproveApplicationAsync(Guid applicationId)
        {
            var app = await _context.DriverShiftApplications
                .Include(a => a.Shift)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (app == null)
                return "Application not found";

            var approvedCount = await _context.DriverShiftApplications
                .CountAsync(a => a.ShiftId == app.ShiftId && a.Status == "Approved");

            if (approvedCount >= app.Shift.MaxDriverCount)
                return "Shift is full";

            app.Status = "Approved";
            await _context.SaveChangesAsync();
            return "Application approved";
        }

        public async Task<Guid> CreateShiftAsync(CreateShiftRequest request)
        {
            var shift = new Shift
            {
                ShiftId = Guid.NewGuid(),
                ShiftDate = DateOnly.FromDateTime(request.ShiftDate),
                ShiftStart = request.ShiftStart,
                ShiftEnd = request.ShiftEnd,
                MaxDriverCount = request.MaxDriverCount,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Shifts.AddAsync(shift);
            await _context.SaveChangesAsync();

            return shift.ShiftId;
        }

        public async Task<string> UpdateShiftAsync(UpdateShiftRequest request)
        {
            var shift = await _context.Shifts
                .FirstOrDefaultAsync(s => s.ShiftId == request.ShiftId);

            if (shift == null)
                return "Shift not found";

            shift.ShiftDate = DateOnly.FromDateTime(request.ShiftDate);
            shift.ShiftStart = request.ShiftStart;
            shift.ShiftEnd = request.ShiftEnd;
            shift.MaxDriverCount = request.MaxDriverCount;
            shift.Description = request.Description;

            await _context.SaveChangesAsync();
            return "Shift updated successfully";
        }

        public async Task<List<ShiftDto>> GetAllShiftsAsync()
        {
            return await _context.Shifts
               .Select(s => new ShiftDto
               {
                   ShiftId = s.ShiftId,
                   ShiftDate = DateOnlyToDateTime(s.ShiftDate),
                   ShiftStart = s.ShiftStart,
                   ShiftEnd = s.ShiftEnd,
                   ShiftStartFormatted = s.ShiftStart.HasValue ? s.ShiftStart.Value.ToString("hh:mm tt") : null,
                   ShiftEndFormatted = s.ShiftEnd.HasValue ? s.ShiftEnd.Value.ToString("hh:mm tt") : null,
                   MaxDriverCount = s.MaxDriverCount,
                   Description = s.Description,
                   CreatedAt = s.CreatedAt
               }).ToListAsync();
        }
        private static DateTime DateOnlyToDateTime(DateOnly date)
        {
            return date.ToDateTime(TimeOnly.MinValue); // converts to DateTime at midnight
        }

        public async Task<List<ShiftDto>> GetActiveShiftsAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return await _context.Shifts
                .Where(s => s.ShiftDate == today)
                .Select(s => new ShiftDto
                {
                    ShiftId = s.ShiftId,
                    ShiftDate = DateOnlyToDateTime(s.ShiftDate),
                    ShiftStart = s.ShiftStart,
                    ShiftEnd = s.ShiftEnd,
                    ShiftStartFormatted = s.ShiftStart.HasValue ? s.ShiftStart.Value.ToString("hh:mm tt") : null,
                    ShiftEndFormatted = s.ShiftEnd.HasValue ? s.ShiftEnd.Value.ToString("hh:mm tt") : null,
                    MaxDriverCount = s.MaxDriverCount,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt
                }).ToListAsync();
        }

        public async Task<List<DriverShiftApplicationDto>> GetDriverAppliedShiftsAsync(Guid Id)
        {
            return await _context.DriverShiftApplications
                .Where(a => a.DriverId == Id)
                .Select(a => new DriverShiftApplicationDto
                {
                    ShiftId = a.ShiftId,
                    Status = a.Status,
                    
                }).ToListAsync();
        }

        public async Task<List<DriverShiftApplicationDto>> GetPendingShiftsApplications()
        {
            var pendingApps = await _context.DriverShiftApplications
                .Where(m => m.Status == "Pending")
                .Include(m => m.Driver)
                .Select(m => new DriverShiftApplicationDto
                {
                    ApplicationId = m.ApplicationId,
                    ShiftId = m.ShiftId,
                    DriverId = m.DriverId,
                    Status = m.Status,
                    AppliedDate = m.AppliedAt,
                    DriverName = m.Driver != null ? m.Driver.FullName : null
                })
                .ToListAsync();

            return pendingApps;
        }

        public async Task<List<DriverShiftApplicationDto>> GetApprovedShiftsApplications()
        {
           var approvedApps = await _context.DriverShiftApplications
                .Where(m => m.Status == "Approved")
                .Include(m => m.Driver)
                .Select(m => new DriverShiftApplicationDto
                {
                    ApplicationId = m.ApplicationId,
                    ShiftId = m.ShiftId,
                    DriverId = m.DriverId,
                    Status = m.Status,
                    AppliedDate = m.AppliedAt,
                    DriverName = m.Driver != null ? m.Driver.FullName : null
                })
                .ToListAsync();
            return approvedApps;
        }

        public async Task<List<DriverShiftApplicationDto>> GetRejectedShiftsApplications()
        {
            var rejectedApps =await _context.DriverShiftApplications
                .Where(m => m.Status == "Rejected")
                .Include(m => m.Driver)
                .Select(m => new DriverShiftApplicationDto
                {
                    ApplicationId = m.ApplicationId,
                    ShiftId = m.ShiftId,
                    DriverId = m.DriverId,
                    Status = m.Status,
                    AppliedDate = m.AppliedAt,
                    DriverName = m.Driver != null ? m.Driver.FullName : null
                })
                .ToListAsync();
            return rejectedApps;
        }

        public async Task<string> RejectApplicationAsync(Guid applicationId)
        {
            var app = await _context.DriverShiftApplications
                .Include(a => a.Shift)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (app == null)
                return "Application not found";

            var approvedCount = await _context.DriverShiftApplications
                .CountAsync(a => a.ShiftId == app.ShiftId && a.Status == "Reject");

            app.Status = "Reject";
            await _context.SaveChangesAsync();
            return "Application rejected";
        }
    }
}
