namespace DentalLocal.Application.Common;

public sealed record Error(string Code, string Message, ErrorType Type, IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public static Error None => new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error ValidationFields(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new("Validation.Failed", "Одно или несколько полей заполнены некорректно.", ErrorType.Validation, fieldErrors);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
