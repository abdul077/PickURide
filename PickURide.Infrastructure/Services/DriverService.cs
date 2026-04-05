using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using PickURide.Infrastructure.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PickURide.Infrastructure.Services
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;
        private readonly IConfiguration _configuration;

        public DriverService(IDriverRepository driverRepository, IConfiguration configuration)
        {
            _driverRepository = driverRepository;
            _configuration = configuration;
        }

        public Task<string> AcceptDriver(Guid driverId)
        => _driverRepository.AcceptDriver(driverId);

        public Task<IEnumerable<AllDriverDtos>> GetAllDriversAsync()
            => _driverRepository.GetAllDriversAsync();

        public Task<IEnumerable<AllDriverDtos>> GetApprovedDriversAsync()
            => _driverRepository.GetApprovedDriversAsync();

        public Task<IEnumerable<DriverAttendanceDto>> GetDriverAttendancesAsync(Guid driverId)
            => _driverRepository.GetDriverAttendancesAsync(driverId);

        public Task<DriverModel?> GetDriverByEmailAsync(string email)
            => _driverRepository.GetDriverByEmailAsync(email);

        public Task<IEnumerable<DriverLocationHistoryDto>> GetDriverLocationHistoriesAsync(Guid driverId)
            => _driverRepository.GetDriverLocationHistoriesAsync(driverId);

        public Task<IEnumerable<DriverOvertimeDutyDto>> GetDriverOvertimeDutiesAsync(Guid driverId)
            => _driverRepository.GetDriverOvertimeDutiesAsync(driverId);

        public Task<IEnumerable<DriverRideDto>> GetDriverRidesAsync(Guid driverId)
             => _driverRepository.GetDriverRidesAsync(driverId);

        public Task<DriverDto?> GetDriverByIdAsync(Guid driverId)
            => _driverRepository.GetByIdAsync(driverId);

        public Task<bool> UpdateDriverDetailsAsync(UpdateDriverDetailsRequest request, CancellationToken cancellationToken)
            => _driverRepository.UpdateDriverDetailsAsync(request, cancellationToken);

        public Task<IEnumerable<DriverShiftApplicationDto>> GetDriverShiftApplicationsAsync(Guid driverId)
            => _driverRepository.GetDriverShiftApplicationsAsync(driverId);

        public Task<IEnumerable<DriverShiftDto>> GetDriverShiftsAsync(Guid driverId)
            => _driverRepository.GetDriverShiftsAsync(driverId);

        public async Task<JwtResponse> LoginDriverAsync(LoginRequest request)
        {
            var user = await _driverRepository.GetByEmailAsync(request.Email);
            
            // Check if user exists
            if (user == null)
            {
                return new JwtResponse
                {
                    Token = string.Empty,
                    UserId = Guid.Empty,
                    Email = string.Empty,
                    FullName = string.Empty,
                    Message = "Invalid Credentials - User not found",
                    ApprovalStatus = null
                };
            }
            
            // Check if password hash exists
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return new JwtResponse
                {
                    Token = string.Empty,
                    UserId = Guid.Empty,
                    Email = string.Empty,
                    FullName = string.Empty,
                    Message = "Invalid Credentials - Password not set",
                    ApprovalStatus = user.ApprovalStatus
                };
            }
            
            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new JwtResponse
                {
                    Token = string.Empty,
                    UserId = Guid.Empty,
                    Email = string.Empty,
                    FullName = string.Empty,
                    Message = "Invalid Credentials - Incorrect password",
                    ApprovalStatus = user.ApprovalStatus
                };
            }
            
            // Check if user is verified
            if (user.Verified == null || user.Verified == false)
            {
                return new JwtResponse
                {
                    Token = string.Empty,
                    UserId = Guid.Empty,
                    Email = string.Empty,
                    FullName = string.Empty,
                    Message = "Unable to login because user account is not verified",
                    ApprovalStatus = user.ApprovalStatus
                };
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            var key = Encoding.ASCII.GetBytes(jwtKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                new Claim(ClaimTypes.NameIdentifier, user.DriverId.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };
            
            // Update device token if provided (don't fail login if this fails)
            try
            {
                if (!string.IsNullOrEmpty(request.DeviceToken))
                {
                    await _driverRepository.UpdateToken(user.DriverId, request.DeviceToken);
                }
            }
            catch (Exception)
            {
                // Log but don't fail login if token update fails
                // This allows login to proceed even if device token update has issues
            }
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            if (user.ApprovalStatus == "Rejected")
            {
                return new JwtResponse
                {
                    Token = tokenHandler.WriteToken(token),
                    Expires = tokenDescriptor.Expires ?? DateTime.UtcNow.AddHours(8),
                    UserId = user.DriverId,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Message = "Rejected by Admin, Reupload your information",
                    ApprovalStatus = "Rejected",
                    StripeAccountId = user.StripeAccountId
                };
            }
            if (user.ApprovalStatus == "Pending")
            {
                return new JwtResponse
                {
                    Token = tokenHandler.WriteToken(token),
                    Expires = tokenDescriptor.Expires ?? DateTime.UtcNow.AddHours(8),
                    UserId = user.DriverId,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Message = "Not Approved by Admin,",
                    ApprovalStatus = "Pending",
                    StripeAccountId = user.StripeAccountId
                };
            }
            await _driverRepository.SetDriverAvailable(user.DriverId);
            return new JwtResponse
            {
                Token = tokenHandler.WriteToken(token),
                Expires = tokenDescriptor.Expires ?? DateTime.UtcNow.AddHours(8),
                UserId = user.DriverId,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                Message = "Login successful",
                ApprovalStatus = "Approved",
                StripeAccountId = user.StripeAccountId,
                RideStatus=user.Status
            };
        }

        public async Task<string> RegisterDriverAsync(RegisterRequest request)
        {
            var userExists = await _driverRepository.GetByEmailAsync(request.Email);
            //if (userExists != null)
            //{
            //    return "Email already registered";
            //}
            if (userExists != null && userExists.Verified == true)
                return "Email already registered";

            if (userExists != null)
            {
                if (userExists.Verified == null || userExists.Verified == false)
                {
                    await _driverRepository.DeleteUnverifiedByEmailAsync(request.Email);
                }
            }


            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new DriverModel
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.Phone,
                PasswordHash = hashedPassword,
                //ApprovalStatus = "Approved",
                //StripeAccountId= "acct_1SsJfaJpIouYWGuy",
                //Verified = true
                //UnComment the below line to set initial status to Rejected after app Approval
                ApprovalStatus = "Rejected"
            };

            await _driverRepository.CreateAsync(user);
            return "Driver registered successfully";
        }

        public Task<string> RejectDriver(Guid driverId, string RejectionReason)
        => _driverRepository.RejectDriver(driverId, RejectionReason);

        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await _driverRepository.GetByEmailAsync(email);
            if (user == null) return false;

            var hashed = BCrypt.Net.BCrypt.HashPassword(newPassword);
            return await _driverRepository.ResetPasswordAsync(user.DriverId, hashed);
        }

        public Task<string> SetDriverAvailable(Guid driverId)
            => _driverRepository.SetDriverAvailable(driverId);

        public Task<string> SetDriverOffline(Guid driverId)
            => _driverRepository.SetDriverOffline(driverId);

        public async Task<bool> UpdateDriverProfileAsync(UpdateDriverProfileRequest request, CancellationToken cancellationToken)
        {
            return await _driverRepository.UpdateProfile(request, cancellationToken);
        }

        public async Task<bool> UploadDriverImagesAsync(UploadDriverImagesRequest request, CancellationToken cancellationToken)
        {
            return await _driverRepository.UploadImages(request, cancellationToken);
        }

        public async Task<string> VerifyUser(string email, bool status)
        {
            try
            {
                await _driverRepository.Verified(email, status);
                return "Account Verified";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public Task<bool> DeleteDriverAsync(Guid driverId)
        {
            return _driverRepository.DeleteDriverAsync(driverId);
        }

        public Task<bool> UpdateStripeAccountIdAsync(Guid driverId, string stripeAccountId)
        {
            return _driverRepository.UpdateStripeAccountIdAsync(driverId, stripeAccountId);
        }
    }
}
