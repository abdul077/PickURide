using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services
{
    public interface IDriverAttendanceService
    {
        Task<Guid> StartDutyAsync(StartDutyRequest request);
        Task EndDutyAsync(EndDutyRequest request);
        Task<bool> IsEligibleForShiftAsync(Guid driverId, DateTime shiftDate);
    }
}
