using HomeManager.Domain.Common;

namespace HomeManager.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int? DaysFresh { get; set; }
    public string? Brand { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid CategoryId { get; set; } 
    public Category? Category { get; set; }   
}
