using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Models
{
    public class StartDutyRequest
    {
        public Guid DriverId { get; set; }
        public string AttendanceType { get; set; } = "Duty"; // or "Shift"
    }

}
