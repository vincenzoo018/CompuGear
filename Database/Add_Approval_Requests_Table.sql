-- =====================================================
-- ADD APPROVAL REQUESTS TABLE
-- This table handles staff requests that need admin approval
-- =====================================================

USE Compugear;
GO

-- Drop table if exists to recreate
IF OBJECT_ID('dbo.ApprovalRequests', 'U') IS NOT NULL
    DROP TABLE ApprovalRequests;
GO

-- Create ApprovalRequests Table
CREATE TABLE ApprovalRequests (
    RequestId INT PRIMARY KEY IDENTITY(1,1),
    RequestCode NVARCHAR(50) NOT NULL UNIQUE,
    
    -- Request Type and Module
    RequestType NVARCHAR(50) NOT NULL, -- StockAdjustment, ProductCreate, ProductUpdate, ProductDelete, OrderCancel, OrderRefund, PriceChange, PromotionCreate, CampaignCreate, InvoiceVoid, TicketEscalate, CustomerDelete, etc.
    Module NVARCHAR(50) NOT NULL, -- Inventory, Sales, Marketing, Billing, Support
    
    -- Request Details
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    Reason NVARCHAR(MAX) NOT NULL, -- Staff must provide a valid reason
    
    -- Related Entity
    EntityType NVARCHAR(50), -- Product, Order, Customer, Invoice, Ticket, Campaign, Promotion
    EntityId INT, -- ID of the related entity
    EntityName NVARCHAR(200), -- Name for display purposes
    
    -- Request Data (JSON format for flexibility)
    RequestData NVARCHAR(MAX), -- JSON object containing request-specific data
    
    -- Status
    Status NVARCHAR(30) DEFAULT 'Pending', -- Pending, Approved, Rejected, Cancelled
    Priority NVARCHAR(20) DEFAULT 'Normal', -- Low, Normal, High, Urgent
    
    -- Requester Info
    RequestedBy INT NOT NULL, -- UserId of staff who made the request
    RequestedAt DATETIME2 DEFAULT GETDATE(),
    
    -- Approver Info
    ApprovedBy INT, -- UserId of admin who approved/rejected
    ApprovedAt DATETIME2,
    ApprovalNotes NVARCHAR(MAX), -- Admin's notes on approval/rejection
    
    -- Notification
    IsRead BIT DEFAULT 0, -- Has admin seen this request
    NotificationSent BIT DEFAULT 0,
    
    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    
    -- Foreign Keys
    CONSTRAINT FK_ApprovalRequests_RequestedBy FOREIGN KEY (RequestedBy) REFERENCES Users(UserId),
    CONSTRAINT FK_ApprovalRequests_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES Users(UserId)
);
GO

-- Create index for faster queries
CREATE INDEX IX_ApprovalRequests_Status ON ApprovalRequests(Status);
CREATE INDEX IX_ApprovalRequests_Module ON ApprovalRequests(Module);
CREATE INDEX IX_ApprovalRequests_RequestedBy ON ApprovalRequests(RequestedBy);
CREATE INDEX IX_ApprovalRequests_RequestType ON ApprovalRequests(RequestType);
GO

-- Create a view for pending requests
CREATE OR ALTER VIEW vw_PendingApprovalRequests AS
SELECT 
    ar.RequestId,
    ar.RequestCode,
    ar.RequestType,
    ar.Module,
    ar.Title,
    ar.Description,
    ar.Reason,
    ar.EntityType,
    ar.EntityId,
    ar.EntityName,
    ar.RequestData,
    ar.Status,
    ar.Priority,
    ar.RequestedBy,
    u.FirstName + ' ' + u.LastName AS RequesterName,
    u.Email AS RequesterEmail,
    r.RoleName AS RequesterRole,
    ar.RequestedAt,
    ar.IsRead,
    ar.CreatedAt
FROM ApprovalRequests ar
INNER JOIN Users u ON ar.RequestedBy = u.UserId
INNER JOIN Roles r ON u.RoleId = r.RoleId
WHERE ar.Status = 'Pending';
GO

-- Sample approval request types with descriptions
PRINT 'Approval Request Types:';
PRINT '=== INVENTORY MODULE ===';
PRINT '  StockAdjustment    - Request to add/remove stock';
PRINT '  ProductCreate      - Request to add new product';
PRINT '  ProductUpdate      - Request to update product details';
PRINT '  ProductDelete      - Request to delete/discontinue product';
PRINT '  PriceChange        - Request to change product price';
PRINT '  SupplierCreate     - Request to add new supplier';
PRINT '  PurchaseOrder      - Request for purchase order approval';
PRINT '';
PRINT '=== SALES MODULE ===';
PRINT '  OrderCancel        - Request to cancel an order';
PRINT '  OrderRefund        - Request to process refund';
PRINT '  DiscountOverride   - Request for special discount';
PRINT '  LeadConvert        - Request to convert lead to customer';
PRINT '';
PRINT '=== MARKETING MODULE ===';
PRINT '  CampaignCreate     - Request to create new campaign';
PRINT '  CampaignActivate   - Request to activate campaign';
PRINT '  PromotionCreate    - Request to create promotion';
PRINT '  PromotionExtend    - Request to extend promotion';
PRINT '';
PRINT '=== BILLING MODULE ===';
PRINT '  InvoiceVoid        - Request to void an invoice';
PRINT '  PaymentRefund      - Request to refund payment';
PRINT '  CreditNote         - Request to issue credit note';
PRINT '';
PRINT '=== SUPPORT MODULE ===';
PRINT '  TicketEscalate     - Request to escalate ticket';
PRINT '  RefundApproval     - Request refund for customer';
PRINT '  CompensationOffer  - Request to offer compensation';
PRINT '';
PRINT 'ApprovalRequests table created successfully!';
GO
