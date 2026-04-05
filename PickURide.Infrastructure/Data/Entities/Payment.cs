using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class Payment
{
    public Guid PaymentId { get; set; }

    public Guid? RideId { get; set; }

    public string? PaymentMethod { get; set; }

    public decimal? PaidAmount { get; set; }

    public decimal? TipAmount { get; set; }

    public decimal? AdminShare { get; set; }

    public decimal? DriverShare { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? DriverId { get; set; }

    public Guid? UserId { get; set; }

    public string? PaymentToken { get; set; }

    public string? PromoCode { get; set; }

    public string? CustomerPaid { get; set; }

    public string? TransferStatus { get; set; }

    public string? TransferId { get; set; }

    public DateTime? TransferredAt { get; set; }

    public virtual Ride? Ride { get; set; }
}
