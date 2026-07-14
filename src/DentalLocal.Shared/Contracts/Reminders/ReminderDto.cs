using DentalLocal.Shared.Enums;

namespace DentalLocal.Shared.Contracts.Reminders;

public sealed record ReminderDto(
    Guid Id,
    Guid PatientId,
    Guid? VisitId,
    string Title,
    DateTimeOffset ReminderDate,
    bool IsCompleted,
    ReminderType Type,
    string? Notes);

public sealed record CreateReminderRequest(
    Guid PatientId,
    Guid? VisitId,
    string Title,
    DateTimeOffset ReminderDate,
    ReminderType Type,
    string? Notes);
