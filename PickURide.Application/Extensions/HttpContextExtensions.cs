using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;

namespace PickURide.Application.Extensions;

public static class HttpContextExtensions
{
    public static Guid? GetUserId(this HttpContext context)
    {
        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }

    public static string? GetUserType(this HttpContext context)
    {
        // Check for Role claim first (Admin has Role claim)
        var roleClaim = context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(roleClaim))
        {
            return "Admin";
        }

        // If no role claim, determine UserType from the route path
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        if (path.Contains("/api/driver") || path.Contains("/api/drivers"))
        {
            return "Driver";
        }
        
        if (path.Contains("/api/user") || path.Contains("/api/users"))
        {
            return "User";
        }

        // If user is authenticated but we can't determine type, return null
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Default to User if authenticated but no specific route match
            return "User";
        }

        return null;
    }

    public static string? GetUserEmail(this HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static string? GetIpAddress(this HttpContext context)
    {
        // Check for forwarded IP first (when behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            return ips[0].Trim();
        }

        // Check for real IP
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to remote IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

    public static string? GetUserAgent(this HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault();
    }

    public static bool IsAdmin(this HttpContext context)
    {
        var role = context.User?.FindFirst(ClaimTypes.Role)?.Value;
        return role == "Admin" || role == "admin";
    }
}

