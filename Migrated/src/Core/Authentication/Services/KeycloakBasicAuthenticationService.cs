using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CreditTransfer.Core.Authentication.Configuration;
using CreditTransfer.Core.Authentication.Models;

namespace CreditTransfer.Core.Authentication.Services;

/// <summary>
/// Basic Authentication service with Keycloak integration
/// Provides secure authentication using HTTP Basic Auth with Keycloak as identity provider
/// </summary>
public class KeycloakBasicAuthenticationService : IBasicAuthenticationService
{
    private readonly BasicAuthenticationOptions _authOptions;
    private readonly RoleMappingOptions _roleOptions;
    private readonly SecuritySettingsOptions _securityOptions;
    private readonly IKeycloakServiceAccountClient _keycloakClient;
    private readonly IAuthenticationCache _cache;
    private readonly IFailedAttemptTracker _failedAttemptTracker;
    private readonly ILogger<KeycloakBasicAuthenticationService> _logger;
    private readonly ActivitySource _activitySource;

    private readonly AuthenticationStatistics _statistics = new();
    private readonly object _statisticsLock = new();

    public KeycloakBasicAuthenticationService(
        IOptions<BasicAuthenticationOptions> authOptions,
        IOptions<RoleMappingOptions> roleOptions,
        IOptions<SecuritySettingsOptions> securityOptions,
        IKeycloakServiceAccountClient keycloakClient,
        IAuthenticationCache cache,
        IFailedAttemptTracker failedAttemptTracker,
        ILogger<KeycloakBasicAuthenticationService> logger)
    {
        _authOptions = authOptions.Value;
        _roleOptions = roleOptions.Value;
        _securityOptions = securityOptions.Value;
        _keycloakClient = keycloakClient;
        _cache = cache;
        _failedAttemptTracker = failedAttemptTracker;
        _logger = logger;
        _activitySource = new ActivitySource("CreditTransfer.Authentication.BasicAuth");
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("BasicAuth.Authenticate");
        activity?.SetTag("username", username);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                activity?.SetTag("result", "invalid_input");
                var failure = AuthenticationFailure.Create(
                    AuthenticationFailureReason.InvalidCredentials,
                    "Username and password are required");
                
                await RecordFailedAttemptAsync(username ?? "unknown", AuthenticationFailureReason.InvalidCredentials, cancellationToken);
                return CreateFailedResult(failure);
            }

            // Normalize username
            username = username.Trim().ToLowerInvariant();
            activity?.SetTag("normalized_username", username);

            // Check if user is locked out
            if (await IsUserLockedOutAsync(username, cancellationToken))
            {
                activity?.SetTag("result", "locked_out");
                var failure = AuthenticationFailure.Create(
                    AuthenticationFailureReason.AccountLocked,
                    "Account is temporarily locked due to too many failed attempts");
                
                _logger.LogWarning("Authentication attempt for locked user {Username}", username);
                return CreateFailedResult(failure);
            }

            // Check cache first if enabled
            AuthenticationResult? cachedResult = null;
            if (_authOptions.EnableCaching)
            {
                var passwordHash = ComputePasswordHash(password);
                var cacheKey = _cache.GenerateCacheKey(username, passwordHash);
                cachedResult = await _cache.GetAsync(cacheKey, cancellationToken);
                
                if (cachedResult != null)
                {
                    activity?.SetTag("cache_hit", true);
                    activity?.SetTag("result", cachedResult.IsAuthenticated ? "success" : "failed");
                    
                    RecordStatistics(true, stopwatch.ElapsedMilliseconds);
                    
                    if (cachedResult.IsAuthenticated)
                    {
                        _logger.LogDebug("Authentication successful from cache for user {Username}", username);
                        await ResetFailedAttemptsAsync(username, cancellationToken);
                    }
                    else
                    {
                        await RecordFailedAttemptAsync(username, AuthenticationFailureReason.InvalidCredentials, cancellationToken);
                    }
                    
                    return cachedResult;
                }
            }

            activity?.SetTag("cache_hit", false);

            // Authenticate with Keycloak
            var isValid = await ValidateCredentialsAsync(username, password, cancellationToken);
            
            if (!isValid)
            {
                activity?.SetTag("result", "invalid_credentials");
                var failure = AuthenticationFailure.Create(
                    AuthenticationFailureReason.InvalidCredentials,
                    "Invalid username or password");

                await RecordFailedAttemptAsync(username, AuthenticationFailureReason.InvalidCredentials, cancellationToken);
                
                var failedResult = CreateFailedResult(failure);
                
                // Cache failed result for a short time to prevent repeated calls
                if (_authOptions.EnableCaching)
                {
                    var passwordHash = ComputePasswordHash(password);
                    var cacheKey = _cache.GenerateCacheKey(username, passwordHash);
                    await _cache.SetAsync(cacheKey, failedResult, TimeSpan.FromMinutes(1), cancellationToken);
                }
                
                RecordStatistics(false, stopwatch.ElapsedMilliseconds);
                return failedResult;
            }

            // Get user information from Keycloak
            var user = await GetUserAsync(username, cancellationToken);
            if (user == null)
            {
                activity?.SetTag("result", "user_not_found");
                var failure = AuthenticationFailure.Create(
                    AuthenticationFailureReason.UserNotFound,
                    "User not found");

                await RecordFailedAttemptAsync(username, AuthenticationFailureReason.UserNotFound, cancellationToken);
                RecordStatistics(false, stopwatch.ElapsedMilliseconds);
                return CreateFailedResult(failure);
            }

            // Check if user is enabled
            if (!user.Enabled)
            {
                activity?.SetTag("result", "user_disabled");
                var failure = AuthenticationFailure.Create(
                    AuthenticationFailureReason.UserDisabled,
                    "User account is disabled");

                await RecordFailedAttemptAsync(username, AuthenticationFailureReason.UserDisabled, cancellationToken);
                RecordStatistics(false, stopwatch.ElapsedMilliseconds);
                return CreateFailedResult(failure);
            }

            // Get user roles
            var userRoles = await GetUserRolesAsync(user.Id, cancellationToken);

            // Create successful authentication result
            var successResult = AuthenticationResult.Success(user.Username, user.Id);
            successResult.Email = user.Email;
            successResult.FirstName = user.FirstName;
            successResult.LastName = user.LastName;
            successResult.IsEnabled = user.Enabled;
            successResult.IsEmailVerified = user.EmailVerified;
            successResult.Roles = userRoles;

            // Add user attributes
            foreach (var attr in user.Attributes)
            {
                if (attr.Value.Any())
                {
                    successResult.Attributes[attr.Key] = attr.Value.First();
                }
            }

            activity?.SetTag("result", "success");
            activity?.SetTag("user_id", user.Id);
            activity?.SetTag("role_count", userRoles.Count);

            // Reset failed attempts on successful authentication
            await ResetFailedAttemptsAsync(username, cancellationToken);

            // Cache successful result
            if (_authOptions.EnableCaching)
            {
                var passwordHash = ComputePasswordHash(password);
                var cacheKey = _cache.GenerateCacheKey(username, passwordHash);
                var expiration = TimeSpan.FromMinutes(_authOptions.CacheExpirationMinutes);
                await _cache.SetAsync(cacheKey, successResult, expiration, cancellationToken);
            }

            RecordStatistics(true, stopwatch.ElapsedMilliseconds);
            
            if (_authOptions.LogAuthenticationAttempts)
            {
                _logger.LogInformation("User {Username} authenticated successfully with {RoleCount} roles", 
                    username, userRoles.Count);
            }

            return successResult;
        }
        catch (Exception ex)
        {
            activity?.SetTag("result", "error");
            activity?.SetTag("exception_type", ex.GetType().Name);

            _logger.LogError(ex, "Exception during authentication for user {Username}", username);
            
            var failure = AuthenticationFailure.Create(
                AuthenticationFailureReason.ServiceUnavailable,
                "Authentication service temporarily unavailable");

            RecordStatistics(false, stopwatch.ElapsedMilliseconds);
            return CreateFailedResult(failure);
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("BasicAuth.ValidateCredentials");
        activity?.SetTag("username", username);

        try
        {
            var isValid = await _keycloakClient.ValidateUserCredentialsAsync(username, password, cancellationToken);
            activity?.SetTag("validation_result", isValid);
            
            RecordKeycloakApiCall();
            
            return isValid;
        }
        catch (Exception ex)
        {
            activity?.SetTag("validation_result", "error");
            activity?.SetTag("exception_type", ex.GetType().Name);
            
            _logger.LogError(ex, "Exception during credential validation for user {Username}", username);
            return false;
        }
    }

    public async Task<KeycloakUser?> GetUserAsync(string username, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("BasicAuth.GetUser");
        activity?.SetTag("username", username);

        try
        {
            // Check user info cache first
            if (_authOptions.EnableCaching)
            {
                var userCacheKey = _cache.GenerateUserInfoCacheKey(username);
                // Note: This would require extending the cache interface to support different types
                // For now, we'll skip user info caching and only cache authentication results
            }

            var user = await _keycloakClient.GetUserByUsernameAsync(username, cancellationToken);
            activity?.SetTag("user_found", user != null);
            
            if (user != null)
            {
                activity?.SetTag("user_id", user.Id);
                activity?.SetTag("user_enabled", user.Enabled);
            }

            RecordKeycloakApiCall();
            
            return user;
        }
        catch (Exception ex)
        {
            activity?.SetTag("exception_type", ex.GetType().Name);
            _logger.LogError(ex, "Exception while getting user {Username}", username);
            return null;
        }
    }

    public async Task<List<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("BasicAuth.GetUserRoles");
        activity?.SetTag("user_id", userId);

        try
        {
            var roles = await _keycloakClient.GetUserRolesAsync(userId, cancellationToken);
            var roleNames = roles.Select(r => r.Name).ToList();
            
            activity?.SetTag("role_count", roleNames.Count);
            
            RecordKeycloakApiCall();
            
            return roleNames;
        }
        catch (Exception ex)
        {
            activity?.SetTag("exception_type", ex.GetType().Name);
            _logger.LogError(ex, "Exception while getting roles for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> IsUserActiveAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(username, cancellationToken);
        return user?.Enabled == true;
    }

    public async Task<bool> IsUserLockedOutAsync(string username, CancellationToken cancellationToken = default)
    {
        if (!_securityOptions.EnableAccountLockout)
            return false;

        return await _failedAttemptTracker.IsUserLockedOutAsync(username, cancellationToken);
    }

    public async Task RecordFailedAttemptAsync(string username, AuthenticationFailureReason reason, CancellationToken cancellationToken = default)
    {
        if (_securityOptions.EnableBruteForceProtection)
        {
            await _failedAttemptTracker.RecordFailedAttemptAsync(username, null, reason, cancellationToken);
        }

        if (_authOptions.LogAuthenticationAttempts)
        {
            _logger.LogWarning("Authentication failed for user {Username}: {Reason}", username, reason);
        }
    }

    public async Task ResetFailedAttemptsAsync(string username, CancellationToken cancellationToken = default)
    {
        if (_securityOptions.EnableBruteForceProtection)
        {
            await _failedAttemptTracker.ResetFailedAttemptsAsync(username, cancellationToken);
        }
    }

    public async Task<bool> HasOperationPermissionAsync(string username, string operation, CancellationToken cancellationToken = default)
    {
        if (!_roleOptions.EnableRoleMapping)
            return true;

        try
        {
            var user = await GetUserAsync(username, cancellationToken);
            if (user == null)
                return false;

            var userRoles = await GetUserRolesAsync(user.Id, cancellationToken);
            return _roleOptions.HasOperationPermission(operation, userRoles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking operation permission for user {Username}, operation {Operation}", 
                username, operation);
            return false;
        }
    }

    public ClaimsPrincipal CreateClaimsPrincipal(AuthenticationResult result)
    {
        return result.ToClaimsPrincipal();
    }

    public async Task<AuthenticationStatistics> GetStatisticsAsync()
    {
        lock (_statisticsLock)
        {
            return new AuthenticationStatistics
            {
                TotalAttempts = _statistics.TotalAttempts,
                SuccessfulAttempts = _statistics.SuccessfulAttempts,
                FailedAttempts = _statistics.FailedAttempts,
                CacheHits = _statistics.CacheHits,
                CacheMisses = _statistics.CacheMisses,
                KeycloakApiCalls = _statistics.KeycloakApiCalls,
                AverageResponseTimeMs = _statistics.AverageResponseTimeMs
            };
        }
    }

    public async Task ClearCacheAsync(string? username = null, CancellationToken cancellationToken = default)
    {
        if (username != null)
        {
            await _cache.ClearUserCacheAsync(username, cancellationToken);
        }
        else
        {
            await _cache.ClearAllAsync(cancellationToken);
        }
    }

    private AuthenticationResult CreateFailedResult(AuthenticationFailure failure)
    {
        return AuthenticationResult.Failed(failure.Message, failure.ErrorCode);
    }

    private string ComputePasswordHash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private void RecordStatistics(bool success, long responseTimeMs)
    {
        lock (_statisticsLock)
        {
            _statistics.TotalAttempts++;
            
            if (success)
                _statistics.SuccessfulAttempts++;
            else
                _statistics.FailedAttempts++;

            // Update average response time
            _statistics.AverageResponseTimeMs = (_statistics.AverageResponseTimeMs * (_statistics.TotalAttempts - 1) + responseTimeMs) / _statistics.TotalAttempts;
        }
    }

    private void RecordKeycloakApiCall()
    {
        lock (_statisticsLock)
        {
            _statistics.KeycloakApiCalls++;
        }
    }
} 