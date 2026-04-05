
using PickURide.Application.Models;
using PickURide.Application.Models.AllRides;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IRideRepository
    {
        Task AddAsync(RideDto ride);
        Task<string> UpdateAsync(RideDto ride);
        Task<string> AssignDriverAsync(Guid rideId, Guid driverId);
        Task<RideDto?> GetByIdAsync(Guid rideId);
        Task<RideDto?> GetEntityByIdAsync(Guid rideId);
        Task<PaginatedResult<AllRidesDto>> GetAllRidesAsync(RidePaginationRequest request);
        Task<string> SetWaitingTime(Guid rideId, TimeOnly waitingTime, string status);
        Task<string> SetWaitingStatus(Guid rideId);
        Task<string> SetArrivedStatus(Guid rideId);
        Task<List<RideStopDto>> GetRideStops(Guid rideId);
        Task<RideDto> GetActiveRideByDriverIdAsync(Guid driverId);
        Task<List<RideDto>> GetScheduleRidesHistory(Guid driverId);
        Task<List<RideDto>> GetRidesHistory(Guid driverId);
        Task<RideHistoryResponse<UserRideHistory>> GetUserScheduleRidesHistory(Guid userId);
        Task<RideHistoryResponse<UserRideHistory>> GetUserRidesHistory(Guid userId);
        Task<List<UserRideHistory>> GetUserCompletedRidesHistory(Guid userId);
        Task<object> GetDriverLastRide(Guid driverId);
        Task<object> GetUserLastRide(Guid userId);
        Task<string> CancelRideAsync(Guid rideId);
        Task<Dictionary<string, int>> GetRideStatusCountsAsync(string? filterPeriod, bool? isScheduledFilter = null);
    }
}
