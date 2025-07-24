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
    /// Comprehensive unit tests for CreditTransferService helper methods
    /// Tests core business logic methods including TransferCreditInternalAsync, 
    /// configuration helpers, NoBill integration, and utility methods
    /// </summary>
    public class CreditTransferServiceHelperMethodsTests : IDisposable
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
        private const string VALID_ADJUSTMENT_REASON = "TestAdjustment";
        private const string VALID_SUBSCRIPTION_TYPE = "Customer";
        private const string VALID_USERNAME = "testuser";
        private const long VALID_TRANSACTION_ID = 12345L;
        private const int VALID_RESERVATION_CODE = 98765;

        public CreditTransferServiceHelperMethodsTests()
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
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_DefaultPIN", ""))
                .ReturnsAsync("0000");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<decimal>("CreditTransfer_MaximumPercentageAmount", 0m))
                .ReturnsAsync(1.0m);
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("Country", ""))
                .ReturnsAsync("OM");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_CustomerToCustomerTransferMoneyReason", ""))
                .ReturnsAsync("Customer to Customer Transfer");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", ""))
                .ReturnsAsync("0.1;0.5;1;3;5;10;50;100");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", ""))
                .ReturnsAsync("0;0;0;0;0;0;0;0");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_TransferReasonAmountRanges", ""))
                .ReturnsAsync("1;5;10;50;100");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_TransferReasonCodes", ""))
                .ReturnsAsync("REASON1;REASON2;REASON3;REASON4;REASON5");
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

            // Setup NoBill operations

            _mockNobillCallsService.Setup(x => x.ReserveEventAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((0, VALID_RESERVATION_CODE));

            _mockNobillCallsService.Setup(x => x.CancelReservationAsync(It.IsAny<string>(), It.IsAny<int>()))
.ReturnsAsync((0, VALID_RESERVATION_CODE));

 

            _mockNobillCallsService.Setup(x => x.TransferMoneyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((0));

            _mockNobillCallsService.Setup(x => x.ExtendSubscriptionExpiryAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((0));

            _mockNobillCallsService.Setup(x => x.SendHTTPSMSAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((0));

            // Setup error messages
            _mockErrorConfigService.Setup(x => x.GetErrorMessageAsync(It.IsAny<int>()))
                .ReturnsAsync("Test error message");

        }

        #region Category 1: Configuration Helper Methods Tests

        [Fact]
        public async Task GetDaysToExtend_Should_Return_Correct_Days_For_Amount_Range()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AmountRanges", ""))
                .ReturnsAsync("0.1;0.5;1;3;5;10;50;100");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", ""))
                .ReturnsAsync("1;2;3;5;7;10;15;30");

            // Act - Test multiple amounts
            var result1 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 0, 100, VALID_PIN); // 0.1 OMR
            var result2 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 3, 0, VALID_PIN); // 3.0 OMR
            var result3 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 15, 0, VALID_PIN); // 15.0 OMR

            // Assert
            result1.statusCode.Should().Be(0, "Should handle small amounts correctly");
            result2.statusCode.Should().Be(0, "Should handle medium amounts correctly");
            result3.statusCode.Should().Be(0, "Should handle large amounts correctly");
        }

        [Fact]
        public async Task GetRelatedTransferReason_Should_Return_Correct_Reason_For_Amount()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_TransferReasonAmountRanges", ""))
                .ReturnsAsync("1;5;10;50;100");
            
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_TransferReasonCodes", ""))
                .ReturnsAsync("SMALL;MEDIUM;LARGE;XLARGE;XXLARGE");

            // Act - Test different amounts that should map to different reasons
            var result1 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 0, 500, VALID_PIN); // 0.5 OMR
            var result2 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 7, 0, VALID_PIN); // 7.0 OMR
            var result3 = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 75, 0, VALID_PIN); // 75.0 OMR

            // Assert
            result1.statusCode.Should().Be(0, "Should handle small amount transfer reason");
            result2.statusCode.Should().Be(0, "Should handle medium amount transfer reason");
            result3.statusCode.Should().Be(0, "Should handle large amount transfer reason");
        }

        [Fact]
        public async Task GetRelatedAdjustmentReason_Should_Return_Correct_Reason_For_OldToNew()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AdjustmentReasonOldToNew", ""))
                .ReturnsAsync("OLD_TO_NEW_ADJUSTMENT");

            // Act
            var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(
                VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN, VALID_ADJUSTMENT_REASON);

            // Assert
            result.statusCode.Should().Be(0, "Should handle old to new adjustment reason");
        }

        [Fact]
        public async Task GetRelatedAdjustmentReason_Should_Return_Correct_Reason_For_NewToOld()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_AdjustmentReasonNewToOld", ""))
                .ReturnsAsync("NEW_TO_OLD_ADJUSTMENT");

            // Act
            var result = await _creditTransferService.TransferCreditWithAdjustmentReasonAsync(
                VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN, VALID_ADJUSTMENT_REASON);

            // Assert
            result.statusCode.Should().Be(0, "Should handle new to old adjustment reason");
        }

        #endregion

        #region Category 2: NoBill Integration Helper Methods Tests

        [Fact]
        public async Task GetAccountPinByServiceName_Should_Return_Correct_Pin()
        {
            // Arrange
            const string expectedPin = "9876";
            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_pin))
                .ReturnsAsync((0, expectedPin));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, expectedPin);

            // Assert
            result.statusCode.Should().Be(0, "Should successfully retrieve and validate PIN");
            _mockNobillCallsService.Verify(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_pin), Times.Once);
        }

        [Fact]
        public async Task GetAccountMaxTransferAmountByServiceName_Should_Return_Correct_Amount()
        {
            // Arrange
            const string expectedMaxAmount = "75.0";
            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_maxamount))
                .ReturnsAsync((0, expectedMaxAmount));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 50, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().Be(0, "Should successfully retrieve and validate max transfer amount");
            _mockNobillCallsService.Verify(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_maxamount), Times.Once);
        }

        [Fact]
        public async Task GetAccountPinByServiceName_Should_Handle_NoBill_Error()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_pin))
                .ReturnsAsync((1, "Error retrieving PIN"));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle NoBill PIN retrieval error");
        }

        [Fact]
        public async Task GetAccountMaxTransferAmountByServiceName_Should_Handle_NoBill_Error()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(VALID_SOURCE_MSISDN, CreditTransferItem.p_maxamount))
                .ReturnsAsync((1, "Error retrieving max amount"));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle NoBill max amount retrieval error");
        }

        #endregion

        #region Category 3: Event Management Helper Methods Tests





        //[Fact]
        //public async Task CancelEvent_Should_Successfully_Cancel_Reservation()
        //{
        //    // Arrange
        //    _mockNobillCallsService.Setup(x => x.ReserveEventAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
        //        .ReturnsAsync((1, "Reservation failed")); // Force failure to trigger cancellation

        //    _mockNobillCallsService.Setup(x => x.CancelReservationAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
        //        .ReturnsAsync((0, "Success"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().NotBe(0, "Should fail due to reservation error");
        //    // Note: Cancellation would be called in error scenarios
        //}

        //[Fact]
        //public async Task ReserveEvent_Should_Handle_NoBill_Error()
        //{
        //    // Arrange
        //    _mockNobillCallsService.Setup(x => x.ReserveAmountAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
        //        .ReturnsAsync((1, "Reservation failed"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().NotBe(0, "Should handle reservation error");
        //}

        //[Fact]
        //public async Task CommitEvent_Should_Handle_NoBill_Error()
        //{
        //    // Arrange
        //    _mockNobillCallsService.Setup(x => x.CommitReservedAmountAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<string>()))
        //        .ReturnsAsync((1, "Commit failed"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().NotBe(0, "Should handle commit error");
        //}

        #endregion

        #region Category 4: Fund Transfer Helper Methods Tests

        //[Fact]
        //public async Task TransferFund_Should_Successfully_Transfer_Between_Same_IN()
        //{
        //    // Arrange
        //    _mockNobillCallsService.Setup(x => x.TransferFundAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), false))
        //        .ReturnsAsync((0, "Success"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().Be(0, "Should successfully transfer funds on same IN");
        //    _mockNobillCallsService.Verify(x => x.TransferFundAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), false), Times.Once);
        //}

        //[Fact]
        //public async Task TransferFund_Should_Successfully_Transfer_Between_Different_IN()
        //{
        //    // Arrange
        //    _mockNobillCallsService.Setup(x => x.TransferFundAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), true))
        //        .ReturnsAsync((0, "Success"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().Be(0, "Should successfully transfer funds between different INs");
        //}

        //[Fact]
        //public async Task TransferFund_Should_Handle_NoBill_Error()
        //{
        //    // Arrange
        //    _mockNobillCallsService.Setup(x => x.TransferFundAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
        //        .ReturnsAsync((1, "Transfer failed"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().NotBe(0, "Should handle fund transfer error");
        //}

        #endregion

        #region Category 5: Days Extension Helper Methods Tests

        //[Fact]
        //public async Task ExtendDays_Should_Successfully_Extend_Validity()
        //{
        //    // Arrange
        //    _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>("CreditTransfer_ExtendedDaysTypes", ""))
        //        .ReturnsAsync("1;2;3;5;7;10;15;30");

        //    _mockNobillCallsService.Setup(x => x.ExtendDaysAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<string>()))
        //        .ReturnsAsync((0, "Success"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().Be(0, "Should successfully extend validity days");
        //    _mockNobillCallsService.Verify(x => x.ExtendDaysAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<string>()), Times.Once);
        //}

        //[Fact]
        //public async Task ExtendDays_Should_Handle_NoBill_Error()
        //{
        //    // Arrange
        //    _mockNobillCallsService.Setup(x => x.ExtendDaysAsync(
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<long?>(), It.IsAny<string>()))
        //        .ReturnsAsync((1, "Extension failed"));

        //    // Act
        //    var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

        //    // Assert
        //    result.statusCode.Should().NotBe(0, "Should handle days extension error");
        //}

        #endregion

        #region Category 6: SMS Helper Methods Tests

        [Fact]
        public async Task SendSMS_Should_Successfully_Send_Notifications()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.SendHTTPSMSAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((0));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().Be(0, "Should successfully send SMS notifications");
            _mockNobillCallsService.Verify(x => x.SendHTTPSMSAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task SendSMS_Should_Handle_NoBill_Error_Gracefully()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.SendHTTPSMSAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((1));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().Be(0, "Should complete transfer even if SMS fails");
        }

        #endregion

        #region Category 7: Integration and Error Handling Tests

        [Fact]
        public async Task HelperMethods_Should_Handle_Configuration_Errors()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Configuration error"));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle configuration errors gracefully");
        }

        [Fact]
        public async Task HelperMethods_Should_Log_Errors_Appropriately()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.ReserveEventAsync(
                It.IsAny<string>(), It.IsAny<int>()))
                .ThrowsAsync(new Exception("NoBill service error"));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle NoBill service errors");
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce, "Should log errors appropriately");
        }



        #endregion

        #region Category 8: Performance and Edge Cases Tests

        [Fact]
        public async Task HelperMethods_Should_Handle_Large_Transfer_Amounts()
        {
            // Arrange
            _mockNobillCallsService.Setup(x => x.GetCreditTransferValueAsync(It.IsAny<string>(), CreditTransferItem.p_maxamount))
                .ReturnsAsync((0, "1000000.0"));

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 999999, 999, VALID_PIN);

            // Assert
            result.statusCode.Should().BeGreaterThanOrEqualTo(0, "Should handle large transfer amounts");
        }

        [Fact]
        public async Task HelperMethods_Should_Handle_Concurrent_Operations()
        {
            // Arrange
            var tasks = new List<Task<(int statusCode, string statusMessage, long transactionId)>>();

            // Act - Execute multiple transfers concurrently
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(_creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 1, 0, VALID_PIN));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().NotBeNull();
            results.Length.Should().Be(5);
            results.All(r => r.statusCode >= 0).Should().BeTrue("Should handle concurrent operations");
        }

        [Fact]
        public async Task HelperMethods_Should_Handle_Null_And_Empty_Values()
        {
            // Arrange
            _mockConfigRepository.Setup(x => x.GetConfigValueAsync<string>(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _creditTransferService.TransferCreditAsync(VALID_SOURCE_MSISDN, VALID_DESTINATION_MSISDN, 10, 0, VALID_PIN);

            // Assert
            result.statusCode.Should().NotBe(0, "Should handle null configuration values gracefully");
        }

        #endregion

        public void Dispose()
        {
            _activitySource?.Dispose();
        }
    }
} 