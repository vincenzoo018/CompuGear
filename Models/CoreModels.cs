using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompuGear.Models
{
    /// <summary>
    /// Role entity - Represents user roles (7 Actor Types)
    /// </summary>
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public int AccessLevel { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>
    /// Permission entity - Represents system permissions
    /// </summary>
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }

        [Required]
        [StringLength(50)]
        public string PermissionName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Module { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>
    /// RolePermission junction entity
    /// </summary>
    public class RolePermission
    {
        [Key]
        public int RolePermissionId { get; set; }

        public int RoleId { get; set; }

        public int PermissionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;
    }

    /// <summary>
    /// Company entity
    /// </summary>
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CompanyCode { get; set; }

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
        public string? Logo { get; set; }

        [StringLength(255)]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? TaxId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }

    /// <summary>
    /// User entity - Represents system users
    /// </summary>
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Salt { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Avatar { get; set; }

        public int RoleId { get; set; }

        public int? CompanyId { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsEmailVerified { get; set; } = false;

        public DateTime? LastLoginAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }

        // Computed Properties
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        // For JSON binding (not stored in database)
        [NotMapped]
        public string? Password { get; set; }

        // Navigation Properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }
    }

    /// <summary>
    /// RoleModuleAccess - Defines which roles can access which ERP modules per company
    /// </summary>
    public class RoleModuleAccess
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string ModuleCode { get; set; } = string.Empty;

        public bool HasAccess { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; } = null!;

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;
    }
}
