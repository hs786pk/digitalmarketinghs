using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

// Named WorkTask (not "Task") to avoid clashing with System.Threading.Tasks.Task
public class WorkTask
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = default!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(500)]
    public string AdUrl { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RewardAmount { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkTaskCompletion> Completions { get; set; } = new List<WorkTaskCompletion>();
}