using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Repositories
{
    public class FareSettingRepository : IFareSettingRepository
    {
        private readonly PickURideDbContext _context;

        public FareSettingRepository(PickURideDbContext context)
        {
            _context = context;
        }

        public Task<decimal> CalculateFareAsync(decimal distance, TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        public async Task<string> CreateAsync(decimal baseFare, decimal perKmRate, decimal perMinuteRate, decimal AdminCommission, string Area)
        {
            var newSetting = new FareSetting
            {
                BaseFare = baseFare,
                PerKmRate = perKmRate,
                PerMinuteRate = perMinuteRate,
                AdminPercentage = AdminCommission,
                AreaType = Area
            };

            _context.FareSettings.Add(newSetting);
            await _context.SaveChangesAsync();

            return $"Fare setting created with ID: {newSetting.SettingId}";
        }

        public async Task<string> CreateWithSlabsAsync(FareSettingUpsertRequest request)
        {
            ValidateUpsertRequest(request, isUpdate: false);

            var newSetting = new FareSetting
            {
                AreaType = request.AreaType,
                BaseFare = request.BaseFare,
                PerKmRate = request.PerKmRate, // back-compat
                PerMinuteRate = request.PerMinuteRate,
                AdminPercentage = request.AdminPercentage
            };

            _context.FareSettings.Add(newSetting);
            await _context.SaveChangesAsync();

            var slabs = NormalizeSlabs(request);
            foreach (var slab in slabs)
            {
                _context.FareDistanceSlabs.Add(new FareDistanceSlab
                {
                    SettingId = newSetting.SettingId,
                    FromKm = slab.FromKm,
                    ToKm = slab.ToKm,
                    RatePerKm = slab.RatePerKm,
                    SortOrder = slab.SortOrder
                });
            }

            await _context.SaveChangesAsync();
            return $"Fare setting created with ID: {newSetting.SettingId}";
        }

        public async Task<string> DeleteAsync(int fareSettingId)
        {
            var fareSetting = await _context.FareSettings.FindAsync(fareSettingId);
            if (fareSetting == null)
            {
                return $"Fare setting with ID {fareSettingId} not found.";
            }

            _context.FareSettings.Remove(fareSetting);
            await _context.SaveChangesAsync();

            return $"Fare setting with ID {fareSettingId} deleted successfully.";
        }

        public Task<List<FareSettings>> GetAllFareSettingsAsync()
        {
            var fareSettings = _context.FareSettings
                .Select(fs => new FareSettings
                {
                    SettingId = fs.SettingId,
                    BaseFare = fs.BaseFare,
                    PerKmRate = fs.PerKmRate,
                    PerMinuteRate = fs.PerMinuteRate,
                    AdminPercentage = fs.AdminPercentage,
                    AreaType = fs.AreaType
                }).ToListAsync();
            return fareSettings;
        }

        public async Task<List<FareSettings>> GetAllFareSettingsWithSlabsAsync()
        {
            var entities = await _context.FareSettings
                .AsNoTracking()
                .Include(fs => fs.FareDistanceSlabs)
                .ToListAsync();

            return entities
                .Select(fs => new FareSettings
                {
                    SettingId = fs.SettingId,
                    BaseFare = fs.BaseFare,
                    PerKmRate = fs.PerKmRate, // back-compat
                    PerMinuteRate = fs.PerMinuteRate,
                    AdminPercentage = fs.AdminPercentage,
                    AreaType = fs.AreaType,
                    Slabs = fs.FareDistanceSlabs
                        .OrderBy(s => s.SortOrder)
                        .ThenBy(s => s.FromKm)
                        .Select(s => new FareDistanceSlabDto
                        {
                            SlabId = s.SlabId,
                            FromKm = s.FromKm,
                            ToKm = s.ToKm,
                            RatePerKm = s.RatePerKm,
                            SortOrder = s.SortOrder
                        }).ToList()
                })
                .ToList();
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
            var fareSetting = _context.FareSettings.Find(settingId);
            if (fareSetting == null)
            {
                return Task.FromResult($"Fare setting with ID {settingId} not found.");
            }
            fareSetting.BaseFare = baseFare;
            fareSetting.PerKmRate = perKmRate;
            fareSetting.PerMinuteRate = perMinuteRate;
            fareSetting.AdminPercentage = AdminCommission;
            fareSetting.AreaType = Area;
            _context.FareSettings.Update(fareSetting);
            _context.SaveChanges();
            return Task.FromResult($"Fare setting with ID {settingId} updated successfully.");
        }

        public async Task<string> UpdateWithSlabsAsync(FareSettingUpsertRequest request)
        {
            ValidateUpsertRequest(request, isUpdate: true);

            var settingId = request.SettingId!.Value;
            var fareSetting = await _context.FareSettings.FirstOrDefaultAsync(x => x.SettingId == settingId);
            if (fareSetting == null)
            {
                return $"Fare setting with ID {settingId} not found.";
            }

            fareSetting.AreaType = request.AreaType;
            fareSetting.BaseFare = request.BaseFare;
            fareSetting.PerKmRate = request.PerKmRate; // back-compat
            fareSetting.PerMinuteRate = request.PerMinuteRate;
            fareSetting.AdminPercentage = request.AdminPercentage;

            var normalizedSlabs = NormalizeSlabs(request);

            var existing = await _context.FareDistanceSlabs.Where(s => s.SettingId == settingId).ToListAsync();
            _context.FareDistanceSlabs.RemoveRange(existing);

            foreach (var slab in normalizedSlabs)
            {
                _context.FareDistanceSlabs.Add(new FareDistanceSlab
                {
                    SettingId = settingId,
                    FromKm = slab.FromKm,
                    ToKm = slab.ToKm,
                    RatePerKm = slab.RatePerKm,
                    SortOrder = slab.SortOrder
                });
            }

            await _context.SaveChangesAsync();
            return $"Fare setting with ID {settingId} updated successfully.";
        }

        private static void ValidateUpsertRequest(FareSettingUpsertRequest request, bool isUpdate)
        {
            if (isUpdate && (!request.SettingId.HasValue || request.SettingId.Value <= 0))
                throw new ArgumentException("SettingId is required for update.");

            if (string.IsNullOrWhiteSpace(request.AreaType))
                throw new ArgumentException("AreaType is required.");

            if (!request.BaseFare.HasValue)
                throw new ArgumentException("BaseFare is required.");

            if (!request.PerMinuteRate.HasValue)
                throw new ArgumentException("PerMinuteRate is required.");

            if (!request.AdminPercentage.HasValue)
                throw new ArgumentException("AdminPercentage is required.");

            // Need at least one of: slabs or perKmRate for back-compat.
            var hasSlabs = request.Slabs != null && request.Slabs.Count > 0;
            if (!hasSlabs && !request.PerKmRate.HasValue)
                throw new ArgumentException("Either Slabs or PerKmRate must be provided.");
        }

        private static List<FareDistanceSlabDto> NormalizeSlabs(FareSettingUpsertRequest request)
        {
            var hasSlabs = request.Slabs != null && request.Slabs.Count > 0;
            var slabs = hasSlabs
                ? request.Slabs!
                : new List<FareDistanceSlabDto>
                {
                    new FareDistanceSlabDto
                    {
                        FromKm = 0m,
                        ToKm = null,
                        RatePerKm = request.PerKmRate ?? 0m,
                        SortOrder = 0
                    }
                };

            // Normalize ordering and validate non-overlap.
            var ordered = slabs
                .Select(s => new FareDistanceSlabDto
                {
                    SlabId = s.SlabId,
                    FromKm = s.FromKm,
                    ToKm = s.ToKm,
                    RatePerKm = s.RatePerKm,
                    SortOrder = s.SortOrder
                })
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.FromKm)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var s = ordered[i];
                if (s.FromKm < 0)
                    throw new ArgumentException("Slab FromKm must be >= 0.");
                if (s.ToKm.HasValue && s.ToKm.Value < s.FromKm)
                    throw new ArgumentException("Slab ToKm must be >= FromKm.");
                if (s.RatePerKm < 0)
                    throw new ArgumentException("Slab RatePerKm must be >= 0.");
                if (s.ToKm is null && i != ordered.Count - 1)
                    throw new ArgumentException("Open-ended slab (ToKm=null) must be the last slab.");
            }

            // Overlap check (simple, supports gaps, disallows overlaps).
            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var cur = ordered[i];
                if (prev.ToKm is null)
                    throw new ArgumentException("Slabs cannot exist after an open-ended slab.");
                if (cur.FromKm < prev.ToKm.Value)
                    throw new ArgumentException("Slabs cannot overlap.");
            }

            // Reassign SortOrder sequentially to keep consistent server order.
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].SortOrder = i;
            }

            return ordered;
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
