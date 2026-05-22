namespace HomeManager.Application.DTOs.Inventory;

public class InventoryDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid StorageId { get; set; }
    public int Quantity { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? BestBeforeDate { get; set; }
    public bool IsOpened { get; set; }
    public DateTime? OpenedAt { get; set; }
    public bool IsConsumed { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
}
