using HomeManager.Domain.Common;

namespace HomeManager.Domain.Entities;

public class Storage : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
