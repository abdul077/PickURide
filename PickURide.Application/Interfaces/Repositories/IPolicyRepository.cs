using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Repositories;

public interface IPolicyRepository
{
    Task<PolicyDto> CreatePolicyAsync(PolicyModel policy);

    Task<PolicyDto?> GetLatestActivePolicyAsync(string policyType);

    Task<List<PolicyDto>> GetAllVersionsAsync(string policyType);

    Task<PolicyDto?> GetPolicyByIdAsync(Guid policyId);

    Task<bool> DeactivateOtherVersionsAsync(string policyType, Guid excludePolicyId);

    Task<int> GetNextVersionAsync(string policyType);
}

