using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PickURide.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<List<Users.GetUserDto>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<UsersModel?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<Users.GetUserDto?> GetUserByIdAsync(Guid userId)
        {
            return await _userRepository.GetUserByIdAsync(userId);
        }

        public async Task<JwtResponse> LoginUserAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
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

            if (user.Verified == null || user.Verified == false)
            {
                return new JwtResponse
                {
                    Token = string.Empty,
                    UserId = Guid.Empty,
                    Email = string.Empty,
                    FullName = string.Empty,
                    Message = "Unable to login because user account is not verified"
                };
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName)
                }),
                Expires = DateTime.UtcNow.AddHours(5),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            await _userRepository.UpdateToken(user.UserId, request.DeviceToken);
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return new JwtResponse
            {
                Token = tokenHandler.WriteToken(token),
                Expires = tokenDescriptor.Expires ?? DateTime.UtcNow.AddHours(5),
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Message = "Login successful"
            };
        }

        public async Task<string> RegisterUserAsync(RegisterRequest request)
        {
            var userExists = await _userRepository.GetByEmailAsync(request.Email);
            if (userExists != null && userExists.Verified == true)
                return "Email already registered";

            if (userExists!=null)
            {
                if (userExists.Verified == null || userExists.Verified == false)
                {
                    await _userRepository.DeleteUnverifiedByEmailAsync(request.Email);
                }
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new UsersModel
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.Phone,
                PasswordHash = hashedPassword
            };

            await _userRepository.CreateAsync(user);
            return "User registered successfully";
        }

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null) return false;

            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            return await _userRepository.ResetPasswordAsync(user.UserId, hashed);
        }

        public Task<UpdateUserModel> SingleUser(Guid userId)
        {
            return _userRepository.SingleUser(userId);
        }

        public Task UpdateUserAsync(UpdateUserModel user)
        {
            return _userRepository.UpdateAsync(user);
        }

        public async Task<string> VerifyUser(string email, bool status)
        {
            try
            {
                await _userRepository.Verified(email, status);
                return "Account Verified";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        public Task<bool> DeleteUserAsync(Guid userId)
        {
            return _userRepository.DeleteUserAsync(userId);
        }
    }
}
