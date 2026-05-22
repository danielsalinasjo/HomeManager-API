using HomeManager.Application.DTOs.Inventory;

namespace HomeManager.Application.Interfaces;

public interface IInventoryService : IBaseService<InventoryDto, CreateInventoryDto, UpdateInventoryDto>
{
    Task<IEnumerable<InventoryDto>> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryDto>> GetByStorageIdAsync(Guid storageId);
    Task<IEnumerable<InventoryDto>> GetExpirationDateAsync(DateTime expirationDate);
}
