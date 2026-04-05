using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class Ride
{
    public Guid RideId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? DriverId { get; set; }

    public string? RideType { get; set; }

    public bool? IsScheduled { get; set; }

    public DateTime? ScheduledTime { get; set; }

    public int? PassengerCount { get; set; }

    public decimal? FareEstimate { get; set; }

    public decimal? FareFinal { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public TimeOnly? RideStartTime { get; set; }

    public TimeOnly? RideEndTime { get; set; }

    public TimeOnly? RideWaitingStartTime { get; set; }

    public TimeOnly? RideWaitingEndTime { get; set; }

    public TimeOnly? RideWaitingTotalTime { get; set; }

    public string? Distance { get; set; }

    public string? AdminCommission { get; set; }

    public string? DriverPayment { get; set; }

    public string? PromoCode { get; set; }

    public decimal? PromoDiscount { get; set; }

    public virtual Driver? Driver { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<RideMessage> RideMessages { get; set; } = new List<RideMessage>();

    public virtual ICollection<RideStop> RideStops { get; set; } = new List<RideStop>();

    public virtual ICollection<Tip> Tips { get; set; } = new List<Tip>();

    public virtual User? User { get; set; }
}
