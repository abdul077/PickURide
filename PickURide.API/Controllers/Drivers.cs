using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Drivers : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly IEmailOTPService _emailOtpService;
        private readonly IDriverLocationService _locationService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly IDriverRepository _driverRepository;

        public Drivers(IDriverService driverService, IEmailOTPService emailOtpService, IDriverLocationService locationService, ITokenBlacklistService tokenBlacklistService, IDriverRepository driverRepository)
        {
            _driverService = driverService;
            _emailOtpService = emailOtpService;
            _locationService = locationService;
            _tokenBlacklistService = tokenBlacklistService;
            _driverRepository = driverRepository;
        }
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _driverService.RegisterDriverAsync(request);
            if (result == "Email already registered")
            {
                return Ok(new { message = "Email already taken" });
            }
            else
            {
                var otp = new Random().Next(100000, 999999).ToString();
                await _emailOtpService.SendOtpAsync(request.Email, otp);
                return Ok(new { message = "User registered. OTP sent to email" });
            }
        }

        [HttpPost("fcm-token")]
        [Authorize]
        public async Task<IActionResult> UpdateFcmToken([FromBody] FcmTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FcmToken))
                return BadRequest(new { message = "FcmToken is required" });

            var driverIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(driverIdClaim, out var driverId))
                return Unauthorized();

            await _driverRepository.UpdateToken(driverId, request.FcmToken);
            return Ok();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }
                
                var result = await _driverService.LoginDriverAsync(request);
                
                // Return the result - it will contain the appropriate message
                // If login failed, the result will have an empty token and error message
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                return StatusCode(500, new { 
                    message = "An error occurred during login", 
                    error = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpPost("{driverId:guid}")]
        public async Task<IActionResult> GetDriverById(Guid driverId)
        {
            var driver = await _driverService.GetDriverByIdAsync(driverId);
            if (driver == null)
            {
                return NotFound(new { message = "Driver not found" });
            }

            return Ok(driver);
        }
        [HttpPost("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpRequest request)
        {
            var emailExists = await _driverService.GetDriverByEmailAsync(request.Email);
            
            if (emailExists.Verified == false || emailExists.Verified==null)
            {
                var valid = _emailOtpService.VerifyOtp(request.Email, request.Otp);
                if (valid == true)
                {
                    await _driverService.VerifyUser(request.Email, true);
                }
                return valid ? Ok(new { success = true }) : BadRequest(new { success = false, message = "OTP expired or invalid" });
            }
           else if(emailExists.Verified == true)
            {
                return BadRequest(new { success = false, message = "User already verified" });
            }
            return BadRequest(new { success = false, message = "User not found" });
        }
        [HttpPost("re-send-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] ResendOtpRequest request)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            await _emailOtpService.SendOtpAsync(request.Email, otp);
            return Ok(new { message = "OTP sent to email" });
        }
        [HttpPost("update-profile")]
        public async Task<IActionResult> UpdateDriverProfile([FromBody] UpdateDriverProfileRequest request)
        {
            if (request == null || request.Id == Guid.Empty)
                return BadRequest("Invalid request.");

            var result = await _driverService.UpdateDriverProfileAsync(request, CancellationToken.None);

            return result
                ? Ok(new { message = "Profile updated successfully" })
                : BadRequest("Driver not found.");
        }

        [HttpPost("upload-images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDriverImages([FromForm] UploadDriverImagesForm form)
        {
            if (form.DriverId == Guid.Empty)
                return BadRequest("Invalid Driver ID.");

            string ConvertToBase64(IFormFile file)
            {
                using var ms = new MemoryStream();
                file.CopyTo(ms);
                return Convert.ToBase64String(ms.ToArray());
            }

            var request = new UploadDriverImagesRequest
            {
                DriverId = form.DriverId,
                LicenseImageBase64 = form.LicenseImage != null ? ConvertToBase64(form.LicenseImage) : null,
                RegistrationImageBase64 = form.RegistrationImage != null ? ConvertToBase64(form.RegistrationImage) : null,
                InsuranceImageBase64 = form.InsuranceImage != null ? ConvertToBase64(form.InsuranceImage) : null,
                SelfieImageBase64 = form.SelfieImage != null ? ConvertToBase64(form.SelfieImage) : null,
            };

            var result = await _driverService.UploadDriverImagesAsync(request, CancellationToken.None);
            return result ? Ok("Images uploaded successfully.") : BadRequest("Driver not found or not verified.");
        }

        [HttpPost("update-details")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateDriverDetails([FromForm] UpdateDriverDetailsForm form)
        {
            if (form.DriverId == Guid.Empty)
                return BadRequest("Invalid Driver ID.");

            string? ConvertToBase64(IFormFile file)
            {
                using var ms = new MemoryStream();
                file.CopyTo(ms);
                return Convert.ToBase64String(ms.ToArray());
            }

            var request = new UpdateDriverDetailsRequest
            {
                DriverId = form.DriverId,
                FullName = form.FullName,
                PhoneNumber = form.PhoneNumber,
                Address = form.Address,
                LicenseNumber = form.LicenseNumber,
                CarLicensePlate = form.CarLicensePlate,
                CarVin = form.CarVin,
                CarRegistration = form.CarRegistration,
                CarInsurance = form.CarInsurance,
                Sin = form.Sin,
                VehicleName = form.VehicleName,
                VehicleColor = form.VehicleColor,
                StripeAccountId = form.StripeAccountId,
                LicenseImageBase64 = form.LicenseImage != null ? ConvertToBase64(form.LicenseImage) : null,
                RegistrationImageBase64 = form.RegistrationImage != null ? ConvertToBase64(form.RegistrationImage) : null,
                InsuranceImageBase64 = form.InsuranceImage != null ? ConvertToBase64(form.InsuranceImage) : null,
                SelfieImageBase64 = form.SelfieImage != null ? ConvertToBase64(form.SelfieImage) : null,
            };

            var updated = await _driverService.UpdateDriverDetailsAsync(request, CancellationToken.None);

            return updated ? Ok(new { message = "Driver details updated" }) : NotFound(new { message = "Driver not found" });
        }


        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordRequest request)
        {
            var user = await _driverService.GetDriverByEmailAsync(request.Email);
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

            var result = await _driverService.ResetPasswordAsync(request.Email, request.NewPassword);
            return result ? Ok("Password reset successful.") : BadRequest("Failed to reset password.");
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
                // Extract expiry and driver ID from JWT
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jwtToken != null)
                {
                    var expiry = jwtToken.ValidTo; // UTC time
                    await _tokenBlacklistService.AddAsync(token, expiry);

                    // Extract driver ID from claims and set driver offline
                    var driverIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    if (driverIdClaim != null && Guid.TryParse(driverIdClaim.Value, out var driverId))
                    {
                        await _driverService.SetDriverOffline(driverId);
                    }
                }
            }

            Response.Cookies.Delete("jwt");

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update(Guid driverId, double lat, double lng)
        {
            await _locationService.UpdateDriverLocationAsync(driverId, lat, lng);
            return Ok(new { message = "Location updated." });
        }

        [HttpPost("get-driver-locations-admin")]
        public async Task<IActionResult> GetLiveLocations()
        {
            var locations = await _locationService.GetLiveLocationsAsync();
            return Ok(locations);
        }
        [HttpPost("remove-driver-live-locations")]
        public async Task<IActionResult> RemoveDriverLiveLocation(Guid driverId)
        {
            var locations = await _locationService.RemoveDriverLocationAsync(driverId);
            return Ok(locations);
        }
        [HttpPost("get-all-drivers-admin")]
        public async Task<IActionResult> GetAllDrivers()
        {
            try
            {
                var result = await _driverService.GetAllDriversAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching drivers", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("get-approved-drivers-admin")]
        public async Task<IActionResult> GetApprovedDrivers()
        {
            var result = await _driverService.GetApprovedDriversAsync();
            return Ok(result);
        }

        [HttpPost("{driverId}/attendances-admin")]
        public async Task<IActionResult> GetDriverAttendances(Guid driverId)
        {
            var result = await _driverService.GetDriverAttendancesAsync(driverId);
            return Ok(result);
        }

        [HttpPost("{driverId}/locations-admin")]
        public async Task<IActionResult> GetDriverLocationHistories(Guid driverId)
        {
            var result = await _driverService.GetDriverLocationHistoriesAsync(driverId);
            return Ok(result);
        }

        [HttpPost("{driverId}/overtimeduties-admin")]
        public async Task<IActionResult> GetDriverOvertimeDuties(Guid driverId)
        {
            var result = await _driverService.GetDriverOvertimeDutiesAsync(driverId);
            return Ok(result);
        }

        [HttpPost("{driverId}/shiftapplications-admin")]
        public async Task<IActionResult> GetDriverShiftApplications(Guid driverId)
        {
            var result = await _driverService.GetDriverShiftApplicationsAsync(driverId);
            return Ok(result);
        }

        [HttpPost("{driverId}/shifts-admin")]
        public async Task<IActionResult> GetDriverShifts(Guid driverId)
        {
            var result = await _driverService.GetDriverShiftsAsync(driverId);
            return Ok(result);
        }

        [HttpPost("{driverId}/rides-admin")]
        public async Task<IActionResult> GetDriverRides(Guid driverId)
        {
            var result = await _driverService.GetDriverRidesAsync(driverId);
            return Ok(result);
        }
        [HttpPost("{email}/selfie-image")]
        public async Task<IActionResult> GetSelfieImage(string email)
        {
            var imageEntity = await _driverService.GetDriverByEmailAsync(email);
            if (imageEntity == null)
                return NotFound();

            var imageBytes = Convert.FromBase64String(imageEntity.SelfieImage);

            // You can detect type dynamically if stored, otherwise assume PNG
            return File(imageBytes, "image/png");
        }
        [HttpPost("{email}/insurance-image")]
        public async Task<IActionResult> GetInsuranceImage(string email)
        {
            var driver = await _driverService.GetDriverByEmailAsync(email);
            if (driver == null || string.IsNullOrEmpty(driver.InsuranceImage))
                return NotFound();

            var bytes = Convert.FromBase64String(driver.InsuranceImage);
            return File(bytes, "image/png");
        }

        [HttpPost("{email}/license-image")]
        public async Task<IActionResult> GetLicenseImage(string email)
        {
            var driver = await _driverService.GetDriverByEmailAsync(email);
            if (driver == null || string.IsNullOrEmpty(driver.LicenseImage))
                return NotFound();

            var bytes = Convert.FromBase64String(driver.LicenseImage);
            return File(bytes, "image/png");
        }

        [HttpPost("{email}/registration-image")]
        public async Task<IActionResult> GetRegistrationImage(string email)
        {
            var driver = await _driverService.GetDriverByEmailAsync(email);
            if (driver == null || string.IsNullOrEmpty(driver.RegistrationImage))
                return NotFound();

            var bytes = Convert.FromBase64String(driver.RegistrationImage);
            return File(bytes, "image/png");
        }
        [HttpPost("set-available-anonymous")]
        [AllowAnonymous]
        public async Task<IActionResult> SetDriverAvailableAnonymous([FromBody] SetDriverAvailableRequest request)
        {
            if (request == null || request.DriverId == Guid.Empty)
            {
                return BadRequest(new { message = "Driver ID is required" });
            }

            try
            {
                var result = await _driverService.SetDriverAvailable(request.DriverId);
                return Ok(new { message = result, success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "An error occurred while setting driver status", 
                    error = ex.Message,
                    success = false 
                });
            }
        }

        [HttpPost("set-available/{driverId}")]
        public async Task<IActionResult> SetDriverAvailable(Guid driverId)
        {
            var result = await _driverService.SetDriverAvailable(driverId);
            return Ok(new { message = result });
        }
        [HttpPost("set-offline/{driverId}")]
        public async Task<IActionResult> SetDriverOffline(Guid driverId)
        {
            var result = await _driverService.SetDriverOffline(driverId);
            return Ok(new { message = result });
        }
        [HttpPost("reject-driver-admin/{driverId}")]
        public async Task<IActionResult> RejectDriver(Guid driverId, string rejectionReason)
        {
            var result = await _driverService.RejectDriver(driverId, rejectionReason);
            return Ok(new { message = result });
        }
        [HttpPost("accept-driver-admin/{driverId}")]
        public async Task<IActionResult> AcceptDriver(Guid driverId)
        {
            var result = await _driverService.AcceptDriver(driverId);
            return Ok(new { message = result });
        }

        [HttpDelete("delete-driver-admin/{driverId}")]
        public async Task<IActionResult> DeleteDriver(Guid driverId)
        {
            var result = await _driverService.DeleteDriverAsync(driverId);
            if (result)
            {
                return Ok(new { message = "Driver deleted successfully" });
            }
            return NotFound(new { message = "Driver not found" });
        }

        [HttpPost("update-stripe-account-id")]
        public async Task<IActionResult> UpdateStripeAccountId([FromBody] UpdateStripeAccountIdRequest request)
        {
            if (request == null || request.DriverId == Guid.Empty || string.IsNullOrEmpty(request.StripeAccountId))
            {
                return BadRequest(new { message = "Driver ID and Stripe Account ID are required" });
            }

            var result = await _driverService.UpdateStripeAccountIdAsync(request.DriverId, request.StripeAccountId);
            
            if (result)
            {
                return Ok(new { message = "Stripe Account ID updated successfully" });
            }
            
            return NotFound(new { message = "Driver not found" });
        }

    }
    public class OtpRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
    public class ResendOtpRequest
    {
        public string Email { get; set; }
    }
    public class SetDriverAvailableRequest
    {
        public Guid DriverId { get; set; }
    }
    public class UpdateStripeAccountIdRequest
    {
        public Guid DriverId { get; set; }
        public string StripeAccountId { get; set; }
    }
}
