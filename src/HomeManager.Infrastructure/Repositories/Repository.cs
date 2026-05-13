using HomeManager.Domain.Common;
using HomeManager.Domain.Interfaces;
using HomeManager.Infrastructure.Persistence;

namespace HomeManager.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly HomeManagerDbContext _context;

    public Repository(HomeManagerDbContext context)
    {
        _context = context;
    }
}
