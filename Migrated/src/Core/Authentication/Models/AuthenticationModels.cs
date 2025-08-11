using System.Security.Claims;

namespace CreditTransfer.Core.Authentication.Models;

/// <summary>
/// Represents the result of an authentication attempt
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Whether authentication was successful
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Authenticated user's username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// User's unique identifier in Keycloak
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's roles from Keycloak
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Whether the user account is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Whether the user's email is verified
    /// </summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>
    /// Additional user attributes from Keycloak
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new();

    /// <summary>
    /// Authentication timestamp
    /// </summary>
    public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Authentication method used
    /// </summary>
    public string AuthenticationMethod { get; set; } = "BasicAuth";

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code if authentication failed
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Create a ClaimsPrincipal from the authentication result
    /// </summary>
    public ClaimsPrincipal ToClaimsPrincipal()
    {
        if (!IsAuthenticated || string.IsNullOrEmpty(Username))
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, Username),
            new(ClaimTypes.AuthenticationMethod, AuthenticationMethod),
            new("auth_time", AuthenticatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
        };

        if (!string.IsNullOrEmpty(UserId))
            claims.Add(new(ClaimTypes.NameIdentifier, UserId));

        if (!string.IsNullOrEmpty(Email))
            claims.Add(new(ClaimTypes.Email, Email));

        if (!string.IsNullOrEmpty(FirstName))
            claims.Add(new(ClaimTypes.GivenName, FirstName));

        if (!string.IsNullOrEmpty(LastName))
            claims.Add(new(ClaimTypes.Surname, LastName));

        // Add roles as claims
        claims.AddRange(Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add custom claims
        claims.Add(new("account_enabled", IsEnabled.ToString()));
        claims.Add(new("email_verified", IsEmailVerified.ToString()));

        // Add custom attributes
        foreach (var attribute in Attributes)
        {
            claims.Add(new($"attr_{attribute.Key}", attribute.Value?.ToString() ?? string.Empty));
        }

        var identity = new ClaimsIdentity(claims, "BasicAuthentication");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Create a failed authentication result
    /// </summary>
    public static AuthenticationResult Failed(string errorMessage, string? errorCode = null)
    {
        return new AuthenticationResult
        {
            IsAuthenticated = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// Create a successful authentication result
    /// </summary>
    public static AuthenticationResult Success(string username, string? userId = null)
    {
        return new AuthenticationResult
        {
            IsAuthenticated = true,
            Username = username,
            UserId = userId,
            IsEnabled = true
        };
    }
}

/// <summary>
/// Represents a user from Keycloak
/// </summary>
public class KeycloakUser
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Whether the user is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether email is verified
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// User creation timestamp
    /// </summary>
    public long CreatedTimestamp { get; set; }

    /// <summary>
    /// Additional user attributes
    /// </summary>
    public Dictionary<string, List<string>> Attributes { get; set; } = new();

    /// <summary>
    /// User's realm roles
    /// </summary>
    public List<string> RealmRoles { get; set; } = new();

    /// <summary>
    /// Get creation date from timestamp
    /// </summary>
    public DateTime CreatedDate => DateTimeOffset.FromUnixTimeMilliseconds(CreatedTimestamp).DateTime;

    /// <summary>
    /// Get display name
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
                return $"{FirstName} {LastName}";
            if (!string.IsNullOrEmpty(FirstName))
                return FirstName;
            if (!string.IsNullOrEmpty(LastName))
                return LastName;
            return Username;
        }
    }
}

/// <summary>
/// Represents a Keycloak role
/// </summary>
public class KeycloakRole
{
    /// <summary>
    /// Role unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Role name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the role is composite
    /// </summary>
    public bool Composite { get; set; }

    /// <summary>
    /// Whether this is a client role
    /// </summary>
    public bool ClientRole { get; set; }

    /// <summary>
    /// Container ID (realm or client)
    /// </summary>
    public string? ContainerId { get; set; }

    /// <summary>
    /// Role attributes
    /// </summary>
    public Dictionary<string, List<string>> Attributes { get; set; } = new();
}

/// <summary>
/// Authentication context for WCF operations
/// </summary>
public class AuthenticationContext
{
    /// <summary>
    /// Whether the request is authenticated
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Authenticated username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// User's unique identifier
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User's roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Authentication timestamp
    /// </summary>
    public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Authentication method
    /// </summary>
    public string AuthenticationMethod { get; set; } = "BasicAuth";

    /// <summary>
    /// User's claims principal
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// Check if user has specific role
    /// </summary>
    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if user has any of the specified roles
    /// </summary>
    public bool HasAnyRole(params string[] roles)
    {
        return roles.Any(role => HasRole(role));
    }

    /// <summary>
    /// Check if user has all specified roles
    /// </summary>
    public bool HasAllRoles(params string[] roles)
    {
        return roles.All(role => HasRole(role));
    }

    /// <summary>
    /// Create anonymous context
    /// </summary>
    public static AuthenticationContext Anonymous => new()
    {
        IsAuthenticated = false,
        Username = "Anonymous"
    };

    /// <summary>
    /// Create authenticated context
    /// </summary>
    public static AuthenticationContext Create(AuthenticationResult result)
    {
        return new AuthenticationContext
        {
            IsAuthenticated = result.IsAuthenticated,
            Username = result.Username,
            UserId = result.UserId,
            Roles = result.Roles,
            AuthenticatedAt = result.AuthenticatedAt,
            AuthenticationMethod = result.AuthenticationMethod,
            Principal = result.ToClaimsPrincipal()
        };
    }
}

/// <summary>
/// Authentication failure reasons
/// </summary>
public enum AuthenticationFailureReason
{
    InvalidCredentials,
    UserNotFound,
    UserDisabled,
    AccountLocked,
    PasswordExpired,
    TooManyFailedAttempts,
    ServiceUnavailable,
    InvalidConfiguration,
    NetworkError,
    UnknownError
}

/// <summary>
/// Detailed authentication failure information
/// </summary>
public class AuthenticationFailure
{
    /// <summary>
    /// Failure reason
    /// </summary>
    public AuthenticationFailureReason Reason { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code for client applications
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Additional failure details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// When the failure occurred
    /// </summary>
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Create authentication failure
    /// </summary>
    public static AuthenticationFailure Create(AuthenticationFailureReason reason, string message, string? errorCode = null)
    {
        return new AuthenticationFailure
        {
            Reason = reason,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Authentication statistics for monitoring
/// </summary>
public class AuthenticationStatistics
{
    /// <summary>
    /// Total authentication attempts
    /// </summary>
    public long TotalAttempts { get; set; }

    /// <summary>
    /// Successful authentications
    /// </summary>
    public long SuccessfulAttempts { get; set; }

    /// <summary>
    /// Failed authentications
    /// </summary>
    public long FailedAttempts { get; set; }

    /// <summary>
    /// Cache hits
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Cache misses
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// Keycloak API calls
    /// </summary>
    public long KeycloakApiCalls { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulAttempts / TotalAttempts * 100 : 0;

    /// <summary>
    /// Cache hit rate percentage
    /// </summary>
    public double CacheHitRate
    {
        get
        {
            var totalCacheRequests = CacheHits + CacheMisses;
            return totalCacheRequests > 0 ? (double)CacheHits / totalCacheRequests * 100 : 0;
        }
    }
} 