using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models.Drivers
{
    public class DriverAttendanceDto
    {
        public Guid AttendanceId { get; set; }
        public Guid DriverId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public double Hours { get; set; }
        public bool Present { get; set; }
    }
}
