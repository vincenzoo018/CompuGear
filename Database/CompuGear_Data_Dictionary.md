# Data Dictionary
## CompuGear - Customer Relationship Management (CRM)

---

## CORE TABLES

### Roles Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| RoleId-PK | Int-AI | 9 | Role's unique ID Number |
| RoleName | Varchar | 50 | Name of the role |
| Description | Varchar | 255 | Description of the role |
| AccessLevel | Int | 9 | Access level (1-7) |
| IsActive | Bit | 1 | Role active status |

---

### Users Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| UserId-PK | Int-AI | 9 | User's unique ID Number |
| RoleId-FK | Int | 9 | Reference to Roles table |
| Username | Varchar | 50 | User's login username |
| Email | Varchar | 100 | User's email address |
| PasswordHash | Varchar | 255 | User's encrypted password |
| FirstName | Varchar | 50 | User's first name |
| LastName | Varchar | 50 | User's last name |
| Phone | Varchar | 20 | User's phone number |
| IsActive | Bit | 1 | User active status |
| CreatedAt | Datetime | - | Date and time user was created |

---

## CUSTOMER MODULE

### Customers Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| CustomerId-PK | Int-AI | 9 | Customer's unique ID Number |
| CustomerCode | Varchar | 20 | Customer's reference code |
| FirstName | Varchar | 50 | Customer's first name |
| LastName | Varchar | 50 | Customer's last name |
| Email | Varchar | 100 | Customer's email address |
| Phone | Varchar | 20 | Customer's phone number |
| Address | Varchar | 255 | Customer's address |
| City | Varchar | 50 | Customer's city |
| Country | Varchar | 50 | Customer's country |
| Status | Varchar | 20 | Customer status (Active/Inactive) |
| TotalOrders | Int | 9 | Total number of orders placed |
| TotalSpent | Decimal | 18,2 | Total amount spent by customer |
| CreatedAt | Datetime | - | Date customer was registered |

---

## INVENTORY MODULE

### ProductCategories Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| CategoryId-PK | Int-AI | 9 | Category's unique ID Number |
| CategoryName | Varchar | 100 | Name of product category |
| Description | Varchar | 255 | Description of category |
| IsActive | Bit | 1 | Category active status |

---

### Brands Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| BrandId-PK | Int-AI | 9 | Brand's unique ID Number |
| BrandName | Varchar | 100 | Name of the brand |
| IsActive | Bit | 1 | Brand active status |

---

### Suppliers Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| SupplierId-PK | Int-AI | 9 | Supplier's unique ID Number |
| SupplierCode | Varchar | 20 | Supplier's reference code |
| SupplierName | Varchar | 100 | Name of the supplier |
| ContactPerson | Varchar | 100 | Supplier's contact person |
| Email | Varchar | 100 | Supplier's email address |
| Phone | Varchar | 20 | Supplier's phone number |
| Address | Varchar | 255 | Supplier's address |
| Status | Varchar | 20 | Supplier status (Active/Inactive) |

---

### Products Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| ProductId-PK | Int-AI | 9 | Product's unique ID Number |
| CategoryId-FK | Int | 9 | Reference to ProductCategories table |
| BrandId-FK | Int | 9 | Reference to Brands table |
| SupplierId-FK | Int | 9 | Reference to Suppliers table |
| ProductCode | Varchar | 50 | Product's reference code |
| SKU | Varchar | 50 | Stock Keeping Unit code |
| ProductName | Varchar | 200 | Name of the product |
| Description | Varchar | 500 | Product description |
| CostPrice | Decimal | 18,2 | Product cost price |
| SellingPrice | Decimal | 18,2 | Product selling price |
| StockQuantity | Int | 9 | Current stock quantity |
| ReorderLevel | Int | 9 | Minimum stock level for reorder |
| ImageUrl | Varchar | 255 | Product image URL |
| Status | Varchar | 20 | Product status (Active/Inactive) |
| CreatedAt | Datetime | - | Date product was added |

---

### StockAlerts Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| AlertId-PK | Int-AI | 9 | Alert's unique ID Number |
| ProductId-FK | Int | 9 | Reference to Products table |
| AlertType | Varchar | 30 | Type of alert (LowStock/OutOfStock) |
| CurrentStock | Int | 9 | Current stock level when alert created |
| ThresholdLevel | Int | 9 | Stock threshold that triggered alert |
| IsResolved | Bit | 1 | Alert resolved status |
| CreatedAt | Datetime | - | Date alert was created |

---

### PurchaseOrders Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| PurchaseOrderId-PK | Int-AI | 9 | Purchase Order's unique ID Number |
| SupplierId-FK | Int | 9 | Reference to Suppliers table |
| OrderDate | Datetime | - | Date order was placed |
| ExpectedDeliveryDate | Datetime | - | Expected delivery date |
| Status | Varchar | 20 | Order status (Pending/Approved/Completed) |
| TotalAmount | Decimal | 18,2 | Total order amount |
| Notes | Varchar | 500 | Additional notes |

---

### PurchaseOrderItems Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| ItemId-PK | Int-AI | 9 | Item's unique ID Number |
| PurchaseOrderId-FK | Int | 9 | Reference to PurchaseOrders table |
| ProductId-FK | Int | 9 | Reference to Products table |
| Quantity | Int | 9 | Quantity ordered |
| UnitPrice | Decimal | 18,2 | Price per unit |
| Subtotal | Decimal | 18,2 | Line item subtotal |

---

## SALES MODULE

### Leads Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| LeadId-PK | Int-AI | 9 | Lead's unique ID Number |
| AssignedTo-FK | Int | 9 | Reference to Users table |
| FirstName | Varchar | 50 | Lead's first name |
| LastName | Varchar | 50 | Lead's last name |
| Email | Varchar | 100 | Lead's email address |
| Phone | Varchar | 20 | Lead's phone number |
| CompanyName | Varchar | 100 | Lead's company name |
| Source | Varchar | 50 | Lead source (Website/Referral/etc.) |
| Status | Varchar | 30 | Lead status (New/Contacted/Qualified) |
| EstimatedValue | Decimal | 18,2 | Estimated deal value |
| Notes | Varchar | 500 | Additional notes |
| CreatedAt | Datetime | - | Date lead was created |

---

### Orders Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| OrderId-PK | Int-AI | 9 | Order's unique ID Number |
| CustomerId-FK | Int | 9 | Reference to Customers table |
| OrderNumber | Varchar | 30 | Order reference number |
| OrderDate | Datetime | - | Date order was placed |
| OrderStatus | Varchar | 30 | Order status (Pending/Processing/Shipped) |
| PaymentStatus | Varchar | 30 | Payment status (Pending/Paid) |
| Subtotal | Decimal | 18,2 | Order subtotal |
| TaxAmount | Decimal | 18,2 | Tax amount |
| TotalAmount | Decimal | 18,2 | Total order amount |
| PaymentMethod | Varchar | 50 | Payment method used |
| ShippingAddress | Varchar | 255 | Shipping address |
| Notes | Varchar | 500 | Additional notes |
| CreatedAt | Datetime | - | Date order was created |

---

### OrderItems Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| ItemId-PK | Int-AI | 9 | Item's unique ID Number |
| OrderId-FK | Int | 9 | Reference to Orders table |
| ProductId-FK | Int | 9 | Reference to Products table |
| ProductName | Varchar | 200 | Product name at time of order |
| Quantity | Int | 9 | Quantity ordered |
| UnitPrice | Decimal | 18,2 | Price per unit |
| TotalPrice | Decimal | 18,2 | Line item total |

---

## SUPPORT MODULE

### SupportTickets Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| TicketId-PK | Int-AI | 9 | Ticket's unique ID Number |
| CustomerId-FK | Int | 9 | Reference to Customers table |
| AssignedTo-FK | Int | 9 | Reference to Users table |
| TicketNumber | Varchar | 20 | Ticket reference number |
| Subject | Varchar | 200 | Ticket subject |
| Description | Text | - | Detailed description of issue |
| Priority | Varchar | 20 | Priority level (Low/Medium/High) |
| Status | Varchar | 30 | Ticket status (Open/In Progress/Resolved) |
| CreatedAt | Datetime | - | Date ticket was created |
| ResolvedAt | Datetime | - | Date ticket was resolved |

---

### TicketMessages Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| MessageId-PK | Int-AI | 9 | Message's unique ID Number |
| TicketId-FK | Int | 9 | Reference to SupportTickets table |
| SenderType | Varchar | 20 | Sender type (Customer/Agent) |
| SenderId | Int | 9 | ID of the sender |
| Message | Text | - | Message content |
| CreatedAt | Datetime | - | Date message was sent |

---

## MARKETING MODULE

### Campaigns Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| CampaignId-PK | Int-AI | 9 | Campaign's unique ID Number |
| CampaignName | Varchar | 200 | Name of the campaign |
| Description | Text | - | Campaign description |
| Type | Varchar | 50 | Campaign type (Email/SMS/Social) |
| Status | Varchar | 30 | Campaign status (Draft/Active/Completed) |
| StartDate | Datetime | - | Campaign start date |
| EndDate | Datetime | - | Campaign end date |
| Budget | Decimal | 18,2 | Campaign budget |
| CreatedAt | Datetime | - | Date campaign was created |

---

### Promotions Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| PromotionId-PK | Int-AI | 9 | Promotion's unique ID Number |
| PromotionCode | Varchar | 50 | Promotion/Discount code |
| PromotionName | Varchar | 200 | Name of the promotion |
| DiscountType | Varchar | 20 | Discount type (Percentage/FixedAmount) |
| DiscountValue | Decimal | 18,2 | Discount value |
| StartDate | Datetime | - | Promotion start date |
| EndDate | Datetime | - | Promotion end date |
| IsActive | Bit | 1 | Promotion active status |

---

## BILLING MODULE

### Invoices Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| InvoiceId-PK | Int-AI | 9 | Invoice's unique ID Number |
| OrderId-FK | Int | 9 | Reference to Orders table |
| CustomerId-FK | Int | 9 | Reference to Customers table |
| InvoiceNumber | Varchar | 30 | Invoice reference number |
| InvoiceDate | Datetime | - | Date invoice was created |
| DueDate | Datetime | - | Payment due date |
| TotalAmount | Decimal | 18,2 | Total invoice amount |
| PaidAmount | Decimal | 18,2 | Amount paid |
| Status | Varchar | 20 | Invoice status (Pending/Paid/Overdue) |

---

### Payments Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| PaymentId-PK | Int-AI | 9 | Payment's unique ID Number |
| InvoiceId-FK | Int | 9 | Reference to Invoices table |
| CustomerId-FK | Int | 9 | Reference to Customers table |
| PaymentNumber | Varchar | 30 | Payment reference number |
| Amount | Decimal | 18,2 | Payment amount |
| PaymentMethod | Varchar | 50 | Payment method (Cash/Card/Transfer) |
| PaymentDate | Datetime | - | Date payment was made |
| Status | Varchar | 20 | Payment status (Completed/Failed) |
| ReferenceNumber | Varchar | 100 | External reference number |

---

## SYSTEM TABLE

### ActivityLogs Table
| Field Names | Datatype | Length | Description |
|-------------|----------|--------|-------------|
| LogId-PK | Int-AI | 9 | Log's unique ID Number |
| UserId-FK | Int | 9 | Reference to Users table |
| Action | Varchar | 50 | Action performed (Create/Update/Delete) |
| Module | Varchar | 50 | Module where action occurred |
| Description | Varchar | 500 | Description of the activity |
| CreatedAt | Datetime | - | Date and time of activity |

---

## LEGEND

| Abbreviation | Meaning |
|--------------|---------|
| PK | Primary Key |
| FK | Foreign Key |
| AI | Auto Increment |
| Int | Integer |
| Varchar | Variable Character |
| Decimal | Decimal Number |
| Bit | Boolean (0 or 1) |
| Text | Long Text |
| Datetime | Date and Time |

---

**Total Tables: 20**

| Module | Tables |
|--------|--------|
| Core | Roles, Users |
| Customer | Customers |
| Inventory | ProductCategories, Brands, Suppliers, Products, StockAlerts, PurchaseOrders, PurchaseOrderItems |
| Sales | Leads, Orders, OrderItems |
| Support | SupportTickets, TicketMessages |
| Marketing | Campaigns, Promotions |
| Billing | Invoices, Payments |
| System | ActivityLogs |
