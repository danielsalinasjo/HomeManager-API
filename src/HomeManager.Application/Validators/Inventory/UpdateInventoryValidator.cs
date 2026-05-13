using FluentValidation;
using HomeManager.Application.DTOs.Inventory;

namespace HomeManager.Application.Validators.Inventory;

public class UpdateInventoryValidator : AbstractValidator<UpdateInventoryDto>
{
    public UpdateInventoryValidator()
    {
    }
}
