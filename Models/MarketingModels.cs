using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Marketing Campaign entity
    /// </summary>
    public class Campaign
    {
        [Key]
        public int CampaignId { get; set; }

        public int? CompanyId { get; set; }

        [StringLength(20)]
        public string? CampaignCode { get; set; }

        [Required]
        [StringLength(200)]
        public string CampaignName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        // Dates
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // Budget
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Budget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualSpend { get; set; } = 0;

        // Targeting
        [StringLength(100)]
        public string? TargetSegment { get; set; }

        public string? TargetAudience { get; set; }

        // Performance
        public int TotalReach { get; set; } = 0;

        public int Impressions { get; set; } = 0;

        public int Clicks { get; set; } = 0;

        public int Conversions { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Revenue { get; set; } = 0;

        // Content
        [StringLength(200)]
        public string? Subject { get; set; }

        public string? Content { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public decimal ClickThroughRate => Impressions > 0 ? (decimal)Clicks / Impressions * 100 : 0;

        [NotMapped]
        public decimal ConversionRate => Clicks > 0 ? (decimal)Conversions / Clicks * 100 : 0;

        [NotMapped]
        public decimal ROI => ActualSpend > 0 ? (Revenue - ActualSpend) / ActualSpend * 100 : 0;

        // Navigation Properties
        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }

        public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
    }

    /// <summary>
    /// Customer Segment entity
    /// </summary>
    public class CustomerSegment
    {
        [Key]
        public int SegmentId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string SegmentName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string? Criteria { get; set; }

        public int CustomerCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsAutomatic { get; set; } = true;

        public DateTime? LastUpdated { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        public virtual ICollection<SegmentMember> Members { get; set; } = new List<SegmentMember>();
    }

    /// <summary>
    /// Segment Member entity
    /// </summary>
    public class SegmentMember
    {
        [Key]
        public int MemberId { get; set; }

        public int SegmentId { get; set; }

        public int CustomerId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("SegmentId")]
        public virtual CustomerSegment Segment { get; set; } = null!;

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;
    }

    /// <summary>
    /// Promotion / Discount Code entity
    /// </summary>
    public class Promotion
    {
        [Key]
        public int PromotionId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string PromotionCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string PromotionName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        // Validity
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        // Usage Limits
        public int? UsageLimit { get; set; }

        public int UsageLimitPerCustomer { get; set; } = 1;

        public int TimesUsed { get; set; } = 0;

        // Restrictions
        public string? ApplicableProducts { get; set; }

        public string? ApplicableCategories { get; set; }

        public string? ApplicableCustomers { get; set; }

        public bool ExcludeOnSaleItems { get; set; } = false;

        // Status
        public bool IsActive { get; set; } = true;

        public int? CampaignId { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public bool IsValid => IsActive && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate && (!UsageLimit.HasValue || TimesUsed < UsageLimit);

        // Navigation Properties
        [ForeignKey("CampaignId")]
        public virtual Campaign? Campaign { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        public virtual ICollection<PromotionUsage> UsageHistory { get; set; } = new List<PromotionUsage>();
    }

    /// <summary>
    /// Promotion Usage History entity
    /// </summary>
    public class PromotionUsage
    {
        [Key]
        public int UsageId { get; set; }

        public int PromotionId { get; set; }

        public int OrderId { get; set; }

        public int CustomerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountApplied { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("PromotionId")]
        public virtual Promotion Promotion { get; set; } = null!;

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;
    }

    /// <summary>
    /// A/B Test entity for campaign testing
    /// </summary>
    public class ABTest
    {
        [Key]
        public int TestId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string TestName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? CampaignId { get; set; }

        [StringLength(20)]
        public string TestType { get; set; } = "Email"; // Email, Content, Subject, Landing

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Running, Completed, Cancelled

        public int? WinningVariantId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("CampaignId")]
        public virtual Campaign? Campaign { get; set; }

        public virtual ICollection<ABTestVariant> Variants { get; set; } = new List<ABTestVariant>();
    }

    /// <summary>
    /// A/B Test Variant entity
    /// </summary>
    public class ABTestVariant
    {
        [Key]
        public int VariantId { get; set; }

        [Required]
        public int TestId { get; set; }

        [Required]
        [StringLength(10)]
        public string VariantName { get; set; } = "A"; // A, B, C, etc.

        [StringLength(200)]
        public string? Subject { get; set; }

        public string? Content { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TrafficPercentage { get; set; } = 50;

        // Metrics
        public int SentCount { get; set; } = 0;

        public int OpenCount { get; set; } = 0;

        public int ClickCount { get; set; } = 0;

        public int ConversionCount { get; set; } = 0;

        [NotMapped]
        public decimal OpenRate => SentCount > 0 ? (decimal)OpenCount / SentCount * 100 : 0;

        [NotMapped]
        public decimal ClickRate => OpenCount > 0 ? (decimal)ClickCount / OpenCount * 100 : 0;

        [NotMapped]
        public decimal ConversionRate => ClickCount > 0 ? (decimal)ConversionCount / ClickCount * 100 : 0;

        // Navigation Properties
        [ForeignKey("TestId")]
        public virtual ABTest? Test { get; set; }
    }

    /// <summary>
    /// Social Media Post entity
    /// </summary>
    public class SocialMediaPost
    {
        [Key]
        public int PostId { get; set; }

        public int? CompanyId { get; set; }

        public int? CampaignId { get; set; }

        [Required]
        [StringLength(20)]
        public string Platform { get; set; } = "Facebook"; // Facebook, Twitter, Instagram, LinkedIn

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MediaUrl { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, Scheduled, Published, Failed

        [StringLength(255)]
        public string? ExternalPostId { get; set; }

        // Engagement Metrics
        public int Likes { get; set; } = 0;

        public int Shares { get; set; } = 0;

        public int Comments { get; set; } = 0;

        public int Reach { get; set; } = 0;

        public int Impressions { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("CampaignId")]
        public virtual Campaign? Campaign { get; set; }
    }
}
