using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;

namespace DentalLocal.Persistence.Repositories;

public class AuditLogRepository(DentalDbContext context) : RepositoryBase<AuditLog>(context), IAuditLogRepository;
