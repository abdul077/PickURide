using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IAdminRepository
    {
        Task<AdminModel?> GetByEmailAsync(string email);
        Task CreateAsync(AdminModel admin);
        Task UpdateToken(Guid Id, string Token);
        Task Verified(string email, bool status);
        Task<List<AdminModel?>> GetAllAdminAsync();
        Task<AdminModel?> GetAdminByIdAsync(Guid adminId);
        Task<Dashboard> GetDashboardDataAsync();
        Task<DashboardTrends> GetDashboardTrendsAsync(TrendRequest request);
    }
}
