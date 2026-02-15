    -- =====================================================
    -- CompuGear CRM - Complete Sample Data Insert
    -- Run this AFTER all schema scripts have been executed
    -- Inserts 5 sample records per table
    -- All data uses CompanyId = 1
    -- =====================================================

    -- NOTE: Run this in the context of your CompuGear database
    -- USE Compugear;
    -- GO

    -- =====================================================
    -- PRE-CHECK: Ensure base data exists
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Companies WHERE CompanyId = 1)
    BEGIN
        PRINT 'ERROR: Company with CompanyId=1 does not exist. Run schema scripts first.';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = 1)
    BEGIN
        PRINT 'ERROR: Users do not exist. Run schema scripts first.';
        RETURN;
    END

    PRINT '========================================';
    PRINT 'Starting Sample Data Insert...';
    PRINT '========================================';

    -- =====================================================
    -- 1. SUPPLIERS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Suppliers WHERE SupplierCode = 'SUP-001')
    BEGIN
        SET IDENTITY_INSERT Suppliers OFF;
        INSERT INTO Suppliers (SupplierCode, SupplierName, ContactPerson, Email, Phone, Address, City, Country, PaymentTerms, Status, Rating, CompanyId) VALUES
        ('SUP-001', 'TechParts Philippines Inc.', 'Ricardo Mendoza', 'ricardo@techparts.ph', '+63-2-8888-1001', '100 IT Park, Cebu Business Park', 'Cebu', 'Philippines', 'Net 30', 'Active', 5, 1),
        ('SUP-002', 'GlobalTech Distributors', 'Linda Tan', 'linda@globaltech.com', '+63-2-8888-1002', '200 Ortigas Center', 'Pasig', 'Philippines', 'Net 45', 'Active', 4, 1),
        ('SUP-003', 'Silicon Valley Trading', 'Mark Reyes', 'mark@svtrading.com', '+63-2-8888-1003', '300 Makati Ave', 'Makati', 'Philippines', 'Net 30', 'Active', 4, 1),
        ('SUP-004', 'Dragon Electronics Ltd.', 'James Wu', 'james@dragonelec.com', '+852-2888-1004', '400 Wan Chai, Hong Kong', 'Hong Kong', 'China', 'Net 60', 'Active', 3, 1),
        ('SUP-005', 'Pacific Hardware Co.', 'Anna Cruz', 'anna@pacifichw.ph', '+63-2-8888-1005', '500 Bonifacio High Street', 'Taguig', 'Philippines', 'Net 30', 'Active', 5, 1);
        PRINT 'Inserted 5 Suppliers';
    END
    GO

    -- =====================================================
    -- 2. LEADS (5 records) - AssignedTo = UserId 3 (Sales Staff)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Leads WHERE LeadCode = 'LEAD-001')
    BEGIN
        INSERT INTO Leads (LeadCode, FirstName, LastName, Email, Phone, CompanyName, JobTitle, Source, Status, Priority, EstimatedValue, Probability, AssignedTo, Description, CompanyId, CreatedBy) VALUES
        ('LEAD-001', 'Roberto', 'Villanueva', 'roberto.v@enterprise.ph', '+63-917-111-2233', 'Enterprise Solutions PH', 'IT Manager', 'Website', 'Qualified', 'High', 150000.00, 70, 3, 'Interested in bulk purchase of workstations for new office', 1, 3),
        ('LEAD-002', 'Carmela', 'Diaz', 'carmela.d@startupco.com', '+63-918-222-3344', 'StartupCo', 'CTO', 'Referral', 'Proposal', 'Medium', 85000.00, 50, 3, 'Looking for gaming setup for development team', 1, 3),
        ('LEAD-003', 'Dennis', 'Lim', 'dennis.l@schoolsph.edu', '+63-919-333-4455', 'Schools PH Academy', 'Procurement Head', 'Phone', 'Contacted', 'High', 500000.00, 30, 3, 'Computer lab setup for 50 stations', 1, 3),
        ('LEAD-004', 'Patricia', 'Torres', 'patricia.t@designhub.com', '+63-920-444-5566', 'Design Hub Creative', 'Creative Director', 'Social Media', 'New', 'Low', 45000.00, 20, 3, 'Needs high-end monitors and graphics cards', 1, 3),
        ('LEAD-005', 'Michael', 'Gonzales', 'michael.g@bpoworld.com', '+63-921-555-6677', 'BPO World Corp', 'Operations Manager', 'Email', 'Negotiation', 'Critical', 1200000.00, 80, 3, 'Full IT infrastructure for new BPO site - 200 workstations', 1, 3);
        PRINT 'Inserted 5 Leads';
    END
    GO

    -- =====================================================
    -- 3. ORDERS (5 records) + ORDER ITEMS
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderNumber = 'ORD-2026-0001')
    BEGIN
        -- Order 1: Maria Santos - 2 items
        INSERT INTO Orders (OrderNumber, CustomerId, OrderDate, OrderStatus, PaymentStatus, Subtotal, TaxAmount, TotalAmount, PaymentMethod, ShippingAddress, ShippingCity, ShippingCountry, CompanyId, AssignedTo, CreatedBy) VALUES
        ('ORD-2026-0001', 1, '2026-01-15 09:30:00', 'Delivered', 'Paid', 34000.00, 4080.00, 38080.00, 'Credit Card', '123 Main St', 'Manila', 'Philippines', 1, 3, 3);
        
        INSERT INTO OrderItems (OrderId, ProductId, ProductName, ProductCode, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 1, 'Intel Core i7-13700K', 'PRD-001', 1, 22500.00, 22500.00),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 5, 'Corsair Vengeance 32GB DDR5', 'PRD-005', 1, 11500.00, 11500.00);

        -- Order 2: Juan Dela Cruz - 3 items
        INSERT INTO Orders (OrderNumber, CustomerId, OrderDate, OrderStatus, PaymentStatus, Subtotal, TaxAmount, TotalAmount, PaymentMethod, ShippingAddress, ShippingCity, ShippingCountry, CompanyId, AssignedTo, CreatedBy) VALUES
        ('ORD-2026-0002', 2, '2026-01-20 14:15:00', 'Processing', 'Paid', 163500.00, 19620.00, 183120.00, 'Bank Transfer', '456 Oak Ave', 'Quezon City', 'Philippines', 1, 3, 3);

        INSERT INTO OrderItems (OrderId, ProductId, ProductName, ProductCode, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 3, 'NVIDIA GeForce RTX 4090', 'PRD-003', 1, 120000.00, 120000.00),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 4, 'ASUS ROG Maximus Z790', 'PRD-004', 1, 32000.00, 32000.00),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 5, 'Corsair Vengeance 32GB DDR5', 'PRD-005', 1, 11500.00, 11500.00);

        -- Order 3: Ana Reyes - 1 item
        INSERT INTO Orders (OrderNumber, CustomerId, OrderDate, OrderStatus, PaymentStatus, Subtotal, TaxAmount, TotalAmount, PaymentMethod, ShippingAddress, ShippingCity, ShippingCountry, CompanyId, AssignedTo, CreatedBy) VALUES
        ('ORD-2026-0003', 3, '2026-02-01 10:00:00', 'Confirmed', 'Pending', 7500.00, 900.00, 8400.00, 'GCash', '789 Pine Rd', 'Makati', 'Philippines', 1, 3, 3);

        INSERT INTO OrderItems (OrderId, ProductId, ProductName, ProductCode, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0003'), 6, 'Samsung 990 Pro 1TB NVMe', 'PRD-006', 1, 7500.00, 7500.00);

        -- Order 4: Tech Corp - 5 items (big corporate order)
        INSERT INTO Orders (OrderNumber, CustomerId, OrderDate, OrderStatus, PaymentStatus, Subtotal, TaxAmount, TotalAmount, PaymentMethod, ShippingAddress, ShippingCity, ShippingCountry, CompanyId, AssignedTo, CreatedBy) VALUES
        ('ORD-2026-0004', 4, '2026-02-05 08:45:00', 'Shipped', 'Paid', 142500.00, 17100.00, 159600.00, 'Bank Transfer', '100 Business Park', 'BGC', 'Philippines', 1, 3, 3);

        INSERT INTO OrderItems (OrderId, ProductId, ProductName, ProductCode, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0004'), 1, 'Intel Core i7-13700K', 'PRD-001', 5, 22500.00, 112500.00),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0004'), 7, 'Logitech G502 X Plus', 'PRD-007', 5, 6000.00, 30000.00);

        -- Order 5: Pedro Garcia - 2 items
        INSERT INTO Orders (OrderNumber, CustomerId, OrderDate, OrderStatus, PaymentStatus, Subtotal, TaxAmount, TotalAmount, PaymentMethod, ShippingAddress, ShippingCity, ShippingCountry, CompanyId, AssignedTo, CreatedBy) VALUES
        ('ORD-2026-0005', 5, '2026-02-10 16:30:00', 'Pending', 'Pending', 17500.00, 2100.00, 19600.00, 'COD', '222 Elm St', 'Cebu', 'Philippines', 1, 3, 3);

        INSERT INTO OrderItems (OrderId, ProductId, ProductName, ProductCode, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0005'), 7, 'Logitech G502 X Plus', 'PRD-007', 1, 6500.00, 6500.00),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0005'), 8, 'Razer BlackWidow V4 Pro', 'PRD-008', 1, 11000.00, 11000.00);

        PRINT 'Inserted 5 Orders with Order Items';
    END
    GO

    -- =====================================================
    -- 4. ORDER STATUS HISTORY (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM OrderStatusHistory WHERE OrderId = (SELECT TOP 1 OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'))
    BEGIN
        INSERT INTO OrderStatusHistory (OrderId, PreviousStatus, NewStatus, Notes, ChangedBy) VALUES
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 'Pending', 'Confirmed', 'Payment received, order confirmed', 3),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 'Confirmed', 'Shipped', 'Package dispatched via LBC Express', 3),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 'Shipped', 'Delivered', 'Customer confirmed delivery', 3),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 'Pending', 'Confirmed', 'Bank transfer verified', 3),
        ((SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 'Confirmed', 'Processing', 'Items being prepared for shipment', 3);
        PRINT 'Inserted 5 Order Status History records';
    END
    GO

    -- =====================================================
    -- 5. INVOICES (5 records) + INVOICE ITEMS
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE InvoiceNumber = 'INV-2026-0001')
    BEGIN
        -- Invoice 1: For Order 1
        INSERT INTO Invoices (InvoiceNumber, OrderId, CustomerId, InvoiceDate, DueDate, Subtotal, TaxAmount, TotalAmount, PaidAmount, BalanceDue, Status, BillingName, BillingAddress, BillingCity, BillingCountry, PaymentTerms, CompanyId, CreatedBy) VALUES
        ('INV-2026-0001', (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 1, '2026-01-15', '2026-02-14', 34000.00, 4080.00, 38080.00, 38080.00, 0.00, 'Paid', 'Maria Santos', '123 Main St', 'Manila', 'Philippines', 'Net 30', 1, 6);

        INSERT INTO InvoiceItems (InvoiceId, ProductId, Description, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0001'), 1, 'Intel Core i7-13700K Processor', 1, 22500.00, 22500.00),
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0001'), 5, 'Corsair Vengeance 32GB DDR5 RAM', 1, 11500.00, 11500.00);

        -- Invoice 2: For Order 2
        INSERT INTO Invoices (InvoiceNumber, OrderId, CustomerId, InvoiceDate, DueDate, Subtotal, TaxAmount, TotalAmount, PaidAmount, BalanceDue, Status, BillingName, BillingAddress, BillingCity, BillingCountry, PaymentTerms, CompanyId, CreatedBy) VALUES
        ('INV-2026-0002', (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 2, '2026-01-20', '2026-02-19', 163500.00, 19620.00, 183120.00, 183120.00, 0.00, 'Paid', 'Juan Dela Cruz', '456 Oak Ave', 'Quezon City', 'Philippines', 'Net 30', 1, 6);

        INSERT INTO InvoiceItems (InvoiceId, ProductId, Description, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0002'), 3, 'NVIDIA GeForce RTX 4090 GPU', 1, 120000.00, 120000.00),
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0002'), 4, 'ASUS ROG Maximus Z790 Motherboard', 1, 32000.00, 32000.00),
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0002'), 5, 'Corsair Vengeance 32GB DDR5 RAM', 1, 11500.00, 11500.00);

        -- Invoice 3: For Order 3
        INSERT INTO Invoices (InvoiceNumber, OrderId, CustomerId, InvoiceDate, DueDate, Subtotal, TaxAmount, TotalAmount, PaidAmount, BalanceDue, Status, BillingName, BillingAddress, BillingCity, BillingCountry, PaymentTerms, CompanyId, CreatedBy) VALUES
        ('INV-2026-0003', (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0003'), 3, '2026-02-01', '2026-03-03', 7500.00, 900.00, 8400.00, 0.00, 8400.00, 'Sent', 'Ana Reyes', '789 Pine Rd', 'Makati', 'Philippines', 'Net 30', 1, 6);

        INSERT INTO InvoiceItems (InvoiceId, ProductId, Description, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0003'), 6, 'Samsung 990 Pro 1TB NVMe SSD', 1, 7500.00, 7500.00);

        -- Invoice 4: For Order 4
        INSERT INTO Invoices (InvoiceNumber, OrderId, CustomerId, InvoiceDate, DueDate, Subtotal, TaxAmount, TotalAmount, PaidAmount, BalanceDue, Status, BillingName, BillingAddress, BillingCity, BillingCountry, PaymentTerms, CompanyId, CreatedBy) VALUES
        ('INV-2026-0004', (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0004'), 4, '2026-02-05', '2026-03-07', 142500.00, 17100.00, 159600.00, 159600.00, 0.00, 'Paid', 'Tech Corp Inc.', '100 Business Park', 'BGC', 'Philippines', 'Net 30', 1, 6);

        INSERT INTO InvoiceItems (InvoiceId, ProductId, Description, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0004'), 1, 'Intel Core i7-13700K Processor x5', 5, 22500.00, 112500.00),
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0004'), 7, 'Logitech G502 X Plus Mouse x5', 5, 6000.00, 30000.00);

        -- Invoice 5: For Order 5
        INSERT INTO Invoices (InvoiceNumber, OrderId, CustomerId, InvoiceDate, DueDate, Subtotal, TaxAmount, TotalAmount, PaidAmount, BalanceDue, Status, BillingName, BillingAddress, BillingCity, BillingCountry, PaymentTerms, CompanyId, CreatedBy) VALUES
        ('INV-2026-0005', (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0005'), 5, '2026-02-10', '2026-03-12', 17500.00, 2100.00, 19600.00, 0.00, 19600.00, 'Pending', 'Pedro Garcia', '222 Elm St', 'Cebu', 'Philippines', 'Net 30', 1, 6);

        INSERT INTO InvoiceItems (InvoiceId, ProductId, Description, Quantity, UnitPrice, TotalPrice) VALUES
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0005'), 7, 'Logitech G502 X Plus Mouse', 1, 6500.00, 6500.00),
        ((SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0005'), 8, 'Razer BlackWidow V4 Pro Keyboard', 1, 11000.00, 11000.00);

        PRINT 'Inserted 5 Invoices with Invoice Items';
    END
    GO

    -- =====================================================
    -- 6. PAYMENTS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Payments WHERE PaymentNumber = 'PAY-2026-0001')
    BEGIN
        INSERT INTO Payments (PaymentNumber, InvoiceId, OrderId, CustomerId, PaymentDate, Amount, PaymentMethod, Status, ReferenceNumber, Currency, CompanyId, ProcessedBy) VALUES
        ('PAY-2026-0001', (SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0001'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 1, '2026-01-15 10:00:00', 38080.00, 'Credit Card', 'Completed', 'CC-REF-78901', 'PHP', 1, 6),
        ('PAY-2026-0002', (SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0002'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 2, '2026-01-20 15:30:00', 183120.00, 'Bank Transfer', 'Completed', 'BT-REF-12345', 'PHP', 1, 6),
        ('PAY-2026-0003', NULL, (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0003'), 3, '2026-02-02 11:00:00', 4200.00, 'GCash', 'Completed', 'GC-REF-55678', 'PHP', 1, 6),
        ('PAY-2026-0004', (SELECT InvoiceId FROM Invoices WHERE InvoiceNumber = 'INV-2026-0004'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0004'), 4, '2026-02-05 09:15:00', 159600.00, 'Bank Transfer', 'Completed', 'BT-REF-67890', 'PHP', 1, 6),
        ('PAY-2026-0005', NULL, NULL, 5, '2026-02-12 14:00:00', 5000.00, 'Maya', 'Pending', 'MY-REF-99001', 'PHP', 1, 6);
        PRINT 'Inserted 5 Payments';
    END
    GO

    -- =====================================================
    -- 7. REFUNDS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Refunds WHERE RefundNumber = 'REF-2026-0001')
    BEGIN
        INSERT INTO Refunds (RefundNumber, PaymentId, OrderId, CustomerId, Amount, Reason, Status, RefundMethod, RequestedBy, CompanyId) VALUES
        ('REF-2026-0001', (SELECT PaymentId FROM Payments WHERE PaymentNumber = 'PAY-2026-0001'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 1, 5000.00, 'Partial refund - product arrived with minor cosmetic damage', 'Approved', 'Original Method', 1, 1),
        ('REF-2026-0002', (SELECT PaymentId FROM Payments WHERE PaymentNumber = 'PAY-2026-0003'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0003'), 3, 8400.00, 'Customer requested cancellation before shipment', 'Processed', 'GCash', 3, 1),
        ('REF-2026-0003', (SELECT PaymentId FROM Payments WHERE PaymentNumber = 'PAY-2026-0004'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0004'), 4, 6000.00, 'One mouse unit was defective on arrival', 'Pending', 'Bank Transfer', 4, 1),
        ('REF-2026-0004', (SELECT PaymentId FROM Payments WHERE PaymentNumber = 'PAY-2026-0002'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 2, 32000.00, 'Motherboard incompatibility with existing setup', 'Rejected', 'Original Method', 2, 1),
        ('REF-2026-0005', (SELECT PaymentId FROM Payments WHERE PaymentNumber = 'PAY-2026-0001'), (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 1, 2000.00, 'Shipping delay compensation', 'Processed', 'Store Credit', 1, 1);
        PRINT 'Inserted 5 Refunds';
    END
    GO

    -- =====================================================
    -- 8. SUPPORT TICKETS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0001')
    BEGIN
        INSERT INTO SupportTickets (TicketNumber, CustomerId, CategoryId, OrderId, ContactName, ContactEmail, ContactPhone, Subject, Description, Priority, Status, AssignedTo, DueDate, Source, CompanyId) VALUES
        ('TKT-2026-0001', 1, 1, (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0001'), 'Maria Santos', 'maria.santos@email.com', '+63-912-345-6789', 'Processor running hot after installation', 'After installing the i7-13700K, temperatures are reaching 95°C under load. I am using the stock cooler. Is this normal or do I need a better cooling solution?', 'High', 'In Progress', 4, DATEADD(HOUR, 24, GETDATE()), 'Web', 1),
        ('TKT-2026-0002', 2, 3, (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0002'), 'Juan Dela Cruz', 'juan.dc@email.com', '+63-923-456-7890', 'Order still showing Processing status', 'My order ORD-2026-0002 has been in Processing status for 5 days now. When will it be shipped? I need these parts urgently for a project deadline.', 'Medium', 'Open', 4, DATEADD(HOUR, 12, GETDATE()), 'Email', 1),
        ('TKT-2026-0003', 3, 5, NULL, 'Ana Reyes', 'ana.r@email.com', '+63-934-567-8901', 'Request for return and refund', 'I would like to return the Samsung SSD I ordered. It is not compatible with my laptop model. The product is still sealed and unused. Please process a return.', 'Medium', 'Pending Customer', 4, DATEADD(HOUR, 24, GETDATE()), 'Web', 1),
        ('TKT-2026-0004', 4, 6, (SELECT OrderId FROM Orders WHERE OrderNumber = 'ORD-2026-0004'), 'Tech Corp Inc.', 'orders@techcorp.com', '+63-945-678-9012', 'Warranty claim for defective mouse', 'One of the 5 Logitech G502 X Plus mice from our bulk order is not working. The left click button is unresponsive. We need a warranty replacement ASAP.', 'High', 'Open', 4, DATEADD(HOUR, 24, GETDATE()), 'Phone', 1),
        ('TKT-2026-0005', 5, 4, NULL, 'Pedro Garcia', 'pedro.g@email.com', '+63-956-789-0123', 'Question about DDR5 RAM compatibility', 'I am planning to buy the Corsair Vengeance 32GB DDR5 RAM. Will it work with an ASRock B650 motherboard? Also, does it support XMP 3.0 profiles?', 'Low', 'Resolved', 4, DATEADD(HOUR, 48, GETDATE()), 'Chat', 1);

        -- Update resolved ticket
        UPDATE SupportTickets SET Resolution = 'Yes, the Corsair Vengeance DDR5 is fully compatible with ASRock B650 boards and supports both AMD EXPO and Intel XMP 3.0 profiles.', ResolvedAt = DATEADD(HOUR, -2, GETDATE()), ResolvedBy = 4
        WHERE TicketNumber = 'TKT-2026-0005';

        PRINT 'Inserted 5 Support Tickets';
    END
    GO

    -- =====================================================
    -- 9. TICKET MESSAGES (5 records)
    -- =====================================================
    IF EXISTS (SELECT 1 FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0001')
    AND NOT EXISTS (SELECT 1 FROM TicketMessages WHERE TicketId = (SELECT TOP 1 TicketId FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0001'))
    BEGIN
        INSERT INTO TicketMessages (TicketId, SenderType, SenderId, Message, IsInternal) VALUES
        ((SELECT TicketId FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0001'), 'Customer', NULL, 'Hi, I just installed my new i7-13700K and the temps are hitting 95°C under load with the stock cooler. Is this expected?', 0),
        ((SELECT TicketId FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0001'), 'Agent', 4, 'Hello Maria! The i7-13700K does run warm with the stock cooler. We recommend an aftermarket cooler like the Noctua NH-D15 or a 240mm AIO. Would you like us to suggest some options from our catalog?', 0),
        ((SELECT TicketId FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0002'), 'Customer', NULL, 'Hi, can someone update me on my order? It has been 5 days already.', 0),
        ((SELECT TicketId FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0003'), 'Agent', 4, 'Hi Ana, we have initiated the return process. Please ship the item back to our warehouse at the address below. Once received, the refund will be processed within 3-5 business days.', 0),
        ((SELECT TicketId FROM SupportTickets WHERE TicketNumber = 'TKT-2026-0005'), 'Agent', 4, 'Hi Pedro! Great news - the Corsair Vengeance DDR5 is fully compatible with ASRock B650 boards. It supports AMD EXPO and Intel XMP 3.0. You are good to go!', 0);
        PRINT 'Inserted 5 Ticket Messages';
    END
    ELSE
        PRINT 'SKIPPED Ticket Messages - parent SupportTickets not found';
    GO

    -- =====================================================
    -- 10. KNOWLEDGE BASE ARTICLES (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM KnowledgeArticles WHERE Slug = 'getting-started-with-compugear')
    BEGIN
        INSERT INTO KnowledgeArticles (CategoryId, Title, Slug, Content, Summary, Tags, ViewCount, HelpfulCount, NotHelpfulCount, Status, PublishedAt, CreatedBy) VALUES
        (1, 'Getting Started with Your CompuGear Account', 'getting-started-with-compugear', 
        '<h2>Welcome to CompuGear!</h2><p>This guide will help you set up your CompuGear account and start shopping for the best computer parts and peripherals.</p><h3>Step 1: Create Your Account</h3><p>Visit our website and click "Register" to create a new account. You will need your email address and a secure password.</p><h3>Step 2: Browse Products</h3><p>Use our catalog to browse through categories like Processors, Graphics Cards, RAM, Storage, and more.</p><h3>Step 3: Place Your Order</h3><p>Add items to your cart and proceed to checkout. We accept Credit Cards, GCash, Maya, and Bank Transfers.</p>', 
        'Complete guide to setting up and using your CompuGear account', 'getting started,account,setup,guide', 245, 52, 3, 'Published', '2026-01-01', 4),
        
        (2, 'Troubleshooting High CPU Temperatures', 'troubleshooting-high-cpu-temps', 
        '<h2>CPU Temperature Troubleshooting Guide</h2><p>If your CPU is running hot, follow these steps to diagnose and fix the issue.</p><h3>Common Causes</h3><ul><li>Insufficient thermal paste application</li><li>Stock cooler inadequate for high-end CPUs</li><li>Poor case airflow</li><li>Dust buildup on heatsink/fans</li></ul><h3>Solutions</h3><ol><li>Reapply thermal paste using the pea method</li><li>Upgrade to an aftermarket cooler (AIO or tower)</li><li>Add case fans for better airflow</li><li>Clean dust filters and heatsinks regularly</li></ol><h3>Recommended Temperatures</h3><p>Idle: 30-45°C | Light Load: 45-65°C | Heavy Load: 65-85°C | Throttle: 90°C+</p>', 
        'Guide to fixing high CPU temperature issues', 'cpu,temperature,overheating,cooling,thermal paste', 189, 41, 2, 'Published', '2026-01-05', 4),
        
        (3, 'Frequently Asked Questions - Orders & Shipping', 'faq-orders-shipping', 
        '<h2>Orders & Shipping FAQ</h2><h3>How long does shipping take?</h3><p>Metro Manila: 1-3 business days. Provincial: 3-7 business days. Remote areas: 7-14 business days.</p><h3>Can I track my order?</h3><p>Yes! Once shipped, you will receive a tracking number via email. You can also check order status in your Customer Portal.</p><h3>What if my order arrives damaged?</h3><p>Contact our support team within 48 hours of delivery with photos of the damage. We will arrange a replacement or refund.</p><h3>Can I cancel my order?</h3><p>Orders can be cancelled before they are shipped. Once shipped, you will need to process a return.</p>', 
        'Common questions about ordering and shipping', 'faq,orders,shipping,tracking,delivery', 312, 78, 5, 'Published', '2026-01-03', 4),
        
        (4, 'How to Choose the Right Graphics Card', 'choosing-right-graphics-card', 
        '<h2>Graphics Card Buying Guide</h2><p>Choosing the right GPU depends on your use case, budget, and system compatibility.</p><h3>For Gaming (1080p)</h3><p>Budget: RTX 4060 or RX 7600. These cards handle 1080p gaming at high settings with ease.</p><h3>For Gaming (1440p/4K)</h3><p>Mid-range: RTX 4070 Ti or RX 7800 XT. High-end: RTX 4090 for ultimate 4K performance.</p><h3>For Content Creation</h3><p>NVIDIA cards with CUDA cores are preferred for video editing (Premiere Pro, DaVinci Resolve) and 3D rendering (Blender).</p><h3>Key Specs to Consider</h3><ul><li>VRAM: 8GB minimum for modern games</li><li>Power Supply requirement</li><li>Physical size (check case clearance)</li><li>Display outputs (HDMI 2.1, DisplayPort 1.4)</li></ul>', 
        'Complete guide to selecting the right GPU for your needs', 'graphics card,gpu,nvidia,amd,gaming,buying guide', 156, 35, 1, 'Published', '2026-01-10', 4),
        
        (5, 'Warranty Policy and Claims Process', 'warranty-policy-claims', 
        '<h2>CompuGear Warranty Policy</h2><h3>Coverage</h3><p>All products sold by CompuGear come with manufacturer warranty. Warranty periods vary by product category:</p><ul><li>Processors: 3 years</li><li>Graphics Cards: 3 years</li><li>RAM: Lifetime</li><li>Storage (SSD): 5 years</li><li>Peripherals: 1-2 years</li></ul><h3>How to File a Warranty Claim</h3><ol><li>Log in to your Customer Portal</li><li>Go to Support > Create Ticket</li><li>Select "Warranty Claims" as category</li><li>Provide order number and describe the issue</li><li>Attach photos/videos of the defect</li></ol><h3>What is NOT Covered</h3><p>Physical damage, water damage, unauthorized modifications, and normal wear and tear are not covered under warranty.</p>', 
        'Understanding our warranty coverage and how to file claims', 'warranty,claims,return,refund,policy', 98, 22, 0, 'Published', '2026-01-08', 4);
        PRINT 'Inserted 5 Knowledge Base Articles';
    END
    GO

    -- =====================================================
    -- 11. CAMPAIGNS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Campaigns WHERE CampaignCode = 'CMP-001')
    BEGIN
        INSERT INTO Campaigns (CampaignCode, CampaignName, Description, Type, Status, StartDate, EndDate, Budget, ActualSpend, TargetSegment, TotalReach, Impressions, Clicks, Conversions, Revenue, CompanyId, CreatedBy) VALUES
        ('CMP-001', 'Summer Tech Sale 2026', 'Massive discounts on all computer components this summer', 'Email', 'Active', '2026-01-01', '2026-03-31', 50000.00, 12500.00, 'All Customers', 15000, 45000, 2200, 180, 850000.00, 1, 5),
        ('CMP-002', 'Gaming Gear Promo', 'Special bundles for gaming peripherals and components', 'Social Media', 'Active', '2026-02-01', '2026-02-28', 30000.00, 8000.00, 'High Value', 8500, 25000, 1500, 95, 320000.00, 1, 5),
        ('CMP-003', 'Corporate IT Solutions', 'Targeted campaign for business and corporate clients', 'Email', 'Scheduled', '2026-03-01', '2026-04-30', 75000.00, 0.00, 'VIP', 0, 0, 0, 0, 0.00, 1, 5),
        ('CMP-004', 'Back to School Deals', 'Student-friendly laptop and accessory packages', 'PPC', 'Draft', '2026-06-01', '2026-07-31', 40000.00, 0.00, 'New Customers', 0, 0, 0, 0, 0.00, 1, 5),
        ('CMP-005', 'Flash Friday Deals', 'Weekly flash sale every Friday with exclusive discounts', 'SMS', 'Active', '2026-01-10', '2026-12-31', 60000.00, 18000.00, 'Newsletter Subscribers', 22000, 66000, 4100, 310, 1200000.00, 1, 5);
        PRINT 'Inserted 5 Campaigns';
    END
    GO

    -- =====================================================
    -- 12. PROMOTIONS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Promotions WHERE PromotionCode = 'SUMMER10')
    BEGIN
        INSERT INTO Promotions (PromotionCode, PromotionName, Description, DiscountType, DiscountValue, MinOrderAmount, MaxDiscountAmount, StartDate, EndDate, UsageLimit, TimesUsed, IsActive, CampaignId, CompanyId, CreatedBy) VALUES
        ('SUMMER10', 'Summer 10% Off', '10% discount on all orders over PHP 10,000', 'Percentage', 10.00, 10000.00, 15000.00, '2026-01-01', '2026-03-31', 500, 45, 1, (SELECT CampaignId FROM Campaigns WHERE CampaignCode = 'CMP-001'), 1, 5),
        ('GAMER2026', 'Gamer Bundle Discount', 'PHP 2,000 off on gaming peripherals bundle', 'FixedAmount', 2000.00, 15000.00, NULL, '2026-02-01', '2026-02-28', 200, 23, 1, (SELECT CampaignId FROM Campaigns WHERE CampaignCode = 'CMP-002'), 1, 5),
        ('FREESHIP', 'Free Shipping Weekend', 'Free shipping on all orders', 'FreeShipping', 0.00, 5000.00, NULL, '2026-02-14', '2026-02-16', 1000, 156, 1, NULL, 1, 5),
        ('WELCOME500', 'New Customer Welcome', 'PHP 500 off on first order', 'FixedAmount', 500.00, 3000.00, NULL, '2026-01-01', '2026-12-31', NULL, 89, 1, NULL, 1, 5),
        ('FLASH25', 'Flash Friday 25% Off', '25% off on selected items every Friday', 'Percentage', 25.00, 8000.00, 10000.00, '2026-01-10', '2026-12-31', 100, 67, 1, (SELECT CampaignId FROM Campaigns WHERE CampaignCode = 'CMP-005'), 1, 5);
        PRINT 'Inserted 5 Promotions';
    END
    GO

    -- =====================================================
    -- 13. PURCHASE ORDERS (5 records) + ITEMS
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM PurchaseOrders WHERE PurchaseOrderId >= 1 AND EXISTS(SELECT 1 FROM Suppliers WHERE SupplierCode = 'SUP-001'))
    BEGIN
        DECLARE @Sup1 INT = (SELECT SupplierId FROM Suppliers WHERE SupplierCode = 'SUP-001');
        DECLARE @Sup2 INT = (SELECT SupplierId FROM Suppliers WHERE SupplierCode = 'SUP-002');
        DECLARE @Sup3 INT = (SELECT SupplierId FROM Suppliers WHERE SupplierCode = 'SUP-003');
        DECLARE @Sup4 INT = (SELECT SupplierId FROM Suppliers WHERE SupplierCode = 'SUP-004');
        DECLARE @Sup5 INT = (SELECT SupplierId FROM Suppliers WHERE SupplierCode = 'SUP-005');

        INSERT INTO PurchaseOrders (SupplierId, OrderDate, ExpectedDeliveryDate, Status, TotalAmount, Notes, CompanyId) VALUES
        (@Sup1, '2026-01-10', '2026-01-25', 'Completed', 750000.00, 'Bulk order for Intel processors', 1),
        (@Sup2, '2026-01-15', '2026-02-01', 'Completed', 500000.00, 'GPU and motherboard restock', 1),
        (@Sup3, '2026-02-01', '2026-02-15', 'Shipped', 320000.00, 'RAM and SSD inventory replenishment', 1),
        (@Sup4, '2026-02-10', '2026-03-01', 'Pending', 450000.00, 'Peripheral accessories from HK supplier', 1),
        (@Sup5, '2026-02-12', '2026-02-20', 'Approved', 180000.00, 'Emergency stock for flash sale items', 1);

        -- PO Items (using latest PO IDs)
        DECLARE @PO1 INT = (SELECT TOP 1 PurchaseOrderId FROM PurchaseOrders WHERE SupplierId = @Sup1 ORDER BY PurchaseOrderId DESC);
        DECLARE @PO2 INT = (SELECT TOP 1 PurchaseOrderId FROM PurchaseOrders WHERE SupplierId = @Sup2 ORDER BY PurchaseOrderId DESC);
        DECLARE @PO3 INT = (SELECT TOP 1 PurchaseOrderId FROM PurchaseOrders WHERE SupplierId = @Sup3 ORDER BY PurchaseOrderId DESC);
        DECLARE @PO4 INT = (SELECT TOP 1 PurchaseOrderId FROM PurchaseOrders WHERE SupplierId = @Sup4 ORDER BY PurchaseOrderId DESC);
        DECLARE @PO5 INT = (SELECT TOP 1 PurchaseOrderId FROM PurchaseOrders WHERE SupplierId = @Sup5 ORDER BY PurchaseOrderId DESC);

        INSERT INTO PurchaseOrderItems (PurchaseOrderId, ProductId, Quantity, UnitPrice, Subtotal) VALUES
        (@PO1, 1, 50, 15000.00, 750000.00),
        (@PO2, 3, 10, 90000.00, 900000.00),  -- Note: Not matching total exactly but realistic
        (@PO2, 4, 15, 25000.00, 375000.00),
        (@PO3, 5, 100, 8000.00, 800000.00),
        (@PO3, 6, 50, 5500.00, 275000.00),
        (@PO4, 7, 60, 4500.00, 270000.00),
        (@PO4, 8, 40, 8000.00, 320000.00),
        (@PO5, 1, 20, 15000.00, 300000.00);

        PRINT 'Inserted 5 Purchase Orders with Items';
    END
    GO

    -- =====================================================
    -- 14. INVENTORY TRANSACTIONS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM InventoryTransactions WHERE ReferenceType = 'PurchaseOrder' AND Notes LIKE '%sample data%')
    BEGIN
        INSERT INTO InventoryTransactions (ProductId, TransactionType, Quantity, PreviousStock, NewStock, UnitCost, TotalCost, ReferenceType, Notes, CreatedBy) VALUES
        (1, 'Purchase', 50, 50, 100, 15000.00, 750000.00, 'PurchaseOrder', 'Stock received from TechParts Philippines - sample data', 7),
        (3, 'Sale', -1, 15, 14, 90000.00, 90000.00, 'Order', 'Sold via order ORD-2026-0002 - sample data', 7),
        (5, 'Sale', -2, 100, 98, 8000.00, 16000.00, 'Order', 'Sold via orders ORD-2026-0001 and ORD-2026-0002 - sample data', 7),
        (7, 'Adjustment', -2, 60, 58, 4500.00, 9000.00, 'Adjustment', 'Stock adjustment - damaged units removed - sample data', 7),
        (6, 'Return', 1, 74, 75, 5500.00, 5500.00, 'Order', 'Customer return from order ORD-2026-0003 - sample data', 7);
        PRINT 'Inserted 5 Inventory Transactions';
    END
    GO

    -- =====================================================
    -- 15. STOCK ALERTS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM StockAlerts WHERE ProductId = 3 AND AlertType = 'LowStock')
    BEGIN
        INSERT INTO StockAlerts (ProductId, AlertType, CurrentStock, ThresholdLevel, IsResolved) VALUES
        (3, 'LowStock', 14, 15, 0),
        (4, 'LowStock', 25, 30, 0),
        (8, 'LowStock', 8, 10, 0),
        (1, 'LowStock', 10, 10, 1),
        (6, 'LowStock', 15, 15, 1);
        PRINT 'Inserted 5 Stock Alerts';
    END
    GO

    -- =====================================================
    -- 16. CHAT SESSIONS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM ChatSessions WHERE SessionToken = 'ses-2026-token-001')
    BEGIN
        INSERT INTO ChatSessions (CustomerId, SessionToken, Status, AgentId, StartedAt, EndedAt, Source, TotalMessages, Rating, CompanyId) VALUES
        (1, 'ses-2026-token-001', 'Ended', 4, '2026-01-15 09:00:00', '2026-01-15 09:25:00', 'Website', 6, 5, 1),
        (2, 'ses-2026-token-002', 'Active', 4, '2026-02-14 14:00:00', NULL, 'Website', 3, NULL, 1),
        (3, 'ses-2026-token-003', 'Ended', 4, '2026-02-01 11:00:00', '2026-02-01 11:30:00', 'Website', 4, 4, 1),
        (5, 'ses-2026-token-004', 'Active', NULL, '2026-02-15 08:30:00', NULL, 'Website', 2, NULL, 1),
        (4, 'ses-2026-token-005', 'Ended', 4, '2026-02-05 10:00:00', '2026-02-05 10:45:00', 'Website', 8, 5, 1);
        PRINT 'Inserted 5 Chat Sessions';
    END
    GO

    -- =====================================================
    -- 17. CHAT MESSAGES (5 records)
    -- =====================================================
    IF EXISTS (SELECT 1 FROM ChatSessions WHERE SessionToken = 'ses-2026-token-001')
    AND NOT EXISTS (SELECT 1 FROM ChatMessages WHERE SessionId = (SELECT TOP 1 SessionId FROM ChatSessions WHERE SessionToken = 'ses-2026-token-001'))
    BEGIN
        INSERT INTO ChatMessages (SessionId, SenderType, SenderId, Message, MessageType, IsRead) VALUES
        ((SELECT SessionId FROM ChatSessions WHERE SessionToken = 'ses-2026-token-001'), 'Customer', NULL, 'Hi! I need help choosing between the i7-13700K and Ryzen 9 7950X for video editing.', 'Text', 1),
        ((SELECT SessionId FROM ChatSessions WHERE SessionToken = 'ses-2026-token-001'), 'Agent', 4, 'Hello Maria! For video editing, the Ryzen 9 7950X with its 16 cores will give you better multi-threaded performance. The i7-13700K is also excellent but has fewer cores. What software do you use?', 'Text', 1),
        ((SELECT SessionId FROM ChatSessions WHERE SessionToken = 'ses-2026-token-002'), 'Customer', NULL, 'When will my order ORD-2026-0002 be shipped? I placed it 5 days ago.', 'Text', 1),
        ((SELECT SessionId FROM ChatSessions WHERE SessionToken = 'ses-2026-token-002'), 'Agent', 4, 'Hi Juan! Let me check that for you. Your order is currently being processed and should ship within the next 24 hours.', 'Text', 1),
        ((SELECT SessionId FROM ChatSessions WHERE SessionToken = 'ses-2026-token-004'), 'Customer', NULL, 'Hello, do you have the Razer BlackWidow V4 Pro in stock? I want to order it today.', 'Text', 0);
        PRINT 'Inserted 5 Chat Messages';
    END
    ELSE
        PRINT 'SKIPPED Chat Messages - parent ChatSessions not found';
    GO

    -- =====================================================
    -- 18. CUSTOMER ADDRESSES (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM CustomerAddresses WHERE CustomerId = 1 AND AddressType = 'Shipping')
    BEGIN
        INSERT INTO CustomerAddresses (CustomerId, AddressType, AddressLine1, AddressLine2, City, State, ZipCode, Country, IsDefault) VALUES
        (1, 'Shipping', '123 Main Street', 'Barangay San Miguel', 'Manila', 'Metro Manila', '1000', 'Philippines', 1),
        (2, 'Shipping', '456 Oak Avenue', 'Novaliches', 'Quezon City', 'Metro Manila', '1100', 'Philippines', 1),
        (3, 'Both', '789 Pine Road', 'Legaspi Village', 'Makati', 'Metro Manila', '1200', 'Philippines', 1),
        (4, 'Billing', '100 Business Park Tower A', '26th Floor', 'BGC, Taguig', 'Metro Manila', '1630', 'Philippines', 1),
        (5, 'Shipping', '222 Elm Street', 'Lahug', 'Cebu City', 'Cebu', '6000', 'Philippines', 1);
        PRINT 'Inserted 5 Customer Addresses';
    END
    GO

    -- =====================================================
    -- 19. SEGMENT MEMBERS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM SegmentMembers WHERE SegmentId = 1 AND CustomerId = 1)
    BEGIN
        INSERT INTO SegmentMembers (SegmentId, CustomerId) VALUES
        (1, 1), -- All Customers - Maria
        (1, 2), -- All Customers - Juan
        (3, 2), -- High Value - Juan (big order)
        (5, 4), -- VIP - Tech Corp
        (6, 1); -- Newsletter - Maria
        PRINT 'Inserted 5 Segment Members';
    END
    GO

    -- =====================================================
    -- 20. ACTIVITY LOGS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM ActivityLogs WHERE Description LIKE '%sample activity%')
    BEGIN
        INSERT INTO ActivityLogs (UserId, UserName, Action, Module, EntityType, Description) VALUES
        (3, 'sarah.johnson', 'Create', 'Orders', 'Orders', 'Created new order ORD-2026-0001 for customer Maria Santos - sample activity'),
        (6, 'john.doe', 'Create', 'Billing', 'Invoices', 'Generated invoice INV-2026-0001 for order ORD-2026-0001 - sample activity'),
        (4, 'mike.chen', 'Update', 'Support', 'SupportTickets', 'Assigned ticket TKT-2026-0001 to self and started investigating - sample activity'),
        (5, 'emily.brown', 'Create', 'Marketing', 'Campaigns', 'Created Summer Tech Sale 2026 campaign - sample activity'),
        (7, 'james.wilson', 'Update', 'Inventory', 'Products', 'Updated stock quantity for Intel Core i7-13700K after receiving PO - sample activity');
        PRINT 'Inserted 5 Activity Logs';
    END
    GO

    -- =====================================================
    -- 21. NOTIFICATIONS (5 records)
    -- =====================================================
    IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Title LIKE '%sample%' OR Title = 'New Order Received')
    BEGIN
        INSERT INTO Notifications (UserId, Type, Title, Message, Link, IsRead) VALUES
        (3, 'Order', 'New Order Received', 'New order ORD-2026-0004 from Tech Corp Inc. worth PHP 159,600.00', '/Admin/Sales/Orders', 0),
        (4, 'Support', 'New Support Ticket', 'Ticket TKT-2026-0004: Warranty claim from Tech Corp Inc.', '/Admin/Support/Tickets', 0),
        (6, 'Payment', 'Payment Received', 'Payment PAY-2026-0002 of PHP 183,120.00 from Juan Dela Cruz', '/Admin/Billing/Payments', 1),
        (7, 'System', 'Low Stock Alert', 'NVIDIA GeForce RTX 4090 stock is below reorder level (14 units)', '/Admin/Inventory/Alerts', 0),
        (5, 'Marketing', 'Campaign Performance Update', 'Flash Friday Deals campaign reached 22,000 customers with 310 conversions', '/Admin/Marketing/Campaigns', 1);
        PRINT 'Inserted 5 Notifications';
    END
    GO

    -- =====================================================
    -- 22. UPDATE CUSTOMER TOTALS
    -- =====================================================
    UPDATE c SET 
        c.TotalOrders = ISNULL(stats.OrderCount, 0),
        c.TotalSpent = ISNULL(stats.TotalSpent, 0)
    FROM Customers c
    LEFT JOIN (
        SELECT CustomerId, COUNT(*) AS OrderCount, SUM(TotalAmount) AS TotalSpent
        FROM Orders
        GROUP BY CustomerId
    ) stats ON c.CustomerId = stats.CustomerId;

    PRINT 'Updated Customer order totals';
    GO

    -- =====================================================
    -- 23. UPDATE CUSTOMER SEGMENTS COUNT
    -- =====================================================
    UPDATE cs SET CustomerCount = ISNULL(cnt.MemberCount, 0)
    FROM CustomerSegments cs
    LEFT JOIN (
        SELECT SegmentId, COUNT(*) AS MemberCount
        FROM SegmentMembers
        GROUP BY SegmentId
    ) cnt ON cs.SegmentId = cnt.SegmentId;

    PRINT 'Updated Customer Segment counts';
    GO

    PRINT '========================================';
    PRINT 'Sample Data Insert Complete!';
    PRINT '========================================';
    PRINT '';
    PRINT 'Summary:';
    PRINT '  - 5 Suppliers';
    PRINT '  - 5 Leads';
    PRINT '  - 5 Orders (with 13 Order Items)';
    PRINT '  - 5 Order Status History records';
    PRINT '  - 5 Invoices (with Invoice Items)';
    PRINT '  - 5 Payments';
    PRINT '  - 5 Refunds';
    PRINT '  - 5 Support Tickets';
    PRINT '  - 5 Ticket Messages';
    PRINT '  - 5 Knowledge Base Articles';
    PRINT '  - 5 Campaigns';
    PRINT '  - 5 Promotions';
    PRINT '  - 5 Purchase Orders (with Items)';
    PRINT '  - 5 Inventory Transactions';
    PRINT '  - 5 Stock Alerts';
    PRINT '  - 5 Chat Sessions';
    PRINT '  - 5 Chat Messages';
    PRINT '  - 5 Customer Addresses';
    PRINT '  - 5 Segment Members';
    PRINT '  - 5 Activity Logs';
    PRINT '  - 5 Notifications';
    PRINT '';
    PRINT 'All data assigned to CompanyId = 1';
    PRINT 'Login credentials: password123 (all users)';
    GO
