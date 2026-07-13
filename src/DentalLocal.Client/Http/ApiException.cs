namespace DentalLocal.Client.Http;

/// <summary>Ошибка API, извлечённая из ProblemDetails (RFC 7807) — сообщение уже готово для показа пользователю.</summary>
public sealed class ApiException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
