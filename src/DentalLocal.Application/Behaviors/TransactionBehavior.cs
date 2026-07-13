using DentalLocal.Application.Common;
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
        catch
        {
            logger.LogError("Транзакция для {RequestName} отменена из-за необработанного исключения", typeof(TRequest).Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
