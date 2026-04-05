using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class DriverOvertimeDuty
{
    public Guid OvertimeDutyId { get; set; }

    public Guid ShiftId { get; set; }

    public Guid DriverId { get; set; }

    public DateTime DutyStart { get; set; }

    public DateTime DutyEnd { get; set; }

    public virtual Driver Driver { get; set; } = null!;

    public virtual Shift Shift { get; set; } = null!;
}
