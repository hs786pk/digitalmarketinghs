using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Helpers;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/work")]
[Authorize(Policy = "RequireActiveUserApi")]
public class WorkController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IWorkTaskService _workTaskService;

    public WorkController(
        IUserAccountRepository userAccountRepository,
        IWorkTaskService workTaskService)
    {
        _userAccountRepository = userAccountRepository;
        _workTaskService = workTaskService;
    }

    [HttpGet]
    public async Task<ActionResult<WorkListDto>> List(CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        var remaining = await _workTaskService.GetRemainingDailyTasksAsync(account.Id, cancellationToken);
        var tasks = await _workTaskService.GetActiveTasksForUserAsync(cancellationToken);

        return Ok(new WorkListDto
        {
            RemainingDailyTasks = remaining,
            Tasks = tasks.Select(t => new WorkTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                AdUrl = t.AdUrl,
                RewardAmount = t.RewardAmount
            }).ToList()
        });
    }

    [HttpPost("{workTaskId:int}/start")]
    public async Task<ActionResult<StartTaskResponse>> Start(
        int workTaskId,
        CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        try
        {
            var completion = await _workTaskService.StartTaskAsync(
                account.Id, workTaskId, cancellationToken);

            // Title/AdUrl/Reward may be on completion.WorkTask if loaded; otherwise re-list
            var tasks = await _workTaskService.GetActiveTasksForUserAsync(cancellationToken);
            var task = tasks.FirstOrDefault(t => t.Id == workTaskId);

            return Ok(new StartTaskResponse
            {
                CompletionId = completion.Id,
                WorkTaskId = workTaskId,
                Title = task?.Title ?? string.Empty,
                AdUrl = task?.AdUrl ?? string.Empty,
                RewardAmount = task?.RewardAmount ?? 0,
                ClaimDelaySeconds = 15
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Could not start task. You may have no remaining daily tasks or an active session."
            });
        }
    }

    [HttpPost("completions/{completionId:int}/complete")]
    public async Task<ActionResult<CompleteTaskResponse>> Complete(
        int completionId,
        CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        try
        {
            var completion = await _workTaskService.CompleteTaskAsync(
                account.Id, completionId, cancellationToken);

            return Ok(new CompleteTaskResponse
            {
                CompletionId = completion.Id,
                RewardAmount = 0, // actual credit is server-side; client can refresh wallet
                Message = "Reward credited to your wallet."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Could not complete task. Wait for the claim delay or start the task again."
            });
        }
    }
}