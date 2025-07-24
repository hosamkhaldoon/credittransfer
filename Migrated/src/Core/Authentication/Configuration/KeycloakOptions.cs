namespace CreditTransfer.Core.Authentication.Configuration;

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// The Keycloak authority URL (e.g., https://keycloak.example.com/realms/myrealm)
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The audience for JWT tokens (typically the client ID)
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Whether to require HTTPS for metadata discovery
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience claim
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer claim
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance for token validation
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The Keycloak realm name
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// The client ID for this application
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret (if using confidential client)
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a confidential client
    /// </summary>
    public bool IsConfidentialClient { get; set; } = true;

    /// <summary>
    /// Additional scopes to request
    /// </summary>
    public List<string> Scopes { get; set; } = new() { "openid", "profile", "email" };

    /// <summary>
    /// Role claim type in JWT tokens
    /// </summary>
    public string RoleClaimType { get; set; } = "realm_access.roles";

    /// <summary>
    /// Name claim type in JWT tokens
    /// </summary>
    public string NameClaimType { get; set; } = "preferred_username";
} 