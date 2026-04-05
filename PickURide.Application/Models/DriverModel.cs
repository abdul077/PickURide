using System;
using System.Collections.Generic;

namespace PickURide.Application.Models;

public partial class DriverModel
{
    public Guid DriverId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? LicenseNumber { get; set; }

    public string? CarLicensePlate { get; set; }

    public string? CarVin { get; set; }

    public string? CarRegistration { get; set; }

    public string? CarInsurance { get; set; }

    public string? Sin { get; set; }

    public string? LicenseImage { get; set; }

    public string? RegistrationImage { get; set; }

    public string? InsuranceImage { get; set; }

    public string? SelfieImage { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? PasswordHash { get; set; }
    public bool? Verified { get; set; }
    public string? RejectedReason { get; set; }
    public string? ApprovalStatus { get; set; }

    public string? StripeAccountId { get; set; }


}
