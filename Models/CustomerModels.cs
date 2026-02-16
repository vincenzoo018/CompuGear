using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Customer Category entity
    /// </summary>
    public class CustomerCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    }

    /// <summary>
    /// Customer entity
    /// </summary>
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        public int? CompanyId { get; set; }

        [StringLength(20)]
        public string? CustomerCode { get; set; }

        public int? UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(255)]
        public string? Avatar { get; set; }

        public int? CategoryId { get; set; }

        // Billing Address
        [StringLength(255)]
        public string? BillingAddress { get; set; }

        [StringLength(50)]
        public string? BillingCity { get; set; }

        [StringLength(50)]
        public string? BillingState { get; set; }

        [StringLength(20)]
        public string? BillingZipCode { get; set; }

        [StringLength(50)]
        public string? BillingCountry { get; set; }

        // Shipping Address
        [StringLength(255)]
        public string? ShippingAddress { get; set; }

        [StringLength(50)]
        public string? ShippingCity { get; set; }

        [StringLength(50)]
        public string? ShippingState { get; set; }

        [StringLength(20)]
        public string? ShippingZipCode { get; set; }

        [StringLength(50)]
        public string? ShippingCountry { get; set; }

        // Business Info
        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(50)]
        public string? TaxId { get; set; }

        // Status
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public int TotalOrders { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSpent { get; set; } = 0;

        public int LoyaltyPoints { get; set; } = 0;

        // Credit Management
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditUsed { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CreditBalance { get; set; }

        [StringLength(20)]
        public string? CreditStatus { get; set; }

        public string? Notes { get; set; }

        // Preferences
        [StringLength(20)]
        public string? PreferredContactMethod { get; set; }

        public bool MarketingOptIn { get; set; } = true;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("CategoryId")]
        public virtual CustomerCategory? Category { get; set; }

        public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public virtual ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    /// <summary>
    /// Customer Address entity
    /// </summary>
    public class CustomerAddress
    {
        [Key]
        public int AddressId { get; set; }

        public int CustomerId { get; set; }

        [Required]
        [StringLength(20)]
        public string AddressType { get; set; } = "Shipping";

        [Required]
        [StringLength(255)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(255)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [Required]
        [StringLength(50)]
        public string Country { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;
    }

    /// <summary>
    /// Product Review entity
    /// </summary>
    public class ProductReview
    {
        [Key]
        public int ReviewId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        public int? OrderId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(100)]
        public string? Title { get; set; }

        public string? Comment { get; set; }

        [StringLength(500)]
        public string? Pros { get; set; }

        [StringLength(500)]
        public string? Cons { get; set; }

        public bool IsVerifiedPurchase { get; set; } = false;

        public int HelpfulCount { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }
    }

    /// <summary>
    /// Product Comparison List
    /// </summary>
    public class ProductComparison
    {
        [Key]
        public int ComparisonId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }

    /// <summary>
    /// Recently Viewed Products
    /// </summary>
    public class RecentlyViewed
    {
        [Key]
        public int ViewId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        public int ViewCount { get; set; } = 1;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }

    /// <summary>
    /// Loyalty Points Configuration
    /// </summary>
    public class LoyaltyProgram
    {
        [Key]
        public int ProgramId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProgramName { get; set; } = "Default Rewards";

        [Column(TypeName = "decimal(18,2)")]
        public decimal PointsPerCurrency { get; set; } = 1; // Points earned per currency unit spent

        [Column(TypeName = "decimal(18,2)")]
        public decimal PointsValue { get; set; } = 0.01m; // Value of each point in currency

        public int MinRedeemPoints { get; set; } = 100;

        public int? PointsExpireDays { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Customer Loyalty Points
    /// </summary>
    public class LoyaltyPoints
    {
        [Key]
        public int PointId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; } = "Earned"; // Earned, Redeemed, Expired, Adjusted

        public int Points { get; set; }

        public int? OrderId { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }

    /// <summary>
    /// Order Tracking/Shipment entity
    /// </summary>
    public class OrderShipment
    {
        [Key]
        public int ShipmentId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [StringLength(50)]
        public string? TrackingNumber { get; set; }

        [StringLength(50)]
        public string? Carrier { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Processing"; // Processing, Shipped, InTransit, OutForDelivery, Delivered, Failed

        public DateTime? ShippedAt { get; set; }

        public DateTime? EstimatedDelivery { get; set; }

        public DateTime? DeliveredAt { get; set; }

        [StringLength(255)]
        public string? ShipToAddress { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public virtual ICollection<ShipmentTracking> TrackingHistory { get; set; } = new List<ShipmentTracking>();
    }

    /// <summary>
    /// Shipment Tracking History
    /// </summary>
    public class ShipmentTracking
    {
        [Key]
        public int TrackingId { get; set; }

        [Required]
        public int ShipmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ShipmentId")]
        public virtual OrderShipment? Shipment { get; set; }
    }

    /// <summary>
    /// Gift Options for orders
    /// </summary>
    public class GiftOption
    {
        [Key]
        public int GiftId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        public int OrderId { get; set; }

        public bool IsGift { get; set; } = true;

        public bool GiftWrap { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal GiftWrapPrice { get; set; } = 0;

        [StringLength(500)]
        public string? GiftMessage { get; set; }

        [StringLength(100)]
        public string? RecipientName { get; set; }

        [StringLength(100)]
        public string? RecipientEmail { get; set; }

        public bool HidePrice { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }

    /// <summary>
    /// Buy Now Pay Later / Installment Plan
    /// </summary>
    public class InstallmentPlan
    {
        [Key]
        public int InstallmentId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int NumberOfInstallments { get; set; } = 3;

        [Column(TypeName = "decimal(18,2)")]
        public decimal InstallmentAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal InterestRate { get; set; } = 0;

        public int PaidInstallments { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Completed, Defaulted, Cancelled

        public DateTime StartDate { get; set; }

        public DateTime? NextDueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public virtual ICollection<InstallmentPayment> Payments { get; set; } = new List<InstallmentPayment>();
    }

    /// <summary>
    /// Installment Payment Record
    /// </summary>
    public class InstallmentPayment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int InstallmentId { get; set; }

        public int InstallmentNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime? PaidAt { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Overdue

        // Navigation Properties
        [ForeignKey("InstallmentId")]
        public virtual InstallmentPlan? Plan { get; set; }
    }

    /// <summary>
    /// Subscription Order for recurring purchases
    /// </summary>
    public class SubscriptionOrder
    {
        [Key]
        public int SubscriptionId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(50)]
        public string SubscriptionCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string Frequency { get; set; } = "Monthly"; // Weekly, BiWeekly, Monthly, Quarterly

        public DateTime NextOrderDate { get; set; }

        public DateTime? LastOrderDate { get; set; }

        public int? LastOrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedTotal { get; set; }

        public int? ShippingAddressId { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Paused, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("ShippingAddressId")]
        public virtual CustomerAddress? ShippingAddress { get; set; }

        public virtual ICollection<SubscriptionItem> Items { get; set; } = new List<SubscriptionItem>();
    }

    /// <summary>
    /// Subscription Order Items
    /// </summary>
    public class SubscriptionItem
    {
        [Key]
        public int ItemId { get; set; }

        [Required]
        public int SubscriptionId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Navigation Properties
        [ForeignKey("SubscriptionId")]
        public virtual SubscriptionOrder? Subscription { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
