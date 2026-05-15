using HomeManager.Domain.Common;

namespace HomeManager.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task CreateAsync(T entity);
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    void Remove(T entity);
    void EditById(T data);
}
