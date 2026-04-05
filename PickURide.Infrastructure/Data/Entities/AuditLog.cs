using System;

namespace PickURide.Infrastructure.Data.Entities;

public partial class AuditLog
{
    public Guid AuditLogId { get; set; }

    public Guid? UserId { get; set; }

    public string? UserType { get; set; }

    public string Action { get; set; } = null!;

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? RequestData { get; set; }

    public string? ResponseData { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string Status { get; set; } = null!;

    public string? ErrorMessage { get; set; }

    public DateTime Timestamp { get; set; }

    public int? Duration { get; set; }
}

