# Credit Transfer System - Complete Keycloak Setup Script (PowerShell) - Enhanced Version
# This script automates the entire Keycloak configuration for testing with proper roles
# Includes public client conversion to eliminate client_secret requirement

<#
.SYNOPSIS
    Complete Keycloak setup script with Basic Auth user management and optional public client conversion

.DESCRIPTION
    This script sets up a complete Keycloak environment for the Credit Transfer system.
    It can optionally convert the client to public configuration to eliminate client_secret requirements.
    It also provides comprehensive Basic Auth user management capabilities.

.PARAMETER KeycloakUrl
    The base URL of the Keycloak server (default: http://localhost:30080)

.PARAMETER ConvertToPublic
    Convert the client to public configuration after creation (eliminates client_secret requirement)

.PARAMETER PublicClientOnly
    Create a public client from the start (no client_secret required)

.PARAMETER AddBasicAuthUser
    Add a new Basic Auth user to Keycloak

.PARAMETER NewUsername
    Username for the new Basic Auth user

.PARAMETER NewPassword
    Password for the new Basic Auth user

.PARAMETER NewEmail
    Email for the new Basic Auth user

.PARAMETER NewFirstName
    First name for the new Basic Auth user

.PARAMETER NewLastName
    Last name for the new Basic Auth user

.PARAMETER NewUserRoles
    Roles to assign to the new Basic Auth user (default: credit-transfer-user)

.PARAMETER RemoveUser
    Remove an existing user from Keycloak

.PARAMETER RemoveUsername
    Username of the user to remove

.PARAMETER ListUsers
    List all users in the Keycloak realm with their roles

.PARAMETER UpdateUser
    Update an existing user's password and/or roles

.PARAMETER UpdateUsername
    Username of the user to update

.PARAMETER UpdatePassword
    New password for the user (leave empty to keep current password)

.PARAMETER UpdateRoles
    New roles for the user (replaces existing roles)

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1
    Standard setup with confidential client (requires client_secret)

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1 -ConvertToPublic
    Standard setup, then convert to public client (no client_secret needed)

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1 -PublicClientOnly
    Create public client from the start (no client_secret needed)

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser -NewUsername "newuser" -NewPassword "password123" -NewEmail "newuser@company.com" -NewFirstName "New" -LastName "User" -NewUserRoles @("credit-transfer-operator", "credit-transfer-user")
    Add a new Basic Auth user with operator privileges

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1 -RemoveUser -RemoveUsername "olduser"
    Remove an existing user

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1 -ListUsers
    List all users and their roles

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1 -UpdateUser -UpdateUsername "existinguser" -UpdatePassword "newpassword123" -UpdateRoles @("credit-transfer-admin", "credit-transfer-user")
    Update a user's password and roles

.EXAMPLE
    .\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://keycloak.company.com:8080"
    Use a different Keycloak server URL
#>

param(
    [string]$KeycloakUrl = "http://localhost:30080",
    [string]$AdminUser = "admin",
    [string]$AdminPass = "admin123",
    [string]$RealmName = "credittransfer",
    [string]$ClientId = "credittransfer-api",
    [string]$ClientSecret = "credittransfer-secret-2024",
    [switch]$ConvertToPublic = $false,
    [switch]$PublicClientOnly = $false,
    
    # Basic Auth User Management Parameters
    [switch]$AddBasicAuthUser = $false,
    [string]$NewUsername = "",
    [string]$NewPassword = "",
    [string]$NewEmail = "",
    [string]$NewFirstName = "",
    [string]$NewLastName = "",
    [string[]]$NewUserRoles = @("credit-transfer-user"),
    [switch]$RemoveUser = $false,
    [string]$RemoveUsername = "",
    [switch]$ListUsers = $false,
    [switch]$UpdateUser = $false,
    [string]$UpdateUsername = "",
    [string]$UpdatePassword = "",
    [string[]]$UpdateRoles = @()
)

# Colors for output
$Colors = @{
    Red = "Red"
    Green = "Green"
    Yellow = "Yellow"
    Blue = "Cyan"
    White = "White"
}

function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor $Colors.Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor $Colors.Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor $Colors.Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $Colors.Red
}

function Wait-ForKeycloak {
    Write-Status "Waiting for Keycloak to be ready..."
    $maxAttempts = 30
    $attempt = 1
    
    while ($attempt -le $maxAttempts) {
        try {
            $response = Invoke-WebRequest -Uri "$KeycloakUrl" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Success "Keycloak is ready!"
                return $true
            }
        }
        catch {
            Write-Host "." -NoNewline
        }
        
        Start-Sleep -Seconds 2
        $attempt++
    }
    
    Write-Error "Keycloak failed to start within expected time"
    return $false
}

function Get-AdminToken {
    Write-Status "Getting admin access token..."
    
    $body = @{
        grant_type = "password"
        client_id = "admin-cli"
        username = $AdminUser
        password = $AdminPass
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
            -Method Post `
            -ContentType "application/x-www-form-urlencoded" `
            -Body $body `
            -ErrorAction Stop
        
        if ($response.access_token) {
            Write-Success "Admin token obtained"
            return $response.access_token
        }
    }
    catch {
        Write-Error "Failed to get admin token: $($_.Exception.Message)"
        return $null
    }
    
    Write-Error "Failed to get admin token"
    return $null
}

function Test-RealmExists {
    param([string]$AdminToken)
    
    Write-Status "Checking if realm exists: $RealmName"
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName" `
            -Method Get `
            -Headers $headers `
            -UseBasicParsing `
            -ErrorAction Stop
        
        if ($response.StatusCode -eq 200) {
            Write-Success "Realm $RealmName exists"
            return $true
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Warning "Realm $RealmName does not exist"
            return $false
        }
        else {
            Write-Error "Error checking realm: $($_.Exception.Message)"
            return $false
        }
    }
    
    return $false
}

function New-Realm {
    param([string]$AdminToken)
    
    Write-Status "Creating realm: $RealmName"
    
    $realmConfig = @{
        realm = $RealmName
        displayName = "Credit Transfer System"
        enabled = $true
        registrationAllowed = $false
        loginWithEmailAllowed = $true
        duplicateEmailsAllowed = $false
        resetPasswordAllowed = $true
        editUsernameAllowed = $false
        bruteForceProtected = $true
        permanentLockout = $false
        maxFailureWaitSeconds = 900
        minimumQuickLoginWaitSeconds = 60
        waitIncrementSeconds = 60
        quickLoginCheckMilliSeconds = 1000
        maxDeltaTimeSeconds = 43200
        failureFactor = 30
        accessTokenLifespan = 3600
        accessTokenLifespanForImplicitFlow = 900
        ssoSessionIdleTimeout = 1800
        ssoSessionMaxLifespan = 36000
        offlineSessionIdleTimeout = 2592000
        accessCodeLifespan = 60
        accessCodeLifespanUserAction = 300
        accessCodeLifespanLogin = 1800
        actionTokenGeneratedByAdminLifespan = 43200
        actionTokenGeneratedByUserLifespan = 300
    } | ConvertTo-Json -Depth 10
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms" `
            -Method Post `
            -Headers $headers `
            -Body $realmConfig `
            -UseBasicParsing `
            -ErrorAction Stop
        
        Write-Success "Realm created successfully"
        return $true
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 409) {
            Write-Warning "Realm already exists"
            return $true
        }
        else {
            Write-Error "Failed to create realm: $($_.Exception.Message)"
            return $false
        }
    }
}

function New-Client {
    param([string]$AdminToken)
    
    Write-Status "Creating client: $ClientId"
    
    $clientConfig = @{
        clientId = $ClientId
        name = "Credit Transfer API Client"
        description = "Client for Credit Transfer API authentication (Public Client - No Secret Required)"
        enabled = $true
        clientAuthenticatorType = "client-secret"
        publicClient = $true
        bearerOnly = $false
        consentRequired = $false
        standardFlowEnabled = $true
        implicitFlowEnabled = $false
        directAccessGrantsEnabled = $true
        serviceAccountsEnabled = $false
        authorizationServicesEnabled = $false
        redirectUris = @("*")
        webOrigins = @("*")
        notBefore = 0
        protocol = "openid-connect"
        attributes = @{
            "saml.assertion.signature" = "false"
            "saml.force.post.binding" = "false"
            "saml.multivalued.roles" = "false"
            "saml.encrypt" = "false"
            "saml.server.signature" = "false"
            "saml.server.signature.keyinfo.ext" = "false"
            "exclude.session.state.from.auth.response" = "false"
            "saml_force_name_id_format" = "false"
            "saml.client.signature" = "false"
            "tls.client.certificate.bound.access.tokens" = "false"
            "saml.authnstatement" = "false"
            "display.on.consent.screen" = "false"
            "saml.onetimeuse.condition" = "false"
            "access.token.lifespan" = "3600"
            "pkce.code.challenge.method" = "S256"
        }
        authenticationFlowBindingOverrides = @{}
        fullScopeAllowed = $true
        nodeReRegistrationTimeout = -1
        defaultClientScopes = @("web-origins", "role_list", "profile", "roles", "email")
        optionalClientScopes = @("address", "phone", "offline_access", "microprofile-jwt")
    } | ConvertTo-Json -Depth 10
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/clients" `
            -Method Post `
            -Headers $headers `
            -Body $clientConfig `
            -UseBasicParsing `
            -ErrorAction Stop
        
        Write-Success "Public client created successfully (no client secret required)"
        return $true
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 409) {
            Write-Warning "Client already exists"
            return $true
        }
        else {
            Write-Error "Failed to create client: $($_.Exception.Message)"
            return $false
        }
    }
}

function New-RealmRole {
    param(
        [string]$AdminToken,
        [string]$RoleName,
        [string]$Description
    )
    
    Write-Status "Creating realm role: $RoleName"
    
    $roleConfig = @{
        name = $RoleName
        description = $Description
        composite = $false
        clientRole = $false
        containerId = $RealmName
    } | ConvertTo-Json -Depth 10
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/roles" `
            -Method Post `
            -Headers $headers `
            -Body $roleConfig `
            -UseBasicParsing `
            -ErrorAction Stop
        
        Write-Success "Role $RoleName created successfully"
        return $true
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 409) {
            Write-Warning "Role $RoleName already exists"
            return $true
        }
        else {
            Write-Error "Failed to create role $RoleName : $($_.Exception.Message)"
            return $false
        }
    }
}

function New-User {
    param(
        [string]$AdminToken,
        [string]$Username,
        [string]$Password,
        [string]$Email,
        [string]$FirstName,
        [string]$LastName
    )
    
    Write-Status "Creating user: $Username"
    
    $userConfig = @{
        username = $Username
        email = $Email
        firstName = $FirstName
        lastName = $LastName
        enabled = $true
        emailVerified = $true
        credentials = @(
            @{
                type = "password"
                value = $Password
                temporary = $false
            }
        )
        attributes = @{}
        requiredActions = @()
    } | ConvertTo-Json -Depth 10
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/users" `
            -Method Post `
            -Headers $headers `
            -Body $userConfig `
            -UseBasicParsing `
            -ErrorAction Stop
        
        Write-Success "User $Username created successfully (Status: $($response.StatusCode))"
        return $true
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 409) {
            Write-Warning "User $Username already exists"
            return $true
        }
        else {
            Write-Error "Failed to create user $Username : $($_.Exception.Message)"
            if ($_.Exception.Response) {
                try {
                    $errorBody = $_.Exception.Response.GetResponseStream()
                    $reader = New-Object System.IO.StreamReader($errorBody)
                    $errorContent = $reader.ReadToEnd()
                    Write-Error "Response body: $errorContent"
                }
                catch {
                    Write-Error "Could not read error response body"
                }
            }
            return $false
        }
    }
}

function Get-UserId {
    param(
        [string]$AdminToken,
        [string]$Username
    )
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        # Try exact username search first
        $response = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users?username=$Username&exact=true" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        if ($response -and $response.Count -gt 0) {
            Write-Status "Found user $Username with ID: $($response[0].id) (enabled: $($response[0].enabled))"
            return $response[0].id
        }
        
        # Fallback: search all users and filter
        Write-Warning "Exact search failed, trying broader search for user: $Username"
        $allUsersResponse = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        $matchingUser = $allUsersResponse | Where-Object { $_.username -eq $Username }
        if ($matchingUser) {
            Write-Status "Found user $Username in broader search with ID: $($matchingUser.id) (enabled: $($matchingUser.enabled))"
            return $matchingUser.id
        }
    }
    catch {
        Write-Error "Failed to get user ID for $Username : $($_.Exception.Message)"
        if ($_.Exception.Response) {
            try {
                $errorBody = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorBody)
                $errorContent = $reader.ReadToEnd()
                Write-Error "Response body: $errorContent"
            }
            catch {
                Write-Error "Could not read error response body"
            }
        }
    }
    
    Write-Warning "User $Username not found in any search method"
    return $null
}

function Remove-UserIfExists {
    param(
        [string]$AdminToken,
        [string]$Username
    )
    
    Write-Status "Checking if user $Username exists for cleanup..."
    $userId = Get-UserId -AdminToken $AdminToken -Username $Username
    if ($userId) {
        Write-Status "Removing existing user $Username to recreate properly..."
        $headers = @{
            Authorization = "Bearer $AdminToken"
            "Content-Type" = "application/json"
        }
        
        try {
            Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId" `
                -Method Delete `
                -Headers $headers `
                -UseBasicParsing `
                -ErrorAction Stop
            
            Write-Success "User $Username removed successfully"
            # Wait longer for user deletion to propagate
            Start-Sleep -Seconds 5
            return $true
        }
        catch {
            Write-Error "Failed to remove user $Username : $($_.Exception.Message)"
            return $false
        }
    }
    else {
        Write-Status "User $Username does not exist, ready for creation"
    }
    return $true
}

function Add-UserToRole {
    param(
        [string]$AdminToken,
        [string]$Username,
        [string]$RoleName
    )
    
    Write-Status "Assigning role $RoleName to user $Username"
    
    # Get user ID with retry logic
    $userId = $null
    $maxRetries = 5
    $retryCount = 0
    
    while (-not $userId -and $retryCount -lt $maxRetries) {
        $userId = Get-UserId -AdminToken $AdminToken -Username $Username
        if (-not $userId) {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Warning "User lookup failed, retrying in 3 seconds... (attempt $retryCount/$maxRetries)"
                Start-Sleep -Seconds 3
            }
        }
    }
    
    if (-not $userId) {
        Write-Error "Cannot find user $Username after $maxRetries attempts"
        return $false
    }
    
    # Get role details
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        Write-Status "Getting role details for: $RoleName"
        $roleResponse = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/roles/$RoleName" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        Write-Status "Role found - ID: $($roleResponse.id), Name: $($roleResponse.name)"
        
        # Create role mapping array - correct Keycloak format
        $roleMapping = @(
            @{
                id = $roleResponse.id
                name = $roleResponse.name
                composite = $false
                clientRole = $false
                containerId = $roleResponse.containerId
            }
        )
        
        $roleMappingJson = $roleMapping | ConvertTo-Json -Depth 5
        Write-Status "Role mapping JSON: $roleMappingJson"
        
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/role-mappings/realm" `
            -Method Post `
            -Headers $headers `
            -Body $roleMappingJson `
            -UseBasicParsing `
            -ErrorAction Stop
        
        Write-Success "Role $RoleName assigned to user $Username (Status: $($response.StatusCode))"
        return $true
    }
    catch {
        Write-Error "Failed to assign role $RoleName to user $Username : $($_.Exception.Message)"
        Write-Error "Status Code: $($_.Exception.Response.StatusCode.value__)"
        
        if ($_.Exception.Response) {
            try {
                $errorBody = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorBody)
                $errorContent = $reader.ReadToEnd()
                Write-Error "Response body: $errorContent"
            }
            catch {
                Write-Error "Could not read error response body"
            }
        }
        return $false
    }
}

function New-UserWithRoles {
    param(
        [string]$AdminToken,
        [string]$Username,
        [string]$Password,
        [string]$Email,
        [string]$FirstName,
        [string]$LastName,
        [string[]]$Roles
    )
    
    Write-Status "Creating user $Username with roles: $($Roles -join ', ')"
    
    # Remove user if exists to ensure clean state
    if (-not (Remove-UserIfExists -AdminToken $AdminToken -Username $Username)) {
        Write-Error "Failed to clean up existing user $Username"
        return $false
    }
    
    # Create user
    if (New-User -AdminToken $AdminToken -Username $Username -Password $Password -Email $Email -FirstName $FirstName -LastName $LastName) {
        # Wait longer for user creation to complete in Keycloak
        Write-Status "Waiting for user creation to complete..."
        Start-Sleep -Seconds 8
        
        # Verify user was created
        $userId = Get-UserId -AdminToken $AdminToken -Username $Username
        if (-not $userId) {
            Write-Error "User $Username was not created successfully"
            return $false
        }
        
        Write-Success "User $Username created successfully with ID: $userId"
        
        # Assign roles with better error handling
        $roleAssignmentSuccess = $true
        foreach ($role in $Roles) {
            if (-not (Add-UserToRole -AdminToken $AdminToken -Username $Username -RoleName $role)) {
                Write-Error "Failed to assign role $role to user $Username"
                $roleAssignmentSuccess = $false
            }
            Start-Sleep -Seconds 2  # Increased delay between role assignments
        }
        
        if ($roleAssignmentSuccess) {
            Write-Success "User $Username created with all roles successfully"
            return $true
        }
        else {
            Write-Warning "User $Username created but some role assignments failed"
            return $true  # User exists, partial success
        }
    }
    else {
        Write-Error "Failed to create user $Username"
        return $false
    }
}

function Test-Authentication {
    param(
        [string]$Username,
        [string]$Password,
        [bool]$UseClientSecret = $true
    )
    
    Write-Status "Testing authentication for user: $Username in realm: $RealmName"
    
    $body = @{
        grant_type = "password"
        client_id = $ClientId
        username = $Username
        password = $Password
        scope = "openid profile email"
    }
    
    # Only add client_secret if UseClientSecret is true (for confidential clients)
    if ($UseClientSecret) {
        $body.client_secret = $ClientSecret
        Write-Status "Using confidential client authentication (with client_secret)"
    }
    else {
        Write-Status "Using public client authentication (no client_secret required)"
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token" `
            -Method Post `
            -ContentType "application/x-www-form-urlencoded" `
            -Body $body `
            -ErrorAction Stop
        
        if ($response.access_token) {
            Write-Success "✅ Authentication successful for $Username in realm $RealmName"
            $token = $response.access_token
            Write-Status "Token preview: $($token.Substring(0, [Math]::Min(50, $token.Length)))..."
            return $true
        }
    }
    catch {
        Write-Error "❌ Authentication failed for $Username in realm $RealmName : $($_.Exception.Message)"
        if ($_.Exception.Response) {
            try {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorContent = $reader.ReadToEnd()
                Write-Error "Response body: $errorContent"
            }
            catch {
                Write-Error "Could not read error response"
            }
        }
        return $false
    }
    
    Write-Error "❌ Authentication failed for $Username - no token received"
    return $false
}

function Get-ClientInfo {
    param([string]$AdminToken)
    
    Write-Status "Getting current client configuration for: $ClientId"
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        # Get all clients and find ours
        $clients = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/clients" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        $client = $clients | Where-Object { $_.clientId -eq $ClientId }
        
        if ($client) {
            Write-Success "Found client: $ClientId"
            Write-Status "Current configuration:"
            Write-Host "  - Public Client: $($client.publicClient)" -ForegroundColor $(if($client.publicClient) { "Green" } else { "Red" })
            Write-Host "  - Client Secret: $(if($client.secret) { "Required" } else { "Not Required" })" -ForegroundColor $(if($client.secret) { "Red" } else { "Green" })
            Write-Host "  - Direct Access Grants: $($client.directAccessGrantsEnabled)" -ForegroundColor $(if($client.directAccessGrantsEnabled) { "Green" } else { "Red" })
            return $client
        }
        else {
            Write-Error "Client $ClientId not found"
            return $null
        }
    }
    catch {
        Write-Error "Failed to get client info: $($_.Exception.Message)"
        return $null
    }
}

function Convert-ToPublicClient {
    param([string]$AdminToken, [object]$Client)
    
    if ($Client.publicClient -eq $true) {
        Write-Success "Client is already configured as public (no client secret required)"
        return $true
    }
    
    Write-Status "Converting client to public configuration..."
    
    # Update client configuration to public
    $Client.publicClient = $true
    $Client.serviceAccountsEnabled = $false  # Public clients can't have service accounts
    
    # Remove secret-related properties
    if ($Client.PSObject.Properties.Name -contains "secret") {
        $Client.PSObject.Properties.Remove("secret")
    }
    
    # Add PKCE support for enhanced security
    if (-not $Client.attributes) {
        $Client.attributes = @{}
    }
    
    # Handle attributes object properly for PowerShell
    if ($Client.attributes -is [PSCustomObject]) {
        $Client.attributes | Add-Member -MemberType NoteProperty -Name "pkce.code.challenge.method" -Value "S256" -Force
    }
    else {
        $Client.attributes["pkce.code.challenge.method"] = "S256"
    }
    
    # Remove client secret creation time if it exists
    if ($Client.attributes -and $Client.attributes."client.secret.creation.time") {
        if ($Client.attributes -is [PSCustomObject]) {
            $Client.attributes.PSObject.Properties.Remove("client.secret.creation.time")
        }
        else {
            $Client.attributes.Remove("client.secret.creation.time")
        }
    }
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    $clientJson = $Client | ConvertTo-Json -Depth 10
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/clients/$($Client.id)" `
            -Method Put `
            -Headers $headers `
            -Body $clientJson `
            -UseBasicParsing `
            -ErrorAction Stop
        
        Write-Success "Client successfully converted to public configuration"
        Write-Success "🎉 Client secret is no longer required for authentication!"
        return $true
    }
    catch {
        Write-Error "Failed to update client: $($_.Exception.Message)"
        if ($_.Exception.Response) {
            try {
                $errorBody = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorBody)
                $errorText = $reader.ReadToEnd()
                Write-Error "Error details: $errorText"
            }
            catch {
                Write-Error "Could not read error response body"
            }
        }
        return $false
    }
}

function Test-PublicClientAuth {
    param([string]$Username = "admin", [string]$Password = "admin123")
    
    Write-Status "Testing public client authentication (no client_secret)..."
    
    $body = @{
        grant_type = "password"
        client_id = $ClientId
        username = $Username
        password = $Password
        scope = "openid profile email"
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token" `
            -Method Post `
            -ContentType "application/x-www-form-urlencoded" `
            -Body $body `
            -ErrorAction Stop
        
        if ($response.access_token) {
            Write-Success "✅ Public client authentication successful!"
            Write-Success "Token obtained with expires_in: $($response.expires_in) seconds"
            return $true
        }
    }
    catch {
        Write-Error "Public client authentication test failed: $($_.Exception.Message)"
        return $false
    }
    
    return $false
}

function Confirm-UserInCorrectRealm {
    param(
        [string]$AdminToken,
        [string]$Username
    )
    
    Write-Status "Verifying user $Username exists in realm $RealmName"
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        # Search for user in the specified realm
        $response = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users?username=$Username&exact=true" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        if ($response -and $response.Count -gt 0) {
            $user = $response[0]
            Write-Success "✅ User $Username confirmed in realm $RealmName (ID: $($user.id), enabled: $($user.enabled))"
            
            # Get user roles
            try {
                $userRoles = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$($user.id)/role-mappings/realm" `
                    -Method Get `
                    -Headers $headers `
                    -ErrorAction Stop
                
                if ($userRoles) {
                    $roleNames = $userRoles | ForEach-Object { $_.name }
                    Write-Status "  Roles: $($roleNames -join ', ')"
                }
            }
            catch {
                Write-Warning "Could not retrieve roles for user $Username"
            }
            
            return $true
        }
        else {
            Write-Error "❌ User $Username NOT found in realm $RealmName"
            return $false
        }
    }
    catch {
        Write-Error "❌ Failed to verify user $Username in realm $RealmName : $($_.Exception.Message)"
        return $false
    }
}

# ====================================================================================
# BASIC AUTH USER MANAGEMENT FUNCTIONS
# ====================================================================================

function Show-BasicAuthUserManagementMenu {
    Write-Host ""
    Write-Host "🔐 BASIC AUTH USER MANAGEMENT" -ForegroundColor $Colors.Green
    Write-Host "=============================" -ForegroundColor $Colors.Green
    Write-Host ""
    Write-Host "Available Commands:" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "ADD USER:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser -NewUsername 'john.doe' -NewPassword 'SecurePass123!' -NewEmail 'john.doe@company.com' -NewFirstName 'John' -NewLastName 'Doe' -NewUserRoles @('credit-transfer-operator', 'credit-transfer-user')" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "REMOVE USER:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -RemoveUser -RemoveUsername 'john.doe'" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "LIST USERS:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -ListUsers" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "UPDATE USER:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -UpdateUser -UpdateUsername 'john.doe' -UpdatePassword 'NewPass123!' -UpdateRoles @('credit-transfer-admin', 'credit-transfer-user')" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Available Roles:" -ForegroundColor $Colors.White
    Write-Host "  • credit-transfer-admin (Full administrative access)" -ForegroundColor $Colors.White
    Write-Host "  • credit-transfer-operator (Standard operations)" -ForegroundColor $Colors.White
    Write-Host "  • credit-transfer-user (Basic user access)" -ForegroundColor $Colors.White
    Write-Host "  • system-user (Service/system account)" -ForegroundColor $Colors.White
    Write-Host ""
}

function Add-BasicAuthUser {
    param([string]$AdminToken)
    
    Write-Host ""
    Write-Host "➕ ADDING NEW BASIC AUTH USER" -ForegroundColor $Colors.Green
    Write-Host "==============================" -ForegroundColor $Colors.Green
    Write-Host ""
    
    # Validate required parameters
    if ([string]::IsNullOrEmpty($NewUsername)) {
        Write-Error "NewUsername is required when using -AddBasicAuthUser"
        Write-Host "Example: -NewUsername 'john.doe'" -ForegroundColor $Colors.Yellow
        return $false
    }
    
    if ([string]::IsNullOrEmpty($NewPassword)) {
        Write-Error "NewPassword is required when using -AddBasicAuthUser"
        Write-Host "Example: -NewPassword 'SecurePass123!'" -ForegroundColor $Colors.Yellow
        return $false
    }
    
    if ([string]::IsNullOrEmpty($NewEmail)) {
        $NewEmail = "$NewUsername@credittransfer.local"
        Write-Warning "No email specified, using: $NewEmail"
    }
    
    if ([string]::IsNullOrEmpty($NewFirstName)) {
        $NewFirstName = $NewUsername
        Write-Warning "No first name specified, using username: $NewFirstName"
    }
    
    if ([string]::IsNullOrEmpty($NewLastName)) {
        $NewLastName = "User"
        Write-Warning "No last name specified, using: $NewLastName"
    }
    
    Write-Status "Creating Basic Auth user with the following details:"
    Write-Host "  Username: $NewUsername" -ForegroundColor $Colors.White
    Write-Host "  Email: $NewEmail" -ForegroundColor $Colors.White
    Write-Host "  Name: $NewFirstName $NewLastName" -ForegroundColor $Colors.White
    Write-Host "  Roles: $($NewUserRoles -join ', ')" -ForegroundColor $Colors.White
    Write-Host ""
    
    # Validate roles exist
    $validRoles = @("credit-transfer-admin", "credit-transfer-operator", "credit-transfer-user", "system-user")
    foreach ($role in $NewUserRoles) {
        if ($role -notin $validRoles) {
            Write-Error "Invalid role: $role. Valid roles are: $($validRoles -join ', ')"
            return $false
        }
    }
    
    # Create user with roles
    $success = New-UserWithRoles -AdminToken $AdminToken -Username $NewUsername -Password $NewPassword -Email $NewEmail -FirstName $NewFirstName -LastName $NewLastName -Roles $NewUserRoles
    
    if ($success) {
        Write-Success "✅ Basic Auth user '$NewUsername' created successfully!"
        Write-Host ""
        Write-Host "User Details:" -ForegroundColor $Colors.White
        Write-Host "  Username: $NewUsername" -ForegroundColor $Colors.White
        Write-Host "  Password: $NewPassword" -ForegroundColor $Colors.White
        Write-Host "  Email: $NewEmail" -ForegroundColor $Colors.White
        Write-Host "  Roles: $($NewUserRoles -join ', ')" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "Basic Auth Header (Base64): $(Get-BasicAuthHeader -Username $NewUsername -Password $NewPassword)" -ForegroundColor $Colors.Green
        Write-Host ""
        
        # Test authentication
        Write-Status "Testing authentication for new user..."
        if (Test-Authentication -Username $NewUsername -Password $NewPassword -UseClientSecret $false) {
            Write-Success "✅ Authentication test successful!"
        }
        else {
            Write-Warning "⚠️ Authentication test failed - user may need time to propagate"
        }
        
        return $true
    }
    else {
        Write-Error "❌ Failed to create Basic Auth user '$NewUsername'"
        return $false
    }
}

function Remove-BasicAuthUser {
    param([string]$AdminToken)
    
    Write-Host ""
    Write-Host "🗑️ REMOVING BASIC AUTH USER" -ForegroundColor $Colors.Yellow
    Write-Host "============================" -ForegroundColor $Colors.Yellow
    Write-Host ""
    
    if ([string]::IsNullOrEmpty($RemoveUsername)) {
        Write-Error "RemoveUsername is required when using -RemoveUser"
        Write-Host "Example: -RemoveUsername 'john.doe'" -ForegroundColor $Colors.Yellow
        return $false
    }
    
    Write-Warning "⚠️ You are about to PERMANENTLY remove user: $RemoveUsername"
    Write-Host "This action cannot be undone!" -ForegroundColor $Colors.Red
    Write-Host ""
    
    # Get user info first
    $userId = Get-UserId -AdminToken $AdminToken -Username $RemoveUsername
    if (-not $userId) {
        Write-Error "User '$RemoveUsername' not found in realm '$RealmName'"
        return $false
    }
    
    Write-Status "Found user '$RemoveUsername' with ID: $userId"
    
    # Confirm deletion
    $confirmation = Read-Host "Type 'DELETE' to confirm removal of user '$RemoveUsername'"
    if ($confirmation -ne "DELETE") {
        Write-Status "User removal cancelled"
        return $false
    }
    
    # Remove user
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId" `
            -Method Delete `
            -Headers $headers `
            -UseBasicParsing `
            -ErrorAction Stop
        
        Write-Success "✅ User '$RemoveUsername' removed successfully"
        return $true
    }
    catch {
        Write-Error "❌ Failed to remove user '$RemoveUsername': $($_.Exception.Message)"
        return $false
    }
}

function Show-AllBasicAuthUsers {
    param([string]$AdminToken)
    
    Write-Host ""
    Write-Host "👥 BASIC AUTH USERS LIST" -ForegroundColor $Colors.Blue
    Write-Host "========================" -ForegroundColor $Colors.Blue
    Write-Host ""
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        # Get all users
        $users = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        if ($users.Count -eq 0) {
            Write-Warning "No users found in realm '$RealmName'"
            return $true
        }
        
        Write-Status "Found $($users.Count) users in realm '$RealmName':"
        Write-Host ""
        
        foreach ($user in $users) {
            $status = if ($user.enabled) { "✅ Active" } else { "❌ Disabled" }
            Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor $Colors.Blue
            Write-Host "👤 Username: $($user.username)" -ForegroundColor $Colors.White
            Write-Host "   Email: $($user.email)" -ForegroundColor $Colors.White
            Write-Host "   Name: $($user.firstName) $($user.lastName)" -ForegroundColor $Colors.White
            Write-Host "   Status: $status" -ForegroundColor $(if ($user.enabled) { $Colors.Green } else { $Colors.Red })
            Write-Host "   Created: $(if ($user.createdTimestamp) { [DateTimeOffset]::FromUnixTimeMilliseconds($user.createdTimestamp).ToString('yyyy-MM-dd HH:mm:ss') } else { 'Unknown' })" -ForegroundColor $Colors.White
            
            # Get user roles
            try {
                $userRoles = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$($user.id)/role-mappings/realm" `
                    -Method Get `
                    -Headers $headers `
                    -ErrorAction Stop
                
                if ($userRoles -and $userRoles.Count -gt 0) {
                    $roleNames = $userRoles | ForEach-Object { $_.name }
                    Write-Host "   Roles: $($roleNames -join ', ')" -ForegroundColor $Colors.Yellow
                }
                else {
                    Write-Host "   Roles: None" -ForegroundColor $Colors.Red
                }
            }
            catch {
                Write-Host "   Roles: Error retrieving roles" -ForegroundColor $Colors.Red
            }
            
            # Generate Basic Auth header
            Write-Host "   Basic Auth: $(Get-BasicAuthHeader -Username $($user.username) -Password '[PASSWORD]')" -ForegroundColor $Colors.Green
            Write-Host ""
        }
        
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor $Colors.Blue
        Write-Host ""
        Write-Success "Listed $($users.Count) users successfully"
        
        return $true
    }
    catch {
        Write-Error "❌ Failed to list users: $($_.Exception.Message)"
        return $false
    }
}

function Update-BasicAuthUser {
    param([string]$AdminToken)
    
    Write-Host ""
    Write-Host "✏️ UPDATING BASIC AUTH USER" -ForegroundColor $Colors.Yellow
    Write-Host "============================" -ForegroundColor $Colors.Yellow
    Write-Host ""
    
    if ([string]::IsNullOrEmpty($UpdateUsername)) {
        Write-Error "UpdateUsername is required when using -UpdateUser"
        Write-Host "Example: -UpdateUsername 'john.doe'" -ForegroundColor $Colors.Yellow
        return $false
    }
    
    # Get user ID
    $userId = Get-UserId -AdminToken $AdminToken -Username $UpdateUsername
    if (-not $userId) {
        Write-Error "User '$UpdateUsername' not found in realm '$RealmName'"
        return $false
    }
    
    Write-Status "Found user '$UpdateUsername' with ID: $userId"
    
    $headers = @{
        Authorization = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    $updateCount = 0
    
    # Update password if provided
    if (-not [string]::IsNullOrEmpty($UpdatePassword)) {
        Write-Status "Updating password for user '$UpdateUsername'..."
        
        $passwordUpdate = @{
            type = "password"
            value = $UpdatePassword
            temporary = $false
        } | ConvertTo-Json
        
        try {
            $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/reset-password" `
                -Method Put `
                -Headers $headers `
                -Body $passwordUpdate `
                -UseBasicParsing `
                -ErrorAction Stop
            
            Write-Success "✅ Password updated successfully"
            $updateCount++
        }
        catch {
            Write-Error "❌ Failed to update password: $($_.Exception.Message)"
            return $false
        }
    }
    
    # Update roles if provided
    if ($UpdateRoles -and $UpdateRoles.Count -gt 0) {
        Write-Status "Updating roles for user '$UpdateUsername'..."
        
        # Validate roles
        $validRoles = @("credit-transfer-admin", "credit-transfer-operator", "credit-transfer-user", "system-user")
        foreach ($role in $UpdateRoles) {
            if ($role -notin $validRoles) {
                Write-Error "Invalid role: $role. Valid roles are: $($validRoles -join ', ')"
                return $false
            }
        }
        
        # Get current roles
        try {
            $currentRoles = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/role-mappings/realm" `
                -Method Get `
                -Headers $headers `
                -ErrorAction Stop
            
            # Remove current roles
            if ($currentRoles -and $currentRoles.Count -gt 0) {
                Write-Status "Removing current roles..."
                $currentRolesJson = $currentRoles | ConvertTo-Json -Depth 5
                
                $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/role-mappings/realm" `
                    -Method Delete `
                    -Headers $headers `
                    -Body $currentRolesJson `
                    -UseBasicParsing `
                    -ErrorAction Stop
            }
            
            # Add new roles
            Write-Status "Adding new roles: $($UpdateRoles -join ', ')"
            $roleAssignmentSuccess = $true
            
            foreach ($roleName in $UpdateRoles) {
                if (-not (Add-UserToRole -AdminToken $AdminToken -Username $UpdateUsername -RoleName $roleName)) {
                    Write-Error "Failed to assign role '$roleName'"
                    $roleAssignmentSuccess = $false
                }
            }
            
            if ($roleAssignmentSuccess) {
                Write-Success "✅ Roles updated successfully"
                $updateCount++
            }
            else {
                Write-Error "❌ Some role updates failed"
                return $false
            }
        }
        catch {
            Write-Error "❌ Failed to update roles: $($_.Exception.Message)"
            return $false
        }
    }
    
    if ($updateCount -eq 0) {
        Write-Warning "No updates specified. Use -UpdatePassword and/or -UpdateRoles parameters."
        return $false
    }
    
    Write-Success "✅ User '$UpdateUsername' updated successfully ($updateCount changes)"
    
    # Test authentication if password was updated
    if (-not [string]::IsNullOrEmpty($UpdatePassword)) {
        Write-Status "Testing authentication with new password..."
        if (Test-Authentication -Username $UpdateUsername -Password $UpdatePassword -UseClientSecret $false) {
            Write-Success "✅ Authentication test successful!"
            Write-Host "New Basic Auth Header: $(Get-BasicAuthHeader -Username $UpdateUsername -Password $UpdatePassword)" -ForegroundColor $Colors.Green
        }
        else {
            Write-Warning "⚠️ Authentication test failed - changes may need time to propagate"
        }
    }
    
    return $true
}

function Get-BasicAuthHeader {
    param(
        [string]$Username,
        [string]$Password
    )
    
    $credentials = "$Username`:$Password"
    $encodedCredentials = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($credentials))
    return "Basic $encodedCredentials"
}

function Test-BasicAuthUser {
    param(
        [string]$Username,
        [string]$Password
    )
    
    Write-Status "Testing Basic Auth for user: $Username"
    
    # Test using direct credential validation (simulating our Basic Auth service)
    $success = Test-Authentication -Username $Username -Password $Password -UseClientSecret $false
    
    if ($success) {
        Write-Success "✅ Basic Auth test successful for user: $Username"
        Write-Host "Authorization Header: $(Get-BasicAuthHeader -Username $Username -Password $Password)" -ForegroundColor $Colors.Green
        return $true
    }
    else {
        Write-Error "❌ Basic Auth test failed for user: $Username"
        return $false
    }
}

function Show-BasicAuthConfiguration {
    Write-Host ""
    Write-Host "⚙️ BASIC AUTH CONFIGURATION" -ForegroundColor $Colors.Blue
    Write-Host "============================" -ForegroundColor $Colors.Blue
    Write-Host ""
    Write-Host "Keycloak Configuration:" -ForegroundColor $Colors.White
    Write-Host "  URL: $KeycloakUrl" -ForegroundColor $Colors.White
    Write-Host "  Realm: $RealmName" -ForegroundColor $Colors.White
    Write-Host "  Client ID: $ClientId" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "appsettings.json Configuration:" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host @"
"KeycloakServiceAccount": {
  "BaseUrl": "$KeycloakUrl",
  "Realm": "$RealmName",
  "ClientId": "credittransfer-basic-auth",
  "ClientSecret": "basic-auth-service-secret-2024"
}
"@ -ForegroundColor $Colors.Green
    Write-Host ""
    Write-Host "Basic Auth Example (curl):" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host @"
curl -X POST "http://localhost:5001/CreditTransferService.svc" \
  -H "Authorization: Basic YWRtaW46YWRtaW4xMjM=" \
  -H "Content-Type: text/xml" \
  -d '<soap:Envelope>...</soap:Envelope>'
"@ -ForegroundColor $Colors.Green
    Write-Host ""
}

# Main execution
function Main {
    Write-Host ""
    Write-Host "🚀 Credit Transfer System - Enhanced Keycloak Setup" -ForegroundColor $Colors.Green
    Write-Host "===================================================" -ForegroundColor $Colors.Green
    Write-Host ""
    
    # Check for Basic Auth user management commands first
    if ($AddBasicAuthUser -or $RemoveUser -or $ListUsers -or $UpdateUser) {
        Write-Host "🔐 BASIC AUTH USER MANAGEMENT MODE" -ForegroundColor $Colors.Green
        Write-Host "==================================" -ForegroundColor $Colors.Green
        Write-Host ""
        Write-Status "Keycloak URL: $KeycloakUrl"
        Write-Status "Target Realm: $RealmName"
        Write-Host ""
        
        # Wait for Keycloak and get admin token
        if (-not (Wait-ForKeycloak)) {
            exit 1
        }
        
        $adminToken = Get-AdminToken
        if (-not $adminToken) {
            exit 1
        }
        
        # Execute the requested user management operation
        if ($AddBasicAuthUser) {
            $success = Add-BasicAuthUser -AdminToken $adminToken
            if ($success) {
                Show-BasicAuthConfiguration
            }
            exit $(if ($success) { 0 } else { 1 })
        }
        elseif ($RemoveUser) {
            $success = Remove-BasicAuthUser -AdminToken $adminToken
            exit $(if ($success) { 0 } else { 1 })
        }
        elseif ($ListUsers) {
            $success = Show-AllBasicAuthUsers -AdminToken $adminToken
            Write-Host ""
            Show-BasicAuthConfiguration
            exit $(if ($success) { 0 } else { 1 })
        }
        elseif ($UpdateUser) {
            $success = Update-BasicAuthUser -AdminToken $adminToken
            exit $(if ($success) { 0 } else { 1 })
        }
    }
    
    # Standard setup mode
    Write-Host "🎯 CRITICAL: All users will be created in realm: '$RealmName'" -ForegroundColor $Colors.Yellow
    Write-Host "   This script will NOT create users in the 'master' realm" -ForegroundColor $Colors.Yellow
    Write-Host "   All authentication must be done against the '$RealmName' realm" -ForegroundColor $Colors.Yellow
    Write-Host ""
    
    Write-Status "Starting enhanced Keycloak setup process for realm: $RealmName"
    Write-Host ""
    
    # Wait for Keycloak
    if (-not (Wait-ForKeycloak)) {
        exit 1
    }
    
    # Get admin token
    $adminToken = Get-AdminToken
    if (-not $adminToken) {
        exit 1
    }
    
    # Check if realm exists, create if needed
    if (-not (Test-RealmExists -AdminToken $adminToken)) {
        if (-not (New-Realm -AdminToken $adminToken)) {
            exit 1
        }
        # Wait for realm creation to complete
        Start-Sleep -Seconds 3
    }
    else {
        Write-Success "Realm $RealmName already exists"
    }
    
    # Create client
    Write-Host ""
    if (-not (New-Client -AdminToken $adminToken)) {
        Write-Error "Client creation failed - continuing anyway"
    }
    Start-Sleep -Seconds 2
    
    # Handle public client conversion if requested
    if ($ConvertToPublic -or $PublicClientOnly) {
        Write-Host ""
        Write-Status "🔐 PUBLIC CLIENT CONVERSION REQUESTED"
        Write-Host ""
        
        # Get current client configuration
        $currentClient = Get-ClientInfo -AdminToken $adminToken
        if ($currentClient) {
            # Convert to public client
            if (Convert-ToPublicClient -AdminToken $adminToken -Client $currentClient) {
                Write-Success "✅ Client successfully converted to public configuration"
                
                # Test public client authentication
                Write-Host ""
                Write-Status "Testing public client authentication..."
                if (Test-PublicClientAuth -Username "admin" -Password "admin123") {
                    Write-Success "🎉 Public client authentication test successful!"
                }
                else {
                    Write-Warning "⚠️ Public client authentication test failed - check configuration"
                }
            }
            else {
                Write-Error "❌ Failed to convert client to public configuration"
            }
        }
        else {
            Write-Error "❌ Cannot convert client - client not found"
        }
    }
    
    # Create realm roles
    Write-Host ""
    Write-Status "Creating realm roles..."
    New-RealmRole -AdminToken $adminToken -RoleName "credit-transfer-admin" -Description "Credit Transfer System Administrator"
    New-RealmRole -AdminToken $adminToken -RoleName "credit-transfer-operator" -Description "Credit Transfer Operator"
    New-RealmRole -AdminToken $adminToken -RoleName "credit-transfer-user" -Description "Credit Transfer Basic User"
    New-RealmRole -AdminToken $adminToken -RoleName "system-user" -Description "System/Service Account User"
    
    # Wait for roles to be created
    Start-Sleep -Seconds 3
    
    # Create users with proper names matching Postman collection
    Write-Host ""
    Write-Status "Creating users for Postman collection compatibility..."
    
    # Add diagnostic function to check realm state
    Write-Status "Checking realm state before user creation..."
    $headers = @{
        Authorization = "Bearer $adminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        $existingUsers = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users" -Method Get -Headers $headers
        Write-Status "Current users in realm: $($existingUsers.Count)"
        
        $existingRoles = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/roles" -Method Get -Headers $headers
        Write-Status "Current roles in realm: $($existingRoles.Count)"
        
        $clients = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/clients" -Method Get -Headers $headers
        $ourClient = $clients | Where-Object { $_.clientId -eq $ClientId }
        if ($ourClient) {
            Write-Success "Client $ClientId found with ID: $($ourClient.id)"
        } else {
            Write-Error "Client $ClientId not found in realm!"
        }
    }
    catch {
        Write-Warning "Could not check realm state: $($_.Exception.Message)"
    }
    
    # IMPORTANT: All users will be created in realm: $RealmName
    Write-Host ""
    Write-Host "🎯 TARGET REALM: $RealmName" -ForegroundColor $Colors.Yellow
    Write-Host "All users will be created in the '$RealmName' realm only" -ForegroundColor $Colors.Yellow
    Write-Host ""
    
    # Primary users matching Postman collection
    Write-Status "Creating primary admin user in realm: $RealmName"
    New-UserWithRoles -AdminToken $adminToken -Username "admin" -Password "admin123" -Email "admin@credittransfer.com" -FirstName "Credit Transfer" -LastName "Admin" -Roles @("credit-transfer-admin", "credit-transfer-operator", "credit-transfer-user")
    
    Write-Status "Creating operator user in realm: $RealmName"
    New-UserWithRoles -AdminToken $adminToken -Username "operator" -Password "operator123" -Email "operator@credittransfer.com" -FirstName "Credit Transfer" -LastName "Operator" -Roles @("credit-transfer-operator", "credit-transfer-user")
    
    # Additional users for comprehensive testing
    Write-Status "Creating API user in realm: $RealmName"
    New-UserWithRoles -AdminToken $adminToken -Username "apiuser" -Password "apiuser123" -Email "apiuser@credittransfer.com" -FirstName "API" -LastName "User" -Roles @("credit-transfer-user")
    
    Write-Status "Creating system user in realm: $RealmName"
    New-UserWithRoles -AdminToken $adminToken -Username "ctsystem" -Password "system123" -Email "system@credittransfer.com" -FirstName "Credit Transfer" -LastName "System" -Roles @("system-user", "credit-transfer-user")
    
    # Alternative named users for automated testing
    Write-Status "Creating alternative admin user in realm: $RealmName"
    New-UserWithRoles -AdminToken $adminToken -Username "ctadmin" -Password "admin123" -Email "ctadmin@credittransfer.com" -FirstName "CT" -LastName "Admin" -Roles @("credit-transfer-admin", "credit-transfer-operator", "credit-transfer-user")
    
    Write-Status "Creating alternative operator user in realm: $RealmName"
    New-UserWithRoles -AdminToken $adminToken -Username "ctoperator" -Password "operator123" -Email "ctoperator@credittransfer.com" -FirstName "CT" -LastName "Operator" -Roles @("credit-transfer-operator", "credit-transfer-user")
    
    Write-Host ""
    Write-Status "VERIFICATION: Confirming all users are in realm '$RealmName'"
    Write-Host ""
    
    # List of all users that should be created
    $expectedUsers = @("admin", "operator", "apiuser", "ctsystem", "ctadmin", "ctoperator")
    
    # Verify each user exists in the correct realm
    $verificationResults = @()
    foreach ($username in $expectedUsers) {
        $verified = Confirm-UserInCorrectRealm -AdminToken $adminToken -Username $username
        $verificationResults += @{ User = $username; InCorrectRealm = $verified }
    }
    
    Write-Host ""
    Write-Status "VERIFICATION RESULTS for realm '$RealmName':"
    foreach ($result in $verificationResults) {
        $status = if ($result.InCorrectRealm) { "✅ CONFIRMED" } else { "❌ MISSING" }
        $color = if ($result.InCorrectRealm) { $Colors.Green } else { $Colors.Red }
        Write-Host "• $($result.User) - $status in realm $RealmName" -ForegroundColor $color
    }
    
    $verifiedCount = ($verificationResults | Where-Object { $_.InCorrectRealm }).Count
    $totalExpected = $expectedUsers.Count
    
    Write-Host ""
    if ($verifiedCount -eq $totalExpected) {
        Write-Success "🎉 ALL $totalExpected users confirmed in realm '$RealmName'"
    }
    else {
        Write-Error "⚠️  Only $verifiedCount/$totalExpected users found in realm '$RealmName'"
    }
    
    Write-Host ""
    Write-Status "Testing authentication for all users in realm '$RealmName'..."
    Write-Host ""
    
    # Determine if we should use client secret based on client configuration
    $useClientSecret = $true
    if ($ConvertToPublic -or $PublicClientOnly) {
        $useClientSecret = $false
        Write-Status "Using public client authentication (no client_secret required)"
    }
    else {
        Write-Status "Using confidential client authentication (with client_secret)"
    }
    
    # Test authentication for primary users
    $authResults = @()
    $authResults += @{ User = "admin"; Success = (Test-Authentication -Username "admin" -Password "admin123" -UseClientSecret $useClientSecret) }
    $authResults += @{ User = "operator"; Success = (Test-Authentication -Username "operator" -Password "operator123" -UseClientSecret $useClientSecret) }
    $authResults += @{ User = "apiuser"; Success = (Test-Authentication -Username "apiuser" -Password "apiuser123" -UseClientSecret $useClientSecret) }
    $authResults += @{ User = "ctsystem"; Success = (Test-Authentication -Username "ctsystem" -Password "system123" -UseClientSecret $useClientSecret) }
    $authResults += @{ User = "ctadmin"; Success = (Test-Authentication -Username "ctadmin" -Password "admin123" -UseClientSecret $useClientSecret) }
    $authResults += @{ User = "ctoperator"; Success = (Test-Authentication -Username "ctoperator" -Password "operator123" -UseClientSecret $useClientSecret) }
    
    Write-Host ""
    Write-Success "🎉 Enhanced Keycloak setup completed!"
    Write-Host ""
    Write-Host "Configuration Summary:" -ForegroundColor $Colors.White
    Write-Host "=====================" -ForegroundColor $Colors.White
    Write-Host "• Keycloak URL: $KeycloakUrl" -ForegroundColor $Colors.White
    Write-Host "• Realm: $RealmName" -ForegroundColor $Colors.White
    Write-Host "• Client ID: $ClientId" -ForegroundColor $Colors.White
    Write-Host "• Client Secret: $ClientSecret" -ForegroundColor $Colors.White
    Write-Host "• Admin Console: $KeycloakUrl/admin" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Roles Created:" -ForegroundColor $Colors.White
    Write-Host "• credit-transfer-admin (Full administrative access)" -ForegroundColor $Colors.White
    Write-Host "• credit-transfer-operator (Standard operations)" -ForegroundColor $Colors.White
    Write-Host "• credit-transfer-user (Basic user access)" -ForegroundColor $Colors.White
    Write-Host "• system-user (Service/system account)" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Users Created & Authentication Results:" -ForegroundColor $Colors.White
    foreach ($result in $authResults) {
        $status = if ($result.Success) { "✅ SUCCESS" } else { "❌ FAILED" }
        $color = if ($result.Success) { $Colors.Green } else { $Colors.Red }
        Write-Host "• $($result.User) - $status" -ForegroundColor $color
    }
    Write-Host ""
    Write-Host "Primary Users (Postman Collection Compatible):" -ForegroundColor $Colors.White
    Write-Host "• admin / admin123 (Admin + Operator + User roles)" -ForegroundColor $Colors.White
    Write-Host "• operator / operator123 (Operator + User roles)" -ForegroundColor $Colors.White
    Write-Host "• apiuser / apiuser123 (User role only)" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Additional Users (Automated Testing):" -ForegroundColor $Colors.White
    Write-Host "• ctadmin / admin123 (Admin + Operator + User roles)" -ForegroundColor $Colors.White
    Write-Host "• ctoperator / operator123 (Operator + User roles)" -ForegroundColor $Colors.White
    Write-Host "• ctsystem / system123 (System + User roles)" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Token Endpoint:" -ForegroundColor $Colors.White
    Write-Host "$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "Test Command Examples:" -ForegroundColor $Colors.Yellow
    
    if ($ConvertToPublic -or $PublicClientOnly) {
        Write-Host ""
        Write-Host "🎉 PUBLIC CLIENT COMMANDS (No client_secret required):" -ForegroundColor $Colors.Green
        Write-Host ""
        Write-Host "Admin Token:" -ForegroundColor $Colors.White
        Write-Host "curl --location '$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token' \\" -ForegroundColor $Colors.White
        Write-Host "  --header 'Content-Type: application/x-www-form-urlencoded' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'grant_type=password' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'client_id=$ClientId' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'username=admin' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'password=admin123' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'scope=openid profile email'" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "Operator Token:" -ForegroundColor $Colors.White
        Write-Host "curl --location '$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token' \\" -ForegroundColor $Colors.White
        Write-Host "  --header 'Content-Type: application/x-www-form-urlencoded' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'grant_type=password' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'client_id=$ClientId' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'username=operator' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'password=operator123' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'scope=openid profile email'" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "✅ NOTICE: No client_secret parameter needed!" -ForegroundColor $Colors.Green
    }
    else {
        Write-Host "CONFIDENTIAL CLIENT COMMANDS (with client_secret):" -ForegroundColor $Colors.White
        Write-Host "Admin Token:" -ForegroundColor $Colors.White
        Write-Host "curl --location '$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token' \\" -ForegroundColor $Colors.White
        Write-Host "  --header 'Content-Type: application/x-www-form-urlencoded' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'grant_type=password' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'client_id=$ClientId' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'client_secret=$ClientSecret' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'username=admin' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'password=admin123' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'scope=openid profile email'" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "Operator Token:" -ForegroundColor $Colors.White
        Write-Host "curl --location '$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token' \\" -ForegroundColor $Colors.White
        Write-Host "  --header 'Content-Type: application/x-www-form-urlencoded' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'grant_type=password' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'client_id=$ClientId' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'client_secret=$ClientSecret' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'username=operator' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'password=operator123' \\" -ForegroundColor $Colors.White
        Write-Host "  --data-urlencode 'scope=openid profile email'" -ForegroundColor $Colors.White
        Write-Host ""
        Write-Host "⚠️  IMPORTANT: client_secret parameter is required for confidential clients!" -ForegroundColor $Colors.Red
    }
    Write-Host ""
    
    # Final verification - list all users in realm
    Write-Host ""
    Write-Host "🔍 FINAL VERIFICATION - CREDITTRANSFER REALM ONLY" -ForegroundColor $Colors.Yellow
    Write-Host "================================================" -ForegroundColor $Colors.Yellow
    Write-Status "Listing all users in realm: '$RealmName'"
    try {
        $finalUsers = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users" -Method Get -Headers @{
            Authorization = "Bearer $adminToken"
            "Content-Type" = "application/json"
        }
        
        Write-Host ""
        Write-Host "📋 USERS IN REALM '$RealmName' ONLY:" -ForegroundColor $Colors.White
        Write-Host "Total users found: $($finalUsers.Count)" -ForegroundColor $Colors.White
        Write-Host ""
        
        foreach ($user in $finalUsers) {
            $status = if ($user.enabled) { "✅ Enabled" } else { "❌ Disabled" }
            Write-Host "  • $($user.username) ($($user.email)) - $status" -ForegroundColor $Colors.White
        }
        
        # Verify all expected users are present
        Write-Host ""
        Write-Status "Verifying all expected users are in realm '$RealmName':"
        $expectedUsernames = @("admin", "operator", "apiuser", "ctsystem", "ctadmin", "ctoperator")
        $actualUsernames = $finalUsers | ForEach-Object { $_.username }
        
        $allUsersPresent = $true
        foreach ($expectedUser in $expectedUsernames) {
            if ($actualUsernames -contains $expectedUser) {
                Write-Host "  ✅ $expectedUser - FOUND in realm $RealmName" -ForegroundColor $Colors.Green
            }
            else {
                Write-Host "  ❌ $expectedUser - MISSING from realm $RealmName" -ForegroundColor $Colors.Red
                $allUsersPresent = $false
            }
        }
        
        Write-Host ""
        if ($allUsersPresent) {
            Write-Success "🎉 ALL EXPECTED USERS CONFIRMED IN REALM '$RealmName'"
        }
        else {
            Write-Error "⚠️  SOME USERS MISSING FROM REALM '$RealmName'"
        }
    }
    catch {
        Write-Warning "Could not list final users: $($_.Exception.Message)"
    }
    
    # Final summary
    $successCount = ($authResults | Where-Object { $_.Success }).Count
    $totalCount = $authResults.Count
    
    Write-Host ""
    if ($successCount -eq $totalCount) {
        Write-Success "🚀 All $totalCount users authenticated successfully! Ready for testing!"
    }
    else {
        Write-Warning "⚠️  $successCount/$totalCount users authenticated successfully. Check failed authentications above."
        Write-Host ""
        Write-Host "TROUBLESHOOTING TIPS:" -ForegroundColor $Colors.Yellow
        Write-Host "1. Wait 10-15 seconds after script completion before testing" -ForegroundColor $Colors.White
        Write-Host "2. Verify users exist in Keycloak admin console: $KeycloakUrl/admin" -ForegroundColor $Colors.White
        Write-Host "3. Always include client_secret in authentication requests" -ForegroundColor $Colors.White
        Write-Host "4. Check Keycloak logs for detailed error information" -ForegroundColor $Colors.White
    }
    
    # Show Basic Auth User Management Menu
    Write-Host ""
    Write-Host "🔧 BASIC AUTH USER MANAGEMENT" -ForegroundColor $Colors.Green
    Write-Host "=============================" -ForegroundColor $Colors.Green
    Write-Host ""
    Write-Host "Now that Keycloak is set up, you can manage Basic Auth users with these commands:" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "📝 LIST ALL USERS:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -ListUsers" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "➕ ADD NEW USER:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser -NewUsername 'jane.doe' -NewPassword 'MySecurePass123!' -NewEmail 'jane.doe@company.com' -NewFirstName 'Jane' -NewLastName 'Doe' -NewUserRoles @('credit-transfer-operator')" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "✏️ UPDATE USER:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -UpdateUser -UpdateUsername 'jane.doe' -UpdatePassword 'NewPassword123!' -UpdateRoles @('credit-transfer-admin')" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "🗑️ REMOVE USER:" -ForegroundColor $Colors.Yellow
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -RemoveUser -RemoveUsername 'jane.doe'" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "📋 AVAILABLE ROLES:" -ForegroundColor $Colors.White
    Write-Host "  • credit-transfer-admin     (Full administrative access)" -ForegroundColor $Colors.White
    Write-Host "  • credit-transfer-operator  (Standard operations)" -ForegroundColor $Colors.White
    Write-Host "  • credit-transfer-user      (Basic user access)" -ForegroundColor $Colors.White
    Write-Host "  • system-user               (Service/system account)" -ForegroundColor $Colors.White
    Write-Host ""
    Write-Host "🌐 CONFIGURABLE KEYCLOAK URL:" -ForegroundColor $Colors.Blue
    Write-Host "  Use -KeycloakUrl parameter to specify different Keycloak server:" -ForegroundColor $Colors.White
    Write-Host "  .\Setup-Keycloak-Complete.ps1 -KeycloakUrl 'http://production-keycloak:8080' -ListUsers" -ForegroundColor $Colors.White
    Write-Host ""
}

# Run main function
Main 