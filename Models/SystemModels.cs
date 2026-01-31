using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Activity Log entity - Comprehensive audit trail
    /// </summary>
    public class ActivityLog
    {
        [Key]
        public long LogId { get; set; }

        public int? UserId { get; set; }

        [StringLength(100)]
        public string? UserName { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty;

        [StringLength(50)]
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        [StringLength(100)]
        public string? SessionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    /// <summary>
    /// Notification entity
    /// </summary>
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Message { get; set; }

        [StringLength(500)]
        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }

    /// <summary>
    /// Chat Session entity
    /// </summary>
    public class ChatSession
    {
        [Key]
        public int SessionId { get; set; }

        public int? CustomerId { get; set; }

        [StringLength(100)]
        public string? VisitorId { get; set; }

        [StringLength(255)]
        public string? SessionToken { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public int? AgentId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        // Metadata
        [StringLength(50)]
        public string? Source { get; set; }

        [StringLength(20)]
        public string? DeviceType { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        // Stats
        public int TotalMessages { get; set; } = 0;

        public int? Rating { get; set; }

        [StringLength(500)]
        public string? Feedback { get; set; }

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [ForeignKey("AgentId")]
        public virtual User? Agent { get; set; }

        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    /// <summary>
    /// Chat Message entity
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        public int SessionId { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderType { get; set; } = string.Empty;

        public int? SenderId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        public string MessageType { get; set; } = "Text";

        public string? Metadata { get; set; }

        [StringLength(100)]
        public string? Intent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Confidence { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("SessionId")]
        public virtual ChatSession Session { get; set; } = null!;
    }

    /// <summary>
    /// Chat Bot Intent entity
    /// </summary>
    public class ChatBotIntent
    {
        [Key]
        public int IntentId { get; set; }

        [Required]
        [StringLength(100)]
        public string IntentName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public string? TrainingPhrases { get; set; }

        public string? Responses { get; set; }

        public string? Actions { get; set; }

        public bool IsActive { get; set; } = true;

        public int Priority { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// System Setting entity
    /// </summary>
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        public string? SettingValue { get; set; }

        [StringLength(20)]
        public string SettingType { get; set; } = "String";

        [StringLength(50)]
        public string? Category { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsEditable { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? UpdatedBy { get; set; }

        // Navigation Properties
        [ForeignKey("UpdatedBy")]
        public virtual User? UpdatedByUser { get; set; }
    }
}
