using System.Text.Json;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using MediatR;

namespace DentalLocal.Application.Behaviors;

/// <summary>
/// Пишет запись в AuditLog для каждой успешно выполненной команды (изменяющей данные) — до сих пор
/// в pipeline не было ни одного behavior'а, гарантирующего хоть какой-то след в журнале (см. п.5/11
/// ТЗ). Не заменяет точечные ручные AuditLog-записи в отдельных handler'ах (они информативнее и
/// остаются как есть) — гарантирует лишь, что ни одна изменяющая команда не останется совсем без следа.
///
/// Регистрируется ПОСЛЕ TransactionBehavior (см. Application/DependencyInjection.cs), поэтому
/// выполняется непосредственно перед/после Handler'а — запись попадает в ту же транзакцию и тот же
/// SaveChangesAsync, что и изменения самой команды (SaveChanges вызывает именно TransactionBehavior).
///
/// Тело команды сериализуется с редактированием чувствительных полей (PIN/код/пароль/хэш/токен) —
/// журнал не должен превращаться во второй способ утечки секретов.
/// </summary>
public sealed class AuditLoggingBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>, ITransactionalRequest
    where TResponse : Result
{
    private static readonly string[] SensitivePropertyMarkers = ["pin", "code", "password", "hash", "secret", "token"];

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (response.IsSuccess)
        {
            var requestName = typeof(TRequest).Name;
            var changesJson = SerializeRedacted(request);

            await unitOfWork.AuditLogs.AddAsync(
                AuditLog.Create("COMMAND_EXECUTED", requestName, null, changesJson),
                cancellationToken);
        }

        return response;
    }

    private static string SerializeRedacted(TRequest request)
    {
        var redacted = new Dictionary<string, object?>();
        foreach (var prop in typeof(TRequest).GetProperties())
        {
            var isSensitive = SensitivePropertyMarkers.Any(marker => prop.Name.Contains(marker, StringComparison.OrdinalIgnoreCase));
            redacted[prop.Name] = isSensitive ? "***" : SafeValue(prop.GetValue(request));
        }

        return JsonSerializer.Serialize(redacted);
    }

    private static object? SafeValue(object? value) => value switch
    {
        null => null,
        byte[] => "<binary>",
        _ => value.ToString(),
    };
}
