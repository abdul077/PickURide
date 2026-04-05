using System;
using System.Collections.Generic;

namespace PickURide.Application.Models;

public class DriverLocationHistory
{
    public Guid DriverId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime LoggedAt { get; set; }
}
