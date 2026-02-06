using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Approval Request Model for Staff-to-Admin approval workflow
    /// </summary>
    public class ApprovalRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestCode { get; set; } = string.Empty;

        // Request Type and Module
        [Required]
        [StringLength(50)]
        public string RequestType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty;

        // Request Details
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        // Related Entity
        [StringLength(50)]
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        [StringLength(200)]
        public string? EntityName { get; set; }

        // Request Data (JSON)
        public string? RequestData { get; set; }

        // Status
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(20)]
        public string Priority { get; set; } = "Normal";

        // Requester Info
        [Required]
        public int RequestedBy { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Approver Info
        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovalNotes { get; set; }

        // Notification
        public bool IsRead { get; set; } = false;

        public bool NotificationSent { get; set; } = false;

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("RequestedBy")]
        public virtual User? Requester { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual User? Approver { get; set; }
    }

    // Request Types Constants
    public static class ApprovalRequestTypes
    {
        // Inventory Module
        public const string StockAdjustment = "StockAdjustment";
        public const string ProductCreate = "ProductCreate";
        public const string ProductUpdate = "ProductUpdate";
        public const string ProductDelete = "ProductDelete";
        public const string PriceChange = "PriceChange";
        public const string SupplierCreate = "SupplierCreate";
        public const string PurchaseOrder = "PurchaseOrder";

        // Sales Module
        public const string OrderCancel = "OrderCancel";
        public const string OrderRefund = "OrderRefund";
        public const string DiscountOverride = "DiscountOverride";
        public const string LeadConvert = "LeadConvert";

        // Marketing Module
        public const string CampaignCreate = "CampaignCreate";
        public const string CampaignActivate = "CampaignActivate";
        public const string PromotionCreate = "PromotionCreate";
        public const string PromotionExtend = "PromotionExtend";

        // Billing Module
        public const string InvoiceVoid = "InvoiceVoid";
        public const string PaymentRefund = "PaymentRefund";
        public const string CreditNote = "CreditNote";

        // Support Module
        public const string TicketEscalate = "TicketEscalate";
        public const string RefundApproval = "RefundApproval";
        public const string CompensationOffer = "CompensationOffer";
    }

    // Module Constants
    public static class ApprovalModules
    {
        public const string Inventory = "Inventory";
        public const string Sales = "Sales";
        public const string Marketing = "Marketing";
        public const string Billing = "Billing";
        public const string Support = "Support";
    }

    // Status Constants
    public static class ApprovalStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string Cancelled = "Cancelled";
    }

    // Priority Constants
    public static class ApprovalPriority
    {
        public const string Low = "Low";
        public const string Normal = "Normal";
        public const string High = "High";
        public const string Urgent = "Urgent";
    }

    // DTO for creating approval requests
    public class CreateApprovalRequestDto
    {
        [Required]
        public string RequestType { get; set; } = string.Empty;

        [Required]
        public string Module { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        public string? EntityName { get; set; }

        public string? RequestData { get; set; }

        public string Priority { get; set; } = "Normal";
    }

    // DTO for approval/rejection
    public class ProcessApprovalDto
    {
        [Required]
        public int RequestId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // Approve or Reject

        public string? ApprovalNotes { get; set; }
    }

    // DTO for approval request list response
    public class ApprovalRequestListDto
    {
        public int RequestId { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public int? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string? RequestData { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterEmail { get; set; } = string.Empty;
        public string RequesterRole { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public string? ApproverName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovalNotes { get; set; }
        public bool IsRead { get; set; }
    }
}
