using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class Shift
{
    public Guid ShiftId { get; set; }

    public DateOnly ShiftDate { get; set; }

    public TimeOnly? ShiftStart { get; set; }

    public TimeOnly? ShiftEnd { get; set; }

    public int MaxDriverCount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DriverOvertimeDuty> DriverOvertimeDuties { get; set; } = new List<DriverOvertimeDuty>();

    public virtual ICollection<DriverShiftApplication> DriverShiftApplications { get; set; } = new List<DriverShiftApplication>();
}
