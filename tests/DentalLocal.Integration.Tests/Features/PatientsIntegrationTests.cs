using DentalLocal.Application.Features.Patients;
using DentalLocal.Domain.Enums;
using DentalLocal.Integration.Tests.TestSupport;

namespace DentalLocal.Integration.Tests.Features;

[Collection(DatabaseCollection.Name)]
public class PatientsIntegrationTests(DatabaseFixture fixture)
{
    private static CreatePatientCommand BuildCommand(string phone) => new(
        "Иванов", "Иван", null, Gender.Male, new DateOnly(1990, 5, 20),
        phone, null, null, null, null, null, null);

    [Fact]
    public async Task CreatePatient_PersistsAndAssignsCardNumber()
    {
        using var scope = fixture.CreateScope();
        var mediator = fixture.CreateMediator(scope);
        var phone = $"90{Random.Shared.Next(1000000, 9999999)}";

        var result = await mediator.Send(BuildCommand(phone));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CardNumber > 0);
        Assert.Equal($"+992{phone}", result.Value.Phone);
        Assert.Equal("Иванов Иван", result.Value.FullName);
    }

    [Fact]
    public async Task CreatePatient_DuplicatePhone_ReturnsConflict()
    {
        using var scope = fixture.CreateScope();
        var mediator = fixture.CreateMediator(scope);
        var phone = $"91{Random.Shared.Next(1000000, 9999999)}";

        var first = await mediator.Send(BuildCommand(phone));
        Assert.True(first.IsSuccess);

        var second = await mediator.Send(BuildCommand(phone));

        Assert.True(second.IsFailure);
        Assert.Equal("Patient.PhoneExists", second.Error.Code);
    }
}
