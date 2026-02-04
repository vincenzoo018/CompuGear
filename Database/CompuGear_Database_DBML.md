# CompuGear Database Schema (DBML)

```dbml
// =====================================================
// COMPUGEAR CRM - SIMPLIFIED DATABASE (20 TABLES)
// =====================================================

// CORE TABLES (2)

Table Roles {
  RoleId int [pk, increment]
  RoleName varchar(50) [not null, unique]
  Description varchar(255)
  AccessLevel int [default: 1]
  IsActive bit [default: 1]
}

Table Users {
  UserId int [pk, increment]
  Username varchar(50) [not null, unique]
  Email varchar(100) [not null, unique]
  PasswordHash varchar(255) [not null]
  FirstName varchar(50) [not null]
  LastName varchar(50) [not null]
  Phone varchar(20)
  RoleId int [not null]
  IsActive bit [default: 1]
  CreatedAt datetime [default: `now()`]
}

// CUSTOMER MODULE (1)

Table Customers {
  CustomerId int [pk, increment]
  CustomerCode varchar(20) [unique]
  FirstName varchar(50) [not null]
  LastName varchar(50) [not null]
  Email varchar(100) [not null]
  Phone varchar(20)
  Address varchar(255)
  City varchar(50)
  Country varchar(50)
  Status varchar(20) [default: 'Active']
  TotalOrders int [default: 0]
  TotalSpent decimal(18,2) [default: 0]
  CreatedAt datetime [default: `now()`]
}

// INVENTORY MODULE (7)

Table ProductCategories {
  CategoryId int [pk, increment]
  CategoryName varchar(100) [not null]
  Description varchar(255)
  IsActive bit [default: 1]
}

Table Brands {
  BrandId int [pk, increment]
  BrandName varchar(100) [not null, unique]
  IsActive bit [default: 1]
}

Table Suppliers {
  SupplierId int [pk, increment]
  SupplierCode varchar(20) [unique]
  SupplierName varchar(100) [not null]
  ContactPerson varchar(100)
  Email varchar(100)
  Phone varchar(20)
  Address varchar(255)
  Status varchar(20) [default: 'Active']
}

Table Products {
  ProductId int [pk, increment]
  ProductCode varchar(50) [not null, unique]
  SKU varchar(50) [unique]
  ProductName varchar(200) [not null]
  Description varchar(500)
  CategoryId int
  BrandId int
  SupplierId int
  CostPrice decimal(18,2) [default: 0]
  SellingPrice decimal(18,2) [not null]
  StockQuantity int [default: 0]
  ReorderLevel int [default: 10]
  ImageUrl varchar(255)
  Status varchar(20) [default: 'Active']
  CreatedAt datetime [default: `now()`]
}

Table StockAlerts {
  AlertId int [pk, increment]
  ProductId int [not null]
  AlertType varchar(30) [not null]
  CurrentStock int [not null]
  ThresholdLevel int [default: 15]
  IsResolved bit [default: 0]
  CreatedAt datetime [default: `now()`]
}

Table PurchaseOrders {
  PurchaseOrderId int [pk, increment]
  SupplierId int [not null]
  OrderDate datetime [default: `now()`]
  ExpectedDeliveryDate datetime
  Status varchar(20) [default: 'Pending']
  TotalAmount decimal(18,2)
  Notes varchar(500)
}

Table PurchaseOrderItems {
  ItemId int [pk, increment]
  PurchaseOrderId int [not null]
  ProductId int [not null]
  Quantity int [not null]
  UnitPrice decimal(18,2) [not null]
  Subtotal decimal(18,2) [not null]
}

// SALES MODULE (3)

Table Leads {
  LeadId int [pk, increment]
  FirstName varchar(50) [not null]
  LastName varchar(50) [not null]
  Email varchar(100)
  Phone varchar(20)
  CompanyName varchar(100)
  Source varchar(50)
  Status varchar(30) [default: 'New']
  EstimatedValue decimal(18,2)
  AssignedTo int
  Notes varchar(500)
  CreatedAt datetime [default: `now()`]
}

Table Orders {
  OrderId int [pk, increment]
  OrderNumber varchar(30) [not null, unique]
  CustomerId int [not null]
  OrderDate datetime [default: `now()`]
  OrderStatus varchar(30) [default: 'Pending']
  PaymentStatus varchar(30) [default: 'Pending']
  Subtotal decimal(18,2) [not null]
  TaxAmount decimal(18,2) [default: 0]
  TotalAmount decimal(18,2) [not null]
  PaymentMethod varchar(50)
  ShippingAddress varchar(255)
  Notes varchar(500)
  CreatedAt datetime [default: `now()`]
}

Table OrderItems {
  ItemId int [pk, increment]
  OrderId int [not null]
  ProductId int [not null]
  ProductName varchar(200) [not null]
  Quantity int [not null]
  UnitPrice decimal(18,2) [not null]
  TotalPrice decimal(18,2) [not null]
}

// SUPPORT MODULE (2)

Table SupportTickets {
  TicketId int [pk, increment]
  TicketNumber varchar(20) [not null, unique]
  CustomerId int
  Subject varchar(200) [not null]
  Description text [not null]
  Priority varchar(20) [default: 'Medium']
  Status varchar(30) [default: 'Open']
  AssignedTo int
  CreatedAt datetime [default: `now()`]
  ResolvedAt datetime
}

Table TicketMessages {
  MessageId int [pk, increment]
  TicketId int [not null]
  SenderType varchar(20) [not null]
  SenderId int
  Message text [not null]
  CreatedAt datetime [default: `now()`]
}

// MARKETING MODULE (2)

Table Campaigns {
  CampaignId int [pk, increment]
  CampaignName varchar(200) [not null]
  Description text
  Type varchar(50) [not null]
  Status varchar(30) [default: 'Draft']
  StartDate datetime
  EndDate datetime
  Budget decimal(18,2)
  CreatedAt datetime [default: `now()`]
}

Table Promotions {
  PromotionId int [pk, increment]
  PromotionCode varchar(50) [not null, unique]
  PromotionName varchar(200) [not null]
  DiscountType varchar(20) [not null]
  DiscountValue decimal(18,2) [not null]
  StartDate datetime [not null]
  EndDate datetime [not null]
  IsActive bit [default: 1]
}

// BILLING MODULE (2)

Table Invoices {
  InvoiceId int [pk, increment]
  InvoiceNumber varchar(30) [not null, unique]
  OrderId int
  CustomerId int [not null]
  InvoiceDate datetime [default: `now()`]
  DueDate datetime [not null]
  TotalAmount decimal(18,2) [not null]
  PaidAmount decimal(18,2) [default: 0]
  Status varchar(20) [default: 'Pending']
}

Table Payments {
  PaymentId int [pk, increment]
  PaymentNumber varchar(30) [not null, unique]
  InvoiceId int
  CustomerId int [not null]
  Amount decimal(18,2) [not null]
  PaymentMethod varchar(50) [not null]
  PaymentDate datetime [default: `now()`]
  Status varchar(20) [default: 'Completed']
  ReferenceNumber varchar(100)
}

// SYSTEM (1)

Table ActivityLogs {
  LogId int [pk, increment]
  UserId int
  Action varchar(50) [not null]
  Module varchar(50) [not null]
  Description varchar(500)
  CreatedAt datetime [default: `now()`]
}

// =====================================================
// RELATIONSHIPS
// =====================================================

// Users
Ref: Users.RoleId > Roles.RoleId

// Products
Ref: Products.CategoryId > ProductCategories.CategoryId
Ref: Products.BrandId > Brands.BrandId
Ref: Products.SupplierId > Suppliers.SupplierId

// Stock Alerts
Ref: StockAlerts.ProductId > Products.ProductId

// Purchase Orders
Ref: PurchaseOrders.SupplierId > Suppliers.SupplierId
Ref: PurchaseOrderItems.PurchaseOrderId > PurchaseOrders.PurchaseOrderId
Ref: PurchaseOrderItems.ProductId > Products.ProductId

// Sales
Ref: Leads.AssignedTo > Users.UserId
Ref: Orders.CustomerId > Customers.CustomerId
Ref: OrderItems.OrderId > Orders.OrderId
Ref: OrderItems.ProductId > Products.ProductId

// Support
Ref: SupportTickets.CustomerId > Customers.CustomerId
Ref: SupportTickets.AssignedTo > Users.UserId
Ref: TicketMessages.TicketId > SupportTickets.TicketId

// Billing
Ref: Invoices.OrderId > Orders.OrderId
Ref: Invoices.CustomerId > Customers.CustomerId
Ref: Payments.InvoiceId > Invoices.InvoiceId
Ref: Payments.CustomerId > Customers.CustomerId

// Activity
Ref: ActivityLogs.UserId > Users.UserId
```
