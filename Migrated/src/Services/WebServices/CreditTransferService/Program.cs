using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Authorization;
using CreditTransfer.Infrastructure.Configuration;
using CreditTransfer.Core.Authentication.Extensions;
using CreditTransfer.Services.WcfService;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Infrastructure.Repositories;
using CreditTransfer.Core.Authentication.Services;
using CreditTransfer.Core.Authentication.Models;
using CreditTransfer.Core.Authentication.Configuration;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry with comprehensive observability
const string serviceName = "CreditTransfer.WcfService";
const string serviceVersion = "1.0.0";

// Create ActivitySource for custom tracing
var activitySource = new ActivitySource(serviceName);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
        .AddAttributes(new Dictionary<string, object>
        {
            ["service.name"] = serviceName,
            ["service.version"] = serviceVersion,
            ["service.instance.id"] = Environment.MachineName,
            ["deployment.environment"] = builder.Environment.EnvironmentName
        }))
    .WithTracing(tracing => tracing
        .AddSource(serviceName)
        .AddSource("CreditTransfer.Core.Business")
        .AddSource("CreditTransfer.Services.WcfService")
        .AddSource("CreditTransfer.Core")
        .AddSource("IntegrationProxies.Nobill")
        .AddSource("CreditTransfer.WcfService.Authentication")
        .SetSampler(new TraceIdRatioBasedSampler(1.0)) // Enable all traces for debugging
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext =>
            {
                // Don't trace health check endpoints to reduce noise
                return !httpContext.Request.Path.StartsWithSegments("/health");
            };
        })
        .AddHttpClientInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.RecordException = true;
        })
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            // Configure OTLP exporter for Jaeger (when running in Docker)
            var jaegerEndpoint = builder.Configuration["Jaeger:Endpoint"] ?? "http://jaeger:4317";
            options.Endpoint = new Uri(jaegerEndpoint);
            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            
            // Fix span ID validation issues
            options.Headers = ""; // Clear any problematic headers
            options.TimeoutMilliseconds = 10000; // 10 second timeout
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(serviceName)
        .AddMeter("CreditTransfer.Core.Business") // Add business logic metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

// Configure OpenTelemetry Logging (replace Microsoft.Extensions.Logging)
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logging => logging
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName, serviceVersion))
    .AddConsoleExporter());

// Configure logging levels to reduce authentication debug noise
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Warning);

// Register ActivitySource for dependency injection
builder.Services.AddSingleton(activitySource);

// Add services to the container
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

// Add Credit Transfer services
builder.Services.AddCreditTransferServices(builder.Configuration);

// Add HTTP client factory for authentication services
builder.Services.AddHttpClient();

// Add Keycloak authentication and authorization
builder.Services.AddKeycloak(builder.Configuration);

// Configure authentication options for Basic auth
builder.Services.Configure<BasicAuthenticationOptions>(builder.Configuration.GetSection("BasicAuthentication"));
builder.Services.Configure<RoleMappingOptions>(builder.Configuration.GetSection("RoleMapping"));
builder.Services.Configure<SecuritySettingsOptions>(builder.Configuration.GetSection("SecuritySettings"));

// Register additional services (ICreditTransferService, repositories, etc. are registered by AddCreditTransferServices)
builder.Services.AddScoped<ITokenValidationService, KeycloakTokenValidationService>();

// Register authentication dependencies
builder.Services.AddScoped<IKeycloakServiceAccountClient, KeycloakServiceAccountClient>();
builder.Services.AddSingleton<IAuthenticationCache, InMemoryAuthenticationCache>();
builder.Services.AddSingleton<IFailedAttemptTracker, InMemoryFailedAttemptTracker>();
builder.Services.AddScoped<IBasicAuthenticationService, KeycloakBasicAuthenticationService>();

// Add WCF service implementation - register both interface and concrete class
builder.Services.AddScoped<ICreditTransferWcfService, CreditTransferWcfService>();
builder.Services.AddScoped<CreditTransferWcfService>(); // CoreWCF needs the concrete class registered directly

var app = builder.Build();

// Set service provider for Basic authentication behavior
WcfBasicAuthenticationBehavior.SetServiceProvider(app.Services);

// Configure CoreWCF with JWT authentication support
app.UseServiceModel(builder =>
{
    // Create BasicHttpBinding with explicit configuration
    var binding = new BasicHttpBinding()
    {
        // Ensure we accept text/xml content type
        MessageEncoding = WSMessageEncoding.Text,
        // Set max message size to handle larger requests if needed
        MaxReceivedMessageSize = 65536,
        MaxBufferSize = 65536
    };
    
    // Add the Credit Transfer WCF service with HTTP binding only (for Docker compatibility)
    builder.AddService<CreditTransferWcfService>((serviceOptions) => {
        // Enable detailed exception information in faults for debugging
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = true;
        
        //// Add JWT authentication behavior to the service
        //serviceOptions.Description.Behaviors.Add(new WcfJwtAuthenticationBehavior(requireAuthentication: false));
    })
           .AddServiceEndpoint<CreditTransferWcfService, ICreditTransferWcfService>(binding, "/CreditTransferService.svc");
});

// Add a health check endpoint
app.MapGet("/health", () => "Credit Transfer WCF Service is running");

// JWT Authentication test endpoint for debugging
app.MapGet("/test-auth", async (HttpContext context, ITokenValidationService tokenService) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader))
    {
        return Results.BadRequest("No Authorization header provided");
    }
    
    var token = tokenService.ExtractBearerToken(authHeader);
    if (string.IsNullOrEmpty(token))
    {
        return Results.BadRequest("Invalid Bearer token format");
    }
    
    try
    {
        var principal = await tokenService.ValidateTokenAsync(token);
        if (principal == null)
        {
            return Results.Unauthorized();
        }
        
        var username = tokenService.GetUsername(principal);
        var roles = tokenService.GetRoles(principal);
        
        return Results.Ok(new {
            Message = "JWT authentication successful",
            Username = username,
            Roles = roles.ToArray(),
            TokenValid = true,
            Timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new {
            Message = "JWT authentication failed",
            Error = ex.Message,
            TokenValid = false,
            Timestamp = DateTime.UtcNow
        });
    }
});

// Prometheus metrics endpoint - Standard implementation
app.MapPrometheusScrapingEndpoint();

// Add service metadata endpoint with observability info
app.MapGet("/", () => new {
    Service = "Credit Transfer WCF Service", 
    Version = "1.0.0",
    Status = "Running",
    SoapEndpoint = "/CreditTransferService.svc",
    Authentication = new {
        Type = "JWT Bearer Token",
        KeycloakIntegration = true,
        TestEndpoint = "/test-auth",
        AuthHeaderFormat = "Authorization: Bearer {token}",
        AlternativeHeader = "X-Authorization: Bearer {token}"
    },
    Observability = new {
        Tracing = "OpenTelemetry -> Jaeger",
        Metrics = "Prometheus",
        MetricsEndpoint = "/metrics",
        JaegerEndpoint = "http://localhost:16686"
    },
    Timestamp = DateTime.UtcNow
});

app.Run();

// Simple in-memory implementations for authentication services
public class InMemoryAuthenticationCache : IAuthenticationCache
{
    private readonly Dictionary<string, AuthenticationResult> _cache = new();

    public Task<AuthenticationResult?> GetAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(cacheKey, out var result);
        return Task.FromResult(result);
    }

    public Task SetAsync(string cacheKey, AuthenticationResult result, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        _cache[cacheKey] = result;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        _cache.Remove(cacheKey);
        return Task.CompletedTask;
    }

    public Task ClearUserCacheAsync(string username, CancellationToken cancellationToken = default)
    {
        var keysToRemove = _cache.Keys.Where(k => k.Contains(username)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, object>> GetStatisticsAsync()
    {
        return Task.FromResult(new Dictionary<string, object>
        {
            ["CacheSize"] = _cache.Count
        });
    }

    public string GenerateCacheKey(string username, string passwordHash)
    {
        return $"auth:{username}:{passwordHash}";
    }

    public string GenerateUserInfoCacheKey(string username)
    {
        return $"userinfo:{username}";
    }
}

public class InMemoryFailedAttemptTracker : IFailedAttemptTracker
{
    private readonly Dictionary<string, List<FailedAttempt>> _failedAttempts = new();
    private readonly Dictionary<string, DateTime> _lockouts = new();

    public Task RecordFailedAttemptAsync(string username, string? ipAddress, AuthenticationFailureReason reason, CancellationToken cancellationToken = default)
    {
        if (!_failedAttempts.ContainsKey(username))
            _failedAttempts[username] = new List<FailedAttempt>();

        _failedAttempts[username].Add(new FailedAttempt
        {
            Username = username,
            IpAddress = ipAddress,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task<int> GetFailedAttemptCountAsync(string username, CancellationToken cancellationToken = default)
    {
        if (!_failedAttempts.ContainsKey(username))
            return Task.FromResult(0);

        var recentAttempts = _failedAttempts[username]
            .Where(a => a.Timestamp > DateTime.UtcNow.AddHours(-1))
            .Count();

        return Task.FromResult(recentAttempts);
    }

    public Task<bool> IsUserLockedOutAsync(string username, CancellationToken cancellationToken = default)
    {
        if (!_lockouts.ContainsKey(username))
            return Task.FromResult(false);

        return Task.FromResult(_lockouts[username] > DateTime.UtcNow);
    }

    public Task ResetFailedAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        _failedAttempts.Remove(username);
        _lockouts.Remove(username);
        return Task.CompletedTask;
    }

    public Task<DateTime?> GetLockoutEndTimeAsync(string username, CancellationToken cancellationToken = default)
    {
        if (!_lockouts.ContainsKey(username))
            return Task.FromResult<DateTime?>(null);

        return Task.FromResult<DateTime?>(_lockouts[username]);
    }

    public Task CleanupExpiredLockoutsAsync(CancellationToken cancellationToken = default)
    {
        var expiredUsers = _lockouts.Where(l => l.Value <= DateTime.UtcNow).Select(l => l.Key).ToList();
        foreach (var user in expiredUsers)
        {
            _lockouts.Remove(user);
        }
        return Task.CompletedTask;
    }

    private class FailedAttempt
    {
        public string Username { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public AuthenticationFailureReason Reason { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
