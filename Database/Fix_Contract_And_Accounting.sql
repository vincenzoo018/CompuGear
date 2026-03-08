-- =====================================================
-- FIX: Add Contract/Penalty columns + Fix Accounting Seed Data
-- Run against the remote database
-- =====================================================

-- =====================================================
-- 1. ADD MISSING CONTRACT & PENALTY COLUMNS TO CompanySubscriptions
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'ContractAgreed')
    ALTER TABLE CompanySubscriptions ADD ContractAgreed BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'ContractAgreedAt')
    ALTER TABLE CompanySubscriptions ADD ContractAgreedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'ContractType')
    ALTER TABLE CompanySubscriptions ADD ContractType NVARCHAR(20) NOT NULL DEFAULT 'Standard';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'ContractTermMonths')
    ALTER TABLE CompanySubscriptions ADD ContractTermMonths INT NOT NULL DEFAULT 12;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'OverdueMonths')
    ALTER TABLE CompanySubscriptions ADD OverdueMonths INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'PenaltyAmount')
    ALTER TABLE CompanySubscriptions ADD PenaltyAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'TotalAmountDue')
    ALTER TABLE CompanySubscriptions ADD TotalAmountDue DECIMAL(18,2) NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'LastPaymentDate')
    ALTER TABLE CompanySubscriptions ADD LastPaymentDate DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'NextDueDate')
    ALTER TABLE CompanySubscriptions ADD NextDueDate DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('CompanySubscriptions') AND name = 'PaymentStatus')
    ALTER TABLE CompanySubscriptions ADD PaymentStatus NVARCHAR(20) NOT NULL DEFAULT 'Current';

PRINT 'CompanySubscriptions: Contract & penalty columns added.';

-- =====================================================
-- 2. ENSURE ACCOUNTING TABLES EXIST
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChartOfAccounts')
BEGIN
    CREATE TABLE ChartOfAccounts (
        AccountId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        AccountCode NVARCHAR(20) NOT NULL,
        AccountName NVARCHAR(100) NOT NULL,
        AccountType NVARCHAR(20) NOT NULL,
        ParentAccountId INT NULL FOREIGN KEY REFERENCES ChartOfAccounts(AccountId),
        Description NVARCHAR(500) NULL,
        NormalBalance NVARCHAR(10) NOT NULL DEFAULT 'Debit',
        IsActive BIT DEFAULT 1,
        IsArchived BIT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );
    PRINT 'ChartOfAccounts table created.';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JournalEntries')
BEGIN
    CREATE TABLE JournalEntries (
        EntryId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        EntryNumber NVARCHAR(30) NOT NULL,
        EntryDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Description NVARCHAR(500) NOT NULL,
        Reference NVARCHAR(100) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Draft',
        TotalDebit DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalCredit DECIMAL(18,2) NOT NULL DEFAULT 0,
        IsArchived BIT DEFAULT 0,
        Notes NVARCHAR(1000) NULL,
        PostedAt DATETIME2 NULL,
        PostedBy INT NULL,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );
    PRINT 'JournalEntries table created.';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JournalEntryLines')
BEGIN
    CREATE TABLE JournalEntryLines (
        LineId INT PRIMARY KEY IDENTITY(1,1),
        EntryId INT NOT NULL FOREIGN KEY REFERENCES JournalEntries(EntryId) ON DELETE CASCADE,
        AccountId INT NOT NULL FOREIGN KEY REFERENCES ChartOfAccounts(AccountId),
        Description NVARCHAR(255) NULL,
        DebitAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreditAmount DECIMAL(18,2) NOT NULL DEFAULT 0
    );
    PRINT 'JournalEntryLines table created.';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GeneralLedger')
BEGIN
    CREATE TABLE GeneralLedger (
        LedgerId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        AccountId INT NOT NULL FOREIGN KEY REFERENCES ChartOfAccounts(AccountId),
        EntryId INT NULL FOREIGN KEY REFERENCES JournalEntries(EntryId),
        TransactionDate DATETIME2 NOT NULL,
        Description NVARCHAR(500) NULL,
        DebitAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        CreditAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        RunningBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
        Reference NVARCHAR(100) NULL,
        IsArchived BIT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE()
    );
    PRINT 'GeneralLedger table created.';
END

-- =====================================================
-- 3. FIX ACCOUNTING SEED DATA: Use CompanyId = NULL for platform-level accounts
--    The SuperAdmin controller queries WHERE CompanyId IS NULL
--    Also ensure correct account codes: 1100 (AR), 4000 (Revenue), 2100 (Tax Payable)
-- =====================================================

-- First check if platform-level accounts already exist
IF NOT EXISTS (SELECT 1 FROM ChartOfAccounts WHERE CompanyId IS NULL AND AccountCode = '1000')
BEGIN
    -- Insert platform-level chart of accounts (CompanyId = NULL)
    INSERT INTO ChartOfAccounts (CompanyId, AccountCode, AccountName, AccountType, NormalBalance, Description, IsActive, IsArchived) VALUES
    -- Assets (1xxx)
    (NULL, '1000', 'Cash', 'Asset', 'Debit', 'Cash on hand and in bank', 1, 0),
    (NULL, '1010', 'Accounts Receivable - Trade', 'Asset', 'Debit', 'Amounts owed by trade customers', 1, 0),
    (NULL, '1020', 'Inventory', 'Asset', 'Debit', 'Goods available for sale', 1, 0),
    (NULL, '1030', 'Prepaid Expenses', 'Asset', 'Debit', 'Expenses paid in advance', 1, 0),
    (NULL, '1100', 'Accounts Receivable - Subscriptions', 'Asset', 'Debit', 'Subscription fees receivable from clients', 1, 0),
    (NULL, '1200', 'Equipment', 'Asset', 'Debit', 'Office and computer equipment', 1, 0),
    -- Liabilities (2xxx)
    (NULL, '2000', 'Accounts Payable', 'Liability', 'Credit', 'Amounts owed to suppliers', 1, 0),
    (NULL, '2010', 'Accrued Expenses', 'Liability', 'Credit', 'Expenses incurred but not yet paid', 1, 0),
    (NULL, '2020', 'Sales Tax Payable', 'Liability', 'Credit', 'VAT/Sales tax collected from trade', 1, 0),
    (NULL, '2100', 'VAT Payable - Subscriptions', 'Liability', 'Credit', 'VAT 12% collected on subscription fees', 1, 0),
    (NULL, '2200', 'Loans Payable', 'Liability', 'Credit', 'Outstanding loan balances', 1, 0),
    -- Equity (3xxx)
    (NULL, '3000', 'Owner''s Equity', 'Equity', 'Credit', 'Owner''s capital investment', 1, 0),
    (NULL, '3010', 'Retained Earnings', 'Equity', 'Credit', 'Accumulated net income', 1, 0),
    -- Revenue (4xxx)
    (NULL, '4000', 'Subscription Revenue', 'Revenue', 'Credit', 'Income from ERP subscription plans', 1, 0),
    (NULL, '4010', 'Service Revenue', 'Revenue', 'Credit', 'Income from professional services', 1, 0),
    (NULL, '4020', 'Penalty Revenue', 'Revenue', 'Credit', 'Income from late payment penalties', 1, 0),
    (NULL, '4030', 'Early Termination Revenue', 'Revenue', 'Credit', 'Income from early termination fees', 1, 0),
    -- Expenses (5xxx)
    (NULL, '5000', 'Cost of Goods Sold', 'Expense', 'Debit', 'Direct cost of products sold', 1, 0),
    (NULL, '5010', 'Salaries Expense', 'Expense', 'Debit', 'Employee salaries and wages', 1, 0),
    (NULL, '5020', 'Rent Expense', 'Expense', 'Debit', 'Office or warehouse rent', 1, 0),
    (NULL, '5030', 'Utilities Expense', 'Expense', 'Debit', 'Electricity, water, internet', 1, 0),
    (NULL, '5040', 'Marketing Expense', 'Expense', 'Debit', 'Advertising and promotions', 1, 0),
    (NULL, '5050', 'Office Supplies', 'Expense', 'Debit', 'Office consumables', 1, 0),
    (NULL, '5060', 'Depreciation Expense', 'Expense', 'Debit', 'Asset depreciation', 1, 0),
    (NULL, '5070', 'Hosting & Infrastructure', 'Expense', 'Debit', 'Server and cloud hosting costs', 1, 0);

    PRINT 'Platform-level Chart of Accounts seeded (CompanyId = NULL).';
END
ELSE
    PRINT 'Platform-level Chart of Accounts already exists.';

PRINT '';
PRINT '===========================================';
PRINT 'MIGRATION COMPLETE';
PRINT '  - CompanySubscriptions: 10 new columns added';
PRINT '  - Accounting tables verified/created';
PRINT '  - Platform-level COA seeded (CompanyId = NULL)';
PRINT '===========================================';
