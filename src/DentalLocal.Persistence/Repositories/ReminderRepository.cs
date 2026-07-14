using DentalLocal.Domain.Entities;
using DentalLocal.Domain.Repositories;

namespace DentalLocal.Persistence.Repositories;

public class ReminderRepository(DentalDbContext context) : RepositoryBase<Reminder>(context), IReminderRepository;
