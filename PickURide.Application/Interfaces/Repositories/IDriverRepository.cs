using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IDriverRepository
    {
        Task<DriverModel?> GetByEmailAsync(string email);
        Task CreateAsync(DriverModel driver);
        Task UpdateToken(Guid Id, string Token);
        Task Verified(string email, bool status);
        Task<bool> UpdateProfile(UpdateDriverProfileRequest request, CancellationToken cancellation);
        Task<bool> UploadImages(UploadDriverImagesRequest request,CancellationToken cancellation);
        Task<DriverModel?> GetDriverByEmailAsync(string email);
        Task<bool> ResetPasswordAsync(Guid Id, string newPassword);
        Task<DriverDto?> GetByIdAsync(Guid driverId);
        Task<DriverDto?> GetAvailableDriverAsync();
        Task<List<DriverDto>> GetAllAvailableDriversAsync();
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
        Task DeleteUnverifiedByEmailAsync(string Email);
        Task<bool> UpdateDriverDetailsAsync(UpdateDriverDetailsRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteDriverAsync(Guid driverId);
        Task<bool> UpdateStripeAccountIdAsync(Guid driverId, string stripeAccountId);
        Task<string?> GetDeviceTokenAsync(Guid driverId);

    }
}
