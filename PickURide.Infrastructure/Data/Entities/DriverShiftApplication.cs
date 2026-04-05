using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class DriverShiftApplication
{
    public Guid ApplicationId { get; set; }

    public Guid ShiftId { get; set; }

    public Guid DriverId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime AppliedAt { get; set; }

    public virtual Driver Driver { get; set; } = null!;

    public virtual Shift Shift { get; set; } = null!;
}
