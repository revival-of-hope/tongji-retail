using System.Text.Json;
using RetailSystem.Api.Contracts;

namespace RetailSystem.Api.Middleware;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "服务器内部错误");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        var body = new ApiEnvelope<object>(statusCode, message, null);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
