using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PickURide.Infrastructure.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly PickURideDbContext _context;

        public FeedbackRepository(PickURideDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(FeedbackDto feedback)
        {
            Feedback feedbackEntity = new Feedback
            {
                FeedbackId = Guid.NewGuid(),
                UserId = feedback.UserId,
                RideId = feedback.RideId,
                DriverId = feedback.DriverId,
                Rating = feedback.Rating,
                Comments = feedback.Comments,
                CreatedAt = DateTime.UtcNow,
                FeedbackFrom = feedback.FeedbackFrom
            };
            
            await _context.Feedbacks.AddAsync(feedbackEntity);
            await _context.SaveChangesAsync();
        }

        public async Task<double> GetAverageRatingByDriverAsync(Guid driverId)
        {
            var ratings = await _context.Feedbacks
                .Where(f => f.DriverId == driverId && f.Rating.HasValue && f.FeedbackFrom == "User")
                .Select(f => f.Rating!.Value)
                .ToListAsync();

            if (ratings == null || !ratings.Any())
            {
                return 0.0;
            }

            return (double)ratings.Average();
        }

        public async Task<double> GetAverageRatingByUserIdAsync(Guid userId)
        {
            var ratings = await _context.Feedbacks
                .Where(f => f.UserId == userId && f.Rating.HasValue && f.FeedbackFrom == "Driver")
                .Select(f => f.Rating!.Value)
                .ToListAsync();

            if (ratings == null || !ratings.Any())
            {
                return 0.0;
            }

            return (double)ratings.Average();
        }

        public async Task<IEnumerable<FeedbackDto>> GetByDriverAsync(Guid driverId)
        {
            return await _context.Feedbacks
                .Where(f => f.DriverId == driverId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    RideId = f.RideId ?? Guid.Empty,
                    UserId = f.UserId ?? Guid.Empty,
                    DriverId = f.DriverId ?? Guid.Empty,
                    Rating = f.Rating ?? 0,
                    Comments = f.Comments,
                    CreatedAt = f.CreatedAt ?? DateTime.MinValue,
                    FeedbackFrom = f.FeedbackFrom ?? string.Empty,
                    DriverName = f.Driver != null ? f.Driver.FullName : null,
                    UserName = f.User != null ? f.User.FullName : null
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FeedbackDto>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Feedbacks
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    RideId = f.RideId ?? Guid.Empty,
                    UserId = f.UserId ?? Guid.Empty,
                    DriverId = f.DriverId ?? Guid.Empty,
                    Rating = f.Rating ?? 0,
                    Comments = f.Comments,
                    CreatedAt = f.CreatedAt ?? DateTime.MinValue,
                    FeedbackFrom = f.FeedbackFrom ?? string.Empty,
                    DriverName = f.Driver != null ? f.Driver.FullName : null,
                    UserName = f.User != null ? f.User.FullName : null
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FeedbackDto>> GetByRideAsync(Guid rideId)
        {
            return await _context.Feedbacks
                .Where(f => f.RideId == rideId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    RideId = f.RideId ?? Guid.Empty,
                    UserId = f.UserId ?? Guid.Empty,
                    DriverId = f.DriverId ?? Guid.Empty,
                    Rating = f.Rating ?? 0,
                    Comments = f.Comments,
                    CreatedAt = f.CreatedAt ?? DateTime.MinValue,
                    FeedbackFrom = f.FeedbackFrom ?? string.Empty,
                    DriverName = f.Driver != null ? f.Driver.FullName : null,
                    UserName = f.User != null ? f.User.FullName : null
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<FeedbackDto>> GetAllAsync()
        {
            return await _context.Feedbacks
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    RideId = f.RideId ?? Guid.Empty,
                    UserId = f.UserId ?? Guid.Empty,
                    DriverId = f.DriverId ?? Guid.Empty,
                    Rating = f.Rating ?? 0,
                    Comments = f.Comments,
                    CreatedAt = f.CreatedAt ?? DateTime.MinValue,
                    FeedbackFrom = f.FeedbackFrom ?? string.Empty,
                    DriverName = f.Driver != null ? f.Driver.FullName : null,
                    UserName = f.User != null ? f.User.FullName : null
                })
                .ToListAsync();
        }
    }
}
