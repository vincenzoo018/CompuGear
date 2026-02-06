-- =====================================================
-- FIX EXISTING USERS WITH CORRECT PASSWORD HASH AND ROLES
-- Run this in SSMS to fix authentication for all staff users
-- Password: password123 for all users
-- =====================================================

USE Compugear;
GO

-- Pre-computed Base64 hash for 'password123' + 'CompuGearSalt2024'
DECLARE @Salt NVARCHAR(255) = 'CompuGearSalt2024';
DECLARE @PasswordHash NVARCHAR(255) = 'LLa6ziN2IFID4vOA6XxZAPHaPMdthL5I4QbicaqplE0=';

-- First, ensure all required roles exist
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 1)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (1, 'Super Admin', 'Full system access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 2)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (2, 'Company Admin', 'Company-level admin access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 3)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (3, 'Sales Staff', 'Sales portal access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 4)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (4, 'Support Staff', 'Support portal access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 5)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (5, 'Marketing Staff', 'Marketing portal access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 6)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (6, 'Billing Staff', 'Billing portal access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 7)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (7, 'Inventory Staff', 'Inventory portal access');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 8)
    INSERT INTO Roles (RoleId, RoleName, Description) VALUES (8, 'Customer', 'Customer portal access');

PRINT 'Roles verified/created.';

-- Update existing roles to correct names
UPDATE Roles SET RoleName = 'Super Admin', Description = 'Full system access' WHERE RoleId = 1;
UPDATE Roles SET RoleName = 'Company Admin', Description = 'Company-level admin access' WHERE RoleId = 2;
UPDATE Roles SET RoleName = 'Sales Staff', Description = 'Sales portal access' WHERE RoleId = 3;
UPDATE Roles SET RoleName = 'Support Staff', Description = 'Support portal access' WHERE RoleId = 4;
UPDATE Roles SET RoleName = 'Marketing Staff', Description = 'Marketing portal access' WHERE RoleId = 5;
UPDATE Roles SET RoleName = 'Billing Staff', Description = 'Billing portal access' WHERE RoleId = 6;
UPDATE Roles SET RoleName = 'Inventory Staff', Description = 'Inventory portal access' WHERE RoleId = 7;
UPDATE Roles SET RoleName = 'Customer', Description = 'Customer portal access' WHERE RoleId = 8;

PRINT 'Roles updated.';

-- Delete existing sample users by email if they exist
DELETE FROM Users WHERE Email IN (
    'admin@compugear.com', 'companyadmin@compugear.com', 
    'sales@compugear.com', 'support@compugear.com', 
    'marketing@compugear.com', 'billing@compugear.com', 
    'inventory@compugear.com'
);

-- Delete existing sample users by username if they exist
DELETE FROM Users WHERE Username IN (
    'admin', 'company.admin', 
    'sarah.johnson', 'mike.chen', 
    'emily.brown', 'john.doe', 
    'james.wilson'
);

PRINT 'Old users deleted.';

-- Ensure Company with Id 1 exists
IF NOT EXISTS (SELECT 1 FROM Companies WHERE CompanyId = 1)
BEGIN
    SET IDENTITY_INSERT Companies ON;
    INSERT INTO Companies (CompanyId, CompanyName, CompanyCode, IsActive, CreatedAt, UpdatedAt)
    VALUES (1, 'CompuGear Main', 'CGMAIN', 1, GETUTCDATE(), GETUTCDATE());
    SET IDENTITY_INSERT Companies OFF;
    PRINT 'Company created.';
END

-- Super Admin (RoleId: 1)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified, CreatedAt, UpdatedAt)
VALUES ('admin', 'admin@compugear.com', @PasswordHash, @Salt, 'System', 'Administrator', 1, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

-- Company Admin (RoleId: 2)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified, CreatedAt, UpdatedAt)
VALUES ('company.admin', 'companyadmin@compugear.com', @PasswordHash, @Salt, 'Company', 'Admin', 2, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

-- Sales Staff (RoleId: 3)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified, CreatedAt, UpdatedAt)
VALUES ('sarah.johnson', 'sales@compugear.com', @PasswordHash, @Salt, 'Sarah', 'Johnson', 3, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

-- Support Staff (RoleId: 4)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified, CreatedAt, UpdatedAt)
VALUES ('mike.chen', 'support@compugear.com', @PasswordHash, @Salt, 'Mike', 'Chen', 4, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

-- Marketing Staff (RoleId: 5)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified, CreatedAt, UpdatedAt)
VALUES ('emily.brown', 'marketing@compugear.com', @PasswordHash, @Salt, 'Emily', 'Brown', 5, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

-- Billing Staff (RoleId: 6)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified, CreatedAt, UpdatedAt)
VALUES ('john.doe', 'billing@compugear.com', @PasswordHash, @Salt, 'John', 'Doe', 6, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

-- Inventory Staff (RoleId: 7)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified, CreatedAt, UpdatedAt)
VALUES ('james.wilson', 'inventory@compugear.com', @PasswordHash, @Salt, 'James', 'Wilson', 7, 1, 1, 1, GETUTCDATE(), GETUTCDATE());

PRINT 'Users created successfully!';

-- Verify users were created
SELECT 
    UserId, 
    Username, 
    Email, 
    FirstName, 
    LastName, 
    RoleId,
    (SELECT RoleName FROM Roles WHERE Roles.RoleId = Users.RoleId) AS RoleName,
    IsActive,
    IsEmailVerified
FROM Users
WHERE Email IN (
    'admin@compugear.com', 'companyadmin@compugear.com', 
    'sales@compugear.com', 'support@compugear.com', 
    'marketing@compugear.com', 'billing@compugear.com', 
    'inventory@compugear.com'
)
ORDER BY RoleId;

-- Verify roles
SELECT * FROM Roles ORDER BY RoleId;

PRINT '';
PRINT '===========================================';
PRINT 'ALL USERS CREATED SUCCESSFULLY!';
PRINT 'Password for all users: password123';
PRINT '';
PRINT 'Login emails:';
PRINT '  Super Admin:    admin@compugear.com';
PRINT '  Company Admin:  companyadmin@compugear.com';
PRINT '  Sales Staff:    sales@compugear.com';
PRINT '  Support Staff:  support@compugear.com';
PRINT '  Marketing Staff: marketing@compugear.com';
PRINT '  Billing Staff:  billing@compugear.com';
PRINT '  Inventory Staff: inventory@compugear.com';
PRINT '===========================================';
GO
