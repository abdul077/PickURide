using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;

namespace PickURide.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Shift : ControllerBase
    {
        private readonly IShiftService _shiftService;

        public Shift(IShiftService shiftService)
        {
            _shiftService = shiftService;
        }

        [HttpPost("create-shift-admin")]
        public async Task<IActionResult> CreateShift([FromBody] CreateShiftRequest request)
        {
            var shiftId = await _shiftService.CreateShiftAsync(request);
            return Ok(new { ShiftId = shiftId });
        }

        [HttpPost("update-shift-admin")]
        public async Task<IActionResult> UpdateShift([FromBody] UpdateShiftRequest request)
        {
            var result = await _shiftService.UpdateShiftAsync(request);
            return Ok(new { Message = result });
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyForShift([FromBody] ApplyShiftRequest request)
        {
            var shift = await _shiftService.ApplyForShiftAsync(request);
            return Ok(new { Message = shift });
        }

        [HttpPost("approve-admin/{applicationId}")]
        public async Task<IActionResult> ApproveApplication(Guid applicationId)
        {
            var shift = await _shiftService.ApproveApplicationAsync(applicationId);
            return Ok(new { Message = shift });
        }
        [HttpPost("reject-admin/{applicationId}")]
        public async Task<IActionResult> RejectApplication(Guid applicationId)
        {
            var shift = await _shiftService.RejectApplicationAsync(applicationId);
            return Ok(new { Message = shift });
        }

        [HttpPost("get-all-shifts-admin")]
        public async Task<IActionResult> GetAllShifts()
        {
            var shifts = await _shiftService.GetAllShiftsAsync();
            return Ok(shifts);
        }
        [HttpPost("get-active-shifts")]
        public async Task<IActionResult> GetActiveShifts()
        {
            var shifts = await _shiftService.GetActiveShiftsAsync();
            return Ok(shifts);
        }
        [HttpPost("get-driver-applied-shifts")]
        public async Task<IActionResult> GetDriverAppliedShifts(Guid Id)
        {
            var shifts = await _shiftService.GetDriversAppliedShiftsAsync(Id);
            var result = shifts.Select(s => new { s.ShiftId, s.Status }).ToList();
            return Ok(result);
        }
        [HttpPost("get-pending-shifts-applications-admin")]
        public async Task<IActionResult> GetPendingShiftsApplications()
        {
            var shifts = await _shiftService.GetPendingShiftsApplications();
            return Ok(shifts);
        }
        [HttpPost("get-approved-shifts-applications-admin")]
        public async Task<IActionResult> GetApprovedShiftsApplications()
        {
            var shifts = await _shiftService.GetApprovedShiftsApplications();
            return Ok(shifts);
        }
        [HttpPost("get-rejected-shifts-applications-admin")]
        public async Task<IActionResult> GetRejectedShiftsApplications()
        {
            var shifts = await _shiftService.GetRejectedShiftsApplications();
            return Ok(shifts);
        }
    }
}
