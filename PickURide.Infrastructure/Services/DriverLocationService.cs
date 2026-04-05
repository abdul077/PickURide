using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using System.Collections.Concurrent;

namespace PickURide.Infrastructure.Services
{
    public class DriverLocationService : IDriverLocationService
    {
        private static readonly ConcurrentDictionary<Guid, DriverLocationHistory> _locationCache = new();
        private readonly PickURideDbContext _context;

        public DriverLocationService(PickURideDbContext context)
        {
            _context = context;
        }

        public Task UpdateDriverLocationAsync(Guid driverId, double latitude, double longitude)
        {
            var location = new DriverLocationHistory
            {
                DriverId = driverId,
                Latitude = latitude,
                Longitude = longitude,
                LoggedAt = DateTime.UtcNow
            };

            _locationCache[driverId] = location;
            return Task.CompletedTask;
        }

        public Task<List<Application.Models.DriverLocationHistory>> GetLiveLocationsAsync()
        {
            return Task.FromResult(_locationCache.Values.Select(x => new Application.Models.DriverLocationHistory
            {
                DriverId = x.DriverId,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                LoggedAt = x.LoggedAt
            }).ToList());
        }

        public async Task PersistCachedLocationsToDatabaseAsync()
        {
            var now = DateTime.UtcNow;

            var entries = _locationCache.Values
                .Where(x => (now - x.LoggedAt).TotalMinutes <= 5)
                .Select(x => new Infrastructure.Data.Entities.DriverLocationHistory
                {
                    DriverId = x.DriverId,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    LoggedAt = x.LoggedAt
                }).ToList();

            if (entries.Any())
            {
                await _context.DriverLocationHistories.AddRangeAsync(entries);
                await _context.SaveChangesAsync();
            }

            // Do NOT clear the cache completely. You can remove old entries if needed:
            var expiredKeys = _locationCache
                .Where(x => (now - x.Value.LoggedAt).TotalMinutes > 10)
                .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _locationCache.TryRemove(key, out _);
            }
        }
        // Add this method to DriverLocationService
        public Task<bool> RemoveDriverLocationAsync(Guid driverId)
        {
            return Task.FromResult(_locationCache.TryRemove(driverId, out _));
        }
    }
}
