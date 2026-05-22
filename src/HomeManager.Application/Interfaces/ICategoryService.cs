using HomeManager.Application.DTOs.Category;

namespace HomeManager.Application.Interfaces;

public interface ICategoryService : INamedEntityService<CategoryDto, CreateCategoryDto, UpdateCategoryDto>
{
}
