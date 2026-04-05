using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Services;

public class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _policyRepository;

    public const string PrivacyPolicyType = "PrivacyPolicy";
    public const string TermsAndConditionsType = "TermsAndConditions";

    public PolicyService(IPolicyRepository policyRepository)
    {
        _policyRepository = policyRepository;
    }

    public async Task<PolicyDto> CreateOrUpdatePolicyAsync(CreatePolicyRequest request, string policyType, Guid adminId)
    {
        // Validate content
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Policy content cannot be empty.", nameof(request));
        }

        // Get next version number
        var nextVersion = await _policyRepository.GetNextVersionAsync(policyType);

        // Create new policy model
        var policyModel = new PolicyModel
        {
            PolicyId = Guid.NewGuid(),
            PolicyType = policyType,
            Version = nextVersion,
            Title = request.Title,
            Content = request.Content,
            IsActive = true,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        // Deactivate all previous versions of the same type
        await _policyRepository.DeactivateOtherVersionsAsync(policyType, policyModel.PolicyId);

        // Create new policy
        var result = await _policyRepository.CreatePolicyAsync(policyModel);

        return result;
    }

    public async Task<PolicyDto?> GetLatestPrivacyPolicyAsync()
    {
        return await _policyRepository.GetLatestActivePolicyAsync(PrivacyPolicyType);
    }

    public async Task<PolicyDto?> GetLatestTermsAndConditionsAsync()
    {
        return await _policyRepository.GetLatestActivePolicyAsync(TermsAndConditionsType);
    }

    public async Task<List<PolicyDto>> GetPrivacyPolicyHistoryAsync()
    {
        return await _policyRepository.GetAllVersionsAsync(PrivacyPolicyType);
    }

    public async Task<List<PolicyDto>> GetTermsAndConditionsHistoryAsync()
    {
        return await _policyRepository.GetAllVersionsAsync(TermsAndConditionsType);
    }

    public async Task<PolicyDto?> GetPolicyByIdAsync(Guid policyId)
    {
        return await _policyRepository.GetPolicyByIdAsync(policyId);
    }
}

