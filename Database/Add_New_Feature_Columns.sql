-- ============================================
-- Migration Script: Add New Feature Columns
-- Date: 2026-02-16
-- Description: Adds columns for Credit Management, Tax, Batch Tracking, etc.
-- ============================================

-- ===========================================
-- 1. CUSTOMER CREDIT MANAGEMENT COLUMNS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Customers') AND name = 'CreditLimit')
BEGIN
    ALTER TABLE Customers ADD CreditLimit DECIMAL(18,2) DEFAULT 0;
    PRINT 'Added CreditLimit to Customers';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Customers') AND name = 'CreditUsed')
BEGIN
    ALTER TABLE Customers ADD CreditUsed DECIMAL(18,2) DEFAULT 0;
    PRINT 'Added CreditUsed to Customers';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Customers') AND name = 'CreditBalance')
BEGIN
    ALTER TABLE Customers ADD CreditBalance DECIMAL(18,2) DEFAULT 0;
    PRINT 'Added CreditBalance to Customers';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Customers') AND name = 'CreditStatus')
BEGIN
    ALTER TABLE Customers ADD CreditStatus NVARCHAR(20) DEFAULT 'Good';
    PRINT 'Added CreditStatus to Customers';
END

-- ===========================================
-- 2. PRODUCT BATCH/SERIAL TRACKING COLUMNS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Products') AND name = 'TrackBatchNumbers')
BEGIN
    ALTER TABLE Products ADD TrackBatchNumbers BIT DEFAULT 0;
    PRINT 'Added TrackBatchNumbers to Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Products') AND name = 'TrackSerialNumbers')
BEGIN
    ALTER TABLE Products ADD TrackSerialNumbers BIT DEFAULT 0;
    PRINT 'Added TrackSerialNumbers to Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Products') AND name = 'TrackExpiry')
BEGIN
    ALTER TABLE Products ADD TrackExpiry BIT DEFAULT 0;
    PRINT 'Added TrackExpiry to Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Products') AND name = 'AutoReorderEnabled')
BEGIN
    ALTER TABLE Products ADD AutoReorderEnabled BIT DEFAULT 0;
    PRINT 'Added AutoReorderEnabled to Products';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Products') AND name = 'AutoReorderQuantity')
BEGIN
    ALTER TABLE Products ADD AutoReorderQuantity INT NULL;
    PRINT 'Added AutoReorderQuantity to Products';
END

-- ===========================================
-- 3. NEW TABLES FOR SALES MODULE
-- ===========================================

-- Quotations Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Quotations')
BEGIN
    CREATE TABLE Quotations (
        QuotationId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        QuotationNumber NVARCHAR(30) NOT NULL,
        CustomerId INT NOT NULL,
        QuotationDate DATETIME2 DEFAULT GETUTCDATE(),
        ValidUntil DATETIME2 NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        DiscountAmount DECIMAL(18,2) DEFAULT 0,
        TaxAmount DECIMAL(18,2) DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) DEFAULT 'Draft',
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL,
        ConvertedToOrderId INT NULL
    );
    PRINT 'Created Quotations table';
END

-- QuotationItems Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuotationItems')
BEGIN
    CREATE TABLE QuotationItems (
        ItemId INT IDENTITY(1,1) PRIMARY KEY,
        QuotationId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL DEFAULT 1,
        UnitPrice DECIMAL(18,2) NOT NULL,
        DiscountPercent DECIMAL(5,2) DEFAULT 0,
        Subtotal DECIMAL(18,2) NOT NULL,
        FOREIGN KEY (QuotationId) REFERENCES Quotations(QuotationId)
    );
    PRINT 'Created QuotationItems table';
END

-- SalesTargets Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SalesTargets')
BEGIN
    CREATE TABLE SalesTargets (
        TargetId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        UserId INT NOT NULL,
        TargetPeriod NVARCHAR(20) NOT NULL,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        TargetAmount DECIMAL(18,2) NOT NULL,
        AchievedAmount DECIMAL(18,2) DEFAULT 0,
        TargetType NVARCHAR(20) DEFAULT 'Revenue',
        Status NVARCHAR(20) DEFAULT 'Active',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created SalesTargets table';
END

-- Commissions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Commissions')
BEGIN
    CREATE TABLE Commissions (
        CommissionId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        UserId INT NOT NULL,
        OrderId INT NULL,
        CommissionRate DECIMAL(5,2) NOT NULL,
        OrderAmount DECIMAL(18,2) NOT NULL,
        CommissionAmount DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(20) DEFAULT 'Pending',
        PaidAt DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created Commissions table';
END

-- PipelineStages Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PipelineStages')
BEGIN
    CREATE TABLE PipelineStages (
        StageId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        StageName NVARCHAR(50) NOT NULL,
        StageOrder INT NOT NULL DEFAULT 0,
        Color NVARCHAR(20) DEFAULT '#3498db',
        Description NVARCHAR(255) NULL,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created PipelineStages table';
    
    -- Insert default stages
    INSERT INTO PipelineStages (StageName, StageOrder, Color) VALUES
    ('New Lead', 1, '#3498db'),
    ('Contacted', 2, '#f39c12'),
    ('Qualified', 3, '#9b59b6'),
    ('Proposal Sent', 4, '#e74c3c'),
    ('Negotiation', 5, '#1abc9c'),
    ('Won', 6, '#27ae60'),
    ('Lost', 7, '#95a5a6');
END

-- ===========================================
-- 4. NEW TABLES FOR INVENTORY MODULE
-- ===========================================

-- ProductBatches Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductBatches')
BEGIN
    CREATE TABLE ProductBatches (
        BatchId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        ProductId INT NOT NULL,
        BatchNumber NVARCHAR(50) NOT NULL,
        SerialNumber NVARCHAR(100) NULL,
        Quantity INT DEFAULT 0,
        RemainingQuantity INT DEFAULT 0,
        CostPrice DECIMAL(18,2) DEFAULT 0,
        ManufactureDate DATETIME2 NULL,
        ExpiryDate DATETIME2 NULL,
        ReceivedDate DATETIME2 DEFAULT GETUTCDATE(),
        Status NVARCHAR(20) DEFAULT 'Active',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created ProductBatches table';
END

-- AutoReorderRules Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AutoReorderRules')
BEGIN
    CREATE TABLE AutoReorderRules (
        RuleId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        ProductId INT NOT NULL,
        SupplierId INT NULL,
        ReorderPoint INT DEFAULT 10,
        ReorderQuantity INT DEFAULT 50,
        MaxStockLevel INT DEFAULT 200,
        IsEnabled BIT DEFAULT 1,
        LastTriggeredAt DATETIME2 NULL,
        LastPurchaseOrderId INT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created AutoReorderRules table';
END

-- ExpiryAlerts Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpiryAlerts')
BEGIN
    CREATE TABLE ExpiryAlerts (
        AlertId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        BatchId INT NOT NULL,
        ProductId INT NOT NULL,
        ExpiryDate DATETIME2 NOT NULL,
        DaysBeforeExpiry INT NOT NULL,
        AlertStatus NVARCHAR(20) DEFAULT 'Pending',
        AlertTriggeredAt DATETIME2 DEFAULT GETUTCDATE(),
        AcknowledgedAt DATETIME2 NULL,
        AcknowledgedBy INT NULL,
        Notes NVARCHAR(500) NULL
    );
    PRINT 'Created ExpiryAlerts table';
END

-- ===========================================
-- 5. NEW TABLES FOR SUPPORT MODULE
-- ===========================================

-- CannedResponses Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CannedResponses')
BEGIN
    CREATE TABLE CannedResponses (
        ResponseId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        Title NVARCHAR(100) NOT NULL,
        Shortcut NVARCHAR(20) NULL,
        Content NVARCHAR(MAX) NOT NULL,
        Category NVARCHAR(50) NULL,
        IsActive BIT DEFAULT 1,
        UsageCount INT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL
    );
    PRINT 'Created CannedResponses table';
    
    -- Insert sample canned responses
    INSERT INTO CannedResponses (Title, Shortcut, Content, Category) VALUES
    ('Greeting', '/hi', 'Hello! Thank you for contacting CompuGear support. How can I assist you today?', 'General'),
    ('Request Details', '/details', 'Could you please provide more details about the issue you are experiencing?', 'General'),
    ('Warranty Info', '/warranty', 'Our products come with a standard 1-year warranty covering manufacturing defects.', 'Warranty'),
    ('Closing', '/bye', 'Thank you for contacting us. Is there anything else I can help you with?', 'General');
END

-- SLAConfigs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SLAConfigs')
BEGIN
    CREATE TABLE SLAConfigs (
        SLAId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        SLAName NVARCHAR(50) NOT NULL,
        Priority NVARCHAR(20) NOT NULL,
        FirstResponseHours INT NOT NULL DEFAULT 24,
        ResolutionHours INT NOT NULL DEFAULT 72,
        EscalateOnBreach BIT DEFAULT 1,
        EscalateToUserId INT NULL,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created SLAConfigs table';
    
    -- Insert default SLA configs
    INSERT INTO SLAConfigs (SLAName, Priority, FirstResponseHours, ResolutionHours) VALUES
    ('Critical SLA', 'Critical', 1, 4),
    ('High Priority SLA', 'High', 4, 24),
    ('Normal SLA', 'Normal', 24, 72),
    ('Low Priority SLA', 'Low', 48, 168);
END

-- ===========================================
-- 6. NEW TABLES FOR MARKETING MODULE
-- ===========================================

-- ABTests Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ABTests')
BEGIN
    CREATE TABLE ABTests (
        TestId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        TestName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        CampaignId INT NULL,
        TestType NVARCHAR(20) DEFAULT 'Email',
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NULL,
        Status NVARCHAR(20) DEFAULT 'Draft',
        WinningVariantId INT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL
    );
    PRINT 'Created ABTests table';
END

-- ABTestVariants Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ABTestVariants')
BEGIN
    CREATE TABLE ABTestVariants (
        VariantId INT IDENTITY(1,1) PRIMARY KEY,
        TestId INT NOT NULL,
        VariantName NVARCHAR(10) NOT NULL,
        Subject NVARCHAR(200) NULL,
        Content NVARCHAR(MAX) NULL,
        TrafficPercentage DECIMAL(5,2) DEFAULT 50,
        SentCount INT DEFAULT 0,
        OpenCount INT DEFAULT 0,
        ClickCount INT DEFAULT 0,
        ConversionCount INT DEFAULT 0,
        FOREIGN KEY (TestId) REFERENCES ABTests(TestId)
    );
    PRINT 'Created ABTestVariants table';
END

-- SocialMediaPosts Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SocialMediaPosts')
BEGIN
    CREATE TABLE SocialMediaPosts (
        PostId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        CampaignId INT NULL,
        Platform NVARCHAR(20) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        MediaUrl NVARCHAR(500) NULL,
        ScheduledAt DATETIME2 NULL,
        PublishedAt DATETIME2 NULL,
        Status NVARCHAR(20) DEFAULT 'Draft',
        ExternalPostId NVARCHAR(255) NULL,
        Likes INT DEFAULT 0,
        Shares INT DEFAULT 0,
        Comments INT DEFAULT 0,
        Reach INT DEFAULT 0,
        Impressions INT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL
    );
    PRINT 'Created SocialMediaPosts table';
END

-- ===========================================
-- 7. NEW TABLES FOR BILLING MODULE
-- ===========================================

-- TaxRates Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaxRates')
BEGIN
    CREATE TABLE TaxRates (
        TaxRateId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        TaxName NVARCHAR(50) NOT NULL,
        TaxCode NVARCHAR(20) NULL,
        Rate DECIMAL(5,2) NOT NULL,
        TaxType NVARCHAR(20) DEFAULT 'Percentage',
        Region NVARCHAR(50) NULL,
        Country NVARCHAR(50) NULL,
        IsDefault BIT DEFAULT 0,
        IsActive BIT DEFAULT 1,
        ApplyToShipping BIT DEFAULT 0,
        EffectiveFrom DATETIME2 DEFAULT GETUTCDATE(),
        EffectiveTo DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created TaxRates table';
    
    -- Insert sample tax rates
    INSERT INTO TaxRates (TaxName, TaxCode, Rate, IsDefault) VALUES
    ('Standard VAT', 'VAT12', 12.00, 1),
    ('Zero Rate', 'VAT0', 0.00, 0);
END

-- ProductTaxCategories Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductTaxCategories')
BEGIN
    CREATE TABLE ProductTaxCategories (
        CategoryId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        CategoryName NVARCHAR(50) NOT NULL,
        Description NVARCHAR(255) NULL,
        DefaultTaxRateId INT NULL,
        IsTaxExempt BIT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created ProductTaxCategories table';
END

-- TaxCalculations Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaxCalculations')
BEGIN
    CREATE TABLE TaxCalculations (
        CalculationId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        InvoiceId INT NULL,
        OrderId INT NULL,
        TaxRateId INT NOT NULL,
        TaxableAmount DECIMAL(18,2) NOT NULL,
        AppliedRate DECIMAL(5,2) NOT NULL,
        TaxAmount DECIMAL(18,2) NOT NULL,
        Notes NVARCHAR(500) NULL,
        CalculatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created TaxCalculations table';
END

PRINT '';
PRINT '============================================';
PRINT 'Migration completed successfully!';
PRINT '============================================';
