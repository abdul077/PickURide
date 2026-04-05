using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class Tip
{
    public Guid TipId { get; set; }

    public Guid? RideId { get; set; }

    public Guid? UserId { get; set; }

    public decimal? Amount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Ride? Ride { get; set; }

    public virtual User? User { get; set; }
}
