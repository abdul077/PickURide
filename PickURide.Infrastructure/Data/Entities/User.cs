using System;
using System.Collections.Generic;

namespace PickURide.Infrastructure.Data.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? DeviceToken { get; set; }

    public bool? Verified { get; set; }

    public string? ProfilePicture { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Ride> Rides { get; set; } = new List<Ride>();

    public virtual ICollection<Tip> Tips { get; set; } = new List<Tip>();
}
