using System.ComponentModel.DataAnnotations;

namespace EarnPlatform.Models;

public class FbrSettings
{
    [Key]
    public int Id { get; set; }

    [MaxLength(300)]
    public string ImagePath { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}