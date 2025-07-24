using CreditTransfer.Core.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CreditTransfer.Core.Domain.Tests;

public class CreditTransferExceptionTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IConfigurationSection> _mockConfigSection;

    public CreditTransferExceptionTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfigSection = new Mock<IConfigurationSection>();
    }

    #region Base Exception Tests

    [Fact]
    public void CreditTransferException_IsAbstractClass()
    {
        // Act & Assert
        typeof(CreditTransferException).IsAbstract.Should().BeTrue();
        typeof(CreditTransferException).BaseType.Should().Be(typeof(Exception));
    }

    [Fact]
    public void CreditTransferException_HasCorrectProperties()
    {
        // Arrange & Act
        var exceptionType = typeof(CreditTransferException);

        // Assert
        exceptionType.GetProperty("ResponseCode").Should().NotBeNull();
        exceptionType.GetProperty("ResponseMessage").Should().NotBeNull();
        exceptionType.GetProperty("ResponseCode")!.PropertyType.Should().Be(typeof(int));
        exceptionType.GetProperty("ResponseMessage")!.PropertyType.Should().Be(typeof(string));
    }

    #endregion

    #region Phone Number Validation Exceptions

    [Fact]
    public void InvalidSourcePhoneException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new InvalidSourcePhoneException();

        // Assert
        exception.ResponseCode.Should().Be(20);
        exception.ResponseMessage.Should().Be("Invalid Source Phone Number");
        exception.Message.Should().Be("Invalid source phone number");
        exception.Should().BeOfType<InvalidSourcePhoneException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void InvalidSourcePhoneException_WithConfiguration_UsesConfigurationMessage()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<string>("ResponseMessages:20")).Returns("Custom source phone error");

        // Act
        var exception = new InvalidSourcePhoneException(_mockConfiguration.Object);

        // Assert
        exception.ResponseCode.Should().Be(20);
        exception.ResponseMessage.Should().Be("Custom source phone error");
    }

    [Fact]
    public void InvalidDestinationPhoneException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new InvalidDestinationPhoneException();

        // Assert
        exception.ResponseCode.Should().Be(21);
        exception.ResponseMessage.Should().Be("Invalid Destination Phone Number");
        exception.Message.Should().Be("Invalid destination phone number");
        exception.Should().BeOfType<InvalidDestinationPhoneException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void InvalidDestinationPhoneException_WithConfiguration_UsesConfigurationMessage()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<string>("ResponseMessages:21")).Returns("Custom destination phone error");

        // Act
        var exception = new InvalidDestinationPhoneException(_mockConfiguration.Object);

        // Assert
        exception.ResponseCode.Should().Be(21);
        exception.ResponseMessage.Should().Be("Custom destination phone error");
    }

    #endregion

    #region PIN Validation Exceptions

    [Fact]
    public void InvalidPinException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new InvalidPinException();

        // Assert
        exception.ResponseCode.Should().Be(22);
        exception.ResponseMessage.Should().Be("Invalid PIN");
        exception.Message.Should().Be("Invalid PIN");
        exception.Should().BeOfType<InvalidPinException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void PinMismatchException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new PinMismatchException();

        // Assert
        exception.ResponseCode.Should().Be(4);
        exception.ResponseMessage.Should().Be("Invalid credit transfer password");
        exception.Message.Should().Be("PIN mismatch");
        exception.Should().BeOfType<PinMismatchException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void PinMismatchException_WithConfiguration_UsesConfigurationMessage()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<string>("ResponseMessages:4")).Returns("Custom PIN mismatch error");

        // Act
        var exception = new PinMismatchException(_mockConfiguration.Object);

        // Assert
        exception.ResponseCode.Should().Be(4);
        exception.ResponseMessage.Should().Be("Custom PIN mismatch error");
    }

    #endregion

    #region Business Rule Exceptions

    [Fact]
    public void SourceAndDestinationSameException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new SourceAndDestinationSameException();

        // Assert
        exception.ResponseCode.Should().Be(3);
        exception.ResponseMessage.Should().Be("A-party and B-party phone numbers are same");
        exception.Message.Should().Be("Source and destination numbers cannot be the same");
        exception.Should().BeOfType<SourceAndDestinationSameException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void NotAllowedToTransferCreditToTheDestinationAccountException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new NotAllowedToTransferCreditToTheDestinationAccountException();

        // Assert
        exception.ResponseCode.Should().Be(33);
        exception.ResponseMessage.Should().Be("Transfer not allowed to this destination account type");
        exception.Message.Should().Be("Transfer not allowed to this destination account type");
        exception.Should().BeOfType<NotAllowedToTransferCreditToTheDestinationAccountException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void ExceedsMaxPerDayTransactionsException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new ExceedsMaxPerDayTransactionsException();

        // Assert
        exception.ResponseCode.Should().Be(34);
        exception.ResponseMessage.Should().Be("Exceeds maximum number of transactions per day");
        exception.Message.Should().Be("Exceeds maximum number of transactions per day");
        exception.Should().BeOfType<ExceedsMaxPerDayTransactionsException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    #endregion

    #region Amount Validation Exceptions

    [Fact]
    public void TransferAmountBelowMinException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new TransferAmountBelowMinException();

        // Assert
        exception.ResponseCode.Should().Be(5);
        exception.ResponseMessage.Should().Be("Amount requested is less than the minimum transferrable amount by A-party");
        exception.Message.Should().Be("Transfer amount is below minimum required");
        exception.Should().BeOfType<TransferAmountBelowMinException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void TransferAmountAboveMaxException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new TransferAmountAboveMaxException();

        // Assert
        exception.ResponseCode.Should().Be(7);
        exception.ResponseMessage.Should().Be("Amount requested is more than the maximum amount that can be transferred by the A-party");
        exception.Message.Should().Be("Transfer amount exceeds maximum allowed");
        exception.Should().BeOfType<TransferAmountAboveMaxException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void InsuffientCreditException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new InsuffientCreditException();

        // Assert
        exception.ResponseCode.Should().Be(23);
        exception.ResponseMessage.Should().Be("Insufficient Credit");
        exception.Message.Should().Be("Insufficient credit for transfer");
        exception.Should().BeOfType<InsuffientCreditException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void RemainingBalanceException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new RemainingBalanceException();

        // Assert
        exception.ResponseCode.Should().Be(35);
        exception.ResponseMessage.Should().Be("Insufficient remaining balance after transfer");
        exception.Message.Should().Be("Insufficient remaining balance after transfer");
        exception.Should().BeOfType<RemainingBalanceException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void RemainingBalanceShouldBeGreaterThanHalfBalance_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new RemainingBalanceShouldBeGreaterThanHalfBalance();

        // Assert
        exception.ResponseCode.Should().Be(40);
        exception.ResponseMessage.Should().Be("Remaining balance should be greater than half of current balance");
        exception.Message.Should().Be("Remaining balance should be greater than half of current balance");
        exception.Should().BeOfType<RemainingBalanceShouldBeGreaterThanHalfBalance>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    #endregion

    #region System Operation Exceptions

    [Fact]
    public void SourcePhoneNumberNotFoundException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new SourcePhoneNumberNotFoundException();

        // Assert
        exception.ResponseCode.Should().Be(26);
        exception.ResponseMessage.Should().Be("Source phone number not found");
        exception.Message.Should().Be("Source phone number not found");
        exception.Should().BeOfType<SourcePhoneNumberNotFoundException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void DestinationPhoneNumberNotFoundException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new DestinationPhoneNumberNotFoundException();

        // Assert
        exception.ResponseCode.Should().Be(27);
        exception.ResponseMessage.Should().Be("Destination phone number not found");
        exception.Message.Should().Be("Destination phone number not found");
        exception.Should().BeOfType<DestinationPhoneNumberNotFoundException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void SubscriptionNotFoundException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new SubscriptionNotFoundException();

        // Assert
        exception.ResponseCode.Should().Be(24);
        exception.ResponseMessage.Should().Be("Subscription Not Found");
        exception.Message.Should().Be("Subscription not found");
        exception.Should().BeOfType<SubscriptionNotFoundException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void PropertyNotFoundException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new PropertyNotFoundException();

        // Assert
        exception.ResponseCode.Should().Be(30);
        exception.ResponseMessage.Should().Be("Property Not Found");
        exception.Message.Should().Be("Property not found");
        exception.Should().BeOfType<PropertyNotFoundException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void UserNotAllowedToCallThisMethodException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new UserNotAllowedToCallThisMethodException();

        // Assert
        exception.ResponseCode.Should().Be(28);
        exception.ResponseMessage.Should().Be("User is not authorized to call this method");
        exception.Message.Should().Be("User is not authorized to call this method");
        exception.Should().BeOfType<UserNotAllowedToCallThisMethodException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void UnknownSubscriberException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new UnknownSubscriberException();

        // Assert
        exception.ResponseCode.Should().Be(2);
        exception.ResponseMessage.Should().Be("Unknown Subscriber");
        exception.Message.Should().Be("Unknown subscriber");
        exception.Should().BeOfType<UnknownSubscriberException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void ConcurrentUpdateDetectedException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new ConcurrentUpdateDetectedException();

        // Assert
        exception.ResponseCode.Should().Be(25);
        exception.ResponseMessage.Should().Be("ConcurrentUpdateDetectedException");
        exception.Message.Should().Be("ConcurrentUpdateDetectedException");
        exception.Should().BeOfType<ConcurrentUpdateDetectedException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void ExpiredReservationCodeException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new ExpiredReservationCodeException();

        // Assert
        exception.ResponseCode.Should().Be(31);
        exception.ResponseMessage.Should().Be("ExpiredReservationCodeException");
        exception.Message.Should().Be("ExpiredReservationCodeException");
        exception.Should().BeOfType<ExpiredReservationCodeException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void SMSFailureException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new SMSFailureException();

        // Assert
        exception.ResponseCode.Should().Be(37);
        exception.ResponseMessage.Should().Be("SMSFailureException");
        exception.Message.Should().Be("SMSFailureException");
        exception.Should().BeOfType<SMSFailureException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    #endregion

    #region Special Cases

    [Fact]
    public void MiscellaneousErrorException_DefaultConstructor_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new MiscellaneousErrorException();

        // Assert
        exception.ResponseCode.Should().Be(14);
        exception.ResponseMessage.Should().Be("Miscellaneous error");
        exception.Message.Should().Be("Miscellaneous error occurred");
        exception.Should().BeOfType<MiscellaneousErrorException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void MiscellaneousErrorException_CustomMessageConstructor_HasCorrectErrorCodeAndMessage()
    {
        // Arrange
        var customMessage = "Custom miscellaneous error";

        // Act
        var exception = new MiscellaneousErrorException(customMessage);

        // Assert
        exception.ResponseCode.Should().Be(14);
        exception.ResponseMessage.Should().Be("Miscellaneous error");
        exception.Message.Should().Be(customMessage);
        exception.Should().BeOfType<MiscellaneousErrorException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    [Fact]
    public void ConfigurationErrorException_HasCorrectErrorCodeAndMessage()
    {
        // Act
        var exception = new ConfigurationErrorException();

        // Assert
        exception.ResponseCode.Should().Be(-1);
        exception.ResponseMessage.Should().Be("Unknown Subscriber");
        exception.Message.Should().Be("ConfigurationError");
        exception.Should().BeOfType<ConfigurationErrorException>();
        exception.Should().BeAssignableTo<CreditTransferException>();
    }

    #endregion

    #region Configuration Integration Tests

    [Fact]
    public void Exception_WithConfiguration_FallsBackToDirectKeyLookup()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<string>("ResponseMessages:22")).Returns((string)null);
        _mockConfiguration.Setup(x => x.GetValue<string>("22")).Returns("Direct key lookup message");

        // Act
        var exception = new InvalidPinException(_mockConfiguration.Object);

        // Assert
        exception.ResponseCode.Should().Be(22);
        exception.ResponseMessage.Should().Be("Direct key lookup message");
    }

    [Fact]
    public void Exception_WithNullConfiguration_UsesDefaultMessage()
    {
        // Act
        var exception = new InvalidPinException(null);

        // Assert
        exception.ResponseCode.Should().Be(22);
        exception.ResponseMessage.Should().Be("Invalid PIN");
    }

    [Fact]
    public void Exception_WithConfigurationButNoValue_UsesDefaultMessage()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<string>("ResponseMessages:22")).Returns((string)null);
        _mockConfiguration.Setup(x => x.GetValue<string>("22")).Returns((string)null);

        // Act
        var exception = new InvalidPinException(_mockConfiguration.Object);

        // Assert
        exception.ResponseCode.Should().Be(22);
        exception.ResponseMessage.Should().Be("Invalid PIN");
    }

    #endregion

    #region Error Code Uniqueness Tests

    [Fact]
    public void AllExceptions_HaveUniqueErrorCodes_ExceptConfigurationErrorException()
    {
        // Arrange
        var exceptions = new List<CreditTransferException>
        {
            new InvalidSourcePhoneException(),
            new InvalidDestinationPhoneException(),
            new InvalidPinException(),
            new SourceAndDestinationSameException(),
            new SourcePhoneNumberNotFoundException(),
            new DestinationPhoneNumberNotFoundException(),
            new SubscriptionNotFoundException(),
            new PropertyNotFoundException(),
            new NotAllowedToTransferCreditToTheDestinationAccountException(),
            new PinMismatchException(),
            new MiscellaneousErrorException(),
            new TransferAmountAboveMaxException(),
            new TransferAmountBelowMinException(),
            new UserNotAllowedToCallThisMethodException(),
            new RemainingBalanceShouldBeGreaterThanHalfBalance(),
            new ExceedsMaxPerDayTransactionsException(),
            new RemainingBalanceException(),
            new InsuffientCreditException(),
            new UnknownSubscriberException(),
            new ConfigurationErrorException(),
            new ExpiredReservationCodeException(),
            new ConcurrentUpdateDetectedException(),
            new SMSFailureException()
        };

        // Act
        var responseCodes = exceptions.Select(e => e.ResponseCode).ToList();
        var duplicateCodes = responseCodes.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        // Assert
        duplicateCodes.Should().BeEmpty("All exception response codes should be unique");
    }

    [Theory]
    [InlineData(2)]    // UnknownSubscriberException
    [InlineData(3)]    // SourceAndDestinationSameException
    [InlineData(4)]    // PinMismatchException
    [InlineData(5)]    // TransferAmountBelowMinException
    [InlineData(7)]    // TransferAmountAboveMaxException
    [InlineData(14)]   // MiscellaneousErrorException
    [InlineData(20)]   // InvalidSourcePhoneException
    [InlineData(21)]   // InvalidDestinationPhoneException
    [InlineData(22)]   // InvalidPinException
    [InlineData(23)]   // InsuffientCreditException
    [InlineData(24)]   // SubscriptionNotFoundException
    [InlineData(25)]   // ConcurrentUpdateDetectedException
    [InlineData(26)]   // SourcePhoneNumberNotFoundException
    [InlineData(27)]   // DestinationPhoneNumberNotFoundException
    [InlineData(28)]   // UserNotAllowedToCallThisMethodException
    [InlineData(30)]   // PropertyNotFoundException
    [InlineData(31)]   // ExpiredReservationCodeException
    [InlineData(33)]   // NotAllowedToTransferCreditToTheDestinationAccountException
    [InlineData(34)]   // ExceedsMaxPerDayTransactionsException
    [InlineData(35)]   // RemainingBalanceException
    [InlineData(37)]   // SMSFailureException
    [InlineData(40)]   // RemainingBalanceShouldBeGreaterThanHalfBalance
    [InlineData(-1)]   // ConfigurationErrorException
    public void ValidErrorCodes_AreDocumented(int errorCode)
    {
        // This test documents all valid error codes in the system
        // Act & Assert
        errorCode.Should().BeOneOf(2, 3, 4, 5, 7, 14, 20, 21, 22, 23, 24, 25, 26, 27, 28, 30, 31, 33, 34, 35, 37, 40, -1);
    }

    #endregion

    #region Exception Hierarchy Tests

    [Fact]
    public void AllExceptions_InheritFromCreditTransferException()
    {
        // Arrange
        var exceptionTypes = new[]
        {
            typeof(InvalidSourcePhoneException),
            typeof(InvalidDestinationPhoneException),
            typeof(InvalidPinException),
            typeof(SourceAndDestinationSameException),
            typeof(SourcePhoneNumberNotFoundException),
            typeof(DestinationPhoneNumberNotFoundException),
            typeof(SubscriptionNotFoundException),
            typeof(PropertyNotFoundException),
            typeof(NotAllowedToTransferCreditToTheDestinationAccountException),
            typeof(PinMismatchException),
            typeof(MiscellaneousErrorException),
            typeof(TransferAmountAboveMaxException),
            typeof(TransferAmountBelowMinException),
            typeof(UserNotAllowedToCallThisMethodException),
            typeof(RemainingBalanceShouldBeGreaterThanHalfBalance),
            typeof(ExceedsMaxPerDayTransactionsException),
            typeof(RemainingBalanceException),
            typeof(InsuffientCreditException),
            typeof(UnknownSubscriberException),
            typeof(ConfigurationErrorException),
            typeof(ExpiredReservationCodeException),
            typeof(ConcurrentUpdateDetectedException),
            typeof(SMSFailureException)
        };

        // Act & Assert
        foreach (var exceptionType in exceptionTypes)
        {
            exceptionType.Should().BeAssignableTo<CreditTransferException>($"{exceptionType.Name} should inherit from CreditTransferException");
            exceptionType.Should().BeAssignableTo<Exception>($"{exceptionType.Name} should inherit from Exception");
        }
    }

    [Fact]
    public void CreditTransferException_InheritsFromException()
    {
        // Act & Assert
        typeof(CreditTransferException).Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region Exception Message Validation Tests

    [Theory]
    [InlineData(typeof(InvalidSourcePhoneException), "Invalid Source Phone Number")]
    [InlineData(typeof(InvalidDestinationPhoneException), "Invalid Destination Phone Number")]
    [InlineData(typeof(InvalidPinException), "Invalid PIN")]
    [InlineData(typeof(SourceAndDestinationSameException), "A-party and B-party phone numbers are same")]
    [InlineData(typeof(PinMismatchException), "Invalid credit transfer password")]
    [InlineData(typeof(MiscellaneousErrorException), "Miscellaneous error")]
    [InlineData(typeof(TransferAmountAboveMaxException), "Amount requested is more than the maximum amount that can be transferred by the A-party")]
    [InlineData(typeof(TransferAmountBelowMinException), "Amount requested is less than the minimum transferrable amount by A-party")]
    [InlineData(typeof(InsuffientCreditException), "Insufficient Credit")]
    [InlineData(typeof(UnknownSubscriberException), "Unknown Subscriber")]
    [InlineData(typeof(ConfigurationErrorException), "Unknown Subscriber")]
    public void Exceptions_HaveCorrectDefaultResponseMessages(Type exceptionType, string expectedMessage)
    {
        // Act
        var exception = (CreditTransferException)Activator.CreateInstance(exceptionType)!;

        // Assert
        exception.ResponseMessage.Should().Be(expectedMessage);
    }

    #endregion
} 