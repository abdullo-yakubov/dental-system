using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;
using DentalLocal.Domain.Exceptions;

namespace DentalLocal.Domain.Tests;

public class VisitTests
{
    private static Visit CreateVisit(DateOnly? date = null, TimeOnly? time = null) =>
        Visit.Create(
            patientId: Guid.NewGuid(),
            visitDate: date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            visitTime: time ?? new TimeOnly(10, 0),
            doctorName: "Курбонали",
            notes: null,
            prescriptions: null,
            materialsUsed: null);

    [Fact]
    public void Create_FutureDate_StatusIsPlanned()
    {
        var visit = Visit.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);

        Assert.Equal(VisitStatus.Planned, visit.Status);
    }

    [Fact]
    public void Create_PastDate_StatusIsCompleted()
    {
        var visit = CreateVisit();

        Assert.Equal(VisitStatus.Completed, visit.Status);
    }

    [Fact]
    public void Create_BlankDoctorName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Visit.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), new TimeOnly(10, 0),
                "  ", null, null, null));
    }

    [Fact]
    public void AddItem_RecalculatesTotalCost()
    {
        var visit = CreateVisit();

        visit.AddItem(Guid.NewGuid(), toothNumber: 21, price: 150_000m, quantity: 1, comment: null);
        visit.AddItem(Guid.NewGuid(), toothNumber: null, price: 25_000m, quantity: 2, comment: null);

        Assert.Equal(200_000m, visit.TotalCost);
    }

    [Fact]
    public void ApplyPayment_PartialPayment_UpdatesRemainingAmount()
    {
        var visit = CreateVisit();
        visit.AddItem(Guid.NewGuid(), null, 100_000m, 1, null);

        visit.ApplyPayment(40_000m);

        Assert.Equal(40_000m, visit.TotalPaid);
        Assert.Equal(60_000m, visit.RemainingAmount);
    }

    [Fact]
    public void ApplyPayment_ExceedsRemaining_ThrowsDomainException()
    {
        var visit = CreateVisit();
        visit.AddItem(Guid.NewGuid(), null, 100_000m, 1, null);

        Assert.Throws<DomainException>(() => visit.ApplyPayment(150_000m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void ApplyPayment_NonPositiveAmount_Throws(decimal amount)
    {
        var visit = CreateVisit();
        visit.AddItem(Guid.NewGuid(), null, 100_000m, 1, null);

        Assert.Throws<ArgumentOutOfRangeException>(() => visit.ApplyPayment(amount));
    }

    [Fact]
    public void Confirm_FromPlanned_SetsConfirmed()
    {
        var visit = Visit.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new TimeOnly(10, 0), "Курбонали", null, null, null);

        visit.Confirm();

        Assert.Equal(VisitStatus.Confirmed, visit.Status);
    }

    [Fact]
    public void Confirm_FromCompleted_Throws()
    {
        var visit = CreateVisit();

        Assert.Throws<DomainException>(() => visit.Confirm());
    }
}
