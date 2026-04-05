using System;
using System.Collections.Generic;

namespace PickURide.Application.Models
{
    public class PaymentDetailDto
    {
        public Guid PaymentId { get; set; }
        public Guid? RideId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? PaidAmount { get; set; }
        public decimal? TipAmount { get; set; }
        public decimal? AdminShare { get; set; }
        public decimal? DriverShare { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? CreatedAt { get; set; }
        public Guid? UserId { get; set; }
        public Guid? DriverId { get; set; }
        public string? PaymentToken { get; set; }

        // User Details
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserPhoneNumber { get; set; }

        // Driver Details
        public string? DriverFullName { get; set; }
        public string? DriverEmail { get; set; }
        public string? DriverPhoneNumber { get; set; }
        public string? DriverLicensePlate { get; set; }
        public string? DriverVehicleName { get; set; }
        public string? DriverVehicleColor { get; set; }

        // Ride Details
        public string? RideType { get; set; }
        public decimal? RideFareFinal { get; set; }
        public string? RideStatus { get; set; }
        public string? RideDistance { get; set; }
        public int? RidePassengerCount { get; set; }
        public DateTime? RideCreatedAt { get; set; }
        public TimeOnly? RideStartTime { get; set; }
        public TimeOnly? RideEndTime { get; set; }
        public string? PickupLocation { get; set; }
        public string? DropoffLocation { get; set; }
        public List<RideStopDto>? RideStops { get; set; }
    }

    public class PaymentEarningsSummaryDto
    {
        public decimal TotalEarnings { get; set; }
        public decimal DailyEarnings { get; set; }
        public decimal WeeklyEarnings { get; set; }
        public decimal MonthlyEarnings { get; set; }
        public decimal TotalAdminShare { get; set; }
        public decimal TotalDriverShare { get; set; }
        public decimal TotalCompletedDriverShare { get; set; }
        public decimal TotalTips { get; set; }
        public int TotalPayments { get; set; }
        public int TotalCompletedPayments { get; set; }
        public int DailyPayments { get; set; }
        public int WeeklyPayments { get; set; }
        public int MonthlyPayments { get; set; }
    }
}
