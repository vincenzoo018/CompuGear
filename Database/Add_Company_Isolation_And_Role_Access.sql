-- =====================================================
-- COMPANY ISOLATION & ROLE-BASED ACCESS CONTROL
-- Run this in SSMS after Add_SuperAdmin_ERP_Tables.sql
-- Adds CompanyId to all business entities and creates
-- RoleModuleAccess table for role-based module control
-- =====================================================

USE Compugear;
GO

-- =====================================================
-- 1. Add CompanyId to business entities
-- =====================================================

-- Products
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Products ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Products SET CompanyId = 1;
    PRINT 'Added CompanyId to Products';
END
GO

-- Orders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Orders ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Orders SET CompanyId = 1;
    PRINT 'Added CompanyId to Orders';
END
GO

-- Customers
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Customers ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Customers SET CompanyId = 1;
    PRINT 'Added CompanyId to Customers';
END
GO

-- Leads
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Leads ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Leads SET CompanyId = 1;
    PRINT 'Added CompanyId to Leads';
END
GO

-- Invoices
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Invoices ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Invoices SET CompanyId = 1;
    PRINT 'Added CompanyId to Invoices';
END
GO

-- Payments
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Payments ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Payments SET CompanyId = 1;
    PRINT 'Added CompanyId to Payments';
END
GO

-- Refunds
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Refunds ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Refunds SET CompanyId = 1;
    PRINT 'Added CompanyId to Refunds';
END
GO

-- Campaigns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Campaigns') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Campaigns ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Campaigns SET CompanyId = 1;
    PRINT 'Added CompanyId to Campaigns';
END
GO

-- CustomerSegments
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerSegments') AND name = 'CompanyId')
BEGIN
    ALTER TABLE CustomerSegments ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE CustomerSegments SET CompanyId = 1;
    PRINT 'Added CompanyId to CustomerSegments';
END
GO

-- Promotions
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Promotions') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Promotions ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Promotions SET CompanyId = 1;
    PRINT 'Added CompanyId to Promotions';
END
GO

-- SupportTickets
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupportTickets') AND name = 'CompanyId')
BEGIN
    ALTER TABLE SupportTickets ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE SupportTickets SET CompanyId = 1;
    PRINT 'Added CompanyId to SupportTickets';
END
GO

-- Suppliers
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Suppliers ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Suppliers SET CompanyId = 1;
    PRINT 'Added CompanyId to Suppliers';
END
GO

-- PurchaseOrders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'CompanyId')
BEGIN
    ALTER TABLE PurchaseOrders ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE PurchaseOrders SET CompanyId = 1;
    PRINT 'Added CompanyId to PurchaseOrders';
END
GO

-- =====================================================
-- 2. Create RoleModuleAccess table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleModuleAccess')
BEGIN
    CREATE TABLE RoleModuleAccess (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId),
        ModuleCode NVARCHAR(50) NOT NULL,
        HasAccess BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_RoleModuleAccess UNIQUE (CompanyId, RoleId, ModuleCode)
    );

    -- Seed default role-module access for CompanyId 1
    -- Company Admin (2) gets all modules
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 2, 'SALES', 1), (1, 2, 'SUPPORT', 1), (1, 2, 'MARKETING', 1),
    (1, 2, 'BILLING', 1), (1, 2, 'INVENTORY', 1), (1, 2, 'CUSTOMERS', 1);

    -- Sales Staff (3) gets SALES + CUSTOMERS
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 3, 'SALES', 1), (1, 3, 'SUPPORT', 0), (1, 3, 'MARKETING', 0),
    (1, 3, 'BILLING', 0), (1, 3, 'INVENTORY', 0), (1, 3, 'CUSTOMERS', 1);

    -- Support Staff (4) gets SUPPORT + CUSTOMERS
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 4, 'SALES', 0), (1, 4, 'SUPPORT', 1), (1, 4, 'MARKETING', 0),
    (1, 4, 'BILLING', 0), (1, 4, 'INVENTORY', 0), (1, 4, 'CUSTOMERS', 1);

    -- Marketing Staff (5) gets MARKETING + CUSTOMERS
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 5, 'SALES', 0), (1, 5, 'SUPPORT', 0), (1, 5, 'MARKETING', 1),
    (1, 5, 'BILLING', 0), (1, 5, 'INVENTORY', 0), (1, 5, 'CUSTOMERS', 1);

    -- Billing Staff (6) gets BILLING + CUSTOMERS
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 6, 'SALES', 0), (1, 6, 'SUPPORT', 0), (1, 6, 'MARKETING', 0),
    (1, 6, 'BILLING', 1), (1, 6, 'INVENTORY', 0), (1, 6, 'CUSTOMERS', 1);

    -- Inventory Staff (8) gets INVENTORY + CUSTOMERS
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 8, 'SALES', 0), (1, 8, 'SUPPORT', 0), (1, 8, 'MARKETING', 0),
    (1, 8, 'BILLING', 0), (1, 8, 'INVENTORY', 1), (1, 8, 'CUSTOMERS', 1);

    PRINT 'RoleModuleAccess table created and seeded.';
END
GO

PRINT '';
PRINT '====================================================';
PRINT 'COMPANY ISOLATION & ROLE ACCESS MIGRATION COMPLETED';
PRINT '====================================================';
GO
