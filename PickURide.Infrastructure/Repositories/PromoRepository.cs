using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;

namespace PickURide.Infrastructure.Repositories
{
    public class PromoRepository : IPromoRepository
    {
        private readonly PickURideDbContext _context;

        public PromoRepository(PickURideDbContext context)
        {
            _context = context;
        }

        public async Task<PromoCodeModel?> GetByCodeAsync(string codeNormalized)
        {
            var entity = await _context.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == codeNormalized);

            if (entity == null) return null;

            return new PromoCodeModel
            {
                PromoCodeId = entity.PromoCodeId,
                Code = entity.Code,
                FlatAmount = entity.FlatAmount,
                MinFare = entity.MinFare,
                ExpiryUtc = entity.ExpiryUtc,
                IsActive = entity.IsActive,
                PerUserLimit = entity.PerUserLimit
            };
        }

        public Task<int> GetUserRedemptionCountAsync(Guid promoCodeId, Guid userId)
        {
            return _context.PromoRedemptions.CountAsync(r => r.PromoCodeId == promoCodeId && r.UserId == userId);
        }

        public async Task AddRedemptionAsync(Guid promoCodeId, Guid userId, Guid? rideId, decimal discountAmount)
        {
            var redemption = new PromoRedemption
            {
                PromoRedemptionId = Guid.NewGuid(),
                PromoCodeId = promoCodeId,
                UserId = userId,
                RideId = rideId,
                DiscountAmount = discountAmount,
                RedeemedAt = DateTime.UtcNow
            };

            await _context.PromoRedemptions.AddAsync(redemption);
            await _context.SaveChangesAsync();
        }
    }
}

