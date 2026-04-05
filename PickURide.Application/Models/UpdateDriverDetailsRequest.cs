using System;

namespace PickURide.Application.Models
{
    public class UpdateDriverDetailsRequest
    {
        public Guid DriverId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
        public string? CarLicensePlate { get; set; }
        public string? CarVin { get; set; }
        public string? CarRegistration { get; set; }
        public string? CarInsurance { get; set; }
        public string? Sin { get; set; }
        public string? VehicleName { get; set; }
        public string? VehicleColor { get; set; }
        public string? StripeAccountId { get; set; }
        public string? LicenseImageBase64 { get; set; }
        public string? RegistrationImageBase64 { get; set; }
        public string? InsuranceImageBase64 { get; set; }
        public string? SelfieImageBase64 { get; set; }
    }
}

