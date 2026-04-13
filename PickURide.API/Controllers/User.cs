using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using PickURide.Infrastructure.Services;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PickURide.Application.Interfaces.Repositories;
using System.Security.Claims;
namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class User : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailOTPService _emailOtpService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IRideService _rideService;
        private readonly IPromoRepository _promoRepository;
        private readonly IUserRepository _userRepository;


        public User(IUserService userService, IEmailOTPService emailOtpService, ITokenBlacklistService tokenBlacklistService, IRideService rideService, IPromoRepository promoRepository, IUserRepository userRepository)
        {
            _userService = userService;
            _emailOtpService = emailOtpService;
            _tokenBlacklistService = tokenBlacklistService;
            _rideService = rideService;
            _promoRepository = promoRepository;
            _userRepository = userRepository;
        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _userService.RegisterUserAsync(request);
            if (result == "Email already registered")
            {
                return Ok(new { message = "Email already taken" });
            }
            else
            {
                var otp = new Random().Next(100000, 999999).ToString();
                await _emailOtpService.SendOtpAsync(request.Email, otp);
                return Ok(new { message = "OTP sent to email" });
            }
        }

        [HttpPost("fcm-token")]
        [Authorize]
        public async Task<IActionResult> UpdateFcmToken([FromBody] FcmTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FcmToken))
                return BadRequest(new { message = "FcmToken is required" });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            await _userRepository.UpdateToken(userId, request.FcmToken);
            return Ok();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _userService.LoginUserAsync(request);
            return Ok(result);
        }
        [HttpPost("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpRequest request)
        {
            var userExists = await _userService.GetUserByEmailAsync(request.Email);
            if (userExists.Verified == true)
            {
                return BadRequest(new { success = false, message = "User already verified" });
            }
            else if (userExists.Verified == false || userExists.Verified == null)
            {
                var valid = _emailOtpService.VerifyOtp(request.Email, request.Otp);
                if (valid == true)
                {
                    await _userService.VerifyUser(request.Email, true);
                }
                return valid ? Ok(new { success = true }) : BadRequest(new { success = false, message = "OTP expired or invalid" });
            }
            else
            {
                return BadRequest(new { success = false, message = "User not found" });
            }

        }
        [HttpPost("re-send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] ResendOtpRequest request)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            await _emailOtpService.SendOtpAsync(request.Email, otp);
            return Ok(new { message = "OTP sent to email" });
        }
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordRequest request)
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null)
                return BadRequest("Email not found.");

            var otp = new Random().Next(100000, 999999).ToString();
            await _emailOtpService.SendOtpAsync(request.Email, otp);

            return Ok(new { message = "OTP sent to email." });
        }
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var isValid = _emailOtpService.VerifyOtp(request.Email, request.Otp);
            if (!isValid)
                return BadRequest("Invalid or expired OTP.");

            var result = await _userService.ResetPasswordAsync(request.Email, request.NewPassword);
            return result ? Ok("Password reset successful.") : BadRequest("Failed to reset password.");
        }

        [HttpPost("get-all-users-admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("get-user-by-id-admin/{userId}")]
        public async Task<IActionResult> GetUserById(Guid userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        private FileStreamResult? ConvertBase64ToFile(string? base64Image)
        {
            if (string.IsNullOrEmpty(base64Image))
                return null;

            // Strip "data:image/...;base64," if present
            var base64Parts = base64Image.Split(',');
            var pureBase64 = base64Parts.Length > 1 ? base64Parts[1] : base64Parts[0];

            var imageBytes = Convert.FromBase64String(pureBase64);

            // detect mime type
            var mimeType = "application/octet-stream";
            if (base64Image.Contains("image/jpeg")) mimeType = "image/jpeg";
            else if (base64Image.Contains("image/png")) mimeType = "image/png";
            else if (base64Image.Contains("image/gif")) mimeType = "image/gif";

            var stream = new MemoryStream(imageBytes);
            return new FileStreamResult(stream, mimeType)
            {
                FileDownloadName = $"profile_{Guid.NewGuid()}.png"
            };
        }

        [HttpPost("single-users/{userId}")]
        public async Task<IActionResult> GetUser(Guid userId)
        {
            var user = await _userService.SingleUser(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                UserId = user.UserId,
                Name = user.FullName,
                PhoneNumber = user.PhoneNumber,
                HasImage = !string.IsNullOrEmpty(user.ProfileImage),
                ProfilePicture=user.ProfileImage
            });
        }

        [HttpPost("single-users/{userId}/image")]
        public async Task<IActionResult> GetUserImage(Guid userId)
        {
            var user = await _userService.SingleUser(userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(user.ProfileImage))
                return NotFound(new { message = "No profile image" });

            var base64 = user.ProfileImage;
            if (base64.Contains(",")) base64 = base64.Split(",")[1];

            var imageBytes = Convert.FromBase64String(base64);
            return File(imageBytes, "image/png", $"{user.FullName}_profile.png");
        }
        [HttpPost("update-user")]
        public async Task<IActionResult> UpdateUser([FromForm] UserProfile model)
        {
            if (model == null)
                return BadRequest(new { message = "Invalid request" });

            string? base64Image = null;

            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await model.ProfileImage.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();

                    // include mime type for frontend compatibility
                    var fileExt = Path.GetExtension(model.ProfileImage.FileName).ToLower();
                    string mimeType = fileExt switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        _ => "application/octet-stream"
                    };

                    base64Image = $"data:{mimeType};base64,{Convert.ToBase64String(fileBytes)}";
                }
            }

            var user = new UpdateUserModel
            {
                UserId = model.UserId,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                ProfileImage = base64Image
            };

            await _userService.UpdateUserAsync(user);

            return Ok(new { message = "User updated successfully" });
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            string? token = null;
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var bearer = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(bearer) && bearer.StartsWith("Bearer "))
                {
                    token = bearer.Substring("Bearer ".Length).Trim();
                }
            }
            else if (Request.Cookies.TryGetValue("jwt", out var cookieToken))
            {
                token = cookieToken;
            }

            if (!string.IsNullOrEmpty(token))
            {
                // Extract expiry from JWT
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken != null)
                {
                    var expiry = jwtToken.ValidTo; // UTC time
                    await _tokenBlacklistService.AddAsync(token, expiry);
                }
            }

            Response.Cookies.Delete("jwt");

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpDelete("delete-user-admin/{userId}")]
        public async Task<IActionResult> DeleteUser(Guid userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            if (result)
            {
                return Ok(new { message = "User deleted successfully" });
            }
            return NotFound(new { message = "User not found" });
        }

        [HttpPost("update-user-admin")]
        public async Task<IActionResult> UpdateUserAdmin([FromBody] UpdateUserAdminRequest request)
        {
            if (request == null || request.UserId == Guid.Empty)
                return BadRequest(new { message = "Invalid request" });

            // Get existing user to get email and profile image
            var existingUser = await _userService.GetUserByIdAsync(request.UserId);
            if (existingUser == null)
                return NotFound(new { message = "User not found" });

            // Get existing user model to preserve profile image if not provided
            var existingUserModel = await _userService.SingleUser(request.UserId);
            
            var user = new UpdateUserModel
            {
                UserId = request.UserId,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                ProfileImage = request.ProfileImage ?? existingUserModel?.ProfileImage // Preserve existing image if not provided
            };

            await _userService.UpdateUserAsync(user);

            // Update verified status if provided
            if (request.Verified.HasValue && existingUser.Email != null)
            {
                await _userService.VerifyUser(existingUser.Email, request.Verified.Value);
            }

            return Ok(new { message = "User updated successfully" });
        }

        [HttpPost("get-latest-ride/{userId}")]
        public async Task<IActionResult> GetLatestRide(Guid userId)
        {
            var latestRide = await _rideService.GetUserLastRide(userId);
            if (latestRide == null)
                return NotFound(new { message = "No ride found for this user" });

            // Cast to LastRideDto to access properties
            if (latestRide is LastRideDto rideDto)
            {
                object stops;
                if (rideDto.RideStops != null && rideDto.RideStops.Any())
                {
                    stops = rideDto.RideStops.OrderBy(s => s.StopOrder).Select(s => new
                    {
                        stopOrder = s.StopOrder ?? 0,
                        location = s.Location ?? string.Empty,
                        latitude = s.Latitude ?? 0.0,
                        longitude = s.Longitude ?? 0.0
                    }).ToList();
                }
                else
                {
                    stops = new List<object>();
                }

                var response = new
                {
                    userId = rideDto.UserId,
                    rideType = rideDto.RideType?.ToLower() ?? "standard",
                    isScheduled = rideDto.IsScheduled ?? false,
                    scheduledTime = rideDto.ScheduledTime,
                    passengerCount = rideDto.PassengerCount ?? 0,
                    fareEstimate = rideDto.FareEstimate ?? 0.0m,
                    paymentToken = rideDto.PaymentIntentId ?? string.Empty,
                    driverStripeAccountId = rideDto.DriverStripeAccountId ?? string.Empty,
                    stops = stops
                };

                return Ok(response);
            }

            return Ok(latestRide);
        }
        [HttpPost("verify-promo")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyPromo([FromBody] VerifyPromoRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PromoCode))
            {
                return BadRequest(new { success = false, message = "Promo code is required." });
            }

            var normalized = (request.PromoCode ?? string.Empty).Trim().ToUpperInvariant();
            var promo = await _promoRepository.GetByCodeAsync(normalized);
            if (promo == null)
            {
                return BadRequest(new { success = false, message = "Promo code is not valid." });
            }

            if (!promo.IsActive)
            {
                return BadRequest(new { success = false, message = "Promo code is inactive." });
            }

            if (promo.ExpiryUtc.HasValue && promo.ExpiryUtc.Value <= DateTime.UtcNow)
            {
                return BadRequest(new { success = false, message = "Promo code is expired." });
            }

            if (request.UserId.HasValue)
            {
                var usedCount = await _promoRepository.GetUserRedemptionCountAsync(promo.PromoCodeId, request.UserId.Value);
                if (usedCount >= promo.PerUserLimit)
                {
                    return BadRequest(new { success = false, message = "Promo code already used." });
                }
            }

            // Return the complete promo object
            return Ok(promo);
        }
        
    }

    public class UpdateUserAdminRequest
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool? Verified { get; set; }
        public string? ProfileImage { get; set; }
    }

    public class VerifyPromoRequest
    {
        public string PromoCode { get; set; } = string.Empty;
        public Guid? UserId { get; set; }
    }

}