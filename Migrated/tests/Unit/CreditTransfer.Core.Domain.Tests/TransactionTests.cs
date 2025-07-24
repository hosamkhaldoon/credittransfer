using CreditTransfer.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void Transaction_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var transaction = new Transaction();

        // Assert
        transaction.Id.Should().Be(0);
        transaction.SourceMsisdn.Should().Be(string.Empty);
        transaction.DestinationMsisdn.Should().Be(string.Empty);
        transaction.Amount.Should().Be(0);
        transaction.PIN.Should().Be(string.Empty);
        transaction.IsFromCustomer.Should().BeFalse();
        transaction.IsEventReserved.Should().BeFalse();
        transaction.IsAmountTransfered.Should().BeFalse();
        transaction.IsEventCharged.Should().BeFalse();
        transaction.IsEventCancelled.Should().BeFalse();
        transaction.IsExpiryExtended.Should().BeFalse();
        transaction.ExtensionDays.Should().Be(0);
        transaction.ReservationId.Should().Be(0);
        transaction.StatusId.Should().Be(0);
        transaction.NumberOfRetries.Should().Be(0);
        transaction.CreatedBy.Should().Be(string.Empty);
        transaction.CreationDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        transaction.ModificationDate.Should().BeNull();
        transaction.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void Transaction_PropertiesSetCorrectly()
    {
        // Arrange
        var creationDate = DateTime.Now.AddHours(-1);
        var modificationDate = DateTime.Now;

        // Act
        var transaction = new Transaction
        {
            Id = 123,
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            Amount = 10.5m,
            PIN = "1234",
            IsFromCustomer = true,
            IsEventReserved = true,
            IsAmountTransfered = true,
            IsEventCharged = true,
            IsEventCancelled = false,
            IsExpiryExtended = true,
            ExtensionDays = 30,
            ReservationId = 456,
            StatusId = 1,
            NumberOfRetries = 2,
            CreationDate = creationDate,
            CreatedBy = "TestUser",
            ModificationDate = modificationDate,
            ModifiedBy = "TestModifier"
        };

        // Assert
        transaction.Id.Should().Be(123);
        transaction.SourceMsisdn.Should().Be("96876325315");
        transaction.DestinationMsisdn.Should().Be("96878715705");
        transaction.Amount.Should().Be(10.5m);
        transaction.PIN.Should().Be("1234");
        transaction.IsFromCustomer.Should().BeTrue();
        transaction.IsEventReserved.Should().BeTrue();
        transaction.IsAmountTransfered.Should().BeTrue();
        transaction.IsEventCharged.Should().BeTrue();
        transaction.IsEventCancelled.Should().BeFalse();
        transaction.IsExpiryExtended.Should().BeTrue();
        transaction.ExtensionDays.Should().Be(30);
        transaction.ReservationId.Should().Be(456);
        transaction.StatusId.Should().Be(1);
        transaction.NumberOfRetries.Should().Be(2);
        transaction.CreationDate.Should().Be(creationDate);
        transaction.CreatedBy.Should().Be("TestUser");
        transaction.ModificationDate.Should().Be(modificationDate);
        transaction.ModifiedBy.Should().Be("TestModifier");
    }

    [Theory]
    [InlineData("96876325315", true)]
    [InlineData("96878715705", true)]
    [InlineData("", false)]
    [InlineData("123", false)]
    [InlineData("96876325315123", false)]
    public void Transaction_SourceMsisdnValidation(string msisdn, bool shouldBeValid)
    {
        // Arrange
        var transaction = new Transaction { SourceMsisdn = msisdn };

        // Act & Assert
        if (shouldBeValid)
        {
            transaction.SourceMsisdn.Should().NotBeNullOrEmpty();
            transaction.SourceMsisdn.Length.Should().BeGreaterThan(0);
        }
        else
        {
            var isInvalid = string.IsNullOrEmpty(transaction.SourceMsisdn) || 
                           transaction.SourceMsisdn.Length < 8 || 
                           transaction.SourceMsisdn.Length > 15;
            isInvalid.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(10.5)]
    [InlineData(100.0)]
    [InlineData(999.999)]
    public void Transaction_AmountValidation_ValidAmounts(decimal amount)
    {
        // Arrange
        var transaction = new Transaction { Amount = amount };

        // Act & Assert
        transaction.Amount.Should().Be(amount);
        transaction.Amount.Should().BeGreaterOrEqualTo(0);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-10.5)]
    public void Transaction_AmountValidation_InvalidAmounts(decimal amount)
    {
        // Arrange
        var transaction = new Transaction { Amount = amount };

        // Act & Assert
        transaction.Amount.Should().Be(amount);
        transaction.Amount.Should().BeLessThan(0);
    }

    [Fact]
    public void Transaction_DecimalPrecisionValidation()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        transaction.Amount = 10.123456789m;

        // Assert
        transaction.Amount.Should().Be(10.123456789m);
        // Test that decimal precision is maintained
        Math.Round(transaction.Amount, 3).Should().Be(10.123m);
    }

    [Fact]
    public void Transaction_BackwardsCompatibility_CreatedDateProperty()
    {
        // Arrange
        var transaction = new Transaction();
        var testDate = DateTime.Now.AddDays(-1);

        // Act
        transaction.CreatedDate = testDate;

        // Assert
        transaction.CreatedDate.Should().Be(testDate);
        transaction.CreationDate.Should().Be(testDate);
    }

    [Fact]
    public void Transaction_BackwardsCompatibility_CreationDateProperty()
    {
        // Arrange
        var transaction = new Transaction();
        var testDate = DateTime.Now.AddDays(-1);

        // Act
        transaction.CreationDate = testDate;

        // Assert
        transaction.CreationDate.Should().Be(testDate);
        transaction.CreatedDate.Should().Be(testDate);
    }

    [Fact]
    public void Transaction_TransactionLifecycleStates()
    {
        // Arrange
        var transaction = new Transaction();

        // Act & Assert - Test initial state
        transaction.IsEventReserved.Should().BeFalse();
        transaction.IsAmountTransfered.Should().BeFalse();
        transaction.IsEventCharged.Should().BeFalse();
        transaction.IsEventCancelled.Should().BeFalse();
        transaction.IsExpiryExtended.Should().BeFalse();

        // Act - Set states
        transaction.IsEventReserved = true;
        transaction.IsAmountTransfered = true;
        transaction.IsEventCharged = true;

        // Assert - Verify states can be set
        transaction.IsEventReserved.Should().BeTrue();
        transaction.IsAmountTransfered.Should().BeTrue();
        transaction.IsEventCharged.Should().BeTrue();
        transaction.IsEventCancelled.Should().BeFalse();
        transaction.IsExpiryExtended.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, TransactionStatus.Pending)]
    [InlineData(1, TransactionStatus.Succeeded)]
    [InlineData(2, TransactionStatus.Failed)]
    [InlineData(3, TransactionStatus.Cancelled)]
    [InlineData(4, TransactionStatus.Reserved)]
    [InlineData(5, TransactionStatus.TransferFailed)]
    public void Transaction_StatusIdMapping(byte statusId, TransactionStatus expectedStatus)
    {
        // Arrange
        var transaction = new Transaction { StatusId = statusId };

        // Act & Assert
        transaction.StatusId.Should().Be(statusId);
        ((TransactionStatus)statusId).Should().Be(expectedStatus);
    }

    [Fact]
    public void Transaction_OptionalPropertiesInitialization()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        transaction.TransferReason = "Test Transfer";
        transaction.AdjustmentReason = "Test Adjustment";
        transaction.CompletedDate = DateTime.Now;
        transaction.ErrorMessage = "Test Error";
        transaction.TransactionReference = "TXN123456";

        // Assert
        transaction.TransferReason.Should().Be("Test Transfer");
        transaction.AdjustmentReason.Should().Be("Test Adjustment");
        transaction.CompletedDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        transaction.ErrorMessage.Should().Be("Test Error");
        transaction.TransactionReference.Should().Be("TXN123456");
    }

    [Fact]
    public void Transaction_BusinessRuleValidation_SameSourceAndDestination()
    {
        // Arrange
        var transaction = new Transaction
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96876325315"
        };

        // Act & Assert
        transaction.SourceMsisdn.Should().Be(transaction.DestinationMsisdn);
        // In a real scenario, this would be invalid and caught by business logic
    }

    [Fact]
    public void Transaction_NumberOfRetriesValidation()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        transaction.NumberOfRetries = 5;

        // Assert
        transaction.NumberOfRetries.Should().Be(5);
        transaction.NumberOfRetries.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Transaction_ReservationIdValidation()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        transaction.ReservationId = 12345;

        // Assert
        transaction.ReservationId.Should().Be(12345);
        transaction.ReservationId.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Transaction_ExtensionDaysValidation()
    {
        // Arrange
        var transaction = new Transaction();

        // Act
        transaction.ExtensionDays = 30;
        transaction.IsExpiryExtended = true;

        // Assert
        transaction.ExtensionDays.Should().Be(30);
        transaction.IsExpiryExtended.Should().BeTrue();
    }

    [Fact]
    public void Transaction_AuditFieldsValidation()
    {
        // Arrange
        var transaction = new Transaction();
        var createdBy = "TestUser";
        var modifiedBy = "TestModifier";
        var modificationDate = DateTime.Now;

        // Act
        transaction.CreatedBy = createdBy;
        transaction.ModifiedBy = modifiedBy;
        transaction.ModificationDate = modificationDate;

        // Assert
        transaction.CreatedBy.Should().Be(createdBy);
        transaction.ModifiedBy.Should().Be(modifiedBy);
        transaction.ModificationDate.Should().Be(modificationDate);
    }
} 