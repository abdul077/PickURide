using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;

namespace PickURide.Infrastructure.Repositories
{
    public class RideMessageRepository : IRideMessageRepository
    {
        private readonly PickURideDbContext _context;
        public RideMessageRepository(PickURideDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(List<SaveRideMessageDto> messages)
        {
            var entities = messages.Select(m => new RideMessage
            {
                MessageId = Guid.NewGuid(),
                RideId = m.RideId,
                SenderId = m.SenderId,
                SenderRole = m.SenderRole,
                Message = m.Message,
                SentAt = m.SentAt
            }).ToList();

            await _context.RideMessages.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
    }
}
