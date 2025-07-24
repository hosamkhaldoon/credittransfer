using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Domain.Enums;
using CreditTransfer.Infrastructure.Repositories;
using CreditTransfer.Infrastructure.Data;

namespace CreditTransfer.Core.Application.Tests
{
    /// <summary>
    /// Comprehensive unit tests for the Transfer Rules System
    /// Tests database-driven business logic for credit transfers
    /// Validates country-specific rules (OM/KSA), subscription type combinations,
    /// wildcard rules, priority evaluation, and caching mechanisms
    /// </summary>
    public class TransferRulesServiceTests : IDisposable
    {
        private readonly CreditTransferDbContext _context;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly ActivitySource _activitySource;
        private readonly TransferRulesRepository _transferRulesService;

        public TransferRulesServiceTests()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<CreditTransferDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CreditTransferDbContext(options);
            _mockCache = new Mock<IDistributedCache>();
            _activitySource = new ActivitySource("CreditTransfer.Tests");
            _transferRulesService = new TransferRulesRepository(_context, _mockCache.Object, _activitySource);

            // Initialize test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Clear existing data
            _context.TransferRules.RemoveRange(_context.TransferRules);
            _context.SaveChanges();

            // Seed OMAN business rules (from 02_CreateTransferRulesTable.sql)
            var omanRules = new List<TransferRule>
            {
                // NOT ALLOWED RULES (Higher Priority = 10)
                new() { Country = "OM", SourceSubscriptionType = "Customer", DestinationSubscriptionType = "Pos", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Prevent customers from transferring to dealers", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Customer", DestinationSubscriptionType = "Distributor", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Prevent customers from transferring to distributors", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "HalafoniCustomer", DestinationSubscriptionType = "Pos", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Prevent Halafoni customers from transferring to dealers", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "HalafoniCustomer", DestinationSubscriptionType = "Distributor", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Prevent Halafoni customers from transferring to distributors", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Customer", DestinationSubscriptionType = "HalafoniCustomer", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Prevent FRiENDi to Halafoni transfers", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "HalafoniCustomer", DestinationSubscriptionType = "Customer", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Prevent Halafoni to FRiENDi transfers", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "HalafoniCustomer", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Prevent dealers from transferring to Halafoni customers", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "DataAccount", DestinationSubscriptionType = "*", IsAllowed = false, ErrorCode = 33, Priority = 5, Description = "Data accounts cannot initiate transfers", IsActive = true },

                // ALLOWED RULES (Lower Priority = 50)
                new() { Country = "OM", SourceSubscriptionType = "Customer", DestinationSubscriptionType = "Customer", IsAllowed = true, Priority = 50, Description = "Customer to Customer transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Customer", DestinationSubscriptionType = "DataAccount", IsAllowed = true, Priority = 50, Description = "Customer to Data account transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "HalafoniCustomer", DestinationSubscriptionType = "HalafoniCustomer", IsAllowed = true, Priority = 50, Description = "Halafoni to Halafoni transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "HalafoniCustomer", DestinationSubscriptionType = "DataAccount", IsAllowed = true, Priority = 50, Description = "Halafoni to Data account transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "Customer", IsAllowed = true, Priority = 50, Description = "Dealer to Customer transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "Pos", IsAllowed = true, Priority = 50, Description = "Dealer to Dealer transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "Distributor", IsAllowed = true, Priority = 50, Description = "Dealer to Distributor transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "DataAccount", IsAllowed = true, Priority = 50, Description = "Dealer to Data account transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "Customer", IsAllowed = true, Priority = 50, Description = "Distributor to Customer transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "HalafoniCustomer", IsAllowed = true, Priority = 50, Description = "Distributor to Halafoni transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "Pos", IsAllowed = true, Priority = 50, Description = "Distributor to Dealer transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "Distributor", IsAllowed = true, Priority = 50, Description = "Distributor to Distributor transfers allowed", IsActive = true },
                new() { Country = "OM", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "DataAccount", IsAllowed = true, Priority = 50, Description = "Distributor to Data account transfers allowed", IsActive = true }
            };

            // Seed KSA business rules
            var ksaRules = new List<TransferRule>
            {
                // NOT ALLOWED RULES (Higher Priority = 10)
                new() { Country = "KSA", SourceSubscriptionType = "VirginPrepaidCustomer", DestinationSubscriptionType = "Pos", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Virgin prepaid customers cannot transfer to dealers", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "VirginPrepaidCustomer", DestinationSubscriptionType = "Distributor", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Virgin prepaid customers cannot transfer to distributors", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "VirginPostpaidCustomer", DestinationSubscriptionType = "Pos", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Virgin postpaid customers cannot transfer to dealers", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "VirginPostpaidCustomer", DestinationSubscriptionType = "Distributor", IsAllowed = false, ErrorCode = 33, Priority = 10, Description = "Virgin postpaid customers cannot transfer to distributors", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "DataAccount", DestinationSubscriptionType = "*", IsAllowed = false, ErrorCode = 33, Priority = 5, Description = "Data accounts cannot initiate transfers", IsActive = true },

                // ALLOWED RULES (Lower Priority = 50)
                new() { Country = "KSA", SourceSubscriptionType = "Customer", DestinationSubscriptionType = "Customer", IsAllowed = true, Priority = 50, Description = "FRiENDi customer to customer transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "VirginPrepaidCustomer", DestinationSubscriptionType = "VirginPrepaidCustomer", IsAllowed = true, Priority = 50, Description = "Virgin prepaid to prepaid transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "VirginPostpaidCustomer", DestinationSubscriptionType = "VirginPostpaidCustomer", IsAllowed = true, Priority = 50, Description = "Virgin postpaid to postpaid transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "Customer", IsAllowed = true, Priority = 50, Description = "Dealer to FRiENDi customer transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "VirginPrepaidCustomer", IsAllowed = true, Priority = 50, Description = "Dealer to Virgin prepaid transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "Pos", DestinationSubscriptionType = "VirginPostpaidCustomer", IsAllowed = true, Priority = 50, Description = "Dealer to Virgin postpaid transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "Customer", IsAllowed = true, Priority = 50, Description = "Distributor to FRiENDi customer transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "VirginPrepaidCustomer", IsAllowed = true, Priority = 50, Description = "Distributor to Virgin prepaid transfers allowed", IsActive = true },
                new() { Country = "KSA", SourceSubscriptionType = "Distributor", DestinationSubscriptionType = "VirginPostpaidCustomer", IsAllowed = true, Priority = 50, Description = "Distributor to Virgin postpaid transfers allowed", IsActive = true }
            };

            // Add test rule for inactive scenarios
            var inactiveRule = new TransferRule
            {
                Country = "OM",
                SourceSubscriptionType = "TestSource",
                DestinationSubscriptionType = "TestDestination",
                IsAllowed = false,
                ErrorCode = 99,
                Priority = 1,
                Description = "Inactive test rule",
                IsActive = false
            };

            _context.TransferRules.AddRange(omanRules);
            _context.TransferRules.AddRange(ksaRules);
            _context.TransferRules.Add(inactiveRule);
            _context.SaveChanges();
        }

        #region Oman Business Rules Tests

        [Fact]
        public async Task EvaluateTransferRule_Oman_Customer_To_Pos_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.Pos);

            // Assert
            result.isAllowed.Should().BeFalse("Customer to Pos transfers are not allowed in Oman");
            result.errorCode.Should().Be(33, "ErrorCode 33 indicates restriction");
            result.errorMessage.Should().Contain("dealer", "Error message should explain dealer restriction");
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Customer_To_Distributor_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.Distributor);

            // Assert
            result.isAllowed.Should().BeFalse("Customer to Distributor transfers are not allowed in Oman");
            result.errorCode.Should().Be(33);
            result.errorMessage.Should().Contain("distributor");
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Customer_To_Customer_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeTrue("Customer to Customer transfers are allowed in Oman");
            result.errorCode.Should().Be(0);
            result.errorMessage.Should().Contain("allowed");
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Customer_To_DataAccount_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.DataAccount);

            // Assert
            result.isAllowed.Should().BeTrue("Customer to DataAccount transfers are allowed in Oman");
            result.errorCode.Should().Be(0);
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_HalafoniCustomer_To_Pos_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.HalafoniCustomer, SubscriptionType.Pos);

            // Assert
            result.isAllowed.Should().BeFalse("HalafoniCustomer to Pos transfers are not allowed in Oman");
            result.errorCode.Should().Be(33);
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_HalafoniCustomer_To_HalafoniCustomer_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.HalafoniCustomer, SubscriptionType.HalafoniCustomer);

            // Assert
            result.isAllowed.Should().BeTrue("HalafoniCustomer to HalafoniCustomer transfers are allowed in Oman");
            result.errorCode.Should().Be(0);
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Customer_To_HalafoniCustomer_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.HalafoniCustomer);

            // Assert
            result.isAllowed.Should().BeFalse("Customer to HalafoniCustomer transfers are not allowed in Oman");
            result.errorCode.Should().Be(33);
            result.errorMessage.Should().Contain("FRiENDi to Halafoni");
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_HalafoniCustomer_To_Customer_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.HalafoniCustomer, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeFalse("HalafoniCustomer to Customer transfers are not allowed in Oman");
            result.errorCode.Should().Be(33);
            result.errorMessage.Should().Contain("Halafoni to FRiENDi");
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Pos_To_HalafoniCustomer_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Pos, SubscriptionType.HalafoniCustomer);

            // Assert
            result.isAllowed.Should().BeFalse("Pos to HalafoniCustomer transfers are not allowed in Oman");
            result.errorCode.Should().Be(33);
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Pos_To_Customer_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Pos, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeTrue("Pos to Customer transfers are allowed in Oman");
            result.errorCode.Should().Be(0);
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Distributor_To_Customer_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Distributor, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeTrue("Distributor to Customer transfers are allowed in Oman");
            result.errorCode.Should().Be(0);
        }

        [Fact]
        public async Task EvaluateTransferRule_Oman_Distributor_To_HalafoniCustomer_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Distributor, SubscriptionType.HalafoniCustomer);

            // Assert
            result.isAllowed.Should().BeTrue("Distributor to HalafoniCustomer transfers are allowed in Oman");
            result.errorCode.Should().Be(0);
        }

        #endregion

        #region KSA Business Rules Tests

        [Fact]
        public async Task EvaluateTransferRule_KSA_VirginPrepaid_To_Pos_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.VirginPrepaidCustomer, SubscriptionType.Pos);

            // Assert
            result.isAllowed.Should().BeFalse("VirginPrepaidCustomer to Pos transfers are not allowed in KSA");
            result.errorCode.Should().Be(33);
            result.errorMessage.Should().Contain("dealer");
        }

        [Fact]
        public async Task EvaluateTransferRule_KSA_VirginPostpaid_To_Distributor_Should_Be_Denied()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.VirginPostpaidCustomer, SubscriptionType.Distributor);

            // Assert
            result.isAllowed.Should().BeFalse("VirginPostpaidCustomer to Distributor transfers are not allowed in KSA");
            result.errorCode.Should().Be(33);
        }

        [Fact]
        public async Task EvaluateTransferRule_KSA_Customer_To_Customer_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.Customer, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeTrue("Customer to Customer transfers are allowed in KSA");
            result.errorCode.Should().Be(0);
        }

        [Fact]
        public async Task EvaluateTransferRule_KSA_VirginPrepaid_To_VirginPrepaid_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.VirginPrepaidCustomer, SubscriptionType.VirginPrepaidCustomer);

            // Assert
            result.isAllowed.Should().BeTrue("VirginPrepaidCustomer to VirginPrepaidCustomer transfers are allowed in KSA");
            result.errorCode.Should().Be(0);
        }

        [Fact]
        public async Task EvaluateTransferRule_KSA_Pos_To_VirginPrepaid_Should_Be_Allowed()
        {
            // Arrange & Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.Pos, SubscriptionType.VirginPrepaidCustomer);

            // Assert
            result.isAllowed.Should().BeTrue("Pos to VirginPrepaidCustomer transfers are allowed in KSA");
            result.errorCode.Should().Be(0);
        }

        #endregion

        #region Wildcard Rules Tests

        [Fact]
        public async Task EvaluateTransferRule_Oman_DataAccount_To_Any_Should_Be_Denied_Wildcard()
        {
            // Arrange & Act - Test multiple destination types with DataAccount source
            var resultToCustomer = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.DataAccount, SubscriptionType.Customer);
            var resultToPos = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.DataAccount, SubscriptionType.Pos);
            var resultToDistributor = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.DataAccount, SubscriptionType.Distributor);

            // Assert - All should be denied due to wildcard rule
            resultToCustomer.isAllowed.Should().BeFalse("DataAccount wildcard rule should deny all transfers");
            resultToCustomer.errorCode.Should().Be(33);
            resultToCustomer.errorMessage.Should().Contain("Data accounts cannot initiate");

            resultToPos.isAllowed.Should().BeFalse("DataAccount wildcard rule should deny all transfers");
            resultToPos.errorCode.Should().Be(33);

            resultToDistributor.isAllowed.Should().BeFalse("DataAccount wildcard rule should deny all transfers");
            resultToDistributor.errorCode.Should().Be(33);
        }

        [Fact]
        public async Task EvaluateTransferRule_KSA_DataAccount_To_Any_Should_Be_Denied_Wildcard()
        {
            // Arrange & Act
            var resultToCustomer = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.DataAccount, SubscriptionType.Customer);
            var resultToVirginPrepaid = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.DataAccount, SubscriptionType.VirginPrepaidCustomer);

            // Assert
            resultToCustomer.isAllowed.Should().BeFalse("DataAccount wildcard rule should apply in KSA too");
            resultToCustomer.errorCode.Should().Be(33);

            resultToVirginPrepaid.isAllowed.Should().BeFalse("DataAccount wildcard rule should apply in KSA too");
            resultToVirginPrepaid.errorCode.Should().Be(33);
        }

        #endregion

        #region Priority Testing

        [Fact]
        public async Task EvaluateTransferRule_Should_Respect_Priority_Order()
        {
            // Arrange - Add conflicting rules with different priorities to test database
            var highPriorityRule = new TransferRule
            {
                Country = "TEST",
                SourceSubscriptionType = "TestSource",
                DestinationSubscriptionType = "TestDest",
                IsAllowed = false,
                ErrorCode = 99,
                Priority = 1, // Higher priority (lower number)
                Description = "High priority denial",
                IsActive = true
            };

            var lowPriorityRule = new TransferRule
            {
                Country = "TEST",
                SourceSubscriptionType = "TestSource",
                DestinationSubscriptionType = "TestDest",
                IsAllowed = true,
                ErrorCode = 0,
                Priority = 100, // Lower priority (higher number)
                Description = "Low priority allow",
                IsActive = true
            };

            _context.TransferRules.AddRange(highPriorityRule, lowPriorityRule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("TEST", SubscriptionType.Customer, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeFalse("High priority rule should override low priority rule");
            result.errorCode.Should().Be(99);
            result.errorMessage.Should().Contain("High priority denial");
        }

        [Fact]
        public async Task EvaluateTransferRule_Wildcard_Should_Have_Higher_Priority_Than_Specific()
        {
            // This test validates that wildcard rules (DataAccount -> *) are evaluated first
            // and take precedence over specific rules due to their higher priority (lower number)

            // Arrange - DataAccount wildcard rule has priority 5, specific rules have priority 50+
            var specificRule = new TransferRule
            {
                Country = "OM",
                SourceSubscriptionType = "DataAccount",
                DestinationSubscriptionType = "Customer",
                IsAllowed = true, // This would conflict with wildcard
                ErrorCode = 0,
                Priority = 50,
                Description = "Specific allow rule that should be overridden",
                IsActive = true
            };

            _context.TransferRules.Add(specificRule);
            await _context.SaveChangesAsync();

            // Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.DataAccount, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeFalse("Wildcard rule should override specific rule due to higher priority");
            result.errorCode.Should().Be(33);
            result.errorMessage.Should().Contain("Data accounts cannot initiate");
        }

        #endregion

        #region Cross-Country Rules Tests

        [Fact]
        public async Task EvaluateTransferRule_Should_Not_Apply_Oman_Rules_To_KSA()
        {
            // Arrange & Act - Test Oman-specific restriction in KSA context
            var result = await _transferRulesService.EvaluateTransferRuleAsync("KSA", SubscriptionType.Customer, SubscriptionType.HalafoniCustomer);

            // Assert - Should be allowed because Oman restriction doesn't apply to KSA
            result.isAllowed.Should().BeTrue("Oman-specific rules should not apply to KSA");
            result.errorCode.Should().Be(0);
            result.errorMessage.Should().Contain("no specific rule found", "Default behavior when no rules exist");
        }

        [Fact]
        public async Task EvaluateTransferRule_Should_Not_Apply_KSA_Rules_To_Oman()
        {
            // Arrange & Act - Test KSA-specific subscription types in Oman context
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.VirginPrepaidCustomer, SubscriptionType.Customer);

            // Assert - Should be allowed because KSA rules don't apply to Oman
            result.isAllowed.Should().BeTrue("KSA-specific rules should not apply to Oman");
            result.errorCode.Should().Be(0);
            result.errorMessage.Should().Contain("no specific rule found");
        }

        #endregion

        #region Default Behavior Tests

        [Fact]
        public async Task EvaluateTransferRule_Should_Default_Allow_When_No_Rules_Exist()
        {
            // Arrange & Act - Test unknown country with unknown subscription types
            var result = await _transferRulesService.EvaluateTransferRuleAsync("UNKNOWN", SubscriptionType.Customer, SubscriptionType.Customer);

            // Assert
            result.isAllowed.Should().BeTrue("Should default to allow when no rules are found");
            result.errorCode.Should().Be(0);
            result.errorMessage.Should().Contain("no specific rule found");
        }

        [Fact]
        public async Task EvaluateTransferRule_Should_Ignore_Inactive_Rules()
        {
            // Arrange - There's an inactive rule seeded in test data
            // Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.Customer);

            // Assert - Should use active rule, not inactive one
            result.isAllowed.Should().BeTrue("Inactive rules should be ignored");
            result.errorCode.Should().NotBe(99, "Should not use error code from inactive rule");
        }

        #endregion

        #region Repository Methods Tests

        [Fact]
        public async Task GetActiveRulesAsync_Should_Return_Only_Active_Rules_For_Country()
        {
            // Arrange & Act
            var omanRules = await _transferRulesService.GetActiveRulesAsync("OM");
            var ksaRules = await _transferRulesService.GetActiveRulesAsync("KSA");

            // Assert
            omanRules.Should().NotBeEmpty("Oman should have transfer rules");
            omanRules.Should().OnlyContain(r => r.Country == "OM" && r.IsActive, "Should only return active Oman rules");
            omanRules.Should().BeInAscendingOrder(r => r.Priority, "Rules should be ordered by priority");

            ksaRules.Should().NotBeEmpty("KSA should have transfer rules");
            ksaRules.Should().OnlyContain(r => r.Country == "KSA" && r.IsActive, "Should only return active KSA rules");

            // Verify some specific rules exist
            omanRules.Should().Contain(r => r.SourceSubscriptionType == "DataAccount" && r.DestinationSubscriptionType == "*", "Wildcard rule should exist");
            omanRules.Should().Contain(r => r.SourceSubscriptionType == "Customer" && r.DestinationSubscriptionType == "Pos" && !r.IsAllowed, "Customer->Pos restriction should exist");
        }

        [Fact]
        public async Task UpsertRuleAsync_Should_Add_New_Rule()
        {
            // Arrange
            var newRule = new TransferRule
            {
                Country = "UAE",
                SourceSubscriptionType = "Customer",
                DestinationSubscriptionType = "Customer",
                IsAllowed = true,
                ErrorCode = 0,
                Priority = 50,
                Description = "UAE Customer to Customer allowed",
                IsActive = true,
                CreatedBy = "TestUser",
                ModifiedBy = "TestUser"
            };

            // Act
            var result = await _transferRulesService.UpsertRuleAsync(newRule);

            // Assert
            result.Should().BeTrue("Upsert should succeed");
            
            var addedRule = await _context.TransferRules
                .FirstOrDefaultAsync(r => r.Country == "UAE" && r.SourceSubscriptionType == "Customer");
            addedRule.Should().NotBeNull("Rule should be added to database");
            addedRule!.Description.Should().Be("UAE Customer to Customer allowed");
        }

        [Fact]
        public async Task DeactivateRuleAsync_Should_Set_Rule_Inactive()
        {
            // Arrange - Get an existing rule
            var existingRule = await _context.TransferRules
                .FirstAsync(r => r.Country == "OM" && r.IsActive);
            var ruleId = existingRule.Id;

            // Act
            var result = await _transferRulesService.DeactivateRuleAsync(ruleId);

            // Assert
            result.Should().BeTrue("Deactivation should succeed");
            
            var deactivatedRule = await _context.TransferRules.FindAsync(ruleId);
            deactivatedRule.Should().NotBeNull();
            deactivatedRule!.IsActive.Should().BeFalse("Rule should be marked as inactive");
        }

        #endregion

        #region Caching Tests

        [Fact]
        public async Task EvaluateTransferRule_Should_Cache_Results()
        {
            // Arrange
            _mockCache.Setup(x => x.GetStringAsync(It.IsAny<string>(), default))
                .ReturnsAsync((string?)null); // Cache miss first time

            // Act
            var result1 = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.Customer);
            var result2 = await _transferRulesService.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.Customer);

            // Assert
            result1.Should().Be(result2, "Results should be consistent");
            
            // Verify cache operations
            _mockCache.Verify(x => x.GetStringAsync(It.IsAny<string>(), default), Times.AtLeast(2), "Should check cache");
            _mockCache.Verify(x => x.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DistributedCacheEntryOptions>(), default), 
                Times.AtLeast(1), "Should cache the result");
        }

        [Fact]
        public async Task GetActiveRulesAsync_Should_Cache_Country_Rules()
        {
            // Arrange
            _mockCache.Setup(x => x.GetStringAsync(It.IsAny<string>(), default))
                .ReturnsAsync((string?)null);

            // Act
            var rules1 = await _transferRulesService.GetActiveRulesAsync("OM");
            var rules2 = await _transferRulesService.GetActiveRulesAsync("OM");

            // Assert
            rules1.Should().BeEquivalentTo(rules2, "Cached results should be equivalent");
            
            _mockCache.Verify(x => x.GetStringAsync(It.IsRegex(".*Country.*OM.*"), default), Times.AtLeast(2), "Should check country-specific cache");
            _mockCache.Verify(x => x.SetStringAsync(It.IsRegex(".*Country.*OM.*"), It.IsAny<string>(), It.IsAny<DistributedCacheEntryOptions>(), default), 
                Times.AtLeast(1), "Should cache country rules");
        }

        #endregion

        #region Comprehensive Business Logic Matrix Tests

        [Theory]
        [InlineData("OM", "Customer", "Pos", false, 33, "Customer to dealer restriction")]
        [InlineData("OM", "Customer", "Distributor", false, 33, "Customer to distributor restriction")]
        [InlineData("OM", "Customer", "Customer", true, 0, "Customer to customer allowed")]
        [InlineData("OM", "Customer", "DataAccount", true, 0, "Customer to data account allowed")]
        [InlineData("OM", "HalafoniCustomer", "Pos", false, 33, "Halafoni to dealer restriction")]
        [InlineData("OM", "HalafoniCustomer", "HalafoniCustomer", true, 0, "Halafoni to halafoni allowed")]
        [InlineData("OM", "Customer", "HalafoniCustomer", false, 33, "FRiENDi to Halafoni restriction")]
        [InlineData("OM", "HalafoniCustomer", "Customer", false, 33, "Halafoni to FRiENDi restriction")]
        [InlineData("OM", "Pos", "Customer", true, 0, "Dealer to customer allowed")]
        [InlineData("OM", "Distributor", "Customer", true, 0, "Distributor to customer allowed")]
        [InlineData("KSA", "VirginPrepaidCustomer", "Pos", false, 33, "Virgin prepaid to dealer restriction")]
        [InlineData("KSA", "Customer", "Customer", true, 0, "KSA customer to customer allowed")]
        [InlineData("KSA", "VirginPrepaidCustomer", "VirginPrepaidCustomer", true, 0, "Virgin prepaid to prepaid allowed")]
        public async Task EvaluateTransferRule_Comprehensive_Business_Logic_Matrix(
            string country,
            string sourceType,
            string destType, 
            bool expectedAllowed,
            int expectedErrorCode,
            string description)
        {
            // Arrange
            var sourceSubscriptionType = Enum.Parse<SubscriptionType>(sourceType);
            var destSubscriptionType = Enum.Parse<SubscriptionType>(destType);

            // Act
            var result = await _transferRulesService.EvaluateTransferRuleAsync(country, sourceSubscriptionType, destSubscriptionType);

            // Assert
            result.isAllowed.Should().Be(expectedAllowed, $"Business rule validation failed for {description}");
            result.errorCode.Should().Be(expectedErrorCode, $"Error code mismatch for {description}");
            
            if (!expectedAllowed)
            {
                result.errorMessage.Should().NotBeNullOrWhiteSpace($"Error message should be provided for denied transfers: {description}");
            }
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _activitySource?.Dispose();
        }
    }

    /// <summary>
    /// Helper class for caching evaluation results
    /// </summary>
    internal class EvaluationResult
    {
        public bool IsAllowed { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
} 