using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class PromoCode
{
    public Guid PromoCodeId { get; set; }

    public string Code { get; set; } = null!;

    public decimal FlatAmount { get; set; }

    public decimal? MinFare { get; set; }

    public DateTime? ExpiryUtc { get; set; }

    public bool IsActive { get; set; }

    public int PerUserLimit { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PromoRedemption> PromoRedemptions { get; set; } = new List<PromoRedemption>();
}

