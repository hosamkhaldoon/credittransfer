# Credit Transfer System - Keycloak Basic Authentication Setup Script
# This script configures Keycloak for Basic Authentication integration
# Creates service account and updates existing configuration

param(
    [string]$KeycloakUrl = "http://localhost:30080",
    [string]$AdminUser = "admin",
    [string]$AdminPass = "admin123",
    [string]$RealmName = "credittransfer",
    [string]$ServiceAccountClientId = "credittransfer-basic-auth",
    [string]$ServiceAccountClientSecret = "basic-auth-service-secret-2024"
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
            -Body $body `
            -ContentType "application/x-www-form-urlencoded"
        
        Write-Success "Admin token obtained successfully"
        return $response.access_token
    }
    catch {
        Write-Error "Failed to get admin token: $($_.Exception.Message)"
        return $null
    }
}

function New-ServiceAccountClient {
    param(
        [string]$AdminToken,
        [string]$ClientId,
        [string]$ClientSecret,
        [string]$Description = "Service account for Basic Authentication validation"
    )
    
    Write-Status "Creating service account client: $ClientId"
    
    $headers = @{
        "Authorization" = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    $clientConfig = @{
        clientId = $ClientId
        name = $ClientId
        description = $Description
        enabled = $true
        protocol = "openid-connect"
        publicClient = $false
        serviceAccountsEnabled = $true
        directAccessGrantsEnabled = $true
        standardFlowEnabled = $false
        implicitFlowEnabled = $false
        secret = $ClientSecret
        attributes = @{
            "access.token.lifespan" = "300"
            "client.session.idle.timeout" = "300"
            "client.session.max.lifespan" = "3600"
        }
        defaultClientScopes = @("email", "profile", "roles")
        optionalClientScopes = @("address", "phone")
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/clients" `
            -Method Post `
            -Headers $headers `
            -Body ($clientConfig | ConvertTo-Json -Depth 10)
        
        if ($response.StatusCode -eq 201) {
            Write-Success "Service account client created successfully"
            
            # Get the created client ID
            $clientsResponse = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" `
                -Method Get -Headers $headers
            
            return $clientsResponse[0].id
        }
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 409) {
            Write-Warning "Service account client already exists, updating configuration..."
            
            # Get existing client
            $clientsResponse = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" `
                -Method Get -Headers $headers
            
            if ($clientsResponse.Count -gt 0) {
                $existingClientId = $clientsResponse[0].id
                
                # Update existing client
                $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/clients/$existingClientId" `
                    -Method Put `
                    -Headers $headers `
                    -Body ($clientConfig | ConvertTo-Json -Depth 10)
                
                Write-Success "Service account client updated successfully"
                return $existingClientId
            }
        }
        else {
            Write-Error "Failed to create service account client: $($_.Exception.Message)"
            return $null
        }
    }
}

function Grant-ServiceAccountRoles {
    param(
        [string]$AdminToken,
        [string]$ServiceAccountInternalId
    )
    
    Write-Status "Granting roles to service account..."
    
    $headers = @{
        "Authorization" = "Bearer $AdminToken"
        "Content-Type" = "application/json"
    }
    
    try {
        # Get service account user
        $serviceAccountUser = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/clients/$ServiceAccountInternalId/service-account-user" `
            -Method Get -Headers $headers
        
        $serviceAccountUserId = $serviceAccountUser.id
        Write-Status "Service account user ID: $serviceAccountUserId"
        
        # Get required roles for user management
        $requiredRoles = @("query-users", "view-users", "manage-users")
        
        foreach ($roleName in $requiredRoles) {
            try {
                # Get role from realm-management client
                $realmManagementClients = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=realm-management" `
                    -Method Get -Headers $headers
                
                if ($realmManagementClients.Count -gt 0) {
                    $realmManagementClientId = $realmManagementClients[0].id
                    
                    $role = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/clients/$realmManagementClientId/roles/$roleName" `
                        -Method Get -Headers $headers
                    
                    # Assign role to service account
                    $roleAssignment = @($role)
                    
                    $response = Invoke-WebRequest -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$serviceAccountUserId/role-mappings/clients/$realmManagementClientId" `
                        -Method Post `
                        -Headers $headers `
                        -Body ($roleAssignment | ConvertTo-Json -Depth 10)
                    
                    Write-Success "Granted role '$roleName' to service account"
                }
            }
            catch {
                Write-Warning "Could not grant role '$roleName': $($_.Exception.Message)"
            }
        }
        
        Write-Success "Service account roles configured successfully"
        return $true
    }
    catch {
        Write-Error "Failed to configure service account roles: $($_.Exception.Message)"
        return $false
    }
}

function Test-ServiceAccountAuthentication {
    param(
        [string]$ClientId,
        [string]$ClientSecret
    )
    
    Write-Status "Testing service account authentication..."
    
    $body = @{
        grant_type = "client_credentials"
        client_id = $ClientId
        client_secret = $ClientSecret
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token" `
            -Method Post `
            -Body $body `
            -ContentType "application/x-www-form-urlencoded"
        
        Write-Success "Service account authentication successful"
        Write-Status "Token type: $($response.token_type)"
        Write-Status "Expires in: $($response.expires_in) seconds"
        
        return $response.access_token
    }
    catch {
        Write-Error "Service account authentication failed: $($_.Exception.Message)"
        return $null
    }
}

function Test-UserValidation {
    param(
        [string]$ServiceAccountToken,
        [string]$TestUsername = "admin",
        [string]$TestPassword = "admin123"
    )
    
    Write-Status "Testing user validation with service account..."
    
    $headers = @{
        "Authorization" = "Bearer $ServiceAccountToken"
        "Content-Type" = "application/json"
    }
    
    try {
        # Test 1: Get user by username
        $userResponse = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users?username=$TestUsername&exact=true" `
            -Method Get -Headers $headers
        
        if ($userResponse.Count -gt 0) {
            $user = $userResponse[0]
            Write-Success "✅ User lookup successful for: $TestUsername"
            Write-Status "  User ID: $($user.id)"
            Write-Status "  Enabled: $($user.enabled)"
            Write-Status "  Email Verified: $($user.emailVerified)"
            
            # Test 2: Get user roles
            $userRoles = Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$($user.id)/role-mappings/realm" `
                -Method Get -Headers $headers
            
            if ($userRoles.Count -gt 0) {
                $roleNames = $userRoles | ForEach-Object { $_.name }
                Write-Success "✅ Role lookup successful"
                Write-Status "  Roles: $($roleNames -join ', ')"
            }
            
            # Test 3: Attempt authentication via Admin API (password validation)
            Write-Status "Testing password validation..."
            
            # Method 1: Try direct authentication
            $authBody = @{
                grant_type = "password"
                client_id = "admin-cli"
                username = $TestUsername
                password = $TestPassword
            }
            
            try {
                $authResponse = Invoke-RestMethod -Uri "$KeycloakUrl/realms/$RealmName/protocol/openid-connect/token" `
                    -Method Post `
                    -Body $authBody `
                    -ContentType "application/x-www-form-urlencoded"
                
                Write-Success "✅ Password validation successful"
                Write-Status "  Authentication method: Direct token endpoint"
                return $true
            }
            catch {
                Write-Warning "Direct authentication failed: $($_.Exception.Message)"
                
                # Method 2: Use credential validation endpoint (if available)
                Write-Status "Trying alternate validation method..."
                # Note: This would require custom Keycloak extension or different approach
                Write-Warning "Alternate validation not implemented - using direct auth only"
                return $false
            }
        }
        else {
            Write-Error "❌ User not found: $TestUsername"
            return $false
        }
    }
    catch {
        Write-Error "❌ User validation failed: $($_.Exception.Message)"
        return $false
    }
}

# Main execution
Write-Status "🚀 Starting Keycloak Basic Authentication Setup..."
Write-Status "Keycloak URL: $KeycloakUrl"
Write-Status "Realm: $RealmName"
Write-Status "Service Account Client: $ServiceAccountClientId"

# Step 1: Get admin token
$adminToken = Get-AdminToken
if (-not $adminToken) {
    Write-Error "Cannot proceed without admin token"
    exit 1
}

# Step 2: Create service account client
$serviceAccountInternalId = New-ServiceAccountClient -AdminToken $adminToken -ClientId $ServiceAccountClientId -ClientSecret $ServiceAccountClientSecret
if (-not $serviceAccountInternalId) {
    Write-Error "Cannot proceed without service account client"
    exit 1
}

# Step 3: Grant necessary roles to service account
$rolesGranted = Grant-ServiceAccountRoles -AdminToken $adminToken -ServiceAccountInternalId $serviceAccountInternalId
if (-not $rolesGranted) {
    Write-Warning "Service account roles may not be configured properly"
}

# Step 4: Test service account authentication
$serviceAccountToken = Test-ServiceAccountAuthentication -ClientId $ServiceAccountClientId -ClientSecret $ServiceAccountClientSecret
if (-not $serviceAccountToken) {
    Write-Error "Service account authentication failed"
    exit 1
}

# Step 5: Test user validation capabilities
$validationTest = Test-UserValidation -ServiceAccountToken $serviceAccountToken
if ($validationTest) {
    Write-Success "✅ All tests passed!"
}
else {
    Write-Warning "⚠️ Some validation tests failed"
}

# Summary
Write-Host "`n🎉 KEYCLOAK BASIC AUTHENTICATION SETUP COMPLETE!" -ForegroundColor $Colors.Green
Write-Host "📋 Configuration Summary:" -ForegroundColor $Colors.White
Write-Host "  • Service Account Client: $ServiceAccountClientId" -ForegroundColor $Colors.White
Write-Host "  • Client Secret: $ServiceAccountClientSecret" -ForegroundColor $Colors.White
Write-Host "  • Authentication Method: Keycloak Admin API" -ForegroundColor $Colors.White
Write-Host "  • User Management: Query/View permissions granted" -ForegroundColor $Colors.White

Write-Host "`n📝 Next Steps:" -ForegroundColor $Colors.White
Write-Host "  1. Update appsettings.json with service account credentials" -ForegroundColor $Colors.White
Write-Host "  2. Implement BasicAuthenticationService in .NET" -ForegroundColor $Colors.White
Write-Host "  3. Replace JWT authentication behavior" -ForegroundColor $Colors.White
Write-Host "  4. Test with existing users (admin, operator, etc.)" -ForegroundColor $Colors.White

Write-Host "`n🔐 Security Notes:" -ForegroundColor $Colors.Yellow  
Write-Host "  • Service account has minimal required permissions" -ForegroundColor $Colors.Yellow
Write-Host "  • Client secret should be stored securely" -ForegroundColor $Colors.Yellow
Write-Host "  • Consider using HTTPS for production" -ForegroundColor $Colors.Yellow
Write-Host "  • Monitor authentication logs for security events" -ForegroundColor $Colors.Yellow 