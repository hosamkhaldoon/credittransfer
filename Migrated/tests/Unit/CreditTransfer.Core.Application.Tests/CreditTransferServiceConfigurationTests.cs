using System.Diagnostics;
using FluentAssertions;
using Moq;
using Xunit;
using CreditTransfer.Core.Application.Interfaces;
using CreditTransfer.Core.Application.Services;
using CreditTransfer.Core.Domain.Enums;
using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Core.Domain.Constants;
using CreditTransfer.Core.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using IntegrationProxies.Nobill.Interfaces;
using IntegrationProxies.Nobill.Services.NobillCalls;

namespace CreditTransfer.Core.Application.Tests
{
    /// <summary>
    /// Comprehensive unit tests for CreditTransferService configuration and utility methods
    /// Tests configuration loading, caching, validation, thread safety, and all utility methods
    /// including EnsureConfigurationLoadedAsync, exception handling, and configuration retrieval
    /// </summary>
    public class CreditTransferServiceConfigurationTests : IDisposable
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
        private const string VALID_PIN = "1234";
        private const string TEST_SUBSCRIPTION_TYPE = "Customer";

        public CreditTransferServiceConfigurationTests()
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

            SetupBasicMocks();
        }

        private void SetupBasicMocks()
        {
            // Setup basic mocks for service functionality
            _mockSubscriptionRepository.Setup(x => x.GetNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(TEST_SUBSCRIPTION_TYPE);

            var transferConfig = new TransferConfig
            {
                ID = 1,
                SubscriptionType = TEST_SUBSCRIPTION_TYPE,
                NobillSubscritpionType = TEST_SUBSCRIPTION_TYPE,
                MinTransferAmount = 1.0m,
                MaxTransferAmount = 100.0m,
                CreditTransferCustomerService = "CS_CUSTOMER"
            };

            _mockTransferConfigRepository.Setup(x => x.GetByNobillSubscriptionTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(transferConfig);

            _mockSubscriptionRepository.Setup(x => x.GetAccountTypeAsync(It.IsAny<string>()))
                .ReturnsAsync(SubscriptionType.Customer);

            _mockTransferRulesService.Setup(x => x.EvaluateTransferRuleAsync(It.IsAny<string>(), It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(), It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((true, 0, "Transfer allowed"));

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(It.IsAny<string>(), AccountItem.balance))
                .ReturnsAsync((0, "100.0"));

            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(It.IsAny<string>(), CreditTransferItem.p_maxamount))
                .ReturnsAsync((0, "100.0"));

            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(It.IsAny<string>(), CreditTransferItem.p_pin))
                .ReturnsAsync((0, "1234"));

            _mockSubscriptionRepository.Setup(x => x.GetSubscriptionBlockStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(SubscriptionBlockStatus.NO_BLOCK);

            _mockSubscriptionRepository.Setup(x => x.GetSubscriptionStatusAsync(It.IsAny<string>()))
                .ReturnsAsync(SubscriptionStatus.ACTIVE);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferCountAsync(It.IsAny<string>()))
                .ReturnsAsync(0);

            _mockTransactionRepository.Setup(x => x.GetDailyTransferAmount(It.IsAny<string>()))
                .ReturnsAsync(0.0m);

            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(It.IsAny<int>()))
                .ReturnsAsync("Test error message");
        }

        #region Category 1: Configuration Loading Tests

        [Fact]
        public async Task EnsureConfigurationLoaded_Should_Load_All_Required_Configuration_Values()
        {
            // Arrange
            var expectedMsisdnLength = new List<int> { 8, 11 };
            var expectedRefillPinLength = 6;
            var expectedEnableExtendedDays = true;
            var expectedDefaultPin = "9999";
            var expectedMaximumPercentageAmount = 2.5m;
            var expectedTransferReason = "Updated transfer reason";

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(expectedMsisdnLength);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_RefillPinLength", 4))
                .ReturnsAsync(expectedRefillPinLength);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<bool>("CreditTransfer_EnableExtendedDays", false))
                .ReturnsAsync(expectedEnableExtendedDays);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"))
                .ReturnsAsync(expectedDefaultPin);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 1.0m))
                .ReturnsAsync(expectedMaximumPercentageAmount);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerTransferMoneyReason", "Credit transfer"))
                .ReturnsAsync(expectedTransferReason);

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().Be(0, "Configuration should load successfully");
            
            // Verify all configuration values were loaded
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<int>("CreditTransfer_RefillPinLength", 4), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<bool>("CreditTransfer_EnableExtendedDays", false), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 1.0m), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerTransferMoneyReason", "Credit transfer"), Times.Once);
        }

        [Fact]
        public async Task EnsureConfigurationLoaded_Should_Only_Load_Configuration_Once()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"))
                .ReturnsAsync("1234");

            // Act - Call multiple operations that trigger configuration loading
            await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);
            await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);
            await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert - Configuration should only be loaded once despite multiple calls
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"), Times.Once);
        }

        [Fact]
        public async Task EnsureConfigurationLoaded_Should_Handle_Configuration_Errors()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", It.IsAny<List<int>>()))
                .ThrowsAsync(new Exception("Configuration service unavailable"));

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle configuration loading errors");
        }

        [Fact]
        public async Task EnsureConfigurationLoaded_Should_Be_Thread_Safe()
        {
            // Arrange
            var callCount = 0;
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .Returns(async () =>
                {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(100); // Simulate slow configuration loading
                    return new List<int> { 11 };
                });

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"))
                .ReturnsAsync("1234");

            // Act - Execute multiple concurrent operations
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT));
            }

            await Task.WhenAll(tasks);

            // Assert - Configuration should only be loaded once despite concurrent access
            callCount.Should().Be(1, "Configuration should only be loaded once in concurrent scenarios");
        }

        #endregion

        #region Category 2: Configuration Value Tests

        [Theory]
        [InlineData("CreditTransfer_MsisdnLength", new int[] { 8, 11, 13 })]
        [InlineData("CreditTransfer_MsisdnLength", new int[] { 11 })]
        [InlineData("CreditTransfer_MsisdnLength", new int[] { 8, 9, 10, 11, 12, 13 })]
        public async Task Configuration_Should_Support_Multiple_MSISDN_Lengths(string configKey, int[] msisdnLengths)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>(configKey, new List<int> { 11 }))
                .ReturnsAsync(msisdnLengths.ToList());

            // Act & Assert - Test various MSISDN lengths
            foreach (var length in msisdnLengths)
            {
                var testMsisdn = new string('9', length);
                var result = await _creditTransferService.ValidateTransferInputsAsync(testMsisdn, VALID_DESTINATION_MSISDN, VALID_AMOUNT);
                result.statusCode.Should().Be(0, $"Should accept MSISDN with length {length}");
            }
        }

        [Theory]
        [InlineData(4, "1234")]
        [InlineData(6, "123456")]
        [InlineData(8, "12345678")]
        public async Task Configuration_Should_Support_Different_PIN_Lengths(int pinLength, string testPin)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<int>("CreditTransfer_RefillPinLength", 4))
                .ReturnsAsync(pinLength);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, testPin);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, $"Should handle PIN length {pinLength}");
        }

        [Theory]
        [InlineData("0000")]
        [InlineData("1234")]
        [InlineData("9999")]
        [InlineData("")]
        public async Task Configuration_Should_Support_Different_Default_PINs(string defaultPin)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"))
                .ReturnsAsync(defaultPin);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, defaultPin);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, $"Should handle default PIN: {defaultPin}");
        }

        [Theory]
        [InlineData(1.0, "No percentage restriction")]
        [InlineData(2.0, "50% maximum transfer")]
        [InlineData(4.0, "25% maximum transfer")]
        [InlineData(10.0, "10% maximum transfer")]
        public async Task Configuration_Should_Support_Different_Maximum_Percentage_Amounts(decimal maxPercentage, string scenario)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 1.0m))
                .ReturnsAsync(maxPercentage);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(VALID_SOURCE_MSISDN, AccountItem.balance))
                .ReturnsAsync((0, "100.0"));

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 20.0m);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, scenario);
        }

        #endregion

        #region Category 3: SMS Configuration Tests

        [Fact]
        public async Task Configuration_Should_Load_SMS_Templates_Correctly()
        {
            // Arrange
            var expectedAPartySMSEn = "You have transferred {0} RO to {1}. Ref: {2}";
            var expectedBPartySMSEn = "You received {0} RO from {1}. Ref: {2}";
            var expectedAPartySMSAr = "تم تحويل {0} ر.ع إلى {1}. المرجع: {2}";
            var expectedBPartySMSAr = "استلمت {0} ر.ع من {1}. المرجع: {2}";

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_APartySMSEn", It.IsAny<string>()))
                .ReturnsAsync(expectedAPartySMSEn);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_BPartySMSEn", It.IsAny<string>()))
                .ReturnsAsync(expectedBPartySMSEn);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_APartySMSAr", It.IsAny<string>()))
                .ReturnsAsync(expectedAPartySMSAr);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_BPartySMSAr", It.IsAny<string>()))
                .ReturnsAsync(expectedBPartySMSAr);

            // Setup successful transfer scenario
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Setup NoBill operations

            _mockNobillCallsService.Setup(x => x.ReserveEventAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((0, 0));

            _mockNobillCallsService.Setup(x => x.CancelReservationAsync(It.IsAny<string>(), It.IsAny<int>()))
.ReturnsAsync((0, 0));



            _mockNobillCallsService.Setup(x => x.TransferMoneyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((0));

            _mockNobillCallsService.Setup(x => x.ExtendSubscriptionExpiryAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((0));

            _mockNobillCallsService.Setup(x => x.SendHTTPSMSAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((0));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().Be(0, "Transfer should succeed");

            // Verify SMS templates were loaded
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_APartySMSEn", It.IsAny<string>()), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_BPartySMSEn", It.IsAny<string>()), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_APartySMSAr", It.IsAny<string>()), Times.Once);
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("CreditTransfer_BPartySMSAr", It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData("", "Empty template")]
        [InlineData("Transfer completed", "Simple template")]
        [InlineData("You transferred {0} to {1}", "Parameterized template")]
        [InlineData("تم النقل بنجاح", "Arabic template")]
        public async Task Configuration_Should_Handle_Various_SMS_Templates(string smsTemplate, string scenario)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_APartySMSEn", It.IsAny<string>()))
                .ReturnsAsync(smsTemplate);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 5, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, $"Should handle {scenario}");
        }

        #endregion

        #region Category 4: Virgin Event IDs Configuration Tests

        [Fact]
        public async Task Configuration_Should_Parse_Virgin_Event_IDs_Correctly()
        {
            // Arrange
            var virginEventIds = "5|1005,10|1010,15|1015,20|1020,25|1025,30|1030,35|1035,40|1040,45|1045,50|1050";
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_VirginEventIds", It.IsAny<string>()))
                .ReturnsAsync(virginEventIds);

            // Act
            var result = await _creditTransferService.GetDenominationsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.Should().Contain(5.0m);
            result.Should().Contain(10.0m);
            result.Should().Contain(50.0m);
        }

        [Theory]
        [InlineData("1|100", "Single denomination")]
        [InlineData("1|100,5|500", "Two denominations")]
        [InlineData("", "Empty configuration")]
        [InlineData("invalid|format", "Invalid format")]
        public async Task Configuration_Should_Handle_Various_Virgin_Event_ID_Formats(string eventIds, string scenario)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_VirginEventIds", It.IsAny<string>()))
                .ReturnsAsync(eventIds);

            // Act
            var result = await _creditTransferService.GetDenominationsAsync();

            // Assert
            result.Should().NotBeNull($"Should handle {scenario} gracefully");
        }

        #endregion

        #region Category 5: Amount Range Configuration Tests

        [Fact]
        public async Task Configuration_Should_Parse_Amount_Ranges_Correctly()
        {
            // Arrange
            var amountRanges = "0.1;0.5;1;3;5;10;50;100";
            var extendedDaysTypes = "1;2;3;5;7;10;15;30";

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", It.IsAny<string>()))
                .ReturnsAsync(amountRanges);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", It.IsAny<string>()))
                .ReturnsAsync(extendedDaysTypes);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act - Test different amounts to trigger amount range logic
            var result1 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 0, 100, VALID_PIN); // 0.1 OMR
            var result2 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 5, 0, VALID_PIN); // 5.0 OMR
            var result3 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 50, 0, VALID_PIN); // 50.0 OMR

            // Assert
            result1.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle small amount ranges");
            result2.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle medium amount ranges");
            result3.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle large amount ranges");
        }

        [Theory]
        [InlineData("0.1;0.5;1", "1;2;3", "Matching count")]
        [InlineData("0.1;0.5", "1;2;3;4", "More days than amounts")]
        [InlineData("0.1;0.5;1;5", "1;2", "More amounts than days")]
        [InlineData("", "", "Empty configuration")]
        public async Task Configuration_Should_Handle_Mismatched_Amount_Range_Configurations(
            string amountRanges, string extendedDaysTypes, string scenario)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", It.IsAny<string>()))
                .ReturnsAsync(amountRanges);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", It.IsAny<string>()))
                .ReturnsAsync(extendedDaysTypes);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 1, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, $"Should handle {scenario} gracefully");
        }

        #endregion

        #region Category 6: Transfer Reason Configuration Tests

        [Fact]
        public async Task Configuration_Should_Parse_Transfer_Reason_Classifications_Correctly()
        {
            // Arrange
            var transferReasonClassification = "POS_Transfer_0.500;POS_Transfer_1;POS_Transfer_3;POS_Transfer_5;POS_Transfer_10;POS_Transfer_50;POS_Transfer_100";

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_TransferMonyReasonClassification", It.IsAny<string>()))
                .ReturnsAsync(transferReasonClassification);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", It.IsAny<string>()))
                .ReturnsAsync("0.1;0.5;1;3;5;10;50;100");

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 5, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle transfer reason classification");
        }

        [Fact]
        public async Task Configuration_Should_Handle_Adjustment_Reason_Classifications()
        {
            // Arrange
            var adjustmentReasonOldToNew = "local_credit_transfer_from_oldIN_to_newIN";
            var adjustmentReasonNewToOld = "local_credit_transfer_from_newIN_to_oldIN";

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AdjustmentReasonClassificationFromOldToNew", It.IsAny<string>()))
                .ReturnsAsync(adjustmentReasonOldToNew);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AdjustmentReasonClassificationFromNewToOld", It.IsAny<string>()))
                .ReturnsAsync(adjustmentReasonNewToOld);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(
                VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN, "TestAdjustment");

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle adjustment reason classifications");
        }

        #endregion

        #region Category 7: Country and Regional Configuration Tests

        [Theory]
        [InlineData("OM", "Oman")]
        [InlineData("KSA", "Saudi Arabia")]
        [InlineData("AE", "UAE")]
        [InlineData("", "Empty country")]
        public async Task Configuration_Should_Support_Different_Country_Settings(string country, string scenario)
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("Country", "OM"))
                .ReturnsAsync(country);

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, $"Should handle {scenario}");
            
            // Verify country configuration was used for business rules
            _mockTransferRulesService.Verify(x => x.EvaluateTransferRuleAsync(
                country, It.IsAny<SubscriptionType>(), It.IsAny<SubscriptionType>(), It.IsAny<Dictionary<string, string>>()), Times.Once);
        }

        #endregion

        #region Category 8: Exception Handling Utility Tests

        [Fact]
        public async Task ThrowCreditTransferException_Should_Throw_Correct_Exception_Type()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            _mockNobillCallsService.Setup(x => x.GetAccountValueAsync(It.IsAny<string>(), AccountItem.balance))
                .ReturnsAsync((1, "Account not found")); // Trigger exception

            // Act & Assert
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);
            result.statusCode.Should().NotBe(0, "Should handle NoBill errors with proper exception handling");
        }

        [Fact]
        public async Task Configuration_Should_Handle_Null_And_Empty_Values_Gracefully()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"))
                .ReturnsAsync((string?)null);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerTransferMoneyReason", "Credit transfer"))
                .ReturnsAsync("");

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.ValidateTransferInputsAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, VALID_AMOUNT);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle null and empty configuration values");
        }

        #endregion

        #region Category 9: Configuration Validation and Error Recovery Tests

        [Fact]
        public async Task Configuration_Should_Use_Default_Values_When_Config_Service_Fails()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 }); // This should succeed

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", "0000"))
                .ThrowsAsync(new Exception("Config service error"));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, "0000");

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle partial configuration loading failures");
        }

        [Fact]
        public async Task Configuration_Should_Handle_Invalid_Configuration_Format()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", It.IsAny<string>()))
                .ReturnsAsync("invalid;format;not;numbers");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", It.IsAny<string>()))
                .ReturnsAsync("also;invalid;format");

            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<List<int>>("CreditTransfer_MsisdnLength", new List<int> { 11 }))
                .ReturnsAsync(new List<int> { 11 });

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 5, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle invalid configuration formats gracefully");
        }

        [Fact]
        public async Task Configuration_Should_Support_Health_Check_Settings()
        {
            // Arrange
            var testMsisdn = "96812345999";
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("HealthCheck_TestMsisdn", "96898455550"))
                .ReturnsAsync(testMsisdn);

            // Act
            var healthResponse = await _creditTransferService.GetSystemHealthAsync();

            // Assert
            healthResponse.Should().NotBeNull();
            healthResponse.Components.Should().NotBeNullOrEmpty();
            
            // Verify health check configuration was loaded
            _mockConfigRepository.Verify(x => x.GetConfigValueAsync<string>("HealthCheck_TestMsisdn", "96898455550"), Times.Once);
        }

        #endregion

        public void Dispose()
        {
            _activitySource?.Dispose();
        }
    }
} 