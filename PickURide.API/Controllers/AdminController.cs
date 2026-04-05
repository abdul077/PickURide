using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Services;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }
        [HttpPost("get-all-admins")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var admins = await _adminService.GetAllAdminAsync();
            if (admins == null || admins.Count == 0)
            {
                return NotFound(new { Message = "No admins found." });
            }
            return Ok(admins);
        }
        [HttpPost("get-admin-by-id/{adminId}")]
        public async Task<IActionResult> GetAdminById([FromRoute] Guid adminId)
        {
            var admin = await _adminService.GetAdminByIdAsync(adminId);
            if (admin == null)
            {
                return NotFound(new { Message = "Admin not found." });
            }
            return Ok(admin);
        }
        [HttpPost("get-admin-by-email")]
        public async Task<IActionResult> GetAdminByEmail([FromQuery] string email)
        {
            var admin = await _adminService.GetAdminByEmailAsync(email);
            if (admin == null)
            {
                return NotFound(new { Message = "Admin not found." });
            }
            return Ok(admin);
        }
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAdmin([FromBody] Application.Models.LoginRequest request)
        {
            var jwtResponse = await _adminService.LoginAdminAsync(request);
            return Ok(jwtResponse);
        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterAdmin([FromBody] Application.Models.AdminRegisterRequest request)
        {
            var result = await _adminService.RegisterAdminAsync(request);
            if (result == "Email already registered")
            {
                return Ok(new { message = "Email already taken" });
            }
            return Ok(new { message = "Admin registered successfully" });
        }
        [HttpPost("dashboard-counts")]
        public async Task<IActionResult> DashboardCounts()
        {
            var result = await _adminService.GetDashboardDataAsync();
            return Ok(result);
        }

        [HttpPost("dashboard-trends")]
        public async Task<IActionResult> GetDashboardTrends([FromBody] Application.Models.TrendRequest request)
        {
            var trends = await _adminService.GetDashboardTrendsAsync(request);
            return Ok(trends);
        }

        [HttpGet("support-chat/history/{driverId}")]
        public async Task<IActionResult> GetSupportChatHistory([FromRoute] Guid driverId, [FromQuery] DateTime? before, [FromQuery] int limit = 50)
        {
            var messages = await _adminService.GetSupportChatHistoryAsync(driverId, before, limit);
            return Ok(messages);
        }
    }
}
