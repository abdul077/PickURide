using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class RideMessage
{
    public Guid MessageId { get; set; }

    public Guid RideId { get; set; }

    public Guid SenderId { get; set; }

    public string SenderRole { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public virtual Ride Ride { get; set; } = null!;
}
