using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IFeedbackRepository
    {
        Task AddAsync(FeedbackDto feedback);
        Task<double> GetAverageRatingByDriverAsync(Guid driverId);
        Task<double> GetAverageRatingByUserIdAsync(Guid userId);
        Task<IEnumerable<FeedbackDto>> GetByDriverAsync(Guid driverId);
        Task<IEnumerable<FeedbackDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<FeedbackDto>> GetByRideAsync(Guid rideId);
        Task<IEnumerable<FeedbackDto>> GetAllAsync();
    }
}
