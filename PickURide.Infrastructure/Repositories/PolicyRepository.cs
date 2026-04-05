using Microsoft.EntityFrameworkCore;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Models;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PickURide.Infrastructure.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly PickURideDbContext _context;

    public PolicyRepository(PickURideDbContext context)
    {
        _context = context;
    }

    public async Task<PolicyDto> CreatePolicyAsync(PolicyModel policy)
    {
        var entity = new Policy
        {
            PolicyId = Guid.NewGuid(),
            PolicyType = policy.PolicyType,
            Version = policy.Version,
            Title = policy.Title,
            Content = policy.Content,
            IsActive = policy.IsActive,
            CreatedBy = policy.CreatedBy,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        };

        _context.Policies.Add(entity);
        await _context.SaveChangesAsync();

        return new PolicyDto
        {
            PolicyId = entity.PolicyId,
            PolicyType = entity.PolicyType,
            Version = entity.Version,
            Title = entity.Title,
            Content = entity.Content,
            IsActive = entity.IsActive,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<PolicyDto?> GetLatestActivePolicyAsync(string policyType)
    {
        var policy = await _context.Policies
            .Where(p => p.PolicyType == policyType && p.IsActive == true)
            .OrderByDescending(p => p.Version)
            .Select(p => new PolicyDto
            {
                PolicyId = p.PolicyId,
                PolicyType = p.PolicyType,
                Version = p.Version,
                Title = p.Title,
                Content = p.Content,
                IsActive = p.IsActive,
                CreatedBy = p.CreatedBy,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return policy;
    }

    public async Task<List<PolicyDto>> GetAllVersionsAsync(string policyType)
    {
        return await _context.Policies
            .Where(p => p.PolicyType == policyType)
            .OrderByDescending(p => p.Version)
            .Select(p => new PolicyDto
            {
                PolicyId = p.PolicyId,
                PolicyType = p.PolicyType,
                Version = p.Version,
                Title = p.Title,
                Content = p.Content,
                IsActive = p.IsActive,
                CreatedBy = p.CreatedBy,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<PolicyDto?> GetPolicyByIdAsync(Guid policyId)
    {
        var policy = await _context.Policies
            .Where(p => p.PolicyId == policyId)
            .Select(p => new PolicyDto
            {
                PolicyId = p.PolicyId,
                PolicyType = p.PolicyType,
                Version = p.Version,
                Title = p.Title,
                Content = p.Content,
                IsActive = p.IsActive,
                CreatedBy = p.CreatedBy,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return policy;
    }

    public async Task<bool> DeactivateOtherVersionsAsync(string policyType, Guid excludePolicyId)
    {
        var policiesToDeactivate = await _context.Policies
            .Where(p => p.PolicyType == policyType && p.PolicyId != excludePolicyId && p.IsActive == true)
            .ToListAsync();

        foreach (var policy in policiesToDeactivate)
        {
            policy.IsActive = false;
            policy.UpdatedAt = DateTime.UtcNow;
        }

        if (policiesToDeactivate.Any())
        {
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> GetNextVersionAsync(string policyType)
    {
        var maxVersion = await _context.Policies
            .Where(p => p.PolicyType == policyType)
            .Select(p => (int?)p.Version)
            .MaxAsync();

        return (maxVersion ?? 0) + 1;
    }
}

