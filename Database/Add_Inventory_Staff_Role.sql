-- =====================================================
-- ADD INVENTORY STAFF ROLE AND FIX USER ASSIGNMENTS
-- Run this in SSMS to add Inventory Staff role
-- =====================================================

USE Compugear;
GO

-- Add Inventory Staff role as RoleId 8
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleId = 8)
BEGIN
    SET IDENTITY_INSERT Roles ON;
    INSERT INTO Roles (RoleId, RoleName, Description, AccessLevel, IsActive, CreatedAt, UpdatedAt)
    VALUES (8, 'Inventory Staff', 'Inventory and stock management', 3, 1, GETUTCDATE(), GETUTCDATE());
    SET IDENTITY_INSERT Roles OFF;
    PRINT 'Inventory Staff role (RoleId 8) created successfully!';
END
ELSE
BEGIN
    UPDATE Roles SET RoleName = 'Inventory Staff', Description = 'Inventory and stock management' WHERE RoleId = 8;
    PRINT 'Inventory Staff role (RoleId 8) already exists - updated.';
END

-- Update the inventory@compugear.com user to use RoleId 8
UPDATE Users SET RoleId = 8, UpdatedAt = GETUTCDATE() 
WHERE Email = 'inventory@compugear.com';
PRINT 'Updated inventory@compugear.com to RoleId 8';

-- Verify roles
SELECT * FROM Roles ORDER BY RoleId;

-- Verify users
SELECT UserId, Username, Email, FirstName, LastName, RoleId, 
       (SELECT RoleName FROM Roles WHERE Roles.RoleId = Users.RoleId) AS RoleName
FROM Users
ORDER BY RoleId;

PRINT '';
PRINT '===========================================';
PRINT 'ROLE MAPPING:';
PRINT '  1 - Super Admin      -> /Home (Admin Dashboard)';
PRINT '  2 - Company Admin    -> /Home (Admin Dashboard)';
PRINT '  3 - Sales Staff      -> /SalesStaff';
PRINT '  4 - Support Staff    -> /SupportStaff';
PRINT '  5 - Marketing Staff  -> /MarketingStaff';
PRINT '  6 - Billing Staff    -> /BillingStaff';
PRINT '  7 - Customer         -> /CustomerPortal';
PRINT '  8 - Inventory Staff  -> /InventoryStaff';
PRINT '===========================================';
GO
