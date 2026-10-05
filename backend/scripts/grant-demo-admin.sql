-- Local portfolio demo only. Register this user first, then log in again after running this script.
-- Execute against the Dotnet8DB database. Replace the email below.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Email nvarchar(320) = N'user@example.com';
DECLARE @UserId int = (SELECT Id FROM Users WHERE Email = @Email);
IF @UserId IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Register the demo user before assigning permissions.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM OperationClaims WHERE Name = N'Admin')
    INSERT INTO OperationClaims (Name) VALUES (N'Admin');
DECLARE @ClaimId int = (SELECT TOP (1) Id FROM OperationClaims WHERE Name = N'Admin' ORDER BY Id);
IF NOT EXISTS (SELECT 1 FROM UserOperationClaims WHERE UserId = @UserId AND OperationClaimId = @ClaimId)
    INSERT INTO UserOperationClaims (UserId, OperationClaimId) VALUES (@UserId, @ClaimId);

COMMIT TRANSACTION;
