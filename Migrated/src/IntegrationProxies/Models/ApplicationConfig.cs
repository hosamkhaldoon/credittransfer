using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IntegrationProxies.Nobill.Models
{
    /// <summary>
    /// Lightweight ApplicationConfig entity for database configuration
    /// Used by integration proxy services for runtime configuration management
    /// </summary>
    [Table("ApplicationConfigs")]
    public class ApplicationConfig
    {
        /// <summary>
        /// Primary key for the configuration entry
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Configuration key name (e.g., "NobillCalls_ServiceUrl", "NobillCalls_UserName")
        /// </summary>
        [Required]
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Configuration value as string (can be parsed to appropriate type by consuming code)
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of what this configuration does
        /// </summary>
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? Description { get; set; }

        /// <summary>
        /// Configuration category for grouping related settings
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? Category { get; set; }

        /// <summary>
        /// Whether this configuration is active and should be used
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this configuration was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created this configuration
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// When this configuration was last modified
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Who last modified this configuration
        /// </summary>
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string? ModifiedBy { get; set; }
    }
} 