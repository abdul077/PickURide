using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;

namespace PickURide.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly PickURideDbContext _context;

        public AdminRepository(PickURideDbContext context)
        {
            _context = context;
        }
        public async Task CreateAsync(AdminModel admin)
        {
            var dbUser = new Admin
            {
                AdminId = Guid.NewGuid(),
                FullName = admin.FullName,
                Email = admin.Email,
                PhoneNumber = admin.PhoneNumber,
                PasswordHash = admin.PasswordHash,
                Role = admin.Role,
                IsActive = admin.IsActive,
                CreatedAt = admin.CreatedAt
            };

            _context.Admins.Add(dbUser);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AdminModel?>> GetAllAdminAsync()
        {
            var admin = await _context.Admins
                .Select(a => new AdminModel
                {
                    AdminId = a.AdminId,
                    FullName = a.FullName,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    PasswordHash = a.PasswordHash,
                    Role = a.Role,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
            if (admin == null || admin.Count == 0)
            {
                return new List<AdminModel?>();
            }
            else
            {
                return admin;
            }
        }

        public Task<AdminModel?> GetByEmailAsync(string email)
        {
            var admin = _context.Admins
                .Where(a => a.Email == email)
                .Select(a => new AdminModel
                {
                    AdminId = a.AdminId,
                    FullName = a.FullName,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    PasswordHash = a.PasswordHash,
                    Role = a.Role,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();
            return admin;
        }

        public Task<AdminModel?> GetAdminByIdAsync(Guid adminId)
        {
            var admin = _context.Admins
                .Where(a => a.AdminId == adminId)
                .Select(a => new AdminModel
                {
                    AdminId = a.AdminId,
                    FullName = a.FullName,
                    Email = a.Email,
                    PhoneNumber = a.PhoneNumber,
                    PasswordHash = a.PasswordHash,
                    Role = a.Role,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();
            return admin;
        }

        public Task UpdateToken(Guid Id, string Token)
        {
            throw new NotImplementedException();
        }

        public Task Verified(string email, bool status)
        {
            throw new NotImplementedException();
        }

        public async Task<Dashboard> GetDashboardDataAsync()
        {
            // Pseudocode:
            // 1. Count total users.
            // 2. Count total drivers.
            // 3. Count completed rides (Status == "Completed").
            // 4. Count pending rides (Status == "Pending" or "Waiting").
            // 5. Count in-progress rides (Status == "InProgress").
            // 6. Sum total income from payments (PaidAmount).
            // 7. Sum total paid to drivers (DriverShare where PaymentStatus == "Paid").
            // 8. Sum total admin commission (AdminShare).
            // 9. Sum total driver shares (DriverShare).

            var totalUsers = await _context.Users.CountAsync();
            var totalDrivers = await _context.Drivers.CountAsync();

            var completedRides = await _context.Set<Ride>()
                .CountAsync(r => r.Status == "Completed");

            var pendingRides = await _context.Set<Ride>()
                .CountAsync(r => r.Status == "Pending" || r.Status == "Waiting");

            var inProgressRides = await _context.Set<Ride>()
                .CountAsync(r => r.Status == "In-Progress");

            // Sum of PaidAmount where PaymentStatus == "completed" (total revenue from completed payments)
            var totalIncome = await _context.Set<Payment>()
                .Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed")
                .SumAsync(p => (decimal?)p.PaidAmount) ?? 0;

            // Sum of DriverShare where PaymentStatus == "completed" (driver earnings from completed payments)
            var paidToDrivers = await _context.Set<Payment>()
                .Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed")
                .SumAsync(p => (decimal?)p.DriverShare) ?? 0;

            // Sum of AdminShare where PaymentStatus == "completed" (commission earned from completed payments)
            var adminCommission = await _context.Set<Payment>()
                .Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed")
                .SumAsync(p => (decimal?)p.AdminShare) ?? 0;

            // Sum of DriverShare where PaymentStatus == "completed" (total driver earnings from completed payments)
            var driverShares = await _context.Set<Payment>()
                .Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed")
                .SumAsync(p => (decimal?)p.DriverShare) ?? 0;

            // Sum of PaidAmount for payments where status is "held" and there is no completed payment for that ride
            var heldPayments = await _context.Set<Payment>()
                .Where(p => p.PaymentStatus != null && p.PaymentStatus.ToLower() == "held" &&
                           p.RideId.HasValue &&
                           !_context.Set<Payment>()
                               .Any(cp => cp.RideId == p.RideId && cp.PaymentStatus != null && cp.PaymentStatus.ToLower() == "completed"))
                .SumAsync(p => (decimal?)p.PaidAmount) ?? 0;

            var activeDrivers=await _context.Drivers.CountAsync(d => d.Status=="Available");

            var totalRides = await _context.Set<Ride>().CountAsync();

            return new Dashboard
            {
                NoOfUsers = totalUsers,
                NoOfDrivers = totalDrivers,
                CompleteRide = completedRides,
                WaitingRides = pendingRides,
                InProgressRide = inProgressRides,
                TotalIncome = totalIncome,
                PaidIncomeDrivers = paidToDrivers,
                AdminCommission = adminCommission,
                DriverShares = driverShares,
                HeldPayments = heldPayments,
                TotalRides = totalRides,
                ActiveDrivers = activeDrivers
            };
        }

        public async Task<DashboardTrends> GetDashboardTrendsAsync(TrendRequest request)
        {
            var days = request.Days ?? 30;
            var startDate = DateTime.UtcNow.AddDays(-days);
            
            // Daily metrics for last N days
            var dailyMetrics = await _context.Set<Payment>()
                .Where(p => p.CreatedAt >= startDate)
                .GroupBy(p => p.CreatedAt!.Value.Date)
                .Select(g => new DailyMetric
                {
                    Date = g.Key,
                    Revenue = g.Sum(p => p.PaidAmount ?? 0),
                    RideCount = g.Where(p => p.RideId.HasValue).Select(p => p.RideId!.Value).Distinct().Count(),
                    Commission = g.Sum(p => p.AdminShare ?? 0)
                })
                .OrderBy(m => m.Date)
                .ToListAsync();
            
            // Monthly metrics for last 12 months (only completed payments for revenue)
            var monthlyData = await _context.Set<Payment>()
                .Where(p => p.CreatedAt >= DateTime.UtcNow.AddMonths(-12) &&
                           p.PaymentStatus != null &&
                           p.PaymentStatus.ToLower() == "completed")
                .GroupBy(p => new { p.CreatedAt!.Value.Year, p.CreatedAt!.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(p => p.PaidAmount ?? 0),
                    CompletedRides = g.Where(p => p.RideId.HasValue).Select(p => p.RideId!.Value).Distinct().Count()
                })
                .ToListAsync();
            
            var monthlyMetrics = monthlyData
                .Select(m => new MonthlyMetric
                {
                    Month = $"{m.Year}-{m.Month:00}",
                    Revenue = m.Revenue,
                    CompletedRides = m.CompletedRides
                })
                .OrderBy(m => m.Month)
                .ToList();
            
            // Status breakdown
            var statusBreakdown = new RideStatusBreakdown
            {
                Completed = await _context.Set<Ride>().CountAsync(r => r.Status == "Completed"),
                InProgress = await _context.Set<Ride>().CountAsync(r => r.Status == "In-Progress"),
                Waiting = await _context.Set<Ride>().CountAsync(r => r.Status == "Waiting" || r.Status == "Pending"),
                Cancelled = await _context.Set<Ride>().CountAsync(r => r.Status == "Cancelled")
            };

            // Transaction status breakdown
            var completedTransactions = await _context.Set<Payment>()
                .Where(p => p.CreatedAt >= startDate && p.PaymentStatus != null && p.PaymentStatus.ToLower() == "completed")
                .GroupBy(p => p.CreatedAt!.Value.Date)
                .Select(g => new TransactionStatusMetric
                {
                    Date = g.Key,
                    Revenue = g.Sum(p => p.PaidAmount ?? 0),
                    TransactionCount = g.Count()
                })
                .OrderBy(m => m.Date)
                .ToListAsync();

            var heldTransactions = await _context.Set<Payment>()
                .Where(p => p.CreatedAt >= startDate &&
                           p.PaymentStatus != null &&
                           p.PaymentStatus.ToLower() == "held" &&
                           p.RideId.HasValue &&
                           !_context.Set<Payment>()
                               .Any(cp => cp.RideId == p.RideId && cp.PaymentStatus != null && cp.PaymentStatus.ToLower() == "completed"))
                .GroupBy(p => p.CreatedAt!.Value.Date)
                .Select(g => new TransactionStatusMetric
                {
                    Date = g.Key,
                    Revenue = g.Sum(p => p.PaidAmount ?? 0),
                    TransactionCount = g.Count()
                })
                .OrderBy(m => m.Date)
                .ToListAsync();

            var cancelledTransactions = await _context.Set<Payment>()
                .Where(p => p.CreatedAt >= startDate && p.PaymentStatus != null && p.PaymentStatus.ToLower() == "cancelled")
                .GroupBy(p => p.CreatedAt!.Value.Date)
                .Select(g => new TransactionStatusMetric
                {
                    Date = g.Key,
                    Revenue = g.Sum(p => p.PaidAmount ?? 0),
                    TransactionCount = g.Count()
                })
                .OrderBy(m => m.Date)
                .ToListAsync();

            var transactionStatusBreakdown = new TransactionStatusBreakdown
            {
                CompletedTransactions = completedTransactions,
                HeldTransactions = heldTransactions,
                CancelledTransactions = cancelledTransactions
            };

            return new DashboardTrends
            {
                DailyMetrics = dailyMetrics,
                MonthlyMetrics = monthlyMetrics,
                StatusBreakdown = statusBreakdown,
                TransactionStatusBreakdown = transactionStatusBreakdown
            };
        }
    }
}
