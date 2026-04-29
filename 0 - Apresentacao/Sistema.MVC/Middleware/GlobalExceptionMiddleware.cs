using System.Net;
using System.Text.Json;
using Sistema.MVC.Models;

namespace Sistema.MVC.Middleware;

public partial class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Erro de validação")]
    private static partial void LogValidationWarning(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Erro não tratado")]
    private static partial void LogUnhandledError(ILogger logger, Exception ex);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException ex)
        {
            LogValidationWarning(logger, ex);
            await WriteErrorAsync(context, HttpStatusCode.UnprocessableEntity, ex.Message);
        }
        catch (Exception ex)
        {
            LogUnhandledError(logger, ex);
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "Erro interno do servidor.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        var payload = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            TraceId = context.TraceIdentifier
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
