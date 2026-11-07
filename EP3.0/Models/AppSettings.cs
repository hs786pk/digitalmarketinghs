using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

// Singleton-style settings row (Id always = 1) for the payment/receiving account
public class AppSettings
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string PaymentAccountName { get; set; } = default!;

    [Required, MaxLength(50)]
    public string PaymentAccountNumber { get; set; } = default!;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}