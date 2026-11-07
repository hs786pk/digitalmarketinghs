using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class Package
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int Level { get; set; }

    public int DailyAdLimit { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserAccount> Subscribers { get; set; } = new List<UserAccount>();
    public ICollection<PaymentApproval> PaymentApprovals { get; set; } = new List<PaymentApproval>();
}