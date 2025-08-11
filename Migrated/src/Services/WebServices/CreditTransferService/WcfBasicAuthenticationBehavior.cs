using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CreditTransfer.Core.Authentication.Services;
using CreditTransfer.Core.Authentication.Models;
using System.Diagnostics;
using System.Text;

namespace CreditTransfer.Services.WcfService;

/// <summary>
/// Custom WCF behavior for Basic Authentication
/// Provides automatic Basic Auth validation at the WCF framework level
/// Replaces JWT authentication with HTTP Basic Authentication + Keycloak integration
/// </summary>
public class WcfBasicAuthenticationBehavior : Attribute, IServiceBehavior, IOperationBehavior
{
    private readonly bool _requireAuthentication;
    private static IServiceProvider? _staticServiceProvider;

    public WcfBasicAuthenticationBehavior(bool requireAuthentication = true)
    {
        _requireAuthentication = requireAuthentication;
    }

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _staticServiceProvider = serviceProvider;
    }

    #region IServiceBehavior Implementation

    public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, 
        System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
    {
        // No binding parameters needed for Basic authentication
    }

    public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
    {
        // Apply the authentication behavior to all endpoints
        foreach (var endpoint in serviceHostBase.ChannelDispatchers)
        {
            if (endpoint is ChannelDispatcher channelDispatcher)
            {
                foreach (var endpointDispatcher in channelDispatcher.Endpoints)
                {
                    // Add our custom message inspector for Basic authentication
                    endpointDispatcher.DispatchRuntime.MessageInspectors.Add(
                        new BasicAuthMessageInspector(_requireAuthentication, _staticServiceProvider));
                }
            }
        }
    }

    public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
    {
        // Validation logic if needed
    }

    #endregion

    #region IOperationBehavior Implementation

    public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
    {
        // No binding parameters needed
    }

    public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
    {
        // Client behavior not applicable for server-side authentication
    }

    public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
    {
        // Apply operation-specific authentication logic if needed
    }

    public void Validate(OperationDescription operationDescription)
    {
        // Validation logic if needed
    }

    #endregion
}

/// <summary>
/// Custom message inspector for Basic Authentication at the WCF message level
/// Validates Basic Auth credentials before operation execution
/// </summary>
public class BasicAuthMessageInspector : IDispatchMessageInspector
{
    private readonly bool _requireAuthentication;
    private readonly ActivitySource _activitySource;
    private readonly IServiceProvider? _serviceProvider;

    public BasicAuthMessageInspector(bool requireAuthentication = true, IServiceProvider? serviceProvider = null)
    {
        _requireAuthentication = requireAuthentication;
        _serviceProvider = serviceProvider;
        _activitySource = new ActivitySource("CreditTransfer.WcfService.BasicAuth");
    }

    public object? AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
    {
        using var activity = _activitySource.StartActivity("WCF.BasicAuthRequest");
        
        try
        {
            // Use the injected service provider
            if (_serviceProvider == null)
            {
                activity?.SetTag("wcf.auth.error", "no_service_provider");
                if (_requireAuthentication)
                {
                    throw new FaultException("Service provider not available for authentication", 
                        new FaultCode("AuthenticationError"));
                }
                return null;
            }

            var authService = _serviceProvider.GetService<IBasicAuthenticationService>();
            var logger = _serviceProvider.GetService<ILogger<BasicAuthMessageInspector>>();

            if (authService == null)
            {
                activity?.SetTag("wcf.auth.error", "no_auth_service");
                logger?.LogWarning("Basic authentication service not available");
                if (_requireAuthentication)
                {
                    throw new FaultException("Authentication service not available", 
                        new FaultCode("AuthenticationError"));
                }
                return null;
            }

            // Extract Basic Auth credentials from the message
            var (username, password) = ExtractBasicAuthCredentials(request);
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                activity?.SetTag("wcf.auth.method", "none");
                activity?.SetTag("wcf.auth.credentials_present", false);
                
                if (_requireAuthentication)
                {
                    logger?.LogWarning("Basic Auth credentials required but not provided in WCF request");
                    activity?.SetTag("wcf.auth.error", "no_credentials");
                    
                    // Send WWW-Authenticate header for Basic auth challenge
                    var fault = new FaultException("Authentication required", new FaultCode("AuthenticationRequired"));
                    
                    // Note: In CoreWCF, adding headers to fault responses requires different approach
                    // This would need to be handled at the HTTP transport level
                    throw fault;
                }
                
                logger?.LogDebug("No Basic Auth credentials provided, proceeding without authentication");
                return AuthenticationContext.Anonymous;
            }

            // Validate the Basic Auth credentials asynchronously
            activity?.SetTag("wcf.auth.method", "basic");
            activity?.SetTag("wcf.auth.credentials_present", true);
            activity?.SetTag("wcf.auth.username", username);
            
            logger?.LogDebug("Validating Basic Auth credentials for user {Username}", username);
            
            // Since WCF message inspectors are synchronous, we need to use GetAwaiter().GetResult()
            // This is acceptable in this context as it's at the message level
            var authenticationTask = authService.AuthenticateAsync(username, password);
            var authResult = authenticationTask.GetAwaiter().GetResult();

            if (!authResult.IsAuthenticated)
            {
                activity?.SetTag("wcf.auth.result", "failed");
                activity?.SetTag("wcf.auth.error", "invalid_credentials");
                logger?.LogWarning("Basic Auth validation failed for user {Username}: {ErrorMessage}", 
                    username, authResult.ErrorMessage);
                
                if (_requireAuthentication)
                {
                    throw new FaultException($"Authentication failed: {authResult.ErrorMessage}", 
                        new FaultCode("AuthenticationFailed"));
                }
                
                return AuthenticationContext.Anonymous;
            }

            // Create authentication context from successful result
            var authContext = AuthenticationContext.Create(authResult);
            
            activity?.SetTag("wcf.auth.result", "success");
            activity?.SetTag("wcf.auth.user_id", authResult.UserId);
            activity?.SetTag("wcf.auth.role_count", authResult.Roles.Count);
            
            logger?.LogDebug("Basic Auth validation successful for user {Username} with {RoleCount} roles", 
                username, authResult.Roles.Count);

            return authContext;
        }
        catch (FaultException)
        {
            // Re-throw WCF faults as-is
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetTag("wcf.auth.result", "error");
            activity?.SetTag("wcf.auth.exception", ex.GetType().Name);
            
            var logger = _serviceProvider?.GetService<ILogger<BasicAuthMessageInspector>>();
            
            logger?.LogError(ex, "Exception during Basic Auth validation in WCF message inspector");
            
            if (_requireAuthentication)
            {
                throw new FaultException("Authentication service error", 
                    new FaultCode("AuthenticationServiceError"));
            }
            
            return AuthenticationContext.Anonymous;
        }
    }

    public void BeforeSendReply(ref Message reply, object correlationState)
    {
        // No action needed before sending reply for Basic auth
        // Could add authentication headers or audit logging here if needed
    }

    /// <summary>
    /// Extract Basic Authentication credentials from WCF message
    /// </summary>
    private (string? username, string? password) ExtractBasicAuthCredentials(Message message)
    {
        try
        {
            // Method 1: Try to get from HTTP request properties
            if (message.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var httpRequestProperty))
            {
                if (httpRequestProperty is HttpRequestMessageProperty httpRequest)
                {
                    var authHeader = httpRequest.Headers["Authorization"];
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        return ParseBasicAuthHeader(authHeader);
                    }
                }
            }

            // Method 2: Try to get from message headers
            var authHeaderIndex = message.Headers.FindHeader("Authorization", "");
            if (authHeaderIndex >= 0)
            {
                var authHeaderValue = message.Headers.GetHeader<string>(authHeaderIndex);
                if (!string.IsNullOrEmpty(authHeaderValue))
                {
                    return ParseBasicAuthHeader(authHeaderValue);
                }
            }

            // Method 3: Try custom header (X-Authorization)
            var customAuthIndex = message.Headers.FindHeader("X-Authorization", "");
            if (customAuthIndex >= 0)
            {
                var customAuthValue = message.Headers.GetHeader<string>(customAuthIndex);
                if (!string.IsNullOrEmpty(customAuthValue))
                {
                    return ParseBasicAuthHeader(customAuthValue);
                }
            }

            // Method 4: Try reflection-based extraction for different transport types
            var credentials = ExtractCredentialsUsingReflection(message);
            if (credentials.HasValue)
            {
                return credentials.Value;
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail authentication extraction
            System.Diagnostics.Debug.WriteLine($"Error extracting Basic Auth credentials: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Parse Basic Authentication header value
    /// </summary>
    private (string? username, string? password) ParseBasicAuthHeader(string authHeader)
    {
        try
        {
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null);
            }

            var encodedCredentials = authHeader.Substring(6); // Remove "Basic " prefix
            var credentialBytes = Convert.FromBase64String(encodedCredentials);
            var credentials = Encoding.UTF8.GetString(credentialBytes);
            
            var colonIndex = credentials.IndexOf(':');
            if (colonIndex == -1)
            {
                return (null, null);
            }

            var username = credentials.Substring(0, colonIndex);
            var password = credentials.Substring(colonIndex + 1);
            
            return (username, password);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Extract credentials using reflection for different transport types
    /// </summary>
    private (string? username, string? password)? ExtractCredentialsUsingReflection(Message message)
    {
        try
        {
            // This is a fallback method for cases where standard header extraction doesn't work
            // Implementation would depend on specific CoreWCF transport details
            
            // For now, return null to indicate no credentials found via reflection
            return null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Helper class for WCF Basic Authentication operations
/// Provides easy access to authentication context within service operations
/// </summary>
public static class WcfBasicAuthenticationHelper
{
    /// <summary>
    /// Get authentication context from current operation context
    /// </summary>
    public static AuthenticationContext? GetAuthenticationContext()
    {
        var context = OperationContext.Current;
        if (context?.IncomingMessageProperties?.ContainsKey("AuthenticationContext") == true)
        {
            return context.IncomingMessageProperties["AuthenticationContext"] as AuthenticationContext;
        }

        // Try to get from instance context extensions
        var instanceContext = context?.InstanceContext;
        var extension = instanceContext?.Extensions.Find<BasicAuthenticationContextExtension>();
        return extension?.AuthenticationContext;
    }

    /// <summary>
    /// Check if current user is authenticated
    /// </summary>
    public static bool IsAuthenticated()
    {
        var authContext = GetAuthenticationContext();
        return authContext?.IsAuthenticated == true;
    }

    /// <summary>
    /// Get current username
    /// </summary>
    public static string? GetUsername()
    {
        var authContext = GetAuthenticationContext();
        return authContext?.Username;
    }

    /// <summary>
    /// Get current user ID
    /// </summary>
    public static string? GetUserId()
    {
        var authContext = GetAuthenticationContext();
        return authContext?.UserId;
    }

    /// <summary>
    /// Get current user roles
    /// </summary>
    public static List<string> GetUserRoles()
    {
        var authContext = GetAuthenticationContext();
        return authContext?.Roles ?? new List<string>();
    }

    /// <summary>
    /// Check if current user has specific role
    /// </summary>
    public static bool HasRole(string role)
    {
        var authContext = GetAuthenticationContext();
        return authContext?.HasRole(role) == true;
    }

    /// <summary>
    /// Check if current user has any of the specified roles
    /// </summary>
    public static bool HasAnyRole(params string[] roles)
    {
        var authContext = GetAuthenticationContext();
        return authContext?.HasAnyRole(roles) == true;
    }

    /// <summary>
    /// Require authentication for current operation
    /// </summary>
    public static void RequireAuthentication()
    {
        if (!IsAuthenticated())
        {
            throw new FaultException("Authentication required", new FaultCode("AuthenticationRequired"));
        }
    }

    /// <summary>
    /// Require specific role for current operation
    /// </summary>
    public static void RequireRole(string role)
    {
        RequireAuthentication();
        
        if (!HasRole(role))
        {
            throw new FaultException($"Role '{role}' required", new FaultCode("InsufficientPermissions"));
        }
    }

    /// <summary>
    /// Require any of the specified roles for current operation
    /// </summary>
    public static void RequireAnyRole(params string[] roles)
    {
        RequireAuthentication();
        
        if (!HasAnyRole(roles))
        {
            var roleList = string.Join(", ", roles);
            throw new FaultException($"One of the following roles required: {roleList}", 
                new FaultCode("InsufficientPermissions"));
        }
    }
}

/// <summary>
/// Extension for storing authentication context in WCF instance context
/// </summary>
public class BasicAuthenticationContextExtension : IExtension<InstanceContext>
{
    public AuthenticationContext AuthenticationContext { get; set; }

    public BasicAuthenticationContextExtension(AuthenticationContext authenticationContext)
    {
        AuthenticationContext = authenticationContext;
    }

    public void Attach(InstanceContext owner)
    {
        // No action needed
    }

    public void Detach(InstanceContext owner)
    {
        // No action needed
    }
} 