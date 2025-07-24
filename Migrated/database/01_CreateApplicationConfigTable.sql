-- =============================================
-- Script: Create Database Tables and Seed Data
-- Description: Creates the ApplicationConfig and Messages tables for storing configuration and SMS templates
-- Author: .NET 8 Migration Tool
-- Date: Generated automatically
-- Note: Using external SQL Server at 10.1.133.31
-- =============================================

-- Note: This script assumes the CreditTransfer database already exists on the external SQL Server
-- Connection: Server=10.1.133.31;Database=CreditTransfer;User Id=sa;Password=fas@335$;TrustServerCertificate=true;

-- =============================================
-- Create ApplicationConfigs table
-- =============================================

-- Drop table if exists (for re-run scenarios)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationConfigs]') AND type in (N'U'))
DROP TABLE [dbo].[ApplicationConfigs]
GO

-- Create ApplicationConfigs table
CREATE TABLE [dbo].[ApplicationConfigs](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Key] [nvarchar](200) NOT NULL,
    [Value] [nvarchar](max) NOT NULL,
    [Description] [nvarchar](1000) NULL,
    [Note] [nvarchar](max) NULL,
    [Category] [nvarchar](100) NULL,
    [IsActive] [bit] NOT NULL DEFAULT 1,
    [CreatedDate] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
    [LastModified] [datetime2](7) NULL,
    [CreatedBy] [nvarchar](100) NULL,
    [ModifiedBy] [nvarchar](100) NULL,
    
    CONSTRAINT [PK_ApplicationConfigs] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
GO

-- Create unique index on Key
CREATE UNIQUE NONCLUSTERED INDEX [IX_ApplicationConfigs_Key] ON [dbo].[ApplicationConfigs]
(
    [Key] ASC
) ON [PRIMARY]
GO

-- Create index on Category
CREATE NONCLUSTERED INDEX [IX_ApplicationConfigs_Category] ON [dbo].[ApplicationConfigs]
(
    [Category] ASC
) ON [PRIMARY]
GO

-- Create index on IsActive
CREATE NONCLUSTERED INDEX [IX_ApplicationConfigs_IsActive] ON [dbo].[ApplicationConfigs]
(
    [IsActive] ASC
) ON [PRIMARY]
GO

-- =============================================
-- Create Messages table
-- =============================================

-- Drop table if exists (for re-run scenarios)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messages]') AND type in (N'U'))
DROP TABLE [dbo].[Messages]
GO

-- Create Messages table
CREATE TABLE [dbo].[Messages](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [Key] [nvarchar](100) NOT NULL,
    [TextEn] [nvarchar](max) NOT NULL,
    [TextAr] [nvarchar](max) NOT NULL,
    [IsActive] [bit] NOT NULL DEFAULT 1,
    [CreatedDate] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
    [LastModified] [datetime2](7) NULL,
    
    CONSTRAINT [PK_Messages] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]
GO

-- Create unique index on Key
CREATE UNIQUE NONCLUSTERED INDEX [IX_Messages_Key] ON [dbo].[Messages]
(
    [Key] ASC
) ON [PRIMARY]
GO

-- Create index on IsActive
CREATE NONCLUSTERED INDEX [IX_Messages_IsActive] ON [dbo].[Messages]
(
    [IsActive] ASC
) ON [PRIMARY]
GO

-- =============================================
-- Insert ApplicationConfig data
-- =============================================

-- Insert initial configuration data from original Web.config
INSERT INTO [dbo].[ApplicationConfigs] ([Key], [Value], [Category], [Description], [IsActive], [CreatedDate], [CreatedBy])
VALUES
    -- Business Configuration
    ('AuthenticatedUserGroup', 'CreditTransferGroup', 'Authentication', 'User group for authentication', 1, GETUTCDATE(), 'System'),
    ('CurrencyUnitConversion', '1000', 'Business', 'Currency unit conversion factor', 1, GETUTCDATE(), 'System'),
    ('MsisdnLength', '11', 'Validation', 'Expected MSISDN length', 1, GETUTCDATE(), 'System'),
    ('RefillPinLength', '4', 'Validation', 'Expected PIN length', 1, GETUTCDATE(), 'System'),
    ('TransferFundEventId', '102', 'Business', 'Event ID for transfer fund operations', 1, GETUTCDATE(), 'System'),
    
    -- Transfer Amounts and Ranges
    ('AmountRanges', '0.1;0.5;1;3;5;10;50;100', 'Business', 'Available transfer amount ranges', 1, GETUTCDATE(), 'System'),
    ('ExtendedDaysTypes', '0;0;0;0;0;0;0', 'Business', 'Extended days for each amount range', 1, GETUTCDATE(), 'System'),
    
    -- Reason Classifications
    ('TransferMonyReasonClassification', 'POS_Transfer_0.500;POS_Transfer_0.500;POS_Transfer_1;POS_Transfer_3;POS_Transfer_5;POS_Transfer_10;POS_Transfer_50;POS_Transfer_100', 'Business', 'Transfer money reason classification', 1, GETUTCDATE(), 'System'),
    ('AdjustmentReasonClassificationFromOldToNew', 'POS_Transfer_0.500_from_oldIN_to_newIN;POS_Transfer_0.500_from_oldIN_to_newIN;POS_Transfer_1_from_oldIN_to_newIN;POS_Transfer_3_from_oldIN_to_newIN;POS_Transfer_5_from_oldIN_to_newIN;POS_Transfer_10_from_oldIN_to_newIN;POS_Transfer_50_from_oldIN_to_newIN;POS_Transfer_100_from_oldIN_to_newIN', 'Business', 'Adjustment reason for old to new transfers', 1, GETUTCDATE(), 'System'),
    ('AdjustmentReasonClassificationFromNewToOld', 'POS_Transfer_0.500_from_newIN_to_oldIN;POS_Transfer_0.500_from_newIN_to_oldIN;POS_Transfer_1_from_newIN_to_oldIN;POS_Transfer_3_from_newIN_to_oldIN;POS_Transfer_5_from_newIN_to_oldIN;POS_Transfer_10_from_newIN_to_oldIN;POS_Transfer_50_from_newIN_to_oldIN;POS_Transfer_100_from_newIN_to_oldIN', 'Business', 'Adjustment reason for new to old transfers', 1, GETUTCDATE(), 'System'),
    
    -- Customer Transfer Configuration
    ('CustomerToCustomerTransferMonyReason', 'Credit transfer', 'Business', 'Reason for customer to customer transfers', 1, GETUTCDATE(), 'System'),
    ('CustomerToCustomerAdjustmentReasonOldToNew', 'local_credit_transfer_from_oldIN_to_newIN', 'Business', 'Customer adjustment reason old to new', 1, GETUTCDATE(), 'System'),
    ('CustomerToCustomerAdjustmentReasonNewToOld', 'local_credit_transfer_from_newIN_to_oldIN', 'Business', 'Customer adjustment reason new to old', 1, GETUTCDATE(), 'System'),
    
    -- NobillCalls Service Configuration
    ('NobillCalls__ServiceUrl', 'http://10.1.132.98/NobillProxy/NobillCalls.asmx', 'ExternalService', 'NobillCalls web service URL', 1, GETUTCDATE(), 'System'),
    ('NobillCalls__Username', 'transfercredit', 'ExternalService', 'NobillCalls service username', 1, GETUTCDATE(), 'System'),
    ('NobillCalls__Password', 'p@ssw0rd', 'ExternalService', 'NobillCalls service password', 1, GETUTCDATE(), 'System'),
    
    -- Transfer Limits
    ('POSMinTransferAmounts', '5', 'Business', 'Minimum transfer amount for POS', 1, GETUTCDATE(), 'System'),
    ('DistributorMinTransferAmounts', '5', 'Business', 'Minimum transfer amount for distributors', 1, GETUTCDATE(), 'System'),
    ('CustomerMinTransferAmounts', '1', 'Business', 'Minimum transfer amount for customers', 1, GETUTCDATE(), 'System'),
    
    -- System Configuration
    ('MaxNumberOfRetries', '10', 'System', 'Maximum number of retry attempts', 1, GETUTCDATE(), 'System'),
    ('DistributorTotalNumberOfTransfers', '5', 'Business', 'Total number of transfers for distributors', 1, GETUTCDATE(), 'System'),
    ('CustomerTotalNumberOfTransfers', '5', 'Business', 'Total number of transfers for customers', 1, GETUTCDATE(), 'System'),
    ('MinAmountAfterTransfer', '3', 'Business', 'Minimum amount after transfer', 1, GETUTCDATE(), 'System'),
    ('AdjustmentType', 'account_adjustment_action', 'Business', 'Type of adjustment action', 1, GETUTCDATE(), 'System'),
    ('shortCodeSMS', '300646', 'SMS', 'SMS short code', 1, GETUTCDATE(), 'System'),
    ('TransactionExpirationPeriodInHours', '24', 'Business', 'Transaction expiration period in hours', 1, GETUTCDATE(), 'System'),
    
    -- Virgin Event Configuration  
    ('VirginEventIds', '5|1005,10|1010,15|1015,20|1020,25|1025,30|1030,35|1035,40|1040,45|1045,50|1050', 'Business', 'Virgin event IDs mapping', 1, GETUTCDATE(), 'System'),
    ('FailedPaymentsFilePath', 'D:\CreditTransferLogs\', 'System', 'Path for failed payments log files', 1, GETUTCDATE(), 'System'),
    ('EnableExtendedDays', 'false', 'Business', 'Enable extended days (KSA) or disabled (Jordan)', 1, GETUTCDATE(), 'System'),
    
    -- Error Messages with Descriptive Keys (from original Web.config)
    ('ErrorMessage_UnknownSubscriber', 'Unknown Subscriber', 'ErrorMessages', 'Error message for code 2 - Unknown Subscriber', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_SourceDestinationSame', 'A-party and B-party phone numbers are same', 'ErrorMessages', 'Error message for code 3 - Same source and destination', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_InvalidPin', 'Invalid credit transfer password', 'ErrorMessages', 'Error message for code 4 - Invalid PIN', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_TransferAmountBelowMin', 'Amount requested is less than the minimum transferrable amount by A-party', 'ErrorMessages', 'Error message for code 5 - Amount below minimum', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_TransferAmountAboveMax', 'Amount requested is more than the maximum amount that can be transferred by the A-party', 'ErrorMessages', 'Error message for code 7 - Amount above maximum', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_MiscellaneousError', 'Miscellaneous error', 'ErrorMessages', 'Error message for code 14 - General error', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_InvalidSourcePhone', 'Invalid Source Phone Number', 'ErrorMessages', 'Error message for code 20 - Invalid source phone', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_InvalidDestinationPhone', 'Invalid Destination Phone Number', 'ErrorMessages', 'Error message for code 21 - Invalid destination phone', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_InvalidPinCode', 'Invalid PIN', 'ErrorMessages', 'Error message for code 22 - Invalid PIN code', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_InsufficientCredit', 'Insufficient Credit', 'ErrorMessages', 'Error message for code 23 - Insufficient balance', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_SubscriptionNotFound', 'Subscription Not Found', 'ErrorMessages', 'Error message for code 24 - Subscription not found', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_ConcurrentUpdateDetected', 'Concurrent Update Detected', 'ErrorMessages', 'Error message for code 25 - Concurrent update', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_SourceSubscriptionNotFound', 'Source Subscription Not Found', 'ErrorMessages', 'Error message for code 26 - Source subscription not found', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_DestinationSubscriptionNotFound', 'Destination Subscription Not Found', 'ErrorMessages', 'Error message for code 27 - Destination subscription not found', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_UserNotAllowed', 'User Not Allowed To Call This Method', 'ErrorMessages', 'Error message for code 28 - User not authorized', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_ConfigurationError', 'Configuration Error', 'ErrorMessages', 'Error message for code 29 - Configuration error', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_PropertyNotFound', 'Property Not Found', 'ErrorMessages', 'Error message for code 30 - Property not found', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_ExpiredReservationCode', 'Expired Reservation Code', 'ErrorMessages', 'Error message for code 31 - Expired reservation', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_BadRequest', 'Bad Request', 'ErrorMessages', 'Error message for code 32 - Bad request', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_TransferNotAllowed', 'Credit transfer is not allowed to this such account type', 'ErrorMessages', 'Error message for code 33 - Transfer not allowed for account type', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_ExceedsMaxPerDay', 'Subscription has been reached the number of transfers per day', 'ErrorMessages', 'Error message for code 34 - Daily transfer limit exceeded', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_RemainingBalance', 'Insufficient Remaining Credit', 'ErrorMessages', 'Error message for code 35 - Insufficient remaining balance', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_InvalidTransferAmount', 'Transfer amount should be a multiple of 5', 'ErrorMessages', 'Error message for code 36 - Invalid transfer amount', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_SmsError', 'An error occurred while sending sms to the destination msisdn', 'ErrorMessages', 'Error message for code 37 - SMS sending error', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_ReserveAmountError', 'An error occurred while reserve amount', 'ErrorMessages', 'Error message for code 38 - Amount reservation error', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_CreditAmountError', 'Failed to credit the amount to the source msisdn', 'ErrorMessages', 'Error message for code 39 - Credit amount error', 1, GETUTCDATE(), 'System'),
    ('ErrorMessage_BalancePercentageError', 'Your balance remaining should be more than 50%', 'ErrorMessages', 'Error message for code 40 - Balance percentage error', 1, GETUTCDATE(), 'System')
GO

-- =============================================
-- Insert Messages data (Bilingual SMS Templates)
-- =============================================

-- Note: Using exact data provided by the user
SET IDENTITY_INSERT [dbo].[Messages] ON 
GO

INSERT [dbo].[Messages] ([Id], [Key], [TextEn], [TextAr], [IsActive], [CreatedDate]) VALUES 
(7, N'CreditTransferSuccessMsgA', N'Thank you. OMR {0} has been successfully transferred to {1}. Your new balance is OMR {2}.', N'لقد تم تحويل {0} ريال الى الرقم {1} بنجاح. رصيدك الحالي {2} ريال', 1, GETUTCDATE()),
(8, N'CreditTransferSuccessMsgB', N'SAR {0} has been successfully received from {1}. Your new balance is  SAR {2}.', N'لقد استلمت {0} ريال من الرقم {1}. رصيدك الحالي {2} ريال', 1, GETUTCDATE()),
(9, N'TransactionExpired', N'Your transaction has been expired.', N'نأسف، لقد انتهت الفترة المحددة لاتمام عملية تحويل الرصيد', 1, GETUTCDATE()),
(10, N'TransactionNotFound', N'Transaction not found.', N'لايوجد لديك عملية تحويل رصيد حالياً', 1, GETUTCDATE()),
(11, N'VirginCreditTransferSMS', N'Enter Your ID / Iqama number, {0} wants to transfer the amount OMR {1}.', N'الرقم {0} يريد تحويل {1} ريال الى حسابك. الرجاء الرد على هذه الرسالة برقم الهوية أو رقم الإقامة', 1, GETUTCDATE()),
(12, N'WrongIdNumber', N'The ID number you entered is incorrect. Please re-enter the correct ID number.', N'رقم الهوية أو رقم الإقامة غير صحيح. الرجاء المحاولة مرة أخرى', 1, GETUTCDATE()),
(13, N'AdjustAccountFailure', N'Failed to credit the balance ', N'نأسف، لقد فشلت عملية تحويل الرصيد. الرجاء المحاولة مرة أخرى', 1, GETUTCDATE()),
(14, N'MoneyBack', N'OMR {0} has been reverted back to your balance due to credit transfer to {1} expiration period.', N'عملية تحويل {0} ريال الى الرقم {1} فشلت وتم إعادة {0} ريال الى حسابك', 1, GETUTCDATE()),
(17, N'NobillComment', N'comment 
Fund transfer detail: {2} OMR from phoneno {0} to phoneno {1} ', N'comment 
Fund transfer detail: {2} OMR from phoneno {0} to phoneno {1} ', 1, GETUTCDATE()),
(18, N'PostpaidCreditTransferSuccessMsgA', N'Thank you. OMR  {0} has been successfully transferred to {1}. ', N'لقد تم تحويل {0} ريال الى الرقم {1} بنجاح.', 1, GETUTCDATE())
GO

SET IDENTITY_INSERT [dbo].[Messages] OFF
GO

-- =============================================
-- Update TransferConfig data (if table exists)
-- =============================================

-- Note: TransferConfig table should already exist with the provided data:
-- The TransferConfig table contains the actual production configuration values
-- for different subscription types and their transfer limits

PRINT 'Database tables created and seeded successfully'
PRINT 'External SQL Server: 10.1.133.31'
PRINT 'Tables created: ApplicationConfigs, Messages'
PRINT 'Note: TransferConfig table should already contain the production data'
GO 