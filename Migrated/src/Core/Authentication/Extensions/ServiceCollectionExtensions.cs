using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using CreditTransfer.Core.Authentication.Configuration;
using CreditTransfer.Core.Authentication.Services;

namespace CreditTransfer.Core.Authentication.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Keycloak JWT authentication services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Keycloak options
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

        // Register token validation service
        services.AddScoped<ITokenValidationService, KeycloakTokenValidationService>();

        // Get Keycloak options for JWT configuration
        var keycloakOptions = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
        if (keycloakOptions == null)
        {
            throw new InvalidOperationException("Keycloak configuration is missing or invalid");
        }

        // Add JWT Bearer authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakOptions.Authority;
                options.Audience = keycloakOptions.Audience;
                options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                options.SaveToken = true;
                options.IncludeErrorDetails = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = keycloakOptions.ValidateIssuer,
                    ValidIssuer = keycloakOptions.Authority,
                    ValidateAudience = keycloakOptions.ValidateAudience,
                    ValidAudience = keycloakOptions.Audience,
                    ValidateLifetime = keycloakOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = keycloakOptions.ClockSkew,
                    NameClaimType = keycloakOptions.NameClaimType,
                    RoleClaimType = keycloakOptions.RoleClaimType
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogError(context.Exception, "JWT authentication failed");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        var username = context.Principal?.FindFirst("preferred_username")?.Value ?? "Unknown";
                        logger.LogDebug("JWT token validated successfully for user: {Username}", username);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("JWT authentication challenge: {Error} - {ErrorDescription}", 
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Adds Keycloak authorization policies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddKeycloakAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Credit Transfer specific policies
            options.AddPolicy("CreditTransferUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("realm_access"));

            options.AddPolicy("CreditTransferAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("credit-transfer-admin"));

            options.AddPolicy("CreditTransferOperator", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("credit-transfer-operator", "credit-transfer-admin"));

            options.AddPolicy("SystemUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("system-user", "credit-transfer-admin"));
        });

        return services;
    }

    /// <summary>
    /// Adds Keycloak authentication and authorization together
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddKeycloak(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddKeycloakAuthentication(configuration)
            .AddKeycloakAuthorization();
    }
} 