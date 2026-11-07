using EarnPlatform.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class AdminAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = default!;

    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser ApplicationUser { get; set; } = default!;

    [Required, MaxLength(150)]
    public string Name { get; set; } = default!;

    [Required, MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required, MaxLength(50)]
    public string Role { get; set; } = "Admin"; // e.g. "Admin", "SuperAdmin"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}