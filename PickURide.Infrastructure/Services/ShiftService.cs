using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Application.Models.Drivers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Services
{
    public class ShiftService : IShiftService
    {
        private readonly IShiftRepository _shiftRepository;

        public ShiftService(IShiftRepository shiftRepository)
        {
            _shiftRepository = shiftRepository;
        }

        public Task<string> ApplyForShiftAsync(ApplyShiftRequest request)
        {
            return _shiftRepository.ApplyForShiftAsync(request);
        }

        public Task<string> ApproveApplicationAsync(Guid applicationId)
        {
            return _shiftRepository.ApproveApplicationAsync(applicationId);
        }

        public Task<Guid> CreateShiftAsync(CreateShiftRequest request)
        {
            return _shiftRepository.CreateShiftAsync(request);
        }

        public Task<string> UpdateShiftAsync(UpdateShiftRequest request)
        {
            return _shiftRepository.UpdateShiftAsync(request);
        }

        public Task<List<ShiftDto>> GetActiveShiftsAsync()
        {
            return _shiftRepository.GetActiveShiftsAsync();
        }

        public Task<List<ShiftDto>> GetAllShiftsAsync()
        {
            return _shiftRepository.GetAllShiftsAsync();
        }

        public Task<List<DriverShiftApplicationDto>> GetApprovedShiftsApplications()
        {
            return _shiftRepository.GetApprovedShiftsApplications();
        }

        public Task<List<DriverShiftApplicationDto>> GetDriversAppliedShiftsAsync(Guid Id)
        {
            return _shiftRepository.GetDriverAppliedShiftsAsync(Id);
        }

        public Task<List<DriverShiftApplicationDto>> GetPendingShiftsApplications()
        {
            return _shiftRepository.GetPendingShiftsApplications();
        }

        public Task<List<DriverShiftApplicationDto>> GetRejectedShiftsApplications()
        {
            return _shiftRepository.GetRejectedShiftsApplications();
        }

        public Task<string> RejectApplicationAsync(Guid applicationId)
        {
            return _shiftRepository.RejectApplicationAsync(applicationId);
        }
    }
}
