using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class DriverShiftDto
    {
        public Guid ShiftId { get; set; }
        public Guid DriverId { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
    }
}
