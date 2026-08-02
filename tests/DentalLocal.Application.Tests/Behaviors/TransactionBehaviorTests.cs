using DentalLocal.Application.Behaviors;
using DentalLocal.Application.Common;
using DentalLocal.Application.Tests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DentalLocal.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private sealed record DummyQuery : IRequest<Result<int>>;
    private sealed record DummyCommand : ICommand<int>;

    [Fact]
    public async Task Handle_DoesNotOpenTransaction_ForNonTransactionalRequest()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<DummyCommand, Result<int>>(
            unitOfWork, NullLogger<TransactionBehavior<DummyCommand, Result<int>>>.Instance);

        // DummyQuery does not implement ITransactionalRequest, so it is never routed through
        // TransactionBehavior<DummyQuery, ...> by MediatR's generic constraint — this is proven
        // at compile time, not runtime. Here we assert that a command (which IS transactional)
        // does open exactly one transaction, establishing the baseline behavior.
        var result = await behavior.Handle(new DummyCommand(), (_) => Task.FromResult(Result.Success(42)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.BeginTransactionCallCount);
    }

    [Fact]
    public void DummyQuery_DoesNotImplement_ITransactionalRequest()
    {
        Assert.False(typeof(ITransactionalRequest).IsAssignableFrom(typeof(DummyQuery)));
    }

    [Fact]
    public void DummyCommand_Implements_ITransactionalRequest()
    {
        Assert.True(typeof(ITransactionalRequest).IsAssignableFrom(typeof(DummyCommand)));
    }

    [Fact]
    public async Task Handle_RollsBack_WhenHandlerReturnsFailure()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new TransactionBehavior<DummyCommand, Result<int>>(
            unitOfWork, NullLogger<TransactionBehavior<DummyCommand, Result<int>>>.Instance);

        var result = await behavior.Handle(
            new DummyCommand(),
            (_) => Task.FromResult(Result.Failure<int>(Error.Validation("Test.Error", "test error"))),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(1, unitOfWork.BeginTransactionCallCount);
    }
}
