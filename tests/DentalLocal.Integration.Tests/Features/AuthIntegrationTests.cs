using DentalLocal.Application.Features.Auth;
using DentalLocal.Integration.Tests.TestSupport;
using DentalLocal.Shared.Common;

namespace DentalLocal.Integration.Tests.Features;

[Collection(DatabaseCollection.Name)]
public class AuthIntegrationTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Login_DefaultAccessCode_ReturnsToken()
    {
        using var scope = fixture.CreateScope();
        var mediator = fixture.CreateMediator(scope);

        var result = await mediator.Send(new LoginCommand(DentalConstants.DefaultAccessCode));

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
        Assert.True(result.Value.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WrongCode_ReturnsValidationError()
    {
        using var scope = fixture.CreateScope();
        var mediator = fixture.CreateMediator(scope);

        var result = await mediator.Send(new LoginCommand("000000"));

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCode", result.Error.Code);
    }
}
