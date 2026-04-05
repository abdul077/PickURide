using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PickURide.Application.Models
{
    public class UploadDriverImagesForm
    {
        [FromForm] public Guid DriverId { get; set; }

        [FromForm] public IFormFile? LicenseImage { get; set; }

        [FromForm] public IFormFile? RegistrationImage { get; set; }

        [FromForm] public IFormFile? InsuranceImage { get; set; }

        [FromForm] public IFormFile? SelfieImage { get; set; }
    }

}
