namespace DentalLocal.Domain.Exceptions;

/// <summary>
/// Кто-то другой изменил ту же запись (например, тот же зуб) между чтением и сохранением —
/// перевод persistence-специфичного DbUpdateConcurrencyException в доменное исключение,
/// чтобы Application-слой мог его перехватить без зависимости от EF Core.
/// </summary>
public class ConcurrencyConflictException(string message, Exception? innerException = null) : Exception(message, innerException);
