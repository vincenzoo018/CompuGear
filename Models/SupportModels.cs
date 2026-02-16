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

    /// <summary>
    /// Canned Response / Quick Reply template
    /// </summary>
    public class CannedResponse
    {
        [Key]
        public int ResponseId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Shortcut { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; }

        public bool IsGlobal { get; set; } = false;

        public int UsageCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? CreatedByUser { get; set; }
    }

    /// <summary>
    /// SLA Configuration
    /// </summary>
    public class SLAConfig
    {
        [Key]
        public int SLAId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string SLAName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        public int FirstResponseHours { get; set; } = 4;

        public int ResolutionHours { get; set; } = 24;

        public bool EscalateOnBreach { get; set; } = true;

        public int? EscalateToUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("EscalateToUserId")]
        public virtual User? EscalateToUser { get; set; }
    }

    /// <summary>
    /// Ticket Auto-Assignment Rule
    /// </summary>
    public class TicketAssignmentRule
    {
        [Key]
        public int RuleId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [StringLength(20)]
        public string AssignmentType { get; set; } = "RoundRobin"; // RoundRobin, LeastBusy, Skills, Category

        public int? CategoryId { get; set; }

        [StringLength(50)]
        public string? RequiredSkill { get; set; }

        public int Priority { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CategoryId")]
        public virtual TicketCategory? Category { get; set; }

        public virtual ICollection<AssignmentRuleAgent> Agents { get; set; } = new List<AssignmentRuleAgent>();
    }

    /// <summary>
    /// Agents assigned to auto-assignment rule
    /// </summary>
    public class AssignmentRuleAgent
    {
        [Key]
        public int Id { get; set; }

        public int RuleId { get; set; }

        public int UserId { get; set; }

        public int? MaxTickets { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Navigation Properties
        [ForeignKey("RuleId")]
        public virtual TicketAssignmentRule? Rule { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Customer Satisfaction Survey
    /// </summary>
    public class SatisfactionSurvey
    {
        [Key]
        public int SurveyId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        public int TicketId { get; set; }

        public int CustomerId { get; set; }

        public int? AgentId { get; set; }

        [Range(1, 5)]
        public int? Rating { get; set; }

        [StringLength(20)]
        public string? Sentiment { get; set; } // Happy, Neutral, Unhappy

        public string? Feedback { get; set; }

        public bool WouldRecommend { get; set; } = true;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        // Navigation Properties
        [ForeignKey("TicketId")]
        public virtual SupportTicket? Ticket { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("AgentId")]
        public virtual User? Agent { get; set; }
    }

    /// <summary>
    /// Ticket Internal Notes
    /// </summary>
    public class TicketNote
    {
        [Key]
        public int NoteId { get; set; }

        [Required]
        public int TicketId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsPrivate { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("TicketId")]
        public virtual SupportTicket? Ticket { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Ticket Tags for categorization
    /// </summary>
    public class TicketTag
    {
        [Key]
        public int TagId { get; set; }

        public int? CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string TagName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Color { get; set; } = "#6c757d";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Ticket-Tag Many-to-Many
    /// </summary>
    public class TicketTagMapping
    {
        [Key]
        public int MappingId { get; set; }

        public int TicketId { get; set; }

        public int TagId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("TicketId")]
        public virtual SupportTicket? Ticket { get; set; }

        [ForeignKey("TagId")]
        public virtual TicketTag? Tag { get; set; }
    }

    /// <summary>
    /// Ticket Time Tracking
    /// </summary>
    public class TicketTimeEntry
    {
        [Key]
        public int EntryId { get; set; }

        [Required]
        public int TicketId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public int DurationMinutes { get; set; } = 0;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsBillable { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("TicketId")]
        public virtual SupportTicket? Ticket { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Ticket Link/Merge for related tickets
    /// </summary>
    public class TicketLink
    {
        [Key]
        public int LinkId { get; set; }

        [Required]
        public int TicketId { get; set; }

        [Required]
        public int LinkedTicketId { get; set; }

        [StringLength(20)]
        public string LinkType { get; set; } = "Related"; // Related, Duplicate, Parent, Child, Blocks, BlockedBy

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("TicketId")]
        public virtual SupportTicket? Ticket { get; set; }

        [ForeignKey("LinkedTicketId")]
        public virtual SupportTicket? LinkedTicket { get; set; }
    }

    /// <summary>
    /// Merged Ticket Record
    /// </summary>
    public class TicketMerge
    {
        [Key]
        public int MergeId { get; set; }

        [Required]
        public int PrimaryTicketId { get; set; }

        [Required]
        public int MergedTicketId { get; set; }

        public int MergedBy { get; set; }

        public DateTime MergedAt { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? MergeReason { get; set; }

        // Navigation Properties
        [ForeignKey("PrimaryTicketId")]
        public virtual SupportTicket? PrimaryTicket { get; set; }

        [ForeignKey("MergedTicketId")]
        public virtual SupportTicket? MergedTicket { get; set; }
    }

    /// <summary>
    /// Chat Transfer Record
    /// </summary>
    public class ChatTransfer
    {
        [Key]
        public int TransferId { get; set; }

        [Required]
        public int ChatSessionId { get; set; }

        public int FromUserId { get; set; }

        public int ToUserId { get; set; }

        [StringLength(255)]
        public string? Reason { get; set; }

        public DateTime TransferredAt { get; set; } = DateTime.UtcNow;

        public bool Accepted { get; set; } = false;

        public DateTime? AcceptedAt { get; set; }

        // Navigation Properties
        [ForeignKey("ChatSessionId")]
        public virtual ChatSession? ChatSession { get; set; }

        [ForeignKey("FromUserId")]
        public virtual User? FromUser { get; set; }

        [ForeignKey("ToUserId")]
        public virtual User? ToUser { get; set; }
    }

    /// <summary>
    /// Chat File Attachment
    /// </summary>
    public class ChatAttachment
    {
        [Key]
        public int AttachmentId { get; set; }

        public int ChatMessageId { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; } = 0;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("ChatMessageId")]
        public virtual ChatMessage? ChatMessage { get; set; }
    }

    /// <summary>
    /// Chat Transcript Email Record
    /// </summary>
    public class ChatTranscript
    {
        [Key]
        public int TranscriptId { get; set; }

        [Required]
        public int ChatSessionId { get; set; }

        [Required]
        [StringLength(100)]
        public string RecipientEmail { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool DeliverySuccess { get; set; } = true;

        // Navigation Properties
        [ForeignKey("ChatSessionId")]
        public virtual ChatSession? ChatSession { get; set; }
    }
}
