using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// ERP Module - Represents available ERP modules that companies can subscribe to
    /// </summary>
    public class ERPModule
    {
        [Key]
        public int ModuleId { get; set; }

        [Required]
        [StringLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ModuleCode { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AnnualPrice { get; set; }

        public string? Features { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<CompanyModuleAccess> CompanyAccess { get; set; } = new List<CompanyModuleAccess>();
    }

    /// <summary>
    /// Company Subscription - Represents a company's subscription plan
    /// </summary>
    public class CompanySubscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(50)]
        public string PlanName { get; set; } = "Basic";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(20)]
        public string BillingCycle { get; set; } = "Monthly";

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        public DateTime? TrialEndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyFee { get; set; }

        public int MaxUsers { get; set; } = 5;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;
    }

    /// <summary>
    /// Company Module Access - Junction table for company-module relationships
    /// </summary>
    public class CompanyModuleAccess
    {
        [Key]
        public int AccessId { get; set; }

        public int CompanyId { get; set; }

        public int ModuleId { get; set; }

        public bool IsEnabled { get; set; } = true;

        public DateTime? ActivatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeactivatedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("ModuleId")]
        public virtual ERPModule Module { get; set; } = null!;
    }

    /// <summary>
    /// Platform Usage Log - Tracks usage across the platform
    /// </summary>
    public class PlatformUsageLog
    {
        [Key]
        public int LogId { get; set; }

        public int? CompanyId { get; set; }

        public int? UserId { get; set; }

        [StringLength(50)]
        public string? ModuleCode { get; set; }

        [StringLength(100)]
        public string? Action { get; set; }

        [StringLength(500)]
        public string? Details { get; set; }

        [StringLength(50)]
        public string? IPAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
