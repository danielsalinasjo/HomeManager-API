namespace HomeManager.Application.Interfaces
{
    public interface INamedEntityService<TDto, TCreateDto, TUpdateDto> : IBaseService<TDto, TCreateDto, TUpdateDto>
    {
        Task<TDto?> GetByNameAsync(string name);
    }
}
