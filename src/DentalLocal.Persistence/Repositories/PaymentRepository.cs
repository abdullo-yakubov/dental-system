using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalLocal.Persistence.Repositories;

public class PaymentRepository(DentalDbContext context) : RepositoryBase<Payment>(context), IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(p => p.Patient)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Payment>> GetByVisitIdAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Include(p => p.Patient)
            .Where(p => p.VisitId == visitId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync(cancellationToken);
}
