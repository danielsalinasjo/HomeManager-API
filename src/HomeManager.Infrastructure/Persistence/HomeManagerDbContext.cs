using Microsoft.EntityFrameworkCore;

namespace HomeManager.Infrastructure.Persistence;

public class HomeManagerDbContext : DbContext
{
    public HomeManagerDbContext(DbContextOptions<HomeManagerDbContext> options)
        : base(options)
    {
    }
}
