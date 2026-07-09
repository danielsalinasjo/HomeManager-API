using HomeManager.Application.DTOs.Category;
using HomeManager.Application.Interfaces;
using HomeManager.Domain.Entities;
using HomeManager.Domain.Interfaces;

namespace HomeManager.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly INamedEntityRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    public CategoryService(INamedEntityRepository<Category> repository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CategoryDto?> GetByNameAsync(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        Category? category = await _categoryRepository.GetByNameAsync(name);

        CategoryDto? categoryDto = category == null ? null : new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };

        return categoryDto;
    }
    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        IEnumerable<Category> categories = await _categoryRepository.GetAllAsync();
        
        IEnumerable<CategoryDto> categoryDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name
        });

        return categoryDtos;
    }
    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        if(id == Guid.Empty)
        {
            return null;
        }

        Category? category = await _categoryRepository.GetByIdAsync(id);

        CategoryDto? categoryDto = category == null ? null : new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };

        return categoryDto;
    }
    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    { 
        if(dto.Name == null || dto.Name.Trim() == "")
        {
            throw new ArgumentException("El nombre de la categoría no puede estar vacío");
        }

        Category? existingCategory = await _categoryRepository.GetByNameAsync(dto.Name);
        if (existingCategory != null)
        {
            throw new ArgumentException("Ya existe una categoría con el mismo nombre");
        }

        Category category = new Category
        {
            Name = dto.Name
        };

        await _categoryRepository.CreateAsync(category);

        await _unitOfWork.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
    }
    public async Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        if(id == Guid.Empty)
        {
            throw new ArgumentException("El id de la categoría no puede estar vacío");
        }

        if(dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        Category? category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
        {
            throw new ArgumentException("No existe una categoría con el id proporcionado");
        }

        category.Name = dto.Name;

        await _unitOfWork.SaveChangesAsync();

        CategoryDto categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name
        };
        
        return categoryDto;
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        if(id == Guid.Empty)
        {
            throw new ArgumentException("El id de la categoría no puede estar vacío");
        }

        Category? category = await _categoryRepository.GetByIdAsync(id);

        if(category == null)
        {
            throw new ArgumentException("No existe una categoría con el id proporcionado");
        }

        _categoryRepository.Remove(category);

        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}
