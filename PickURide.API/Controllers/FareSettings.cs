using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FareSettings : ControllerBase
    {
        private readonly IFareSettingService _fareService;

        public FareSettings(IFareSettingService fareService)
        {
            _fareService = fareService;
        }
        [HttpPost("set-fare")]
        public async Task<IActionResult> SetFare([FromBody] FareSettingUpsertRequest request)
        {
            // Allow binding from query string too (backwards compatibility with old Angular).
            // Legacy Angular sends query params with null body; hydrate from query if needed.
            HydrateFromLegacyQueryIfNeeded(request, isUpdate: false);
            try
            {
                var result = await _fareService.CreateWithSlabsAsync(request);
                return Ok(new { Message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("get-fare")]
        public async Task<IActionResult> GetFare()
        {
            var fare = await _fareService.GetAllFareSettingsWithSlabsAsync();
            
            // Ensure minimum base fare of 5 (without rounding)
            const decimal minimumBaseFare = 5;
            foreach (var fareSetting in fare)
            {
                if (fareSetting.BaseFare == null || fareSetting.BaseFare < minimumBaseFare)
                {
                    fareSetting.BaseFare = minimumBaseFare;
                }
                // Return decimal values as stored in database (no rounding)
            }
            
            return Ok(fare);
        }
        [HttpPost("update-fare")]
        public async Task<IActionResult> UpdateFare([FromBody] FareSettingUpsertRequest request)
        {
            HydrateFromLegacyQueryIfNeeded(request, isUpdate: true);
            try
            {
                var result = await _fareService.UpdateWithSlabsAsync(request);
                return Ok(new { Message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        private void HydrateFromLegacyQueryIfNeeded(FareSettingUpsertRequest request, bool isUpdate)
        {
            if (!string.IsNullOrWhiteSpace(request.AreaType) ||
                request.BaseFare.HasValue ||
                request.PerKmRate.HasValue ||
                request.PerMinuteRate.HasValue ||
                request.AdminPercentage.HasValue ||
                (request.Slabs != null && request.Slabs.Count > 0) ||
                (isUpdate && request.SettingId.HasValue))
            {
                return;
            }

            var q = Request?.Query;
            if (q == null || q.Count == 0) return;

            if (q.TryGetValue("Area", out var area))
                request.AreaType = area.ToString();

            if (q.TryGetValue("baseFare", out var baseFare) && decimal.TryParse(baseFare.ToString(), out var bf))
                request.BaseFare = bf;

            if (q.TryGetValue("perKmRate", out var perKmRate) && decimal.TryParse(perKmRate.ToString(), out var pkr))
                request.PerKmRate = pkr;

            if (q.TryGetValue("perMinuteRate", out var perMinuteRate) && decimal.TryParse(perMinuteRate.ToString(), out var pmr))
                request.PerMinuteRate = pmr;

            if (q.TryGetValue("AdminCommision", out var adminC1) && decimal.TryParse(adminC1.ToString(), out var ap1))
                request.AdminPercentage = ap1;
            else if (q.TryGetValue("AdminCommission", out var adminC2) && decimal.TryParse(adminC2.ToString(), out var ap2))
                request.AdminPercentage = ap2;

            if (isUpdate && q.TryGetValue("settingsId", out var settingsId) && int.TryParse(settingsId.ToString(), out var sid))
                request.SettingId = sid;
        }

        [HttpDelete("delete-fare/{fareSettingId}")]
        public async Task<IActionResult> DeleteFare(int fareSettingId)
        {
            var result = await _fareService.DeleteAsync(fareSettingId);
            if (result.Contains("not found"))
            {
                return NotFound(new { Message = result });
            }
            return Ok(new { Message = result });
        }
    }
}
