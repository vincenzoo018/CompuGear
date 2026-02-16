-- ============================================
-- Migration Script: Customer Portal & Support Staff Features
-- Date: 2026-02-16
-- Description: Adds tables for e-commerce features, loyalty, reviews,
--              ticket enhancements, and live chat improvements
-- ============================================

-- ===========================================
-- 1. PRODUCT REVIEWS & RATINGS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductReviews')
BEGIN
    CREATE TABLE ProductReviews (
        ReviewId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        ProductId INT NOT NULL,
        CustomerId INT NOT NULL,
        OrderId INT NULL,
        Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
        Title NVARCHAR(100) NULL,
        Comment NVARCHAR(MAX) NULL,
        Pros NVARCHAR(500) NULL,
        Cons NVARCHAR(500) NULL,
        IsVerifiedPurchase BIT DEFAULT 0,
        HelpfulCount INT DEFAULT 0,
        Status NVARCHAR(20) DEFAULT 'Pending',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        ApprovedAt DATETIME2 NULL
    );
    PRINT 'Created ProductReviews table';
END

-- ===========================================
-- 2. PRODUCT COMPARISON
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductComparisons')
BEGIN
    CREATE TABLE ProductComparisons (
        ComparisonId INT IDENTITY(1,1) PRIMARY KEY,
        CustomerId INT NOT NULL,
        ProductId INT NOT NULL,
        AddedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created ProductComparisons table';
END

-- ===========================================
-- 3. RECENTLY VIEWED
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RecentlyViewed')
BEGIN
    CREATE TABLE RecentlyViewed (
        ViewId INT IDENTITY(1,1) PRIMARY KEY,
        CustomerId INT NOT NULL,
        ProductId INT NOT NULL,
        ViewedAt DATETIME2 DEFAULT GETUTCDATE(),
        ViewCount INT DEFAULT 1
    );
    PRINT 'Created RecentlyViewed table';
END

-- ===========================================
-- 4. LOYALTY PROGRAMS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyPrograms')
BEGIN
    CREATE TABLE LoyaltyPrograms (
        ProgramId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        ProgramName NVARCHAR(50) NOT NULL DEFAULT 'Default Rewards',
        PointsPerCurrency DECIMAL(18,2) DEFAULT 1,
        PointsValue DECIMAL(18,2) DEFAULT 0.01,
        MinRedeemPoints INT DEFAULT 100,
        PointsExpireDays INT NULL,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created LoyaltyPrograms table';
    
    -- Insert default loyalty program
    INSERT INTO LoyaltyPrograms (ProgramName, PointsPerCurrency, PointsValue, MinRedeemPoints) 
    VALUES ('CompuGear Rewards', 1, 0.01, 100);
END

-- ===========================================
-- 5. LOYALTY POINTS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoyaltyPoints')
BEGIN
    CREATE TABLE LoyaltyPoints (
        PointId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        CustomerId INT NOT NULL,
        TransactionType NVARCHAR(20) NOT NULL DEFAULT 'Earned',
        Points INT NOT NULL,
        OrderId INT NULL,
        Description NVARCHAR(255) NULL,
        ExpiresAt DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created LoyaltyPoints table';
END

-- ===========================================
-- 6. ORDER SHIPMENTS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderShipments')
BEGIN
    CREATE TABLE OrderShipments (
        ShipmentId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        OrderId INT NOT NULL,
        TrackingNumber NVARCHAR(50) NULL,
        Carrier NVARCHAR(50) NULL,
        Status NVARCHAR(20) DEFAULT 'Processing',
        ShippedAt DATETIME2 NULL,
        EstimatedDelivery DATETIME2 NULL,
        DeliveredAt DATETIME2 NULL,
        ShipToAddress NVARCHAR(255) NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created OrderShipments table';
END

-- ===========================================
-- 7. SHIPMENT TRACKING HISTORY
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShipmentTrackings')
BEGIN
    CREATE TABLE ShipmentTrackings (
        TrackingId INT IDENTITY(1,1) PRIMARY KEY,
        ShipmentId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Location NVARCHAR(255) NULL,
        Description NVARCHAR(500) NULL,
        Timestamp DATETIME2 DEFAULT GETUTCDATE(),
        FOREIGN KEY (ShipmentId) REFERENCES OrderShipments(ShipmentId)
    );
    PRINT 'Created ShipmentTrackings table';
END

-- ===========================================
-- 8. GIFT OPTIONS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GiftOptions')
BEGIN
    CREATE TABLE GiftOptions (
        GiftId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        OrderId INT NOT NULL,
        IsGift BIT DEFAULT 1,
        GiftWrap BIT DEFAULT 0,
        GiftWrapPrice DECIMAL(18,2) DEFAULT 0,
        GiftMessage NVARCHAR(500) NULL,
        RecipientName NVARCHAR(100) NULL,
        RecipientEmail NVARCHAR(100) NULL,
        HidePrice BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created GiftOptions table';
END

-- ===========================================
-- 9. INSTALLMENT PLANS (BUY NOW PAY LATER)
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstallmentPlans')
BEGIN
    CREATE TABLE InstallmentPlans (
        InstallmentId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        OrderId INT NOT NULL,
        CustomerId INT NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        NumberOfInstallments INT DEFAULT 3,
        InstallmentAmount DECIMAL(18,2) NOT NULL,
        InterestRate DECIMAL(5,2) DEFAULT 0,
        PaidInstallments INT DEFAULT 0,
        Status NVARCHAR(20) DEFAULT 'Active',
        StartDate DATETIME2 NOT NULL,
        NextDueDate DATETIME2 NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created InstallmentPlans table';
END

-- ===========================================
-- 10. INSTALLMENT PAYMENTS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'InstallmentPayments')
BEGIN
    CREATE TABLE InstallmentPayments (
        PaymentId INT IDENTITY(1,1) PRIMARY KEY,
        InstallmentId INT NOT NULL,
        InstallmentNumber INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        DueDate DATETIME2 NOT NULL,
        PaidAt DATETIME2 NULL,
        Status NVARCHAR(20) DEFAULT 'Pending',
        FOREIGN KEY (InstallmentId) REFERENCES InstallmentPlans(InstallmentId)
    );
    PRINT 'Created InstallmentPayments table';
END

-- ===========================================
-- 11. SUBSCRIPTION ORDERS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubscriptionOrders')
BEGIN
    CREATE TABLE SubscriptionOrders (
        SubscriptionId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        CustomerId INT NOT NULL,
        SubscriptionCode NVARCHAR(50) NOT NULL,
        Frequency NVARCHAR(20) DEFAULT 'Monthly',
        NextOrderDate DATETIME2 NOT NULL,
        LastOrderDate DATETIME2 NULL,
        LastOrderId INT NULL,
        EstimatedTotal DECIMAL(18,2) DEFAULT 0,
        ShippingAddressId INT NULL,
        Status NVARCHAR(20) DEFAULT 'Active',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created SubscriptionOrders table';
END

-- ===========================================
-- 12. SUBSCRIPTION ITEMS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubscriptionItems')
BEGIN
    CREATE TABLE SubscriptionItems (
        ItemId INT IDENTITY(1,1) PRIMARY KEY,
        SubscriptionId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT DEFAULT 1,
        UnitPrice DECIMAL(18,2) NOT NULL,
        FOREIGN KEY (SubscriptionId) REFERENCES SubscriptionOrders(SubscriptionId)
    );
    PRINT 'Created SubscriptionItems table';
END

-- ===========================================
-- 13. TICKET ASSIGNMENT RULES
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketAssignmentRules')
BEGIN
    CREATE TABLE TicketAssignmentRules (
        RuleId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        RuleName NVARCHAR(100) NOT NULL,
        AssignmentType NVARCHAR(20) DEFAULT 'RoundRobin',
        CategoryId INT NULL,
        RequiredSkill NVARCHAR(50) NULL,
        Priority INT DEFAULT 0,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created TicketAssignmentRules table';
    
    -- Insert default rule
    INSERT INTO TicketAssignmentRules (RuleName, AssignmentType) 
    VALUES ('Default Round Robin', 'RoundRobin');
END

-- ===========================================
-- 14. ASSIGNMENT RULE AGENTS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AssignmentRuleAgents')
BEGIN
    CREATE TABLE AssignmentRuleAgents (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RuleId INT NOT NULL,
        UserId INT NOT NULL,
        MaxTickets INT NULL,
        IsAvailable BIT DEFAULT 1,
        FOREIGN KEY (RuleId) REFERENCES TicketAssignmentRules(RuleId)
    );
    PRINT 'Created AssignmentRuleAgents table';
END

-- ===========================================
-- 15. SATISFACTION SURVEYS (CSAT)
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SatisfactionSurveys')
BEGIN
    CREATE TABLE SatisfactionSurveys (
        SurveyId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        TicketId INT NOT NULL,
        CustomerId INT NOT NULL,
        AgentId INT NULL,
        Rating INT NULL CHECK (Rating >= 1 AND Rating <= 5),
        Sentiment NVARCHAR(20) NULL,
        Feedback NVARCHAR(MAX) NULL,
        WouldRecommend BIT DEFAULT 1,
        SentAt DATETIME2 DEFAULT GETUTCDATE(),
        RespondedAt DATETIME2 NULL
    );
    PRINT 'Created SatisfactionSurveys table';
END

-- ===========================================
-- 16. TICKET NOTES (INTERNAL)
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketNotes')
BEGIN
    CREATE TABLE TicketNotes (
        NoteId INT IDENTITY(1,1) PRIMARY KEY,
        TicketId INT NOT NULL,
        UserId INT NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        IsPrivate BIT DEFAULT 1,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created TicketNotes table';
END

-- ===========================================
-- 17. TICKET TAGS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketTags')
BEGIN
    CREATE TABLE TicketTags (
        TagId INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NULL,
        TagName NVARCHAR(50) NOT NULL,
        Color NVARCHAR(20) DEFAULT '#6c757d',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created TicketTags table';
    
    -- Insert sample tags
    INSERT INTO TicketTags (TagName, Color) VALUES
    ('Urgent', '#dc3545'),
    ('VIP Customer', '#ffc107'),
    ('Technical', '#17a2b8'),
    ('Billing Issue', '#28a745'),
    ('Follow-up', '#6f42c1');
END

-- ===========================================
-- 18. TICKET TAG MAPPINGS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketTagMappings')
BEGIN
    CREATE TABLE TicketTagMappings (
        MappingId INT IDENTITY(1,1) PRIMARY KEY,
        TicketId INT NOT NULL,
        TagId INT NOT NULL,
        AddedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created TicketTagMappings table';
END

-- ===========================================
-- 19. TICKET TIME ENTRIES
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketTimeEntries')
BEGIN
    CREATE TABLE TicketTimeEntries (
        EntryId INT IDENTITY(1,1) PRIMARY KEY,
        TicketId INT NOT NULL,
        UserId INT NOT NULL,
        StartTime DATETIME2 NOT NULL,
        EndTime DATETIME2 NULL,
        DurationMinutes INT DEFAULT 0,
        Description NVARCHAR(255) NULL,
        IsBillable BIT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created TicketTimeEntries table';
END

-- ===========================================
-- 20. TICKET LINKS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketLinks')
BEGIN
    CREATE TABLE TicketLinks (
        LinkId INT IDENTITY(1,1) PRIMARY KEY,
        TicketId INT NOT NULL,
        LinkedTicketId INT NOT NULL,
        LinkType NVARCHAR(20) DEFAULT 'Related',
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL
    );
    PRINT 'Created TicketLinks table';
END

-- ===========================================
-- 21. TICKET MERGES
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketMerges')
BEGIN
    CREATE TABLE TicketMerges (
        MergeId INT IDENTITY(1,1) PRIMARY KEY,
        PrimaryTicketId INT NOT NULL,
        MergedTicketId INT NOT NULL,
        MergedBy INT NOT NULL,
        MergedAt DATETIME2 DEFAULT GETUTCDATE(),
        MergeReason NVARCHAR(255) NULL
    );
    PRINT 'Created TicketMerges table';
END

-- ===========================================
-- 22. CHAT TRANSFERS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatTransfers')
BEGIN
    CREATE TABLE ChatTransfers (
        TransferId INT IDENTITY(1,1) PRIMARY KEY,
        ChatSessionId INT NOT NULL,
        FromUserId INT NOT NULL,
        ToUserId INT NOT NULL,
        Reason NVARCHAR(255) NULL,
        TransferredAt DATETIME2 DEFAULT GETUTCDATE(),
        Accepted BIT DEFAULT 0,
        AcceptedAt DATETIME2 NULL
    );
    PRINT 'Created ChatTransfers table';
END

-- ===========================================
-- 23. CHAT ATTACHMENTS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatAttachments')
BEGIN
    CREATE TABLE ChatAttachments (
        AttachmentId INT IDENTITY(1,1) PRIMARY KEY,
        ChatMessageId INT NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(100) NULL,
        FileSize BIGINT DEFAULT 0,
        UploadedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'Created ChatAttachments table';
END

-- ===========================================
-- 24. CHAT TRANSCRIPTS
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatTranscripts')
BEGIN
    CREATE TABLE ChatTranscripts (
        TranscriptId INT IDENTITY(1,1) PRIMARY KEY,
        ChatSessionId INT NOT NULL,
        RecipientEmail NVARCHAR(100) NOT NULL,
        SentAt DATETIME2 DEFAULT GETUTCDATE(),
        DeliverySuccess BIT DEFAULT 1
    );
    PRINT 'Created ChatTranscripts table';
END

PRINT '';
PRINT '============================================';
PRINT 'Customer Portal & Support Staff Migration completed!';
PRINT '============================================';
