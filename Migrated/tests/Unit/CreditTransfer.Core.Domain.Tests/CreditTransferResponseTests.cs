using CreditTransfer.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

public class CreditTransferResponseTests
{
    [Fact]
    public void CreditTransferResponse_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var response = new CreditTransferResponse();

        // Assert
        response.StatusCode.Should().Be(0);
        response.StatusMessage.Should().Be(string.Empty);
        response.RequestId.Should().Be(string.Empty);
        response.ResponseTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        response.IsSuccess.Should().BeTrue(); // StatusCode 0 means success
        response.TransactionId.Should().BeNull();
        response.ProcessedAmount.Should().BeNull();
    }

    [Fact]
    public void CreditTransferResponse_PropertiesSetCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var requestId = Guid.NewGuid().ToString();
        var transactionId = Guid.NewGuid().ToString();

        // Act
        var response = new CreditTransferResponse
        {
            StatusCode = 1,
            StatusMessage = "Test Error",
            RequestId = requestId,
            ResponseTimestamp = timestamp,
            TransactionId = transactionId,
            ProcessedAmount = 10.5m
        };

        // Assert
        response.StatusCode.Should().Be(1);
        response.StatusMessage.Should().Be("Test Error");
        response.RequestId.Should().Be(requestId);
        response.ResponseTimestamp.Should().Be(timestamp);
        response.TransactionId.Should().Be(transactionId);
        response.ProcessedAmount.Should().Be(10.5m);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(22, false)]
    [InlineData(23, false)]
    [InlineData(404, false)]
    [InlineData(-1, false)]
    public void CreditTransferResponse_IsSuccess_StatusCodeValidation(int statusCode, bool expectedSuccess)
    {
        // Arrange
        var response = new CreditTransferResponse { StatusCode = statusCode };

        // Act
        var isSuccess = response.IsSuccess;

        // Assert
        isSuccess.Should().Be(expectedSuccess);
    }

    [Fact]
    public void CreditTransferResponse_Success_FactoryMethod()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var transactionId = Guid.NewGuid().ToString();
        var amount = 25.75m;

        // Act
        var response = CreditTransferResponse.Success(requestId, transactionId, amount);

        // Assert
        response.StatusCode.Should().Be(0);
        response.StatusMessage.Should().Be("Success");
        response.RequestId.Should().Be(requestId);
        response.TransactionId.Should().Be(transactionId);
        response.ProcessedAmount.Should().Be(amount);
        response.IsSuccess.Should().BeTrue();
        response.ResponseTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreditTransferResponse_Success_FactoryMethod_MinimalParameters()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Success(requestId);

        // Assert
        response.StatusCode.Should().Be(0);
        response.StatusMessage.Should().Be("Success");
        response.RequestId.Should().Be(requestId);
        response.TransactionId.Should().BeNull();
        response.ProcessedAmount.Should().BeNull();
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CreditTransferResponse_Success_FactoryMethod_WithTransactionId()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var transactionId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Success(requestId, transactionId);

        // Assert
        response.StatusCode.Should().Be(0);
        response.StatusMessage.Should().Be("Success");
        response.RequestId.Should().Be(requestId);
        response.TransactionId.Should().Be(transactionId);
        response.ProcessedAmount.Should().BeNull();
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CreditTransferResponse_Error_FactoryMethod()
    {
        // Arrange
        var statusCode = 22;
        var statusMessage = "Invalid PIN";
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Error(statusCode, statusMessage, requestId);

        // Assert
        response.StatusCode.Should().Be(statusCode);
        response.StatusMessage.Should().Be(statusMessage);
        response.RequestId.Should().Be(requestId);
        response.IsSuccess.Should().BeFalse();
        response.TransactionId.Should().BeNull();
        response.ProcessedAmount.Should().BeNull();
        response.ResponseTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(1, "Transfer failed")]
    [InlineData(2, "Unknown subscriber")]
    [InlineData(22, "Invalid PIN")]
    [InlineData(23, "Insufficient credit")]
    [InlineData(26, "Source phone number not found")]
    [InlineData(27, "Destination phone number not found")]
    [InlineData(28, "User not allowed to call this method")]
    [InlineData(29, "Configuration error")]
    [InlineData(30, "Property not found")]
    public void CreditTransferResponse_Error_FactoryMethod_CommonErrorCodes(int statusCode, string statusMessage)
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Error(statusCode, statusMessage, requestId);

        // Assert
        response.StatusCode.Should().Be(statusCode);
        response.StatusMessage.Should().Be(statusMessage);
        response.RequestId.Should().Be(requestId);
        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreditTransferResponse_ResponseTimestamp_AutoGeneration()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        
        // Act
        var response = new CreditTransferResponse();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        response.ResponseTimestamp.Should().BeAfter(beforeCreation);
        response.ResponseTimestamp.Should().BeBefore(afterCreation);
    }

    [Fact]
    public void CreditTransferResponse_ResponseTimestamp_CanBeSet()
    {
        // Arrange
        var customTimestamp = DateTime.UtcNow.AddDays(-1);
        var response = new CreditTransferResponse
        {
            ResponseTimestamp = customTimestamp
        };

        // Act & Assert
        response.ResponseTimestamp.Should().Be(customTimestamp);
    }

    [Fact]
    public void CreditTransferResponse_TransactionId_OptionalField()
    {
        // Arrange
        var response = new CreditTransferResponse();

        // Act & Assert
        response.TransactionId.Should().BeNull();
        
        // Set transaction ID
        response.TransactionId = "TXN123456";
        response.TransactionId.Should().Be("TXN123456");
    }

    [Fact]
    public void CreditTransferResponse_ProcessedAmount_OptionalField()
    {
        // Arrange
        var response = new CreditTransferResponse();

        // Act & Assert
        response.ProcessedAmount.Should().BeNull();
        
        // Set processed amount
        response.ProcessedAmount = 15.25m;
        response.ProcessedAmount.Should().Be(15.25m);
    }

    [Fact]
    public void CreditTransferResponse_ProcessedAmount_DecimalPrecision()
    {
        // Arrange
        var response = new CreditTransferResponse
        {
            ProcessedAmount = 10.123456789m
        };

        // Act & Assert
        response.ProcessedAmount.Should().Be(10.123456789m);
        Math.Round(response.ProcessedAmount.Value, 3).Should().Be(10.123m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Test message")]
    [InlineData("Very long error message that describes the issue in detail")]
    [InlineData("مرحبا بكم في خدمة تحويل الرصيد")] // Arabic text
    public void CreditTransferResponse_StatusMessage_VariousMessages(string statusMessage)
    {
        // Arrange
        var response = new CreditTransferResponse { StatusMessage = statusMessage };

        // Act & Assert
        response.StatusMessage.Should().Be(statusMessage);
    }

    [Fact]
    public void CreditTransferResponse_RequestId_Validation()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var response = new CreditTransferResponse { RequestId = requestId };

        // Act & Assert
        response.RequestId.Should().Be(requestId);
        response.RequestId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreditTransferResponse_RequestId_GuidFormat()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var response = new CreditTransferResponse { RequestId = requestId };

        // Act & Assert
        Guid.TryParse(response.RequestId, out var parsedGuid).Should().BeTrue();
        parsedGuid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreditTransferResponse_BusinessRuleValidation_SuccessWithAmount()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var amount = 50.0m;

        // Act
        var response = CreditTransferResponse.Success(requestId, amount: amount);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.ProcessedAmount.Should().Be(amount);
        response.ProcessedAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreditTransferResponse_BusinessRuleValidation_SuccessWithoutAmount()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Success(requestId);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.ProcessedAmount.Should().BeNull();
    }

    [Fact]
    public void CreditTransferResponse_BusinessRuleValidation_ErrorWithoutAmount()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Error(23, "Insufficient credit", requestId);

        // Assert
        response.IsSuccess.Should().BeFalse();
        response.ProcessedAmount.Should().BeNull();
        response.TransactionId.Should().BeNull();
    }

    [Fact]
    public void CreditTransferResponse_StatusCode_NegativeValues()
    {
        // Arrange
        var response = new CreditTransferResponse { StatusCode = -1 };

        // Act & Assert
        response.StatusCode.Should().Be(-1);
        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreditTransferResponse_StatusCode_LargeValues()
    {
        // Arrange
        var response = new CreditTransferResponse { StatusCode = 999 };

        // Act & Assert
        response.StatusCode.Should().Be(999);
        response.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void CreditTransferResponse_FactoryMethods_CreateDifferentInstances()
    {
        // Arrange
        var requestId1 = Guid.NewGuid().ToString();
        var requestId2 = Guid.NewGuid().ToString();

        // Act
        var successResponse = CreditTransferResponse.Success(requestId1);
        var errorResponse = CreditTransferResponse.Error(22, "Invalid PIN", requestId2);

        // Assert
        successResponse.Should().NotBe(errorResponse);
        successResponse.IsSuccess.Should().BeTrue();
        errorResponse.IsSuccess.Should().BeFalse();
        successResponse.RequestId.Should().NotBe(errorResponse.RequestId);
    }

    [Fact]
    public void CreditTransferResponse_ResponseStructure_CompleteValidation()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var transactionId = Guid.NewGuid().ToString();
        var amount = 25.5m;
        var timestamp = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var response = new CreditTransferResponse
        {
            StatusCode = 0,
            StatusMessage = "Transfer completed successfully",
            RequestId = requestId,
            ResponseTimestamp = timestamp,
            TransactionId = transactionId,
            ProcessedAmount = amount
        };

        // Assert
        response.StatusCode.Should().Be(0);
        response.StatusMessage.Should().Be("Transfer completed successfully");
        response.RequestId.Should().Be(requestId);
        response.ResponseTimestamp.Should().Be(timestamp);
        response.TransactionId.Should().Be(transactionId);
        response.ProcessedAmount.Should().Be(amount);
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CreditTransferResponse_ErrorHandling_DataIntegrity()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var errorMessage = "Database connection failed";

        // Act
        var response = CreditTransferResponse.Error(14, errorMessage, requestId);

        // Assert
        response.StatusCode.Should().Be(14);
        response.StatusMessage.Should().Be(errorMessage);
        response.RequestId.Should().Be(requestId);
        response.IsSuccess.Should().BeFalse();
        response.TransactionId.Should().BeNull();
        response.ProcessedAmount.Should().BeNull();
    }

    [Fact]
    public void CreditTransferResponse_TimestampValidation_Consistency()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        
        // Act
        var response1 = new CreditTransferResponse();
        var response2 = new CreditTransferResponse();
        
        // Assert
        response1.ResponseTimestamp.Should().BeOnOrAfter(beforeCreation);
        response2.ResponseTimestamp.Should().BeOnOrAfter(response1.ResponseTimestamp);
    }

    [Fact]
    public void CreditTransferResponse_ZeroAmount_Validation()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Success(requestId, amount: 0.0m);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.ProcessedAmount.Should().Be(0.0m);
    }

    [Fact]
    public void CreditTransferResponse_NegativeAmount_Validation()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();

        // Act
        var response = CreditTransferResponse.Success(requestId, amount: -10.0m);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.ProcessedAmount.Should().Be(-10.0m);
        // Note: Business logic should prevent negative amounts, but entity allows it
    }
} 