using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Services
{
    public interface IDriverLocationService
    {
        Task UpdateDriverLocationAsync(Guid driverId, double latitude, double longitude);
        Task<List<DriverLocationHistory>> GetLiveLocationsAsync();
        Task PersistCachedLocationsToDatabaseAsync();
        Task<bool> RemoveDriverLocationAsync(Guid driverId);
    }
}
