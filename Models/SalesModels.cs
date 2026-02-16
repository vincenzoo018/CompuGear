using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Sales Lead entity
    /// </summary>
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        public int? CompanyId { get; set; }

        [StringLength(20)]
        public string? LeadCode { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        // Lead Details
        [StringLength(50)]
        public string? Source { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "New";

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedValue { get; set; }

        public int? Probability { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        // Assignment
        public int? AssignedTo { get; set; }

        // Notes
        public string? Description { get; set; }

        public string? Notes { get; set; }

        // Conversion
        public bool IsConverted { get; set; } = false;

        public int? ConvertedCustomerId { get; set; }

        public DateTime? ConvertedAt { get; set; }

        // Timestamps
        public DateTime? LastContactedAt { get; set; }

        public DateTime? NextFollowUp { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        // Navigation Properties
        [ForeignKey("AssignedTo")]
        public virtual User? AssignedUser { get; set; }

        [ForeignKey("ConvertedCustomerId")]
        public virtual Customer? ConvertedCustomer { get; set; }
    }

    /// <summary>
    /// Order entity
    /// </summary>
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // Status
        [StringLength(30)]
        public string OrderStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        // Pricing
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Payment
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        // Shipping Info
        [StringLength(50)]
        public string? ShippingMethod { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

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

        // Billing Info
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

        // Additional
        public string? Notes { get; set; }

        public string? InternalNotes { get; set; }

        // Assignment
        public int? AssignedTo { get; set; }

        // Dates
        public DateTime? ConfirmedAt { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public decimal BalanceDue => TotalAmount - PaidAmount;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("AssignedTo")]
        public virtual User? AssignedUser { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    /// <summary>
    /// Order Item entity
    /// </summary>
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ProductCode { get; set; }

        [StringLength(50)]
        public string? SKU { get; set; }

        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Warranty
        public int? WarrantyPeriod { get; set; }

        public DateTime? WarrantyExpiryDate { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }

    /// <summary>
    /// Order Status History entity
    /// </summary>
    public class OrderStatusHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int OrderId { get; set; }

        [StringLength(30)]
        public string? PreviousStatus { get; set; }

        [Required]
        [StringLength(30)]
        public string NewStatus { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public int? ChangedBy { get; set; }

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("ChangedBy")]
        public virtual User? ChangedByUser { get; set; }
    }

    /// <summary>
    /// Sales Quotation entity
    /// </summary>
    public class Quotation
    {
        [Key]
        public int QuotationId { get; set; }

        public int? CompanyId { get; set; }

        [StringLength(20)]
        public string QuotationNumber { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        public int? LeadId { get; set; }

        [StringLength(100)]
        public string? CustomerName { get; set; }

        [StringLength(100)]
        public string? CustomerEmail { get; set; }

        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        public DateTime QuotationDate { get; set; } = DateTime.UtcNow;

        public DateTime ValidUntil { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } = 0;

        [StringLength(30)]
        public string Status { get; set; } = "Draft"; // Draft, Sent, Accepted, Rejected, Expired, Converted

        [StringLength(500)]
        public string? Terms { get; set; }

        public string? Notes { get; set; }

        public int? ConvertedOrderId { get; set; }

        public DateTime? ConvertedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        public virtual ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
    }

    /// <summary>
    /// Quotation Line Item
    /// </summary>
    public class QuotationItem
    {
        [Key]
        public int ItemId { get; set; }

        public int QuotationId { get; set; }

        public int? ProductId { get; set; }

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } = 0;

        [ForeignKey("QuotationId")]
        public virtual Quotation Quotation { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }

    /// <summary>
    /// Sales Target / Goal entity
    /// </summary>
    public class SalesTarget
    {
        [Key]
        public int TargetId { get; set; }

        public int? CompanyId { get; set; }

        public int? UserId { get; set; }

        [StringLength(50)]
        public string TargetName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Period { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Quarterly, Yearly

        public int Year { get; set; }

        public int? Month { get; set; }

        public int? Quarter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AchievedAmount { get; set; } = 0;

        public int TargetOrders { get; set; } = 0;

        public int AchievedOrders { get; set; } = 0;

        [NotMapped]
        public decimal ProgressPercentage => TargetAmount > 0 ? (AchievedAmount / TargetAmount) * 100 : 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Completed, Exceeded

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Sales Commission record
    /// </summary>
    public class Commission
    {
        [Key]
        public int CommissionId { get; set; }

        public int? CompanyId { get; set; }

        public int UserId { get; set; }

        public int? OrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Paid

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }
    }

    /// <summary>
    /// Sales Pipeline Stage
    /// </summary>
    public class PipelineStage
    {
        [Key]
        public int StageId { get; set; }

        public int? CompanyId { get; set; }

        [StringLength(50)]
        public string StageName { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        [StringLength(7)]
        public string Color { get; set; } = "#008080";

        public int Probability { get; set; } = 50; // Default win probability

        public bool IsDefault { get; set; } = false;

        public bool IsWonStage { get; set; } = false;

        public bool IsLostStage { get; set; } = false;
    }
}
