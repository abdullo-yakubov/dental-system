using AutoMapper;
using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Visits;
using MediatR;

namespace DentalLocal.Application.Features.Visits;

/// <summary>Отмена визита без потери истории (п. 17.2 ТЗ) — статус меняется на Cancelled, запись остаётся.</summary>
public sealed record CancelVisitCommand(Guid Id) : ICommand<VisitDto>;

public sealed class CancelVisitCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<CancelVisitCommand, Result<VisitDto>>
{
    public async Task<Result<VisitDto>> Handle(CancelVisitCommand request, CancellationToken cancellationToken)
    {
        var visit = await unitOfWork.Visits.GetByIdWithItemsAsync(request.Id, cancellationToken);
        if (visit is null)
            return Result.Failure<VisitDto>(Error.NotFound("Visit.NotFound", "Визит не найден."));

        try
        {
            visit.Cancel();
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return Result.Failure<VisitDto>(Error.Validation("Visit.CannotCancel", ex.Message));
        }

        await unitOfWork.AuditLogs.AddAsync(
            AuditLog.Create("VISIT_CANCELLED", "Visit", visit.Id, null), cancellationToken);

        return Result.Success(mapper.Map<VisitDto>(visit));
    }
}
