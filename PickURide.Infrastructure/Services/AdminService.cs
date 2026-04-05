using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly ISupportChatRepository _supportChatRepository;
        private readonly IConfiguration _configuration;

        public AdminService(IAdminRepository adminRepository, ISupportChatRepository supportChatRepository, IConfiguration configuration)
        {
            _adminRepository = adminRepository;
            _supportChatRepository = supportChatRepository;
            _configuration = configuration;
        }
        public async Task<AdminModel?> GetAdminByEmailAsync(string email)
        {
            return await _adminRepository.GetByEmailAsync(email);
        }

        public async Task<AdminModel?> GetAdminByIdAsync(Guid userId)
        {
            return await _adminRepository.GetAdminByIdAsync(userId);
        }

        public async Task<List<AdminModel?>> GetAllAdminAsync()
        {
            return await _adminRepository.GetAllAdminAsync();
        }

        public Task<Dashboard> GetDashboardDataAsync()
        {
            return _adminRepository.GetDashboardDataAsync();
        }

        public Task<DashboardTrends> GetDashboardTrendsAsync(TrendRequest request)
        {
            return _adminRepository.GetDashboardTrendsAsync(request);
        }

        public async Task<JwtResponse> LoginAdminAsync(LoginRequest request)
        {
            var user = await _adminRepository.GetByEmailAsync(request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new JwtResponse
                {
                    Token = string.Empty,
                    UserId = Guid.Empty,
                    Email = string.Empty,
                    FullName = string.Empty,
                    Message = "Invalid Credentials"
                };
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.AdminId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return new JwtResponse
            {
                Token = tokenHandler.WriteToken(token),
                Expires = tokenDescriptor.Expires ?? DateTime.UtcNow.AddHours(5),
                UserId = user.AdminId,
                Email = user.Email,
                FullName = user.FullName,
                Message = "Login successful"
            };
        }

        public async Task<string> RegisterAdminAsync(AdminRegisterRequest request)
        {
            var userExists = await _adminRepository.GetByEmailAsync(request.Email);
            if (userExists != null)
                return "Email already registered";

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new AdminModel
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.Phone,
                PasswordHash = hashedPassword,
                CreatedAt=DateTime.UtcNow,
                IsActive=true,
                Role= request.Role
            };

            await _adminRepository.CreateAsync(user);
            return "Registered successfully";
        }

        public Task<string> VerifyAdmin(string email, bool status)
        {
            throw new NotImplementedException();
        }

        public async Task<List<SupportChatModel>> GetSupportChatHistoryAsync(Guid driverId, DateTime? before = null, int limit = 50)
        {
            return await _supportChatRepository.GetChatHistoryAsync(driverId, before, limit);
        }
    }
}
