using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PickURide.Application.Interfaces.Repositories;
using PickURide.Application.Interfaces.Services;
using PickURide.Application.Middleware;
using PickURide.Infrastructure.Data;
using PickURide.Infrastructure.Hub;
using PickURide.Infrastructure.Hubs;
using PickURide.Infrastructure.Repositories;
using PickURide.Infrastructure.Services;
using PickURide.Infrastructure.Services.Background;
using System.Text;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Log startup
    Console.WriteLine("=== Starting PickURide API ===");
    Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

    // Get connection string
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("ERROR: DefaultConnection is not configured in appsettings.json");
        throw new InvalidOperationException("Database connection string is missing");
    }
    Console.WriteLine("✓ Connection string found");

    // Add DbContext
    builder.Services.AddDbContext<PickURideDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
        options.EnableDetailedErrors(builder.Environment.IsDevelopment());
    });

    // Register repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IDriverRepository, DriverRepository>();
    builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
    builder.Services.AddScoped<IFareSettingRepository, FareSettingRepository>();
    builder.Services.AddScoped<IAdminRepository, AdminRepository>();
    builder.Services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();
    builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
    builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
    builder.Services.AddScoped<ITipRepository, TipRepository>();
    builder.Services.AddScoped<IRideMessageRepository, RideMessageRepository>();
        builder.Services.AddScoped<ISupportChatRepository, SupportChatRepository>();
    builder.Services.AddScoped<PickURide.Application.Interfaces.Repositories.IAuditLogRepository, PickURide.Infrastructure.Repositories.AuditLogRepository>();
    builder.Services.AddScoped<PickURide.Application.Interfaces.Repositories.IPolicyRepository, PickURide.Infrastructure.Repositories.PolicyRepository>();
    builder.Services.AddScoped<IPromoRepository, PromoRepository>();

    // Register services
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IDriverService, DriverService>();
    builder.Services.AddScoped<IShiftService, ShiftService>();
    builder.Services.AddScoped<IFareSettingService, FareSettingService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
    builder.Services.AddScoped<IDriverAttendanceService, DriverAttendanceService>();
    builder.Services.AddScoped<IEmailOTPService, EmailOTPService>();
    builder.Services.AddScoped<IDriverLocationService, DriverLocationService>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IRideService, RideService>();
    builder.Services.AddScoped<IRideChatCacheService, RideChatCacheService>();
    builder.Services.AddScoped<IStripeService, StripeService>();
    builder.Services.AddScoped<IAuditLogService, PickURide.Infrastructure.Services.AuditLogService>();
    builder.Services.AddScoped<IPolicyService, PickURide.Infrastructure.Services.PolicyService>();

    // Register infrastructure services
    builder.Services.AddMemoryCache();
    builder.Services.AddHttpClient();
    builder.Services.AddSignalR();
    builder.Services.AddHostedService<DriverLocationFlushService>();

    Console.WriteLine("✓ Services registered");

    // Get JWT configuration
    var jwtKey = builder.Configuration["Jwt:Key"];
    var jwtIssuer = builder.Configuration["Jwt:Issuer"];
    var jwtAudience = builder.Configuration["Jwt:Audience"];

    // Validate JWT configuration
    if (string.IsNullOrEmpty(jwtKey))
    {
        Console.WriteLine("ERROR: Jwt:Key is not configured in appsettings.json");
        Console.WriteLine("Add this to your appsettings.json:");
        Console.WriteLine(@"{
  ""Jwt"": {
    ""Key"": ""YourSuperSecretKeyThatIsAtLeast32CharactersLong!!!"",
    ""Issuer"": ""PickURide"",
    ""Audience"": ""PickURideAPI""
  }
}");
        throw new InvalidOperationException("JWT Key is missing from configuration");
    }

    if (jwtKey.Length < 32)
    {
        Console.WriteLine($"WARNING: JWT Key is too short ({jwtKey.Length} characters). Should be at least 32 characters.");
    }

    Console.WriteLine("✓ JWT configuration validated");

    // Configure JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false; // Set to true in production with HTTPS
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Configure SignalR token handling
        // SignalR WebSocket connections pass the token via query string, not Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // For SignalR connections, token can be in query string
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                // If the request is for a SignalR hub and has token in query string, use it
                if (!string.IsNullOrEmpty(accessToken) && 
                    (path.StartsWithSegments("/ridechathub") || 
                     path.StartsWithSegments("/locationhub") || 
                     path.StartsWithSegments("/rideHub")))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    Console.WriteLine($"Auth failed: {context.Exception.Message}");
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();
    Console.WriteLine("✓ Authentication configured");

    // Add Controllers
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.AddEndpointsApiExplorer();

    // Configure Swagger
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "PickURide API",
            Version = "v1",
            Description = "PickURide API Documentation"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your token"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    Console.WriteLine("✓ Swagger configured");

    // Configure CORS - Important for SmarterASP.NET and SignalR with credentials
    // When using credentials (JWT tokens), we must specify explicit origins (not AllowAnyOrigin)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    "http://pickurides.com",
                    "https://pickurides.com",
                    "http://admin.pickurides.com",
                    "https://admin.pickurides.com",
                    "http://home.pickurides.com",
                    "https://home.pickurides.com",
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:5000",
                    "http://localhost:3000",
                    "http://127.0.0.1:4200",
                    "http://127.0.0.1:5000",
                    "http://127.0.0.1:3000",
                    "http://localhost:5192",  // API default port
                    "https://localhost:7008", // API HTTPS port
                    "file://"                 // Allow local file access for testing HTML files
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for SignalR with JWT tokens (accessTokenFactory)
        });
    });

    // Configure forwarded headers for reverse proxy (SmarterASP.NET requirement)
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                                   Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    Console.WriteLine("✓ CORS configured");

    // Build application
    Console.WriteLine("Building application...");
    var app = builder.Build();

    Console.WriteLine("✓ Application built successfully");

    // Configure middleware pipeline
    Console.WriteLine("Configuring middleware pipeline...");

    // Forwarded headers MUST be first (for SmarterASP.NET reverse proxy)
    app.UseForwardedHeaders();

    // Development-specific middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        Console.WriteLine("✓ Developer exception page enabled");
    }

    // CORS
    app.UseCors();

    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PickURide API V1");
        c.RoutePrefix = "swagger";
    });
    Console.WriteLine("✓ Swagger UI available at /swagger");

    // Static files
    app.UseStaticFiles();

    // Routing
    app.UseRouting();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Custom middleware
    try
    {
        app.UseMiddleware<PickURide.Application.Middleware.AuditLogMiddleware>();
        Console.WriteLine("✓ AuditLogMiddleware registered");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Could not register AuditLogMiddleware: {ex.Message}");
    }

    // Map endpoints
    app.MapControllers();
    app.MapHub<DriverHub>("/locationhub");
    app.MapHub<RideChatHub>("/ridechathub");
    app.MapHub<RideChatHub>("/rideHub");

    Console.WriteLine("✓ Endpoints mapped");
    Console.WriteLine("=== PickURide API Started Successfully ===");
    Console.WriteLine($"Listening on: {string.Join(", ", app.Urls)}");
    Console.WriteLine("Press Ctrl+C to shut down");

    // Seed database in background (non-blocking)
    _ = Task.Run(async () =>
    {
        await Task.Delay(3000);
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PickURideDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            if (await context.Database.CanConnectAsync())
            {
                var adminRepo = scope.ServiceProvider.GetRequiredService<IAdminRepository>();
                var admin = await adminRepo.GetByEmailAsync("admin@picku.com");

                if (admin == null)
                {
                    var newAdmin = new PickURide.Application.Models.AdminModel
                    {
                        FullName = "Admin",
                        Email = "admin@picku.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("12345"),
                        Role = "Admin",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await adminRepo.CreateAsync(newAdmin);
                    logger.LogInformation("✓ Default admin created: admin@picku.com / 12345");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database seeding skipped: {ex.Message}");
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("=== FATAL ERROR ===");
    Console.WriteLine($"Type: {ex.GetType().Name}");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");

    if (ex.InnerException != null)
    {
        Console.WriteLine("\n=== Inner Exception ===");
        Console.WriteLine($"Type: {ex.InnerException.GetType().Name}");
        Console.WriteLine($"Message: {ex.InnerException.Message}");
        Console.WriteLine($"Stack Trace:\n{ex.InnerException.StackTrace}");
    }

    Console.WriteLine("\n=== Troubleshooting ===");
    Console.WriteLine("1. Check your appsettings.json exists and has valid JSON");
    Console.WriteLine("2. Verify ConnectionStrings:DefaultConnection is configured");
    Console.WriteLine("3. Verify Jwt:Key, Jwt:Issuer, and Jwt:Audience are configured");
    Console.WriteLine("4. Ensure SQL Server is running and accessible");
    Console.WriteLine("5. Check that all referenced projects and packages are built");

    throw;
}