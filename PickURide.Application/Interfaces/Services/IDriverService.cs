using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services
{
    public interface IDriverService
    {
        Task<string> RegisterDriverAsync(RegisterRequest request);
        Task<JwtResponse> LoginDriverAsync(LoginRequest request);
        Task<string> VerifyUser(string email, bool status);
        Task<bool> UpdateDriverProfileAsync(UpdateDriverProfileRequest request, CancellationToken cancellationToken);
        Task<bool> UploadDriverImagesAsync(UploadDriverImagesRequest request, CancellationToken cancellationToken);
        Task<DriverModel?> GetDriverByEmailAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string newPassword);
        Task<IEnumerable<AllDriverDtos>> GetAllDriversAsync();
        Task<IEnumerable<AllDriverDtos>> GetApprovedDriversAsync();
        Task<IEnumerable<DriverAttendanceDto>> GetDriverAttendancesAsync(Guid driverId);
        Task<IEnumerable<DriverLocationHistoryDto>> GetDriverLocationHistoriesAsync(Guid driverId);
        Task<IEnumerable<DriverOvertimeDutyDto>> GetDriverOvertimeDutiesAsync(Guid driverId);
        Task<IEnumerable<DriverShiftApplicationDto>> GetDriverShiftApplicationsAsync(Guid driverId);
        Task<IEnumerable<DriverShiftDto>> GetDriverShiftsAsync(Guid driverId);
        Task<IEnumerable<DriverRideDto>> GetDriverRidesAsync(Guid driverId);
        Task<string> SetDriverAvailable(Guid driverId);
        Task<string> SetDriverOffline(Guid driverId);
        Task<string> RejectDriver(Guid driverId, string RejectionReason);
        Task<string> AcceptDriver(Guid driverId);
        Task<DriverDto?> GetDriverByIdAsync(Guid driverId);
        Task<bool> UpdateDriverDetailsAsync(UpdateDriverDetailsRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteDriverAsync(Guid driverId);
        Task<bool> UpdateStripeAccountIdAsync(Guid driverId, string stripeAccountId);
    }
}
