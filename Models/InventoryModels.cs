using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Product Category entity
    /// </summary>
    public class ProductCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public int? ParentCategoryId { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ParentCategoryId")]
        public virtual ProductCategory? ParentCategory { get; set; }

        public virtual ICollection<ProductCategory> SubCategories { get; set; } = new List<ProductCategory>();

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    /// <summary>
    /// Brand entity
    /// </summary>
    public class Brand
    {
        [Key]
        public int BrandId { get; set; }

        [Required]
        [StringLength(100)]
        public string BrandName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Logo { get; set; }

        [StringLength(255)]
        public string? Website { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    /// <summary>
    /// Product entity
    /// </summary>
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SKU { get; set; }

        [StringLength(50)]
        public string? Barcode { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        public int? CategoryId { get; set; }

        public int? BrandId { get; set; }

        public int? SupplierId { get; set; }

        // Pricing
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CompareAtPrice { get; set; }

        // Stock
        public int StockQuantity { get; set; } = 0;

        public int ReorderLevel { get; set; } = 10;

        public int MaxStockLevel { get; set; } = 1000;

        // Dimensions
        [Column(TypeName = "decimal(10,2)")]
        public decimal? Weight { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Length { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Width { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Height { get; set; }

        // Images
        [StringLength(255)]
        public string? MainImageUrl { get; set; }

        // Status
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsFeatured { get; set; } = false;

        public bool IsOnSale { get; set; } = false;

        // Warranty
        public int? WarrantyPeriod { get; set; }

        // SEO
        [StringLength(200)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        [StringLength(500)]
        public string? MetaKeywords { get; set; }

        [StringLength(200)]
        public string? Slug { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public string StockStatus => StockQuantity == 0 ? "Out of Stock" : StockQuantity <= ReorderLevel ? "Low Stock" : "In Stock";

        [NotMapped]
        public decimal Profit => SellingPrice - CostPrice;

        // Navigation Properties
        [ForeignKey("CategoryId")]
        public virtual ProductCategory? Category { get; set; }

        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public virtual ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();

        public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

        public virtual ICollection<StockAlert> StockAlerts { get; set; } = new List<StockAlert>();

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    /// <summary>
    /// Product Image entity
    /// </summary>
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(255)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AltText { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsMain { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }

    /// <summary>
    /// Product Specification entity
    /// </summary>
    public class ProductSpecification
    {
        [Key]
        public int SpecId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string SpecName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string SpecValue { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }

    /// <summary>
    /// Inventory Transaction entity
    /// </summary>
    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(30)]
        public string TransactionType { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int PreviousStock { get; set; }

        public int NewStock { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalCost { get; set; }

        [StringLength(50)]
        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }
    }

    /// <summary>
    /// Stock Alert entity
    /// </summary>
    public class StockAlert
    {
        [Key]
        public int AlertId { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(30)]
        public string AlertType { get; set; } = string.Empty;

        public int CurrentStock { get; set; }

        public int ThresholdLevel { get; set; }

        public bool IsResolved { get; set; } = false;

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("ResolvedBy")]
        public virtual User? ResolvedByUser { get; set; }
    }

    /// <summary>
    /// Supplier entity
    /// </summary>
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }

        [StringLength(20)]
        public string? SupplierCode { get; set; }

        [Required]
        [StringLength(100)]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(255)]
        public string? Website { get; set; }

        [StringLength(100)]
        public string? PaymentTerms { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public int? Rating { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }

    public class PurchaseOrder
    {
        [Key]
        public int PurchaseOrderId { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? ExpectedDeliveryDate { get; set; }

        public DateTime? ActualDeliveryDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Shipped, Completed, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        public virtual ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }

    public class PurchaseOrderItem
    {
        [Key]
        public int PurchaseOrderItemId { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        // Navigation Properties
        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }
    }
}
