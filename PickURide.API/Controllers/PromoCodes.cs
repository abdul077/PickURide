using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System.Linq;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromoCodes : ControllerBase
    {
        private readonly PickURideDbContext _db;

        public PromoCodes(PickURideDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var promos = await _db.PromoCodes
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.PromoCodeId,
                    p.Code,
                    p.FlatAmount,
                    p.MinFare,
                    p.ExpiryUtc,
                    p.IsActive,
                    p.PerUserLimit,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(promos);
        }

        public sealed class PromoUpsertRequest
        {
            public Guid? PromoCodeId { get; set; }
            public string Code { get; set; } = string.Empty;
            public decimal FlatAmount { get; set; }
            public decimal? MinFare { get; set; }
            public DateTime? ExpiryUtc { get; set; }
            public bool IsActive { get; set; } = true;
            public int PerUserLimit { get; set; } = 1;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PromoUpsertRequest request)
        {
            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { Message = "Code is required." });
            if (request.FlatAmount <= 0)
                return BadRequest(new { Message = "FlatAmount must be > 0." });
            if (request.PerUserLimit <= 0)
                return BadRequest(new { Message = "PerUserLimit must be > 0." });

            var exists = await _db.PromoCodes.AnyAsync(p => p.Code == code);
            if (exists)
                return Conflict(new { Message = "Promo code already exists." });

            var entity = new PromoCode
            {
                PromoCodeId = Guid.NewGuid(),
                Code = code,
                FlatAmount = request.FlatAmount,
                MinFare = request.MinFare,
                ExpiryUtc = request.ExpiryUtc,
                IsActive = request.IsActive,
                PerUserLimit = request.PerUserLimit,
                CreatedAt = DateTime.UtcNow
            };

            await _db.PromoCodes.AddAsync(entity);
            await _db.SaveChangesAsync();
            return Ok(new { Message = "Promo created", entity.PromoCodeId });
        }

        [HttpPut("{promoCodeId:guid}")]
        public async Task<IActionResult> Update(Guid promoCodeId, [FromBody] PromoUpsertRequest request)
        {
            var entity = await _db.PromoCodes.FirstOrDefaultAsync(p => p.PromoCodeId == promoCodeId);
            if (entity == null) return NotFound(new { Message = "Promo not found." });

            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { Message = "Code is required." });
            if (request.FlatAmount <= 0)
                return BadRequest(new { Message = "FlatAmount must be > 0." });
            if (request.PerUserLimit <= 0)
                return BadRequest(new { Message = "PerUserLimit must be > 0." });

            var duplicate = await _db.PromoCodes.AnyAsync(p => p.PromoCodeId != promoCodeId && p.Code == code);
            if (duplicate)
                return Conflict(new { Message = "Another promo already uses this code." });

            entity.Code = code;
            entity.FlatAmount = request.FlatAmount;
            entity.MinFare = request.MinFare;
            entity.ExpiryUtc = request.ExpiryUtc;
            entity.IsActive = request.IsActive;
            entity.PerUserLimit = request.PerUserLimit;

            await _db.SaveChangesAsync();
            return Ok(new { Message = "Promo updated" });
        }

        [HttpGet("{promoCodeId:guid}/usage")]
        public async Task<IActionResult> Usage(Guid promoCodeId)
        {
            var exists = await _db.PromoCodes.AnyAsync(p => p.PromoCodeId == promoCodeId);
            if (!exists) return NotFound(new { Message = "Promo not found." });

            var usage = await _db.PromoRedemptions
                .Where(r => r.PromoCodeId == promoCodeId)
                .GroupBy(r => r.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count(),
                    TotalDiscount = g.Sum(x => x.DiscountAmount),
                    LastRedeemedAt = g.Max(x => x.RedeemedAt)
                })
                .OrderByDescending(x => x.LastRedeemedAt)
                .ToListAsync();

            return Ok(usage);
        }

        [HttpGet("usage")]
        public async Task<IActionResult> AllUsage()
        {
            var rows = await _db.PromoRedemptions
                .Join(_db.PromoCodes, r => r.PromoCodeId, p => p.PromoCodeId, (r, p) => new { r, p })
                .GroupBy(x => new { x.p.PromoCodeId, x.p.Code, x.r.UserId })
                .Select(g => new
                {
                    PromoCodeId = g.Key.PromoCodeId,
                    Code = g.Key.Code,
                    UserId = g.Key.UserId,
                    Count = g.Count(),
                    TotalDiscount = g.Sum(x => x.r.DiscountAmount),
                    LastRedeemedAt = g.Max(x => x.r.RedeemedAt)
                })
                .OrderByDescending(x => x.LastRedeemedAt)
                .ToListAsync();

            return Ok(rows);
        }
    }
}

