-- =====================================================
-- CompuGear CRM Database Schema
-- Database Name: Compugear
-- Version: 1.0
-- Description: Complete database schema for CompuGear CRM
-- Supports 7 Actor Types:
--   1. Super Admin
--   2. Company Admin
--   3. Sales Staff
--   4. Customer Support Staff
--   5. Marketing Staff
--   6. Accounting & Billing Staff
--   7. Customer
-- =====================================================

-- Create Database
USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'Compugear')
BEGIN
    ALTER DATABASE Compugear SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Compugear;
END
GO

CREATE DATABASE Compugear;
GO

USE Compugear;
GO

-- =====================================================
-- CORE TABLES
-- =====================================================

-- Roles Table (7 Actor Types)
CREATE TABLE Roles (
    RoleId INT PRIMARY KEY IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255),
    AccessLevel INT DEFAULT 1, -- 1-7, 7 being highest (Super Admin)
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Insert Default Roles
INSERT INTO Roles (RoleName, Description, AccessLevel) VALUES
('Super Admin', 'Full system access with all privileges', 7),
('Company Admin', 'Company-wide administrative access', 6),
('Sales Staff', 'Sales and order management access', 5),
('Customer Support Staff', 'Customer support and ticket management', 4),
('Marketing Staff', 'Marketing campaigns and promotions management', 3),
('Accounting & Billing Staff', 'Financial and billing management', 2),
('Customer', 'Customer portal access', 1);

-- Companies Table
CREATE TABLE Companies (
    CompanyId INT PRIMARY KEY IDENTITY(1,1),
    CompanyName NVARCHAR(100) NOT NULL,
    CompanyCode NVARCHAR(20) UNIQUE,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    City NVARCHAR(50),
    Country NVARCHAR(50),
    Logo NVARCHAR(255),
    Website NVARCHAR(255),
    TaxId NVARCHAR(50),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Insert Default Company
INSERT INTO Companies (CompanyName, CompanyCode, Email, Phone, Address, City, Country)
VALUES ('CompuGear Technologies', 'CGT-001', 'info@compugear.com', '+1-555-0100', '123 Tech Avenue', 'Manila', 'Philippines');

-- Users Table
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Salt NVARCHAR(255),
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Phone NVARCHAR(20),
    Avatar NVARCHAR(255),
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId),
    CompanyId INT FOREIGN KEY REFERENCES Companies(CompanyId),
    IsActive BIT DEFAULT 1,
    IsEmailVerified BIT DEFAULT 0,
    LastLoginAt DATETIME2,
    FailedLoginAttempts INT DEFAULT 0,
    LockoutEnd DATETIME2,
    PasswordChangedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT,
    UpdatedBy INT
);

-- Permissions Table
CREATE TABLE Permissions (
    PermissionId INT PRIMARY KEY IDENTITY(1,1),
    PermissionName NVARCHAR(50) NOT NULL UNIQUE,
    Module NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Insert Default Permissions
INSERT INTO Permissions (PermissionName, Module, Description) VALUES
-- User Management
('users.view', 'User Management', 'View user accounts'),
('users.create', 'User Management', 'Create new users'),
('users.edit', 'User Management', 'Edit user accounts'),
('users.delete', 'User Management', 'Delete users'),
('roles.manage', 'User Management', 'Manage roles and permissions'),
-- Customer Management
('customers.view', 'Customer Management', 'View customer profiles'),
('customers.create', 'Customer Management', 'Create new customers'),
('customers.edit', 'Customer Management', 'Edit customer information'),
('customers.delete', 'Customer Management', 'Delete customers'),
-- Sales Management
('sales.view', 'Sales Management', 'View orders and sales'),
('sales.create', 'Sales Management', 'Create orders'),
('sales.edit', 'Sales Management', 'Edit orders'),
('sales.delete', 'Sales Management', 'Delete orders'),
('leads.manage', 'Sales Management', 'Manage sales leads'),
-- Inventory Management
('inventory.view', 'Inventory Management', 'View products and stock'),
('inventory.create', 'Inventory Management', 'Add new products'),
('inventory.edit', 'Inventory Management', 'Edit products'),
('inventory.delete', 'Inventory Management', 'Delete products'),
('stock.manage', 'Inventory Management', 'Manage stock levels'),
-- Support Management
('support.view', 'Support Management', 'View support tickets'),
('support.create', 'Support Management', 'Create tickets'),
('support.edit', 'Support Management', 'Edit tickets'),
('support.resolve', 'Support Management', 'Resolve tickets'),
('knowledge.manage', 'Support Management', 'Manage knowledge base'),
-- Marketing Management
('marketing.view', 'Marketing Management', 'View campaigns'),
('marketing.create', 'Marketing Management', 'Create campaigns'),
('marketing.edit', 'Marketing Management', 'Edit campaigns'),
('marketing.delete', 'Marketing Management', 'Delete campaigns'),
('promotions.manage', 'Marketing Management', 'Manage promotions'),
-- Billing Management
('billing.view', 'Billing Management', 'View invoices and payments'),
('billing.create', 'Billing Management', 'Create invoices'),
('billing.edit', 'Billing Management', 'Edit invoices'),
('billing.delete', 'Billing Management', 'Delete invoices'),
('payments.process', 'Billing Management', 'Process payments'),
-- Settings
('settings.view', 'Settings', 'View system settings'),
('settings.edit', 'Settings', 'Edit system settings'),
-- Reports
('reports.view', 'Reports', 'View reports'),
('reports.export', 'Reports', 'Export reports');

-- Role Permissions Junction Table
CREATE TABLE RolePermissions (
    RolePermissionId INT PRIMARY KEY IDENTITY(1,1),
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(RoleId) ON DELETE CASCADE,
    PermissionId INT NOT NULL FOREIGN KEY REFERENCES Permissions(PermissionId) ON DELETE CASCADE,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT UQ_RolePermission UNIQUE (RoleId, PermissionId)
);

-- Grant all permissions to Super Admin (RoleId = 1)
INSERT INTO RolePermissions (RoleId, PermissionId)
SELECT 1, PermissionId FROM Permissions;

-- =====================================================
-- CUSTOMER MODULE TABLES
-- =====================================================

-- Customer Categories
CREATE TABLE CustomerCategories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255),
    DiscountPercent DECIMAL(5,2) DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

INSERT INTO CustomerCategories (CategoryName, Description, DiscountPercent) VALUES
('Standard', 'Regular customers', 0),
('Premium', 'Premium loyalty customers', 5),
('VIP', 'VIP customers with highest priority', 10),
('Corporate', 'Corporate/Business accounts', 7),
('Wholesale', 'Wholesale buyers', 15);

-- Customers Table
CREATE TABLE Customers (
    CustomerId INT PRIMARY KEY IDENTITY(1,1),
    CustomerCode NVARCHAR(20) UNIQUE,
    UserId INT FOREIGN KEY REFERENCES Users(UserId), -- Link to user account if registered
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    Avatar NVARCHAR(255),
    CategoryId INT FOREIGN KEY REFERENCES CustomerCategories(CategoryId),
    -- Address Information
    BillingAddress NVARCHAR(255),
    BillingCity NVARCHAR(50),
    BillingState NVARCHAR(50),
    BillingZipCode NVARCHAR(20),
    BillingCountry NVARCHAR(50),
    ShippingAddress NVARCHAR(255),
    ShippingCity NVARCHAR(50),
    ShippingState NVARCHAR(50),
    ShippingZipCode NVARCHAR(20),
    ShippingCountry NVARCHAR(50),
    -- Business Info (for corporate)
    CompanyName NVARCHAR(100),
    TaxId NVARCHAR(50),
    -- Status
    Status NVARCHAR(20) DEFAULT 'Active', -- Active, Inactive, Blocked
    TotalOrders INT DEFAULT 0,
    TotalSpent DECIMAL(18,2) DEFAULT 0,
    LoyaltyPoints INT DEFAULT 0,
    Notes NVARCHAR(MAX),
    -- Preferences
    PreferredContactMethod NVARCHAR(20), -- Email, Phone, SMS
    MarketingOptIn BIT DEFAULT 1,
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Customer Addresses (Multiple addresses per customer)
CREATE TABLE CustomerAddresses (
    AddressId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId) ON DELETE CASCADE,
    AddressType NVARCHAR(20) NOT NULL, -- Billing, Shipping, Both
    AddressLine1 NVARCHAR(255) NOT NULL,
    AddressLine2 NVARCHAR(255),
    City NVARCHAR(50) NOT NULL,
    State NVARCHAR(50),
    ZipCode NVARCHAR(20),
    Country NVARCHAR(50) NOT NULL,
    IsDefault BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- =====================================================
-- INVENTORY MODULE TABLES
-- =====================================================

-- Product Categories
CREATE TABLE ProductCategories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    ParentCategoryId INT FOREIGN KEY REFERENCES ProductCategories(CategoryId),
    Description NVARCHAR(255),
    ImageUrl NVARCHAR(255),
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

INSERT INTO ProductCategories (CategoryName, Description) VALUES
('Computer Systems', 'Desktop PCs, laptops, and complete systems'),
('Components', 'Computer hardware components'),
('Peripherals', 'Keyboards, mice, monitors, etc.'),
('Networking', 'Routers, switches, cables'),
('Software', 'Operating systems and applications'),
('Accessories', 'Cases, bags, cleaning supplies'),
('Gaming', 'Gaming gear and equipment'),
('Storage', 'Hard drives, SSDs, USB drives');

-- Brands Table
CREATE TABLE Brands (
    BrandId INT PRIMARY KEY IDENTITY(1,1),
    BrandName NVARCHAR(100) NOT NULL UNIQUE,
    Logo NVARCHAR(255),
    Website NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

INSERT INTO Brands (BrandName) VALUES
('Intel'), ('AMD'), ('NVIDIA'), ('ASUS'), ('MSI'), ('Gigabyte'),
('Corsair'), ('Kingston'), ('Samsung'), ('Western Digital'),
('Logitech'), ('Razer'), ('HP'), ('Dell'), ('Lenovo'), ('Acer');

-- Products Table
CREATE TABLE Products (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    ProductCode NVARCHAR(50) NOT NULL UNIQUE,
    SKU NVARCHAR(50) UNIQUE,
    Barcode NVARCHAR(50),
    ProductName NVARCHAR(200) NOT NULL,
    ShortDescription NVARCHAR(500),
    FullDescription NVARCHAR(MAX),
    CategoryId INT FOREIGN KEY REFERENCES ProductCategories(CategoryId),
    BrandId INT FOREIGN KEY REFERENCES Brands(BrandId),
    -- Pricing
    CostPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
    SellingPrice DECIMAL(18,2) NOT NULL,
    CompareAtPrice DECIMAL(18,2), -- Original price for showing discounts
    -- Stock
    StockQuantity INT DEFAULT 0,
    ReorderLevel INT DEFAULT 10,
    MaxStockLevel INT DEFAULT 1000,
    -- Product Details
    Weight DECIMAL(10,2), -- in kg
    Length DECIMAL(10,2), -- in cm
    Width DECIMAL(10,2),
    Height DECIMAL(10,2),
    -- Images
    MainImageUrl NVARCHAR(255),
    -- Status
    Status NVARCHAR(20) DEFAULT 'Active', -- Active, Inactive, Discontinued
    IsFeatured BIT DEFAULT 0,
    IsOnSale BIT DEFAULT 0,
    -- Warranty
    WarrantyPeriod INT, -- in months
    -- SEO
    MetaTitle NVARCHAR(200),
    MetaDescription NVARCHAR(500),
    MetaKeywords NVARCHAR(500),
    Slug NVARCHAR(200),
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Product Images
CREATE TABLE ProductImages (
    ImageId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId) ON DELETE CASCADE,
    ImageUrl NVARCHAR(255) NOT NULL,
    AltText NVARCHAR(200),
    DisplayOrder INT DEFAULT 0,
    IsMain BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Product Specifications
CREATE TABLE ProductSpecifications (
    SpecId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId) ON DELETE CASCADE,
    SpecName NVARCHAR(100) NOT NULL,
    SpecValue NVARCHAR(255) NOT NULL,
    DisplayOrder INT DEFAULT 0
);

-- Inventory Transactions (Stock movements)
CREATE TABLE InventoryTransactions (
    TransactionId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    TransactionType NVARCHAR(30) NOT NULL, -- Purchase, Sale, Return, Adjustment, Transfer, Damaged
    Quantity INT NOT NULL,
    PreviousStock INT NOT NULL,
    NewStock INT NOT NULL,
    UnitCost DECIMAL(18,2),
    TotalCost DECIMAL(18,2),
    ReferenceType NVARCHAR(50), -- Order, PurchaseOrder, Adjustment
    ReferenceId INT,
    Notes NVARCHAR(500),
    TransactionDate DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Stock Alerts
CREATE TABLE StockAlerts (
    AlertId INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    AlertType NVARCHAR(30) NOT NULL, -- LowStock, OutOfStock, Overstock
    CurrentStock INT NOT NULL,
    ThresholdLevel INT NOT NULL,
    IsResolved BIT DEFAULT 0,
    ResolvedAt DATETIME2,
    ResolvedBy INT FOREIGN KEY REFERENCES Users(UserId),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Suppliers
CREATE TABLE Suppliers (
    SupplierId INT PRIMARY KEY IDENTITY(1,1),
    SupplierCode NVARCHAR(20) UNIQUE,
    SupplierName NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(100),
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    Address NVARCHAR(255),
    City NVARCHAR(50),
    Country NVARCHAR(50),
    Website NVARCHAR(255),
    PaymentTerms NVARCHAR(100),
    Status NVARCHAR(20) DEFAULT 'Active',
    Rating INT, -- 1-5
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- =====================================================
-- SALES MODULE TABLES
-- =====================================================

-- Sales Leads
CREATE TABLE Leads (
    LeadId INT PRIMARY KEY IDENTITY(1,1),
    LeadCode NVARCHAR(20) UNIQUE,
    -- Contact Info
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100),
    Phone NVARCHAR(20),
    CompanyName NVARCHAR(100),
    JobTitle NVARCHAR(100),
    -- Lead Details
    Source NVARCHAR(50), -- Website, Referral, Social Media, Walk-in, Phone, Email
    Status NVARCHAR(30) DEFAULT 'New', -- New, Contacted, Qualified, Proposal, Negotiation, Won, Lost
    Priority NVARCHAR(20) DEFAULT 'Medium', -- Low, Medium, High, Critical
    EstimatedValue DECIMAL(18,2),
    Probability INT, -- 0-100%
    ExpectedCloseDate DATE,
    -- Assignment
    AssignedTo INT FOREIGN KEY REFERENCES Users(UserId),
    -- Notes
    Description NVARCHAR(MAX),
    Notes NVARCHAR(MAX),
    -- Conversion
    IsConverted BIT DEFAULT 0,
    ConvertedCustomerId INT FOREIGN KEY REFERENCES Customers(CustomerId),
    ConvertedAt DATETIME2,
    -- Timestamps
    LastContactedAt DATETIME2,
    NextFollowUp DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Orders Table
CREATE TABLE Orders (
    OrderId INT PRIMARY KEY IDENTITY(1,1),
    OrderNumber NVARCHAR(30) NOT NULL UNIQUE,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    OrderDate DATETIME2 DEFAULT GETDATE(),
    -- Status
    OrderStatus NVARCHAR(30) DEFAULT 'Pending', -- Pending, Confirmed, Processing, Shipped, Delivered, Cancelled, Returned
    PaymentStatus NVARCHAR(30) DEFAULT 'Pending', -- Pending, Partial, Paid, Refunded
    -- Pricing
    Subtotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    DiscountPercent DECIMAL(5,2) DEFAULT 0,
    TaxAmount DECIMAL(18,2) DEFAULT 0,
    ShippingAmount DECIMAL(18,2) DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    -- Payment
    PaymentMethod NVARCHAR(50), -- Cash, Credit Card, Debit Card, PayMongo, Bank Transfer, COD
    PaymentReference NVARCHAR(100),
    PaidAmount DECIMAL(18,2) DEFAULT 0,
    -- Shipping Info
    ShippingMethod NVARCHAR(50),
    TrackingNumber NVARCHAR(100),
    ShippingAddress NVARCHAR(255),
    ShippingCity NVARCHAR(50),
    ShippingState NVARCHAR(50),
    ShippingZipCode NVARCHAR(20),
    ShippingCountry NVARCHAR(50),
    -- Billing Info
    BillingAddress NVARCHAR(255),
    BillingCity NVARCHAR(50),
    BillingState NVARCHAR(50),
    BillingZipCode NVARCHAR(20),
    BillingCountry NVARCHAR(50),
    -- Additional
    Notes NVARCHAR(MAX),
    InternalNotes NVARCHAR(MAX),
    -- Assignment
    AssignedTo INT FOREIGN KEY REFERENCES Users(UserId),
    -- Dates
    ConfirmedAt DATETIME2,
    ShippedAt DATETIME2,
    DeliveredAt DATETIME2,
    CancelledAt DATETIME2,
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Order Items
CREATE TABLE OrderItems (
    OrderItemId INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL FOREIGN KEY REFERENCES Orders(OrderId) ON DELETE CASCADE,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    ProductName NVARCHAR(200) NOT NULL, -- Store name at time of order
    ProductCode NVARCHAR(50),
    SKU NVARCHAR(50),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    DiscountPercent DECIMAL(5,2) DEFAULT 0,
    TaxAmount DECIMAL(18,2) DEFAULT 0,
    TotalPrice DECIMAL(18,2) NOT NULL,
    -- Warranty
    WarrantyPeriod INT,
    WarrantyExpiryDate DATE,
    -- Serial Number (if applicable)
    SerialNumber NVARCHAR(100),
    Notes NVARCHAR(500)
);

-- Order Status History
CREATE TABLE OrderStatusHistory (
    HistoryId INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL FOREIGN KEY REFERENCES Orders(OrderId) ON DELETE CASCADE,
    PreviousStatus NVARCHAR(30),
    NewStatus NVARCHAR(30) NOT NULL,
    Notes NVARCHAR(500),
    ChangedAt DATETIME2 DEFAULT GETDATE(),
    ChangedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- =====================================================
-- SUPPORT MODULE TABLES
-- =====================================================

-- Ticket Categories
CREATE TABLE TicketCategories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255),
    SLAHours INT DEFAULT 24, -- Response time in hours
    Priority NVARCHAR(20) DEFAULT 'Medium',
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

INSERT INTO TicketCategories (CategoryName, Description, SLAHours, Priority) VALUES
('Technical Support', 'Hardware and software technical issues', 24, 'Medium'),
('Billing Inquiry', 'Questions about invoices and payments', 48, 'Low'),
('Order Issue', 'Problems with orders or delivery', 12, 'High'),
('Product Information', 'Questions about products', 48, 'Low'),
('Returns & Refunds', 'Return and refund requests', 24, 'Medium'),
('Warranty Claims', 'Warranty-related issues', 24, 'Medium'),
('Account Issues', 'Login and account problems', 12, 'High'),
('General Inquiry', 'General questions and feedback', 72, 'Low');

-- Support Tickets
CREATE TABLE SupportTickets (
    TicketId INT PRIMARY KEY IDENTITY(1,1),
    TicketNumber NVARCHAR(20) NOT NULL UNIQUE,
    CustomerId INT FOREIGN KEY REFERENCES Customers(CustomerId),
    CategoryId INT FOREIGN KEY REFERENCES TicketCategories(CategoryId),
    OrderId INT FOREIGN KEY REFERENCES Orders(OrderId), -- Related order if applicable
    -- Contact Info (for non-registered customers)
    ContactName NVARCHAR(100),
    ContactEmail NVARCHAR(100) NOT NULL,
    ContactPhone NVARCHAR(20),
    -- Ticket Details
    Subject NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Priority NVARCHAR(20) DEFAULT 'Medium', -- Low, Medium, High, Critical
    Status NVARCHAR(30) DEFAULT 'Open', -- Open, In Progress, Pending Customer, Resolved, Closed
    -- Assignment
    AssignedTo INT FOREIGN KEY REFERENCES Users(UserId),
    AssignedAt DATETIME2,
    -- SLA
    DueDate DATETIME2,
    SLABreached BIT DEFAULT 0,
    -- Resolution
    Resolution NVARCHAR(MAX),
    ResolvedAt DATETIME2,
    ResolvedBy INT FOREIGN KEY REFERENCES Users(UserId),
    -- Customer Feedback
    SatisfactionRating INT, -- 1-5
    Feedback NVARCHAR(MAX),
    -- Source
    Source NVARCHAR(30) DEFAULT 'Web', -- Web, Email, Phone, Chat, Social Media
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    ClosedAt DATETIME2
);

-- Ticket Messages (Conversation thread)
CREATE TABLE TicketMessages (
    MessageId INT PRIMARY KEY IDENTITY(1,1),
    TicketId INT NOT NULL FOREIGN KEY REFERENCES SupportTickets(TicketId) ON DELETE CASCADE,
    SenderType NVARCHAR(20) NOT NULL, -- Customer, Agent, System
    SenderId INT, -- User ID if agent
    Message NVARCHAR(MAX) NOT NULL,
    IsInternal BIT DEFAULT 0, -- Internal notes not visible to customer
    Attachments NVARCHAR(MAX), -- JSON array of attachment URLs
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Ticket Attachments
CREATE TABLE TicketAttachments (
    AttachmentId INT PRIMARY KEY IDENTITY(1,1),
    TicketId INT NOT NULL FOREIGN KEY REFERENCES SupportTickets(TicketId) ON DELETE CASCADE,
    MessageId INT FOREIGN KEY REFERENCES TicketMessages(MessageId),
    FileName NVARCHAR(255) NOT NULL,
    FileUrl NVARCHAR(500) NOT NULL,
    FileSize INT,
    FileType NVARCHAR(50),
    UploadedAt DATETIME2 DEFAULT GETDATE(),
    UploadedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Knowledge Base Categories
CREATE TABLE KnowledgeCategories (
    CategoryId INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255),
    ParentCategoryId INT FOREIGN KEY REFERENCES KnowledgeCategories(CategoryId),
    DisplayOrder INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

INSERT INTO KnowledgeCategories (CategoryName) VALUES
('Getting Started'), ('Troubleshooting'), ('FAQs'), ('Product Guides'),
('Warranty Information'), ('Return Policy'), ('Payment Methods');

-- Knowledge Base Articles
CREATE TABLE KnowledgeArticles (
    ArticleId INT PRIMARY KEY IDENTITY(1,1),
    CategoryId INT FOREIGN KEY REFERENCES KnowledgeCategories(CategoryId),
    Title NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(200),
    Content NVARCHAR(MAX) NOT NULL,
    Summary NVARCHAR(500),
    Tags NVARCHAR(500), -- Comma-separated
    ViewCount INT DEFAULT 0,
    HelpfulCount INT DEFAULT 0,
    NotHelpfulCount INT DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'Draft', -- Draft, Published, Archived
    PublishedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- =====================================================
-- MARKETING MODULE TABLES
-- =====================================================

-- Marketing Campaigns
CREATE TABLE Campaigns (
    CampaignId INT PRIMARY KEY IDENTITY(1,1),
    CampaignCode NVARCHAR(20) UNIQUE,
    CampaignName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Type NVARCHAR(50) NOT NULL, -- Email, SMS, Social Media, PPC, Display, Affiliate
    Status NVARCHAR(30) DEFAULT 'Draft', -- Draft, Scheduled, Active, Paused, Completed, Cancelled
    -- Dates
    StartDate DATETIME2,
    EndDate DATETIME2,
    -- Budget
    Budget DECIMAL(18,2),
    ActualSpend DECIMAL(18,2) DEFAULT 0,
    -- Targeting
    TargetSegment NVARCHAR(100),
    TargetAudience NVARCHAR(MAX), -- JSON criteria
    -- Performance
    TotalReach INT DEFAULT 0,
    Impressions INT DEFAULT 0,
    Clicks INT DEFAULT 0,
    Conversions INT DEFAULT 0,
    Revenue DECIMAL(18,2) DEFAULT 0,
    -- Content
    Subject NVARCHAR(200),
    Content NVARCHAR(MAX),
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Customer Segments
CREATE TABLE CustomerSegments (
    SegmentId INT PRIMARY KEY IDENTITY(1,1),
    SegmentName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Criteria NVARCHAR(MAX), -- JSON rules for segmentation
    CustomerCount INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    IsAutomatic BIT DEFAULT 1, -- Auto-updated based on criteria
    LastUpdated DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

INSERT INTO CustomerSegments (SegmentName, Description) VALUES
('All Customers', 'All registered customers'),
('New Customers', 'Customers registered in last 30 days'),
('High Value', 'Customers with total purchases over $1000'),
('At Risk', 'Customers with no purchases in 90 days'),
('VIP', 'VIP category customers'),
('Newsletter Subscribers', 'Opted in to marketing emails');

-- Segment Members
CREATE TABLE SegmentMembers (
    MemberId INT PRIMARY KEY IDENTITY(1,1),
    SegmentId INT NOT NULL FOREIGN KEY REFERENCES CustomerSegments(SegmentId) ON DELETE CASCADE,
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId) ON DELETE CASCADE,
    AddedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT UQ_SegmentMember UNIQUE (SegmentId, CustomerId)
);

-- Promotions / Discount Codes
CREATE TABLE Promotions (
    PromotionId INT PRIMARY KEY IDENTITY(1,1),
    PromotionCode NVARCHAR(50) NOT NULL UNIQUE,
    PromotionName NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    DiscountType NVARCHAR(20) NOT NULL, -- Percentage, FixedAmount, FreeShipping, BuyXGetY
    DiscountValue DECIMAL(18,2) NOT NULL,
    MinOrderAmount DECIMAL(18,2) DEFAULT 0,
    MaxDiscountAmount DECIMAL(18,2), -- Cap for percentage discounts
    -- Validity
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    -- Usage Limits
    UsageLimit INT, -- Total uses allowed
    UsageLimitPerCustomer INT DEFAULT 1,
    TimesUsed INT DEFAULT 0,
    -- Restrictions
    ApplicableProducts NVARCHAR(MAX), -- JSON product IDs
    ApplicableCategories NVARCHAR(MAX), -- JSON category IDs
    ApplicableCustomers NVARCHAR(MAX), -- JSON customer IDs or segment IDs
    ExcludeOnSaleItems BIT DEFAULT 0,
    -- Status
    IsActive BIT DEFAULT 1,
    CampaignId INT FOREIGN KEY REFERENCES Campaigns(CampaignId),
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Promotion Usage History
CREATE TABLE PromotionUsage (
    UsageId INT PRIMARY KEY IDENTITY(1,1),
    PromotionId INT NOT NULL FOREIGN KEY REFERENCES Promotions(PromotionId),
    OrderId INT NOT NULL FOREIGN KEY REFERENCES Orders(OrderId),
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    DiscountApplied DECIMAL(18,2) NOT NULL,
    UsedAt DATETIME2 DEFAULT GETDATE()
);

-- =====================================================
-- BILLING MODULE TABLES
-- =====================================================

-- Invoices
CREATE TABLE Invoices (
    InvoiceId INT PRIMARY KEY IDENTITY(1,1),
    InvoiceNumber NVARCHAR(30) NOT NULL UNIQUE,
    OrderId INT FOREIGN KEY REFERENCES Orders(OrderId),
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    -- Invoice Details
    InvoiceDate DATETIME2 DEFAULT GETDATE(),
    DueDate DATETIME2 NOT NULL,
    -- Amounts
    Subtotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    TaxAmount DECIMAL(18,2) DEFAULT 0,
    ShippingAmount DECIMAL(18,2) DEFAULT 0,
    TotalAmount DECIMAL(18,2) NOT NULL,
    PaidAmount DECIMAL(18,2) DEFAULT 0,
    BalanceDue DECIMAL(18,2) NOT NULL,
    -- Status
    Status NVARCHAR(20) DEFAULT 'Draft', -- Draft, Sent, Paid, Partial, Overdue, Cancelled, Refunded
    -- Billing Info
    BillingName NVARCHAR(100),
    BillingAddress NVARCHAR(255),
    BillingCity NVARCHAR(50),
    BillingState NVARCHAR(50),
    BillingZipCode NVARCHAR(20),
    BillingCountry NVARCHAR(50),
    BillingEmail NVARCHAR(100),
    -- Terms
    PaymentTerms NVARCHAR(100),
    Notes NVARCHAR(MAX),
    InternalNotes NVARCHAR(MAX),
    -- Timestamps
    SentAt DATETIME2,
    PaidAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Invoice Items
CREATE TABLE InvoiceItems (
    ItemId INT PRIMARY KEY IDENTITY(1,1),
    InvoiceId INT NOT NULL FOREIGN KEY REFERENCES Invoices(InvoiceId) ON DELETE CASCADE,
    ProductId INT FOREIGN KEY REFERENCES Products(ProductId),
    Description NVARCHAR(500) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) DEFAULT 0,
    TaxAmount DECIMAL(18,2) DEFAULT 0,
    TotalPrice DECIMAL(18,2) NOT NULL
);

-- Payment Methods (Saved customer payment methods)
CREATE TABLE PaymentMethods (
    MethodId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId) ON DELETE CASCADE,
    MethodType NVARCHAR(30) NOT NULL, -- CreditCard, DebitCard, PayMongo, BankAccount
    Provider NVARCHAR(50), -- Visa, Mastercard, GCash, Maya, etc.
    -- Card Details (encrypted/tokenized)
    LastFourDigits NVARCHAR(4),
    ExpiryMonth INT,
    ExpiryYear INT,
    CardHolderName NVARCHAR(100),
    -- Bank Details
    BankName NVARCHAR(100),
    AccountNumber NVARCHAR(50), -- Encrypted
    -- PayMongo
    PayMongoToken NVARCHAR(255),
    -- Status
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- Payments
CREATE TABLE Payments (
    PaymentId INT PRIMARY KEY IDENTITY(1,1),
    PaymentNumber NVARCHAR(30) NOT NULL UNIQUE,
    InvoiceId INT FOREIGN KEY REFERENCES Invoices(InvoiceId),
    OrderId INT FOREIGN KEY REFERENCES Orders(OrderId),
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    -- Payment Details
    PaymentDate DATETIME2 DEFAULT GETDATE(),
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL, -- Cash, CreditCard, DebitCard, PayMongo, BankTransfer, GCash, Maya
    -- Status
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Completed, Failed, Refunded, Cancelled
    -- Reference
    TransactionId NVARCHAR(100), -- External transaction ID
    ReferenceNumber NVARCHAR(100),
    PayMongoPaymentId NVARCHAR(100),
    -- QR Payment (PayMongo)
    QRCodeUrl NVARCHAR(500),
    QRCodeExpiry DATETIME2,
    -- Details
    Currency NVARCHAR(3) DEFAULT 'PHP',
    Notes NVARCHAR(500),
    FailureReason NVARCHAR(500),
    -- Timestamps
    ProcessedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    ProcessedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Refunds
CREATE TABLE Refunds (
    RefundId INT PRIMARY KEY IDENTITY(1,1),
    RefundNumber NVARCHAR(30) NOT NULL UNIQUE,
    PaymentId INT NOT NULL FOREIGN KEY REFERENCES Payments(PaymentId),
    OrderId INT FOREIGN KEY REFERENCES Orders(OrderId),
    CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
    -- Refund Details
    Amount DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(500) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Processed, Rejected
    RefundMethod NVARCHAR(50), -- Original Method, Store Credit, Bank Transfer
    -- Timestamps
    RequestedAt DATETIME2 DEFAULT GETDATE(),
    ApprovedAt DATETIME2,
    ProcessedAt DATETIME2,
    RequestedBy INT FOREIGN KEY REFERENCES Users(UserId),
    ApprovedBy INT FOREIGN KEY REFERENCES Users(UserId),
    ProcessedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- =====================================================
-- ACTIVITY & AUDIT TABLES
-- =====================================================

-- Activity Log (Comprehensive audit trail)
CREATE TABLE ActivityLogs (
    LogId BIGINT PRIMARY KEY IDENTITY(1,1),
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    UserName NVARCHAR(100),
    Action NVARCHAR(50) NOT NULL, -- Login, Logout, Create, Update, Delete, View, Export, etc.
    Module NVARCHAR(50) NOT NULL, -- Users, Customers, Orders, etc.
    EntityType NVARCHAR(50), -- Table name
    EntityId INT,
    Description NVARCHAR(500),
    OldValues NVARCHAR(MAX), -- JSON of old values
    NewValues NVARCHAR(MAX), -- JSON of new values
    IPAddress NVARCHAR(50),
    UserAgent NVARCHAR(500),
    SessionId NVARCHAR(100),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Create index for performance
CREATE INDEX IX_ActivityLogs_UserId ON ActivityLogs(UserId);
CREATE INDEX IX_ActivityLogs_Module ON ActivityLogs(Module);
CREATE INDEX IX_ActivityLogs_CreatedAt ON ActivityLogs(CreatedAt);

-- Notifications
CREATE TABLE Notifications (
    NotificationId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId) ON DELETE CASCADE,
    Type NVARCHAR(50) NOT NULL, -- Order, Payment, Support, System, Marketing
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(500),
    Link NVARCHAR(500),
    IsRead BIT DEFAULT 0,
    ReadAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- =====================================================
-- CHAT / AI CHATBOT TABLES
-- =====================================================

-- Chat Sessions
CREATE TABLE ChatSessions (
    SessionId INT PRIMARY KEY IDENTITY(1,1),
    CustomerId INT FOREIGN KEY REFERENCES Customers(CustomerId),
    VisitorId NVARCHAR(100), -- For non-logged in visitors
    SessionToken NVARCHAR(255) UNIQUE,
    Status NVARCHAR(20) DEFAULT 'Active', -- Active, Ended, Transferred
    AgentId INT FOREIGN KEY REFERENCES Users(UserId), -- If transferred to human
    StartedAt DATETIME2 DEFAULT GETDATE(),
    EndedAt DATETIME2,
    -- Metadata
    Source NVARCHAR(50), -- Website, Mobile, Facebook, etc.
    DeviceType NVARCHAR(20),
    IPAddress NVARCHAR(50),
    -- Stats
    TotalMessages INT DEFAULT 0,
    Rating INT, -- 1-5
    Feedback NVARCHAR(500)
);

-- Chat Messages
CREATE TABLE ChatMessages (
    MessageId INT PRIMARY KEY IDENTITY(1,1),
    SessionId INT NOT NULL FOREIGN KEY REFERENCES ChatSessions(SessionId) ON DELETE CASCADE,
    SenderType NVARCHAR(20) NOT NULL, -- Customer, Bot, Agent
    SenderId INT,
    Message NVARCHAR(MAX) NOT NULL,
    MessageType NVARCHAR(20) DEFAULT 'Text', -- Text, Image, File, Button, Card
    Metadata NVARCHAR(MAX), -- JSON for rich messages
    Intent NVARCHAR(100), -- Detected intent (for bot messages)
    Confidence DECIMAL(5,2), -- AI confidence score
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Chat Bot Intents
CREATE TABLE ChatBotIntents (
    IntentId INT PRIMARY KEY IDENTITY(1,1),
    IntentName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    Category NVARCHAR(50),
    TrainingPhrases NVARCHAR(MAX), -- JSON array
    Responses NVARCHAR(MAX), -- JSON array
    Actions NVARCHAR(MAX), -- JSON actions to perform
    IsActive BIT DEFAULT 1,
    Priority INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

-- =====================================================
-- SYSTEM SETTINGS
-- =====================================================

-- System Settings
CREATE TABLE SystemSettings (
    SettingId INT PRIMARY KEY IDENTITY(1,1),
    SettingKey NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(MAX),
    SettingType NVARCHAR(20) DEFAULT 'String', -- String, Number, Boolean, JSON
    Category NVARCHAR(50),
    Description NVARCHAR(500),
    IsEditable BIT DEFAULT 1,
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedBy INT FOREIGN KEY REFERENCES Users(UserId)
);

-- Insert Default Settings
INSERT INTO SystemSettings (SettingKey, SettingValue, SettingType, Category, Description) VALUES
-- Company Settings
('company.name', 'CompuGear Technologies', 'String', 'Company', 'Company name'),
('company.email', 'info@compugear.com', 'String', 'Company', 'Company email'),
('company.phone', '+63-XXX-XXX-XXXX', 'String', 'Company', 'Company phone'),
('company.address', 'Manila, Philippines', 'String', 'Company', 'Company address'),
('company.currency', 'PHP', 'String', 'Company', 'Default currency'),
('company.timezone', 'Asia/Manila', 'String', 'Company', 'Timezone'),
-- Tax Settings
('tax.enabled', 'true', 'Boolean', 'Tax', 'Enable tax calculation'),
('tax.rate', '12', 'Number', 'Tax', 'Default tax rate percentage'),
('tax.name', 'VAT', 'String', 'Tax', 'Tax name'),
-- Order Settings
('order.prefix', 'ORD-', 'String', 'Orders', 'Order number prefix'),
('order.auto_confirm', 'false', 'Boolean', 'Orders', 'Auto-confirm orders'),
-- Invoice Settings
('invoice.prefix', 'INV-', 'String', 'Billing', 'Invoice number prefix'),
('invoice.due_days', '30', 'Number', 'Billing', 'Default payment due days'),
-- Support Settings
('support.auto_assign', 'true', 'Boolean', 'Support', 'Auto-assign tickets'),
('support.sla_hours', '24', 'Number', 'Support', 'Default SLA hours'),
-- Email Settings
('email.smtp_host', '', 'String', 'Email', 'SMTP server host'),
('email.smtp_port', '587', 'Number', 'Email', 'SMTP port'),
-- PayMongo Settings
('paymongo.enabled', 'true', 'Boolean', 'Payment', 'Enable PayMongo'),
('paymongo.public_key', '', 'String', 'Payment', 'PayMongo public key'),
('paymongo.secret_key', '', 'String', 'Payment', 'PayMongo secret key');

-- =====================================================
-- VIEWS FOR REPORTING
-- =====================================================
GO

-- Daily Sales Summary View
CREATE VIEW vw_DailySalesSummary AS
SELECT 
    CAST(OrderDate AS DATE) AS SaleDate,
    COUNT(OrderId) AS TotalOrders,
    SUM(TotalAmount) AS TotalRevenue,
    SUM(CASE WHEN OrderStatus = 'Delivered' THEN 1 ELSE 0 END) AS CompletedOrders,
    SUM(CASE WHEN OrderStatus = 'Cancelled' THEN 1 ELSE 0 END) AS CancelledOrders,
    AVG(TotalAmount) AS AverageOrderValue
FROM Orders
GROUP BY CAST(OrderDate AS DATE);
GO

-- Customer Overview View
CREATE VIEW vw_CustomerOverview AS
SELECT 
    c.CustomerId,
    c.CustomerCode,
    CONCAT(c.FirstName, ' ', c.LastName) AS CustomerName,
    c.Email,
    c.Phone,
    cc.CategoryName AS CustomerCategory,
    c.Status,
    c.TotalOrders,
    c.TotalSpent,
    c.LoyaltyPoints,
    c.CreatedAt AS JoinedDate,
    (SELECT MAX(OrderDate) FROM Orders o WHERE o.CustomerId = c.CustomerId) AS LastOrderDate
FROM Customers c
LEFT JOIN CustomerCategories cc ON c.CategoryId = cc.CategoryId;
GO

-- Product Inventory View
CREATE VIEW vw_ProductInventory AS
SELECT 
    p.ProductId,
    p.ProductCode,
    p.ProductName,
    pc.CategoryName,
    b.BrandName,
    p.CostPrice,
    p.SellingPrice,
    p.StockQuantity,
    p.ReorderLevel,
    CASE 
        WHEN p.StockQuantity = 0 THEN 'Out of Stock'
        WHEN p.StockQuantity <= p.ReorderLevel THEN 'Low Stock'
        ELSE 'In Stock'
    END AS StockStatus,
    p.Status
FROM Products p
LEFT JOIN ProductCategories pc ON p.CategoryId = pc.CategoryId
LEFT JOIN Brands b ON p.BrandId = b.BrandId;
GO

-- =====================================================
-- INSERT SAMPLE DATA
-- =====================================================

-- Insert Sample Admin User (Password: Admin@123 - Note: Use proper hashing in production)
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified)
VALUES ('admin', 'admin@compugear.com', 'hashed_password_here', 'salt_here', 'System', 'Administrator', 1, 1, 1, 1);

-- Insert Sample Staff Users
INSERT INTO Users (Username, Email, PasswordHash, Salt, FirstName, LastName, RoleId, CompanyId, IsActive, IsEmailVerified) VALUES
('sarah.johnson', 'sarah.j@compugear.com', 'hashed_password', 'salt', 'Sarah', 'Johnson', 3, 1, 1, 1), -- Sales Staff
('mike.chen', 'mike.c@compugear.com', 'hashed_password', 'salt', 'Mike', 'Chen', 4, 1, 1, 1), -- Support Staff
('emily.brown', 'emily.b@compugear.com', 'hashed_password', 'salt', 'Emily', 'Brown', 5, 1, 1, 1), -- Marketing Staff
('john.doe', 'john.d@compugear.com', 'hashed_password', 'salt', 'John', 'Doe', 6, 1, 1, 1); -- Billing Staff

-- Insert Sample Customers
INSERT INTO Customers (CustomerCode, FirstName, LastName, Email, Phone, CategoryId, Status, BillingAddress, BillingCity, BillingCountry)
VALUES 
('CUST-0001', 'Maria', 'Santos', 'maria.santos@email.com', '+63-912-345-6789', 2, 'Active', '123 Main St', 'Manila', 'Philippines'),
('CUST-0002', 'Juan', 'Dela Cruz', 'juan.dc@email.com', '+63-923-456-7890', 3, 'Active', '456 Oak Ave', 'Quezon City', 'Philippines'),
('CUST-0003', 'Ana', 'Reyes', 'ana.r@email.com', '+63-934-567-8901', 1, 'Active', '789 Pine Rd', 'Makati', 'Philippines'),
('CUST-0004', 'Tech Corp', 'Inc.', 'orders@techcorp.com', '+63-945-678-9012', 4, 'Active', '100 Business Park', 'BGC', 'Philippines'),
('CUST-0005', 'Pedro', 'Garcia', 'pedro.g@email.com', '+63-956-789-0123', 1, 'Active', '222 Elm St', 'Cebu', 'Philippines');

-- Insert Sample Products
INSERT INTO Products (ProductCode, SKU, ProductName, ShortDescription, CategoryId, BrandId, CostPrice, SellingPrice, StockQuantity, ReorderLevel, Status)
VALUES 
('PRD-001', 'INTEL-I7-13700K', 'Intel Core i7-13700K', '13th Gen Intel Core i7 Processor', 2, 1, 15000, 22500, 50, 10, 'Active'),
('PRD-002', 'AMD-R9-7950X', 'AMD Ryzen 9 7950X', 'AMD Ryzen 9 Desktop Processor', 2, 2, 25000, 35000, 30, 10, 'Active'),
('PRD-003', 'NVIDIA-RTX4090', 'NVIDIA GeForce RTX 4090', 'NVIDIA RTX 4090 Graphics Card', 2, 3, 90000, 120000, 15, 5, 'Active'),
('PRD-004', 'ASUS-ROG-MB', 'ASUS ROG Maximus Z790', 'ASUS ROG Gaming Motherboard', 2, 4, 25000, 32000, 25, 10, 'Active'),
('PRD-005', 'CORSAIR-RAM-32', 'Corsair Vengeance 32GB DDR5', '32GB DDR5 RAM Kit', 2, 7, 8000, 11500, 100, 20, 'Active'),
('PRD-006', 'SAMSUNG-SSD-1TB', 'Samsung 990 Pro 1TB NVMe', '1TB NVMe SSD', 8, 9, 5500, 7500, 75, 15, 'Active'),
('PRD-007', 'LOGITECH-G502', 'Logitech G502 X Plus', 'Wireless Gaming Mouse', 3, 11, 4500, 6500, 60, 15, 'Active'),
('PRD-008', 'RAZER-KEYBOARD', 'Razer BlackWidow V4 Pro', 'Mechanical Gaming Keyboard', 3, 12, 8000, 11000, 40, 10, 'Active');

PRINT 'CompuGear Database Schema Created Successfully!';
GO
