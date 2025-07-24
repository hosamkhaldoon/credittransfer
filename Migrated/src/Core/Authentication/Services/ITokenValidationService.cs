using System.Security.Claims;

namespace CreditTransfer.Core.Authentication.Services;

public interface ITokenValidationService
{
    /// <summary>
    /// Validates a JWT token and returns the claims principal
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Claims principal if token is valid, null otherwise</returns>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the bearer token from an authorization header
    /// </summary>
    /// <param name="authorizationHeader">The authorization header value</param>
    /// <returns>The bearer token if found, null otherwise</returns>
    string? ExtractBearerToken(string? authorizationHeader);

    /// <summary>
    /// Checks if the user has the required role
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <param name="role">The required role</param>
    /// <returns>True if user has the role, false otherwise</returns>
    bool HasRole(ClaimsPrincipal principal, string role);

    /// <summary>
    /// Gets the user ID from the claims principal
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The user ID if found, null otherwise</returns>
    string? GetUserId(ClaimsPrincipal principal);

    /// <summary>
    /// Gets the username from the claims principal
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>The username if found, null otherwise</returns>
    string? GetUsername(ClaimsPrincipal principal);

    /// <summary>
    /// Gets all roles from the claims principal
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>List of roles</returns>
    IEnumerable<string> GetRoles(ClaimsPrincipal principal);
} 