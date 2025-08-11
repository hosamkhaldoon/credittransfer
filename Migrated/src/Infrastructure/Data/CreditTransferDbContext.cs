using CreditTransfer.Core.Domain.Entities;
using CreditTransfer.Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CreditTransfer.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework DbContext for Credit Transfer application
    /// Manages all database entities and relationships
    /// </summary>
    public class CreditTransferDbContext : DbContext
    {
        public CreditTransferDbContext(DbContextOptions<CreditTransferDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Application configurations stored in database
        /// </summary>
        public DbSet<ApplicationConfig> ApplicationConfigs { get; set; }

        /// <summary>
        /// Transfer configurations by subscription type
        /// </summary>
        public DbSet<TransferConfig> TransferConfig { get; set; }

        /// <summary>
        /// Credit transfer transactions
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// Bilingual message templates for SMS notifications
        /// </summary>
        public DbSet<Message> Messages { get; set; }

        /// <summary>
        /// Configurable transfer business rules
        /// </summary>
        public DbSet<TransferRule> TransferRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ApplicationConfig entity
            modelBuilder.Entity<ApplicationConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.CreatedBy).HasMaxLength(100);
                entity.Property(e => e.ModifiedBy).HasMaxLength(100);

                // Create unique index on Key
                entity.HasIndex(e => e.Key).IsUnique().HasDatabaseName("IX_ApplicationConfigs_Key");

                // Create index on Category
                entity.HasIndex(e => e.Category).HasDatabaseName("IX_ApplicationConfigs_Category");

                // Create index on IsActive
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_ApplicationConfigs_IsActive");
            });

            // Configure TransferConfig entity
            modelBuilder.Entity<TransferConfig>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.NobillSubscritpionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TransferCounterName).HasMaxLength(100);
                entity.Property(e => e.CreditTransferCustomerService).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SubscriptionType).HasMaxLength(50);
                entity.Property(e => e.MinTransferAmount).HasColumnType("decimal(18,3)");
                entity.Property(e => e.DailyTransferCapLimit).HasColumnType("decimal(19,4)");
                entity.Property(e => e.MaxTransferAmount).HasColumnType("decimal(18,3)");
                entity.Property(e => e.MinPostTransferBalance).HasColumnType("decimal(18,3)");

                // Create index on SubscriptionType
                entity.HasIndex(e => e.SubscriptionType).HasDatabaseName("IX_TransferConfigs_SubscriptionType");

            });

            // Configure Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                // Map to actual database table name (singular)
                entity.ToTable("Transaction");

                entity.HasKey(e => e.Id);

                // Map properties to actual database columns
                entity.Property(e => e.SourceMsisdn).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DestinationMsisdn).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Amount).HasColumnType("money"); // Use money type to match actual DB
                entity.Property(e => e.PIN).IsRequired().HasMaxLength(6); // Match actual DB length
                entity.Property(e => e.IsFromCustomer).IsRequired();
                entity.Property(e => e.IsEventReserved).IsRequired();
                entity.Property(e => e.IsAmountTransfered).IsRequired();
                entity.Property(e => e.IsEventCharged).IsRequired();
                entity.Property(e => e.IsEventCancelled).IsRequired();
                entity.Property(e => e.IsExpiryExtended).IsRequired();
                entity.Property(e => e.ExtensionDays).IsRequired();
                entity.Property(e => e.ReservationId).IsRequired();
                entity.Property(e => e.StatusId).IsRequired();
                entity.Property(e => e.NumberOfRetries).IsRequired();
                entity.Property(e => e.CreationDate).IsRequired();
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50); // Match actual DB length
                entity.Property(e => e.ModificationDate).IsRequired(false);
                entity.Property(e => e.ModifiedBy).HasMaxLength(50);

                // Additional properties for migration compatibility (not in original DB)
                entity.Property(e => e.TransferReason).HasMaxLength(200);
                entity.Property(e => e.AdjustmentReason).HasMaxLength(200);
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
                entity.Property(e => e.TransactionReference).HasMaxLength(100);
                entity.Property(e => e.CompletedDate).IsRequired(false);

                // Ignore the backwards compatibility property
                entity.Ignore(e => e.CreatedDate);

                // Create indexes for performance (updating index names to match table name)
                entity.HasIndex(e => e.SourceMsisdn).HasDatabaseName("IX_Transaction_SourceMsisdn");
                entity.HasIndex(e => e.DestinationMsisdn).HasDatabaseName("IX_Transaction_DestinationMsisdn");
                entity.HasIndex(e => e.CreationDate).HasDatabaseName("IX_Transaction_CreationDate");
                entity.HasIndex(e => e.StatusId).HasDatabaseName("IX_Transaction_StatusId");
            });

            // Configure Message entity
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TextEn).IsRequired();
                entity.Property(e => e.TextAr).IsRequired();

                // Create unique index on Key
                entity.HasIndex(e => e.Key).IsUnique().HasDatabaseName("IX_Messages_Key");

                // Create index on IsActive
                entity.HasIndex(e => e.IsActive).HasDatabaseName("IX_Messages_IsActive");
            });

            // Configure TransferRule entity
            modelBuilder.Entity<TransferRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(10).HasDefaultValue("OM");
                entity.Property(e => e.SourceSubscriptionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DestinationSubscriptionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.IsAllowed).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.ErrorCode).IsRequired(false);
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
                entity.Property(e => e.RequiresConfiguration).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.ConfigurationKey).HasMaxLength(100);
                entity.Property(e => e.ConfigurationValue).HasMaxLength(100);
                entity.Property(e => e.Priority).IsRequired().HasDefaultValue(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100).HasDefaultValue("System");
                entity.Property(e => e.ModifiedBy).IsRequired().HasMaxLength(100).HasDefaultValue("System");

                // Create indexes for performance
                entity.HasIndex(e => new { e.Country, e.IsActive }).HasDatabaseName("IX_TransferRules_Country_Active");
                entity.HasIndex(e => new { e.SourceSubscriptionType, e.DestinationSubscriptionType }).HasDatabaseName("IX_TransferRules_SourceDest");
                entity.HasIndex(e => e.Priority).HasDatabaseName("IX_TransferRules_Priority");
            });

            // Seed initial data
            SeedApplicationConfigs(modelBuilder);
            SeedTransferConfigs(modelBuilder);
            SeedMessages(modelBuilder);
        }

        /// <summary>
        /// Seeds initial ApplicationConfig data with values from original Web.config
        /// </summary>
        private void SeedApplicationConfigs(ModelBuilder modelBuilder)
        {
            var configs = new List<ApplicationConfig>
            {
                // Business Configuration (with CreditTransfer_ prefix)
                new ApplicationConfig { Id = 1, Key = "CreditTransfer_CurrencyUnitConversion", Value = "1000", Category = "Business", Description = "Currency unit conversion factor", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 2, Key = "CreditTransfer_MsisdnLength", Value = "11", Category = "Validation", Description = "Expected MSISDN length", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 3, Key = "CreditTransfer_RefillPinLength", Value = "4", Category = "Validation", Description = "Expected PIN length", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 4, Key = "CreditTransfer_TransferFundEventId", Value = "102", Category = "Business", Description = "Event ID for transfer fund operations", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // Transfer Amounts and Ranges
                new ApplicationConfig { Id = 5, Key = "CreditTransfer_AmountRanges", Value = "0.1;0.5;1;3;5;10;50;100", Category = "Business", Description = "Available transfer amount ranges", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 6, Key = "CreditTransfer_ExtendedDaysTypes", Value = "0;0;0;0;0;0;0", Category = "Business", Description = "Extended days for each amount range", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // Reason Classifications
                new ApplicationConfig { Id = 7, Key = "CreditTransfer_TransferMonyReasonClassification", Value = "POS_Transfer_0.500;POS_Transfer_0.500;POS_Transfer_1;POS_Transfer_3;POS_Transfer_5;POS_Transfer_10;POS_Transfer_50;POS_Transfer_100", Category = "Business", Description = "Transfer money reason classification", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 8, Key = "CreditTransfer_AdjustmentReasonClassificationFromOldToNew", Value = "POS_Transfer_0.500_from_oldIN_to_newIN;POS_Transfer_0.500_from_oldIN_to_newIN;POS_Transfer_1_from_oldIN_to_newIN;POS_Transfer_3_from_oldIN_to_newIN;POS_Transfer_5_from_oldIN_to_newIN;POS_Transfer_10_from_oldIN_to_newIN;POS_Transfer_50_from_oldIN_to_newIN;POS_Transfer_100_from_oldIN_to_newIN", Category = "Business", Description = "Adjustment reason for old to new transfers", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 9, Key = "CreditTransfer_AdjustmentReasonClassificationFromNewToOld", Value = "POS_Transfer_0.500_from_newIN_to_oldIN;POS_Transfer_0.500_from_newIN_to_oldIN;POS_Transfer_1_from_newIN_to_oldIN;POS_Transfer_3_from_newIN_to_oldIN;POS_Transfer_5_from_newIN_to_oldIN;POS_Transfer_10_from_newIN_to_oldIN;POS_Transfer_50_from_newIN_to_oldIN;POS_Transfer_100_from_newIN_to_oldIN", Category = "Business", Description = "Adjustment reason for new to old transfers", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // Customer Transfer Configuration
                new ApplicationConfig { Id = 10, Key = "CreditTransfer_CustomerToCustomerTransferMonyReason", Value = "Credit transfer", Category = "Business", Description = "Reason for customer to customer transfers", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 11, Key = "CreditTransfer_CustomerToCustomerAdjustmentReasonOldToNew", Value = "local_credit_transfer_from_oldIN_to_newIN", Category = "Business", Description = "Customer adjustment reason old to new", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 12, Key = "CreditTransfer_CustomerToCustomerAdjustmentReasonNewToOld", Value = "local_credit_transfer_from_newIN_to_oldIN", Category = "Business", Description = "Customer adjustment reason new to old", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // Transfer Limits
                new ApplicationConfig { Id = 13, Key = "CreditTransfer_POSMinTransferAmounts", Value = "5", Category = "Business", Description = "Minimum transfer amount for POS", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 14, Key = "CreditTransfer_DistributorMinTransferAmounts", Value = "5", Category = "Business", Description = "Minimum transfer amount for distributors", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 15, Key = "CreditTransfer_CustomerMinTransferAmounts", Value = "1", Category = "Business", Description = "Minimum transfer amount for customers", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // System Configuration
                new ApplicationConfig { Id = 16, Key = "CreditTransfer_MaxNumberOfRetries", Value = "10", Category = "System", Description = "Maximum number of retry attempts", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 17, Key = "CreditTransfer_DistributorTotalNumberOfTransfers", Value = "5", Category = "Business", Description = "Total number of transfers for distributors", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 18, Key = "CreditTransfer_CustomerTotalNumberOfTransfers", Value = "5", Category = "Business", Description = "Total number of transfers for customers", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 19, Key = "CreditTransfer_MinAmountAfterTransfer", Value = "3", Category = "Business", Description = "Minimum amount after transfer", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 20, Key = "CreditTransfer_AdjustmentType", Value = "account_adjustment_action", Category = "Business", Description = "Type of adjustment action", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 21, Key = "CreditTransfer_shortCodeSMS", Value = "300646", Category = "SMS", Description = "SMS short code", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 22, Key = "CreditTransfer_TransactionExpirationPeriodInHours", Value = "24", Category = "Business", Description = "Transaction expiration period in hours", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // Virgin Event Configuration  
                new ApplicationConfig { Id = 23, Key = "CreditTransfer_VirginEventIds", Value = "5|1005,10|1010,15|1015,20|1020,25|1025,30|1030,35|1035,40|1040,45|1045,50|1050", Category = "Business", Description = "Virgin event IDs mapping", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 24, Key = "CreditTransfer_FailedPaymentsFilePath", Value = "D:\\CreditTransferLogs\\", Category = "System", Description = "Path for failed payments log files", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 25, Key = "CreditTransfer_EnableExtendedDays", Value = "false", Category = "Business", Description = "Enable extended days (KSA) or disabled (Jordan)", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },

                // Additional Configuration (missing from original seeding but present in DB)
                new ApplicationConfig { Id = 26, Key = "Country", Value = "OM", Category = "System", Description = "Country code for transfer operations", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 27, Key = "CreditTransfer_DefaultPIN", Value = "0000", Category = "Security", Description = "Default PIN for customer service transfers", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 28, Key = "CreditTransfer_MaximumPercentageAmount", Value = "1.0", Category = "Business", Description = "Maximum percentage of balance that can be transferred", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 29, Key = "CreditTransfer_CustomerToCustomerTransferMoneyReason", Value = "Credit transfer", Category = "Business", Description = "Default reason for customer to customer transfers", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 30, Key = "CreditTransfer_SubscriptionTypes", Value = "friendi-2,TouristSim", Category = "Business", Description = "Subscription types for IN validation", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // SMS Templates (missing from original seeding but referenced in code)
                new ApplicationConfig { Id = 31, Key = "CreditTransfer_APartySMSEn", Value = "You have successfully transferred {0} RO to {1}.", Category = "SMS", Description = "English SMS template for sender", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 32, Key = "CreditTransfer_APartySMSAr", Value = "تم تحويل {0} ر.ع بنجاح إلى الرقم {1}", Category = "SMS", Description = "Arabic SMS template for sender", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 33, Key = "CreditTransfer_BPartySMSEn", Value = "You have received {0} RO from {1}", Category = "SMS", Description = "English SMS template for receiver", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 34, Key = "CreditTransfer_BPartySMSAr", Value = "لقد استلمت {0} ريال عماني من {1}", Category = "SMS", Description = "Arabic SMS template for receiver", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },

                // NobillCalls Service Configuration (starting from ID 400)
                new ApplicationConfig { Id = 400, Key = "NobillCalls_ServiceUrl", Value = "http://10.1.132.98/NobillProxy.UAT/NobillCalls.asmx", Category = "NobillCalls", Description = "NobillCalls web service endpoint URL", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 401, Key = "NobillCalls_UserName", Value = "transfercredit", Category = "NobillCalls", Description = "NobillCalls web service username", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 402, Key = "NobillCalls_Password", Value = "p@ssw0rd", Category = "NobillCalls", Description = "NobillCalls web service password", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 403, Key = "NobillCalls_TimeoutSeconds", Value = "30", Category = "NobillCalls", Description = "NobillCalls web service timeout in seconds", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 404, Key = "NobillCalls_RetryCount", Value = "3", Category = "NobillCalls", Description = "NobillCalls web service retry count on failure", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 405, Key = "NobillCalls_EnableLogging", Value = "true", Category = "NobillCalls", Description = "Enable detailed logging for NobillCalls operations", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                
                // Error Messages (starting from ID 500)
                new ApplicationConfig { Id = 500, Key = "ErrorMessage_UnknownSubscriber", Value = "Unknown Subscriber", Category = "ErrorMessages", Description = "Error message for code 2 - Unknown Subscriber", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 501, Key = "ErrorMessage_SourceDestinationSame", Value = "A-party and B-party phone numbers are same", Category = "ErrorMessages", Description = "Error message for code 3 - Same source and destination", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 502, Key = "ErrorMessage_InvalidPin", Value = "Invalid credit transfer password", Category = "ErrorMessages", Description = "Error message for code 4 - Invalid PIN", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 503, Key = "ErrorMessage_TransferAmountBelowMin", Value = "Amount requested is less than the minimum transferrable amount by A-party", Category = "ErrorMessages", Description = "Error message for code 5 - Amount below minimum", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 504, Key = "ErrorMessage_TransferAmountAboveMax", Value = "Amount requested is more than the maximum amount that can be transferred by the A-party", Category = "ErrorMessages", Description = "Error message for code 7 - Amount above maximum", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 505, Key = "ErrorMessage_MiscellaneousError", Value = "Miscellaneous error", Category = "ErrorMessages", Description = "Error message for code 14 - General error", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 506, Key = "ErrorMessage_InvalidSourcePhone", Value = "Invalid Source Phone Number", Category = "ErrorMessages", Description = "Error message for code 20 - Invalid source phone", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 507, Key = "ErrorMessage_InvalidDestinationPhone", Value = "Invalid Destination Phone Number", Category = "ErrorMessages", Description = "Error message for code 21 - Invalid destination phone", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 508, Key = "ErrorMessage_InvalidPinCode", Value = "Invalid PIN", Category = "ErrorMessages", Description = "Error message for code 22 - Invalid PIN code", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 509, Key = "ErrorMessage_InsufficientCredit", Value = "Insufficient Credit", Category = "ErrorMessages", Description = "Error message for code 23 - Insufficient balance", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 510, Key = "ErrorMessage_SubscriptionNotFound", Value = "Subscription Not Found", Category = "ErrorMessages", Description = "Error message for code 24 - Subscription not found", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 511, Key = "ErrorMessage_ConcurrentUpdateDetected", Value = "Concurrent Update Detected", Category = "ErrorMessages", Description = "Error message for code 25 - Concurrent update", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 512, Key = "ErrorMessage_SourceSubscriptionNotFound", Value = "Source Subscription Not Found", Category = "ErrorMessages", Description = "Error message for code 26 - Source subscription not found", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 513, Key = "ErrorMessage_DestinationSubscriptionNotFound", Value = "Destination Subscription Not Found", Category = "ErrorMessages", Description = "Error message for code 27 - Destination subscription not found", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 514, Key = "ErrorMessage_UserNotAllowed", Value = "User Not Allowed To Call This Method", Category = "ErrorMessages", Description = "Error message for code 28 - User not authorized", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 515, Key = "ErrorMessage_ConfigurationError", Value = "Configuration Error", Category = "ErrorMessages", Description = "Error message for code 29 - Configuration error", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 516, Key = "ErrorMessage_PropertyNotFound", Value = "Property Not Found", Category = "ErrorMessages", Description = "Error message for code 30 - Property not found", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 517, Key = "ErrorMessage_ExpiredReservationCode", Value = "Expired Reservation Code", Category = "ErrorMessages", Description = "Error message for code 31 - Expired reservation", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 518, Key = "ErrorMessage_BadRequest", Value = "Bad Request", Category = "ErrorMessages", Description = "Error message for code 32 - Bad request", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 519, Key = "ErrorMessage_TransferNotAllowed", Value = "Credit transfer is not allowed to this such account type", Category = "ErrorMessages", Description = "Error message for code 33 - Transfer not allowed for account type", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 520, Key = "ErrorMessage_ExceedsMaxPerDay", Value = "Subscription has been reached the number of transfers per day", Category = "ErrorMessages", Description = "Error message for code 34 - Daily transfer limit exceeded", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 521, Key = "ErrorMessage_RemainingBalance", Value = "Insufficient Remaining Credit", Category = "ErrorMessages", Description = "Error message for code 35 - Insufficient remaining balance", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 522, Key = "ErrorMessage_InvalidTransferAmount", Value = "Transfer amount should be a multiple of 5", Category = "ErrorMessages", Description = "Error message for code 36 - Invalid transfer amount", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 523, Key = "ErrorMessage_SmsError", Value = "An error occurred while sending sms to the destination msisdn", Category = "ErrorMessages", Description = "Error message for code 37 - SMS sending error", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 524, Key = "ErrorMessage_ReserveAmountError", Value = "An error occurred while reserve amount", Category = "ErrorMessages", Description = "Error message for code 38 - Amount reservation error", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 525, Key = "ErrorMessage_CreditAmountError", Value = "Failed to credit the amount to the source msisdn", Category = "ErrorMessages", Description = "Error message for code 39 - Credit amount error", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 526, Key = "ErrorMessage_BalancePercentageError", Value = "Your balance remaining should be more than 50%", Category = "ErrorMessages", Description = "Error message for code 40 - Balance percentage error", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 527, Key = "ErrorMessage_CreditTransferServiceIsBlocked", Value = "Credit transfer service is blocked", Category = "ErrorMessages", Description = "Error message for code 41 - Credit transfer service is blocked", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 528, Key = "ErrorMessage_OCSTimeout", Value = "OCS timeout occurred", Category = "ErrorMessages", Description = "Error message for code 42 - OCS timeout", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 529, Key = "ErrorMessage_ExceedsMaxCapPerDay", Value = "Subscription has been reached the cap of transfers per day", Category = "ErrorMessages", Description = "Error message for code 43 - ExceedsMaxCapPerDay", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" },
                new ApplicationConfig { Id = 531, Key = "Message_Success", Value = "OK", Category = "Messages", Description = "message for code 0 - Success", IsActive = true, CreatedDate = DateTime.UtcNow, CreatedBy = "System" }

            };

            modelBuilder.Entity<ApplicationConfig>().HasData(configs);
        }

        /// <summary>
        /// Seeds TransferConfig data with actual production values
        /// </summary>
        private void SeedTransferConfigs(ModelBuilder modelBuilder)
        {
            var transferConfigs = new List<TransferConfig>
            {
                new TransferConfig { ID = 1, NobillSubscritpionType = "FRiENDi - B", TransferCounterName = null, CreditTransferCustomerService = "CS.Credit_transfer", DailyTransferCountLimit = null, MinTransferAmount = 0.0100m, TransferFeesEventId = 0, MinPostTransferBalance = null, MaxTransferAmount = null, SubscriptionType = "Customer", TransferToOtherOperator = null },
                new TransferConfig { ID = 2, NobillSubscritpionType = "dealer", TransferCounterName = null, CreditTransferCustomerService = "CS.Credit_transfer", DailyTransferCountLimit = null, MinTransferAmount = 0.1000m, TransferFeesEventId = 0, MinPostTransferBalance = null, MaxTransferAmount = null, SubscriptionType = "POS", TransferToOtherOperator = null },
                new TransferConfig { ID = 3, NobillSubscritpionType = "Halafoni", TransferCounterName = null, CreditTransferCustomerService = "CS.Credit_transfer", DailyTransferCountLimit = null, MinTransferAmount = 0.0100m, TransferFeesEventId = 0, MinPostTransferBalance = null, MaxTransferAmount = null, SubscriptionType = "HalafoniCustomer", TransferToOtherOperator = null },
                new TransferConfig { ID = 4, NobillSubscritpionType = "FRIENDI-2", TransferCounterName = null, CreditTransferCustomerService = "CS.Credit_transfer", DailyTransferCountLimit = null, MinTransferAmount = 0.0100m, TransferFeesEventId = 0, MinPostTransferBalance = null, MaxTransferAmount = null, SubscriptionType = "Customer", TransferToOtherOperator = null },
                new TransferConfig { ID = 5, NobillSubscritpionType = "FRiENDi_OO_Test", TransferCounterName = null, CreditTransferCustomerService = "CS.Credit_transfer", DailyTransferCountLimit = null, MinTransferAmount = 0.0100m, TransferFeesEventId = 0, MinPostTransferBalance = null, MaxTransferAmount = null, SubscriptionType = "Customer", TransferToOtherOperator = null },
                new TransferConfig { ID = 6, NobillSubscritpionType = "Tourist", TransferCounterName = null, CreditTransferCustomerService = "CS.Credit_transfer", DailyTransferCountLimit = null, MinTransferAmount = 0.0100m, TransferFeesEventId = 0, MinPostTransferBalance = null, MaxTransferAmount = null, SubscriptionType = "Customer", TransferToOtherOperator = null },
                new TransferConfig { ID = 7, NobillSubscritpionType = "FRIENDI-3", TransferCounterName = null, CreditTransferCustomerService = "CS.Credit_transfer", DailyTransferCountLimit = null, MinTransferAmount = 0.0100m, TransferFeesEventId = 0, MinPostTransferBalance = null, MaxTransferAmount = null, SubscriptionType = "Customer", TransferToOtherOperator = null }
            };

            modelBuilder.Entity<TransferConfig>().HasData(transferConfigs);
        }

        /// <summary>
        /// Seeds Messages data with bilingual SMS templates
        /// </summary>
        private void SeedMessages(ModelBuilder modelBuilder)
        {
            var messages = new List<Message>
            {
                new Message { Id = 7, Key = "CreditTransferSuccessMsgA", TextEn = "Thank you. OMR {0} has been successfully transferred to {1}. Your new balance is OMR {2}.", TextAr = "لقد تم تحويل {0} ريال الى الرقم {1} بنجاح. رصيدك الحالي {2} ريال", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 8, Key = "CreditTransferSuccessMsgB", TextEn = "SAR {0} has been successfully received from {1}. Your new balance is  SAR {2}.", TextAr = "لقد استلمت {0} ريال من الرقم {1}. رصيدك الحالي {2} ريال", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 9, Key = "TransactionExpired", TextEn = "Your transaction has been expired.", TextAr = "نأسف، لقد انتهت الفترة المحددة لاتمام عملية تحويل الرصيد", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 10, Key = "TransactionNotFound", TextEn = "Transaction not found.", TextAr = "لايوجد لديك عملية تحويل رصيد حالياً", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 11, Key = "VirginCreditTransferSMS", TextEn = "Enter Your ID / Iqama number, {0} wants to transfer the amount OMR {1}.", TextAr = "الرقم {0} يريد تحويل {1} ريال الى حسابك. الرجاء الرد على هذه الرسالة برقم الهوية أو رقم الإقامة", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 12, Key = "WrongIdNumber", TextEn = "The ID number you entered is incorrect. Please re-enter the correct ID number.", TextAr = "رقم الهوية أو رقم الإقامة غير صحيح. الرجاء المحاولة مرة أخرى", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 13, Key = "AdjustAccountFailure", TextEn = "Failed to credit the balance ", TextAr = "نأسف، لقد فشلت عملية تحويل الرصيد. الرجاء المحاولة مرة أخرى", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 14, Key = "MoneyBack", TextEn = "OMR {0} has been reverted back to your balance due to credit transfer to {1} expiration period.", TextAr = "عملية تحويل {0} ريال الى الرقم {1} فشلت وتم إعادة {0} ريال الى حسابك", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 17, Key = "NobillComment", TextEn = "comment \nFund transfer detail: {2} OMR from phoneno {0} to phoneno {1} ", TextAr = "comment \nFund transfer detail: {2} OMR from phoneno {0} to phoneno {1} ", IsActive = true, CreatedDate = DateTime.UtcNow },
                new Message { Id = 18, Key = "PostpaidCreditTransferSuccessMsgA", TextEn = "Thank you. OMR  {0} has been successfully transferred to {1}. ", TextAr = "لقد تم تحويل {0} ريال الى الرقم {1} بنجاح.", IsActive = true, CreatedDate = DateTime.UtcNow }
            };

            modelBuilder.Entity<Message>().HasData(messages);
        }
    }
}
