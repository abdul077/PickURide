using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Services
{
    public class DriverAttendanceService : IDriverAttendanceService
    {
        private readonly PickURideDbContext _context;

        public DriverAttendanceService(PickURideDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> StartDutyAsync(StartDutyRequest request)
        {
            var existing = await _context.DriverAttendances
                .Where(a => a.DriverId == request.DriverId && a.EndTime == null)
                .FirstOrDefaultAsync();

            if (existing != null)
                throw new Exception("Already on duty.");

            var attendance = new DriverAttendance
            {
                AttendanceId = Guid.NewGuid(),
                DriverId = request.DriverId,
                StartTime = DateTime.UtcNow,
                AttendanceType = request.AttendanceType,
                CreatedAt = DateTime.UtcNow
            };

            await _context.DriverAttendances.AddAsync(attendance);
            await _context.SaveChangesAsync();
            return attendance.AttendanceId;
        }

        public async Task EndDutyAsync(EndDutyRequest request)
        {
            var attendance = await _context.DriverAttendances
                .FirstOrDefaultAsync(a => a.AttendanceId == request.AttendanceId);

            if (attendance == null)
                throw new Exception("Attendance not found.");

            if (attendance.EndTime != null)
                throw new Exception("Duty already ended.");

            attendance.EndTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsEligibleForShiftAsync(Guid driverId, DateTime shiftDate)
        {
            var totalMinutes = await _context.DriverAttendances
                .Where(a => a.DriverId == driverId &&
                            a.StartTime.Date == shiftDate.Date &&
                            a.EndTime != null)
                .SumAsync(a => EF.Functions.DateDiffMinute(a.StartTime, a.EndTime.Value));

            return totalMinutes >= 480; // 8 hours
        }
    }

}
