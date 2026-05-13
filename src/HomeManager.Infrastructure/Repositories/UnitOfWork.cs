using HomeManager.Domain.Interfaces;
using HomeManager.Infrastructure.Persistence;

namespace HomeManager.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly HomeManagerDbContext _context;

    public UnitOfWork(HomeManagerDbContext context)
    {
        _context = context;
    }
}
