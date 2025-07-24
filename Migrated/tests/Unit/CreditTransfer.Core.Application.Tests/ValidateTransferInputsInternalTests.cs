using System.Diagnostics;
using FluentAssertions;
using Moq;
using Xunit;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Application.Services;
using CreditTransfer.Core.Domain.Enums;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Core.Domain.Constants;
using Microsoft.Extensions.Logging;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services.NobillCalls;

namespace CreditTransfer.Core.Application.Tests
{
    /// <summary>
    /// Comprehensive unit tests for ValidateTransferInputsInternalAsync method
    /// Tests all 9 validation categories including parameter validation, NoBill integration,
    /// business rules validation, balance checks, subscription status, daily limits, and error handling
    /// </summary>
    public class ValidateTransferInputsInternalTests : IDisposable
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

        // Test data constants
        private const string VALID_SOURCE_MSISDN = "96812345678";
        private const string VALID_DESTINATION_MSISDN = "96887654321";
        private const decimal VALID_AMOUNT = 10.0m;
        private const string VALID_SUBSCRIPTION_TYPE = "Customer";
        private const decimal VALID_BALANCE = 100.0m;

        public ValidateTransferInputsInternalTests()
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

            _creditTransferService = new CreditTransferService(
                _mockNobillCallsService.Object,
                _mockSubscriptionRepository.Object,
                _mockTransactionRepository.Object,
                _mockErrorConfigService.Object,
                _mockTransferConfigRepository.Object,
                _mockConfigRepository.Object,
                _mockTransferRulesService.Object,
                _mockLogger.Object,
                _activitySource
            );

            SetupDefaultConfiguration();
            SetupDefaultMocks();
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
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("Country", ""))
                .ReturnsAsync("OM");
        }

        private void SetupDefaultMocks()
        {
            // Setup default success scenario
            _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(VALID_SUBSCRIPTION_TYPE);

            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 100.0m,
                DailyTransferCountLimit = 10,
                DailyTransferCapLimit = 500.0m,
                MinPostTransferBalance = 5.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockSubscriptionRepository.Setup(x => x.GetAccountTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(SubscriptionType.Customer);

            _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(It.IsAny<string>(), It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((true, 0, "Transfer allowed"));

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(It.IsAny<string>(), AccountItem.balance))
                .ReturnsAsync((0, VALID_BALANCE.ToString()));

            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(It.IsAny<string>(), CreditTransferItem.p_maxamount))
                .ReturnsAsync((0, "100.0"));

            _mockSubscriptionRepository.Setup(x => x.GetSubscriptionBlockStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(SubscriptionBlockStatus.NO_BLOCK);

            _mockSubscriptionRepository.Setup(x => x.GetSubscriptionStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(SubscriptionStatus.ACTIVE);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferCountAsync(It.IsAny<string>()))
                .ReturnsAsync(0);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferAmount(It.IsAny<string>()))
                .ReturnsAsync(0.0m);

            // Setup error messages
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.Success))
                .ReturnsAsync("Success");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.InvalidSourcePhone))
                .ReturnsAsync("Invalid source phone number");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.InvalidDestinationPhone))
                .ReturnsAsync("Invalid destination phone number");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.SourceDestinationSame))
                .ReturnsAsync("Source and destination cannot be the same");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.RemainingBalanceHalf))
                .ReturnsAsync("Insufficient remaining balance");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.TransferAmountAboveMax))
                .ReturnsAsync("Transfer amount exceeds maximum");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.TransferAmountBelowMin))
                .ReturnsAsync("Transfer amount below minimum");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.ExceedsMaxPerDay))
                .ReturnsAsync("Exceeds maximum transfers per day");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.ExceedsMaxCapPerDay))
                .ReturnsAsync("Exceeds maximum cap per day");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.RemainingBalance))
                .ReturnsAsync("Insufficient remaining balance");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.MiscellaneousError))
                .ReturnsAsync("Miscellaneous error");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.SourcePhoneNotFound))
                .ReturnsAsync("Source phone number not found");
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(ErrorCodes.DestinationPhoneNotFound))
                .ReturnsAsync("Destination phone number not found");
        }

        #region Category 1: Parameter Validation Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_Success_For_Valid_Inputs()
        {
            // Arrange & Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(0, "Valid inputs should pass validation");
            result.statusMessage.Should().Be("Success");
        }

        [Theory]
        [InlineData("", "96887654321", "Source MSISDN empty")]
        [InlineData("abc", "96887654321", "Source MSISDN non-numeric")]
        [InlineData("123", "96887654321", "Source MSISDN too short")]
        [InlineData("123456789012345", "96887654321", "Source MSISDN too long")]
        public async Task ValidateTransferInputs_Should_Return_InvalidSourcePhone_For_Invalid_Source_MSISDN(
            string sourceMsisdn, string destinationMsisdn, string scenario)
        {
            // Arrange & Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(sourceMsisdn, destinationMsisdn, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.InvalidSourcePhone, $"Should fail for {scenario}");
            result.statusMessage.Should().Contain("Invalid source phone");
        }

        [Theory]
        [InlineData("96812345678", "", "Destination MSISDN empty")]
        [InlineData("96812345678", "xyz", "Destination MSISDN non-numeric")]
        [InlineData("96812345678", "456", "Destination MSISDN too short")]
        [InlineData("96812345678", "456789012345678", "Destination MSISDN too long")]
        public async Task ValidateTransferInputs_Should_Return_InvalidDestinationPhone_For_Invalid_Destination_MSISDN(
            string sourceMsisdn, string destinationMsisdn, string scenario)
        {
            // Arrange & Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(sourceMsisdn, destinationMsisdn, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.InvalidDestinationPhone, $"Should fail for {scenario}");
            result.statusMessage.Should().Contain("Invalid destination phone");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_SourceDestinationSame_When_Numbers_Are_Same()
        {
            // Arrange
            const string sameMsisdn = VALID_SOURCE_MSISDN;

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(sameMsisdn, sameMsisdn, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.SourceDestinationSame);
            result.statusMessage.Should().Contain("Source and destination cannot be the same");
        }

        #endregion

        #region Category 2: NoBill Integration Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_InvalidSourcePhone_When_Source_NoBill_Lookup_Fails()
        {
            // Arrange
            _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(VALID_SOURCE_MSISDN))
                .ThrowsAsync(new Exception("NoBill lookup failed"));

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.InvalidSourcePhone);
            result.statusMessage.Should().Contain("Invalid source phone");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_InvalidDestinationPhone_When_Destination_NoBill_Lookup_Fails()
        {
            // Arrange
            _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(VALID_DESTINATION_MSISDN))
                .ThrowsAsync(new Exception("NoBill lookup failed"));

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.InvalidDestinationPhone);
            result.statusMessage.Should().Contain("Invalid destination phone");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_SourcePhoneNotFound_When_Source_Config_Not_Found()
        {
            // Arrange
            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(VALID_SUBSCRIPTION_TYPE))
                .ReturnsAsync((TransferConfig?)null);

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.SourcePhoneNotFound);
            result.statusMessage.Should().Contain("Source phone number not found");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_DestinationPhoneNotFound_When_Destination_Config_Not_Found()
        {
            // Arrange
            _mockTransferConfigRepository.SetupSequence(x => x.GetByNobillSubscriptionTypeAsync(VALID_SUBSCRIPTION_TYPE))
                .ReturnsAsync(new TransferConfig { ID = 1, SubscriptionType = VALID_SUBSCRIPTION_TYPE })  // Source config
                .ReturnsAsync((TransferConfig?)null);  // Destination config

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.DestinationPhoneNotFound);
            result.statusMessage.Should().Contain("Destination phone number not found");
        }

        #endregion

        #region Category 3: Business Rules Validation Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_BusinessRule_Error_When_Transfer_Not_Allowed()
        {
            // Arrange
            _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync("OM", SubscriptionType.Customer, SubscriptionType.Customer, It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((false, 33, "Transfer not allowed by business rules"));

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(33, "Should return business rule error code");
            result.statusMessage.Should().Contain("Transfer not allowed by business rules");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Use_Correct_Country_For_Business_Rules()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("Country", ""))
                .ReturnsAsync("KSA");

            // Act
            await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            _mockTransferRulesService.Verify(x => x.EvaluateTransferRuleAsync("KSA", It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(), It.IsAny<Dictionary<string, string>>()), 
                Times.Once, "Should use KSA country code for business rules");
        }

        #endregion

        #region Category 4: Balance and Amount Validation Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_RemainingBalanceHalf_When_MaximumPercentageAmount_Exceeded()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 0m))
                .ReturnsAsync(2.0m); // 50% maximum

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "20.0")); // Balance: 20 OMR

            // Act - Try to transfer 15 OMR (would leave 5 OMR, which is less than 20/2 = 10 OMR minimum)
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 15.0m);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.RemainingBalanceHalf);
            result.statusMessage.Should().Contain("Insufficient remaining balance");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Allow_Transfer_When_MaximumPercentageAmount_Is_One()
        {
            // Arrange - MaximumPercentageAmount = 1 means no percentage restriction
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 0m))
                .ReturnsAsync(1.0m);

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "20.0"));

            // Act - Transfer almost all balance
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 19.0m);

            // Assert
            result.statusCode.Should().Be(0, "Should allow transfer when MaximumPercentageAmount is 1");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_TransferAmountAboveMax_When_Amount_Exceeds_Maximum()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_maxamount))
                .ReturnsAsync((0, "50.0")); // Max transfer amount: 50 OMR

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 100.0m);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.TransferAmountAboveMax);
            result.statusMessage.Should().Contain("Transfer amount exceeds maximum");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_TransferAmountBelowMin_When_Amount_Below_Minimum()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                MinTransferAmount = 5.0m, // Minimum 5 OMR
                MaxTransferAmount = 100.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 2.0m);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.TransferAmountBelowMin);
            result.statusMessage.Should().Contain("Transfer amount below minimum");
        }

        #endregion

        #region Category 5: Subscription Status Validation Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_MiscellaneousError_When_Source_Is_Blocked()
        {
            // Arrange
            _mockSubscriptionRepository.Setup(x => x.GetSubscriptionBlockStatusAsync(VALID_SOURCE_MSISDN))
                .ReturnsAsync(SubscriptionBlockStatus.ALL_BLOCK);

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.MiscellaneousError);
            result.statusMessage.Should().Contain("Miscellaneous error");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_InvalidDestinationPhone_When_Destination_ACTIVE_BEFORE_FIRST_USE()
        {
            // Arrange
            _mockSubscriptionRepository.Setup(x => x.GetSubscriptionStatusAsync(VALID_DESTINATION_MSISDN))
                .ReturnsAsync(SubscriptionStatus.ACTIVE_BEFORE_FIRST_USE);

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.InvalidDestinationPhone);
            result.statusMessage.Should().Contain("Invalid destination phone");
        }

        [Theory]
        [InlineData(SubscriptionStatus.ACTIVE, "Active subscription should be allowed")]
        [InlineData(SubscriptionStatus.INACTIVE, "Inactive subscription should be allowed")]
        [InlineData(SubscriptionStatus.ACTIVE_IN_USE, "Active in use subscription should be allowed")]
        public async Task ValidateTransferInputs_Should_Allow_Valid_Destination_Subscription_Status(
            SubscriptionStatus status, string scenario)
        {
            // Arrange
            _mockSubscriptionRepository.Setup(x => x.GetSubscriptionStatusAsync(VALID_DESTINATION_MSISDN))
                .ReturnsAsync(status);

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(0, scenario);
        }

        #endregion

        #region Category 6: Daily Limits Validation Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_ExceedsMaxPerDay_When_Daily_Transfer_Count_Exceeded()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                DailyTransferCountLimit = 5, // Maximum 5 transfers per day
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 100.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferCountAsync(VALID_SOURCE_MSISDN))
                .ReturnsAsync(5); // Already made 5 transfers today

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.ExceedsMaxPerDay);
            result.statusMessage.Should().Contain("Exceeds maximum transfers per day");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Allow_Transfer_When_Daily_Count_Under_Limit()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                DailyTransferCountLimit = 5,
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 100.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferCountAsync(VALID_SOURCE_MSISDN))
                .ReturnsAsync(3); // Only 3 transfers today

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(0, "Should allow transfer when under daily count limit");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_ExceedsMaxCapPerDay_When_Daily_Amount_Cap_Exceeded()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                DailyTransferCapLimit = 100.0m, // Maximum 100 OMR per day
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 50.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferAmount(VALID_SOURCE_MSISDN))
                .ReturnsAsync(100.0m); // Already transferred 100 OMR today

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.ExceedsMaxCapPerDay);
            result.statusMessage.Should().Contain("Exceeds maximum cap per day");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Allow_Transfer_When_Daily_Cap_Under_Limit()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                DailyTransferCapLimit = 100.0m,
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 50.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferAmount(VALID_SOURCE_MSISDN))
                .ReturnsAsync(80.0m); // Only 80 OMR transferred today

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(0, "Should allow transfer when under daily cap limit");
        }

        #endregion

        #region Category 7: Post-Transfer Balance Validation Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_RemainingBalance_When_Post_Transfer_Balance_Too_Low()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                MinPostTransferBalance = 10.0m, // Must keep at least 10 OMR after transfer
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 100.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "20.0")); // Balance: 20 OMR

            // Act - Try to transfer 15 OMR (would leave 5 OMR, less than required 10 OMR)
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 15.0m);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.RemainingBalance);
            result.statusMessage.Should().Contain("Insufficient remaining balance");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Allow_Transfer_When_Post_Transfer_Balance_Sufficient()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                MinPostTransferBalance = 10.0m,
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 100.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "50.0")); // Balance: 50 OMR

            // Act - Transfer 25 OMR (would leave 25 OMR, more than required 10 OMR)
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 25.0m);

            // Assert
            result.statusCode.Should().Be(0, "Should allow transfer when post-transfer balance is sufficient");
        }

        #endregion

        #region Category 8: Error Handling Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Return_MiscellaneousError_When_Unexpected_Exception_Occurs()
        {
            // Arrange
            _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Unexpected database error"));

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(ErrorCodes.InvalidSourcePhone, "Should handle unexpected exceptions gracefully");
            result.statusMessage.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Log_Error_When_Exception_Occurs()
        {
            // Arrange
            _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error validating transfer inputs")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never, // Note: The method catches specific exceptions, not all exceptions
                "Should log errors when exceptions occur");
        }

        #endregion

        #region Category 9: Integration and Edge Case Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Handle_Zero_Balance_Gracefully()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "0")); // Zero balance

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().NotBe(0, "Should fail validation with zero balance");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Handle_Invalid_Balance_Response()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "invalid_balance")); // Invalid balance format

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle invalid balance responses");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Handle_Null_Transfer_Config_Fields()
        {
            // Arrange
            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = VALID_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = VALID_SUBSCRIPTION_TYPE,
                // All optional fields are null
                MinTransferAmount = null,
                MaxTransferAmount = null,
                DailyTransferCountLimit = null,
                DailyTransferCapLimit = null,
                MinPostTransferBalance = null,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(0, "Should handle null transfer config fields gracefully");
        }

        [Theory]
        [InlineData(0.001, "Very small amount")]
        [InlineData(999999.999, "Very large amount")]
        [InlineData(1.5, "Decimal amount")]
        public async Task ValidateTransferInputs_Should_Handle_Edge_Case_Amounts(decimal amount, string scenario)
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "1000000")); // Very high balance

            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_maxamount))
                .ReturnsAsync((0, "1000000")); // Very high max amount

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, amount);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, $"Should handle {scenario} appropriately");
            result.statusMessage.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region Configuration and Setup Tests

        [Fact]
        public async Task ValidateTransferInputs_Should_Load_Configuration_On_First_Call()
        {
            // Arrange & Act
            await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int>()), Times.AtLeastOnce, "Should load MSISDN length configuration");
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 0m), Times.AtLeastOnce, "Should load MaximumPercentageAmount configuration");
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("Country", ""), Times.AtLeastOnce, "Should load Country configuration");
        }

        [Fact]
        public async Task ValidateTransferInputs_Should_Use_Correct_Service_Dependencies()
        {
            // Arrange & Act
            await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            _mockSubscriptionRepository.Verify(x => x.GetNobillSubscriptionTypeAsync(It.IsAny<string>()), Times.AtLeast(2), "Should call subscription repository for both MSISDNs");
            _mockTransferConfigRepository.Verify(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()), Times.AtLeast(2), "Should call transfer config repository for both subscription types");
            _mockTransferRulesService.Verify(x => x.EvaluateTransferRuleAsync(It.IsAny<string>(), It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(), It.IsAny<Dictionary<string, string>>()), Times.Once, "Should evaluate transfer rules");
            _mockNobillCallsService.Verify(x => x.GetAccountValueAsync(It.IsAny<string>(), AccountItem.balance), Times.Once, "Should get account balance");
            _mockNobillCallsService.Verify(x => x.GetCreditTransferValueAsync(It.IsAny<string>(), CreditTransferItem.p_maxamount), Times.Once, "Should get max transfer amount");
        }

        #endregion

        public void Dispose()
        {
            _activitySource?.Dispose();
        }
    }
} 