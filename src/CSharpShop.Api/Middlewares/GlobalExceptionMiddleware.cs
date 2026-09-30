using CSharpShop.Application.DTOs;

namespace CSharpShop.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (
            !context.Response.HasStarted &&
            !context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Lỗi khi xử lý {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(
                new ApiResponse<object>
                {
                    Success = false,
                    Message = "Có lỗi hệ thống. Vui lòng thử lại sau.",
                    Data = null
                });
        }
    }
}