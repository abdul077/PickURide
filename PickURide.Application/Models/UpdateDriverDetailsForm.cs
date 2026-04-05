using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace PickURide.Application.Models
{
    public class UpdateDriverDetailsForm
    {
        [FromForm] public Guid DriverId { get; set; }
        [FromForm] public string? FullName { get; set; }
        [FromForm] public string? PhoneNumber { get; set; }
        [FromForm] public string? Address { get; set; }
        [FromForm] public string? LicenseNumber { get; set; }
        [FromForm] public string? CarLicensePlate { get; set; }
        [FromForm] public string? CarVin { get; set; }
        [FromForm] public string? CarRegistration { get; set; }
        [FromForm] public string? CarInsurance { get; set; }
        [FromForm] public string? Sin { get; set; }
        [FromForm] public string? VehicleName { get; set; }
        [FromForm] public string? VehicleColor { get; set; }
        [FromForm] public string? StripeAccountId { get; set; }

        [FromForm] public IFormFile? LicenseImage { get; set; }
        [FromForm] public IFormFile? RegistrationImage { get; set; }
        [FromForm] public IFormFile? InsuranceImage { get; set; }
        [FromForm] public IFormFile? SelfieImage { get; set; }
    }
}

