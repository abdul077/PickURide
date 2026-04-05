using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PickURide.Application.Interfaces.Services;

public interface IPolicyService
{
    Task<PolicyDto> CreateOrUpdatePolicyAsync(CreatePolicyRequest request, string policyType, Guid adminId);

    Task<PolicyDto?> GetLatestPrivacyPolicyAsync();

    Task<PolicyDto?> GetLatestTermsAndConditionsAsync();

    Task<List<PolicyDto>> GetPrivacyPolicyHistoryAsync();

    Task<List<PolicyDto>> GetTermsAndConditionsHistoryAsync();

    Task<PolicyDto?> GetPolicyByIdAsync(Guid policyId);
}

