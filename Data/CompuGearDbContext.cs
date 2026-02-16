using Microsoft.EntityFrameworkCore;
using CompuGear.Models;

namespace CompuGear.Data
{
    /// <summary>
    /// CompuGear Database Context
    /// Main database context for the CompuGear CRM system
    /// </summary>
    public class CompuGearDbContext : DbContext
    {
        public CompuGearDbContext(DbContextOptions<CompuGearDbContext> options)
            : base(options)
        {
        }

        // Core Entities
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; }

        // Customer Module
        public DbSet<CustomerCategory> CustomerCategories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }

        // Inventory Module
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductSpecification> ProductSpecifications { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<StockAlert> StockAlerts { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

        // Sales Module
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }

        // Support Module
        public DbSet<TicketCategory> TicketCategories { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<TicketMessage> TicketMessages { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }
        public DbSet<KnowledgeCategory> KnowledgeCategories { get; set; }
        public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }

        // Marketing Module
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<CustomerSegment> CustomerSegments { get; set; }
        public DbSet<SegmentMember> SegmentMembers { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionUsage> PromotionUsages { get; set; }

        // Billing Module
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Refund> Refunds { get; set; }

        // System/Activity
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatBotIntent> ChatBotIntents { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // Approval Workflow
        public DbSet<ApprovalRequest> ApprovalRequests { get; set; }

        // Role Module Access
        public DbSet<RoleModuleAccess> RoleModuleAccess { get; set; }

        // Super Admin ERP
        public DbSet<ERPModule> ERPModules { get; set; }
        public DbSet<CompanySubscription> CompanySubscriptions { get; set; }
        public DbSet<CompanyModuleAccess> CompanyModuleAccess { get; set; }
        public DbSet<PlatformUsageLog> PlatformUsageLogs { get; set; }

        // Sales Pipeline & Quotations
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }
        public DbSet<SalesTarget> SalesTargets { get; set; }
        public DbSet<Commission> Commissions { get; set; }
        public DbSet<PipelineStage> PipelineStages { get; set; }

        // Inventory Batch/Expiry Tracking
        public DbSet<ProductBatch> ProductBatches { get; set; }
        public DbSet<AutoReorderRule> AutoReorderRules { get; set; }
        public DbSet<ExpiryAlert> ExpiryAlerts { get; set; }

        // Support Enhancements
        public DbSet<CannedResponse> CannedResponses { get; set; }
        public DbSet<SLAConfig> SLAConfigs { get; set; }

        // Marketing A/B Testing & Social Media
        public DbSet<ABTest> ABTests { get; set; }
        public DbSet<ABTestVariant> ABTestVariants { get; set; }
        public DbSet<SocialMediaPost> SocialMediaPosts { get; set; }

        // Tax Calculations
        public DbSet<TaxRate> TaxRates { get; set; }
        public DbSet<ProductTaxCategory> ProductTaxCategories { get; set; }
        public DbSet<TaxCalculation> TaxCalculations { get; set; }

        // Customer Portal - Reviews & Loyalty
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ProductComparison> ProductComparisons { get; set; }
        public DbSet<RecentlyViewed> RecentlyViewed { get; set; }
        public DbSet<LoyaltyProgram> LoyaltyPrograms { get; set; }
        public DbSet<LoyaltyPoints> LoyaltyPoints { get; set; }

        // Order Tracking & Shipping
        public DbSet<OrderShipment> OrderShipments { get; set; }
        public DbSet<ShipmentTracking> ShipmentTrackings { get; set; }

        // Checkout Enhancements
        public DbSet<GiftOption> GiftOptions { get; set; }
        public DbSet<InstallmentPlan> InstallmentPlans { get; set; }
        public DbSet<InstallmentPayment> InstallmentPayments { get; set; }
        public DbSet<SubscriptionOrder> SubscriptionOrders { get; set; }
        public DbSet<SubscriptionItem> SubscriptionItems { get; set; }

        // Support Staff - Ticket Enhancements
        public DbSet<TicketAssignmentRule> TicketAssignmentRules { get; set; }
        public DbSet<AssignmentRuleAgent> AssignmentRuleAgents { get; set; }
        public DbSet<SatisfactionSurvey> SatisfactionSurveys { get; set; }
        public DbSet<TicketNote> TicketNotes { get; set; }
        public DbSet<TicketTag> TicketTags { get; set; }
        public DbSet<TicketTagMapping> TicketTagMappings { get; set; }
        public DbSet<TicketTimeEntry> TicketTimeEntries { get; set; }
        public DbSet<TicketLink> TicketLinks { get; set; }
        public DbSet<TicketMerge> TicketMerges { get; set; }

        // Live Chat Enhancements
        public DbSet<ChatTransfer> ChatTransfers { get; set; }
        public DbSet<ChatAttachment> ChatAttachments { get; set; }
        public DbSet<ChatTranscript> ChatTranscripts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============ Indexes ============

            // Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Roles
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            // Permissions
            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.PermissionName)
                .IsUnique();

            // RolePermissions
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            // Companies
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.CompanyCode)
                .IsUnique();

            // Customers
            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.CustomerCode)
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email);

            // Products
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.ProductCode)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique();

            // Brands
            modelBuilder.Entity<Brand>()
                .HasIndex(b => b.BrandName)
                .IsUnique();

            // Leads
            modelBuilder.Entity<Lead>()
                .HasIndex(l => l.LeadCode)
                .IsUnique();

            // Orders
            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            // Tickets
            modelBuilder.Entity<SupportTicket>()
                .HasIndex(t => t.TicketNumber)
                .IsUnique();

            // Invoices
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // Payments
            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.PaymentNumber)
                .IsUnique();

            // Promotions
            modelBuilder.Entity<Promotion>()
                .HasIndex(p => p.PromotionCode)
                .IsUnique();

            // SegmentMembers
            modelBuilder.Entity<SegmentMember>()
                .HasIndex(sm => new { sm.SegmentId, sm.CustomerId })
                .IsUnique();

            // Activity Logs
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.Module);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.CreatedAt);

            // System Settings
            modelBuilder.Entity<SystemSetting>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            // Chat Sessions
            modelBuilder.Entity<ChatSession>()
                .HasIndex(c => c.SessionToken)
                .IsUnique();

            // Chat Bot Intents
            modelBuilder.Entity<ChatBotIntent>()
                .HasIndex(c => c.IntentName)
                .IsUnique();

            // ERP Modules
            modelBuilder.Entity<ERPModule>()
                .HasIndex(m => m.ModuleCode)
                .IsUnique();

            // Role Module Access
            modelBuilder.Entity<RoleModuleAccess>()
                .HasIndex(r => new { r.CompanyId, r.RoleId, r.ModuleCode })
                .IsUnique();

            // Company Module Access
            modelBuilder.Entity<CompanyModuleAccess>()
                .HasIndex(a => new { a.CompanyId, a.ModuleId })
                .IsUnique();

            // Platform Usage Logs
            modelBuilder.Entity<PlatformUsageLog>()
                .HasIndex(l => l.CompanyId);

            modelBuilder.Entity<PlatformUsageLog>()
                .HasIndex(l => l.CreatedAt);

            // ============ Seed Data ============

            // Seed Roles (8 Actor Types including Inventory Staff)
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Super Admin", Description = "Full system access with all privileges", AccessLevel = 8 },
                new Role { RoleId = 2, RoleName = "Company Admin", Description = "Company-wide administrative access", AccessLevel = 7 },
                new Role { RoleId = 3, RoleName = "Sales Staff", Description = "Sales and order management access", AccessLevel = 6 },
                new Role { RoleId = 4, RoleName = "Customer Support Staff", Description = "Customer support and ticket management", AccessLevel = 5 },
                new Role { RoleId = 5, RoleName = "Marketing Staff", Description = "Marketing campaigns and promotions management", AccessLevel = 4 },
                new Role { RoleId = 6, RoleName = "Accounting & Billing Staff", Description = "Financial and billing management", AccessLevel = 3 },
                new Role { RoleId = 7, RoleName = "Customer", Description = "Customer portal access", AccessLevel = 1 },
                new Role { RoleId = 8, RoleName = "Inventory Staff", Description = "Inventory and stock management", AccessLevel = 2 }
            );

            // Seed ERP Modules
            modelBuilder.Entity<ERPModule>().HasData(
                new ERPModule { ModuleId = 1, ModuleName = "Sales Management", ModuleCode = "SALES", Description = "Complete sales pipeline, orders, leads and revenue tracking", MonthlyPrice = 49.99m, AnnualPrice = 499.99m, SortOrder = 1, Features = "Order Management,Lead Tracking,Sales Reports,Revenue Analytics" },
                new ERPModule { ModuleId = 2, ModuleName = "Customer Support", ModuleCode = "SUPPORT", Description = "Ticket management, live chat, knowledge base and SLA tracking", MonthlyPrice = 39.99m, AnnualPrice = 399.99m, SortOrder = 2, Features = "Ticket System,Live Chat,Knowledge Base,SLA Management" },
                new ERPModule { ModuleId = 3, ModuleName = "Marketing", ModuleCode = "MARKETING", Description = "Campaign management, promotions, customer segmentation and analytics", MonthlyPrice = 44.99m, AnnualPrice = 449.99m, SortOrder = 3, Features = "Campaigns,Promotions,Segments,Marketing Analytics" },
                new ERPModule { ModuleId = 4, ModuleName = "Billing & Accounting", ModuleCode = "BILLING", Description = "Invoice generation, payment processing, financial reports", MonthlyPrice = 54.99m, AnnualPrice = 549.99m, SortOrder = 4, Features = "Invoices,Payments,Refunds,Financial Reports" },
                new ERPModule { ModuleId = 5, ModuleName = "Inventory Management", ModuleCode = "INVENTORY", Description = "Product catalog, stock tracking, purchase orders and suppliers", MonthlyPrice = 44.99m, AnnualPrice = 449.99m, SortOrder = 5, Features = "Products,Stock Levels,Purchase Orders,Suppliers" },
                new ERPModule { ModuleId = 6, ModuleName = "Customer Management", ModuleCode = "CUSTOMERS", Description = "Customer profiles, categories, addresses and purchase history", MonthlyPrice = 29.99m, AnnualPrice = 299.99m, SortOrder = 6, Features = "Customer Profiles,Categories,History,Addresses" }
            );

            // Seed Customer Categories
            modelBuilder.Entity<CustomerCategory>().HasData(
                new CustomerCategory { CategoryId = 1, CategoryName = "Standard", Description = "Regular customers", DiscountPercent = 0 },
                new CustomerCategory { CategoryId = 2, CategoryName = "Premium", Description = "Premium loyalty customers", DiscountPercent = 5 },
                new CustomerCategory { CategoryId = 3, CategoryName = "VIP", Description = "VIP customers with highest priority", DiscountPercent = 10 },
                new CustomerCategory { CategoryId = 4, CategoryName = "Corporate", Description = "Corporate/Business accounts", DiscountPercent = 7 },
                new CustomerCategory { CategoryId = 5, CategoryName = "Wholesale", Description = "Wholesale buyers", DiscountPercent = 15 }
            );

            // Seed Product Categories
            modelBuilder.Entity<ProductCategory>().HasData(
                new ProductCategory { CategoryId = 1, CategoryName = "Computer Systems", Description = "Desktop PCs, laptops, and complete systems" },
                new ProductCategory { CategoryId = 2, CategoryName = "Components", Description = "Computer hardware components" },
                new ProductCategory { CategoryId = 3, CategoryName = "Peripherals", Description = "Keyboards, mice, monitors, etc." },
                new ProductCategory { CategoryId = 4, CategoryName = "Networking", Description = "Routers, switches, cables" },
                new ProductCategory { CategoryId = 5, CategoryName = "Software", Description = "Operating systems and applications" },
                new ProductCategory { CategoryId = 6, CategoryName = "Accessories", Description = "Cases, bags, cleaning supplies" },
                new ProductCategory { CategoryId = 7, CategoryName = "Gaming", Description = "Gaming gear and equipment" },
                new ProductCategory { CategoryId = 8, CategoryName = "Storage", Description = "Hard drives, SSDs, USB drives" }
            );

            // Seed Brands
            modelBuilder.Entity<Brand>().HasData(
                new Brand { BrandId = 1, BrandName = "Intel" },
                new Brand { BrandId = 2, BrandName = "AMD" },
                new Brand { BrandId = 3, BrandName = "NVIDIA" },
                new Brand { BrandId = 4, BrandName = "ASUS" },
                new Brand { BrandId = 5, BrandName = "MSI" },
                new Brand { BrandId = 6, BrandName = "Gigabyte" },
                new Brand { BrandId = 7, BrandName = "Corsair" },
                new Brand { BrandId = 8, BrandName = "Kingston" },
                new Brand { BrandId = 9, BrandName = "Samsung" },
                new Brand { BrandId = 10, BrandName = "Western Digital" },
                new Brand { BrandId = 11, BrandName = "Logitech" },
                new Brand { BrandId = 12, BrandName = "Razer" },
                new Brand { BrandId = 13, BrandName = "HP" },
                new Brand { BrandId = 14, BrandName = "Dell" },
                new Brand { BrandId = 15, BrandName = "Lenovo" },
                new Brand { BrandId = 16, BrandName = "Acer" }
            );

            // Seed Ticket Categories
            modelBuilder.Entity<TicketCategory>().HasData(
                new TicketCategory { CategoryId = 1, CategoryName = "Technical Support", Description = "Hardware and software technical issues", SLAHours = 24, Priority = "Medium" },
                new TicketCategory { CategoryId = 2, CategoryName = "Billing Inquiry", Description = "Questions about invoices and payments", SLAHours = 48, Priority = "Low" },
                new TicketCategory { CategoryId = 3, CategoryName = "Order Issue", Description = "Problems with orders or delivery", SLAHours = 12, Priority = "High" },
                new TicketCategory { CategoryId = 4, CategoryName = "Product Information", Description = "Questions about products", SLAHours = 48, Priority = "Low" },
                new TicketCategory { CategoryId = 5, CategoryName = "Returns & Refunds", Description = "Return and refund requests", SLAHours = 24, Priority = "Medium" },
                new TicketCategory { CategoryId = 6, CategoryName = "Warranty Claims", Description = "Warranty-related issues", SLAHours = 24, Priority = "Medium" },
                new TicketCategory { CategoryId = 7, CategoryName = "Account Issues", Description = "Login and account problems", SLAHours = 12, Priority = "High" },
                new TicketCategory { CategoryId = 8, CategoryName = "General Inquiry", Description = "General questions and feedback", SLAHours = 72, Priority = "Low" }
            );

            // Seed Knowledge Categories
            modelBuilder.Entity<KnowledgeCategory>().HasData(
                new KnowledgeCategory { CategoryId = 1, CategoryName = "Getting Started" },
                new KnowledgeCategory { CategoryId = 2, CategoryName = "Troubleshooting" },
                new KnowledgeCategory { CategoryId = 3, CategoryName = "FAQs" },
                new KnowledgeCategory { CategoryId = 4, CategoryName = "Product Guides" },
                new KnowledgeCategory { CategoryId = 5, CategoryName = "Warranty Information" },
                new KnowledgeCategory { CategoryId = 6, CategoryName = "Return Policy" },
                new KnowledgeCategory { CategoryId = 7, CategoryName = "Payment Methods" }
            );

            // Seed Default Company
            modelBuilder.Entity<Company>().HasData(
                new Company 
                { 
                    CompanyId = 1, 
                    CompanyName = "CompuGear Technologies", 
                    CompanyCode = "CGT-001", 
                    Email = "info@compugear.com", 
                    Phone = "+63-XXX-XXX-XXXX", 
                    Address = "123 Tech Avenue", 
                    City = "Manila", 
                    Country = "Philippines" 
                }
            );
        }
    }
}
