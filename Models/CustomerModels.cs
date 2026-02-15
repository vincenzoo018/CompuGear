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
}
