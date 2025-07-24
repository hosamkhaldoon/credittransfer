# Credit Transfer API Testing with Postman

This directory contains Postman collections and environments for comprehensive testing of the Credit Transfer system migration from .NET Framework 4.0 to .NET 8.

## Files Overview

- **`CreditTransfer-API-Collection.json`** - Main Postman collection with all API endpoints
- **`CreditTransfer-Environment.json`** - Environment variables for local development
- **`README.md`** - This documentation file

## Quick Start

### 1. Import into Postman

1. Open Postman
2. Click **Import** button
3. Select **Upload Files**
4. Import both files:
   - `CreditTransfer-API-Collection.json`
   - `CreditTransfer-Environment.json`

### 2. Set Environment

1. In Postman, select the **Credit Transfer Environment** from the environment dropdown
2. Verify the environment variables are set correctly:
   - `keycloak_url`: http://localhost:8080
   - `rest_api_url`: http://localhost:5002
   - `wcf_service_url`: http://localhost:5001

### 3. Start Services

Before testing, ensure all services are running:

```bash
# Navigate to the Migrated directory
cd Migrated

# Start all services with Docker Compose
docker-compose up -d

# Wait for services to be ready (about 2-3 minutes)
# Check service health
curl http://localhost:5002/health
curl http://localhost:8080/realms/credittransfer/.well-known/openid_configuration
```

## Collection Structure

### 1. Authentication
- **Get Keycloak Token (Admin)** - Obtains admin JWT token
- **Get Keycloak Token (Operator)** - Obtains operator JWT token

### 2. REST API Endpoints
- **Health Check** - Verify API service is running
- **Get Denominations** - Retrieve available transfer denominations
- **Transfer Credit** - Standard credit transfer with PIN
- **Transfer Credit with Adjustment Reason** - Transfer with adjustment reason
- **Transfer Credit Without PIN** - Service center transfer without PIN
- **Validate Transfer Inputs** - Validate transfer parameters

### 3. WCF Service Endpoints
- **WCF - Get Denominations** - SOAP equivalent of REST denominations
- **WCF - Transfer Credit** - SOAP equivalent of REST transfer
- **WCF - Transfer Credit with Adjustment Reason** - SOAP transfer with adjustment
- **WCF - Transfer Credit Without PIN** - SOAP transfer without PIN
- **WCF - Validate Transfer Inputs** - SOAP validation

### 4. Error Scenarios
- **REST - Invalid Source Phone** - Test invalid phone number validation
- **REST - Same Source and Destination** - Test same number validation
- **REST - Unauthorized Access** - Test authentication requirement

## Testing Workflow

### Step 1: Authentication
1. Run **"Get Keycloak Token (Admin)"** first
   - This automatically sets the `access_token` environment variable
   - The token is used for all subsequent authenticated requests

### Step 2: Basic API Testing
1. Run **"Health Check"** to verify REST API is responding
2. Run **"Get Denominations"** to test authenticated endpoint

### Step 3: Business Logic Testing
1. **Transfer Credit** - Test standard transfer flow
2. **Transfer Credit with Adjustment Reason** - Test adjustment flow
3. **Transfer Credit Without PIN** - Test service center flow
4. **Validate Transfer Inputs** - Test validation logic

### Step 4: WCF Compatibility Testing
1. Run all WCF endpoints to verify SOAP compatibility
2. Compare responses with REST API responses

### Step 5: Error Handling Testing
1. Run all error scenario tests
2. Verify proper error codes and messages

## Environment Variables

### Service URLs
- `keycloak_url` - Keycloak authentication server URL
- `rest_api_url` - REST API service URL
- `wcf_service_url` - WCF service URL

### Authentication
- `access_token` - JWT token (auto-populated)
- `refresh_token` - Refresh token (auto-populated)
- `operator_token` - Operator JWT token (auto-populated)
- `admin_username` / `admin_password` - Admin credentials
- `operator_username` / `operator_password` - Operator credentials
- `client_id` - Keycloak client ID
- `realm` - Keycloak realm name

### Test Data
- `test_source_msisdn` - Source phone number for testing
- `test_destination_msisdn` - Destination phone number for testing

## Production Environment Setup

For production testing, create a new environment with production URLs:

```json
{
  "keycloak_url": "https://auth.yourcompany.com",
  "rest_api_url": "https://api.yourcompany.com",
  "wcf_service_url": "https://wcf.yourcompany.com"
}
```

## Expected Response Formats

### REST API Success Response
```json
{
  "statusCode": 0,
  "statusMessage": "Success",
  "transactionId": "TXN123456789"
}
```

### WCF Service Success Response
```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <TransferCreditResponse xmlns="http://tempuri.org/">
      <statusCode>0</statusCode>
      <statusMessage>Success</statusMessage>
    </TransferCreditResponse>
  </soap:Body>
</soap:Envelope>
```

### Error Response
```json
{
  "statusCode": 20,
  "statusMessage": "Invalid source phone number format"
}
```

## Status Codes Reference

| Code | Description |
|------|-------------|
| 0    | Success |
| 3    | Miscellaneous Error |
| 7    | Subscription Not Found |
| 20   | Invalid Phone Number Format |
| 22   | Invalid PIN |
| 30   | Property Not Found |

## Automated Testing

The collection includes test scripts that automatically:
- Extract and store JWT tokens
- Validate response status codes
- Check response structure
- Verify business logic responses

## Troubleshooting

### Common Issues

1. **401 Unauthorized**
   - Run authentication request first
   - Check if token has expired (tokens expire after 1 hour)

2. **Connection Refused**
   - Verify services are running: `docker-compose ps`
   - Check service logs: `docker-compose logs [service-name]`

3. **Invalid Token**
   - Re-run authentication request
   - Check Keycloak is properly configured

4. **SOAP Parsing Errors**
   - Verify Content-Type header is set to `text/xml; charset=utf-8`
   - Check SOAPAction header matches the operation

### Service Health Checks

```bash
# Check REST API
curl http://localhost:5002/health

# Check WCF Service WSDL
curl http://localhost:5001/CreditTransferService.svc?wsdl

# Check Keycloak
curl http://localhost:8080/realms/credittransfer/.well-known/openid_configuration
```

## Migration Validation

This collection validates that the migrated .NET 8 system maintains:
- ✅ **API Compatibility** - Same endpoints and parameters
- ✅ **Response Format** - Identical response structures
- ✅ **Business Logic** - Same validation rules and error codes
- ✅ **Authentication** - Secure JWT-based authentication
- ✅ **Error Handling** - Consistent error responses

## Support

For issues with the API testing:
1. Check service logs: `docker-compose logs`
2. Verify environment variables are set correctly
3. Ensure all services are running and healthy
4. Check network connectivity between services 