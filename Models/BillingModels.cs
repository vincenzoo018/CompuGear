using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Invoice entity
    /// </summary>
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(30)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int? OrderId { get; set; }

        public int CustomerId { get; set; }

        // Invoice Details
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        // Amounts
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceDue { get; set; }

        // Status
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        // Billing Info
        [StringLength(100)]
        public string? BillingName { get; set; }

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

        [StringLength(100)]
        [EmailAddress]
        public string? BillingEmail { get; set; }

        // Terms
        [StringLength(100)]
        public string? PaymentTerms { get; set; }

        public string? Notes { get; set; }

        public string? InternalNotes { get; set; }

        // Timestamps
        public DateTime? SentAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsOverdue => Status != "Paid" && Status != "Cancelled" && DateTime.UtcNow > DueDate;

        // Navigation Properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }

        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    /// <summary>
    /// Invoice Item entity
    /// </summary>
    public class InvoiceItem
    {
        [Key]
        public int ItemId { get; set; }

        public int InvoiceId { get; set; }

        public int? ProductId { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation Properties
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }

    /// <summary>
    /// Payment Method entity (Saved customer payment methods)
    /// </summary>
    public class PaymentMethod
    {
        [Key]
        public int MethodId { get; set; }

        public int CustomerId { get; set; }

        [Required]
        [StringLength(30)]
        public string MethodType { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Provider { get; set; }

        // Card Details (tokenized)
        [StringLength(4)]
        public string? LastFourDigits { get; set; }

        public int? ExpiryMonth { get; set; }

        public int? ExpiryYear { get; set; }

        [StringLength(100)]
        public string? CardHolderName { get; set; }

        // Bank Details
        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? AccountNumber { get; set; }

        // PayMongo
        [StringLength(255)]
        public string? PayMongoToken { get; set; }

        // Status
        public bool IsDefault { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;
    }

    /// <summary>
    /// Payment entity
    /// </summary>
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [StringLength(30)]
        public string PaymentNumber { get; set; } = string.Empty;

        public int? InvoiceId { get; set; }

        public int? OrderId { get; set; }

        public int CustomerId { get; set; }

        // Payment Details
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethodType { get; set; } = string.Empty;

        // Status
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        // Reference
        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(100)]
        public string? PayMongoPaymentId { get; set; }

        // QR Payment (PayMongo)
        [StringLength(500)]
        public string? QRCodeUrl { get; set; }

        public DateTime? QRCodeExpiry { get; set; }

        // Details
        [StringLength(3)]
        public string Currency { get; set; } = "PHP";

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        // Timestamps
        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? ProcessedBy { get; set; }

        // Navigation Properties
        [ForeignKey("InvoiceId")]
        public virtual Invoice? Invoice { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("ProcessedBy")]
        public virtual User? ProcessedByUser { get; set; }

        public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }

    /// <summary>
    /// Refund entity
    /// </summary>
    public class Refund
    {
        [Key]
        public int RefundId { get; set; }

        [Required]
        [StringLength(30)]
        public string RefundNumber { get; set; } = string.Empty;

        public int PaymentId { get; set; }

        public int? OrderId { get; set; }

        public int CustomerId { get; set; }

        // Refund Details
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(50)]
        public string? RefundMethod { get; set; }

        // Timestamps
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public int? RequestedBy { get; set; }

        public int? ApprovedBy { get; set; }

        public int? ProcessedBy { get; set; }

        // Navigation Properties
        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; } = null!;

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("RequestedBy")]
        public virtual User? RequestedByUser { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? ApprovedByUser { get; set; }

        [ForeignKey("ProcessedBy")]
        public virtual User? ProcessedByUser { get; set; }
    }
}
