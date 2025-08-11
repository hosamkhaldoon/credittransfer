namespace CreditTransfer.Core.Authentication.Configuration;

/// <summary>
/// Configuration options for Basic Authentication
/// </summary>
public class BasicAuthenticationOptions
{
    public const string SectionName = "BasicAuthentication";

    /// <summary>
    /// Authentication realm name shown in WWW-Authenticate header
    /// </summary>
    public string Realm { get; set; } = "CreditTransfer API";

    /// <summary>
    /// Enable authentication result caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum failed authentication attempts before lockout
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Account lockout duration in minutes
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Minimum password length requirement
    /// </summary>
    public int PasswordMinLength { get; set; } = 8;

    /// <summary>
    /// Require strong password validation
    /// </summary>
    public bool RequireStrongPasswords { get; set; } = false;

    /// <summary>
    /// Log all authentication attempts
    /// </summary>
    public bool LogAuthenticationAttempts { get; set; } = true;

    /// <summary>
    /// Enable detailed authentication logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = true;
}

/// <summary>
/// Configuration options for Keycloak Service Account
/// </summary>
public class KeycloakServiceAccountOptions
{
    public const string SectionName = "KeycloakServiceAccount";

    /// <summary>
    /// Keycloak base URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak realm name
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Service account client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Service account client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Token endpoint path
    /// </summary>
    public string TokenEndpoint { get; set; } = "/realms/{realm}/protocol/openid-connect/token";

    /// <summary>
    /// Admin API base URL path
    /// </summary>
    public string AdminApiBaseUrl { get; set; } = "/admin/realms/{realm}";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Enable HTTPS certificate validation
    /// </summary>
    public bool EnableHttpsValidation { get; set; } = true;

    /// <summary>
    /// User agent string for HTTP requests
    /// </summary>
    public string UserAgent { get; set; } = "CreditTransfer-BasicAuth/1.0";

    /// <summary>
    /// Get the formatted token endpoint URL
    /// </summary>
    public string GetTokenEndpointUrl()
    {
        var endpoint = TokenEndpoint.Replace("{realm}", Realm);
        return $"{BaseUrl.TrimEnd('/')}{endpoint}";
    }

    /// <summary>
    /// Get the formatted admin API base URL
    /// </summary>
    public string GetAdminApiBaseUrl()
    {
        var baseUrl = AdminApiBaseUrl.Replace("{realm}", Realm);
        return $"{BaseUrl.TrimEnd('/')}{baseUrl}";
    }
}

/// <summary>
/// Configuration options for authentication caching
/// </summary>
public class AuthenticationCacheOptions
{
    public const string SectionName = "AuthenticationCache";

    /// <summary>
    /// Cache provider type (Memory, Redis, Distributed)
    /// </summary>
    public string Provider { get; set; } = "Redis";

    /// <summary>
    /// Default cache expiration time
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Enable sliding expiration
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Maximum cache size (entries)
    /// </summary>
    public int MaxCacheSize { get; set; } = 10000;

    /// <summary>
    /// Cache key prefix
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "auth:";

    /// <summary>
    /// Enable cache statistics collection
    /// </summary>
    public bool EnableCacheStatistics { get; set; } = true;

    /// <summary>
    /// Cache cleanup interval in minutes
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Configuration options for role mapping
/// </summary>
public class RoleMappingOptions
{
    public const string SectionName = "RoleMapping";

    /// <summary>
    /// Enable role mapping functionality
    /// </summary>
    public bool EnableRoleMapping { get; set; } = true;

    /// <summary>
    /// Default role for authenticated users
    /// </summary>
    public string DefaultRole { get; set; } = "credit-transfer-user";

    /// <summary>
    /// Role hierarchy mapping (parent -> children)
    /// </summary>
    public Dictionary<string, string[]> RoleHierarchy { get; set; } = new();

    /// <summary>
    /// Operation permissions mapping (operation -> allowed roles)
    /// </summary>
    public Dictionary<string, string[]> OperationPermissions { get; set; } = new();

    /// <summary>
    /// Check if user has permission for operation
    /// </summary>
    public bool HasOperationPermission(string operation, IEnumerable<string> userRoles)
    {
        if (!EnableRoleMapping || !OperationPermissions.ContainsKey(operation))
        {
            return true; // Allow if role mapping is disabled or operation not configured
        }

        var allowedRoles = OperationPermissions[operation];
        
        // Check for wildcard permission
        if (allowedRoles.Contains("*"))
        {
            return true;
        }

        // Check direct role match
        if (userRoles.Any(role => allowedRoles.Contains(role)))
        {
            return true;
        }

        // Check inherited roles through hierarchy
        foreach (var userRole in userRoles)
        {
            if (RoleHierarchy.ContainsKey(userRole))
            {
                var inheritedRoles = RoleHierarchy[userRole];
                if (inheritedRoles.Any(inherited => allowedRoles.Contains(inherited)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Get effective roles for user (including inherited roles)
    /// </summary>
    public IEnumerable<string> GetEffectiveRoles(IEnumerable<string> userRoles)
    {
        var effectiveRoles = new HashSet<string>(userRoles);

        foreach (var role in userRoles)
        {
            if (RoleHierarchy.ContainsKey(role))
            {
                foreach (var inheritedRole in RoleHierarchy[role])
                {
                    effectiveRoles.Add(inheritedRole);
                }
            }
        }

        return effectiveRoles;
    }
}

/// <summary>
/// Configuration options for security settings
/// </summary>
public class SecuritySettingsOptions
{
    public const string SectionName = "SecuritySettings";

    /// <summary>
    /// Require HTTPS for authentication
    /// </summary>
    public bool RequireHttps { get; set; } = false;

    /// <summary>
    /// Enable account lockout functionality
    /// </summary>
    public bool EnableAccountLockout { get; set; } = true;

    /// <summary>
    /// Enable brute force protection
    /// </summary>
    public bool EnableBruteForceProtection { get; set; } = true;

    /// <summary>
    /// Maximum concurrent sessions per user
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 100;

    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 60;

    /// <summary>
    /// Enable audit logging
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Enable sensitive data masking in logs
    /// </summary>
    public bool SensitiveDataMasking { get; set; } = true;

    /// <summary>
    /// Password validation settings
    /// </summary>
    public PasswordValidationOptions PasswordValidation { get; set; } = new();
}

/// <summary>
/// Password validation configuration
/// </summary>
public class PasswordValidationOptions
{
    /// <summary>
    /// Minimum password length
    /// </summary>
    public int MinLength { get; set; } = 8;

    /// <summary>
    /// Require at least one digit
    /// </summary>
    public bool RequireDigit { get; set; } = false;

    /// <summary>
    /// Require at least one lowercase letter
    /// </summary>
    public bool RequireLowercase { get; set; } = false;

    /// <summary>
    /// Require at least one uppercase letter
    /// </summary>
    public bool RequireUppercase { get; set; } = false;

    /// <summary>
    /// Require at least one non-alphanumeric character
    /// </summary>
    public bool RequireNonAlphanumeric { get; set; } = false;

    /// <summary>
    /// Maximum password length
    /// </summary>
    public int MaxLength { get; set; } = 128;
} 