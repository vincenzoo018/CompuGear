-- Migration for remote database - split batches to avoid parse errors

-- 1. Suppliers CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'CompanyId')
    ALTER TABLE Suppliers ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'CompanyId')
    EXEC('UPDATE Suppliers SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 2. Products CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CompanyId')
    ALTER TABLE Products ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'CompanyId')
    EXEC('UPDATE Products SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 3. Products SupplierId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SupplierId')
    ALTER TABLE Products ADD SupplierId INT NULL;
GO

-- 4. Orders CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CompanyId')
    ALTER TABLE Orders ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Orders') AND name = 'CompanyId')
    EXEC('UPDATE Orders SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 5. Customers CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'CompanyId')
    ALTER TABLE Customers ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'CompanyId')
    EXEC('UPDATE Customers SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 6. Leads CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'CompanyId')
    ALTER TABLE Leads ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Leads') AND name = 'CompanyId')
    EXEC('UPDATE Leads SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 7. Invoices CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'CompanyId')
    ALTER TABLE Invoices ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'CompanyId')
    EXEC('UPDATE Invoices SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 8. Payments CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'CompanyId')
    ALTER TABLE Payments ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'CompanyId')
    EXEC('UPDATE Payments SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 9. Refunds CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'CompanyId')
    ALTER TABLE Refunds ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Refunds') AND name = 'CompanyId')
    EXEC('UPDATE Refunds SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 10. Campaigns CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Campaigns') AND name = 'CompanyId')
    ALTER TABLE Campaigns ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Campaigns') AND name = 'CompanyId')
    EXEC('UPDATE Campaigns SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 11. CustomerSegments CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerSegments') AND name = 'CompanyId')
    ALTER TABLE CustomerSegments ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CustomerSegments') AND name = 'CompanyId')
    EXEC('UPDATE CustomerSegments SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 12. Promotions CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Promotions') AND name = 'CompanyId')
    ALTER TABLE Promotions ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Promotions') AND name = 'CompanyId')
    EXEC('UPDATE Promotions SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 13. SupportTickets CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupportTickets') AND name = 'CompanyId')
    ALTER TABLE SupportTickets ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SupportTickets') AND name = 'CompanyId')
    EXEC('UPDATE SupportTickets SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 14. ChatSessions CompanyId
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChatSessions') AND name = 'CompanyId')
    ALTER TABLE ChatSessions ADD CompanyId INT NULL;
GO
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChatSessions') AND name = 'CompanyId')
    EXEC('UPDATE ChatSessions SET CompanyId = 1 WHERE CompanyId IS NULL');
GO

-- 15. Create PurchaseOrders table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrders')
BEGIN
    CREATE TABLE PurchaseOrders (
        PurchaseOrderId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NULL,
        SupplierId INT NOT NULL,
        OrderDate DATETIME2 DEFAULT GETDATE(),
        ExpectedDeliveryDate DATETIME2,
        ActualDeliveryDate DATETIME2,
        Status NVARCHAR(20) DEFAULT 'Pending',
        TotalAmount DECIMAL(18,2) DEFAULT 0,
        Notes NVARCHAR(MAX),
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        UpdatedAt DATETIME2
    );
    PRINT 'Created PurchaseOrders table';
END
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrders')
AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrders') AND name = 'CompanyId')
    ALTER TABLE PurchaseOrders ADD CompanyId INT NULL;
GO

-- 16. Create PurchaseOrderItems table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrderItems')
BEGIN
    CREATE TABLE PurchaseOrderItems (
        PurchaseOrderItemId INT PRIMARY KEY IDENTITY(1,1),
        PurchaseOrderId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) DEFAULT 0,
        Subtotal DECIMAL(18,2) DEFAULT 0
    );
    PRINT 'Created PurchaseOrderItems table';
END
GO

-- 17. ERPModules
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
    INSERT INTO ERPModules (ModuleName, ModuleCode, Description, MonthlyPrice, AnnualPrice, SortOrder) VALUES
    ('Sales Management', 'SALES', 'Sales pipeline', 49.99, 499.99, 1),
    ('Customer Support', 'SUPPORT', 'Ticket management', 39.99, 399.99, 2),
    ('Marketing', 'MARKETING', 'Campaign management', 44.99, 449.99, 3),
    ('Billing', 'BILLING', 'Invoice processing', 54.99, 549.99, 4),
    ('Inventory', 'INVENTORY', 'Product tracking', 44.99, 449.99, 5),
    ('Customers', 'CUSTOMERS', 'Customer profiles', 29.99, 299.99, 6);
    PRINT 'Created ERPModules';
END
GO

-- 18. CompanySubscriptions
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanySubscriptions')
BEGIN
    CREATE TABLE CompanySubscriptions (
        SubscriptionId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL,
        PlanName NVARCHAR(50) DEFAULT 'Basic',
        Status NVARCHAR(20) DEFAULT 'Active',
        BillingCycle NVARCHAR(20) DEFAULT 'Monthly',
        StartDate DATETIME2 DEFAULT GETUTCDATE(),
        EndDate DATETIME2,
        TrialEndDate DATETIME2,
        MonthlyFee DECIMAL(18,2) DEFAULT 0,
        MaxUsers INT DEFAULT 5,
        Notes NVARCHAR(500),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT,
        UpdatedBy INT
    );
    INSERT INTO CompanySubscriptions (CompanyId, PlanName, Status, BillingCycle, MonthlyFee, MaxUsers)
    VALUES (1, 'Enterprise', 'Active', 'Annual', 199.99, 50);
    PRINT 'Created CompanySubscriptions';
END
GO

-- 19. CompanyModuleAccess
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CompanyModuleAccess')
BEGIN
    CREATE TABLE CompanyModuleAccess (
        AccessId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL,
        ModuleId INT NOT NULL,
        IsEnabled BIT DEFAULT 1,
        ActivatedAt DATETIME2 DEFAULT GETUTCDATE(),
        DeactivatedAt DATETIME2,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_CompanyModule UNIQUE (CompanyId, ModuleId)
    );
    INSERT INTO CompanyModuleAccess (CompanyId, ModuleId, IsEnabled)
    SELECT 1, ModuleId, 1 FROM ERPModules;
    PRINT 'Created CompanyModuleAccess';
END
GO

-- 20. RoleModuleAccess
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RoleModuleAccess')
BEGIN
    CREATE TABLE RoleModuleAccess (
        Id INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL,
        RoleId INT NOT NULL,
        ModuleCode NVARCHAR(50) NOT NULL,
        HasAccess BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_RoleModuleAccess UNIQUE (CompanyId, RoleId, ModuleCode)
    );
    INSERT INTO RoleModuleAccess (CompanyId, RoleId, ModuleCode, HasAccess) VALUES
    (1, 2, 'SALES', 1), (1, 2, 'SUPPORT', 1), (1, 2, 'MARKETING', 1),
    (1, 2, 'BILLING', 1), (1, 2, 'INVENTORY', 1), (1, 2, 'CUSTOMERS', 1);
    PRINT 'Created RoleModuleAccess';
END
GO

-- 21. PlatformUsageLogs
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlatformUsageLogs')
BEGIN
    CREATE TABLE PlatformUsageLogs (
        LogId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NOT NULL,
        UserId INT,
        Action NVARCHAR(100) NOT NULL,
        Module NVARCHAR(50),
        Details NVARCHAR(500),
        IPAddress NVARCHAR(45),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created PlatformUsageLogs';
END
GO

-- 22. ApprovalRequests
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApprovalRequests')
BEGIN
    CREATE TABLE ApprovalRequests (
        RequestId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT,
        RequestType NVARCHAR(50) NOT NULL,
        RequestedBy INT NOT NULL,
        RequestDate DATETIME2 DEFAULT GETUTCDATE(),
        Status NVARCHAR(20) DEFAULT 'Pending',
        Priority NVARCHAR(20) DEFAULT 'Normal',
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX),
        EntityType NVARCHAR(50),
        EntityId INT,
        RequestData NVARCHAR(MAX),
        ReviewedBy INT,
        ReviewDate DATETIME2,
        ReviewNotes NVARCHAR(MAX),
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created ApprovalRequests';
END
GO

PRINT 'ALL MIGRATIONS COMPLETE';
GO
