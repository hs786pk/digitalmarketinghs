using System.ComponentModel.DataAnnotations;

namespace EarnPlatform.Models;

public class ChannelSettings
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string WhatsAppChannelLink { get; set; } = default!;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}