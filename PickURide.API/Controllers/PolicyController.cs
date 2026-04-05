using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickURide.Application.Extensions;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using PickURide.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PickURide.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PolicyController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpPost("create-privacy-policy")]
    public async Task<IActionResult> CreatePrivacyPolicy([FromBody] CreatePolicyRequest request)
    {
        // Only Admin can create/update policies
        if (!HttpContext.IsAdmin())
        {
            return Unauthorized(new { message = "Only administrators can create or update policies." });
        }

        var adminId = HttpContext.GetUserId();
        if (!adminId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        try
        {
            var result = await _policyService.CreateOrUpdatePolicyAsync(
                request,
                PolicyService.PrivacyPolicyType,
                adminId.Value
            );

            return Ok(new { message = "Privacy Policy created successfully.", policy = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the policy.", error = ex.Message });
        }
    }

    [HttpPost("create-terms-and-conditions")]
    public async Task<IActionResult> CreateTermsAndConditions([FromBody] CreatePolicyRequest request)
    {
        // Only Admin can create/update policies
        if (!HttpContext.IsAdmin())
        {
            return Unauthorized(new { message = "Only administrators can create or update policies." });
        }

        var adminId = HttpContext.GetUserId();
        if (!adminId.HasValue)
        {
            return Unauthorized(new { message = "User not authenticated." });
        }

        try
        {
            var result = await _policyService.CreateOrUpdatePolicyAsync(
                request,
                PolicyService.TermsAndConditionsType,
                adminId.Value
            );

            return Ok(new { message = "Terms & Conditions created successfully.", policy = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the policy.", error = ex.Message });
        }
    }

    [HttpPost("get-privacy-policy")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPrivacyPolicy()
    {
        // Public endpoint - no authentication required
        var policy = await _policyService.GetLatestPrivacyPolicyAsync();

        if (policy == null)
        {
            return NotFound(new { message = "Privacy Policy not found." });
        }

        return Ok(policy);
    }

    [HttpPost("get-terms-and-conditions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTermsAndConditions()
    {
        // Public endpoint - no authentication required
        var policy = await _policyService.GetLatestTermsAndConditionsAsync();

        if (policy == null)
        {
            return NotFound(new { message = "Terms & Conditions not found." });
        }

        return Ok(policy);
    }

    [HttpPost("get-privacy-policy-history")]
    public async Task<IActionResult> GetPrivacyPolicyHistory()
    {
        // Only Admin can view history
        if (!HttpContext.IsAdmin())
        {
            return Unauthorized(new { message = "Only administrators can view policy history." });
        }

        var history = await _policyService.GetPrivacyPolicyHistoryAsync();
        return Ok(history);
    }

    [HttpPost("get-terms-and-conditions-history")]
    public async Task<IActionResult> GetTermsAndConditionsHistory()
    {
        // Only Admin can view history
        if (!HttpContext.IsAdmin())
        {
            return Unauthorized(new { message = "Only administrators can view policy history." });
        }

        var history = await _policyService.GetTermsAndConditionsHistoryAsync();
        return Ok(history);
    }

    [HttpPost("get-policy/{policyId}")]
    public async Task<IActionResult> GetPolicyById(Guid policyId)
    {
        // Only Admin can view specific policy versions
        if (!HttpContext.IsAdmin())
        {
            return Unauthorized(new { message = "Only administrators can view specific policy versions." });
        }

        var policy = await _policyService.GetPolicyByIdAsync(policyId);

        if (policy == null)
        {
            return NotFound(new { message = "Policy not found." });
        }

        return Ok(policy);
    }
}

