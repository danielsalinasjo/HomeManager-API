using HomeManager.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace HomeManager.Domain.Entities;

public class Inventory : BaseEntity
{
    public Guid ProductId { get; set;  }
    public required Product Product { get; set; }
    public Guid StorageId { get; set; }
    public required Storage Storage { get; set; }
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
