-- =====================================================
-- ADD ACCOUNTING TABLES: Chart of Accounts, Journal Entries, General Ledger
-- Run this in SSMS to add accounting/bookkeeping tables
-- =====================================================

USE Compugear;
GO

-- =====================================================
-- 1. CHART OF ACCOUNTS (COA)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChartOfAccounts')
BEGIN
    CREATE TABLE ChartOfAccounts (
        AccountId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        AccountCode NVARCHAR(20) NOT NULL,
        AccountName NVARCHAR(100) NOT NULL,
        AccountType NVARCHAR(20) NOT NULL, -- Asset, Liability, Equity, Revenue, Expense
        ParentAccountId INT NULL FOREIGN KEY REFERENCES ChartOfAccounts(AccountId),
        Description NVARCHAR(500) NULL,
        NormalBalance NVARCHAR(10) NOT NULL DEFAULT 'Debit', -- Debit or Credit
        IsActive BIT DEFAULT 1,
        IsArchived BIT DEFAULT 0,
        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
        CreatedBy INT NULL,
        UpdatedBy INT NULL
    );

    -- Seed default Chart of Accounts
    INSERT INTO ChartOfAccounts (CompanyId, AccountCode, AccountName, AccountType, NormalBalance, Description) VALUES
    -- Assets (1xxx)
    (1, '1000', 'Cash', 'Asset', 'Debit', 'Cash on hand and in bank'),
    (1, '1010', 'Accounts Receivable', 'Asset', 'Debit', 'Amounts owed by customers'),
    (1, '1020', 'Inventory', 'Asset', 'Debit', 'Goods available for sale'),
    (1, '1030', 'Prepaid Expenses', 'Asset', 'Debit', 'Expenses paid in advance'),
    (1, '1100', 'Equipment', 'Asset', 'Debit', 'Office and computer equipment'),
    -- Liabilities (2xxx)
    (1, '2000', 'Accounts Payable', 'Liability', 'Credit', 'Amounts owed to suppliers'),
    (1, '2010', 'Accrued Expenses', 'Liability', 'Credit', 'Expenses incurred but not yet paid'),
    (1, '2020', 'Sales Tax Payable', 'Liability', 'Credit', 'VAT/Sales tax collected'),
    (1, '2100', 'Loans Payable', 'Liability', 'Credit', 'Outstanding loan balances'),
    -- Equity (3xxx)
    (1, '3000', 'Owner''s Equity', 'Equity', 'Credit', 'Owner''s capital investment'),
    (1, '3010', 'Retained Earnings', 'Equity', 'Credit', 'Accumulated net income'),
    -- Revenue (4xxx)
    (1, '4000', 'Sales Revenue', 'Revenue', 'Credit', 'Income from product sales'),
    (1, '4010', 'Service Revenue', 'Revenue', 'Credit', 'Income from services'),
    (1, '4020', 'Other Income', 'Revenue', 'Credit', 'Miscellaneous income'),
    -- Expenses (5xxx)
    (1, '5000', 'Cost of Goods Sold', 'Expense', 'Debit', 'Direct cost of products sold'),
    (1, '5010', 'Salaries Expense', 'Expense', 'Debit', 'Employee salaries and wages'),
    (1, '5020', 'Rent Expense', 'Expense', 'Debit', 'Office or warehouse rent'),
    (1, '5030', 'Utilities Expense', 'Expense', 'Debit', 'Electricity, water, internet'),
    (1, '5040', 'Marketing Expense', 'Expense', 'Debit', 'Advertising and promotions'),
    (1, '5050', 'Office Supplies', 'Expense', 'Debit', 'Office consumables'),
    (1, '5060', 'Depreciation Expense', 'Expense', 'Debit', 'Asset depreciation');

    PRINT 'ChartOfAccounts table created and seeded.';
END
GO

-- =====================================================
-- 2. JOURNAL ENTRIES
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JournalEntries')
BEGIN
    CREATE TABLE JournalEntries (
        EntryId INT PRIMARY KEY IDENTITY(1,1),
        CompanyId INT NULL FOREIGN KEY REFERENCES Companies(CompanyId),
        EntryNumber NVARCHAR(30) NOT NULL,
        EntryDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Description NVARCHAR(500) NOT NULL,
        Reference NVARCHAR(100) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft, Posted, Void
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
GO

-- =====================================================
-- 3. JOURNAL ENTRY LINES (Debit/Credit lines)
-- =====================================================
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
GO

-- =====================================================
-- 4. GENERAL LEDGER (Auto-populated from posted journal entries)
-- =====================================================
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
GO

PRINT '';
PRINT '===========================================';
PRINT 'ACCOUNTING TABLES CREATED SUCCESSFULLY';
PRINT '  - ChartOfAccounts (with default accounts)';
PRINT '  - JournalEntries';
PRINT '  - JournalEntryLines';
PRINT '  - GeneralLedger';
PRINT '===========================================';
GO
