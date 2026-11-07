using System.ComponentModel.DataAnnotations;

namespace EarnPlatform.Models;

/// <summary>
/// Single-row settings table (like ContactSettings/ChannelSettings) holding
/// the content shown on the user-facing Salary page. Admin-editable so the
/// program description can change without a code deployment.
/// </summary>
public class SalarySettings
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = "Salary Program";

    [Required, MaxLength(4000)]
    public string Description { get; set; } = default!;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}