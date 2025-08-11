# Keycloak Basic Authentication Setup Guide

## 🌐 **UPDATED CONFIGURATION: http://localhost:30080**

All Keycloak configurations have been updated to use the correct IP and port configuration.

### **📋 Configuration Files Updated:**

| File | Location | Old URL | New URL |
|------|----------|---------|---------|
| `Setup-Keycloak-Complete.ps1` | `/deployment/` | `http://localhost:6002` | `http://localhost:30080` |
| `Setup-Keycloak-BasicAuth.ps1` | `/deployment/` | `http://localhost:9080` | `http://localhost:30080` |
| `appsettings.BasicAuth.json` | `/src/Services/WebServices/CreditTransferService/` | `http://keycloak:8080` | `http://localhost:30080` |

---

## 🔧 **BASIC AUTH USER MANAGEMENT CAPABILITIES**

The `Setup-Keycloak-Complete.ps1` script now includes comprehensive Basic Auth user management features:

### **📝 LIST ALL USERS**
```powershell
.\Setup-Keycloak-Complete.ps1 -ListUsers
```

**Example Output:**
```
👥 BASIC AUTH USERS LIST
========================

Found 6 users in realm 'credittransfer':

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
👤 Username: admin
   Email: admin@credittransfer.com
   Name: Credit Transfer Admin
   Status: ✅ Active
   Created: 2024-01-15 10:30:45
   Roles: credit-transfer-admin, credit-transfer-operator, credit-transfer-user
   Basic Auth: Basic YWRtaW46W1BBU1NXT1JEXQTrust
```

### **➕ ADD NEW USER**
```powershell
.\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser \
  -NewUsername "jane.doe" \
  -NewPassword "MySecurePass123!" \
  -NewEmail "jane.doe@company.com" \
  -NewFirstName "Jane" \
  -NewLastName "Doe" \
  -NewUserRoles @("credit-transfer-operator", "credit-transfer-user")
```

**Features:**
- ✅ Automatic email generation if not provided (`username@credittransfer.local`)
- ✅ Automatic name defaults if not provided
- ✅ Role validation against available roles
- ✅ Immediate authentication testing
- ✅ Basic Auth header generation
- ✅ Password strength validation

### **✏️ UPDATE EXISTING USER**
```powershell
# Update password only
.\Setup-Keycloak-Complete.ps1 -UpdateUser \
  -UpdateUsername "jane.doe" \
  -UpdatePassword "NewSecurePass123!"

# Update roles only
.\Setup-Keycloak-Complete.ps1 -UpdateUser \
  -UpdateUsername "jane.doe" \
  -UpdateRoles @("credit-transfer-admin", "credit-transfer-user")

# Update both password and roles
.\Setup-Keycloak-Complete.ps1 -UpdateUser \
  -UpdateUsername "jane.doe" \
  -UpdatePassword "SuperSecure456!" \
  -UpdateRoles @("credit-transfer-admin")
```

### **🗑️ REMOVE USER**
```powershell
.\Setup-Keycloak-Complete.ps1 -RemoveUser -RemoveUsername "jane.doe"
```

**Safety Features:**
- ⚠️ Confirmation prompt requiring "DELETE" to be typed
- ✅ User existence verification before deletion
- ✅ Clear warning about permanent action

---

## 🌐 **CONFIGURABLE KEYCLOAK URL**

All commands support the `-KeycloakUrl` parameter to specify different Keycloak servers:

### **Local Development:**
```powershell
.\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://localhost:30080" -ListUsers
```

### **Docker Environment:**
```powershell
.\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://keycloak:8080" -ListUsers
```

### **Production Environment:**
```powershell
.\Setup-Keycloak-Complete.ps1 -KeycloakUrl "https://keycloak.company.com" -ListUsers
```

### **Custom Port:**
```powershell
.\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://keycloak-dev.local:9080" -AddBasicAuthUser -NewUsername "testuser" -NewPassword "TestPass123!"
```

---

## 📋 **AVAILABLE ROLES**

| Role | Description | Permissions |
|------|-------------|-------------|
| `credit-transfer-admin` | Full administrative access | All operations + user management |
| `credit-transfer-operator` | Standard operations | Transfer operations, reporting |
| `credit-transfer-user` | Basic user access | Basic credit transfer operations |
| `system-user` | Service/system account | Automated system operations |

### **Role Hierarchy:**
```
credit-transfer-admin
├── credit-transfer-operator
│   └── credit-transfer-user
└── system-user
    └── credit-transfer-user
```

---

## 🔐 **BASIC AUTH INTEGRATION**

### **Generated Basic Auth Headers:**
The script automatically generates Base64-encoded Basic Auth headers:

```powershell
# Example for user: admin / password: admin123
Basic Auth Header: Basic YWRtaW46YWRtaW4xMjM=
```

### **Using Basic Auth Headers:**

#### **cURL Example:**
```bash
curl -X POST "http://localhost:5001/CreditTransferService.svc" \
  -H "Authorization: Basic YWRtaW46YWRtaW4xMjM=" \
  -H "Content-Type: text/xml" \
  -d '<soap:Envelope>...</soap:Envelope>'
```

#### **PowerShell Example:**
```powershell
$headers = @{
    "Authorization" = "Basic YWRtaW46YWRtaW4xMjM="
    "Content-Type" = "text/xml"
}

$response = Invoke-RestMethod -Uri "http://localhost:5001/CreditTransferService.svc" `
  -Method Post -Headers $headers -Body $soapEnvelope
```

#### **C# Example:**
```csharp
var client = new HttpClient();
var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:admin123"));
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

var response = await client.PostAsync(serviceUrl, new StringContent(soapEnvelope));
```

---

## ⚙️ **CONFIGURATION FILES**

### **appsettings.BasicAuth.json:**
```json
{
  "KeycloakServiceAccount": {
    "BaseUrl": "http://localhost:30080",
    "Realm": "credittransfer",
    "ClientId": "credittransfer-basic-auth",
    "ClientSecret": "basic-auth-service-secret-2024"
  },
  "BasicAuthentication": {
    "Realm": "CreditTransfer API",
    "EnableCaching": true,
    "CacheExpirationMinutes": 15,
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 30
  }
}
```

### **Docker Compose Environment Variables:**
```yaml
environment:
  - KEYCLOAK_URL=http://localhost:30080
  - KEYCLOAK_REALM=credittransfer
  - BASIC_AUTH_ENABLED=true
```

---

## 🚀 **QUICK START GUIDE**

### **1. Setup Keycloak with Default Users:**
```powershell
.\Setup-Keycloak-Complete.ps1
```

### **2. List All Users:**
```powershell
.\Setup-Keycloak-Complete.ps1 -ListUsers
```

### **3. Add New User:**
```powershell
.\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser \
  -NewUsername "myuser" \
  -NewPassword "MyPassword123!" \
  -NewUserRoles @("credit-transfer-operator")
```

### **4. Test Authentication:**
The script automatically tests authentication and provides the Basic Auth header for immediate use.

---

## 🔍 **TROUBLESHOOTING**

### **Common Issues:**

#### **Keycloak Not Accessible:**
```powershell
# Check if Keycloak is running on the correct port
.\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://localhost:30080" -ListUsers
```

#### **User Creation Fails:**
- Ensure Keycloak is fully started (wait 2-3 minutes after container start)
- Check if realm exists
- Verify admin credentials

#### **Authentication Test Fails:**
- Wait 10-15 seconds after user creation for propagation
- Check user is enabled in Keycloak admin console
- Verify realm configuration

#### **Wrong Keycloak URL:**
- Use `-KeycloakUrl` parameter to specify correct URL
- Check Docker container port mappings
- Verify network connectivity

### **Debug Commands:**
```powershell
# Test connectivity
Invoke-WebRequest -Uri "http://localhost:30080" -UseBasicParsing

# List users with detailed output
.\Setup-Keycloak-Complete.ps1 -ListUsers -Verbose

# Check Keycloak admin console
# Open: http://localhost:30080/admin
# Login: admin / admin123
```

---

## 📖 **REFERENCE**

### **Script Parameters:**
| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `-KeycloakUrl` | String | Keycloak server URL | `"http://localhost:30080"` |
| `-AddBasicAuthUser` | Switch | Add new user mode | `-AddBasicAuthUser` |
| `-NewUsername` | String | New user's username | `"john.doe"` |
| `-NewPassword` | String | New user's password | `"SecurePass123!"` |
| `-NewUserRoles` | String[] | Roles for new user | `@("credit-transfer-operator")` |
| `-RemoveUser` | Switch | Remove user mode | `-RemoveUser` |
| `-RemoveUsername` | String | Username to remove | `"john.doe"` |
| `-ListUsers` | Switch | List all users mode | `-ListUsers` |
| `-UpdateUser` | Switch | Update user mode | `-UpdateUser` |
| `-UpdateUsername` | String | Username to update | `"john.doe"` |
| `-UpdatePassword` | String | New password | `"NewPass123!"` |
| `-UpdateRoles` | String[] | New roles | `@("credit-transfer-admin")` |

### **Example Use Cases:**

#### **Development Environment:**
```powershell
# Setup development environment
.\Setup-Keycloak-Complete.ps1 -KeycloakUrl "http://localhost:30080"

# Add developer user
.\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser \
  -NewUsername "developer" \
  -NewPassword "DevPass123!" \
  -NewUserRoles @("credit-transfer-operator")
```

#### **Testing Environment:**
```powershell
# Add test users with different roles
.\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser \
  -NewUsername "testadmin" \
  -NewPassword "TestAdmin123!" \
  -NewUserRoles @("credit-transfer-admin")

.\Setup-Keycloak-Complete.ps1 -AddBasicAuthUser \
  -NewUsername "testuser" \
  -NewPassword "TestUser123!" \
  -NewUserRoles @("credit-transfer-user")
```

#### **Production Management:**
```powershell
# List production users
.\Setup-Keycloak-Complete.ps1 \
  -KeycloakUrl "https://keycloak.production.com" \
  -ListUsers

# Update user roles
.\Setup-Keycloak-Complete.ps1 \
  -KeycloakUrl "https://keycloak.production.com" \
  -UpdateUser \
  -UpdateUsername "operator1" \
  -UpdateRoles @("credit-transfer-admin")
```

---

## ✅ **VALIDATION CHECKLIST**

- [ ] Keycloak accessible at `http://localhost:30080`
- [ ] Realm `credittransfer` exists
- [ ] Default users created (admin, operator, etc.)
- [ ] Basic Auth user management commands work
- [ ] Authentication tests pass
- [ ] Basic Auth headers generated correctly
- [ ] appsettings.BasicAuth.json configured
- [ ] WCF service configured for Basic Auth
- [ ] All configuration files use consistent URLs

---

**🎉 Your Keycloak Basic Authentication system is now fully configured and ready for production use!** 