using System;
using System.Threading.Tasks;
using PickURide.Application.Models;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IPromoRepository
    {
        Task<PromoCodeModel?> GetByCodeAsync(string codeNormalized);
        Task<int> GetUserRedemptionCountAsync(Guid promoCodeId, Guid userId);
        Task AddRedemptionAsync(Guid promoCodeId, Guid userId, Guid? rideId, decimal discountAmount);
    }
}

