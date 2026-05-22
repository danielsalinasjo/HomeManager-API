using HomeManager.Application.DTOs.Product;

namespace HomeManager.Application.Interfaces;

public interface IProductService : INamedEntityService<ProductDto, CreateProductDto, UpdateProductDto>
{
    Task<IEnumerable<ProductDto>> GetByBrandAsync(string brand);
    Task<IEnumerable<ProductDto>> GetByPriceRangeAsync(decimal fromPrice, decimal toPrice);
    Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId);
}
