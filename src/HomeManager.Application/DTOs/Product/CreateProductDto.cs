namespace HomeManager.Application.DTOs.Product;

public class CreateProductDto
{
    public required string Name { get; set; } 
    public int? DaysFresh { get; set; }
    public string? Brand { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public Guid CategoryId { get; set; }
}
