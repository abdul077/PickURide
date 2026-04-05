using System;

namespace PickURide.Application.Models;

public class PolicyDto
{
    public Guid PolicyId { get; set; }

    public string PolicyType { get; set; } = null!;

    public int Version { get; set; }

    public string? Title { get; set; }

    public string Content { get; set; } = null!;

    public bool IsActive { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

