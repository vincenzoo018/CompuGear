using System.ComponentModel.DataAnnotations;

namespace CompuGear.Models.ViewModels
{
    // ============ User Management ViewModels ============
    
    public class UserViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }

        public int? CompanyId { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Avatar { get; set; }
    }

    public class UserListViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ============ Customer ViewModels ============

    public class CustomerViewModel
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public int? CategoryId { get; set; }

        public string? BillingAddress { get; set; }

        public string? BillingCity { get; set; }

        public string? BillingState { get; set; }

        public string? BillingZipCode { get; set; }

        public string? BillingCountry { get; set; }

        public string? ShippingAddress { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingState { get; set; }

        public string? ShippingZipCode { get; set; }

        public string? ShippingCountry { get; set; }

        public string? CompanyName { get; set; }

        public string? TaxId { get; set; }

        public string Status { get; set; } = "Active";

        public string? PreferredContactMethod { get; set; }

        public bool MarketingOptIn { get; set; } = true;

        public string? Notes { get; set; }
    }

    public class CustomerListViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CategoryName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ============ Product ViewModels ============

    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product code is required")]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        public string? SKU { get; set; }

        public string? Barcode { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        public int? CategoryId { get; set; }

        public int? BrandId { get; set; }

        [Required(ErrorMessage = "Cost price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Cost price must be positive")]
        public decimal CostPrice { get; set; }

        [Required(ErrorMessage = "Selling price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Selling price must be greater than 0")]
        public decimal SellingPrice { get; set; }

        public decimal? CompareAtPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be positive")]
        public int StockQuantity { get; set; }

        public int ReorderLevel { get; set; } = 10;

        public int MaxStockLevel { get; set; } = 1000;

        public decimal? Weight { get; set; }

        public string? MainImageUrl { get; set; }

        public string Status { get; set; } = "Active";

        public bool IsFeatured { get; set; }

        public bool IsOnSale { get; set; }

        public int? WarrantyPeriod { get; set; }
    }

    public class ProductListViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // ============ Order ViewModels ============

    public class OrderViewModel
    {
        public int OrderId { get; set; }

        public string? OrderNumber { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }

        public string OrderStatus { get; set; } = "Pending";

        public string PaymentStatus { get; set; } = "Pending";

        public string? PaymentMethod { get; set; }

        public string? ShippingMethod { get; set; }

        public string? ShippingAddress { get; set; }

        public string? ShippingCity { get; set; }

        public string? ShippingState { get; set; }

        public string? ShippingZipCode { get; set; }

        public string? ShippingCountry { get; set; }

        public string? Notes { get; set; }

        public int? AssignedTo { get; set; }

        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderListViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }

    // ============ Lead ViewModels ============

    public class LeadViewModel
    {
        public int LeadId { get; set; }

        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public string? CompanyName { get; set; }

        public string? JobTitle { get; set; }

        public string? Source { get; set; }

        public string Status { get; set; } = "New";

        public string Priority { get; set; } = "Medium";

        public decimal? EstimatedValue { get; set; }

        public int? Probability { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        public int? AssignedTo { get; set; }

        public string? Description { get; set; }

        public string? Notes { get; set; }

        public DateTime? NextFollowUp { get; set; }
    }

    // ============ Support Ticket ViewModels ============

    public class TicketViewModel
    {
        public int TicketId { get; set; }

        public string? TicketNumber { get; set; }

        public int? CustomerId { get; set; }

        public int? CategoryId { get; set; }

        public int? OrderId { get; set; }

        public string? ContactName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        [Phone]
        public string? ContactPhone { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public string Priority { get; set; } = "Medium";

        public string Status { get; set; } = "Open";

        public int? AssignedTo { get; set; }

        public string Source { get; set; } = "Web";
    }

    public class TicketListViewModel
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CategoryName { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedToName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool SLABreached { get; set; }
    }

    // ============ Invoice ViewModels ============

    public class InvoiceViewModel
    {
        public int InvoiceId { get; set; }

        public string? InvoiceNumber { get; set; }

        public int? OrderId { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        public string? PaymentTerms { get; set; }

        public string? Notes { get; set; }

        public List<InvoiceItemViewModel> Items { get; set; } = new();
    }

    public class InvoiceItemViewModel
    {
        public int? ProductId { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalPrice { get; set; }
    }

    public class InvoiceListViewModel
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceDue { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
    }

    // ============ Payment ViewModels ============

    public class PaymentViewModel
    {
        public int PaymentId { get; set; }

        public string? PaymentNumber { get; set; }

        public int? InvoiceId { get; set; }

        public int? OrderId { get; set; }

        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethodType { get; set; } = string.Empty;

        public string? TransactionId { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }
    }

    // ============ Campaign ViewModels ============

    public class CampaignViewModel
    {
        public int CampaignId { get; set; }

        public string? CampaignCode { get; set; }

        [Required(ErrorMessage = "Campaign name is required")]
        [StringLength(200)]
        public string CampaignName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Campaign type is required")]
        public string Type { get; set; } = string.Empty;

        public string Status { get; set; } = "Draft";

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public decimal? Budget { get; set; }

        public string? TargetSegment { get; set; }

        public string? Subject { get; set; }

        public string? Content { get; set; }
    }

    // ============ Promotion ViewModels ============

    public class PromotionViewModel
    {
        public int PromotionId { get; set; }

        [Required(ErrorMessage = "Promotion code is required")]
        [StringLength(50)]
        public string PromotionCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Promotion name is required")]
        [StringLength(200)]
        public string PromotionName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Discount type is required")]
        public string DiscountType { get; set; } = "Percentage";

        [Required(ErrorMessage = "Discount value is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal DiscountValue { get; set; }

        public decimal MinOrderAmount { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        public int? UsageLimit { get; set; }

        public int UsageLimitPerCustomer { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public int? CampaignId { get; set; }
    }

    // ============ Dashboard ViewModels ============

    public class DashboardViewModel
    {
        // Summary Stats
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int PendingOrders { get; set; }
        public int OpenTickets { get; set; }
        public int LowStockProducts { get; set; }
        public decimal RevenueGrowth { get; set; }

        // Charts Data
        public List<ChartDataPoint> RevenueChart { get; set; } = new();
        public List<ChartDataPoint> OrdersChart { get; set; } = new();
        public List<PieChartData> CategorySales { get; set; } = new();

        // Recent Data
        public List<OrderListViewModel> RecentOrders { get; set; } = new();
        public List<TicketListViewModel> RecentTickets { get; set; } = new();
        public List<ProductListViewModel> LowStockAlerts { get; set; } = new();
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class PieChartData
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    // ============ API Response ViewModels ============

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrevious => PageNumber > 1;
        public bool HasNext => PageNumber < TotalPages;
    }
}
