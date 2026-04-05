using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class Feedback
{
    public Guid FeedbackId { get; set; }

    public Guid? RideId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? DriverId { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? FeedbackFrom { get; set; } // "User" or "Driver"

    public virtual Ride? Ride { get; set; }

    public virtual User? User { get; set; }

    public virtual Driver? Driver { get; set; }
}
