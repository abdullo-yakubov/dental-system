namespace DentalLocal.Domain.Enums;

public enum PaymentKind : short
{
    /// <summary>Оплата, внесённая непосредственно во время лечения.</summary>
    AtTreatment = 1,

    /// <summary>Погашение ранее возникшего долга.</summary>
    DebtRepayment = 2,
}
