using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class WorkTaskCompletion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserAccount User { get; set; } = default!;

    [Required]
    public int WorkTaskId { get; set; }

    [ForeignKey(nameof(WorkTaskId))]
    public WorkTask WorkTask { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal RewardAmount { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public WorkTaskCompletionStatus Status { get; set; } = WorkTaskCompletionStatus.Started;
}