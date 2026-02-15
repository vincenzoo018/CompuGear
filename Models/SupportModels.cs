using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Ticket Category entity
    /// </summary>
    public class TicketCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public int SLAHours { get; set; } = 24;

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<SupportTicket> Tickets { get; set; } = new List<SupportTicket>();
    }

    /// <summary>
    /// Support Ticket entity
    /// </summary>
    public class SupportTicket
    {
        [Key]
        public int TicketId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        public int? CategoryId { get; set; }

        public int? OrderId { get; set; }

        // Contact Info
        [StringLength(100)]
        public string? ContactName { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        // Ticket Details
        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [StringLength(30)]
        public string Status { get; set; } = "Open";

        // Assignment
        public int? AssignedTo { get; set; }

        public DateTime? AssignedAt { get; set; }

        // SLA
        public DateTime? DueDate { get; set; }

        public bool SLABreached { get; set; } = false;

        // Resolution
        public string? Resolution { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedBy { get; set; }

        // Customer Feedback
        public int? SatisfactionRating { get; set; }

        public string? Feedback { get; set; }

        // Source
        [StringLength(30)]
        public string Source { get; set; } = "Web";

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("CategoryId")]
        public virtual TicketCategory? Category { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual User? AssignedUser { get; set; }

        [ForeignKey("ResolvedBy")]
        public virtual User? ResolvedByUser { get; set; }

        public virtual ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();

        public virtual ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    }

    /// <summary>
    /// Ticket Message entity
    /// </summary>
    public class TicketMessage
    {
        [Key]
        public int MessageId { get; set; }

        public int TicketId { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderType { get; set; } = string.Empty;

        public int? SenderId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public bool IsInternal { get; set; } = false;

        public string? Attachments { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("TicketId")]
        public virtual SupportTicket Ticket { get; set; } = null!;

        [ForeignKey("SenderId")]
        public virtual User? Sender { get; set; }
    }

    /// <summary>
    /// Ticket Attachment entity
    /// </summary>
    public class TicketAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        public int TicketId { get; set; }

        public int? MessageId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        public int? FileSize { get; set; }

        [StringLength(50)]
        public string? FileType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int? UploadedBy { get; set; }

        // Navigation Properties
        [ForeignKey("TicketId")]
        public virtual SupportTicket Ticket { get; set; } = null!;

        [ForeignKey("MessageId")]
        public virtual TicketMessage? TicketMessage { get; set; }

        [ForeignKey("UploadedBy")]
        public virtual User? UploadedByUser { get; set; }
    }

    /// <summary>
    /// Knowledge Base Category entity
    /// </summary>
    public class KnowledgeCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ParentCategoryId")]
        public virtual KnowledgeCategory? ParentCategory { get; set; }

        public virtual ICollection<KnowledgeCategory> SubCategories { get; set; } = new List<KnowledgeCategory>();

        public virtual ICollection<KnowledgeArticle> Articles { get; set; } = new List<KnowledgeArticle>();
    }

    /// <summary>
    /// Knowledge Base Article entity
    /// </summary>
    public class KnowledgeArticle
    {
        [Key]
        public int ArticleId { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Summary { get; set; }

        [StringLength(500)]
        public string? Tags { get; set; }

        public int ViewCount { get; set; } = 0;

        public int HelpfulCount { get; set; } = 0;

        public int NotHelpfulCount { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("CategoryId")]
        public virtual KnowledgeCategory? Category { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }
    }
}
