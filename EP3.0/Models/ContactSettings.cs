using System.ComponentModel.DataAnnotations;

namespace EarnPlatform.Models;

public class ContactSettings
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string WhatsAppAccountName { get; set; } = default!;

    [Required, MaxLength(30)]
    public string WhatsAppNumber { get; set; } = default!;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}