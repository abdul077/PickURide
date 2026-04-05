using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverAttendance : ControllerBase
    {
        private readonly IDriverAttendanceService _attendanceService;

        public DriverAttendance(IDriverAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartDuty([FromBody] StartDutyRequest request)
        {
            var attendanceId = await _attendanceService.StartDutyAsync(request);
            return Ok(new { AttendanceId = attendanceId });
        }

        [HttpPost("end")]
        public async Task<IActionResult> EndDuty([FromBody] EndDutyRequest request)
        {
            await _attendanceService.EndDutyAsync(request);
            return Ok(new { Message = "Duty ended successfully." });
        }

        [HttpGet("is-eligible-for-shift")]
        public async Task<IActionResult> IsEligibleForShift([FromQuery] Guid driverId, [FromQuery] DateTime shiftDate)
        {
            var isEligible = await _attendanceService.IsEligibleForShiftAsync(driverId, shiftDate);
            return Ok(new { IsEligible = isEligible });
        }
    }
}
