using PickURide.Application.Models.AllRides;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class DriverShiftApplicationDto
    {
        public Guid ApplicationId { get; set; }
        public Guid DriverId { get; set; }
        public Guid ShiftId { get; set; }
        public DateTime AppliedDate { get; set; }
        public string? Status { get; set; }
        public ShiftDto Shift { get; set; } = new();
        public string DriverName { get; set; }
    }
}
