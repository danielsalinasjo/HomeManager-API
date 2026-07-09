using HomeManager.Domain.Common;

namespace HomeManager.Domain.Interfaces;

public interface INamedEntityRepository<T> : IRepository<T>
    where T : BaseEntity
{
    Task<T?> GetByNameAsync(string name);
}
