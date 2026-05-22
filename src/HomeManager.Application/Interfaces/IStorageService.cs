using HomeManager.Application.DTOs.Storage;

namespace HomeManager.Application.Interfaces;

public interface IStorageService : INamedEntityService<StorageDto, CreateStorageDto, UpdateStorageDto>
{
}
