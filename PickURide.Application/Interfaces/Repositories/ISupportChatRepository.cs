using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface ISupportChatRepository
    {
        Task<List<SupportChatModel>> GetChatHistoryAsync(Guid driverId, DateTime? before = null, int limit = 50);
        Task<List<SupportChatModel>> GetChatHistoryBySenderAsync(Guid senderId, DateTime? before = null, int limit = 50);
    }
}
