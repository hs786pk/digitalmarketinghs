using EarnPlatform.Api.Dtos;
using EarnPlatform.Common.Exceptions;
using EarnPlatform.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Api.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;
    private readonly IHostEnvironment _env;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        if (!context.HttpContext.Request.Path.StartsWithSegments("/api"))
            return;

        var (status, body) = Map(context.Exception);
        var requestId = context.HttpContext.GetCorrelationId();
        body.RequestId = requestId;

        if (status >= 500)
        {
            _logger.LogError(context.Exception,
                "API unhandled error RequestId={RequestId} Path={Path} Status={Status}",
                requestId,
                context.HttpContext.Request.Path.Value,
                status);
        }
        else
        {
            _logger.LogInformation(
                "API business error RequestId={RequestId} Status={Status} Message={Message}",
                requestId,
                status,
                body.Message);
        }

        context.Result = new ObjectResult(body) { StatusCode = status };
        context.ExceptionHandled = true;
    }

    private (int Status, ApiErrorResponse Body) Map(Exception ex) => ex switch
    {
        ValidationException v => (StatusCodes.Status400BadRequest, new ApiErrorResponse
        {
            Message = v.Message,
            Errors = v.Errors
        }),
        NotFoundException n => (StatusCodes.Status404NotFound, new ApiErrorResponse { Message = n.Message }),
        ConflictException c => (StatusCodes.Status409Conflict, new ApiErrorResponse { Message = c.Message }),
        InsufficientBalanceException b => (StatusCodes.Status400BadRequest, new ApiErrorResponse { Message = b.Message }),
        ForbiddenException f => (StatusCodes.Status403Forbidden, new ApiErrorResponse { Message = f.Message }),
        UnauthorizedAppException u => (StatusCodes.Status401Unauthorized, new ApiErrorResponse { Message = u.Message }),
        ConcurrencyException x => (StatusCodes.Status409Conflict, new ApiErrorResponse { Message = x.Message }),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, new ApiErrorResponse
        {
            Message = "The record was modified by another request. Please try again."
        }),
        DbUpdateException => (StatusCodes.Status409Conflict, new ApiErrorResponse
        {
            Message = "Could not save changes. A conflicting record may already exist."
        }),
        InvalidOperationException inv => (StatusCodes.Status400BadRequest, new ApiErrorResponse
        {
            Message = inv.Message
        }),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, new ApiErrorResponse
        {
            Message = "You are not allowed to perform this action."
        }),
        _ => (StatusCodes.Status500InternalServerError, new ApiErrorResponse
        {
            Message = _env.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Please try again later."
        })
    };
}