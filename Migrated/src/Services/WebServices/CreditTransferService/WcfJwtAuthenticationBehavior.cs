using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CreditTransfer.Core.Authentication.Services;
using System.Diagnostics;

namespace CreditTransfer.Services.WcfService;

/// <summary>
/// Custom WCF behavior for JWT authentication
/// Provides automatic JWT token validation at the WCF framework level
/// </summary>
public class WcfJwtAuthenticationBehavior : Attribute, IServiceBehavior, IOperationBehavior
{
    private readonly bool _requireAuthentication;

    public WcfJwtAuthenticationBehavior(bool requireAuthentication = true)
    {
        _requireAuthentication = requireAuthentication;
    }

    #region IServiceBehavior Implementation

    public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, 
        System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
    {
        // No binding parameters needed for JWT authentication
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
                    // Add our custom message inspector for JWT authentication
                    endpointDispatcher.DispatchRuntime.MessageInspectors.Add(
                        new JwtMessageInspector(_requireAuthentication));
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
/// Custom message inspector for JWT authentication at the WCF message level
/// Validates JWT tokens before operation execution
/// </summary>
public class JwtMessageInspector : IDispatchMessageInspector
{
    private readonly bool _requireAuthentication;
    private readonly ActivitySource _activitySource;

    public JwtMessageInspector(bool requireAuthentication = true)
    {
        _requireAuthentication = requireAuthentication;
        _activitySource = new ActivitySource("CreditTransfer.WcfService.Authentication");
    }

    public object? AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
    {
        using var activity = _activitySource.StartActivity("WCF.AuthenticateRequest");
        
        try
        {
            // Get the service provider from the instance context
            var serviceProvider = instanceContext.Extensions.Find<ServiceProviderExtension>()?.ServiceProvider;
            if (serviceProvider == null)
            {
                activity?.SetTag("wcf.auth.error", "no_service_provider");
                if (_requireAuthentication)
                {
                    throw new FaultException("Service provider not available for authentication", 
                        new FaultCode("AuthenticationError"));
                }
                return null;
            }

            var tokenValidationService = serviceProvider.GetService<ITokenValidationService>();
            var logger = serviceProvider.GetService<ILogger<JwtMessageInspector>>();

            if (tokenValidationService == null)
            {
                activity?.SetTag("wcf.auth.error", "no_token_service");
                logger?.LogWarning("Token validation service not available");
                if (_requireAuthentication)
                {
                    throw new FaultException("Authentication service not available", 
                        new FaultCode("AuthenticationError"));
                }
                return null;
            }

            // Extract JWT token from the message
            var jwtToken = ExtractJwtTokenFromMessage(request);
            
            if (string.IsNullOrEmpty(jwtToken))
            {
                activity?.SetTag("wcf.auth.method", "none");
                activity?.SetTag("wcf.auth.token_present", false);
                
                if (_requireAuthentication)
                {
                    logger?.LogWarning("JWT token required but not provided in WCF request");
                    activity?.SetTag("wcf.auth.error", "no_token");
                    throw new FaultException("Authentication token required", 
                        new FaultCode("AuthenticationRequired"));
                }
                
                logger?.LogDebug("No JWT token provided, proceeding without authentication");
                return new AuthenticationContext { IsAuthenticated = false, Username = "Anonymous" };
            }

            // Validate the JWT token
            activity?.SetTag("wcf.auth.method", "jwt");
            activity?.SetTag("wcf.auth.token_present", true);
            
            var validationTask = tokenValidationService.ValidateTokenAsync(jwtToken);
            var principal = validationTask.GetAwaiter().GetResult();

            if (principal == null)
            {
                activity?.SetTag("wcf.auth.result", "failed");
                activity?.SetTag("wcf.auth.error", "invalid_token");
                logger?.LogWarning("JWT token validation failed for WCF request");
                
                if (_requireAuthentication)
                {
                    throw new FaultException("Invalid authentication token", 
                        new FaultCode("AuthenticationFailed"));
                }
                
                return new AuthenticationContext { IsAuthenticated = false, Username = "InvalidUser" };
            }

            // Extract user information from the validated token
            var username = tokenValidationService.GetUsername(principal) ?? "Unknown";
            var userId = tokenValidationService.GetUserId(principal);
            var roles = tokenValidationService.GetRoles(principal).ToList();

            activity?.SetTag("wcf.auth.result", "success");
            activity?.SetTag("wcf.auth.username", username);
            activity?.SetTag("wcf.auth.user_id", userId);
            activity?.SetTag("wcf.auth.roles", string.Join(",", roles));

            logger?.LogDebug("JWT authentication successful for WCF request: {Username}, Roles: [{Roles}]", 
                username, string.Join(", ", roles));

            // Store authentication context for the operation
            var authContext = new AuthenticationContext
            {
                IsAuthenticated = true,
                Username = username,
                UserId = userId,
                Roles = roles,
                Principal = principal
            };

            // Add authentication context to the operation context
            if (OperationContext.Current != null)
            {
                OperationContext.Current.Extensions.Add(new AuthenticationContextExtension(authContext));
            }

            return authContext;
        }
        catch (FaultException)
        {
            // Re-throw fault exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("wcf.auth.error", "exception");
            activity?.SetTag("wcf.auth.error_type", ex.GetType().Name);
            
            var logger = instanceContext.Extensions.Find<ServiceProviderExtension>()?.ServiceProvider
                ?.GetService<ILogger<JwtMessageInspector>>();
            logger?.LogError(ex, "Error during WCF JWT authentication");
            
            if (_requireAuthentication)
            {
                throw new FaultException("Authentication error occurred", 
                    new FaultCode("AuthenticationError"));
            }
            
            return new AuthenticationContext { IsAuthenticated = false, Username = "ErrorUser" };
        }
    }

    public void BeforeSendReply(ref Message reply, object? correlationState)
    {
        // Cleanup or logging before sending reply if needed
        if (correlationState is AuthenticationContext authContext)
        {
            // Log successful operation completion
            var logger = OperationContext.Current?.Extensions.Find<ServiceProviderExtension>()?.ServiceProvider
                ?.GetService<ILogger<JwtMessageInspector>>();
            
            logger?.LogDebug("WCF operation completed for authenticated user: {Username}", 
                authContext.Username);
        }
    }

    /// <summary>
    /// Extracts JWT token from WCF message headers
    /// Supports various header formats and transport methods
    /// </summary>
    private string? ExtractJwtTokenFromMessage(Message message)
    {
        try
        {
            // Method 1: Check message headers for Authorization header
            var headers = message.Headers;
            if (headers != null)
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    if (header.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                        header.Name.Equals("X-Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var headerValue = ExtractHeaderStringValue(headers, i);
                            if (!string.IsNullOrEmpty(headerValue) && 
                                headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                return headerValue.Substring("Bearer ".Length).Trim();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail - try other methods
                            Console.WriteLine($"Error extracting header {header.Name}: {ex.Message}");
                        }
                    }
                }
            }

            // Method 2: Check message properties for HTTP request information
            if (message.Properties != null)
            {
                foreach (var property in message.Properties)
                {
                    if (property.Key.Contains("Http", StringComparison.OrdinalIgnoreCase))
                    {
                        var authHeader = ExtractAuthHeaderFromProperty(property.Value);
                        if (!string.IsNullOrEmpty(authHeader) && 
                            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            return authHeader.Substring("Bearer ".Length).Trim();
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting JWT token from WCF message: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts header string value using reflection
    /// </summary>
    private string? ExtractHeaderStringValue(MessageHeaders headers, int headerIndex)
    {
        try
        {
            var headersType = headers.GetType();
            var getReaderMethod = headersType.GetMethod("GetReaderAtHeader");
            if (getReaderMethod != null)
            {
                var reader = getReaderMethod.Invoke(headers, new object[] { headerIndex });
                if (reader != null)
                {
                    var readMethod = reader.GetType().GetMethod("ReadElementContentAsString", Type.EmptyTypes);
                    if (readMethod != null)
                    {
                        return readMethod.Invoke(reader, null)?.ToString();
                    }
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts authorization header from HTTP property using reflection
    /// </summary>
    private string? ExtractAuthHeaderFromProperty(object? httpProperty)
    {
        if (httpProperty == null) return null;

        try
        {
            var propertyType = httpProperty.GetType();
            
            // Look for Headers property
            var headersProperty = propertyType.GetProperty("Headers");
            if (headersProperty != null)
            {
                var headers = headersProperty.GetValue(httpProperty);
                if (headers != null)
                {
                    // Try to get Authorization header
                    var headersType = headers.GetType();
                    var getMethod = headersType.GetMethod("get_Item", new[] { typeof(string) });
                    if (getMethod != null)
                    {
                        var authValue = getMethod.Invoke(headers, new object[] { "Authorization" });
                        return authValue?.ToString();
                    }

                    // Try indexer
                    var indexer = headersType.GetProperty("Item", new[] { typeof(string) });
                    if (indexer != null)
                    {
                        var authValue = indexer.GetValue(headers, new object[] { "Authorization" });
                        return authValue?.ToString();
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Authentication context to store user information
/// </summary>
public class AuthenticationContext
{
    public bool IsAuthenticated { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public List<string> Roles { get; set; } = new();
    public System.Security.Claims.ClaimsPrincipal? Principal { get; set; }
}

/// <summary>
/// Extension to store authentication context in operation context
/// </summary>
public class AuthenticationContextExtension : IExtension<OperationContext>
{
    public AuthenticationContext AuthenticationContext { get; }

    public AuthenticationContextExtension(AuthenticationContext authenticationContext)
    {
        AuthenticationContext = authenticationContext;
    }

    public void Attach(OperationContext owner) { }
    public void Detach(OperationContext owner) { }
}

/// <summary>
/// Extension to store service provider in instance context
/// </summary>
public class ServiceProviderExtension : IExtension<InstanceContext>
{
    public IServiceProvider ServiceProvider { get; }

    public ServiceProviderExtension(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public void Attach(InstanceContext owner) { }
    public void Detach(InstanceContext owner) { }
}

/// <summary>
/// Helper methods for WCF JWT authentication
/// </summary>
public static class WcfAuthenticationHelper
{
    /// <summary>
    /// Gets the current authenticated user from the operation context
    /// </summary>
    public static AuthenticationContext? GetCurrentAuthenticationContext()
    {
        return OperationContext.Current?.Extensions.Find<AuthenticationContextExtension>()?.AuthenticationContext;
    }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    public static bool IsAuthenticated()
    {
        return GetCurrentAuthenticationContext()?.IsAuthenticated == true;
    }

    /// <summary>
    /// Gets the current username
    /// </summary>
    public static string GetCurrentUsername()
    {
        return GetCurrentAuthenticationContext()?.Username ?? "Anonymous";
    }

    /// <summary>
    /// Gets the current user roles
    /// </summary>
    public static List<string> GetCurrentUserRoles()
    {
        return GetCurrentAuthenticationContext()?.Roles ?? new List<string>();
    }

    /// <summary>
    /// Checks if the current user has a specific role
    /// </summary>
    public static bool HasRole(string role)
    {
        var roles = GetCurrentUserRoles();
        return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Requires authentication and throws if not authenticated
    /// </summary>
    public static void RequireAuthentication()
    {
        if (!IsAuthenticated())
        {
            throw new FaultException("Authentication required", new FaultCode("AuthenticationRequired"));
        }
    }

    /// <summary>
    /// Requires a specific role and throws if not authorized
    /// </summary>
    public static void RequireRole(string role)
    {
        RequireAuthentication();
        if (!HasRole(role))
        {
            throw new FaultException($"Role '{role}' required", new FaultCode("AuthorizationFailed"));
        }
    }
} 