using CreditTransfer.Infrastructure.Configuration;
using CreditTransfer.Core.Authentication.Extensions;
using Microsoft.AspNetCore.Authorization;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Authentication.Services;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenTelemetry with comprehensive observability
const string serviceName = "CreditTransfer.RestApi";
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
        .AddSource("CreditTransfer.Services.RestApi")
        .AddSource("CreditTransfer.Services.RestApi.Controllers")
        .AddSource("CreditTransfer.Core")
        .AddSource("IntegrationProxies.Nobill")
        .AddSource("CreditTransfer.WcfService.Authentication")
        .SetSampler(new TraceIdRatioBasedSampler(1.0)) // Enable all traces for debugging
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext =>
            {
                // Only exclude basic health check endpoints to reduce noise
                // But include /health/system for comprehensive diagnostics tracing
                var path = httpContext.Request.Path.Value?.ToLowerInvariant() ?? "";
                return !path.Equals("/health") && 
                       !path.Equals("/health/ready") && 
                       !path.Equals("/health/live");
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
        .AddMeter("CreditTransfer.Services.RestApi.Controllers") // Add controller metrics
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
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // For better JSON serialization

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Credit Transfer API", 
        Version = "v1",
        Description = "Modern REST API for Credit Transfer operations with Keycloak authentication and OpenTelemetry observability"
    });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add Credit Transfer services
builder.Services.AddCreditTransferServices(builder.Configuration);

// Add Keycloak authentication and authorization
builder.Services.AddKeycloak(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register additional services (ICreditTransferService, repositories, etc. are registered by AddCreditTransferServices)
builder.Services.AddScoped<ITokenValidationService, KeycloakTokenValidationService>();

// Register Meter as singleton for OpenTelemetry metrics (ActivitySource already registered above)
builder.Services.AddSingleton(new Meter("CreditTransfer.Services.RestApi.Controllers", "1.0.0"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Credit Transfer API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

// Prometheus metrics endpoint - Standard implementation with anonymous access
app.MapPrometheusScrapingEndpoint();

// Root endpoint with observability info
app.MapGet("/", [AllowAnonymous] () => new { 
    Service = "Credit Transfer REST API", 
    Version = "1.0.0",
    Status = "Running",
    Authentication = "Keycloak JWT",
    Observability = new {
        Tracing = "OpenTelemetry -> Jaeger",
        Metrics = "Prometheus",
        MetricsEndpoint = "/metrics",
        JaegerEndpoint = "http://localhost:16686"
    },
    Timestamp = DateTime.UtcNow 
});

app.Run();

// Make Program class accessible for testing
public partial class Program { }
