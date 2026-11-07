using System.ComponentModel.DataAnnotations;
using EarnPlatform.Models;

namespace EarnPlatform.Api.Dtos;

public class SubmitWithdrawalRequest
{
    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [Required, MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
}

public class WorkTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AdUrl { get; set; } = string.Empty;
    public decimal RewardAmount { get; set; }
}

public class WorkListDto
{
    public int RemainingDailyTasks { get; set; }
    public List<WorkTaskDto> Tasks { get; set; } = new();
}

public class StartTaskResponse
{
    public int CompletionId { get; set; }
    public int WorkTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AdUrl { get; set; } = string.Empty;
    public decimal RewardAmount { get; set; }
    public int ClaimDelaySeconds { get; set; } = 15;
}

public class CompleteTaskResponse
{
    public int CompletionId { get; set; }
    public decimal RewardAmount { get; set; }
    public string Message { get; set; } = "Reward credited.";
}

public class TeamDto
{
    public int ActiveTeamMembers { get; set; }
    public int DirectReferrals { get; set; }
    public int AccountLevel { get; set; }
    public int DailyAdLimit { get; set; }
    public List<TeamMemberDto> Members { get; set; } = new();
}

public class TeamMemberDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string? PackageName { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
}