-- Seed 10 Support Tickets for Customer: vincenzocassano@yahoo.com (CustomerId = 7)
-- Orders available: 26-30 (ORD-2026-0026 to ORD-2026-0030)
-- Categories: 1=Technical Support, 2=Billing Inquiry, 3=Order Issue, 4=Product Information,
--             5=Returns & Refunds, 6=Warranty Claims, 7=Account Issues, 8=General Inquiry

INSERT INTO SupportTickets 
    (TicketNumber, CustomerId, CategoryId, OrderId, ContactName, ContactEmail, ContactPhone, Subject, Description, Priority, Status, Source, CreatedAt, UpdatedAt, CompanyId)
VALUES
    ('TKT-2026-0004', 7, 3, 30, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Order ORD-2026-0030 not yet shipped',
     'Hi, I placed order ORD-2026-0030 three days ago and the status still shows Confirmed. Could you please provide an update on when it will be shipped? I need the items by next week for a project deadline.',
     'High', 'Open', 'Web', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), 1),

    ('TKT-2026-0005', 7, 1, NULL, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Unable to connect new GPU to motherboard',
     'I recently purchased an RTX 4070 Ti from your store and I am having trouble getting it to work with my ASUS ROG Strix B650 motherboard. The card powers on but no display output is detected. I have tried re-seating it and using different PCIe slots. Could your technical team help me troubleshoot?',
     'High', 'In Progress', 'Web', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 1),

    ('TKT-2026-0006', 7, 2, 26, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Double charged for order ORD-2026-0026',
     'I noticed that my credit card was charged twice for order ORD-2026-0026. The total amount of the order is PHP 45,500 but I see two charges of the same amount on my bank statement. Please investigate and process a refund for the duplicate charge as soon as possible.',
     'High', 'Open', 'Web', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), 1),

    ('TKT-2026-0007', 7, 5, 27, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Return request for defective RAM module',
     'One of the 2x16GB DDR5 RAM modules from order ORD-2026-0027 appears to be defective. My system only detects 16GB instead of 32GB. I have tested each stick individually and confirmed that one module is dead on arrival. I would like to initiate a return and replacement.',
     'Medium', 'Resolved', 'Web', DATEADD(DAY, -10, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), 1),

    ('TKT-2026-0008', 7, 6, 28, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Warranty claim for power supply unit',
     'The Corsair RM850x PSU from order ORD-2026-0028 stopped working after 2 months. The unit no longer powers on and I can smell a faint burning odor. This should be covered under the manufacturer warranty. Please advise on how to proceed with the warranty claim.',
     'Medium', 'In Progress', 'Web', DATEADD(DAY, -7, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), 1),

    ('TKT-2026-0009', 7, 4, NULL, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Compatibility check for custom PC build',
     'I am planning a custom PC build and wanted to verify compatibility before ordering. Can you confirm that the following parts will work together: AMD Ryzen 9 7950X, ASUS ROG Crosshair X670E Hero, G.Skill Trident Z5 RGB DDR5 6000MHz 64GB, and NVIDIA RTX 4090 Founders Edition?',
     'Low', 'Closed', 'Web', DATEADD(DAY, -14, GETUTCDATE()), DATEADD(DAY, -12, GETUTCDATE()), 1),

    ('TKT-2026-0010', 7, 7, NULL, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Cannot update shipping address in my account',
     'I am trying to update my default shipping address in my account settings but the save button does not respond when clicked. I have tried different browsers (Chrome, Firefox, Edge) and cleared my cache. This is urgent because I have upcoming orders that need to ship to my new address.',
     'High', 'Open', 'Web', DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), 1),

    ('TKT-2026-0011', 7, 8, NULL, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'How to track loyalty points and rewards',
     'I have been a regular customer and would like to know how I can track my loyalty points balance. Is there a rewards dashboard? Also, are there any ongoing promotions or discounts for returning customers? I saw something about a VIP tier program on your website but could not find more details.',
     'Low', 'Resolved', 'Web', DATEADD(DAY, -20, GETUTCDATE()), DATEADD(DAY, -18, GETUTCDATE()), 1),

    ('TKT-2026-0012', 7, 3, 29, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Wrong item received in order ORD-2026-0029',
     'I received order ORD-2026-0029 today but one of the items is incorrect. I ordered an Intel Core i7-14700K but received an Intel Core i5-14600K instead. The invoice shows the correct item but the physical product is wrong. Please arrange for a replacement to be sent.',
     'High', 'In Progress', 'Web', DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), 1),

    ('TKT-2026-0013', 7, 1, NULL, 'Vincenzo Cassano', 'vincenzocassano@yahoo.com', '+63 912 345 6789',
     'Blue screen after installing new SSD',
     'After installing the Samsung 990 Pro 2TB NVMe SSD I purchased from your store, my PC keeps getting BSOD with error code INACCESSIBLE_BOOT_DEVICE. I have updated my BIOS to the latest version and tried different M.2 slots. The SSD is detected in BIOS but Windows crashes during boot. Need urgent technical help.',
     'Medium', 'Open', 'Web', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()), 1);

-- Also insert some ticket messages for realism
INSERT INTO TicketMessages (TicketId, SenderType, SenderId, Message, IsInternal, CreatedAt)
SELECT t.TicketId, 'Customer', 7, 'Hi, just following up on this ticket. Any updates?', 0, DATEADD(DAY, -1, GETUTCDATE())
FROM SupportTickets t WHERE t.TicketNumber = 'TKT-2026-0005';

INSERT INTO TicketMessages (TicketId, SenderType, SenderId, Message, IsInternal, CreatedAt)
SELECT t.TicketId, 'Agent', NULL, 'Hello Vincenzo, we have tested a similar configuration in our lab and identified the issue. Please try updating your BIOS to version 1802 and make sure to enable ''Above 4G Decoding'' in BIOS settings. Let us know if that resolves the display output issue.', 0, DATEADD(DAY, -2, GETUTCDATE())
FROM SupportTickets t WHERE t.TicketNumber = 'TKT-2026-0005';

INSERT INTO TicketMessages (TicketId, SenderType, SenderId, Message, IsInternal, CreatedAt)
SELECT t.TicketId, 'Agent', NULL, 'We have processed your return request. A prepaid shipping label has been sent to your email. Once we receive the defective RAM module, a replacement will be shipped within 1-2 business days.', 0, DATEADD(DAY, -6, GETUTCDATE())
FROM SupportTickets t WHERE t.TicketNumber = 'TKT-2026-0007';

INSERT INTO TicketMessages (TicketId, SenderType, SenderId, Message, IsInternal, CreatedAt)
SELECT t.TicketId, 'Customer', 7, 'Thank you! I received the replacement and it is working perfectly now. Great service!', 0, DATEADD(DAY, -4, GETUTCDATE())
FROM SupportTickets t WHERE t.TicketNumber = 'TKT-2026-0007';

INSERT INTO TicketMessages (TicketId, SenderType, SenderId, Message, IsInternal, CreatedAt)
SELECT t.TicketId, 'Agent', NULL, 'Hi Vincenzo, all the components you listed are fully compatible with each other. The Ryzen 9 7950X pairs perfectly with the X670E Hero motherboard, and the DDR5-6000 RAM is well within EXPO support range. The RTX 4090 FE will also fit with no clearance issues in most ATX cases. You are good to go!', 0, DATEADD(DAY, -13, GETUTCDATE())
FROM SupportTickets t WHERE t.TicketNumber = 'TKT-2026-0009';

INSERT INTO TicketMessages (TicketId, SenderType, SenderId, Message, IsInternal, CreatedAt)
SELECT t.TicketId, 'Customer', 7, 'Perfect, thank you for confirming! I will place the order this week.', 0, DATEADD(DAY, -12, GETUTCDATE())
FROM SupportTickets t WHERE t.TicketNumber = 'TKT-2026-0009';
