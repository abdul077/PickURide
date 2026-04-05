using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class SupportChat
{
    public Guid ChatId { get; set; }

    public Guid? SenderId { get; set; }

    public Guid? ReceiverId { get; set; }

    public string? Message { get; set; }

    public DateTime? SentAt { get; set; }

    public string? SenderRole { get; set; }
}
