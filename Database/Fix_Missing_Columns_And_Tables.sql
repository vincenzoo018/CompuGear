-- =====================================================
-- FIX MISSING COLUMNS AND TABLES
-- Run this in SSMS to add all missing columns/tables
-- that the C# models and API controllers expect.
-- Fixes: "Invalid column name 'CompanyId'" on Suppliers,
--         Missing SupplierId on Products,
--         Missing PurchaseOrders/PurchaseOrderItems tables.
-- =====================================================

USE Compugear;
GO

-- =====================================================
-- 1. Add CompanyId to Suppliers (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Suppliers ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Suppliers SET CompanyId = 1;
    PRINT 'Added CompanyId to Suppliers';
END
GO

-- =====================================================
-- 2. Add CompanyId to Products (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Products ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Products SET CompanyId = 1;
    PRINT 'Added CompanyId to Products';
END
GO

-- =====================================================
-- 3. Add SupplierId to Products (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SupplierId')
BEGIN
    ALTER TABLE Products ADD SupplierId INT NULL FOREIGN KEY REFERENCES Suppliers(SupplierId);
    PRINT 'Added SupplierId to Products';
END
GO

-- =====================================================
-- 4. Add CompanyId to Orders (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Orders ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Orders SET CompanyId = 1;
    PRINT 'Added CompanyId to Orders';
END
GO

-- =====================================================
-- 5. Add CompanyId to Customers (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Customers ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Customers SET CompanyId = 1;
    PRINT 'Added CompanyId to Customers';
END
GO

-- =====================================================
-- 6. Add CompanyId to Leads (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Leads ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Leads SET CompanyId = 1;
    PRINT 'Added CompanyId to Leads';
END
GO

-- =====================================================
-- 7. Add CompanyId to Invoices (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Invoices ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Invoices SET CompanyId = 1;
    PRINT 'Added CompanyId to Invoices';
END
GO

-- =====================================================
-- 8. Add CompanyId to Payments (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Payments ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Payments SET CompanyId = 1;
    PRINT 'Added CompanyId to Payments';
END
GO

-- =====================================================
-- 9. Add CompanyId to Refunds (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Refunds ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Refunds SET CompanyId = 1;
    PRINT 'Added CompanyId to Refunds';
END
GO

-- =====================================================
-- 10. Add CompanyId to Campaigns (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Campaigns') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Campaigns ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Campaigns SET CompanyId = 1;
    PRINT 'Added CompanyId to Campaigns';
END
GO

-- =====================================================
-- 11. Add CompanyId to CustomerSegments (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerSegments') AND name = 'CompanyId')
BEGIN
    ALTER TABLE CustomerSegments ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE CustomerSegments SET CompanyId = 1;
    PRINT 'Added CompanyId to CustomerSegments';
END
GO

-- =====================================================
-- 12. Add CompanyId to Promotions (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Promotions') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Promotions ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE Promotions SET CompanyId = 1;
    PRINT 'Added CompanyId to Promotions';
END
GO

-- =====================================================
-- 13. Add CompanyId to SupportTickets (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupportTickets') AND name = 'CompanyId')
BEGIN
    ALTER TABLE SupportTickets ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE SupportTickets SET CompanyId = 1;
    PRINT 'Added CompanyId to SupportTickets';
END
GO

-- =====================================================
-- 14. Add CompanyId to ChatSessions (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChatSessions') AND name = 'CompanyId')
BEGIN
    ALTER TABLE ChatSessions ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
    UPDATE ChatSessions SET CompanyId = 1;
    PRINT 'Added CompanyId to ChatSessions';
END
GO

-- =====================================================
-- 15. Create PurchaseOrders table (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrders')
BEGIN
    CREATE TABLE PurchaseOrders (
        PurchaseOrderId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        SupplierId INT NOT NULL FOREIGN KEY REFERENCES Suppliers(SupplierId),
        OrderDate DATETIME2 DEFAULT GETDATE(),
        ExpectedDeliveryDate DATETIME2,
        ActualDeliveryDate DATETIME2,
        Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Shipped, Completed, Cancelled
        TotalAmount DECIMAL(18,2) DEFAULT 0,
        Notes NVARCHAR(MAX),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2
    );
    PRINT 'Created PurchaseOrders table';
END
ELSE
BEGIN
    -- Add CompanyId to PurchaseOrders if table exists but column is missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'CompanyId')
    BEGIN
        ALTER TABLE PurchaseOrders ADD CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId);
        UPDATE PurchaseOrders SET CompanyId = 1;
        PRINT 'Added CompanyId to PurchaseOrders';
    END
END
GO

-- =====================================================
-- 16. Create PurchaseOrderItems table (if missing)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrderItems')
BEGIN
    CREATE TABLE PurchaseOrderItems (
        PurchaseOrderItemId INT PRIMARY KEY IDENTITY(1,1),
        PurchaseOrderId INT NOT NULL FOREIGN KEY REFERENCES PurchaseOrders(PurchaseOrderId) ON DELETE CASCADE,
        ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) DEFAULT 0,
        Subtotal DECIMAL(18,2) DEFAULT 0
    );
    PRINT 'Created PurchaseOrderItems table';
END
GO

-- =====================================================
-- 17. Create ERP tables if missing
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

    INSERT INTO CompanySubscriptions (CompanyId, PlanName, Status, BillingCycle, MonthlyFee, MaxUsers, StartDate)
    VALUES (1, 'Enterprise', 'Active', 'Annual', 199.99, 50, GETUTCDATE());

    PRINT 'CompanySubscriptions table created.';
END
GO

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

    INSERT INTO CompanyModuleAccess (CompanyId, ModuleId, IsEnabled)
    SELECT 1, ModuleId, 1 FROM ERPModules;

    PRINT 'CompanyModuleAccess table created.';
END
GO

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

    -- Company Admin (2) gets all modules
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 2, 'SALES', 1), (1, 2, 'SUPPORT', 1), (1, 2, 'MARKETING', 1),
    (1, 2, 'BILLING', 1), (1, 2, 'INVENTORY', 1), (1, 2, 'CUSTOMERS', 1);

    -- Sales Staff (3)
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 3, 'SALES', 1), (1, 3, 'CUSTOMERS', 1);

    -- Support Staff (4)
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 4, 'SUPPORT', 1), (1, 4, 'CUSTOMERS', 1);

    -- Marketing Staff (5)
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 5, 'MARKETING', 1), (1, 5, 'CUSTOMERS', 1);

    -- Billing Staff (6)
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 6, 'BILLING', 1), (1, 6, 'CUSTOMERS', 1);

    -- Inventory Staff (7)
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 7, 'INVENTORY', 1), (1, 7, 'CUSTOMERS', 1);

    PRINT 'RoleModuleAccess table created and seeded.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlatformUsageLogs')
BEGIN
    CREATE TABLE PlatformUsageLogs (
        LogId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        UserId INT FOREIGN KEY REFERENCES Users(UserId),
        Action NVARCHAR(100) NOT NULL,
        Module NVARCHAR(50),
        Details NVARCHAR(500),
        IPAddress NVARCHAR(45),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'PlatformUsageLogs table created.';
END
GO

-- =====================================================
-- 18. Create ApprovalRequests table if missing
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApprovalRequests')
BEGIN
    CREATE TABLE ApprovalRequests (
        RequestId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT FOREIGN KEY REFERENCES Companies(CompanyId),
        RequestType NVARCHAR(50) NOT NULL,
        RequestedBy INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
        RequestDate DATETIME2 DEFAULT GETUTCDATE(),
        Status NVARCHAR(20) DEFAULT 'Pending',
        Priority NVARCHAR(20) DEFAULT 'Normal',
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX),
        EntityType NVARCHAR(50),
        EntityId INT,
        RequestData NVARCHAR(MAX),
        ReviewedBy INT FOREIGN KEY REFERENCES Users(UserId),
        ReviewDate DATETIME2,
        ReviewNotes NVARCHAR(MAX),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'ApprovalRequests table created.';
END
GO

PRINT '';
PRINT '====================================================';
PRINT 'ALL MISSING COLUMNS AND TABLES HAVE BEEN FIXED!';
PRINT 'Restart your application to apply changes.';
PRINT '====================================================';
GO
