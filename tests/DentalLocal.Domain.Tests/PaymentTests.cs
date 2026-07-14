using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Enums;

namespace DentalLocal.Domain.Tests;

public class PaymentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_NonPositiveAmount_Throws(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Payment.Create(Guid.NewGuid(), Guid.NewGuid(), amount, PaymentMethod.Cash, PaymentKind.AtTreatment, null));
    }

    [Fact]
    public void CreateReversal_CopiesOriginalAmountAndLinksToOriginal()
    {
        var original = Payment.Create(Guid.NewGuid(), Guid.NewGuid(), 50_000m, PaymentMethod.Card, PaymentKind.DebtRepayment, "исходная оплата");

        var reversal = Payment.CreateReversal(original, "ошибка ввода");

        Assert.Equal(original.Amount, reversal.Amount);
        Assert.Equal(original.VisitId, reversal.VisitId);
        Assert.Equal(original.PatientId, reversal.PatientId);
        Assert.Equal(original.Method, reversal.Method);
        Assert.Equal(original.Kind, reversal.Kind);
        Assert.Equal(original.Id, reversal.ReversalOfPaymentId);
        Assert.Equal("ошибка ввода", reversal.Note);
    }
}
