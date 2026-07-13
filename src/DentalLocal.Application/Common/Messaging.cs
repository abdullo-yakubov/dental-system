using MediatR;

namespace DentalLocal.Application.Common;

/// <summary>Маркер команды — используется TransactionBehavior для оборачивания в транзакцию БД.</summary>
public interface ITransactionalRequest;

public interface ICommand : IRequest<Result>, ITransactionalRequest;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, ITransactionalRequest;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
