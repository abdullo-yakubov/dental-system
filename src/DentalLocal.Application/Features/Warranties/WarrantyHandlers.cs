using DentalLocal.Application.Common;
using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using DentalLocal.Shared.Contracts.Warranties;
using MediatR;

namespace DentalLocal.Application.Features.Warranties;

// ── Queries ──────────────────────────────────────────────────────────────────

public sealed record GetPatientWarrantiesQuery(Guid PatientId) : IQuery<IReadOnlyList<WarrantyDto>>;

public sealed class GetPatientWarrantiesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPatientWarrantiesQuery, Result<IReadOnlyList<WarrantyDto>>>
{
    public async Task<Result<IReadOnlyList<WarrantyDto>>> Handle(
        GetPatientWarrantiesQuery request, CancellationToken cancellationToken)
    {
        var warranties = await unitOfWork.Warranties.GetByPatientIdAsync(request.PatientId, cancellationToken);
        return Result.Success<IReadOnlyList<WarrantyDto>>(await ToDtosAsync(warranties, unitOfWork, cancellationToken));
    }

    internal static async Task<IReadOnlyList<WarrantyDto>> ToDtosAsync(
        IReadOnlyList<Warranty> warranties, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        var result = new List<WarrantyDto>(warranties.Count);
        foreach (var w in warranties)
        {
            var procedure = await unitOfWork.Procedures.GetByIdAsync(w.ProcedureId, cancellationToken);
            result.Add(new WarrantyDto(w.Id, w.PatientId, w.VisitId, w.ProcedureId, procedure?.Name ?? string.Empty,
                w.ToothNumber, w.StartDate, w.EndDate, w.Notes));
        }

        return result;
    }
}

// ---------------------------------------------------------------------------

public sealed record GetToothWarrantiesQuery(Guid PatientId, short ToothNumber) : IQuery<IReadOnlyList<WarrantyDto>>;

public sealed class GetToothWarrantiesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetToothWarrantiesQuery, Result<IReadOnlyList<WarrantyDto>>>
{
    public async Task<Result<IReadOnlyList<WarrantyDto>>> Handle(
        GetToothWarrantiesQuery request, CancellationToken cancellationToken)
    {
        var warranties = await unitOfWork.Warranties.GetByToothNumberAsync(request.PatientId, request.ToothNumber, cancellationToken);
        return Result.Success<IReadOnlyList<WarrantyDto>>(
            await GetPatientWarrantiesQueryHandler.ToDtosAsync(warranties, unitOfWork, cancellationToken));
    }
}
