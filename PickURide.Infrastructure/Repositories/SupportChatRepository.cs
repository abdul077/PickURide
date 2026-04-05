using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;

namespace PickURide.Infrastructure.Repositories
{
    public class SupportChatRepository : ISupportChatRepository
    {
        private readonly PickURideDbContext _context;

        public SupportChatRepository(PickURideDbContext context)
        {
            _context = context;
        }

        public async Task<List<SupportChatModel>> GetChatHistoryAsync(Guid driverId, DateTime? before = null, int limit = 50)
        {
            var query = _context.SupportChats
                .Where(sc => sc.SenderId == driverId || sc.ReceiverId == driverId)
                .OrderByDescending(sc => sc.SentAt)
                .AsQueryable();

            if (before.HasValue)
            {
                query = query.Where(sc => sc.SentAt < before.Value);
            }

            var messages = await query
                .Take(limit)
                .OrderBy(sc => sc.SentAt)
                .Select(sc => new SupportChatModel
                {
                    ChatId = sc.ChatId,
                    SenderId = sc.SenderId,
                    ReceiverId = sc.ReceiverId,
                    Message = sc.Message,
                    SentAt = sc.SentAt,
                    SenderRole = sc.SenderRole
                })
                .ToListAsync();

            return messages;
        }

        public async Task<List<SupportChatModel>> GetChatHistoryBySenderAsync(Guid senderId, DateTime? before = null, int limit = 50)
        {
            var query = _context.SupportChats
                .Where(sc => sc.SenderId == senderId)
                .OrderByDescending(sc => sc.SentAt)
                .AsQueryable();

            if (before.HasValue)
            {
                query = query.Where(sc => sc.SentAt < before.Value);
            }

            var messages = await query
                .Take(limit)
                .OrderBy(sc => sc.SentAt)
                .Select(sc => new SupportChatModel
                {
                    ChatId = sc.ChatId,
                    SenderId = sc.SenderId,
                    ReceiverId = sc.ReceiverId,
                    Message = sc.Message,
                    SentAt = sc.SentAt,
                    SenderRole = sc.SenderRole
                })
                .ToListAsync();

            return messages;
        }
    }
}
