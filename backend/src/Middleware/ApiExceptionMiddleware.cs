using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected. There is no response left to write.
        }
        catch (BadHttpRequestException ex)
        {
            logger.LogWarning(ex, "Invalid HTTP request");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "请求格式或参数不正确");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON request body");
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "请求 JSON 格式不正确");
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Database concurrency conflict");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "数据已被其他请求修改，请刷新后重试");
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Database constraint conflict");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "数据冲突或不符合数据库约束，请检查后重试");
        }
        catch (DbException ex) when (IsDatabaseConflict(ex))
        {
            logger.LogWarning(ex, "Database transaction conflict");
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "数据库并发或约束冲突，请刷新后重试");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception");
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "服务器内部错误");
        }
    }

    private static bool IsDatabaseConflict(DbException exception)
    {
        var message = exception.ToString();
        return message.Contains("ORA-08177", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ORA-00001", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ORA-02290", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ORA-02291", StringComparison.OrdinalIgnoreCase)
            || message.Contains("ORA-02292", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await ApiResults.WriteAsync(context.Response, statusCode, message);
    }
}
