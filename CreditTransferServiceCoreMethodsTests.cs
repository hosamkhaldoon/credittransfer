using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Application.Services;
using CreditTransfer.Core.Domain.Constants;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Core.Domain.Enums;
using FluentAssertions;
using IntegrationProxies.Nobill;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using NobillCalls;

namespace CreditTransfer.Core.Application.Tests;

/// <summary>
/// Comprehensive unit tests for CreditTransferService core methods
/// Tests all 5 main public methods with extensive validation scenarios
/// </summary>
public class CreditTransferServiceCoreMethodsTests : IDisposable
{
    private readonly Mock<INobillCallsService> _mockNobillCallsService;
    private readonly Mock<ISubscriptionRepository> _mockSubscriptionRepository;
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<ITransferConfigRepository> _mockTransferConfigRepository;
    private readonly Mock<IErrorConfigurationService> _mockErrorConfigService;
    private readonly Mock<IApplicationConfigRepository> _mockConfigRepository;
    private readonly Mock<ITransferRulesService> _mockTransferRulesService;
    private readonly Mock<ILogger<CreditTransferService>> _mockLogger;
    private readonly ActivitySource _activitySource;
    private readonly CreditTransferService _creditTransferService;

    public CreditTransferServiceCoreMethodsTests()
    {
        // Initialize all mock dependencies
        _mockNobillCallsService = new Mock<INobillCallsService>();
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockTransferConfigRepository = new Mock<ITransferConfigRepository>();
        _mockErrorConfigService = new Mock<IErrorConfigurationService>();
        _mockConfigRepository = new Mock<IApplicationConfigRepository>();
        _mockTransferRulesService = new Mock<ITransferRulesService>();
        _mockLogger = new Mock<ILogger<CreditTransferService>>();

        // Setup Activity Source for OpenTelemetry
        _activitySource = new ActivitySource("CreditTransfer.Core.Business");

        // Setup default mocks
        SetupDefaultConfiguration();
        SetupDefaultErrorMessages();

        // Create the service under test
        _creditTransferService = new CreditTransferService(
            _mockNobillCallsService.Object,
            _mockSubscriptionRepository.Object,
            _mockTransactionRepository.Object,
            _mockTransferConfigRepository.Object,
            _mockErrorConfigService.Object,
            _mockConfigRepository.Object,
            _mockTransferRulesService.Object,
            _mockLogger.Object
        );
    }

    private void SetupDefaultConfiguration()
    {
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", It.IsAny<List<int>>()))
            .ReturnsAsync(new List<int> { 8, 9, 10, 11, 12 });
            
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_MinRiyal", It.IsAny<int>()))
            .ReturnsAsync(0);
            
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_MaxRiyal", It.IsAny<int>()))
            .ReturnsAsync(1000);
            
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_MinBaisa", It.IsAny<int>()))
            .ReturnsAsync(0);
            
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_MaxBaisa", It.IsAny<int>()))
            .ReturnsAsync(1000);
            
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_PinLength", It.IsAny<int>()))
            .ReturnsAsync(4);
            
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", It.IsAny<string>()))
            .ReturnsAsync("1234");
            
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_VirginEventIds", It.IsAny<string>()))
            .ReturnsAsync("100;200;300;400;500");
    }

    private void SetupDefaultErrorMessages()
    {
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.Success))
            .ReturnsAsync("Success");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.InvalidPin))
            .ReturnsAsync("Invalid PIN");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.InsufficientBalance))
            .ReturnsAsync("Insufficient balance");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.TransferAmountAboveMax))
            .ReturnsAsync("Transfer amount exceeds maximum");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.TransferAmountBelowMin))
            .ReturnsAsync("Transfer amount below minimum");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.ExceedsMaxPerDay))
            .ReturnsAsync("Exceeds maximum transfers per day");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.ExceedsMaxCapPerDay))
            .ReturnsAsync("Exceeds maximum cap per day");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.AmountNotMultipleOfFive))
            .ReturnsAsync("Transfer amount not valid");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("SuccessMessage", It.IsAny<string>()))
            .ReturnsAsync("Success");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("ValidationSuccessMessage", It.IsAny<string>()))
            .ReturnsAsync("Validation successful");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("ErrorMessage_InvalidInput", It.IsAny<string>()))
            .ReturnsAsync("Invalid input");
    }

    #region TransferCreditAsync Tests

    [Fact]
    public async Task TransferCreditAsync_Should_Return_Success_For_Valid_Transfer()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 500;
        const string pin = "1234";
        const long expectedTransactionId = 123456;

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, expectedTransactionId);

        // Act
        var result = await _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Assert
        result.statusCode.Should().Be(0);
        result.statusMessage.Should().Be("Success");
        result.transactionId.Should().Be(expectedTransactionId);
    }

    [Fact]
    public async Task TransferCreditAsync_Should_Call_Configuration_Load_Once()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 0;
        const string pin = "1234";

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Act
        await _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Assert
        _mockConfigRepository.Verify(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", It.IsAny<List<int>>()), Times.Once);
        _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData(5, 0, "Valid minimum transfer")]
    [InlineData(100, 0, "Valid maximum transfer")]
    [InlineData(10, 500, "Valid riyal and baisa")]
    [InlineData(0, 1000, "Valid baisa only")]
    public async Task TransferCreditAsync_Should_Handle_Various_Amount_Formats(int amountRiyal, int amountBaisa, string scenario)
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const string pin = "1234";

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Act
        var result = await _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Assert
        result.statusCode.Should().Be(0, $"Transfer should succeed for {scenario}");
        result.statusMessage.Should().Be("Success");
        result.transactionId.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("", "96887654321", "Source MSISDN empty")]
    [InlineData("96812345678", "", "Destination MSISDN empty")]
    [InlineData("96812345678", "96812345678", "Source equals destination")]
    [InlineData("123", "96887654321", "Source MSISDN too short")]
    [InlineData("96812345678", "123", "Destination MSISDN too short")]
    public async Task TransferCreditAsync_Should_Return_Error_For_Invalid_MSISDNs(string sourceMsisdn, string destinationMsisdn, string scenario)
    {
        // Arrange
        const int amountRiyal = 10;
        const int amountBaisa = 0;
        const string pin = "1234";

        SetupValidationFailureScenario();

        // Act
        var result = await _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Assert
        result.statusCode.Should().NotBe(0, $"Transfer should fail for {scenario}");
        result.statusMessage.Should().NotBeNullOrEmpty();
        result.transactionId.Should().Be(0);
    }

    [Theory]
    [InlineData(-1, 0, "Negative riyal")]
    [InlineData(0, -1, "Negative baisa")]
    [InlineData(0, 0, "Zero amount")]
    [InlineData(1001, 0, "Exceeds maximum riyal")]
    [InlineData(0, 1001, "Exceeds maximum baisa")]
    public async Task TransferCreditAsync_Should_Return_Error_For_Invalid_Amounts(int amountRiyal, int amountBaisa, string scenario)
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const string pin = "1234";

        SetupValidationFailureScenario();

        // Act
        var result = await _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Assert
        result.statusCode.Should().NotBe(0, $"Transfer should fail for {scenario}");
        result.statusMessage.Should().NotBeNullOrEmpty();
        result.transactionId.Should().Be(0);
    }

    [Theory]
    [InlineData("", "Empty PIN")]
    [InlineData("123", "PIN too short")]
    [InlineData("12345", "PIN too long")]
    [InlineData("abcd", "Non-numeric PIN")]
    public async Task TransferCreditAsync_Should_Return_Error_For_Invalid_PINs(string pin, string scenario)
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 0;

        SetupValidationFailureScenario();

        // Act
        var result = await _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Assert
        result.statusCode.Should().NotBe(0, $"Transfer should fail for {scenario}");
        result.statusMessage.Should().NotBeNullOrEmpty();
        result.transactionId.Should().Be(0);
    }

    #endregion

    #region TransferCreditWithAdjustmentReasonAsync Tests

    [Fact]
    public async Task TransferCreditWithAdjustmentReasonAsync_Should_Return_Success_For_Valid_Transfer()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 500;
        const string pin = "1234";
        const string adjustmentReason = "Customer service adjustment";
        const long expectedTransactionId = 123456;

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, expectedTransactionId);

        // Act
        var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason);

        // Assert
        result.statusCode.Should().Be(0);
        result.statusMessage.Should().Be("Success");
        result.transactionId.Should().Be(expectedTransactionId);
    }

    [Theory]
    [InlineData("", "Empty adjustment reason")]
    [InlineData("   ", "Whitespace only adjustment reason")]
    [InlineData(null, "Null adjustment reason")]
    public async Task TransferCreditWithAdjustmentReasonAsync_Should_Handle_Invalid_AdjustmentReason(string adjustmentReason, string scenario)
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 0;
        const string pin = "1234";

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Act
        var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason);

        // Assert
        result.statusCode.Should().Be(0, $"Transfer should still succeed despite {scenario}");
        result.statusMessage.Should().Be("Success");
        result.transactionId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TransferCreditWithAdjustmentReasonAsync_Should_Include_AdjustmentReason_In_Processing()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 0;
        const string pin = "1234";
        const string adjustmentReason = "Manual adjustment for customer service";

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Act
        var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason);

        // Assert
        result.statusCode.Should().Be(0);
        result.statusMessage.Should().Be("Success");
        result.transactionId.Should().BeGreaterThan(0);
        
        // Verify that the transaction repository was called with a transaction containing the adjustment reason
        _mockTransactionRepository.Verify(x => x.AddTransactionAsync(It.Is<Transaction>(t => 
            t.AdjustmentReason == adjustmentReason)), Times.Once);
    }

    #endregion

    #region GetDenominationsAsync Tests

    [Fact]
    public async Task GetDenominationsAsync_Should_Return_Standard_Denominations()
    {
        // Arrange
        var expectedDenominations = new List<decimal> { 1.0m, 2.0m, 5.0m, 10.0m, 20.0m, 50.0m };
        SetupDenominationsScenario(expectedDenominations);

        // Act
        var result = await _creditTransferService.GetDenominationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDenominations);
    }

    [Fact]
    public async Task GetDenominationsAsync_Should_Return_Ascending_Order()
    {
        // Arrange
        var unorderedDenominations = new List<decimal> { 50.0m, 1.0m, 20.0m, 5.0m, 10.0m, 2.0m };
        SetupDenominationsScenario(unorderedDenominations);

        // Act
        var result = await _creditTransferService.GetDenominationsAsync();

        // Assert
        result.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetDenominationsAsync_Should_Handle_Empty_Configuration()
    {
        // Arrange
        SetupDenominationsScenario(new List<decimal>());

        // Act
        var result = await _creditTransferService.GetDenominationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDenominationsAsync_Should_Return_Unique_Values()
    {
        // Arrange
        var denominationsWithDuplicates = new List<decimal> { 1.0m, 2.0m, 2.0m, 5.0m, 5.0m, 10.0m };
        SetupDenominationsScenario(denominationsWithDuplicates);

        // Act
        var result = await _creditTransferService.GetDenominationsAsync();

        // Assert
        result.Should().OnlyHaveUniqueItems();
        result.Should().Contain(new[] { 1.0m, 2.0m, 5.0m, 10.0m });
        result.Should().HaveCount(4); // Should remove duplicates
    }

    #endregion

    #region TransferCreditWithoutPinAsync Tests

    [Fact]
    public async Task TransferCreditWithoutPinAsync_Should_Return_Success_For_Valid_Transfer()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const decimal amountRiyal = 10.5m;

        SetupSuccessfulValidationScenario(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Act
        var result = await _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.statusCode.Should().Be(0);
        result.statusMessage.Should().Be("Success");
    }

    [Theory]
    [InlineData(0.5, "Valid fractional amount")]
    [InlineData(1.0, "Valid whole amount")]
    [InlineData(10.250, "Valid amount with baisa")]
    [InlineData(100.999, "Valid maximum amount")]
    public async Task TransferCreditWithoutPinAsync_Should_Handle_Decimal_Amounts(decimal amountRiyal, string scenario)
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";

        SetupSuccessfulValidationScenario(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Act
        var result = await _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.statusCode.Should().Be(0, $"Transfer should succeed for {scenario}");
        result.statusMessage.Should().Be("Success");
    }

    [Theory]
    [InlineData(0, "Zero amount")]
    [InlineData(-1, "Negative amount")]
    [InlineData(1001, "Exceeds maximum amount")]
    public async Task TransferCreditWithoutPinAsync_Should_Return_Error_For_Invalid_Amounts(decimal amountRiyal, string scenario)
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";

        SetupValidationFailureScenario();

        // Act
        var result = await _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.statusCode.Should().NotBe(0, $"Transfer should fail for {scenario}");
        result.statusMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TransferCreditWithoutPinAsync_Should_Use_Default_PIN()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const decimal amountRiyal = 10.0m;
        const string expectedDefaultPin = "1234";

        SetupSuccessfulValidationScenario(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Act
        var result = await _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.statusCode.Should().Be(0);
        
        // Verify that the default PIN configuration was retrieved
        _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", It.IsAny<string>()), Times.AtLeastOnce);
    }

    #endregion

    #region ValidateTransferInputsAsync Tests

    [Fact]
    public async Task ValidateTransferInputsAsync_Should_Return_Success_For_Valid_Inputs()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const decimal amountRiyal = 10.0m;

        SetupSuccessfulValidationScenario(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Act
        var result = await _creditTransferService.ValidateTransferInputsAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.StatusCode.Should().Be(0);
        result.StatusMessage.Should().Be("Validation successful");
    }

    [Theory]
    [InlineData("", "96887654321", "Source MSISDN empty")]
    [InlineData("96812345678", "", "Destination MSISDN empty")]
    [InlineData("96812345678", "96812345678", "Source equals destination")]
    [InlineData("123", "96887654321", "Source MSISDN too short")]
    [InlineData("96812345678", "123", "Destination MSISDN too short")]
    public async Task ValidateTransferInputsAsync_Should_Return_Error_For_Invalid_MSISDNs(string sourceMsisdn, string destinationMsisdn, string scenario)
    {
        // Arrange
        const decimal amountRiyal = 10.0m;

        SetupValidationFailureScenario();

        // Act
        var result = await _creditTransferService.ValidateTransferInputsAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.StatusCode.Should().NotBe(0, $"Validation should fail for {scenario}");
        result.StatusMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(0, "Zero amount")]
    [InlineData(-1, "Negative amount")]
    [InlineData(1001, "Exceeds maximum amount")]
    public async Task ValidateTransferInputsAsync_Should_Return_Error_For_Invalid_Amounts(decimal amountRiyal, string scenario)
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";

        SetupValidationFailureScenario();

        // Act
        var result = await _creditTransferService.ValidateTransferInputsAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.StatusCode.Should().NotBe(0, $"Validation should fail for {scenario}");
        result.StatusMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateTransferInputsAsync_Should_Not_Perform_Actual_Transfer()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const decimal amountRiyal = 10.0m;

        SetupSuccessfulValidationScenario(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Act
        var result = await _creditTransferService.ValidateTransferInputsAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.StatusCode.Should().Be(0);
        
        // Verify that no actual transfer operations were called
        _mockNobillCallsService.Verify(x => x.ReserveEventAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ValidateTransferInputsAsync_InvalidAmount_ReturnsAmountNotMultipleOfFive()
    {
        // Arrange
        var sourceMsisdn = "96876325315";
        var destinationMsisdn = "96878715705";
        var amount = 123.45m; // Invalid amount (not multiple of 5)
        
        // Act
        var result = await _creditTransferService.ValidateTransferInputsAsync(
            sourceMsisdn, destinationMsisdn, amount);
        
        // Assert
        result.StatusCode.Should().Be(ErrorCodes.AmountNotMultipleOfFive);
        result.StatusMessage.Should().Contain("multiple of 5");
    }

    #endregion

    #region Activity and Telemetry Tests

    [Fact]
    public async Task TransferCreditAsync_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 0;
        const string pin = "1234";

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        Activity? capturedActivity = null;
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        
        listener.ActivityStopped = activity =>
        {
            if (activity.Source.Name == "CreditTransfer.Core.Business")
                capturedActivity = activity;
        };

        ActivitySource.AddActivityListener(listener);

        var result = await _creditTransferService.TransferCreditAsync(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Assert
        result.statusCode.Should().Be(0);
        capturedActivity.Should().NotBeNull();
        capturedActivity!.DisplayName.Should().Be("CreditTransfer.TransferCredit");
        capturedActivity.Tags.Should().Contain(tag => tag.Key == "operation" && tag.Value == "TransferCredit");
        capturedActivity.Tags.Should().Contain(tag => tag.Key == "source.msisdn" && tag.Value == sourceMsisdn);
        capturedActivity.Tags.Should().Contain(tag => tag.Key == "destination.msisdn" && tag.Value == destinationMsisdn);
        capturedActivity.Tags.Should().Contain(tag => tag.Key == "amount.riyal" && tag.Value == amountRiyal.ToString());
        capturedActivity.Tags.Should().Contain(tag => tag.Key == "amount.baisa" && tag.Value == amountBaisa.ToString());
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulTransferScenario(string sourceMsisdn, string destinationMsisdn, int amountRiyal, int amountBaisa, string pin, long transactionId = 123456)
    {
        var sourceSubscriptionType = "Customer";
        var destinationSubscriptionType = "Customer";
        var sourceBalance = 100.0m;
        var maxTransferAmount = 50.0m;
        var transferConfig = new TransferConfig
        {
            ID = 1,
            SubscriptionType = "Customer",
            NobillSubscritpionType = sourceSubscriptionType,
            MinTransferAmount = 1.0m,
            MaxTransferAmount = maxTransferAmount,
            DailyTransferCountLimit = 10,
            DailyTransferCapLimit = 100.0m,
            MinPostTransferBalance = 1.0m,
            CreditTransferCustomerService = "CS_CUSTOMER"
        };

        // Setup subscription repository mocks
        _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(sourceMsisdn))
            .ReturnsAsync(sourceSubscriptionType);
        
        _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(destinationMsisdn))
            .ReturnsAsync(destinationSubscriptionType);
        
        _mockSubscriptionRepository.Setup(x => x.GetAccountTypeAsync(sourceSubscriptionType))
            .ReturnsAsync(SubscriptionType.Customer);
        
        _mockSubscriptionRepository.Setup(x => x.GetAccountTypeAsync(destinationSubscriptionType))
            .ReturnsAsync(SubscriptionType.Customer);
        
        _mockSubscriptionRepository.Setup(x => x.GetSubscriptionBlockStatusAsync(sourceMsisdn))
            .ReturnsAsync(SubscriptionBlockStatus.NO_BLOCK);
        
        _mockSubscriptionRepository.Setup(x => x.GetSubscriptionStatusAsync(destinationMsisdn))
            .ReturnsAsync(SubscriptionStatus.ACTIVE);

        // Setup transfer config repository mocks
        _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(sourceSubscriptionType))
            .ReturnsAsync(transferConfig);
        
        _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(destinationSubscriptionType))
            .ReturnsAsync(transferConfig);

        // Setup transfer rules service
        string specificServiceName = "CreditTransfer";
        _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(sourceMsisdn, It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>()))
            .ReturnsAsync((true, 0, "Success"));

        // Setup NoBill service mocks with fixed return tuples
        _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(sourceMsisdn, AccountItem.balance))
            .ReturnsAsync((sourceBalance.ToString(), 0));
        
        _mockNobillCallsService.Setup(x => x.GetCreditTransferValueByServiceNameAsync(sourceMsisdn, CreditTransferItem.p_pin, specificServiceName))
            .ReturnsAsync((pin, 0));
        
        _mockNobillCallsService.Setup(x => x.GetCreditTransferValueByServiceNameAsync(sourceMsisdn, CreditTransferItem.p_maxamount, specificServiceName))
            .ReturnsAsync((maxTransferAmount.ToString(), 0));
        
        _mockNobillCallsService.Setup(x => x.ReserveEventAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((12345.ToString(), 0));

        _mockNobillCallsService.Setup(x => x.ChargeReservedEventAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(0);

        _mockNobillCallsService.Setup(x => x.GetSubscriptionValueAsync(It.IsAny<string>(), It.IsAny<SubscriptionItem>()))
            .ReturnsAsync(("Customer", 0));

        // Setup transaction repository mocks
        _mockTransactionRepository.Setup(x => x.GetDailyTransferCountAsync(sourceMsisdn))
            .ReturnsAsync(0);
        
        _mockTransactionRepository.Setup(x => x.GetDailyTransferAmount(sourceMsisdn))
            .ReturnsAsync(0.0m);
        
        _mockTransactionRepository.Setup(x => x.AddTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(transactionId);
    }

    private void SetupValidationFailureScenario()
    {
        // Setup for failure scenarios - return validation errors
        _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(It.IsAny<string>(), It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>()))
            .ReturnsAsync((false, 20, "Invalid input"));
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(20))
            .ReturnsAsync("Invalid input");
        
        // Setup basic dependencies to avoid null reference exceptions
        _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(It.IsAny<string>()))
            .ReturnsAsync("Customer");
        
        var transferConfig = new TransferConfig
        {
            ID = 1,
            SubscriptionType = "Customer",
            NobillSubscritpionType = "Customer",
            MinTransferAmount = 1.0m,
            MaxTransferAmount = 100.0m,
            DailyTransferCountLimit = 10,
            DailyTransferCapLimit = 100.0m,
            MinPostTransferBalance = 1.0m,
            CreditTransferCustomerService = "CS_CUSTOMER"
        };
        
        _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
            .ReturnsAsync(transferConfig);
        
        _mockSubscriptionRepository.Setup(x => x.GetAccountTypeAsync(It.IsAny<string>()))
            .ReturnsAsync(SubscriptionType.Customer);
    }

    private void SetupSuccessfulValidationScenario(string sourceMsisdn, string destinationMsisdn, decimal amountRiyal)
    {
        var sourceSubscriptionType = "Customer";
        var destinationSubscriptionType = "Customer";
        var transferConfig = new TransferConfig
        {
            ID = 1,
            SubscriptionType = "Customer",
            NobillSubscritpionType = sourceSubscriptionType,
            MinTransferAmount = 1.0m,
            MaxTransferAmount = 100.0m,
            DailyTransferCountLimit = 10,
            DailyTransferCapLimit = 100.0m,
            MinPostTransferBalance = 1.0m,
            CreditTransferCustomerService = "CS_CUSTOMER"
        };

        // Setup subscription repository mocks
        _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(sourceMsisdn))
            .ReturnsAsync(sourceSubscriptionType);
        
        _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(destinationMsisdn))
            .ReturnsAsync(destinationSubscriptionType);
        
        _mockSubscriptionRepository.Setup(x => x.GetAccountTypeAsync(sourceSubscriptionType))
            .ReturnsAsync(SubscriptionType.Customer);
        
        _mockSubscriptionRepository.Setup(x => x.GetAccountTypeAsync(destinationSubscriptionType))
            .ReturnsAsync(SubscriptionType.Customer);
        
        _mockSubscriptionRepository.Setup(x => x.GetSubscriptionBlockStatusAsync(sourceMsisdn))
            .ReturnsAsync(SubscriptionBlockStatus.NO_BLOCK);
        
        _mockSubscriptionRepository.Setup(x => x.GetSubscriptionStatusAsync(destinationMsisdn))
            .ReturnsAsync(SubscriptionStatus.ACTIVE);

        // Setup transfer config repository mocks
        _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(sourceSubscriptionType))
            .ReturnsAsync(transferConfig);
        
        _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(destinationSubscriptionType))
            .ReturnsAsync(transferConfig);

        // Setup transfer rules service
        string specificServiceName = "CreditTransfer";
        _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(sourceMsisdn, It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>()))
            .ReturnsAsync((true, 0, "Validation successful"));

        // Setup NoBill service mocks with fixed return tuples
        _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(sourceMsisdn, AccountItem.balance))
            .ReturnsAsync(("100.0", 0));
        
        _mockNobillCallsService.Setup(x => x.GetCreditTransferValueByServiceNameAsync(sourceMsisdn, CreditTransferItem.p_maxamount, specificServiceName))
            .ReturnsAsync(("50.0", 0));

        // Setup transaction repository mocks
        _mockTransactionRepository.Setup(x => x.GetDailyTransferCountAsync(sourceMsisdn))
            .ReturnsAsync(0);
        
        _mockTransactionRepository.Setup(x => x.GetDailyTransferAmount(sourceMsisdn))
            .ReturnsAsync(0.0m);
    }

    private void SetupDenominationsScenario(List<decimal> denominations)
    {
        // Convert denominations to string format as expected by configuration
        var eventIds = string.Join(";", denominations.Select(d => ((int)(d * 100)).ToString()));
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_VirginEventIds", It.IsAny<string>()))
            .ReturnsAsync(eventIds);
    }

    public void Dispose()
    {
        _activitySource?.Dispose();
    }

    #endregion
} 