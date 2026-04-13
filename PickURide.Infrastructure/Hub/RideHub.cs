using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Services;

namespace PickURide.Infrastructure.Hub
{
    public class RideHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly IDriverLocationService _locationService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RideHub> _logger;
        private readonly IPushNotificationService _pushNotification;
        private readonly IUserRepository _userRepository;
        private readonly IDriverRepository _driverRepository;

        public RideHub(
            IDriverLocationService locationService,
            IMemoryCache cache,
            ILogger<RideHub> logger,
            IPushNotificationService pushNotification,
            IUserRepository userRepository,
            IDriverRepository driverRepository)
        {
            _locationService = locationService;
            _cache = cache;
            _logger = logger;
            _pushNotification = pushNotification;
            _userRepository = userRepository;
            _driverRepository = driverRepository;
        }
        public async Task SubscribeToRide(Guid rideId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, rideId.ToString());
        }

        public async Task UnsubscribeFromRide(Guid rideId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, rideId.ToString());
        }

        public async Task SubscribeDriver(Guid driverId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, driverId.ToString());
        }

        public async Task UnsubscribeDriver(Guid driverId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, driverId.ToString());
        }
        public async Task UpdateLocation(Guid rideId, Guid driverId,string driverName, double latitude, double longitude)
        {
            await _locationService.UpdateDriverLocationAsync(driverId, latitude, longitude);

            var payload = new
            {
                rideId,
                driverId,
                driverName,
                latitude,
                longitude,
                loggedAt = DateTime.UtcNow
            };
            await Clients.All.SendAsync("ReceiveAllLocation", payload);

            // Broadcast to users subscribed to this ride
            await Clients.Group(rideId.ToString())
                .SendAsync("ReceiveRideLocation", payload);
        }
        public async Task GetLiveLocations()
        {
            var locations = await _locationService.GetLiveLocationsAsync();
            var response = locations.Select(l => new
            {
                driverId = l.DriverId,
                latitude = l.Latitude,
                longitude = l.Longitude,
                loggedAt = l.LoggedAt
            });

            await Clients.Caller.SendAsync("ReceiveLiveLocations", response);
        }

        public async Task<List<DriverLocationHistory>> GetNearbyDrivers(double userLat, double userLng, double radiusKm = 5)
        {
            var allDrivers = _cache.Get<List<DriverLocationHistory>>("LiveDriverLocations") ?? new List<DriverLocationHistory>();

            // Find nearby drivers (assuming all are available)
            var nearbyDrivers = allDrivers
                .Where(d => d.Latitude.HasValue && d.Longitude.HasValue &&
                            GetDistance(userLat, userLng, d.Latitude.Value, d.Longitude.Value) <= radiusKm)
                .ToList();

            _logger.LogInformation($"Found {nearbyDrivers.Count} nearby drivers within {radiusKm} km radius.");
            return nearbyDrivers;
        }
        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        //Payment
        public async Task SendPaymentSuccess(string driverId, string userId, string message, decimal tipAmount, decimal totalAmount)
        {
            var payload = new
            {
                Message = message,
                TipAmount = tipAmount,
                TotalAmount = totalAmount
            };

            await Clients.Group(driverId).SendAsync("ReceivePaymentSuccess", payload);
            await Clients.Group(userId).SendAsync("ReceivePaymentSuccess", payload);

            if (Guid.TryParse(driverId, out var driverGuid))
            {
                var driverToken = await _driverRepository.GetDeviceTokenAsync(driverGuid);
                if (!string.IsNullOrWhiteSpace(driverToken))
                {
                    var tipStr = tipAmount.ToString(CultureInfo.InvariantCulture);
                    var totalStr = totalAmount.ToString(CultureInfo.InvariantCulture);
                    await _pushNotification.SendToTokenAsync(
                        driverToken,
                        "Payment received",
                        $"Total: {totalStr} | Tip: {tipStr}",
                        new Dictionary<string, string>
                        {
                            { "type", "payment_success" },
                            { "userId", userId }
                        });
                }
            }

            if (Guid.TryParse(userId, out var userGuid))
            {
                var userToken = await _userRepository.GetDeviceTokenAsync(userGuid);
                if (!string.IsNullOrWhiteSpace(userToken))
                {
                    await _pushNotification.SendToTokenAsync(
                        userToken,
                        "Payment done",
                        message,
                        new Dictionary<string, string>
                        {
                            { "type", "payment_success" },
                            { "driverId", driverId }
                        });
                }
            }
        }

        // Optional: Join group by user type/id
        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnConnectedAsync();
        }
    }
}
