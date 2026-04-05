using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class DriverShift
{
    public Guid ShiftId { get; set; }

    public Guid? DriverId { get; set; }

    public TimeOnly? ShiftStart { get; set; }

    public TimeOnly? ShiftEnd { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Driver? Driver { get; set; }
}
