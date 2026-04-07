using Microsoft.Extensions.Caching.Memory;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PickURide.Infrastructure.Services
{
    public class RideChatCacheService : IRideChatCacheService
    {
        private readonly IMemoryCache _cache;
        private const string RideChatKeyPrefix = "RideChat_";
        private static readonly ConcurrentDictionary<Guid, List<ChatMessageDto>> DriverChatCache = new();
        private static readonly ConcurrentDictionary<Guid, object> _locks = new();
        private static readonly ConcurrentDictionary<Guid, long> _rideNextSequence = new();

        public RideChatCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void SaveMessage(ChatMessageDto message)
        {
            // Validate message first
            if (message == null)
                return;

            if (message.RideId == Guid.Empty)
                return;

            if (string.IsNullOrWhiteSpace(message.Message))
                return;

            var key = RideChatKeyPrefix + message.RideId;
            
            // Get or create a lock object for this specific ride
            var lockObject = _locks.GetOrAdd(message.RideId, _ => new object());
            
            // Thread-safe approach: lock on the specific ride's lock object
            lock (lockObject)
            {
                var messages = _cache.GetOrCreate(key, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    return new List<ChatMessageDto>();
                });

                if (messages != null)
                {
                    var next = _rideNextSequence.AddOrUpdate(message.RideId, 1L, (_, s) => s + 1);
                    message.Sequence = next;

                    messages.Add(message);
                    // Explicitly set the cache entry to ensure it's updated
                    _cache.Set(key, messages, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    });
                }
            }
        }

        public List<ChatMessageDto> GetMessages(Guid rideId)
        {
            return _cache.TryGetValue(RideChatKeyPrefix + rideId, out List<ChatMessageDto> messages)
                ? messages.OrderBy(m => m.Sequence).ThenBy(m => m.SentAt).ToList()
                : new List<ChatMessageDto>();
        }

        public List<ChatMessageDto> GetMessagesAfter(Guid rideId, long afterSequence)
        {
            var lockObject = _locks.GetOrAdd(rideId, _ => new object());
            lock (lockObject)
            {
                if (!_cache.TryGetValue(RideChatKeyPrefix + rideId, out List<ChatMessageDto> messages) || messages == null)
                    return new List<ChatMessageDto>();

                return messages
                    .Where(m => m.Sequence > afterSequence)
                    .OrderBy(m => m.Sequence)
                    .ThenBy(m => m.SentAt)
                    .Select(m => new ChatMessageDto
                    {
                        RideId = m.RideId,
                        DriverId = m.DriverId,
                        SenderId = m.SenderId,
                        SenderRole = m.SenderRole,
                        Message = m.Message,
                        SentAt = m.SentAt,
                        ChatType = m.ChatType,
                        Sequence = m.Sequence
                    })
                    .ToList();
            }
        }

        public void ClearMessages(Guid rideId)
        {
            _cache.Remove(RideChatKeyPrefix + rideId);
            _rideNextSequence.TryRemove(rideId, out _);
        }

        public void SaveDriverAdminMessage(Guid driverId, ChatMessageDto message)
        {
            var history = DriverChatCache.GetOrAdd(driverId, _ => new List<ChatMessageDto>());
            lock (history)
            {
                history.Add(message);
            }
        }

        public List<ChatMessageDto> GetDriverAdminMessages(Guid driverId)
        {
            if (DriverChatCache.TryGetValue(driverId, out var messages))
            {
                lock (messages)
                {
                    return messages
                        .OrderBy(m => m.SentAt)
                        .Select(m => new ChatMessageDto
                        {
                            DriverId = m.DriverId,
                            RideId = m.RideId,
                            SenderId = m.SenderId,
                            SenderRole = m.SenderRole,
                            Message = m.Message,
                            SentAt = m.SentAt,
                            ChatType = m.ChatType
                        })
                        .ToList();
                }
            }

            return new List<ChatMessageDto>();
        }

        public void ClearDriverAdminMessages(Guid driverId)
        {
            DriverChatCache.TryRemove(driverId, out _);
        }

        public IEnumerable<Guid> GetDriverAdminConversationIds()
        {
            return DriverChatCache.Keys.ToArray();
        }
    }
}
