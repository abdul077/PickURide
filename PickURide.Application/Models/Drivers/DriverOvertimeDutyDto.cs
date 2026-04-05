using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class DriverOvertimeDutyDto
    {
        public Guid OvertimeId { get; set; }
        public Guid DriverId { get; set; }
        public DateTime DutyDate { get; set; }
        public int Hours { get; set; }
    }
}
