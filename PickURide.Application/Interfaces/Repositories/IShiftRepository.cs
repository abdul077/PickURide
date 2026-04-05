using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories
{
    public interface IShiftRepository
    {
        Task<Guid> CreateShiftAsync(CreateShiftRequest request);
        Task<string> UpdateShiftAsync(UpdateShiftRequest request);
        Task<string> ApplyForShiftAsync(ApplyShiftRequest request);
        Task<string> ApproveApplicationAsync(Guid applicationId);
        Task<string> RejectApplicationAsync(Guid applicationId);
        Task<List<ShiftDto>> GetAllShiftsAsync();
        Task<List<ShiftDto>> GetActiveShiftsAsync();
        Task<List<DriverShiftApplicationDto>> GetDriverAppliedShiftsAsync(Guid Id);
        Task<List<DriverShiftApplicationDto>> GetPendingShiftsApplications();
        Task<List<DriverShiftApplicationDto>> GetApprovedShiftsApplications();
        Task<List<DriverShiftApplicationDto>> GetRejectedShiftsApplications();
    }
}
