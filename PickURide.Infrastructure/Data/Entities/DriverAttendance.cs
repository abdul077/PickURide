using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class DriverAttendance
{
    public Guid AttendanceId { get; set; }

    public Guid DriverId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string AttendanceType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Driver Driver { get; set; } = null!;
}
