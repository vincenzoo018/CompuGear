-- =====================================================
-- CompuGear ERPWebsite Subscription System Migration
-- This script ensures all tables needed for the 
-- ERPWebsite subscription flow exist.
-- Safe to run multiple times (IF NOT EXISTS guards).
-- =====================================================

-- Ensure Companies table has all needed columns
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'Website')
BEGIN
    ALTER TABLE Companies ADD Website NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'TaxId')
BEGIN
    ALTER TABLE Companies ADD TaxId NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'Logo')
BEGIN
    ALTER TABLE Companies ADD Logo NVARCHAR(500) NULL;
END
GO

-- Ensure ERPModules table exists with seed data
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ERPModules')
BEGIN
    CREATE TABLE ERPModules (
        ModuleId INT IDENTITY(1,1) PRIMARY KEY,
        ModuleName NVARCHAR(100) NOT NULL,
        ModuleCode NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL,
        Icon NVARCHAR(100) NULL,
        IsActive BIT DEFAULT 1,
        MonthlyPrice DECIMAL(18,2) DEFAULT 0,
        AnnualPrice DECIMAL(18,2) DEFAULT 0,
        Features NVARCHAR(MAX) NULL,
        SortOrder INT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
END
GO

-- Seed ERP Modules if empty
IF NOT EXISTS (SELECT 1 FROM ERPModules)
BEGIN
    INSERT INTO ERPModules (ModuleName, ModuleCode, Description, Icon, IsActive, MonthlyPrice, AnnualPrice, SortOrder)
    VALUES
        ('Sales Management', 'SALES', 'Complete sales pipeline, lead tracking, quotations, and order management', 'dollar-sign', 1, 499, 4990, 1),
        ('Inventory Control', 'INVENTORY', 'Stock tracking, batch management, warehouse operations, and supplier management', 'package', 1, 499, 4990, 2),
        ('Billing & Invoicing', 'BILLING', 'Invoice generation, payment tracking, tax calculations, and financial reports', 'credit-card', 1, 699, 6990, 3),
        ('Customer Support', 'SUPPORT', 'Ticketing system, live chat, knowledge base, and SLA management', 'message-circle', 1, 599, 5990, 4),
        ('Marketing', 'MARKETING', 'Campaign management, email marketing, lead tracking, and analytics', 'trending-up', 1, 599, 5990, 5),
        ('Admin Dashboard', 'ADMIN', 'User management, role-based access, approval workflows, and system settings', 'settings', 1, 399, 3990, 6);
END
GO

-- Ensure CompanySubscriptions table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CompanySubscriptions')
BEGIN
    CREATE TABLE CompanySubscriptions (
        SubscriptionId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL,
        PlanName NVARCHAR(50) NOT NULL DEFAULT 'Basic',
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        BillingCycle NVARCHAR(20) DEFAULT 'Monthly',
        StartDate DATETIME2 DEFAULT GETUTCDATE(),
        EndDate DATETIME2 NULL,
        TrialEndDate DATETIME2 NULL,
        MonthlyFee DECIMAL(18,2) DEFAULT 0,
        MaxUsers INT DEFAULT 5,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL,
        UpdatedBy INT NULL,
        FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
    );
END
GO

-- Ensure CompanyModuleAccess table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CompanyModuleAccess')
BEGIN
    CREATE TABLE CompanyModuleAccess (
        AccessId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL,
        ModuleId INT NOT NULL,
        IsEnabled BIT DEFAULT 1,
        ActivatedAt DATETIME2 DEFAULT GETUTCDATE(),
        DeactivatedAt DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
        FOREIGN KEY (ModuleId) REFERENCES ERPModules(ModuleId)
    );
END
GO

-- Ensure PlatformUsageLogs table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PlatformUsageLogs')
BEGIN
    CREATE TABLE PlatformUsageLogs (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        UserId INT NULL,
        ModuleCode NVARCHAR(50) NULL,
        Action NVARCHAR(100) NULL,
        Details NVARCHAR(500) NULL,
        IPAddress NVARCHAR(50) NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
        FOREIGN KEY (UserId) REFERENCES Users(UserId)
    );
END
GO

-- Ensure RoleModuleAccess table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RoleModuleAccess')
BEGIN
    CREATE TABLE RoleModuleAccess (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL,
        RoleId INT NOT NULL,
        ModuleCode NVARCHAR(50) NOT NULL,
        HasAccess BIT DEFAULT 1,
        FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
        FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
    );
END
GO

PRINT '✓ ERPWebsite subscription tables verified/created successfully!';
GO
