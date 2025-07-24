-- Update NoBill Configuration in Database
-- This script updates the NoBill service configuration in the ApplicationConfig table

USE CreditTransferDB;

-- Update NoBill Service URL to use host.docker.internal
UPDATE ApplicationConfig 
SET ConfigValue = 'http://host.docker.internal:9099/NobillProxy.UAT/NobillCalls.asmx'
WHERE ConfigKey = 'NobillCalls:ServiceUrl';

-- Update NoBill Username
UPDATE ApplicationConfig 
SET ConfigValue = 'transfercredit'
WHERE ConfigKey = 'NobillCalls:UserName';

-- Update NoBill Password  
UPDATE ApplicationConfig 
SET ConfigValue = 'p@ssw0rd'
WHERE ConfigKey = 'NobillCalls:Password';

-- Insert missing configurations if they don't exist
IF NOT EXISTS (SELECT 1 FROM ApplicationConfig WHERE ConfigKey = 'NobillCalls:ServiceUrl')
BEGIN
    INSERT INTO ApplicationConfig (ConfigKey, ConfigValue, Description, IsActive, Category, CreatedDate, LastModifiedDate)
    VALUES ('NobillCalls:ServiceUrl', 'http://host.docker.internal:9099/NobillProxy.UAT/NobillCalls.asmx', 'NoBill Service URL through VPN proxy', 1, 'NoBill', GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM ApplicationConfig WHERE ConfigKey = 'NobillCalls:UserName')
BEGIN
    INSERT INTO ApplicationConfig (ConfigKey, ConfigValue, Description, IsActive, Category, CreatedDate, LastModifiedDate)
    VALUES ('NobillCalls:UserName', 'transfercredit', 'NoBill Service Username', 1, 'NoBill', GETDATE(), GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM ApplicationConfig WHERE ConfigKey = 'NobillCalls:Password')
BEGIN
    INSERT INTO ApplicationConfig (ConfigKey, ConfigValue, Description, IsActive, Category, CreatedDate, LastModifiedDate)
    VALUES ('NobillCalls:Password', 'p@ssw0rd', 'NoBill Service Password', 1, 'NoBill', GETDATE(), GETDATE());
END

-- Show the updated configuration
SELECT ConfigKey, ConfigValue, Description, IsActive 
FROM ApplicationConfig 
WHERE ConfigKey LIKE 'NobillCalls:%'
ORDER BY ConfigKey;

PRINT 'NoBill configuration updated successfully!'; 