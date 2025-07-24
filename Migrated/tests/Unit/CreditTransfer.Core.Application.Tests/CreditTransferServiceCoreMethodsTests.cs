using CreditTransfer.Core.Application.DTOs;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Application.Services;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Core.Domain.Enums;
using CreditTransfer.Core.Domain.Constants;
using FluentAssertions;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services.NobillCalls;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using Xunit;

namespace CreditTransfer.Core.Application.Tests;

/// <summary>
/// Unit tests for CreditTransferService core transfer methods
/// Tests the 5 main public methods: TransferCreditAsync, TransferCreditWithAdjustmentReasonAsync, 
/// GetDenominationsAsync, TransferCreditWithoutPinAsync, ValidateTransferInputsAsync
/// </summary>
public class CreditTransferServiceCoreMethodsTests
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
        _mockNobillCallsService = new Mock<INobillCallsService>();
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockTransferConfigRepository = new Mock<ITransferConfigRepository>();
        _mockErrorConfigService = new Mock<IErrorConfigurationService>();
        _mockConfigRepository = new Mock<IApplicationConfigRepository>();
        _mockTransferRulesService = new Mock<ITransferRulesService>();
        _mockLogger = new Mock<ILogger<CreditTransferService>>();
        _activitySource = new ActivitySource("CreditTransfer.Tests");

        // Setup default configuration values
        SetupDefaultConfiguration();
        
        // Setup default error messages
        SetupDefaultErrorMessages();

        _creditTransferService = new CreditTransferService(
            _mockNobillCallsService.Object,
            _mockSubscriptionRepository.Object,
            _mockTransactionRepository.Object,
            _mockErrorConfigService.Object,
            _mockTransferConfigRepository.Object,
            _mockConfigRepository.Object,
            _mockTransferRulesService.Object,
            _mockLogger.Object,
            _activitySource);
    }

    private void SetupDefaultConfiguration()
    {
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int>()))
            .ReturnsAsync(new List<int> { 11 });
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_RefillPinLength", 0))
            .ReturnsAsync(4);
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<bool>("CreditTransfer_EnableExtendedDays", false))
            .ReturnsAsync(false);
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", ""))
            .ReturnsAsync("0000");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 0m))
            .ReturnsAsync(1.0m);
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerTransferMoneyReason", ""))
            .ReturnsAsync("Credit transfer");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("Country", ""))
            .ReturnsAsync("OM");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_VirginEventIds", ""))
            .ReturnsAsync("1.0,2.0,5.0,10.0,20.0,50.0");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", ""))
            .ReturnsAsync("0.1;0.5;1;3;5;10;50;100");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", ""))
            .ReturnsAsync("0;0;0;0;0;0;0");
    }

    private void SetupDefaultErrorMessages()
    {
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.Success))
            .ReturnsAsync("Success");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.InvalidSourcePhone))
            .ReturnsAsync("Invalid source phone number");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.InvalidDestinationPhone))
            .ReturnsAsync("Invalid destination phone number");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.SourcePhoneNotFound))
            .ReturnsAsync("Source phone number not found");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.DestinationPhoneNotFound))
            .ReturnsAsync("Destination phone number not found");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.MiscellaneousError))
            .ReturnsAsync("Miscellaneous error");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.SourceDestinationSame))
            .ReturnsAsync("Source and destination cannot be the same");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.RemainingBalance))
            .ReturnsAsync("Insufficient remaining balance");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.RemainingBalanceHalf))
            .ReturnsAsync("Transfer would leave insufficient balance");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.TransferAmountAboveMax))
            .ReturnsAsync("Transfer amount exceeds maximum");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.TransferAmountBelowMin))
            .ReturnsAsync("Transfer amount below minimum");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.ExceedsMaxPerDay))
            .ReturnsAsync("Exceeds maximum transfers per day");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.ExceedsMaxCapPerDay))
            .ReturnsAsync("Exceeds maximum cap per day");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.InvalidPin))
            .ReturnsAsync("Invalid PIN");
        
        _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.AmountNotMultipleOfFive))
            .ReturnsAsync("Transfer amount not valid");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("SuccessMessage", ""))
            .ReturnsAsync("Success");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("ValidationSuccessMessage", ""))
            .ReturnsAsync("Validation successful");
        
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("ErrorMessage_InvalidInput", ""))
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
        _mockConfigRepository.Verify(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int>()), Times.Once);
        _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", ""), Times.Once);
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
        const int amountBaisa = 0;
        const string pin = "1234";
        const string adjustmentReason = "Promotional bonus";
        const long expectedTransactionId = 123456;

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, expectedTransactionId);

        // Act
        var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(
            sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason);

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

        SetupValidationFailureScenario();

        // Act
        var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(
            sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason);

        // Assert
        result.statusCode.Should().NotBe(0, $"Transfer should fail for {scenario}");
        result.statusMessage.Should().NotBeNullOrEmpty();
        result.transactionId.Should().Be(0);
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
        const string adjustmentReason = "Customer complaint resolution";

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Act
        var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(
            sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin, adjustmentReason);

        // Assert
        result.statusCode.Should().Be(0);
        result.statusMessage.Should().Be("Success");
    }

    #endregion

    #region GetDenominationsAsync Tests

    [Fact]
    public async Task GetDenominationsAsync_Should_Return_Standard_Denominations()
    {
        // Arrange
        var expectedDenominations = new List<decimal> { 1, 2, 5, 10, 20, 50, 100 };
        SetupDenominationsScenario(expectedDenominations);

        // Act
        var result = await _creditTransferService.GetDenominationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(7);
        result.Should().BeEquivalentTo(expectedDenominations);
    }

    [Fact]
    public async Task GetDenominationsAsync_Should_Return_Ascending_Order()
    {
        // Arrange
        var denominations = new List<decimal> { 100, 1, 50, 2, 20, 5, 10 };
        SetupDenominationsScenario(denominations);

        // Act
        var result = await _creditTransferService.GetDenominationsAsync();

        // Assert
        result.Should().NotBeNull();
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
        var denominationsWithDuplicates = new List<decimal> { 1, 2, 5, 5, 10, 10, 20 };
        SetupDenominationsScenario(denominationsWithDuplicates);

        // Act
        var result = await _creditTransferService.GetDenominationsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyHaveUniqueItems();
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
        const long expectedTransactionId = 123456;

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, (int)amountRiyal, 
            (int)((amountRiyal - Math.Floor(amountRiyal)) * 1000), "0000", expectedTransactionId);

        // Act
        var result = await _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.statusCode.Should().Be(0);
        result.statusMessage.Should().Be("Success");
        result.transactionId.Should().Be(expectedTransactionId);
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

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, (int)amountRiyal, 
            (int)((amountRiyal - Math.Floor(amountRiyal)) * 1000), "0000");

        // Act
        var result = await _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.statusCode.Should().Be(0, $"Transfer should succeed for {scenario}");
        result.statusMessage.Should().Be("Success");
        result.transactionId.Should().BeGreaterThan(0);
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
        result.transactionId.Should().Be(0);
    }

    [Fact]
    public async Task TransferCreditWithoutPinAsync_Should_Use_Default_PIN()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const decimal amountRiyal = 10.0m;

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, 10, 0, "0000");

        // Act
        var result = await _creditTransferService.TransferCreditWithoutPinAsync(sourceMsisdn, destinationMsisdn, amountRiyal);

        // Assert
        result.statusCode.Should().Be(0);
        _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", ""), Times.Once);
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
        result.statusCode.Should().Be(0);
        result.statusMessage.Should().Be("Validation successful");
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
        result.statusCode.Should().NotBe(0, $"Validation should fail for {scenario}");
        result.statusMessage.Should().NotBeNullOrEmpty();
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
        result.statusCode.Should().NotBe(0, $"Validation should fail for {scenario}");
        result.statusMessage.Should().NotBeNullOrEmpty();
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
        result.statusCode.Should().Be(0);
        // Verify no actual transfer operations were called
        _mockNobillCallsService.Verify(x => x.ReserveEventAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region OpenTelemetry Tests

    [Fact]
    public async Task TransferCreditAsync_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        const string sourceMsisdn = "96812345678";
        const string destinationMsisdn = "96887654321";
        const int amountRiyal = 10;
        const int amountBaisa = 500;
        const string pin = "1234";

        SetupSuccessfulTransferScenario(sourceMsisdn, destinationMsisdn, amountRiyal, amountBaisa, pin);

        // Act
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData;
        listener.SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData;

        Activity? capturedActivity = null;
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
        _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(It.IsAny<string>(), It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(),null ))
            .ReturnsAsync((true, 0, "Success"));

        // Setup NoBill service mocks
        _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(sourceMsisdn, AccountItem.balance))
            .ReturnsAsync((0, sourceBalance.ToString()));
        
        _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(sourceMsisdn, CreditTransferItem.p_pin))
            .ReturnsAsync((0, pin));
        
        _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(sourceMsisdn, CreditTransferItem.p_maxamount))
            .ReturnsAsync((0, maxTransferAmount.ToString()));
        
        _mockNobillCallsService.Setup(x => x.ReserveEventAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((0, 12345));

        _mockNobillCallsService.Setup(x => x.ChargeReservedEventAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(0);

        _mockNobillCallsService.Setup(x => x.GetSubscriptionValueAsync(It.IsAny<string>(), It.IsAny<SubscriptionItem>()))
            .ReturnsAsync((0, "Customer"));

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
        _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(It.IsAny<string>(), It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(),null))
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
        _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(It.IsAny<string>(), It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(),null))
            .ReturnsAsync((true, 0, "Validation successful"));

        // Setup NoBill service mocks
        _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(sourceMsisdn, AccountItem.balance))
            .ReturnsAsync((0, "100.0"));
        
        _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(sourceMsisdn, CreditTransferItem.p_maxamount))
            .ReturnsAsync((0, "50.0"));

        // Setup transaction repository mocks
        _mockTransactionRepository.Setup(x => x.GetDailyTransferCountAsync(sourceMsisdn))
            .ReturnsAsync(0);
        
        _mockTransactionRepository.Setup(x => x.GetDailyTransferAmount(sourceMsisdn))
            .ReturnsAsync(0.0m);
    }

    private void SetupDenominationsScenario(List<decimal> denominations)
    {
        var virginEventIds = string.Join(",", denominations.Select(d => d.ToString()));
        _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_VirginEventIds", ""))
            .ReturnsAsync(virginEventIds);
    }

    #endregion
    /// <summary>
    /// dispose method to clean up resources
    /// </summary>
    public void Dispose()
    {
        _activitySource?.Dispose();
    }
} 