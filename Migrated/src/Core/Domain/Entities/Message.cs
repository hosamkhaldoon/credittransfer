using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CreditTransfer.Core.Domain.Entities;

/// <summary>
/// Represents bilingual message templates for SMS notifications and system messages
/// Used for credit transfer notifications and error messages
/// </summary>
[Table("Messages")]
public class Message
{
    /// <summary>
    /// Primary key for the message
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unique key identifier for the message
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// English text of the message
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string TextEn { get; set; } = string.Empty;

    /// <summary>
    /// Arabic text of the message
    /// </summary>
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string TextAr { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this message is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this message was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this message was last modified
    /// </summary>
    public DateTime? LastModified { get; set; }
} 