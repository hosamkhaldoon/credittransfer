using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using CreditTransfer.Core.Authentication.Configuration;
using CreditTransfer.Core.Authentication.Models;

namespace CreditTransfer.Core.Authentication.Services;

/// <summary>
/// Keycloak service account client for Basic Authentication integration
/// Handles all communication with Keycloak Admin API
/// </summary>
public class KeycloakServiceAccountClient : IKeycloakServiceAccountClient, IDisposable
{
    private readonly KeycloakServiceAccountOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _tokenCache;
    private readonly ILogger<KeycloakServiceAccountClient> _logger;
    private readonly ActivitySource _activitySource;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    private const string TokenCacheKey = "keycloak_service_account_token";
    private const string HttpClientName = "KeycloakServiceAccount";

    public KeycloakServiceAccountClient(
        IOptions<KeycloakServiceAccountOptions> options,
        IHttpClientFactory httpClientFactory,
        IMemoryCache tokenCache,
        ILogger<KeycloakServiceAccountClient> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _tokenCache = tokenCache;
        _logger = logger;
        _activitySource = new ActivitySource("CreditTransfer.Authentication.Keycloak");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        ValidateConfiguration();
    }

    public async Task<string> GetServiceAccountTokenAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Keycloak.GetServiceAccountToken");
        
        // Check cache first
        if (_tokenCache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            activity?.SetTag("cache_hit", true);
            _logger.LogDebug("Using cached service account token");
            return cachedToken;
        }

        activity?.SetTag("cache_hit", false);
        
        try
        {
            using var httpClient = CreateHttpClient();
            
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret)
            });

            var tokenUrl = _options.GetTokenEndpointUrl();
            activity?.SetTag("token_endpoint", tokenUrl);

            _logger.LogDebug("Requesting service account token from {TokenEndpoint}", tokenUrl);

            var response = await httpClient.PostAsync(tokenUrl, tokenRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                activity?.SetTag("error", true);
                activity?.SetTag("status_code", (int)response.StatusCode);
                
                _logger.LogError("Failed to get service account token. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                
                throw new InvalidOperationException($"Failed to get service account token: {response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(_jsonOptions, cancellationToken);
            
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                activity?.SetTag("error", true);
                _logger.LogError("Invalid token response from Keycloak");
                throw new InvalidOperationException("Invalid token response from Keycloak");
            }

            // Cache token with 90% of its lifetime to allow for refresh
            var cacheExpiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn * 0.9);
            _tokenCache.Set(TokenCacheKey, tokenResponse.AccessToken, cacheExpiration);

            activity?.SetTag("token_expires_in", tokenResponse.ExpiresIn);
            activity?.SetTag("cache_expiration_seconds", cacheExpiration.TotalSeconds);

            _logger.LogDebug("Service account token obtained successfully, expires in {ExpiresIn} seconds", 
                tokenResponse.ExpiresIn);

            return tokenResponse.AccessToken;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            activity?.SetTag("error", true);
            activity?.SetTag("exception_type", ex.GetType().Name);
            
            _logger.LogError(ex, "Exception occurred while getting service account token");
            throw new InvalidOperationException("Failed to get service account token", ex);
        }
    }

    public async Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Keycloak.ValidateUserCredentials");
        activity?.SetTag("username", username);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            activity?.SetTag("validation_result", "invalid_input");
            _logger.LogWarning("Username or password is empty");
            return false;
        }

        try
        {
            using var httpClient = CreateHttpClient();
            
            var credentialRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", "admin-cli"),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var tokenUrl = _options.GetTokenEndpointUrl();
            
            _logger.LogDebug("Validating user credentials for {Username}", username);

            var response = await httpClient.PostAsync(tokenUrl, credentialRequest, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                activity?.SetTag("validation_result", "success");
                _logger.LogDebug("User credentials validated successfully for {Username}", username);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                activity?.SetTag("validation_result", "failed");
                activity?.SetTag("status_code", (int)response.StatusCode);
                
                _logger.LogDebug("User credential validation failed for {Username}. Status: {StatusCode}", 
                    username, response.StatusCode);
                
                return false;
            }
        }
        catch (Exception ex)
        {
            activity?.SetTag("validation_result", "error");
            activity?.SetTag("exception_type", ex.GetType().Name);
            
            _logger.LogError(ex, "Exception occurred while validating credentials for {Username}", username);
            return false;
        }
    }

    public async Task<KeycloakUser?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Keycloak.GetUserByUsername");
        activity?.SetTag("username", username);

        if (string.IsNullOrEmpty(username))
        {
            activity?.SetTag("result", "invalid_input");
            return null;
        }

        try
        {
            var serviceAccountToken = await GetServiceAccountTokenAsync(cancellationToken);
            using var httpClient = CreateHttpClient();
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceAccountToken);

            var adminApiUrl = _options.GetAdminApiBaseUrl();
            var usersUrl = $"{adminApiUrl}/users?username={Uri.EscapeDataString(username)}&exact=true";
            
            activity?.SetTag("api_url", usersUrl);
            
            _logger.LogDebug("Getting user information for {Username} from {ApiUrl}", username, usersUrl);

            var response = await httpClient.GetAsync(usersUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                activity?.SetTag("result", "api_error");
                activity?.SetTag("status_code", (int)response.StatusCode);
                
                _logger.LogWarning("Failed to get user {Username}. Status: {StatusCode}", 
                    username, response.StatusCode);
                
                return null;
            }

            var users = await response.Content.ReadFromJsonAsync<KeycloakUser[]>(_jsonOptions, cancellationToken);
            var user = users?.FirstOrDefault();

            if (user != null)
            {
                activity?.SetTag("result", "found");
                activity?.SetTag("user_id", user.Id);
                activity?.SetTag("user_enabled", user.Enabled);
                
                _logger.LogDebug("User {Username} found with ID {UserId}", username, user.Id);
            }
            else
            {
                activity?.SetTag("result", "not_found");
                _logger.LogDebug("User {Username} not found", username);
            }

            return user;
        }
        catch (Exception ex)
        {
            activity?.SetTag("result", "error");
            activity?.SetTag("exception_type", ex.GetType().Name);
            
            _logger.LogError(ex, "Exception occurred while getting user {Username}", username);
            return null;
        }
    }

    public async Task<List<KeycloakRole>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Keycloak.GetUserRoles");
        activity?.SetTag("user_id", userId);

        if (string.IsNullOrEmpty(userId))
        {
            activity?.SetTag("result", "invalid_input");
            return new List<KeycloakRole>();
        }

        try
        {
            var serviceAccountToken = await GetServiceAccountTokenAsync(cancellationToken);
            using var httpClient = CreateHttpClient();
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceAccountToken);

            var adminApiUrl = _options.GetAdminApiBaseUrl();
            var rolesUrl = $"{adminApiUrl}/users/{Uri.EscapeDataString(userId)}/role-mappings/realm";
            
            activity?.SetTag("api_url", rolesUrl);
            
            _logger.LogDebug("Getting roles for user {UserId} from {ApiUrl}", userId, rolesUrl);

            var response = await httpClient.GetAsync(rolesUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                activity?.SetTag("result", "api_error");
                activity?.SetTag("status_code", (int)response.StatusCode);
                
                _logger.LogWarning("Failed to get roles for user {UserId}. Status: {StatusCode}", 
                    userId, response.StatusCode);
                
                return new List<KeycloakRole>();
            }

            var roles = await response.Content.ReadFromJsonAsync<KeycloakRole[]>(_jsonOptions, cancellationToken) 
                ?? Array.Empty<KeycloakRole>();

            activity?.SetTag("result", "success");
            activity?.SetTag("role_count", roles.Length);
            
            _logger.LogDebug("Found {RoleCount} roles for user {UserId}", roles.Length, userId);

            return roles.ToList();
        }
        catch (Exception ex)
        {
            activity?.SetTag("result", "error");
            activity?.SetTag("exception_type", ex.GetType().Name);
            
            _logger.LogError(ex, "Exception occurred while getting roles for user {UserId}", userId);
            return new List<KeycloakRole>();
        }
    }

    public async Task<List<KeycloakRole>> GetRealmRolesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Keycloak.GetRealmRoles");

        try
        {
            var serviceAccountToken = await GetServiceAccountTokenAsync(cancellationToken);
            using var httpClient = CreateHttpClient();
            
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", serviceAccountToken);

            var adminApiUrl = _options.GetAdminApiBaseUrl();
            var rolesUrl = $"{adminApiUrl}/roles";
            
            activity?.SetTag("api_url", rolesUrl);
            
            _logger.LogDebug("Getting realm roles from {ApiUrl}", rolesUrl);

            var response = await httpClient.GetAsync(rolesUrl, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                activity?.SetTag("result", "api_error");
                activity?.SetTag("status_code", (int)response.StatusCode);
                
                _logger.LogWarning("Failed to get realm roles. Status: {StatusCode}", response.StatusCode);
                return new List<KeycloakRole>();
            }

            var roles = await response.Content.ReadFromJsonAsync<KeycloakRole[]>(_jsonOptions, cancellationToken) 
                ?? Array.Empty<KeycloakRole>();

            activity?.SetTag("result", "success");
            activity?.SetTag("role_count", roles.Length);
            
            _logger.LogDebug("Found {RoleCount} realm roles", roles.Length);

            return roles.ToList();
        }
        catch (Exception ex)
        {
            activity?.SetTag("result", "error");
            activity?.SetTag("exception_type", ex.GetType().Name);
            
            _logger.LogError(ex, "Exception occurred while getting realm roles");
            return new List<KeycloakRole>();
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("Keycloak.HealthCheck");

        try
        {
            // Try to get a service account token
            var token = await GetServiceAccountTokenAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(token))
            {
                activity?.SetTag("health", "unhealthy");
                activity?.SetTag("reason", "no_token");
                return false;
            }

            // Try to call Admin API
            using var httpClient = CreateHttpClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var adminApiUrl = _options.GetAdminApiBaseUrl();
            var healthUrl = $"{adminApiUrl}/roles?first=0&max=1";
            
            var response = await httpClient.GetAsync(healthUrl, cancellationToken);
            
            var isHealthy = response.IsSuccessStatusCode;
            
            activity?.SetTag("health", isHealthy ? "healthy" : "unhealthy");
            activity?.SetTag("status_code", (int)response.StatusCode);

            if (isHealthy)
            {
                _logger.LogDebug("Keycloak service account health check passed");
            }
            else
            {
                _logger.LogWarning("Keycloak service account health check failed. Status: {StatusCode}", 
                    response.StatusCode);
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            activity?.SetTag("health", "unhealthy");
            activity?.SetTag("exception_type", ex.GetType().Name);
            
            _logger.LogError(ex, "Keycloak service account health check failed with exception");
            return false;
        }
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        
        if (httpClient.BaseAddress == null)
        {
            httpClient.BaseAddress = new Uri(_options.BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
            
            if (!string.IsNullOrEmpty(_options.UserAgent))
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
            }
        }

        return httpClient;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_options.BaseUrl))
            throw new InvalidOperationException("Keycloak BaseUrl is not configured");
        
        if (string.IsNullOrEmpty(_options.Realm))
            throw new InvalidOperationException("Keycloak Realm is not configured");
        
        if (string.IsNullOrEmpty(_options.ClientId))
            throw new InvalidOperationException("Keycloak ClientId is not configured");
        
        if (string.IsNullOrEmpty(_options.ClientSecret))
            throw new InvalidOperationException("Keycloak ClientSecret is not configured");

        _logger.LogInformation("Keycloak service account client initialized for realm '{Realm}' with client '{ClientId}'", 
            _options.Realm, _options.ClientId);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _activitySource?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Token response from Keycloak
    /// </summary>
    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public string Scope { get; set; } = string.Empty;
    }
} 