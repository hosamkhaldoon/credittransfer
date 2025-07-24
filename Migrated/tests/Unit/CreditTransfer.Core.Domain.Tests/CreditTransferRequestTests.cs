using CreditTransfer.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

public class CreditTransferRequestTests
{
    [Fact]
    public void CreditTransferRequest_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var request = new CreditTransferRequest();

        // Assert
        request.SourceMsisdn.Should().Be(string.Empty);
        request.DestinationMsisdn.Should().Be(string.Empty);
        request.AmountRiyal.Should().Be(0);
        request.AmountBaisa.Should().Be(0);
        request.Pin.Should().Be(string.Empty);
        request.AdjustmentReason.Should().BeNull();
        request.UserName.Should().Be(string.Empty);
        request.RequestTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.RequestId.Should().NotBeNullOrEmpty();
        request.RequestId.Should().MatchRegex(@"^[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}$");
    }

    [Fact]
    public void CreditTransferRequest_PropertiesSetCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var requestId = Guid.NewGuid().ToString();

        // Act
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = 10,
            AmountBaisa = 500,
            Pin = "1234",
            AdjustmentReason = "Test adjustment",
            UserName = "TestUser",
            RequestTimestamp = timestamp,
            RequestId = requestId
        };

        // Assert
        request.SourceMsisdn.Should().Be("96876325315");
        request.DestinationMsisdn.Should().Be("96878715705");
        request.AmountRiyal.Should().Be(10);
        request.AmountBaisa.Should().Be(500);
        request.Pin.Should().Be("1234");
        request.AdjustmentReason.Should().Be("Test adjustment");
        request.UserName.Should().Be("TestUser");
        request.RequestTimestamp.Should().Be(timestamp);
        request.RequestId.Should().Be(requestId);
    }

    [Theory]
    [InlineData(0, 0, 0.0)]
    [InlineData(1, 0, 1.0)]
    [InlineData(10, 0, 10.0)]
    [InlineData(0, 500, 0.5)]
    [InlineData(0, 1000, 1.0)]
    [InlineData(10, 500, 10.5)]
    [InlineData(5, 250, 5.25)]
    [InlineData(100, 750, 100.75)]
    public void CreditTransferRequest_TotalAmountCalculation(int riyal, int baisa, decimal expectedTotal)
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            AmountRiyal = riyal,
            AmountBaisa = baisa
        };

        // Act
        var totalAmount = request.TotalAmount;

        // Assert
        totalAmount.Should().Be(expectedTotal);
    }

    [Fact]
    public void CreditTransferRequest_TotalAmountCalculation_Precision()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            AmountRiyal = 0,
            AmountBaisa = 1  // 1 baisa = 0.001 OMR
        };

        // Act
        var totalAmount = request.TotalAmount;

        // Assert
        totalAmount.Should().Be(0.001m);
        Math.Round(totalAmount, 3).Should().Be(0.001m);
    }

    [Fact]
    public void CreditTransferRequest_TotalAmountCalculation_MaxBaisa()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            AmountRiyal = 0,
            AmountBaisa = 999  // 999 baisa = 0.999 OMR
        };

        // Act
        var totalAmount = request.TotalAmount;

        // Assert
        totalAmount.Should().Be(0.999m);
    }

    [Fact]
    public void CreditTransferRequest_IsValid_AllFieldsValid()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = 10,
            AmountBaisa = 500,
            Pin = "1234",
            UserName = "TestUser"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "96878715705", 10, 500, "1234", "TestUser", false)]
    [InlineData("96876325315", "", 10, 500, "1234", "TestUser", false)]
    [InlineData("96876325315", "96878715705", 10, 500, "", "TestUser", false)]
    [InlineData("96876325315", "96878715705", 10, 500, "1234", "", false)]
    [InlineData("96876325315", "96878715705", -1, 500, "1234", "TestUser", false)]
    [InlineData("96876325315", "96878715705", 10, -1, "1234", "TestUser", false)]
    [InlineData("96876325315", "96876325315", 10, 500, "1234", "TestUser", false)] // Same source and destination
    public void CreditTransferRequest_IsValid_InvalidScenarios(string source, string destination, int riyal, int baisa, string pin, string userName, bool expectedValid)
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = source,
            DestinationMsisdn = destination,
            AmountRiyal = riyal,
            AmountBaisa = baisa,
            Pin = pin,
            UserName = userName
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Fact]
    public void CreditTransferRequest_IsValid_SameSourceAndDestination()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96876325315", // Same as source
            AmountRiyal = 10,
            AmountBaisa = 500,
            Pin = "1234",
            UserName = "TestUser"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("96876325315", true)]
    [InlineData("96878715705", true)]
    [InlineData("96898455550", true)]
    [InlineData("968", false)]
    [InlineData("", false)]
    [InlineData("123456789012345", false)]
    [InlineData("abcdefghijk", false)]
    public void CreditTransferRequest_MsisdnValidation(string msisdn, bool shouldBeValid)
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = msisdn,
            DestinationMsisdn = "96878715705",
            AmountRiyal = 10,
            AmountBaisa = 0,
            Pin = "1234",
            UserName = "TestUser"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        if (shouldBeValid)
        {
            isValid.Should().BeTrue();
        }
        else
        {
            isValid.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(0, 0, true)]  // 0 amount is valid
    [InlineData(1, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(10, 500, true)]
    [InlineData(999, 999, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(-1, -1, false)]
    public void CreditTransferRequest_AmountValidation(int riyal, int baisa, bool shouldBeValid)
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = riyal,
            AmountBaisa = baisa,
            Pin = "1234",
            UserName = "TestUser"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("1234", true)]
    [InlineData("0000", true)]
    [InlineData("9999", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("12345", true)] // 5 digit PIN
    [InlineData("123", true)]   // 3 digit PIN
    public void CreditTransferRequest_PinValidation(string pin, bool shouldBeValid)
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = 10,
            AmountBaisa = 0,
            Pin = pin,
            UserName = "TestUser"
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("TestUser", true)]
    [InlineData("Admin", true)]
    [InlineData("user@example.com", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void CreditTransferRequest_UserNameValidation(string userName, bool shouldBeValid)
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = 10,
            AmountBaisa = 0,
            Pin = "1234",
            UserName = userName
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().Be(shouldBeValid);
    }

    [Fact]
    public void CreditTransferRequest_AdjustmentReasonOptional()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = 10,
            AmountBaisa = 0,
            Pin = "1234",
            UserName = "TestUser",
            AdjustmentReason = null
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().BeTrue();
        request.AdjustmentReason.Should().BeNull();
    }

    [Fact]
    public void CreditTransferRequest_AdjustmentReasonWithValue()
    {
        // Arrange
        var adjustmentReason = "Customer service adjustment";
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = 10,
            AmountBaisa = 0,
            Pin = "1234",
            UserName = "TestUser",
            AdjustmentReason = adjustmentReason
        };

        // Act
        var isValid = request.IsValid();

        // Assert
        isValid.Should().BeTrue();
        request.AdjustmentReason.Should().Be(adjustmentReason);
    }

    [Fact]
    public void CreditTransferRequest_RequestIdUniqueness()
    {
        // Arrange
        var request1 = new CreditTransferRequest();
        var request2 = new CreditTransferRequest();

        // Act & Assert
        request1.RequestId.Should().NotBe(request2.RequestId);
        request1.RequestId.Should().NotBeNullOrEmpty();
        request2.RequestId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreditTransferRequest_RequestTimestampAutoGeneration()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        
        // Act
        var request = new CreditTransferRequest();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        request.RequestTimestamp.Should().BeAfter(beforeCreation);
        request.RequestTimestamp.Should().BeBefore(afterCreation);
    }

    [Fact]
    public void CreditTransferRequest_RequestTimestampCanBeSet()
    {
        // Arrange
        var customTimestamp = DateTime.UtcNow.AddDays(-1);
        var request = new CreditTransferRequest
        {
            RequestTimestamp = customTimestamp
        };

        // Act & Assert
        request.RequestTimestamp.Should().Be(customTimestamp);
    }

    [Fact]
    public void CreditTransferRequest_BusinessRuleValidation_ZeroAmount()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            SourceMsisdn = "96876325315",
            DestinationMsisdn = "96878715705",
            AmountRiyal = 0,
            AmountBaisa = 0,
            Pin = "1234",
            UserName = "TestUser"
        };

        // Act
        var isValid = request.IsValid();
        var totalAmount = request.TotalAmount;

        // Assert
        isValid.Should().BeTrue();
        totalAmount.Should().Be(0.0m);
    }

    [Fact]
    public void CreditTransferRequest_CurrencyConversionValidation()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            AmountRiyal = 10,
            AmountBaisa = 500
        };

        // Act
        var totalAmount = request.TotalAmount;

        // Assert
        totalAmount.Should().Be(10.5m);
        // Validate conversion: 10 Riyal + 500 Baisa = 10 + (500/1000) = 10.5 OMR
        (request.AmountRiyal + (request.AmountBaisa / 1000m)).Should().Be(totalAmount);
    }

    [Fact]
    public void CreditTransferRequest_MaxBaisaValidation()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            AmountRiyal = 0,
            AmountBaisa = 1000  // 1000 baisa = 1 OMR
        };

        // Act
        var totalAmount = request.TotalAmount;

        // Assert
        totalAmount.Should().Be(1.0m);
    }

    [Fact]
    public void CreditTransferRequest_LargeAmountValidation()
    {
        // Arrange
        var request = new CreditTransferRequest
        {
            AmountRiyal = 999,
            AmountBaisa = 999
        };

        // Act
        var totalAmount = request.TotalAmount;

        // Assert
        totalAmount.Should().Be(999.999m);
    }

    [Fact]
    public void CreditTransferRequest_RequestIdGuidFormat()
    {
        // Arrange
        var request = new CreditTransferRequest();

        // Act & Assert
        Guid.TryParse(request.RequestId, out var parsedGuid).Should().BeTrue();
        parsedGuid.Should().NotBe(Guid.Empty);
    }
} 