using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Exceptions;

namespace DentalLocal.Domain.Tests;

public class DebtTests
{
    [Fact]
    public void Create_PositiveAmount_StatusIsOpen()
    {
        var debt = Debt.Create(Guid.NewGuid(), Guid.NewGuid(), 100_000m);

        Assert.Equal(DebtStatus.Open, debt.Status);
        Assert.Equal(100_000m, debt.RemainingAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveAmount_Throws(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Debt.Create(Guid.NewGuid(), Guid.NewGuid(), amount));
    }

    [Fact]
    public void ApplyPayment_PartialAmount_StaysOpen()
    {
        var debt = Debt.Create(Guid.NewGuid(), Guid.NewGuid(), 100_000m);

        debt.ApplyPayment(40_000m);

        Assert.Equal(60_000m, debt.RemainingAmount);
        Assert.Equal(DebtStatus.Open, debt.Status);
    }

    [Fact]
    public void ApplyPayment_FullAmount_ClosesDebt()
    {
        var debt = Debt.Create(Guid.NewGuid(), Guid.NewGuid(), 100_000m);

        debt.ApplyPayment(100_000m);

        Assert.Equal(0m, debt.RemainingAmount);
        Assert.Equal(DebtStatus.Closed, debt.Status);
        Assert.NotNull(debt.ClosedAt);
    }

    [Fact]
    public void ApplyPayment_ExceedsRemaining_Throws()
    {
        var debt = Debt.Create(Guid.NewGuid(), Guid.NewGuid(), 100_000m);

        Assert.Throws<DomainException>(() => debt.ApplyPayment(150_000m));
    }

    [Fact]
    public void ApplyPayment_AlreadyClosed_Throws()
    {
        var debt = Debt.Create(Guid.NewGuid(), Guid.NewGuid(), 100_000m);
        debt.ApplyPayment(100_000m);

        Assert.Throws<DomainException>(() => debt.ApplyPayment(1m));
    }
}
