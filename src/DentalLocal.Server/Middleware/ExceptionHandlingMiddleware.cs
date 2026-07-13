using Microsoft.AspNetCore.Http;

namespace DentalLocal.Server.Middleware;

/// <summary>Ловит необработанные исключения, логирует их и отдаёт клиенту ProblemDetails 500 без внутренних деталей (см. п.7 ТЗ).</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid();
            logger.LogError(ex, "Необработанное исключение [{CorrelationId}]", correlationId);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Внутренняя ошибка сервера.",
                extensions: new Dictionary<string, object?> { ["correlationId"] = correlationId }
            ).ExecuteAsync(context);
        }
    }
}
