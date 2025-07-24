using CreditTransfer.Core.Authentication.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CreditTransfer.Core.Authentication.Services;

/// <summary>
/// Keycloak token validation service implementation
/// Enhanced with OpenTelemetry instrumentation for authentication operation tracking
/// </summary>
public class KeycloakTokenValidationService : ITokenValidationService
{
    private readonly KeycloakOptions _keycloakOptions;
    private readonly ILogger<KeycloakTokenValidationService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public KeycloakTokenValidationService(
        IOptions<KeycloakOptions> keycloakOptions,
        ILogger<KeycloakTokenValidationService> logger,
        ActivitySource activitySource)
    {
        _keycloakOptions = keycloakOptions.Value;
        _logger = logger;
        _activitySource = activitySource;
        _tokenHandler = new JwtSecurityTokenHandler();

        // Initialize the configuration manager for OpenID Connect discovery
        var metadataAddress = $"{_keycloakOptions.Authority}/.well-known/openid_configuration";
        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever())
        {
            AutomaticRefreshInterval = TimeSpan.FromHours(1),
            RefreshInterval = TimeSpan.FromMinutes(30)
        };

        _logger.LogInformation("Keycloak token validation service initialized with authority: {Authority}", 
            _keycloakOptions.Authority);
    }

    /// <summary>
    /// Validates a JWT token and returns the claims principal
    /// Enhanced with OpenTelemetry activity tracking
    /// </summary>
    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Authentication.ValidateToken");
        activity?.SetTag("operation", "ValidateToken");
        activity?.SetTag("service", "KeycloakTokenValidationService");
        activity?.SetTag("auth.provider", "Keycloak");
        activity?.SetTag("keycloak.authority", _keycloakOptions.Authority);
        activity?.SetTag("keycloak.audience", _keycloakOptions.Audience);
        
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Token is null or empty");
                activity?.SetTag("validation.result", "failed");
                activity?.SetTag("validation.error", "empty_token");
                _logger.LogWarning("Token validation failed: Token is null or empty");
                return null;
            }

            activity?.SetTag("token.length", token.Length);
            
            // Parse token for basic info without validation
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var issuer = jwtToken?.Issuer;
            var audience = jwtToken?.Audiences?.FirstOrDefault();
            var subject = jwtToken?.Subject;
            var expiration = jwtToken?.ValidTo;
            
            activity?.SetTag("token.issuer", issuer);
            activity?.SetTag("token.audience", audience);
            activity?.SetTag("token.subject", subject);
            activity?.SetTag("token.expiration", expiration?.ToString("O"));
            
            // Get the OpenID Connect configuration
            using var configActivity = _activitySource.StartActivity("Authentication.GetOpenIdConfiguration");
            var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
            if (configuration == null)
            {
                _logger.LogWarning("OpenID Connect configuration is null, cannot validate token");
                activity?.SetStatus(ActivityStatusCode.Error, "Configuration is null");
                activity?.SetTag("validation.result", "failed");
                activity?.SetTag("validation.error", "configuration_null");
                return null;
            }
            configActivity?.SetTag("config.issuer", configuration?.Issuer);
            configActivity?.SetTag("config.signing_keys.count", configuration?.SigningKeys?.Count ?? 0);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = _keycloakOptions.ValidateIssuer,
                ValidIssuer = _keycloakOptions.Authority,
                ValidateAudience = _keycloakOptions.ValidateAudience,
                ValidAudience = _keycloakOptions.Audience,
                ValidateLifetime = _keycloakOptions.ValidateLifetime,
                IssuerSigningKeys = configuration!.SigningKeys,
                ValidateIssuerSigningKey = true,
                ClockSkew = _keycloakOptions.ClockSkew,
                NameClaimType = _keycloakOptions.NameClaimType,
                RoleClaimType = _keycloakOptions.RoleClaimType
            };

            activity?.SetTag("validation.validate_issuer", _keycloakOptions.ValidateIssuer);
            activity?.SetTag("validation.validate_audience", _keycloakOptions.ValidateAudience);
            activity?.SetTag("validation.validate_lifetime", _keycloakOptions.ValidateLifetime);
            activity?.SetTag("validation.clock_skew", _keycloakOptions.ClockSkew.ToString());

            // Validate the token
            using var tokenValidationActivity = _activitySource.StartActivity("Authentication.PerformTokenValidation");
            var result = await _tokenHandler.ValidateTokenAsync(token, validationParameters);
            if(result == null || result.Claims == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Token validation result is null");
                activity?.SetTag("validation.result", "failed");
                activity?.SetTag("validation.error", "null_validation_result");
                _logger.LogWarning("Token validation failed: Result is null");
                return null;
            }
            if (result.IsValid)
            {
                var username = GetUsername(result.ClaimsIdentity);
                var roles = GetUserRoles(result.ClaimsIdentity);
                
                activity?.SetTag("validation.result", "success");
                activity?.SetTag("user.name", username);
                activity?.SetTag("user.roles.count", roles?.Count ?? 0);
                activity?.SetTag("claims.count", result.ClaimsIdentity?.Claims?.Count() ?? 0);
                
                tokenValidationActivity?.SetTag("validation.successful", true);
                tokenValidationActivity?.SetTag("user.authenticated", username);
                
                _logger.LogDebug("Token validation successful for user: {Username}", username);
                return new ClaimsPrincipal(result.ClaimsIdentity!);
            }
            else
            {
                var errorMessage = result.Exception?.Message ?? "Unknown validation error";
                activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                activity?.SetTag("validation.result", "failed");
                activity?.SetTag("validation.error", "token_invalid");
                activity?.SetTag("validation.error_message", errorMessage);
                
                tokenValidationActivity?.SetTag("validation.successful", false);
                tokenValidationActivity?.SetTag("validation.error", errorMessage);
                
                _logger.LogWarning("Token validation failed: {Exception}", errorMessage);
                return null;
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("validation.result", "failed");
            activity?.SetTag("validation.error", "exception");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            
            _logger.LogError(ex, "Exception during token validation");
            return null;
        }
    }

    /// <summary>
    /// Extracts user roles from claims identity
    /// </summary>
    private List<string> GetUserRoles(ClaimsIdentity claimsIdentity)
    {
        return claimsIdentity?.FindAll(_keycloakOptions.RoleClaimType)
            ?.Select(c => c.Value)
            ?.ToList() ?? new List<string>();
    }

    public string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;

        const string bearerPrefix = "Bearer ";
        if (authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorizationHeader.Substring(bearerPrefix.Length).Trim();
        }

        return null;
    }

    public bool HasRole(ClaimsPrincipal principal, string role)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return false;

        // Check for realm roles in Keycloak format
        var realmAccessClaim = principal.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            // Parse the realm_access claim which contains roles in JSON format
            try
            {
                var realmAccess = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(realmAccessClaim.Value);
                if (realmAccess?.ContainsKey("roles") == true)
                {
                    var roles = System.Text.Json.JsonSerializer.Deserialize<string[]>(realmAccess["roles"].ToString() ?? "[]");
                    return roles?.Contains(role, StringComparer.OrdinalIgnoreCase) == true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing realm_access claim for role validation");
            }
        }

        // Fallback to standard role claims
        return principal.IsInRole(role);
    }

    public string? GetUserId(ClaimsPrincipal principal)
    {
        return principal?.FindFirst("sub")?.Value ?? 
               principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetUsername(ClaimsPrincipal principal)
    {
        return principal?.FindFirst("preferred_username")?.Value ?? 
               principal?.FindFirst(_keycloakOptions.NameClaimType)?.Value ??
               principal?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public string? GetUsername(ClaimsIdentity identity)
    {
        return identity?.FindFirst("preferred_username")?.Value ?? 
               identity?.FindFirst(_keycloakOptions.NameClaimType)?.Value ??
               identity?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public IEnumerable<string> GetRoles(ClaimsPrincipal principal)
    {
        var roles = new List<string>();

        if (principal?.Identity?.IsAuthenticated != true)
            return roles;

        // Get realm roles from Keycloak
        var realmAccessClaim = principal.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            try
            {
                var realmAccess = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(realmAccessClaim.Value);
                if (realmAccess?.ContainsKey("roles") == true)
                {
                    var realmRoles = System.Text.Json.JsonSerializer.Deserialize<string[]>(realmAccess["roles"].ToString() ?? "[]");
                    if (realmRoles != null)
                        roles.AddRange(realmRoles);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing realm_access claim for roles");
            }
        }

        // Get standard role claims
        var standardRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        roles.AddRange(standardRoles);

        return roles.Distinct();
    }
} 