using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Services
{
    public class FareSettingService : IFareSettingService
    {
        private readonly IFareSettingRepository _fareSettings;
        public FareSettingService(IFareSettingRepository fareSettings)
        {
            _fareSettings = fareSettings;
        }
        public Task<decimal> CalculateFareAsync(decimal distance, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateAsync(decimal baseFare, decimal perKmRate, decimal perMinuteRate, decimal AdminCommission, string Area)
        {
            return _fareSettings.CreateAsync(baseFare, perKmRate, perMinuteRate, AdminCommission, Area);
        }

        public Task<string> DeleteAsync(int fareSettingId)
        {
            return _fareSettings.DeleteAsync(fareSettingId);
        }

        public Task<List<FareSettings>> GetAllFareSettingsAsync()
        {
            return _fareSettings.GetAllFareSettingsAsync();
        }

        public Task<List<FareSettings>> GetAllFareSettingsWithSlabsAsync()
        {
            return _fareSettings.GetAllFareSettingsWithSlabsAsync();
        }

        public Task<decimal> GetBaseFareAsync()
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetFareByIdAsync(int fareSettingId)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetMinimumFareAsync()
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetPerKmRateAsync()
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetPerMinuteRateAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateAsync(int settingId, decimal baseFare, decimal perKmRate, decimal perMinuteRate, decimal AdminCommission, string Area)
        {
            return _fareSettings.UpdateAsync(settingId, baseFare, perKmRate, perMinuteRate, AdminCommission,Area);
        }

        public Task<string> CreateWithSlabsAsync(FareSettingUpsertRequest request)
        {
            return _fareSettings.CreateWithSlabsAsync(request);
        }

        public Task<string> UpdateWithSlabsAsync(FareSettingUpsertRequest request)
        {
            return _fareSettings.UpdateWithSlabsAsync(request);
        }

        public Task UpdateBaseFareAsync(decimal baseFare)
        {
            throw new NotImplementedException();
        }

        public Task UpdateMinimumFareAsync(decimal minimumFare)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePerKmRateAsync(decimal perKmRate)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePerMinuteRateAsync(decimal perMinuteRate)
        {
            throw new NotImplementedException();
        }
    }
}
