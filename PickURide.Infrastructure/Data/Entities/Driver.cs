using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickURide.Infrastructure.Data.Entities;

public partial class Driver
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

    public string? DeviceToken { get; set; }

    public bool? Verified { get; set; }

    public string? VehicleName { get; set; }

    public string? VehicleColor { get; set; }

    public string? RejectionReason { get; set; }

    public string? ApprovalStatus { get; set; }

    [Column("StripeAccountId")]
    public string? StripeAccountId { get; set; }

    public virtual ICollection<DriverAttendance> DriverAttendances { get; set; } = new List<DriverAttendance>();

    public virtual ICollection<DriverLocationHistory> DriverLocationHistories { get; set; } = new List<DriverLocationHistory>();

    public virtual ICollection<DriverOvertimeDuty> DriverOvertimeDuties { get; set; } = new List<DriverOvertimeDuty>();

    public virtual ICollection<DriverShiftApplication> DriverShiftApplications { get; set; } = new List<DriverShiftApplication>();

    public virtual ICollection<DriverShift> DriverShifts { get; set; } = new List<DriverShift>();

    public virtual ICollection<Ride> Rides { get; set; } = new List<Ride>();
}
