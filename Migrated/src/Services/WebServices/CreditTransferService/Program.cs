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

// Add Keycloak authentication and authorization
builder.Services.AddKeycloak(builder.Configuration);

// Register additional services (ICreditTransferService, repositories, etc. are registered by AddCreditTransferServices)
builder.Services.AddScoped<ITokenValidationService, KeycloakTokenValidationService>();

// Add WCF service implementation - register both interface and concrete class
builder.Services.AddScoped<ICreditTransferWcfService, CreditTransferWcfService>();
builder.Services.AddScoped<CreditTransferWcfService>(); // CoreWCF needs the concrete class registered directly

var app = builder.Build();

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
        
        //// Add service provider injection behavior
        //serviceOptions.Description.Behaviors.Add(new ServiceProviderInjectionBehavior(app.Services));
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
