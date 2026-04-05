using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface ITipRepository
    {
        Task<string> AddAsync(TipDto tip);
        Task<string> GetTipbyRideId(Guid rideId);
    }
}
