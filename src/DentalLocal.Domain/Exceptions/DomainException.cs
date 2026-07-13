namespace DentalLocal.Domain.Exceptions;

/// <summary>
/// Выбрасывается при нарушении инварианта домена (например, попытке удалить неудаляемую историю).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
