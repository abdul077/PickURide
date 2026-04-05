using System;

namespace PickURide.Application.Models
{
    public class AssignDriverRequest
    {
        public Guid RideId { get; set; }
        public Guid DriverId { get; set; }
    }
}