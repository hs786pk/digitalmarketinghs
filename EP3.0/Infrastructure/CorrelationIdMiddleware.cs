namespace EarnPlatform.Infrastructure;

/// <summary>
/// Ensures every request has a correlation id (header + HttpContext.Items + response header).
/// Clients may send X-Request-Id; otherwise one is generated.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Request-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(incoming)
            ? Guid.NewGuid().ToString("N")
            : incoming.Trim();

        if (correlationId.Length > 64)
            correlationId = correlationId[..64];

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (context.RequestServices
                   .GetRequiredService<ILoggerFactory>()
                   .CreateLogger("EarnPlatform.Request")
                   .BeginScope(new Dictionary<string, object>
                   {
                       ["RequestId"] = correlationId,
                       ["Path"] = context.Request.Path.Value ?? "",
                       ["Method"] = context.Request.Method
                   }))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdExtensions
{
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var v)
            ? v as string
            : null;

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}