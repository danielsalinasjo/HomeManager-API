using FluentValidation;
using HomeManager.Application.DTOs.Inventory;

namespace HomeManager.Application.Validators.Inventory;

public class CreateInventoryValidator : AbstractValidator<CreateInventoryDto>
{
    public CreateInventoryValidator()
    {
    }
}
