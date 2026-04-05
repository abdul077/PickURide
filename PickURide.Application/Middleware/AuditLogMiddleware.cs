using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PickURide.Application.Extensions;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PickURide.Application.Middleware;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private const int MaxRequestSize = 10240; // 10KB
    private const int MaxResponseSize = 10240; // 10KB

    // Routes to skip logging
    private static readonly string[] SkipRoutes = new[]
    {
        "/swagger",
        "/health",
        "/api/AuditLog",
        "/favicon.ico"
    };

    public AuditLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        // Skip logging for certain routes
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Only log selected high-value events to avoid noise
        var currentPath = context.Request.Path;
        var currentMethod = context.Request.Method;
        var currentAction = DetermineAction(currentMethod, currentPath);
        var currentEntity = DetermineEntityType(currentPath);
        if (!ShouldLogEvent(currentAction, currentEntity, currentPath))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = string.Empty;
        var responseBody = string.Empty;
        var originalResponseBodyStream = context.Response.Body;
        var statusCode = 200;
        var errorMessage = (string?)null;

        try
        {
            // Capture request body
            if (context.Request.ContentLength > 0 && context.Request.ContentLength <= MaxRequestSize)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // Capture response body
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                statusCode = 500;
                errorMessage = ex.Message;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                statusCode = context.Response.StatusCode;

                // Capture response body
                if (responseBodyStream.Length > 0 && responseBodyStream.Length <= MaxResponseSize)
                {
                    responseBodyStream.Position = 0;
                    responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                    responseBodyStream.Position = 0;
                    await responseBodyStream.CopyToAsync(originalResponseBodyStream);
                }
                else
                {
                    await responseBodyStream.CopyToAsync(originalResponseBodyStream);
                }
            }
        }
        catch (Exception ex)
        {
            statusCode = context.Response.StatusCode != 200 ? context.Response.StatusCode : 500;
            errorMessage = ex.Message;
        }
        finally
        {
            context.Response.Body = originalResponseBodyStream;

            // Capture all context data before async operation to avoid disposal issues
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;
            var capturedUserId = context.GetUserId();
            var capturedUserType = context.GetUserType();
            var capturedIpAddress = context.GetIpAddress();
            var capturedUserAgent = context.GetUserAgent();
            var capturedDuration = stopwatch.ElapsedMilliseconds;
            var requestServices = context.RequestServices;

            // Log the action asynchronously without blocking the response
            // Use a scoped service provider to ensure DbContext is available
            _ = Task.Run(async () =>
            {
                try
                {
                    // Create a new scope for the background task
                    using var scope = requestServices.CreateScope();
                    var scopedAuditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                    await LogActionAsync(
                        path, 
                        method, 
                        scopedAuditLogService, 
                        requestBody, 
                        responseBody, 
                        statusCode, 
                        errorMessage, 
                        capturedDuration,
                        capturedUserId,
                        capturedUserType,
                        capturedIpAddress,
                        capturedUserAgent);
                }
                catch (Exception ex)
                {
                    // Log error to console for debugging (but don't break the application)
                    Console.WriteLine($"[AuditLogMiddleware] Error in background logging task: {ex.Message}");
                    Console.WriteLine($"[AuditLogMiddleware] Stack trace: {ex.StackTrace}");
                }
            });
        }
    }

    private static bool ShouldSkipLogging(PathString path)
    {
        var pathString = path.Value?.ToLowerInvariant() ?? string.Empty;
        return SkipRoutes.Any(route => pathString.StartsWith(route.ToLowerInvariant()));
    }

    private static bool ShouldLogEvent(string action, string? entityType, PathString path)
    {
        var pathLower = path.Value?.ToLowerInvariant() ?? string.Empty;
        var hasCrudVerb = pathLower.Contains("create") || pathLower.Contains("update") || pathLower.Contains("delete") ||
                          pathLower.Contains("approve") || pathLower.Contains("reject") || pathLower.Contains("apply");
        var isDeleteAction = string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase);

        // Auth: login/logout
        if (pathLower.Contains("login") || pathLower.Contains("logout"))
            return true;

        // Ride assignments / ride create-update
        if (pathLower.Contains("/ride") && (hasCrudVerb || pathLower.Contains("assign") || isDeleteAction))
            return true;

        // Profile updates
        if (pathLower.Contains("profile") && (hasCrudVerb || isDeleteAction))
            return true;

        // Admin & app CRUD (admin, user, driver, fare, payment, shift, feedback)
        if ((hasCrudVerb || isDeleteAction) &&
            (pathLower.Contains("/admin") || pathLower.Contains("/user") || pathLower.Contains("/users") ||
             pathLower.Contains("/driver") || pathLower.Contains("/drivers") ||
             pathLower.Contains("/fare") || pathLower.Contains("/payment") ||
             pathLower.Contains("/shift") || pathLower.Contains("/feedback")))
            return true;

        // Default: do not log (covers data-loading POSTs like get-* endpoints)
        return false;
    }

    private static async Task LogActionAsync(
        string path,
        string method,
        IAuditLogService auditLogService,
        string requestBody,
        string responseBody,
        int statusCode,
        string? errorMessage,
        long duration,
        Guid? capturedUserId,
        string? capturedUserType,
        string? capturedIpAddress,
        string? capturedUserAgent)
    {
        try
        {
            var pathLower = path.ToLowerInvariant();
            var userId = capturedUserId;
            var userType = capturedUserType;

            // Always try to determine userType from route if not already set (important for login/logout)
            if (string.IsNullOrEmpty(userType))
            {
                if (pathLower.Contains("/api/driver") || pathLower.Contains("/api/drivers"))
                {
                    userType = "Driver";
                }
                else if (pathLower.Contains("/api/admin"))
                {
                    userType = "Admin";
                }
                else if (pathLower.Contains("/api/user") || pathLower.Contains("/api/users"))
                {
                    userType = "User";
                }
                else
                {
                    userType = "Anonymous"; // Default for unknown routes
                }
            }

            // For login endpoints, extract UserId from response body if login was successful
            if (pathLower.Contains("login") && statusCode == 200 && !string.IsNullOrEmpty(responseBody))
            {
                try
                {
                    var responseJson = JsonDocument.Parse(responseBody);
                    // Try different property names for userId
                    if (responseJson.RootElement.TryGetProperty("userId", out var userIdElement))
                    {
                        if (userIdElement.ValueKind == JsonValueKind.String)
                        {
                            var userIdString = userIdElement.GetString();
                            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var parsedUserId) && parsedUserId != Guid.Empty)
                            {
                                userId = parsedUserId;
                            }
                        }
                    }
                    // Also check for UserId (capital U)
                    else if (responseJson.RootElement.TryGetProperty("UserId", out var userIdElement2))
                    {
                        if (userIdElement2.ValueKind == JsonValueKind.String)
                        {
                            var userIdString = userIdElement2.GetString();
                            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var parsedUserId) && parsedUserId != Guid.Empty)
                            {
                                userId = parsedUserId;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If parsing fails, log for debugging but continue with null userId
                    Console.WriteLine($"[AuditLogMiddleware] Error parsing login response: {ex.Message}");
                    var previewLength = Math.Min(200, responseBody?.Length ?? 0);
                    Console.WriteLine($"[AuditLogMiddleware] Response body: {responseBody?.Substring(0, previewLength)}");
                }
            }

            // Determine action from HTTP method and route
            var action = DetermineAction(method, new PathString(path));

            // Determine entity type from route
            var entityType = DetermineEntityType(new PathString(path));

            // Determine entity ID from route path (extract from URL pattern)
            var entityId = ExtractEntityIdFromPath(path);

            // Serialize request/response data (limit size)
            var requestData = LimitSize(SerializeJson(requestBody), MaxRequestSize);
            var responseData = LimitSize(SerializeJson(responseBody), MaxResponseSize);

            var status = statusCode >= 200 && statusCode < 300 ? "Success" :
                        statusCode >= 400 && statusCode < 500 ? "Failed" : "Error";

            var auditLogRequest = new AuditLogRequest
            {
                UserId = userId,
                UserType = userType ?? "Anonymous",
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                RequestData = requestData,
                ResponseData = responseData,
                IpAddress = capturedIpAddress,
                UserAgent = capturedUserAgent,
                Status = status,
                ErrorMessage = errorMessage,
                Duration = (int)duration
            };

            await auditLogService.LogActionAsync(auditLogRequest);
            
            // Log success for debugging (can be removed in production)
            Console.WriteLine($"[AuditLogMiddleware] Successfully logged action: {auditLogRequest.Action} for user: {auditLogRequest.UserId} ({auditLogRequest.UserType}) - Entity: {auditLogRequest.EntityType}, EntityId: {auditLogRequest.EntityId}");
        }
        catch (Exception ex)
        {
            // Log error to console for debugging (but don't break the application)
            Console.WriteLine($"[AuditLogMiddleware] Error logging action: {ex.Message}");
            Console.WriteLine($"[AuditLogMiddleware] Path: {path}, Method: {method}, Status: {statusCode}");
            Console.WriteLine($"[AuditLogMiddleware] Stack trace: {ex.StackTrace}");
        }
    }

    private static Guid? ExtractEntityIdFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Extract GUID from path patterns like:
        // /api/Drivers/delete-driver-admin/{guid}
        // /api/Admin/get-admin-by-id/{guid}
        // /api/User/{guid}
        var guidPattern = @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})";
        var regex = new System.Text.RegularExpressions.Regex(guidPattern);
        var match = regex.Match(path);
        
        if (match.Success && Guid.TryParse(match.Value, out var guidId))
        {
            return guidId;
        }

        return null;
    }

    private static string DetermineAction(string method, PathString path)
    {
        var pathString = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Map HTTP methods to actions
        return method.ToUpperInvariant() switch
        {
            "GET" => pathString.Contains("get") ? "View" : "Get",
            "POST" => DeterminePostAction(pathString),
            "PUT" => "Update",
            "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => method
        };
    }

    private static string DeterminePostAction(string path)
    {
        if (path.Contains("login")) return "Login";
        if (path.Contains("logout")) return "Logout";
        if (path.Contains("register")) return "Register";
        if (path.Contains("create")) return "Create";
        if (path.Contains("update")) return "Update";
        if (path.Contains("delete")) return "Delete";
        if (path.Contains("approve")) return "Approve";
        if (path.Contains("reject")) return "Reject";
        if (path.Contains("apply")) return "Apply";
        return "Post";
    }

    private static string? DetermineEntityType(PathString path)
    {
        var pathString = path.Value?.ToLowerInvariant() ?? string.Empty;

        if (pathString.Contains("/admin")) return "Admin";
        if (pathString.Contains("/driver")) return "Driver";
        if (pathString.Contains("/user")) return "User";
        if (pathString.Contains("/ride")) return "Ride";
        if (pathString.Contains("/shift")) return "Shift";
        if (pathString.Contains("/payment")) return "Payment";
        if (pathString.Contains("/fare")) return "FareSetting";
        if (pathString.Contains("/feedback")) return "Feedback";

        return null;
    }


    private static string? SerializeJson(string? data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return null;
        }

        try
        {
            // Try to parse and re-serialize to ensure valid JSON
            var jsonDoc = JsonDocument.Parse(data);
            return JsonSerializer.Serialize(jsonDoc);
        }
        catch
        {
            // If not valid JSON, return as-is (truncated)
            return data;
        }
    }

    private static string? LimitSize(string? data, int maxSize)
    {
        if (string.IsNullOrEmpty(data) || data.Length <= maxSize)
        {
            return data;
        }

        return data.Substring(0, maxSize) + "... [truncated]";
    }
}

