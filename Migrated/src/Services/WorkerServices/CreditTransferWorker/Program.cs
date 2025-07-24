using CreditTransfer.Services.WorkerService;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using CreditTransfer.Infrastructure.Configuration;

// Configure OpenTelemetry with comprehensive observability
const string serviceName = "CreditTransfer.WorkerService";
const string serviceVersion = "1.0.0";

var builder = Host.CreateApplicationBuilder(args);

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
        }))
    .WithMetrics(metrics => metrics
        .AddMeter(serviceName)
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddPrometheusExporter());

// Configure OpenTelemetry Logging (replace default logging)
builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(logging => logging
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName, serviceVersion))
    .AddConsoleExporter());

// Register all Credit Transfer services using centralized configuration
builder.Services.AddCreditTransferServices(builder.Configuration);

// Add the Worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
