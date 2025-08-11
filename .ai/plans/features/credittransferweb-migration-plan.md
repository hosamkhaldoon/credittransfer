# PRD: CreditTransferWeb Migration - .NET Framework 4.x to .NET 8

## 1. Product overview

### 1.1 Document title and version

- PRD: CreditTransferWeb Migration - .NET Framework 4.x to .NET 8
- Version: 1.0

### 1.2 Product summary

This feature plan outlines the migration of the existing CreditTransferWeb project from .NET Framework 4.x to .NET 8, modernizing the USSD/HTTP handler while preserving the same business logic and XML processing capabilities. The migration will replace WCF service integration with modern REST API calls using Keycloak JWT authentication and implement the service as a modern ASP.NET Core Web API.

## 2. Goals

### 2.1 Business goals

- Modernize the USSD/HTTP handler to .NET 8 for improved performance and maintainability
- Maintain 100% backward compatibility with existing XML request/response format
- Replace legacy WCF integration with modern REST API calls
- Improve security with JWT authentication and modern authorization
- Enable containerization and cloud deployment capabilities
- Reduce maintenance overhead and improve development velocity

### 2.2 User goals

- Seamless USSD credit transfer operations with no service interruption
- Identical XML request/response format for existing integrations
- Improved response times and reliability
- Enhanced security and audit capabilities

### 2.3 Non-goals

- Changing the XML request/response format (preserve exact compatibility)
- Modifying business logic or validation rules
- Replacing the USSD interface itself
- Changing existing USSD workflows or user experience

## 3. User personas

### 3.1 Key user types

- USSD system integrators
- Mobile network operators
- Credit transfer service consumers
- System administrators

### 3.2 Basic persona details

- **USSD Gateway**: External system that sends XML credit transfer requests
- **Mobile Subscribers**: End users initiating credit transfers via USSD
- **System Operators**: Personnel monitoring and maintaining the service

### 3.3 Role-based access

- **USSD Gateway**: HTTP POST access to XML processing endpoint
- **System Monitoring**: Access to health checks and status endpoints
- **Administrators**: Full access to configuration and management endpoints

## 4. Functional requirements

- **XML Request Processing** (Priority: Critical)
  - Process incoming XML requests in exact same format as original
  - Parse umsprot XML structure with exec_req and data elements
  - Extract SourceMsisdn, DestinationMsisdn, AmountRiyal, AmountHalala, PIN
  - Apply country code transformation (add country code prefix to destination)

- **REST API Integration** (Priority: Critical)
  - Replace WCF service calls with REST API calls to CreditTransferController
  - Implement JWT authentication with Keycloak token acquisition
  - Call /api/credittransfer/transfer endpoint with proper authentication
  - Handle API responses and error scenarios gracefully

- **Response Generation** (Priority: Critical)
  - Generate XML responses in identical format to original system
  - Map API response codes to appropriate XML response codes
  - Preserve exact response template structure and formatting

- **Security & Authentication** (Priority: High)
  - Implement JWT token acquisition from Keycloak
  - Secure API communication with Bearer token authentication
  - Maintain audit trail and logging for security compliance

- **Configuration Management** (Priority: Medium)
  - Externalize configuration for REST API endpoints
  - Configure Keycloak authentication parameters
  - Support environment-specific configuration

## 5. User experience

### 5.1 Entry points & first-time user flow

- USSD gateway sends HTTP POST request with XML payload to the web handler
- System processes request identically to original .NET Framework version
- Response returned in same XML format with no observable differences

### 5.2 Core experience

- **Step 1**: Receive HTTP POST with XML umsprot request
  - Parse XML to extract credit transfer parameters
  - Apply same validation and transformation logic as original
- **Step 2**: Authenticate and call REST API
  - Acquire JWT token from Keycloak using configured credentials
  - Call credit transfer REST API with Bearer token authentication
- **Step 3**: Process response and generate XML
  - Map API response to XML response format
  - Return identical XML structure to maintain compatibility

### 5.3 Advanced features & edge cases

- Error handling for API authentication failures
- Network timeout and retry mechanisms
- Invalid XML request handling
- Malformed response scenarios
- Service unavailability fallback

### 5.4 UI/UX highlights

- No UI changes (HTTP handler only)
- Identical XML request/response interface
- Transparent migration with no external impact

## 6. Narrative

External USSD systems continue to send the exact same XML credit transfer requests to the migrated service. The new .NET 8 implementation processes these requests using modern REST API calls instead of legacy WCF services, while maintaining identical XML response formats. Users experience improved performance and reliability without any changes to their integration code.

## 7. Success metrics

### 7.1 User-centric metrics

- 100% XML request/response format compatibility
- Zero integration changes required for existing clients
- Improved response time performance (target: 20% improvement)

### 7.2 Business metrics

- Successful migration with zero downtime
- Reduced maintenance overhead for development team
- Enhanced security posture with modern authentication

### 7.3 Technical metrics

- All existing XML processing test cases pass
- Performance benchmarks meet or exceed original system
- Security audit compliance with modern standards

## 8. Technical considerations

### 8.1 Integration points

- Keycloak authentication server for JWT token acquisition
- CreditTransferController REST API for business logic execution
- External USSD gateway systems for XML request/response
- Configuration management for environment-specific settings

### 8.2 Data storage & privacy

- No direct data storage in the web handler (stateless processing)
- All business data handled by underlying credit transfer service
- Audit logging for security and compliance requirements

### 8.3 Scalability & performance

- Stateless design enables horizontal scaling
- Connection pooling for REST API calls
- JWT token caching to reduce authentication overhead
- Containerization support for cloud deployment

### 8.4 Potential challenges

- XML parsing compatibility across .NET Framework and .NET 8
- HTTP client configuration for REST API integration
- Error mapping between API responses and XML error codes
- JWT token lifecycle management and renewal

## 9. Milestones & sequencing

### 9.1 Project estimate

- Medium: 2-3 weeks

### 9.2 Team size & composition

- Small Team: 1-2 people (1 Senior Developer, 1 QA Engineer)

### 9.3 Suggested phases

- **Phase 1**: Project scaffolding and XML processing migration (1 week)
  - Key deliverables: ASP.NET Core project setup, XML parsing logic
- **Phase 2**: REST API integration and authentication (1 week)
  - Key deliverables: JWT authentication, API client implementation
- **Phase 3**: Testing and deployment preparation (0.5-1 week)
  - Key deliverables: Comprehensive testing, Docker containerization

## 10. User stories

### 10.1 XML Request Processing

- **ID**: US-001
- **Description**: As a USSD gateway, I want to send the same XML credit transfer requests to the migrated service so that I don't need to modify my integration code.
- **Acceptance Criteria**:
  - Service accepts HTTP POST requests with XML payload
  - XML parsing extracts SourceMsisdn, DestinationMsisdn, AmountRiyal, AmountHalala, PIN
  - Country code transformation applied to destination MSISDN
  - Invalid XML requests return appropriate error responses

### 10.2 REST API Integration

- **ID**: US-002
- **Description**: As the web handler service, I want to call the credit transfer REST API with proper authentication so that I can execute business logic securely.
- **Acceptance Criteria**:
  - JWT token acquired from Keycloak using configured credentials
  - REST API called with Bearer token authentication
  - API request contains properly formatted transfer parameters
  - Error responses handled gracefully with appropriate mapping

### 10.3 Response Generation

- **ID**: US-003
- **Description**: As a USSD gateway, I want to receive XML responses in the exact same format as the original system so that my response processing continues to work.
- **Acceptance Criteria**:
  - XML response format identical to original template
  - Response codes mapped correctly from API to XML format
  - Error scenarios generate appropriate XML error responses
  - Response timing comparable to original system

### 10.4 Security and Authentication

- **ID**: US-004
- **Description**: As a system administrator, I want the migrated service to use modern JWT authentication so that security is enhanced while maintaining service functionality.
- **Acceptance Criteria**:
  - Keycloak integration implemented for JWT token acquisition
  - Tokens cached appropriately to reduce authentication overhead
  - API calls include proper Bearer token authentication
  - Audit logging captures security-relevant events

### 10.5 Configuration Management

- **ID**: US-005
- **Description**: As a system operator, I want externalized configuration for the migrated service so that I can deploy it across different environments.
- **Acceptance Criteria**:
  - REST API endpoint URLs configurable via appsettings
  - Keycloak authentication parameters externalized
  - Environment-specific configuration supported
  - Configuration validation at startup

### 10.6 Performance and Reliability

- **ID**: US-006
- **Description**: As a USSD gateway, I want the migrated service to perform at least as well as the original system so that user experience is not degraded.
- **Acceptance Criteria**:
  - Response times meet or exceed original system performance
  - Connection pooling implemented for efficiency
  - Retry mechanisms for transient failures
  - Health check endpoints available for monitoring

### 10.7 Containerization Support

- **ID**: US-007
- **Description**: As a DevOps engineer, I want the migrated service to support containerization so that I can deploy it using modern infrastructure.
- **Acceptance Criteria**:
  - Dockerfile created for containerization
  - Configuration externalized for container deployment
  - Health check endpoints compatible with container orchestration
  - Docker Compose integration with existing service stack 