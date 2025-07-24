-- =============================================
-- Credit Transfer Business Rules Configuration
-- Replaces hard-coded business logic with configurable database table
-- =============================================

-- Create TransferRules table for configurable business logic
CREATE TABLE [dbo].[TransferRules] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Country] NVARCHAR(10) NOT NULL DEFAULT 'OM', -- OM for Oman, KSA for Saudi Arabia
    [SourceSubscriptionType] NVARCHAR(50) NOT NULL,
    [DestinationSubscriptionType] NVARCHAR(50) NOT NULL,
    [IsAllowed] BIT NOT NULL DEFAULT 0,
    [ErrorCode] INT NULL, -- Error code to return when not allowed
    [ErrorMessage] NVARCHAR(500) NULL, -- Custom error message
    [RequiresConfiguration] BIT NOT NULL DEFAULT 0, -- Whether rule depends on configuration (like TransferToOtherOperator)
    [ConfigurationKey] NVARCHAR(100) NULL, -- Configuration key to check (e.g., 'TransferToOtherOperator')
    [ConfigurationValue] NVARCHAR(100) NULL, -- Required configuration value
    [Priority] INT NOT NULL DEFAULT 100, -- Rule priority (lower number = higher priority)
    [Description] NVARCHAR(500) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'System',
    [ModifiedBy] NVARCHAR(100) NOT NULL DEFAULT 'System'
);

-- Create indexes for performance
CREATE INDEX IX_TransferRules_Country_Active ON [dbo].[TransferRules] ([Country], [IsActive]);
CREATE INDEX IX_TransferRules_SourceDest ON [dbo].[TransferRules] ([SourceSubscriptionType], [DestinationSubscriptionType]);
CREATE INDEX IX_TransferRules_Priority ON [dbo].[TransferRules] ([Priority]);

-- =============================================
-- Insert OMAN Business Rules
-- =============================================

-- OMAN: NOT ALLOWED RULES (Higher Priority = 10)
-- Rule 1: Customer/HalafoniCustomer cannot transfer to Pos/Distributor
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [ErrorCode], [Priority], [Description]) VALUES
('OM', 'Customer', 'Pos', 0, 33, 10, 'Prevent customers from transferring to dealers'),
('OM', 'Customer', 'Distributor', 0, 33, 10, 'Prevent customers from transferring to distributors'),
('OM', 'HalafoniCustomer', 'Pos', 0, 33, 10, 'Prevent Halafoni customers from transferring to dealers'),
('OM', 'HalafoniCustomer', 'Distributor', 0, 33, 10, 'Prevent Halafoni customers from transferring to distributors');

-- Rule 2: Cross-customer type validation (Customer ↔ HalafoniCustomer)
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [ErrorCode], [Priority], [Description]) VALUES
('OM', 'Customer', 'HalafoniCustomer', 0, 33, 10, 'Prevent FRiENDi to Halafoni transfers'),
('OM', 'HalafoniCustomer', 'Customer', 0, 33, 10, 'Prevent Halafoni to FRiENDi transfers');

-- Rule 3: Pos to HalafoniCustomer validation
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [ErrorCode], [Priority], [Description]) VALUES
('OM', 'Pos', 'HalafoniCustomer', 0, 33, 10, 'Prevent dealers from transferring to Halafoni customers');

-- Rule 4: DataAccount cannot transfer to anyone
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [ErrorCode], [Priority], [Description]) VALUES
('OM', 'DataAccount', '*', 0, 33, 5, 'Data accounts cannot initiate transfers');

-- OMAN: ALLOWED RULES (Lower Priority = 50)
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [Priority], [Description]) VALUES
('OM', 'Customer', 'Customer', 1, 50, 'Customer to Customer transfers allowed'),
('OM', 'Customer', 'DataAccount', 1, 50, 'Customer to Data account transfers allowed'),
('OM', 'HalafoniCustomer', 'HalafoniCustomer', 1, 50, 'Halafoni to Halafoni transfers allowed'),
('OM', 'HalafoniCustomer', 'DataAccount', 1, 50, 'Halafoni to Data account transfers allowed'),
('OM', 'Pos', 'Customer', 1, 50, 'Dealer to Customer transfers allowed'),
('OM', 'Pos', 'Pos', 1, 50, 'Dealer to Dealer transfers allowed'),
('OM', 'Pos', 'Distributor', 1, 50, 'Dealer to Distributor transfers allowed'),
('OM', 'Pos', 'DataAccount', 1, 50, 'Dealer to Data account transfers allowed'),
('OM', 'Distributor', 'Customer', 1, 50, 'Distributor to Customer transfers allowed'),
('OM', 'Distributor', 'HalafoniCustomer', 1, 50, 'Distributor to Halafoni transfers allowed'),
('OM', 'Distributor', 'Pos', 1, 50, 'Distributor to Dealer transfers allowed'),
('OM', 'Distributor', 'Distributor', 1, 50, 'Distributor to Distributor transfers allowed'),
('OM', 'Distributor', 'DataAccount', 1, 50, 'Distributor to Data account transfers allowed');

-- =============================================
-- Insert KSA Business Rules
-- =============================================

-- KSA: NOT ALLOWED RULES (Higher Priority = 10)
-- Rule 1: Virgin customers cannot transfer to Pos/Distributor
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [ErrorCode], [Priority], [Description]) VALUES
('KSA', 'VirginPrepaidCustomer', 'Pos', 0, 33, 10, 'Virgin prepaid customers cannot transfer to dealers'),
('KSA', 'VirginPrepaidCustomer', 'Distributor', 0, 33, 10, 'Virgin prepaid customers cannot transfer to distributors'),
('KSA', 'VirginPostpaidCustomer', 'Pos', 0, 33, 10, 'Virgin postpaid customers cannot transfer to dealers'),
('KSA', 'VirginPostpaidCustomer', 'Distributor', 0, 33, 10, 'Virgin postpaid customers cannot transfer to distributors');

-- Rule 2: DataAccount cannot transfer to anyone
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [ErrorCode], [Priority], [Description]) VALUES
('KSA', 'DataAccount', '*', 0, 33, 5, 'Data accounts cannot initiate transfers');

-- KSA: Simple rules without configuration dependencies (for initial setup)
-- Cross-operator transfers are allowed by default in KSA

-- KSA: Always allowed rules
INSERT INTO [dbo].[TransferRules] ([Country], [SourceSubscriptionType], [DestinationSubscriptionType], [IsAllowed], [Priority], [Description]) VALUES
('KSA', 'Customer', 'Customer', 1, 50, 'FRiENDi customer to customer transfers allowed'),
('KSA', 'VirginPrepaidCustomer', 'VirginPrepaidCustomer', 1, 50, 'Virgin prepaid to prepaid transfers allowed'),
('KSA', 'VirginPostpaidCustomer', 'VirginPostpaidCustomer', 1, 50, 'Virgin postpaid to postpaid transfers allowed'),
('KSA', 'Pos', 'Customer', 1, 50, 'Dealer to FRiENDi customer transfers allowed'),
('KSA', 'Pos', 'VirginPrepaidCustomer', 1, 50, 'Dealer to Virgin prepaid transfers allowed'),
('KSA', 'Pos', 'VirginPostpaidCustomer', 1, 50, 'Dealer to Virgin postpaid transfers allowed'),
('KSA', 'Distributor', 'Customer', 1, 50, 'Distributor to FRiENDi customer transfers allowed'),
('KSA', 'Distributor', 'VirginPrepaidCustomer', 1, 50, 'Distributor to Virgin prepaid transfers allowed'),
('KSA', 'Distributor', 'VirginPostpaidCustomer', 1, 50, 'Distributor to Virgin postpaid transfers allowed');

-- =============================================
-- Create view for easy rule lookup
-- =============================================
CREATE VIEW [dbo].[vw_ActiveTransferRules] AS
SELECT 
    Country,
    SourceSubscriptionType,
    DestinationSubscriptionType,
    IsAllowed,
    ErrorCode,
    ErrorMessage,
    RequiresConfiguration,
    ConfigurationKey,
    ConfigurationValue,
    Priority,
    Description
FROM [dbo].[TransferRules]
WHERE IsActive = 1;

-- =============================================
-- Create simplified stored procedure for rule evaluation
-- =============================================
CREATE PROCEDURE [dbo].[sp_EvaluateTransferRule]
    @Country NVARCHAR(10),
    @SourceSubscriptionType NVARCHAR(50),
    @DestinationSubscriptionType NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @IsAllowed BIT = 1; -- Default allow
    DECLARE @ErrorCode INT = 0;
    DECLARE @ErrorMessage NVARCHAR(500) = 'Transfer allowed';
    DECLARE @RuleFound BIT = 0;
    
    -- Check for wildcard source rules first (highest priority)
    IF EXISTS (SELECT 1 FROM [dbo].[TransferRules] 
               WHERE Country = @Country 
               AND SourceSubscriptionType = @SourceSubscriptionType 
               AND DestinationSubscriptionType = '*' 
               AND IsActive = 1)
    BEGIN
        SELECT TOP 1
            @IsAllowed = IsAllowed,
            @ErrorCode = ISNULL(ErrorCode, 0),
            @ErrorMessage = ISNULL(ErrorMessage, 'Transfer not allowed'),
            @RuleFound = 1
        FROM [dbo].[TransferRules]
        WHERE Country = @Country 
        AND SourceSubscriptionType = @SourceSubscriptionType 
        AND DestinationSubscriptionType = '*'
        AND IsActive = 1
        ORDER BY Priority ASC;
    END
    
    -- Check for specific source-destination rules (overrides wildcard if found)
    IF EXISTS (SELECT 1 FROM [dbo].[TransferRules] 
               WHERE Country = @Country 
               AND SourceSubscriptionType = @SourceSubscriptionType 
               AND DestinationSubscriptionType = @DestinationSubscriptionType 
               AND IsActive = 1)
    BEGIN
        SELECT TOP 1
            @IsAllowed = IsAllowed,
            @ErrorCode = ISNULL(ErrorCode, 0),
            @ErrorMessage = ISNULL(ErrorMessage, 'Transfer rule applied'),
            @RuleFound = 1
        FROM [dbo].[TransferRules]
        WHERE Country = @Country 
        AND SourceSubscriptionType = @SourceSubscriptionType 
        AND DestinationSubscriptionType = @DestinationSubscriptionType
        AND IsActive = 1
        ORDER BY Priority ASC;
    END
    
    -- Return results
    SELECT 
        @IsAllowed AS IsAllowed,
        @ErrorCode AS ErrorCode,
        @ErrorMessage AS ErrorMessage,
        @RuleFound AS RuleFound;
END;

-- =============================================
-- Sample usage query
-- =============================================
/*
-- Test Oman rules
EXEC [dbo].[sp_EvaluateTransferRule] 'OM', 'Customer', 'Pos';                    -- Should return IsAllowed = 0
EXEC [dbo].[sp_EvaluateTransferRule] 'OM', 'Customer', 'Customer';               -- Should return IsAllowed = 1
EXEC [dbo].[sp_EvaluateTransferRule] 'OM', 'DataAccount', 'Customer';            -- Should return IsAllowed = 0

-- Test KSA rules
EXEC [dbo].[sp_EvaluateTransferRule] 'KSA', 'Customer', 'VirginPrepaidCustomer'; -- Should return IsAllowed = 1
EXEC [dbo].[sp_EvaluateTransferRule] 'KSA', 'VirginPrepaidCustomer', 'Pos';      -- Should return IsAllowed = 0
EXEC [dbo].[sp_EvaluateTransferRule] 'KSA', 'DataAccount', 'Customer';           -- Should return IsAllowed = 0
*/ 