using EarnPlatform.Models;

namespace EarnPlatform.Factories.Interfaces;

public interface IWorkTaskFactory
{
    WorkTask CreateWorkTask(string title, string? description, string adUrl, decimal rewardAmount, bool isActive = true);
}