using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class RideStop
{
    public Guid RideStopId { get; set; }

    public Guid? RideId { get; set; }

    public int? StopOrder { get; set; }

    public string? Location { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public virtual Ride? Ride { get; set; }
}
