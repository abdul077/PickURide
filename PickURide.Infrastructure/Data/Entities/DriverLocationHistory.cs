using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class DriverLocationHistory
{
    public int Id { get; set; }

    public Guid? DriverId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime? LoggedAt { get; set; }

    public virtual Driver? Driver { get; set; }
}
