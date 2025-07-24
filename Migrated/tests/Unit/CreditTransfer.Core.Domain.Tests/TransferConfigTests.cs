using CreditTransfer.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

public class TransferConfigTests
{
    [Fact]
    public void TransferConfig_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new TransferConfig();

        // Assert
        config.ID.Should().Be(0);
        config.NobillSubscritpionType.Should().Be(string.Empty);
        config.TransferCounterName.Should().BeNull();
        config.CreditTransferCustomerService.Should().Be(string.Empty);
        config.DailyTransferCountLimit.Should().BeNull();
        config.DailyTransferCapLimit.Should().BeNull();
        config.MinTransferAmount.Should().BeNull();
        config.TransferFeesEventId.Should().BeNull();
        config.MinPostTransferBalance.Should().BeNull();
        config.MaxTransferAmount.Should().BeNull();
        config.SubscriptionType.Should().BeNull();
        config.TransferToOtherOperator.Should().BeNull();
    }

    [Fact]
    public void TransferConfig_PropertiesSetCorrectly()
    {
        // Arrange & Act
        var config = new TransferConfig
        {
            ID = 123,
            NobillSubscritpionType = "PREPAID",
            TransferCounterName = "DailyTransferCounter",
            CreditTransferCustomerService = "CreditTransferService",
            DailyTransferCountLimit = 10,
            DailyTransferCapLimit = 100.0m,
            MinTransferAmount = 1.0m,
            TransferFeesEventId = 456,
            MinPostTransferBalance = 5.0m,
            MaxTransferAmount = 50.0m,
            SubscriptionType = "Customer",
            TransferToOtherOperator = true
        };

        // Assert
        config.ID.Should().Be(123);
        config.NobillSubscritpionType.Should().Be("PREPAID");
        config.TransferCounterName.Should().Be("DailyTransferCounter");
        config.CreditTransferCustomerService.Should().Be("CreditTransferService");
        config.DailyTransferCountLimit.Should().Be(10);
        config.DailyTransferCapLimit.Should().Be(100.0m);
        config.MinTransferAmount.Should().Be(1.0m);
        config.TransferFeesEventId.Should().Be(456);
        config.MinPostTransferBalance.Should().Be(5.0m);
        config.MaxTransferAmount.Should().Be(50.0m);
        config.SubscriptionType.Should().Be("Customer");
        config.TransferToOtherOperator.Should().BeTrue();
    }

    [Theory]
    [InlineData("PREPAID")]
    [InlineData("POSTPAID")]
    [InlineData("HYBRID")]
    [InlineData("DEALER")]
    [InlineData("STAFF")]
    public void TransferConfig_ValidNobillSubscriptionTypes(string subscriptionType)
    {
        // Arrange
        var config = new TransferConfig { NobillSubscritpionType = subscriptionType };

        // Act & Assert
        config.NobillSubscritpionType.Should().Be(subscriptionType);
        config.NobillSubscritpionType.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void TransferConfig_ValidDailyTransferCountLimits(int limit)
    {
        // Arrange
        var config = new TransferConfig { DailyTransferCountLimit = limit };

        // Act & Assert
        config.DailyTransferCountLimit.Should().Be(limit);
        config.DailyTransferCountLimit.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    [InlineData(500.0)]
    [InlineData(1000.0)]
    public void TransferConfig_ValidDailyTransferCapLimits(decimal limit)
    {
        // Arrange
        var config = new TransferConfig { DailyTransferCapLimit = limit };

        // Act & Assert
        config.DailyTransferCapLimit.Should().Be(limit);
        config.DailyTransferCapLimit.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(5.0)]
    public void TransferConfig_ValidMinTransferAmounts(decimal amount)
    {
        // Arrange
        var config = new TransferConfig { MinTransferAmount = amount };

        // Act & Assert
        config.MinTransferAmount.Should().Be(amount);
        config.MinTransferAmount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(10.0)]
    [InlineData(50.0)]
    [InlineData(100.0)]
    [InlineData(500.0)]
    public void TransferConfig_ValidMaxTransferAmounts(decimal amount)
    {
        // Arrange
        var config = new TransferConfig { MaxTransferAmount = amount };

        // Act & Assert
        config.MaxTransferAmount.Should().Be(amount);
        config.MaxTransferAmount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    [InlineData(10.0)]
    public void TransferConfig_ValidMinPostTransferBalances(decimal balance)
    {
        // Arrange
        var config = new TransferConfig { MinPostTransferBalance = balance };

        // Act & Assert
        config.MinPostTransferBalance.Should().Be(balance);
        config.MinPostTransferBalance.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void TransferConfig_MinMaxTransferAmountValidation()
    {
        // Arrange
        var config = new TransferConfig 
        { 
            MinTransferAmount = 1.0m, 
            MaxTransferAmount = 50.0m 
        };

        // Act & Assert
        config.MinTransferAmount.Should().BeLessThan(config.MaxTransferAmount.Value);
        config.MaxTransferAmount.Should().BeGreaterThan(config.MinTransferAmount.Value);
    }

    [Fact]
    public void TransferConfig_TransferFeesEventIdValidation()
    {
        // Arrange
        var config = new TransferConfig { TransferFeesEventId = 12345 };

        // Act & Assert
        config.TransferFeesEventId.Should().Be(12345);
        config.TransferFeesEventId.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Dealer")]
    [InlineData("Staff")]
    [InlineData("VIP")]
    public void TransferConfig_ValidSubscriptionTypes(string subscriptionType)
    {
        // Arrange
        var config = new TransferConfig { SubscriptionType = subscriptionType };

        // Act & Assert
        config.SubscriptionType.Should().Be(subscriptionType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TransferConfig_TransferToOtherOperatorValidation(bool canTransfer)
    {
        // Arrange
        var config = new TransferConfig { TransferToOtherOperator = canTransfer };

        // Act & Assert
        config.TransferToOtherOperator.Should().Be(canTransfer);
    }

    [Fact]
    public void TransferConfig_CreditTransferCustomerServiceValidation()
    {
        // Arrange
        var serviceName = "CreditTransferService";
        var config = new TransferConfig { CreditTransferCustomerService = serviceName };

        // Act & Assert
        config.CreditTransferCustomerService.Should().Be(serviceName);
        config.CreditTransferCustomerService.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TransferConfig_TransferCounterNameValidation()
    {
        // Arrange
        var counterName = "DailyTransferCounter";
        var config = new TransferConfig { TransferCounterName = counterName };

        // Act & Assert
        config.TransferCounterName.Should().Be(counterName);
        config.TransferCounterName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TransferConfig_BusinessRuleValidation_MinBalanceVsMinTransfer()
    {
        // Arrange
        var config = new TransferConfig 
        { 
            MinTransferAmount = 5.0m, 
            MinPostTransferBalance = 10.0m 
        };

        // Act & Assert
        config.MinTransferAmount.Should().Be(5.0m);
        config.MinPostTransferBalance.Should().Be(10.0m);
        // Business rule: User must have at least (MinTransferAmount + MinPostTransferBalance) before transfer
        var minimumRequiredBalance = config.MinTransferAmount.Value + config.MinPostTransferBalance.Value;
        minimumRequiredBalance.Should().Be(15.0m);
    }

    [Fact]
    public void TransferConfig_BusinessRuleValidation_DailyLimits()
    {
        // Arrange
        var config = new TransferConfig 
        { 
            DailyTransferCountLimit = 10, 
            DailyTransferCapLimit = 100.0m,
            MaxTransferAmount = 50.0m
        };

        // Act & Assert
        config.DailyTransferCountLimit.Should().Be(10);
        config.DailyTransferCapLimit.Should().Be(100.0m);
        config.MaxTransferAmount.Should().Be(50.0m);
        
        // Business rule: With max transfer amount, user can't exceed daily cap in 2 transfers
        var maxPossibleDailyTransfer = config.MaxTransferAmount.Value * config.DailyTransferCountLimit.Value;
        maxPossibleDailyTransfer.Should().Be(500.0m);
        maxPossibleDailyTransfer.Should().BeGreaterThan(config.DailyTransferCapLimit.Value);
    }

    [Fact]
    public void TransferConfig_NullableFieldsValidation()
    {
        // Arrange
        var config = new TransferConfig 
        { 
            ID = 1, 
            NobillSubscritpionType = "PREPAID",
            CreditTransferCustomerService = "TestService"
        };

        // Act & Assert
        config.TransferCounterName.Should().BeNull();
        config.DailyTransferCountLimit.Should().BeNull();
        config.DailyTransferCapLimit.Should().BeNull();
        config.MinTransferAmount.Should().BeNull();
        config.TransferFeesEventId.Should().BeNull();
        config.MinPostTransferBalance.Should().BeNull();
        config.MaxTransferAmount.Should().BeNull();
        config.SubscriptionType.Should().BeNull();
        config.TransferToOtherOperator.Should().BeNull();
    }

    [Fact]
    public void TransferConfig_DecimalPrecisionValidation()
    {
        // Arrange
        var config = new TransferConfig 
        { 
            MinTransferAmount = 1.123m,
            MaxTransferAmount = 50.456m,
            DailyTransferCapLimit = 100.789m,
            MinPostTransferBalance = 5.012m
        };

        // Act & Assert
        config.MinTransferAmount.Should().Be(1.123m);
        config.MaxTransferAmount.Should().Be(50.456m);
        config.DailyTransferCapLimit.Should().Be(100.789m);
        config.MinPostTransferBalance.Should().Be(5.012m);
    }

    [Fact]
    public void TransferConfig_SubscriptionTypeMapping()
    {
        // Arrange
        var config = new TransferConfig 
        { 
            NobillSubscritpionType = "PREPAID",
            SubscriptionType = "Customer"
        };

        // Act & Assert
        config.NobillSubscritpionType.Should().Be("PREPAID");
        config.SubscriptionType.Should().Be("Customer");
        // Business rule: Mapping between NobillSubscritpionType and SubscriptionType
        config.NobillSubscritpionType.Should().NotBe(config.SubscriptionType);
    }

    [Fact]
    public void TransferConfig_TransferRestrictionsValidation()
    {
        // Arrange
        var restrictiveConfig = new TransferConfig 
        { 
            NobillSubscritpionType = "DEALER",
            TransferToOtherOperator = false,
            DailyTransferCountLimit = 0,
            DailyTransferCapLimit = 0,
            MinTransferAmount = 0,
            MaxTransferAmount = 0
        };

        // Act & Assert
        restrictiveConfig.TransferToOtherOperator.Should().BeFalse();
        restrictiveConfig.DailyTransferCountLimit.Should().Be(0);
        restrictiveConfig.DailyTransferCapLimit.Should().Be(0);
        restrictiveConfig.MinTransferAmount.Should().Be(0);
        restrictiveConfig.MaxTransferAmount.Should().Be(0);
        // This configuration would effectively disable transfers
    }

    [Fact]
    public void TransferConfig_PermissiveTransferValidation()
    {
        // Arrange
        var permissiveConfig = new TransferConfig 
        { 
            NobillSubscritpionType = "STAFF",
            TransferToOtherOperator = true,
            DailyTransferCountLimit = 1000,
            DailyTransferCapLimit = 10000.0m,
            MinTransferAmount = 0.1m,
            MaxTransferAmount = 1000.0m,
            MinPostTransferBalance = 0.0m
        };

        // Act & Assert
        permissiveConfig.TransferToOtherOperator.Should().BeTrue();
        permissiveConfig.DailyTransferCountLimit.Should().Be(1000);
        permissiveConfig.DailyTransferCapLimit.Should().Be(10000.0m);
        permissiveConfig.MinTransferAmount.Should().Be(0.1m);
        permissiveConfig.MaxTransferAmount.Should().Be(1000.0m);
        permissiveConfig.MinPostTransferBalance.Should().Be(0.0m);
        // This configuration allows very flexible transfers
    }

    [Fact]
    public void TransferConfig_IDValidation()
    {
        // Arrange
        var config = new TransferConfig { ID = 12345 };

        // Act & Assert
        config.ID.Should().Be(12345);
        config.ID.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TransferConfig_BusinessRuleEnforcement()
    {
        // Arrange
        var config = new TransferConfig 
        { 
            NobillSubscritpionType = "PREPAID",
            SubscriptionType = "Customer",
            MinTransferAmount = 1.0m,
            MaxTransferAmount = 50.0m,
            MinPostTransferBalance = 2.0m,
            DailyTransferCountLimit = 10,
            DailyTransferCapLimit = 200.0m,
            TransferToOtherOperator = true
        };

        // Act & Assert - Validate business rules
        config.MinTransferAmount.Should().BeLessThan(config.MaxTransferAmount.Value);
        config.MinPostTransferBalance.Should().BeGreaterOrEqualTo(0);
        config.DailyTransferCountLimit.Should().BeGreaterThan(0);
        config.DailyTransferCapLimit.Should().BeGreaterThan(0);
        
        // Business rule: Daily cap should be achievable with max transfer amount
        var minTransfersNeeded = config.DailyTransferCapLimit.Value / config.MaxTransferAmount.Value;
        minTransfersNeeded.Should().BeLessOrEqualTo(config.DailyTransferCountLimit.Value);
    }
} 