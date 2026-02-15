-- =====================================================
-- SUPER ADMIN ERP MODULE TABLES
-- Run this in SSMS to add ERP subscription tables
-- =====================================================

USE Compugear;
GO

-- =====================================================
-- ERP Modules Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ERPModules')
BEGIN
    CREATE TABLE ERPModules (
        ModuleId INT PRIMARY KEY IDENTITY(1,1),
        ModuleName NVARCHAR(100) NOT NULL,
        ModuleCode NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(500),
        Icon NVARCHAR(100),
        IsActive BIT DEFAULT 1,
        MonthlyPrice DECIMAL(18,2) DEFAULT 0,
        AnnualPrice DECIMAL(18,2) DEFAULT 0,
        Features NVARCHAR(MAX),
        SortOrder INT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );

    INSERT INTO ERPModules (ModuleName, ModuleCode, Description, MonthlyPrice, AnnualPrice, SortOrder, Features) VALUES
    ('Sales Management', 'SALES', 'Complete sales pipeline, orders, leads and revenue tracking', 49.99, 499.99, 1, 'Order Management,Lead Tracking,Sales Reports,Revenue Analytics'),
    ('Customer Support', 'SUPPORT', 'Ticket management, live chat, knowledge base and SLA tracking', 39.99, 399.99, 2, 'Ticket System,Live Chat,Knowledge Base,SLA Management'),
    ('Marketing', 'MARKETING', 'Campaign management, promotions, customer segmentation and analytics', 44.99, 449.99, 3, 'Campaigns,Promotions,Segments,Marketing Analytics'),
    ('Billing & Accounting', 'BILLING', 'Invoice generation, payment processing, financial reports', 54.99, 549.99, 4, 'Invoices,Payments,Refunds,Financial Reports'),
    ('Inventory Management', 'INVENTORY', 'Product catalog, stock tracking, purchase orders and suppliers', 44.99, 449.99, 5, 'Products,Stock Levels,Purchase Orders,Suppliers'),
    ('Customer Management', 'CUSTOMERS', 'Customer profiles, categories, addresses and purchase history', 29.99, 299.99, 6, 'Customer Profiles,Categories,History,Addresses');

    PRINT 'ERPModules table created and seeded.';
END
GO

-- =====================================================
-- Company Subscriptions Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanySubscriptions')
BEGIN
    CREATE TABLE CompanySubscriptions (
        SubscriptionId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        PlanName NVARCHAR(50) NOT NULL DEFAULT 'Basic',
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        BillingCycle NVARCHAR(20) DEFAULT 'Monthly',
        StartDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        EndDate DATETIME2,
        TrialEndDate DATETIME2,
        MonthlyFee DECIMAL(18,2) DEFAULT 0,
        MaxUsers INT DEFAULT 5,
        Notes NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
        UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
    );

    -- Add default subscription for CompuGear Main
    INSERT INTO CompanySubscriptions (CompanyId, PlanName, Status, BillingCycle, MonthlyFee, MaxUsers, StartDate)
    VALUES (1, 'Enterprise', 'Active', 'Annual', 199.99, 50, GETUTCDATE());

    PRINT 'CompanySubscriptions table created.';
END
GO

-- =====================================================
-- Company Module Access Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanyModuleAccess')
BEGIN
    CREATE TABLE CompanyModuleAccess (
        AccessId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        ModuleId INT NOT NULL FOREIGN KEY REFERENCES ERPModules(ModuleId),
        IsEnabled BIT DEFAULT 1,
        ActivatedAt DATETIME2 DEFAULT GETUTCDATE(),
        DeactivatedAt DATETIME2,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_CompanyModule UNIQUE (CompanyId, ModuleId)
    );

    -- Give CompuGear Main access to all modules
    INSERT INTO CompanyModuleAccess (CompanyId, ModuleId, IsEnabled)
    SELECT 1, ModuleId, 1 FROM ERPModules;

    PRINT 'CompanyModuleAccess table created.';
END
GO

-- =====================================================
-- Platform Usage Logs Table
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlatformUsageLogs')
BEGIN
    CREATE TABLE PlatformUsageLogs (
        LogId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT FOREIGN KEY REFERENCES Companies(CompanyId),
        UserId INT FOREIGN KEY REFERENCES Users(UserId),
        ModuleCode NVARCHAR(50),
        Action NVARCHAR(100),
        Details NVARCHAR(500),
        IPAddress NVARCHAR(50),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );

    PRINT 'PlatformUsageLogs table created.';
END
GO

-- =====================================================
-- Verify Super Admin User exists
-- =====================================================
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = 'superadmin@compugear.com')
BEGIN
    DECLARE @Salt NVARCHAR(255) = 'CompuGearSalt2024';
    DECLARE @PasswordHash NVARCHAR(255) = 'LLa6ziN2IFID4vOA6XxZAPHaPMdthL5I4QbicaqplE0=';

    -- Ensure admin@compugear.com is RoleId 1 (Super Admin)
    UPDATE Users SET RoleId = 1 WHERE Email = 'admin@compugear.com';
    
    PRINT 'Super Admin user verified.';
END
GO

PRINT '';
PRINT '===========================================';
PRINT 'SUPER ADMIN ERP TABLES CREATED SUCCESSFULLY';
PRINT '';
PRINT 'Super Admin Login: admin@compugear.com';
PRINT 'Password: password123';
PRINT '===========================================';
GO
