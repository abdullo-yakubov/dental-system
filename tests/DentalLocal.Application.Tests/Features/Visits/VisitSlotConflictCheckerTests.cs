using DentalLocal.Application.Features.Visits;
using DentalLocal.Application.Tests.TestSupport;
using DentalLocal.Domain.Entities;

namespace DentalLocal.Application.Tests.Features.Visits;

public class VisitSlotConflictCheckerTests
{
    private static Visit CreateVisit(DateOnly date, TimeOnly time, string doctorName = "Курбонали") =>
        Visit.Create(Guid.NewGuid(), date, time, doctorName, null, null, null);

    [Fact]
    public async Task HasConflictAsync_SameDoctorSameSlot_ReturnsTrue()
    {
        var uow = new FakeUnitOfWork();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var time = new TimeOnly(10, 0);
        uow.VisitsData.Add(CreateVisit(date, time));

        var hasConflict = await VisitSlotConflictChecker.HasConflictAsync(uow, "Курбонали", date, time, excludeVisitId: null, CancellationToken.None);

        Assert.True(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_DifferentTime_ReturnsFalse()
    {
        var uow = new FakeUnitOfWork();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        uow.VisitsData.Add(CreateVisit(date, new TimeOnly(10, 0)));

        var hasConflict = await VisitSlotConflictChecker.HasConflictAsync(
            uow, "Курбонали", date, new TimeOnly(11, 0), excludeVisitId: null, CancellationToken.None);

        Assert.False(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_CancelledVisitInSlot_ReturnsFalse()
    {
        var uow = new FakeUnitOfWork();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var time = new TimeOnly(10, 0);
        var visit = CreateVisit(date, time);
        visit.Cancel();
        uow.VisitsData.Add(visit);

        var hasConflict = await VisitSlotConflictChecker.HasConflictAsync(uow, "Курбонали", date, time, excludeVisitId: null, CancellationToken.None);

        Assert.False(hasConflict);
    }

    [Fact]
    public async Task HasConflictAsync_ExcludedVisitId_ReturnsFalse()
    {
        // Перенос визита самого на себя (тот же слот) не должен считаться конфликтом.
        var uow = new FakeUnitOfWork();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var time = new TimeOnly(10, 0);
        var visit = CreateVisit(date, time);
        uow.VisitsData.Add(visit);

        var hasConflict = await VisitSlotConflictChecker.HasConflictAsync(uow, "Курбонали", date, time, excludeVisitId: visit.Id, CancellationToken.None);

        Assert.False(hasConflict);
    }
}
