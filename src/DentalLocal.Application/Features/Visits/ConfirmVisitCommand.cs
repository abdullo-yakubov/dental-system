using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Visits;
using MediatR;

namespace DentalLocal.Application.Features.Visits;

/// <summary>Кнопка «Подтвердить» на карточке визита (см. п. 19.3 ТЗ).</summary>
public sealed record ConfirmVisitCommand(Guid Id) : ICommand<VisitDto>;

public sealed class ConfirmVisitCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<ConfirmVisitCommand, Result<VisitDto>>
{
    public async Task<Result<VisitDto>> Handle(ConfirmVisitCommand request, CancellationToken cancellationToken)
    {
        var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (visit is null)
            return Result.Failure<VisitDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        try
        {
            visit.Confirm();
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return Result.Failure<VisitDto>(Error.Validation("Visit.CannotConfirm", ex.Message));
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("VISIT_CONFIRMED", "Visit", visit.Id, null), cancellationToken);

        return Result.Success(mapper.Map<VisitDto>(visit));
    }
}
