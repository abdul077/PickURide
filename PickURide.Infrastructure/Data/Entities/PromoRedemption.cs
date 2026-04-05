using System;

namespace PickURide.Infrastructure.Data.Entities;

public partial class PromoRedemption
{
    public Guid PromoRedemptionId { get; set; }

    public Guid PromoCodeId { get; set; }

    public Guid UserId { get; set; }

    public Guid? RideId { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime RedeemedAt { get; set; }

    public virtual PromoCode PromoCode { get; set; } = null!;
}

