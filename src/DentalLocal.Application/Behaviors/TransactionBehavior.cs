using DentalLocal.Application.Common;
using DentalLocal.Domain.Exceptions;
using DentalLocal.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalLocal.Application.Behaviors;

/// <summary>Оборачивает выполнение команд (не запросов) в транзакцию БД — см. п.6 ТЗ.</summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>, ITransactionalRequest
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(cancellationToken);

            if (response.IsSuccess)
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return response;
        }
        catch (ConcurrencyConflictException ex)
        {
            // Кто-то другой изменил ту же запись между чтением и сохранением (см. xmin на ToothState,
            // промпт 30 аудита) — это ожидаемый конфликт, а не 500: клиент должен перечитать данные
            // и предложить повторить действие, а не увидеть общую ошибку сервера.
            logger.LogWarning("Конфликт версий при обработке {RequestName}: {Message}", typeof(TRequest).Name, ex.Message);
            await transaction.RollbackAsync(cancellationToken);
            return CreateFailureResponse(Error.Conflict("Concurrency.Conflict", ex.Message));
        }
        catch
        {
            logger.LogError("Транзакция для {RequestName} отменена из-за необработанного исключения", typeof(TRequest).Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static TResponse CreateFailureResponse(Error error)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        var valueType = responseType.GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
            .MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
