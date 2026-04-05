using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
 
namespace PickURide.Infrastructure.Hub
{
    public class RideChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private const string AdminGroup = "DriverSupport_Admins";
        private readonly IRideChatCacheService _chatCacheService;
        private readonly IDriverRepository _driverRepository;
        private readonly IDriverLocationService _locationService;
        private readonly ILogger<RideChatHub> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;
        private static Timer _flushTimer;
        private static readonly object _timerLock = new object();
        private static bool _timerInitialized = false;

        public RideChatHub(
            IRideChatCacheService chatCacheService, 
            IDriverRepository driverRepository,
            IDriverLocationService locationService,
            ILogger<RideChatHub> logger,
            IServiceProvider serviceProvider,
            IMemoryCache cache)
        {
            _chatCacheService = chatCacheService;
            _driverRepository = driverRepository;
            _locationService = locationService;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _cache = cache;
            
            // Initialize the flush timer once
            InitializeFlushTimer();
        }

        private void InitializeFlushTimer()
        {
            lock (_timerLock)
            {
                if (!_timerInitialized)
                {
                    _logger.LogInformation("RideChatHub location flush timer started at {time}", DateTime.UtcNow);
                    _flushTimer = new Timer(async _ => await FlushLocationsAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                    _timerInitialized = true;
                }
            }
        }

        private async Task FlushLocationsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IDriverLocationService>();
                await service.PersistCachedLocationsToDatabaseAsync();
                _logger.LogInformation("Flushed live driver locations to DB from RideChatHub.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while flushing driver locations to DB from RideChatHub");
            }
        }
        public async Task SendMessage(Guid rideId, Guid senderId, string message, string senderRole = "Rider")
        {
            var chatMessage = new ChatMessageDto
            {
                RideId = rideId,
                SenderId = senderId,
                SenderRole = senderRole,
                Message = message,
                SentAt = DateTime.UtcNow,
                ChatType = "Ride"
            };

            // ✅ Save full message in cache
            _chatCacheService.SaveMessage(chatMessage);

            // ✅ Broadcast only the needed fields
            var response = new
            {
                SenderId = chatMessage.SenderId,
                SenderRole = chatMessage.SenderRole,
                Message = chatMessage.Message,
                DateTime = chatMessage.SentAt
            };

            await Clients.Group(rideId.ToString()).SendAsync("ReceiveMessage", response);
        }

        public async Task JoinRideChat(Guid rideId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, rideId.ToString());
        }

        public async Task LeaveRideChat(Guid rideId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, rideId.ToString());
        }
        // ✅ Return chat history for a ride
        public async Task GetRideChatHistory(Guid rideId)
        {
            // Pull from your cache service
            var messages = _chatCacheService.GetMessages(rideId);

            // Return only the fields needed by the client
            var response = messages.Select(m => new
            {
                SenderId = m.SenderId,
                SenderRole = m.SenderRole,
                Message = m.Message,
                DateTime = m.SentAt
            });

            // Send back just to the caller
            await Clients.Caller.SendAsync("ReceiveRideChatHistory", response);
        }

        public async Task JoinAdminDashboard()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
            await SendDriverConversationListToCaller();
        }

        public async Task JoinDriverSupport(Guid driverId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, DriverSupportGroup(driverId));
        }

        public async Task LeaveDriverSupport(Guid driverId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DriverSupportGroup(driverId));
        }

        public async Task SendDriverAdminMessage(Guid driverId, Guid senderId, string senderRole, string message)
        {
            var chatMessage = new ChatMessageDto
            {
                DriverId = driverId,
                SenderId = senderId,
                SenderRole = senderRole,
                Message = message,
                SentAt = DateTime.UtcNow,
                ChatType = "DriverAdmin"
            };

            _chatCacheService.SaveDriverAdminMessage(driverId, chatMessage);

            // Save to database for persistence
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var supportChat = new SupportChat
                {
                    ChatId = Guid.NewGuid(),
                    SenderId = senderId,
                    ReceiverId = driverId,
                    Message = message,
                    SentAt = chatMessage.SentAt,
                    SenderRole = senderRole
                };

                // Add to context and save
                var context = scope.ServiceProvider.GetRequiredService<PickURideDbContext>();
                await context.SupportChats.AddAsync(supportChat);
                await context.SaveChangesAsync();

                _logger.LogInformation("Saved support chat message to database: {ChatId}", supportChat.ChatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save support chat message to database");
                // Continue with real-time functionality even if DB save fails
            }

            var driver = await _driverRepository.GetByIdAsync(driverId);
            var driverName = driver?.FullName ?? "Unknown Driver";

            var payload = new
            {
                DriverId = driverId,
                DriverName = driverName,
                SenderId = chatMessage.SenderId,
                SenderRole = chatMessage.SenderRole,
                Message = chatMessage.Message,
                DateTime = chatMessage.SentAt
            };

            await Clients.Group(DriverSupportGroup(driverId)).SendAsync("ReceiveDriverAdminMessage", payload);
            await BroadcastDriverAdminHistory(driverId, driverName);
            await BroadcastConversationSummary(driverId, driverName);

            await Clients.Group(AdminGroup).SendAsync("DriverAdminMessageSummary", new
            {
                DriverId = driverId,
                DriverName = driverName,
                LastMessage = chatMessage.Message,
                LastMessageAt = chatMessage.SentAt
            });
        }

        public async Task GetDriverAdminChatHistory(Guid driverId)
        {
            var driver = await _driverRepository.GetByIdAsync(driverId);
            var driverName = driver?.FullName ?? "Unknown Driver";
            var history = _chatCacheService
                .GetDriverAdminMessages(driverId)
                .Select(m => new
                {
                    DriverId = driverId,
                    DriverName = driverName,
                    SenderId = m.SenderId,
                    SenderRole = m.SenderRole,
                    Message = m.Message,
                    DateTime = m.SentAt
                });

            await Clients.Caller.SendAsync("ReceiveDriverAdminChatHistory", history);
        }

        public async Task GetDriverAdminConversationList()
        {
            await SendDriverConversationListToCaller();
        }

        private async Task SendDriverConversationListToCaller()
        {
            var conversationIds = _chatCacheService.GetDriverAdminConversationIds();
            var summaries = new List<object>();

            foreach (var driverId in conversationIds)
            {
                var driver = await _driverRepository.GetByIdAsync(driverId);
                var driverName = driver?.FullName ?? "Unknown Driver";
                var history = _chatCacheService.GetDriverAdminMessages(driverId);
                var lastMessage = history.OrderByDescending(m => m.SentAt).FirstOrDefault();

                summaries.Add(new
                {
                    DriverId = driverId,
                    DriverName = driverName,
                    LastMessage = lastMessage?.Message,
                    LastMessageAt = lastMessage?.SentAt
                });
            }

            await Clients.Caller.SendAsync("ReceiveDriverAdminConversationList", summaries);
        }

        private static string DriverSupportGroup(Guid driverId) => $"DriverSupport_{driverId}";

        private async Task BroadcastDriverAdminHistory(Guid driverId, string driverName)
        {
            var history = _chatCacheService
                .GetDriverAdminMessages(driverId)
                .Select(m => new
                {
                    DriverId = driverId,
                    DriverName = driverName,
                    SenderId = m.SenderId,
                    SenderRole = m.SenderRole,
                    Message = m.Message,
                    DateTime = m.SentAt
                })
                .ToList();

            await Clients.Group(DriverSupportGroup(driverId)).SendAsync("ReceiveDriverAdminChatHistory", history);
        }

        private async Task BroadcastConversationSummary(Guid driverId, string driverName)
        {
            var history = _chatCacheService.GetDriverAdminMessages(driverId);
            var lastMessage = history.OrderByDescending(m => m.SentAt).FirstOrDefault();

            var summary = new
            {
                DriverId = driverId,
                DriverName = driverName,
                LastMessage = lastMessage?.Message,
                LastMessageAt = lastMessage?.SentAt
            };

            await Clients.Group(AdminGroup).SendAsync("ReceiveDriverAdminConversationSummary", summary);
            await Clients.Group(DriverSupportGroup(driverId)).SendAsync("ReceiveDriverAdminConversationSummary", summary);
        }

        // ✅ Location update methods (copied from DriverLocationFlushService functionality)
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

            // Broadcast to ALL clients (global live map)
            await Clients.All.SendAsync("ReceiveLocation", payload);

            // Send only to that ride group
            await Clients.Group(rideId.ToString())
                .SendAsync("ReceiveRideLocation", payload);
        }

        public async Task UpdateLocationWithDriverName(Guid rideId, Guid driverId, string driverName, double latitude, double longitude)
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

            // Broadcast to ALL clients
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

        public async Task JoinLocationGroup(Guid rideId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, rideId.ToString());
            await Clients.Caller.SendAsync("JoinedLocationGroup", rideId);
        }

        public async Task LeaveLocationGroup(Guid rideId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, rideId.ToString());
            await Clients.Caller.SendAsync("LeftLocationGroup", rideId);
        }

        // ✅ Methods copied from RideHub
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

        public async Task<List<PickURide.Application.Models.DriverLocationHistory>> GetNearbyDrivers(double userLat, double userLng, double radiusKm = 5)
        {
            var allDrivers = _cache.Get<List<PickURide.Application.Models.DriverLocationHistory>>("LiveDriverLocations") ?? new List<PickURide.Application.Models.DriverLocationHistory>();

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

        // Payment
        public async Task SendPaymentSuccess(string driverId, string userId, string message, decimal tipAmount, decimal totalAmount)
        {
            // Notify the driver
            await Clients.Group(driverId).SendAsync("ReceivePaymentSuccess", new
            {
                Message = message,
                TipAmount = tipAmount,
                TotalAmount = totalAmount
            });

            // Notify the user
            await Clients.Group(userId).SendAsync("ReceivePaymentSuccess", new
            {
                Message = message,
                TipAmount = tipAmount,
                TotalAmount = totalAmount
            });
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
