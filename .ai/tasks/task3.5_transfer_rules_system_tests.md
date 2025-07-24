---
id: 3.5
title: 'Transfer Rules System Comprehensive Tests'
status: pending
priority: critical
feature: Unit Testing Framework
dependencies: [3.1, 4]
assigned_agent: null
created_at: "2025-07-06T17:40:19Z"
started_at: null
completed_at: null
error_log: null
---

## Description

Implement comprehensive unit tests for the Transfer Rules system that replaces hard-coded business logic with configurable database-driven rules from 02_CreateTransferRulesTable.sql.

## Details

- **Database Schema Validation Tests**:
  - TransferRules table structure and constraints validation
  - sp_EvaluateTransferRule stored procedure testing
  - vw_ActiveTransferRules view functionality validation
  - Index performance and query optimization validation
  - Foreign key constraints and data integrity testing

- **Business Rules Data Validation Tests**:
  - Oman (OM) business rules data integrity verification
  - KSA business rules data integrity verification
  - Wildcard rule data validation (DataAccount -> *)
  - Priority ordering validation (10 for restrictions, 50 for allowed)
  - Error code mapping validation (ErrorCode 33 for restrictions)
  - Rule description and documentation validation

- **Core Business Logic Test Matrix**:
  - **Oman Rules Validation**:
    - Customer -> Pos (DENIED, ErrorCode 33)
    - Customer -> Distributor (DENIED, ErrorCode 33)
    - Customer -> Customer (ALLOWED)
    - Customer -> DataAccount (ALLOWED)
    - HalafoniCustomer -> Pos (DENIED, ErrorCode 33)
    - HalafoniCustomer -> Distributor (DENIED, ErrorCode 33)
    - HalafoniCustomer -> HalafoniCustomer (ALLOWED)
    - Customer -> HalafoniCustomer (DENIED, ErrorCode 33)
    - HalafoniCustomer -> Customer (DENIED, ErrorCode 33)
    - Pos -> HalafoniCustomer (DENIED, ErrorCode 33)
    - DataAccount -> * (DENIED, ErrorCode 33)
    - Pos -> Customer (ALLOWED)
    - Distributor -> Customer (ALLOWED)
    - All other allowed combinations from database

  - **KSA Rules Validation**:
    - VirginPrepaidCustomer -> Pos (DENIED, ErrorCode 33)
    - VirginPrepaidCustomer -> Distributor (DENIED, ErrorCode 33)
    - VirginPostpaidCustomer -> Pos (DENIED, ErrorCode 33)
    - VirginPostpaidCustomer -> Distributor (DENIED, ErrorCode 33)
    - DataAccount -> * (DENIED, ErrorCode 33)
    - Customer -> VirginPrepaidCustomer (ALLOWED)
    - VirginPrepaidCustomer -> VirginPrepaidCustomer (ALLOWED)
    - All allowed combinations from database

- **Rule Evaluation Logic Tests**:
  - Priority-based rule selection (lower priority number wins)
  - Wildcard rule evaluation vs. specific rule precedence
  - Country-specific rule application
  - Default behavior when no rules found (allow by default)
  - Configuration-dependent rule evaluation
  - Rule conflict resolution testing
  - Multiple matching rules handling

- **Integration with CreditTransferService Tests**:
  - EvaluateTransferRuleAsync integration testing
  - Error code mapping in ValidateTransferInputsInternalAsync
  - Country configuration retrieval and application
  - Transfer Rules service dependency injection validation
  - Exception handling when Transfer Rules service fails
  - Performance impact on credit transfer validation
  - OpenTelemetry activity tracking for rule evaluation

- **Caching and Performance Tests**:
  - Redis cache key generation and management
  - Cache expiry policies (43200 minutes / 30 days)
  - Cache hit/miss ratio optimization
  - Cache invalidation on rule updates
  - Concurrent access and cache coherency
  - Database query performance with proper indexing
  - Rule evaluation performance under load

## Test Strategy

- **Coverage Requirements**:
  - **Database Coverage**: 100% for all stored procedures and views
  - **Business Logic Coverage**: 100% for all rule combinations from SQL script
  - **Integration Coverage**: 100% for CreditTransferService integration
  - **Performance Coverage**: 95%+ for caching and query optimization
  - **Error Scenario Coverage**: 100% for all failure modes

- **Test Data Management**:
  - Use actual data from 02_CreateTransferRulesTable.sql for realistic testing
  - Create test scenarios for all 20+ rule combinations
  - Include edge cases like unknown countries and subscription types
  - Test data for both OM and KSA configurations
  - Performance test data with large rule sets

- **Mock Strategy**:
  - Mock Entity Framework DbContext with in-memory database seeded with actual rules
  - Mock IDistributedCache for Redis testing scenarios
  - Mock ActivitySource for OpenTelemetry validation
  - Mock CreditTransferDbContext for repository testing
  - Simulate database failures and timeout scenarios
  - Mock configuration retrieval for country settings

- **Integration Testing Scenarios**:
  - End-to-end rule evaluation from CreditTransferService
  - Database connectivity and query execution
  - Cache performance and fallback behavior
  - Rule modification and cache invalidation
  - Country configuration changes and rule application
  - Subscription type enum mapping validation

- **Performance Benchmarks**:
  - Rule evaluation should complete within 10ms
  - Cache hit rate should exceed 95% in normal operation
  - Database queries should use optimal indexes
  - Concurrent rule evaluations should not degrade performance
  - Memory usage should remain stable under load

- **Success Criteria**:
  - All 20+ business rule combinations tested and validated
  - 100% compatibility with original hard-coded business logic
  - All database schema elements tested (tables, views, procedures)
  - Cache performance meets defined benchmarks
  - Integration with CreditTransferService validated
  - Error handling and fallback behavior tested
  - OpenTelemetry instrumentation validated
  - Test execution time under 30 seconds 