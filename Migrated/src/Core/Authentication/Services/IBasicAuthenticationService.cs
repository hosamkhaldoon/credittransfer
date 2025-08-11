using System.Security.Claims;
using CreditTransfer.Core.Authentication.Models;

namespace CreditTransfer.Core.Authentication.Services;

/// <summary>
/// Interface for Basic Authentication service with Keycloak integration
/// </summary>
public interface IBasicAuthenticationService
{
    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate user credentials against Keycloak
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if credentials are valid</returns>
    Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user information from Keycloak
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information or null if not found</returns>
    Task<KeycloakUser?> GetUserAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user roles from Keycloak
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user roles</returns>
    Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is active and not locked
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is active</returns>
    Task<bool> IsUserActiveAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user account is locked due to failed attempts
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is locked out</returns>
    Task<bool> IsUserLockedOutAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record failed authentication attempt
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="reason">Failure reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordFailedAttemptAsync(string username, AuthenticationFailureReason reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset failed attempt counter for user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetFailedAttemptsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has required role for operation
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="operation">Operation name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has permission</returns>
    Task<bool> HasOperationPermissionAsync(string username, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create claims principal from authentication result
    /// </summary>
    /// <param name="result">Authentication result</param>
    /// <returns>Claims principal</returns>
    ClaimsPrincipal CreateClaimsPrincipal(AuthenticationResult result);

    /// <summary>
    /// Get authentication statistics
    /// </summary>
    /// <returns>Authentication statistics</returns>
    Task<AuthenticationStatistics> GetStatisticsAsync();

    /// <summary>
    /// Clear authentication cache
    /// </summary>
    /// <param name="username">Optional username to clear specific user cache</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearCacheAsync(string? username = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for Keycloak service account operations
/// </summary>
public interface IKeycloakServiceAccountClient
{
    /// <summary>
    /// Get service account access token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token</returns>
    Task<string> GetServiceAccountTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate user credentials using Keycloak token endpoint
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if credentials are valid</returns>
    Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by username using Admin API
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    Task<KeycloakUser?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user roles using Admin API
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of roles</returns>
    Task<List<KeycloakRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all realm roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of realm roles</returns>
    Task<List<KeycloakRole>> GetRealmRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check service account health
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service account is working</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for authentication caching
/// </summary>
public interface IAuthenticationCache
{
    /// <summary>
    /// Get cached authentication result
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached result or null</returns>
    Task<AuthenticationResult?> GetAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set authentication result in cache
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="result">Authentication result</param>
    /// <param name="expiration">Cache expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync(string cacheKey, AuthenticationResult result, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove item from cache
    /// </summary>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear cache for user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearUserCacheAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all authentication cache
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <returns>Cache statistics</returns>
    Task<Dictionary<string, object>> GetStatisticsAsync();

    /// <summary>
    /// Generate cache key for user credentials
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="passwordHash">Password hash</param>
    /// <returns>Cache key</returns>
    string GenerateCacheKey(string username, string passwordHash);

    /// <summary>
    /// Generate cache key for user info
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>Cache key</returns>
    string GenerateUserInfoCacheKey(string username);
}

/// <summary>
/// Interface for failed attempt tracking
/// </summary>
public interface IFailedAttemptTracker
{
    /// <summary>
    /// Record failed authentication attempt
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="reason">Failure reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordFailedAttemptAsync(string username, string? ipAddress, AuthenticationFailureReason reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get failed attempt count for user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Failed attempt count</returns>
    Task<int> GetFailedAttemptCountAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is locked out
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is locked out</returns>
    Task<bool> IsUserLockedOutAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset failed attempts for user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ResetFailedAttemptsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get lockout end time for user
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lockout end time or null if not locked</returns>
    Task<DateTime?> GetLockoutEndTimeAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired lockout records
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredLockoutsAsync(CancellationToken cancellationToken = default);
} 