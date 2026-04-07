using PickURide.Application.Models;
using PickURide.Application.Models.AllRides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services
{
    public interface IRideService
    {
        Task<object> BookRideAsync(BookRideRequest request);
        Task<RideDto?> GetRideByIdAsync(Guid rideId);
        Task<object> EndRideAsync(Guid rideId);
        Task<string> StartRideAsync(Guid rideId);
        Task<PaginatedResult<AllRidesDto>> GetAllRidesAsync(RidePaginationRequest request);
        Task<string> AssignDriverAsync(Guid rideId, Guid driverId);
        Task<object> SetWaitingTimeAsync(Guid rideId, TimeOnly waitingTime,string status);
        Task<string> SetWaitingStatusAsync(Guid rideId);
        Task<string> SetArrivedStatusAsync(Guid rideId);
        Task<object> FareEstimate(string Address, decimal distance, string duration, Guid? userId = null, string? promoCode = null);
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
