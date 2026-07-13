using DentalLocal.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalLocal.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = Guid.NewGuid();

        logger.LogInformation("Начало обработки {RequestName} [{CorrelationId}]", requestName, correlationId);

        var response = await next(cancellationToken);

        if (response.IsSuccess)
        {
            logger.LogInformation("Успешно обработан {RequestName} [{CorrelationId}]", requestName, correlationId);
        }
        else
        {
            logger.LogWarning(
                "Ошибка при обработке {RequestName} [{CorrelationId}]: {ErrorCode} — {ErrorMessage}",
                requestName, correlationId, response.Error.Code, response.Error.Message);
        }

        return response;
    }
}
