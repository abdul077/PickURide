using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services
{
    public interface IFareSettingService
    {
        Task<decimal> GetBaseFareAsync();
        Task<decimal> GetPerKmRateAsync();
        Task<decimal> GetPerMinuteRateAsync();
        Task<decimal> GetMinimumFareAsync();
        Task UpdateBaseFareAsync(decimal baseFare);
        Task UpdatePerKmRateAsync(decimal perKmRate);
        Task UpdatePerMinuteRateAsync(decimal perMinuteRate);
        Task UpdateMinimumFareAsync(decimal minimumFare);
        Task<decimal> CalculateFareAsync(decimal distance, TimeSpan duration);
        Task<string> CreateAsync(decimal baseFare, decimal perKmRate, decimal perMinuteRate, decimal AdminCommision, string Area);
        Task<string> UpdateAsync(int settingsId, decimal baseFare, decimal perKmRate, decimal perMinuteRate, decimal AdminCommission, string Area);
        Task<decimal> GetFareByIdAsync(int fareSettingId);
        Task<string> DeleteAsync(int fareSettingId);
        Task<List<FareSettings>> GetAllFareSettingsAsync();

        Task<List<FareSettings>> GetAllFareSettingsWithSlabsAsync();
        Task<string> CreateWithSlabsAsync(FareSettingUpsertRequest request);
        Task<string> UpdateWithSlabsAsync(FareSettingUpsertRequest request);
    }
}
