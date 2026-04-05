using System;

namespace PickURide.Application.Models
{
    public class PromoCodeModel
    {
        public Guid PromoCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal FlatAmount { get; set; }
        public decimal? MinFare { get; set; }
        public DateTime? ExpiryUtc { get; set; }
        public bool IsActive { get; set; }
        public int PerUserLimit { get; set; }
    }
}

