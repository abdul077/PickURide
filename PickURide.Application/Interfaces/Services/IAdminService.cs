using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Services
{
    public interface IAdminService
    {
        Task<string> RegisterAdminAsync(AdminRegisterRequest request);
        Task<JwtResponse> LoginAdminAsync(LoginRequest request);
        Task<string> VerifyAdmin(string email, bool status);
        Task<List<AdminModel?>> GetAllAdminAsync();
        Task<AdminModel?> GetAdminByIdAsync(Guid userId);
        Task<AdminModel?> GetAdminByEmailAsync(string email);
        Task<Dashboard> GetDashboardDataAsync();
        Task<DashboardTrends> GetDashboardTrendsAsync(TrendRequest request);
        Task<List<SupportChatModel>> GetSupportChatHistoryAsync(Guid driverId, DateTime? before = null, int limit = 50);
    }
}
