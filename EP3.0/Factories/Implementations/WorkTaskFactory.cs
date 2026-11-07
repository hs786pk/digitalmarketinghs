using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;

namespace EarnPlatform.Factories.Implementations;

public class WorkTaskFactory : IWorkTaskFactory
{
    public WorkTask CreateWorkTask(string title, string? description, string adUrl, decimal rewardAmount, bool isActive = true)
    {
        return new WorkTask
        {
            Title = title,
            Description = description,
            AdUrl = adUrl,
            RewardAmount = rewardAmount,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}