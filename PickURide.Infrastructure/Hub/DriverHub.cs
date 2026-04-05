using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;

namespace PickURide.Infrastructure.Hubs
{
    [AllowAnonymous]
    public class DriverHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly IDriverLocationService _locationService;

        public DriverHub(IDriverLocationService locationService)
        {
            _locationService = locationService;
        }

        //public async Task UpdateLocation(Guid driverId, double latitude, double longitude)
        //{
        //    await _locationService.UpdateDriverLocationAsync(driverId, latitude, longitude);

        //    // Optionally broadcast it to all clients
        //    await Clients.All.SendAsync("ReceiveLocation", new { driverId, latitude, longitude });
        //}
        public async Task UpdateLocation(Guid rideId, Guid driverId, double latitude, double longitude)
        {
            await _locationService.UpdateDriverLocationAsync(driverId, latitude, longitude);

            var payload = new
            {
                rideId,
                driverId,
                latitude,
                longitude,
                loggedAt = DateTime.UtcNow
            };

            // 1️⃣ Broadcast to ALL clients (global live map)
            await Clients.All.SendAsync("ReceiveLocation", payload);

            // 2️⃣ Send only to that ride group
            await Clients.Group(rideId.ToString())
                .SendAsync("ReceiveRideLocation", payload);
        }
        public async Task JoinRideGroup(Guid rideId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, rideId.ToString());
            await Clients.Caller.SendAsync("JoinedRideGroup", rideId);
        }

        // 🚘 Rider or Driver leaves ride-specific group
        public async Task LeaveRideGroup(Guid rideId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, rideId.ToString());
            await Clients.Caller.SendAsync("LeftRideGroup", rideId);
        }

    }
}
